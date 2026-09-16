using FreeSql;
using HDWallet.Tron;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;

namespace TokenPay.BgServices;

public class CollectionTRONService : BaseScheduledService
{
    private readonly IConfiguration _configuration;
    private readonly TelegramBot _bot;
    private readonly IFreeSql freeSql;
    private readonly Dictionary<(string ApiUrl, long Energy, int Duration, string TimeUnit), EnergyQuote> _energyQuotes = new();
        /// <summary>
        /// 是否启用归集功能
        /// </summary>
    private bool Enable => _configuration.GetValue("Collection:Enable", false);
        /// <summary>
        /// 是否启用能量租赁
        /// </summary>
    private bool UseEnergy => _configuration.GetValue("Collection:UseEnergy", true);
        /// <summary>
        /// 每次归集操作强制检查所有地址余额
        /// </summary>
    private bool ForceCheckAllAddress => _configuration.GetValue("Collection:ForceCheckAllAddress", false);
        /// <summary>
        /// 是否保留0.000001USDT
        /// </summary>
    private bool RetainUSDT => _configuration.GetValue("Collection:RetainUSDT", true);
        /// <summary>
        /// 最小归集USDT
        /// </summary>
    private decimal MinUSDT => _configuration.GetValue("Collection:MinUSDT", 0.1m);
        /// <summary>
        /// 消耗能量数量（请勿修改）
        /// </summary>
    private long DefaultNeedEnergy => _configuration.GetValue("Collection:NeedEnergy", 64285);
        /// <summary>
        /// 最低租赁能量数量（请勿修改）
        /// </summary>
    private long EnergyMinValue => _configuration.GetValue("Collection:EnergyMinValue", 64400);
        /// <summary>
        /// 当前能量单价（请勿修改）
        /// </summary>
    private decimal EnergyPrice => _configuration.GetValue("Collection:EnergyPrice", 100m);
        /// <summary>
        /// 租赁能量时长（请勿修改）
        /// </summary>
    private int RentDuration => _configuration.GetValue("Collection:RentDuration", 10);
        /// <summary>
        /// 租赁能量时长单位（请勿修改）
        /// </summary>
    private string RentTimeUnit => _configuration.GetValue("Collection:RentTimeUnit", "m")!;
        /// <summary>
        /// 归集收款地址
        /// </summary>
    private string Address => _configuration.GetValue<string>("Collection:Address")!;

    public CollectionTRONService(
        IConfiguration configuration,
        TelegramBot bot,
        IFreeSql freeSql,
        ILogger<CollectionTRONService> logger)
        : base("TRON归集任务", TimeSpan.FromHours(configuration.GetValue("Collection:CheckTime", 1)), logger)
    {
        _configuration = configuration;
        _bot = bot;
        this.freeSql = freeSql;
    }

    protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
    {
        if (!Enable) return;

        using var tron = new TronCollectionClient(_configuration);
        using var energyApi = new EnergyApi(_logger, _configuration);
        var mainWallet = await GetMainWalletAsync(tron, stoppingToken);
        if (!await ValidateCollectionAddressAsync(tron, stoppingToken)) return;

        var repository = freeSql.GetRepository<Tokens>();
        var list = await RefreshBalancesAsync(tron, repository, stoppingToken);

        // USDT 转账可能消耗 TRX 和带宽，相关地址留到下一轮刷新余额后再归集 TRX，节省手续费钱包TRX。
        var trxList = list.Where(x => x.USDT <= MinUSDT).ToList();
        await CollectUsdtAsync(tron, energyApi, mainWallet, repository, list, stoppingToken);
        await CollectTrxAsync(tron, repository, trxList, stoppingToken);
    }

    private async Task<TronWallet> GetMainWalletAsync(TronCollectionClient tron, CancellationToken stoppingToken)
    {
        var sendToTelegram = false;
        if (!File.Exists("手续费钱包私钥.txt"))
        {
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            File.WriteAllText("手续费钱包私钥.txt", Convert.ToHexString(ecKey.GetPrivateKeyAsBytes()));
            sendToTelegram = true;
        }

        var privateKey = File.ReadAllText("手续费钱包私钥.txt").Trim();
        var mainWallet = new TronWallet(privateKey);
        _logger.LogInformation("手续费钱包地址为：{a}", mainWallet.Address);
        if (sendToTelegram)
        {
            await _bot.SendTextMessageAsync(@$"<b>创建手续费钱包</b>

手续费钱包地址：<code>{mainWallet.Address}</code>
手续费钱包私钥：<tg-spoiler>
{privateKey[..32]}
{privateKey[32..]}
</tg-spoiler>
非必要，请不要复制此私钥！！！
为避免被盗，已拆分私钥为两段，请分段复制

<b>请向此地址转入TRX用于归集USDT</b>
");
        }

        await WaitForMainWalletTrxAsync(tron, mainWallet, 10m, stoppingToken);
        return mainWallet;
    }

    private async Task<bool> ValidateCollectionAddressAsync(TronCollectionClient tron, CancellationToken stoppingToken)
    {
        try
        {
            Address.Base58ToHex();
        }
        catch (Exception)
        {
            _logger.LogError("归集收款地址{a}有误！", Address);
            await _bot.SendTextMessageAsync(@$"归集收款地址有误，请检查

归集收款地址：<code>{Address}</code>");
            return false;
        }

        var usdt = await tron.UsdtAsync(Address, stoppingToken);
        if (usdt <= 0)
        {
            _logger.LogError("归集收款地址{a}必须有USDT！", Address);
            await _bot.SendTextMessageAsync(@$"归集收款地址必须有USDT

归集收款地址：<code>{Address}</code>");
            return false;
        }

        var trx = await tron.TrxAsync(Address, stoppingToken);
        _logger.LogInformation("归集收款地址，当前TRX余额：{trx}，当前USDT余额：{usdt}", trx, usdt);
        await _bot.SendTextMessageAsync(@$"归集收款地址余额

归集收款地址：<code>{Address}</code>
当前TRX余额：{trx} TRX
当前USDT余额：{usdt} USDT");
        return true;
    }

    private async Task<List<Tokens>> RefreshBalancesAsync(
        TronCollectionClient tron,
        IBaseRepository<Tokens> repository,
        CancellationToken stoppingToken)
    {
        var list = await repository.Where(x => x.Currency == TokenCurrency.TRX)
            .Where(x => ForceCheckAllAddress || !x.LastCheckTime.HasValue || x.USDT > MinUSDT || x.Value > 0.5m)
            .ToListAsync();
        var count = 0;
        foreach (var item in list)
        {
            if (stoppingToken.IsCancellationRequested) break;
            if (!ForceCheckAllAddress && item.LastCheckTime.HasValue &&
                (DateTime.Now - item.LastCheckTime.Value).TotalHours <= 24)
                continue;

            if (count > 0) await Task.Delay(1500, stoppingToken);
            item.Value = await tron.TrxAsync(item.Address, stoppingToken);
            item.USDT = await tron.UsdtAsync(item.Address, stoppingToken);
            item.LastCheckTime = DateTime.Now;
            await repository.UpdateAsync(item);
            _logger.LogInformation("更新地址余额数据：{a}/{b}，TRX：{TRX}，USDT：{USDT}",
                ++count, list.Count, item.Value, item.USDT);
        }
        list = await repository.Where(x => x.Currency == TokenCurrency.TRX)
            .Where(x => x.USDT > MinUSDT || x.Value > 0.5m)
            .ToListAsync();
        _logger.LogInformation(@"共计查询到{count}个需要归集的地址，有TRX的地址有{a}个，共有 {b} TRX，有USDT的地址有{c}个，共有 {d} USDT",
            list.Count,
            list.Count(x => x.Value > 0.5m),
            list.Where(x => x.Value > 0.5m).Sum(x => x.Value),
            list.Count(x => x.USDT > MinUSDT),
            list.Where(x => x.USDT > MinUSDT).Sum(x => x.USDT));

        return list;
    }

    private async Task CollectTrxAsync(
        TronCollectionClient tron,
        IBaseRepository<Tokens> repository,
        List<Tokens> list,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("------------------------------");
        if (list.Any(x => x.Value > 0.5m))
            _logger.LogInformation("开始归集TRX");
        else
            _logger.LogInformation("跳过归集TRX");

        foreach (var item in list.Where(x => x.Value > 0.5m))
        {
            if (stoppingToken.IsCancellationRequested) return;
            var amount = item.Value;
            try
            {
                var wallet = new TronWallet(item.Key);
                var transaction = await tron.BuildTrxAsync(wallet, Address, amount, stoppingToken);
                var resource = await tron.ResourcesAsync(wallet.Address, stoppingToken);
                if (!TronCollectionClient.HasBandwidth(resource, TronCollectionClient.Bandwidth(transaction)))
                {
                    _logger.LogWarning("归集TRX失败，地址：{a}，缺少带宽", wallet.Address);
                    continue;
                }

                await tron.BroadcastAsync(transaction, stoppingToken);
                var txid = transaction.Value<string>("txID");
                _logger.LogInformation("归集TRX成功，TRX：{a}，Txid：{b}", amount, txid);
                item.Value = 0;
                await repository.UpdateAsync(item);
                await _bot.SendTextMessageAsync(@$"归集TRX成功！

归集地址：<code>{item.Address}</code>
归集数量：{amount} TRX
交易哈希：{txid} <b><a href=""https://tronscan.org/#/transaction/{txid}?lang=zh"">查看交易</a></b>", cancellationToken: stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning("归集TRX失败，失败原因：{b}", e.Message);
            }
        }
    }

    private async Task CollectUsdtAsync(
        TronCollectionClient tron,
        EnergyApi energyApi,
        TronWallet mainWallet,
        IBaseRepository<Tokens> repository,
        List<Tokens> list,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("------------------------------");
        if (list.Any(x => x.USDT > MinUSDT))
            _logger.LogInformation("开始归集USDT");
        else
        {
            _logger.LogInformation("跳过归集USDT");
            return;
        }

        var chainParameters = await tron.GetChainParametersAsync(stoppingToken);
        var energyFeeSun = chainParameters.GetValueOrDefault("getEnergyFee", checked((long)EnergyPrice));
        var bandwidthFeeSun = chainParameters.GetValueOrDefault("getTransactionFee", 1000);
        var activationFeeSun = chainParameters.GetValueOrDefault("getCreateNewAccountFeeInSystemContract", 1_000_000);

        foreach (var item in list.Where(x => x.USDT > MinUSDT))
        {
            if (stoppingToken.IsCancellationRequested) return;
            try
            {
                // 无论本轮是否成功，手续费补充或消耗后都需要在下一轮刷新余额。
                item.LastCheckTime = null;
                await repository.UpdateAsync(item);
                await CollectSingleUsdtAsync(tron, energyApi, mainWallet, repository, item,
                    energyFeeSun, bandwidthFeeSun, activationFeeSun, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning("归集USDT失败，失败原因：{b}", e.Message);
            }
        }
    }

    private async Task CollectSingleUsdtAsync(
        TronCollectionClient tron,
        EnergyApi energyApi,
        TronWallet mainWallet,
        IBaseRepository<Tokens> repository,
        Tokens item,
        long energyFeeSun,
        long bandwidthFeeSun,
        long activationFeeSun,
        CancellationToken stoppingToken)
    {
        var wallet = new TronWallet(item.Key);
        if (!await EnsureActivatedAsync(tron, mainWallet, wallet.Address, bandwidthFeeSun, activationFeeSun, stoppingToken))
            return;

        var retainAmount = RetainUSDT ? 0.000001m : 0;
        var transferAmount = item.USDT - retainAmount;
        var estimatedEnergy = await EstimateEnergyAsync(tron, wallet.Address, transferAmount, stoppingToken);
        var resource = await tron.ResourcesAsync(wallet.Address, stoppingToken);
        var availableEnergy = Math.Max(0, resource.EnergyLimit - resource.EnergyUsed);
        var needEnergy = Math.Max(0, estimatedEnergy - availableEnergy);

        if (needEnergy > 0 && UseEnergy)
        {
            var rentalEnergy = Math.Max(needEnergy, EnergyMinValue);
            if (!await RentEnergyAsync(tron, energyApi, mainWallet, wallet.Address,
                    rentalEnergy, estimatedEnergy, bandwidthFeeSun, stoppingToken))
                return;
            resource = await tron.ResourcesAsync(wallet.Address, stoppingToken);
            availableEnergy = Math.Max(0, resource.EnergyLimit - resource.EnergyUsed);
            needEnergy = Math.Max(0, estimatedEnergy - availableEnergy);
            if (needEnergy > 0)
            {
                _logger.LogWarning("归集USDT失败，能量租赁后仍不足，地址：{a}，缺少能量：{e}", wallet.Address, needEnergy);
                return;
            }
        }

        var feeLimit = checked(Math.Max(estimatedEnergy, 1) * energyFeeSun);
        var transaction = await tron.BuildUsdtAsync(wallet, Address, transferAmount, feeLimit, stoppingToken);
        var bandwidth = TronCollectionClient.Bandwidth(transaction);
        var bandwidthTrx = TronCollectionClient.HasBandwidth(resource, bandwidth)
            ? 0
            : bandwidth * bandwidthFeeSun / 1_000_000m;
        var energyTrx = UseEnergy ? 0 : needEnergy * energyFeeSun / 1_000_000m;
        var requiredTrx = energyTrx + bandwidthTrx;
        if (!await EnsureTransferFeeAsync(tron, mainWallet, wallet.Address, requiredTrx, bandwidthFeeSun, stoppingToken))
            return;

        if (transaction["raw_data"]!.Value<long>("expiration") <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            transaction = await tron.BuildUsdtAsync(wallet, Address, transferAmount, feeLimit, stoppingToken);
        await tron.BroadcastAsync(transaction, stoppingToken);
        var txid = transaction.Value<string>("txID");
        _logger.LogInformation("归集USDT成功，USDT：{a}，Txid：{b}", transferAmount, txid);
        item.USDT = 0;
        await repository.UpdateAsync(item);
        await _bot.SendTextMessageAsync(@$"归集USDT成功！

归集地址：<code>{item.Address}</code>
归集数量：{transferAmount} USDT
交易哈希：{txid} <b><a href=""https://tronscan.org/#/transaction/{txid}?lang=zh"">查看交易</a></b>", cancellationToken: stoppingToken);
    }

    private async Task<long> EstimateEnergyAsync(
        TronCollectionClient tron,
        string address,
        decimal amount,
        CancellationToken stoppingToken)
    {
        try
        {
            return await tron.EstimateEnergyAsync(address, Address, amount, stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning("预估USDT转账能量失败，使用配置值：{energy}，失败原因：{msg}", DefaultNeedEnergy, e.Message);
            return DefaultNeedEnergy;
        }
    }

    private async Task<bool> EnsureActivatedAsync(
        TronCollectionClient tron,
        TronWallet mainWallet,
        string address,
        long bandwidthFeeSun,
        long activationFeeSun,
        CancellationToken stoppingToken)
    {
        var account = await tron.AccountAsync(address, stoppingToken);
        if (account.Value<long?>("create_time").GetValueOrDefault() != 0) return true;

        _logger.LogInformation("地址未激活，激活：{a}", address);
        var activation = await SendMainTrxAsync(tron, mainWallet, address, 0.000001m,
            bandwidthFeeSun, activationFeeSun, stoppingToken);
        if (activation && await WaitUntilAsync(async ct =>
                (await tron.AccountAsync(address, ct)).Value<long?>("create_time").GetValueOrDefault() != 0,
                10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), $"等待地址激活：{address}", stoppingToken))
        {
            _logger.LogInformation("激活成功，地址：{a}", address);
            return true;
        }

        _logger.LogWarning("激活失败，跳过此地址，地址：{a}", address);
        return false;
    }

    private async Task<bool> EnsureTransferFeeAsync(
        TronCollectionClient tron,
        TronWallet mainWallet,
        string address,
        decimal requiredTrx,
        long bandwidthFeeSun,
        CancellationToken stoppingToken)
    {
        var nowTrx = await tron.TrxAsync(address, stoppingToken);
        if (nowTrx >= requiredTrx) return true;

        var feeTransfer = await SendMainTrxAsync(tron, mainWallet, address,
            requiredTrx - nowTrx, bandwidthFeeSun, 0, stoppingToken);
        if (feeTransfer && await WaitUntilAsync(
                async ct => await tron.TrxAsync(address, ct) >= requiredTrx,
                10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), $"等待手续费到账：{address}", stoppingToken))
        {
            _logger.LogInformation("转账手续费成功，地址：{a}", address);
            return true;
        }

        _logger.LogWarning("转账手续费失败，跳过此地址，地址：{a}", address);
        return false;
    }

    private async Task<bool> WaitUntilAsync(
        Func<CancellationToken, Task<bool>> condition,
        int maxAttempts,
        TimeSpan interval,
        TimeSpan timeout,
        string operation,
        CancellationToken stoppingToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(timeout);
        var token = timeoutCts.Token;
        Exception? lastError = null;
        try
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                if (attempt > 0) await Task.Delay(interval, token);
                try
                {
                    var completed = await condition(token);
                    token.ThrowIfCancellationRequested();
                    lastError = null;
                    if (completed) return true;
                }
                catch (Exception e) when (!token.IsCancellationRequested && e is not OperationCanceledException)
                {
                    lastError = e;
                    _logger.LogWarning(e, "{operation}查询失败，第{attempt}/{maxAttempts}次", operation, attempt + 1, maxAttempts);
                }
            }
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
            stoppingToken.ThrowIfCancellationRequested();
            _logger.LogWarning("{operation}超时，等待上限：{timeout}，最近查询错误：{error}",
                operation, timeout, lastError?.Message ?? "无");
            return false;
        }
        stoppingToken.ThrowIfCancellationRequested();
        _logger.LogWarning("{operation}已达到{maxAttempts}次查询上限，{reason}", operation, maxAttempts,
            lastError == null ? "条件仍未满足" : $"最后一次查询失败：{lastError.Message}");
        return false;
    }

    private async Task<bool> RentEnergyAsync(
        TronCollectionClient tron,
        EnergyApi energyApi,
        TronWallet mainWallet,
        string targetAddress,
        long rentalEnergy,
        long requiredEnergy,
        long bandwidthFeeSun,
        CancellationToken stoppingToken)
    {
        var quoteKey = (
            ApiUrl: _configuration.GetValue("Collection:EnergyApiUrl", "https://api-energy.trxd.win")!,
            Energy: rentalEnergy,
            Duration: RentDuration,
            TimeUnit: RentTimeUnit.ToLowerInvariant());
        var quoteRefreshed = false;
        EnergyOrder order;
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var quote = _energyQuotes.GetValueOrDefault(quoteKey);
            try
            {
                if (quote == null)
                {
                    quote = await energyApi.GetQuoteAsync(new EnergyQuoteRequest
                    {
                        Energy = quoteKey.Energy,
                        Duration = quoteKey.Duration,
                        TimeUnit = quoteKey.TimeUnit
                    }, stoppingToken);
                    _energyQuotes[quoteKey] = quote;
                    _logger.LogInformation("能量价格预估：{@result}", quote);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "能量价格预估失败！能量数量：{energy}", rentalEnergy);
                await _bot.SendTextMessageAsync(@$"能量价格预估失败！

能量数量：{rentalEnergy}", cancellationToken: stoppingToken);
                return false;
            }

            var payment = await tron.BuildTrxAsync(mainWallet, quote.PaymentAddress, quote.PaymentAmount, stoppingToken);
            var paymentBandwidth = TronCollectionClient.Bandwidth(payment);
            var mainResource = await tron.ResourcesAsync(mainWallet.Address, stoppingToken);
            var paymentCost = quote.PaymentAmount + (TronCollectionClient.HasBandwidth(mainResource, paymentBandwidth)
                ? 0
                : paymentBandwidth * bandwidthFeeSun / 1_000_000m);
            if (await WaitForMainWalletTrxAsync(tron, mainWallet, paymentCost, stoppingToken)) continue;

            try
            {
                order = await energyApi.CreatePaidOrderAsync(new EnergyPaidOrder
                {
                    QuoteId = quote.QuoteId,
                    TargetAddress = targetAddress,
                    Energy = quote.Energy,
                    Duration = quote.Duration,
                    TimeUnit = quote.TimeUnit,
                    SignedTransaction = payment
                }, stoppingToken);
                _logger.LogInformation("能量下单，订单信息：{@order}", order);
                break;
            }
            catch (EnergyApiException e) when (e.Code is "PAYMENT_ADDRESS_DISABLED" or "QUOTE_CHANGED")
            {
                _energyQuotes.Remove(quoteKey);
                _logger.LogWarning(e, "能量报价已失效，报价：{quoteId}，是否已刷新重试：{refreshed}",
                    quote.QuoteId, quoteRefreshed);
                if (quoteRefreshed) return false;
                quoteRefreshed = true;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning("归集USDT失败，能量租赁失败，失败原因：{msg}\n请求参数：{@CreateModel}", e.Message,
                    new { quote.QuoteId, TargetAddress = targetAddress, Energy = quote.Energy, quote.Duration, quote.TimeUnit });
                return false;
            }
        }

        var orderResolved = true;
        if (order.Status != EnergyOrderStatus.Active && !IsTerminalFailure(order.Status))
        {
            orderResolved = await WaitUntilAsync(async ct =>
            {
                order = await energyApi.GetOrderAsync(order.OrderNo, ct);
                return order.Status == EnergyOrderStatus.Active || IsTerminalFailure(order.Status);
            }, 100, TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(5),
                $"等待能量订单：{order.OrderNo}", stoppingToken);
        }
        stoppingToken.ThrowIfCancellationRequested();
        if (!orderResolved || order.Status != EnergyOrderStatus.Active)
        {
            _logger.LogWarning("能量订单未生效，订单：{orderNo}，当前状态：{status}", order.OrderNo, order.Status);
            return false;
        }

        return await WaitUntilAsync(async ct =>
        {
            var resource = await tron.ResourcesAsync(targetAddress, ct);
            var energy = Math.Max(0, resource.EnergyLimit - resource.EnergyUsed);
            if (energy < requiredEnergy) return false;
            _logger.LogInformation("能量租赁成功，当前能量：{e}，地址：{a}", energy, targetAddress);
            return true;
        }, 5, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(15),
            $"等待能量到账：{targetAddress}", stoppingToken);
    }

    private static bool IsTerminalFailure(EnergyOrderStatus status) => status is
        EnergyOrderStatus.Failed or EnergyOrderStatus.RefundPending or EnergyOrderStatus.Refunded or
        EnergyOrderStatus.ManualReview or EnergyOrderStatus.ReclaimDue or EnergyOrderStatus.Reclaiming or
        EnergyOrderStatus.Completed;

    private async Task<bool> SendMainTrxAsync(
        TronCollectionClient tron,
        TronWallet mainWallet,
        string to,
        decimal amount,
        long bandwidthFeeSun,
        long activationFeeSun,
        CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var transaction = await tron.BuildTrxAsync(mainWallet, to, amount, stoppingToken);
            var resource = await tron.ResourcesAsync(mainWallet.Address, stoppingToken);
            var cost = amount + activationFeeSun / 1_000_000m;
            if (!TronCollectionClient.HasBandwidth(resource, TronCollectionClient.Bandwidth(transaction)))
                cost += TronCollectionClient.Bandwidth(transaction) * bandwidthFeeSun / 1_000_000m;
            // 充值等待没有时间上限，等待后重新构建交易并计算费用。
            if (await WaitForMainWalletTrxAsync(tron, mainWallet, cost, stoppingToken)) continue;
            try
            {
                await tron.BroadcastAsync(transaction, stoppingToken);
                return true;
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested && e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "手续费钱包转账失败，地址：{address}，金额：{amount}", to, amount);
                return false;
            }
        }
    }

    // 返回是否等待过充值，调用方据此刷新报价或重建交易。
    private async Task<bool> WaitForMainWalletTrxAsync(
        TronCollectionClient tron,
        TronWallet mainWallet,
        decimal minTrx,
        CancellationToken stoppingToken)
    {
        const int trxCheckTime = 10;
        var waited = false;
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var mainTrx = await tron.TrxAsync(mainWallet.Address, stoppingToken);
            if (mainTrx >= minTrx)
            {
                if (waited)
                    _logger.LogInformation("充值完成，当前TRX余额：{trx}", mainTrx);
                return waited;
            }

            _logger.LogWarning("手续费钱包TRX不足！需要TRX：{minTrx}，当前TRX：{mainTrx}", minTrx, mainTrx);
            await _bot.SendTextMessageAsync(@$"手续费钱包TRX不足，等待充值后继续归集。

手续费钱包地址：<code>{mainWallet.Address}</code>
当前TRX余额：{mainTrx} TRX
本次需要余额：{minTrx} TRX

请先充值TRX，余额检查将在 {trxCheckTime} 秒后重试。

如无需使用归集功能，请将配置文件中的<b>Collection:Enable</b>配置为<b>false</b>", cancellationToken: stoppingToken);
            waited = true;
            await Task.Delay(TimeSpan.FromSeconds(trxCheckTime), stoppingToken);
        }
    }
}
