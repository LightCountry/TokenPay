using Flurl;
using Flurl.Http;
using FreeSql;
using HDWallet.Tron;
using Nethereum.Signer;
using Org.BouncyCastle.Asn1.X509;
using System.Collections.Generic;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class CollectionTRONService : BaseScheduledService
    {
        private readonly IConfiguration _configuration;
        private readonly TelegramBot _bot;
        private readonly IFreeSql freeSql;
        private readonly ILogger<CollectionTRONService> _logger;
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
        private long DefaultNeedEnergy => _configuration.GetValue("Collection:NeedEnergy", 31895);
        /// <summary>
        /// 最低租赁能量数量（请勿修改）
        /// </summary>
        private long EnergyMinValue => _configuration.GetValue("Collection:EnergyMinValue", 32000);
        /// <summary>
        /// 当前能量单价（请勿修改）
        /// </summary>
        private decimal EnergyPrice => _configuration.GetValue("Collection:EnergyPrice", 420m);
        /// <summary>
        /// 归集收款地址
        /// </summary>
        private string Address => _configuration.GetValue<string>("Collection:Address")!;
        private int CheckTime => _configuration.GetValue("Collection:CheckTime", 3);
        /// <summary>
        /// 预估带宽消耗的TRX
        /// </summary>
        private decimal NetUsedTrx => 0.3m;
        private EnergyApi energyApi => new EnergyApi(_logger, _configuration);
        public CollectionTRONService(
            IConfiguration configuration,
            TelegramBot bot,
            IFreeSql freeSql,
            ILogger<CollectionTRONService> logger) : base("TRON归集任务", TimeSpan.FromHours(configuration.GetValue("Collection:CheckTime", 1)), logger)
        {
            this._configuration = configuration;
            this._logger = logger;
            this._bot = bot;
            this.freeSql = freeSql;
        }
        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            if (!Enable) return;
            var SendToTelegram = false;
            if (!File.Exists("手续费钱包私钥.txt"))
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var rawPrivateKey = ecKey.GetPrivateKeyAsBytes();
                var hex = Convert.ToHexString(rawPrivateKey);
                File.WriteAllText("手续费钱包私钥.txt", hex);
                SendToTelegram = true;
            }
            var privateKey = File.ReadAllText("手续费钱包私钥.txt");
            var mainWallet = new TronWallet(privateKey);
            _logger.LogInformation("手续费钱包地址为：{a}", mainWallet.Address);
            if (SendToTelegram)
            {
                await _bot.SendTextMessageAsync(@$"<b>创建手续费钱包</b>

手续费钱包地址：<code>{mainWallet.Address}</code>
手续费钱包私钥：<tg-spoiler>
{privateKey.Substring(0, 32)}
{privateKey.Substring(32, 32)}
</tg-spoiler>
非必要，请不要复制此私钥！！！
为避免被盗，已拆分私钥为两段，请分段复制

<b>请向此地址转入TRX用于归集USDT</b>
");
            }
            var mainTrx = await QueryTronAction.GetTRXAsync(mainWallet.Address);
            _logger.LogInformation("手续费钱包当前TRX余额：{trx}", mainTrx);
            if (mainTrx < 1)
            {
                while (!stoppingToken.IsCancellationRequested && mainTrx < 1)
                {
                    var TrxCheckTime = 10;
                    _logger.LogInformation("手续费钱包地址为：{a}", mainWallet.Address);
                    _logger.LogInformation("等待向手续费钱包充值TRX");
                    mainTrx = await QueryTronAction.GetTRXAsync(mainWallet.Address);
                    if (mainTrx > 1)
                        _logger.LogInformation("充值完成，当前TRX余额：{trx}", mainTrx);
                    else
                    {
                        await _bot.SendTextMessageAsync(@$"手续费钱包地址需要充值TRX

手续费钱包地址：<code>{mainWallet.Address}</code>
当前TRX余额：{mainTrx} TRX


请先充值TRX，余额检查将在 {TrxCheckTime} 秒后重试。

如无需使用归集功能，请将配置文件中的<b>Collection:Enable</b>配置为<b>false</b>");
                    }
                    await Task.Delay(TrxCheckTime * 1000);
                }
            }
            try
            {
                Address.Base58ToHex();
            }
            catch (Exception)
            {
                _logger.LogError("归集收款地址{a}有误！", Address);
                await _bot.SendTextMessageAsync(@$"归集收款地址有误，请检查

归集收款地址：<code>{Address}</code>");
                return;
            }
            var usdt = await QueryTronAction.GetUsdtAmountAsync(Address);
            if (usdt <= 0)
            {
                _logger.LogError("归集收款地址{a}必须有USDT！", Address);
                await _bot.SendTextMessageAsync(@$"归集收款地址必须有USDT

归集收款地址：<code>{Address}</code>");
                return;
            }
            else
            {
                var trx = await QueryTronAction.GetTRXAsync(Address);
                _logger.LogInformation("归集收款地址，当前TRX余额：{trx}，当前USDT余额：{usdt}", trx, usdt);
                await _bot.SendTextMessageAsync(@$"归集收款地址余额

归集收款地址：<code>{Address}</code>
当前TRX余额：{trx} USDT
当前USDT余额：{usdt} USDT");
            }
            var _repository = freeSql.GetRepository<Tokens>();
            var list = await _repository.Where(x => x.Currency == TokenCurrency.TRX).Where(x => ForceCheckAllAddress || (x.USDT > MinUSDT || x.Value > 0.5m)).ToListAsync();
            var count = 0;
            foreach (var item in list)
            {
                if (stoppingToken.IsCancellationRequested) return;
                if (item.LastCheckTime.HasValue && (DateTime.Now - item.LastCheckTime.Value).TotalHours <= 1)
                {
                    //避免短时间重复检查余额
                    continue;
                }
                var TRX = await QueryTronAction.GetTRXAsync(item.Address);
                var USDT = await QueryTronAction.GetUsdtAmountAsync(item.Address);
                item.Value = TRX;
                item.USDT = USDT;
                item.LastCheckTime = DateTime.Now;
                await _repository.UpdateAsync(item);
                _logger.LogInformation("更新地址余额数据：{a}/{b}，TRX：{TRX}，USDT：{USDT}", ++count, list.Count, TRX, USDT);
                await Task.Delay(1500);
            }
            list = await _repository.Where(x => x.Currency == TokenCurrency.TRX).Where(x => x.USDT > MinUSDT || x.Value > 0.5m).ToListAsync();
            _logger.LogInformation(@"共计查询到{count}个需要归集的地址，有TRX的地址有{a}个，共有 {b} TRX，有USDT的地址有{c}个，共有 {d} USDT",
                list.Count,
                list.Where(x => x.Value > 0.5m).Count(),
                list.Where(x => x.Value > 0.5m).Sum(x => x.Value),
                list.Where(x => x.USDT > MinUSDT).Count(),
                list.Where(x => x.USDT > MinUSDT).Sum(x => x.USDT));
            Func<int, Task<(decimal, string)>> GetPrice = async (int ResourceValue) =>
            {
                var resp = await energyApi.OrderPrice(ResourceValue);
                _logger.LogInformation("能量价格预估：{@result}", resp);
                if (resp != null && resp.Code == 0)
                {
                    var EnergyPayAddress = resp.Data.PayAddress;
                    var EnergyPrice = resp.Data.Price;
                    return (EnergyPrice, EnergyPayAddress);
                }
                _logger.LogError("能量价格预估失败！");
                await _bot.SendTextMessageAsync(@$"能量价格预估失败！

能量数量：{ResourceValue}");
                return (0, string.Empty);
            };
            _logger.LogInformation("------------------------------");
            if (list.Where(x => x.Value > 0.5m).Any())
                _logger.LogInformation("开始归集TRX");
            else
                _logger.LogInformation("跳过归集TRX");
            foreach (var item in list.Where(x => x.Value > 0.5m))
            {
                if (stoppingToken.IsCancellationRequested) return;
                var wallet = new TronWallet(item.Key);

                var account = await QueryTronAction.GetAccountResourceAsync(wallet.Address);
                if (account.FreeNetLimit - account.FreeNetUsed < 280)
                {
                    continue;
                }
                var (success, txid) = await wallet.TransferTrxAsync(item.Value, Address);
                if (success)
                {
                    _logger.LogInformation("归集TRX成功，TRX：{a}，Txid：{b}", item.Value, txid);
                    item.Value = 0;
                    await _repository.UpdateAsync(item);
                    await _bot.SendTextMessageAsync(@$"归集TRX成功！

归集地址：<code>{item.Address}</code>
归集数量：{item.Value} TRX
交易哈希：{txid} <b><a href=""https://tronscan.org/#/transaction/{txid}?lang=zh"">查看交易</a></b>");
                }
                else
                {
                    _logger.LogWarning("归集TRX失败，失败原因：{b}", txid);
                }
            }
            _logger.LogInformation("------------------------------");
            if (list.Where(x => x.USDT > MinUSDT).Any())
                _logger.LogInformation("开始归集USDT");
            else
                _logger.LogInformation("跳过归集USDT");
            foreach (var item in list.Where(x => x.USDT > MinUSDT))
            {
                if (stoppingToken.IsCancellationRequested) return;
                var wallet = new TronWallet(item.Key);
                var account = await QueryTronAction.GetAccountAsync(wallet.Address);
                if (account.CreateTime == 0)
                {
                    _logger.LogInformation("地址未激活，激活：{a}", wallet.Address);

                    if (!await CheckMainWalletTrx(mainWallet, NetUsedTrx))
                    {
                        return;
                    }
                    var (success2, txid3) = await mainWallet.TransferTrxAsync(0.000001m, wallet.Address);
                    if (success2)
                    {
                        _logger.LogInformation("激活成功，地址：{a}", wallet.Address);
                    }
                    else
                    {
                        _logger.LogWarning("激活失败，跳过此地址，地址：{a}", wallet.Address);
                        continue;
                    }
                }
                var NeedEnergy = DefaultNeedEnergy;
                var accountResource = await QueryTronAction.GetAccountResourceAsync(wallet.Address);
                var needNet = accountResource.FreeNetLimit - accountResource.FreeNetUsed < 400;
                var energy = accountResource.EnergyLimit - accountResource.EnergyUsed;
                NeedEnergy -= energy;
                if (NeedEnergy > 0)
                {
                    if (!UseEnergy)
                    {
                        var trx = NeedEnergy * EnergyPrice / 1_000_000;
                        if (needNet)
                        {
                            trx += 0.5m;
                        }
                        var nowTrx = await QueryTronAction.GetTRXAsync(wallet.Address);
                        if (nowTrx < trx)
                        {
                            if (!await CheckMainWalletTrx(mainWallet, trx - nowTrx + NetUsedTrx))
                            {
                                return;
                            }
                            var (success2, txid3) = await mainWallet.TransferTrxAsync(trx - nowTrx, wallet.Address);
                            if (success2)
                            {
                                _logger.LogInformation("转账手续费成功，地址：{a}", wallet.Address);
                            }
                            else
                            {
                                _logger.LogWarning("转账手续费失败，跳过此地址，地址：{a}", wallet.Address);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (needNet)
                        {
                            var trx = 0.5m;
                            var nowTrx = await QueryTronAction.GetTRXAsync(wallet.Address);
                            if (nowTrx < trx)
                            {
                                if (!await CheckMainWalletTrx(mainWallet, trx + NetUsedTrx))
                                {
                                    return;
                                }
                                var (success2, txid3) = await mainWallet.TransferTrxAsync(trx - nowTrx, wallet.Address);
                                if (success2)
                                {
                                    _logger.LogInformation("转账手续费成功，地址：{a}", wallet.Address);
                                }
                                else
                                {
                                    _logger.LogWarning("转账手续费失败，跳过此地址，地址：{a}", wallet.Address);
                                    continue;
                                }
                            }
                        }
                        if (NeedEnergy < EnergyMinValue)
                        {
                            NeedEnergy = EnergyMinValue;
                        }
                        var (amountTrx, PaymentAddress) = await GetPrice((int)NeedEnergy);
                        if (amountTrx == 0)
                        {
                            _logger.LogWarning("能量价格预估失败！能量数量：{a}", NeedEnergy);
                            continue;
                        }
                        if (!await CheckMainWalletTrx(mainWallet, amountTrx + NetUsedTrx))
                        {
                            return;
                        }
                        var (success3, msg, txn) = await QueryTronAction.GetTransferTrxSignedTxnToJobjectAsync(
                            mainWallet.Address,
                            privateKey,
                            amountTrx,
                            PaymentAddress);
                        if (success3)
                        {
                            var CreateModel = new CreateOrderModel
                            {
                                PayAddress = mainWallet.Address,
                                PayAmount = amountTrx,
                                ReceiveAddress = wallet.Address,
                                RentDuration = 1,
                                RentTimeUnit = "h",
                                ResourceValue = (int)NeedEnergy,
                                SignedTxn = txn!
                            };
                            var feeResult = await energyApi.CreateOrder(CreateModel);
                            if (feeResult.Code == 0)
                            {
                                var count2 = 0;
                                await Task.Delay(3000);
                                while (!stoppingToken.IsCancellationRequested && count2 < 30)
                                {
                                    try
                                    {
                                        var feeResult2 = await energyApi.OrderQuery(feeResult.Data.OrderNo);
                                        if (feeResult2.Code == 0)
                                        {
                                            if (feeResult2.Data.Status == FeeeOrderStatus.已质押)
                                            {
                                                count = 30;
                                                count2 = 30;
                                                var TxId = string.Empty;

                                                var count3 = 0;
                                                while (!stoppingToken.IsCancellationRequested && count3 < 5)
                                                {
                                                    try
                                                    {
                                                        accountResource = await QueryTronAction.GetAccountResourceAsync(wallet.Address);
                                                        energy = accountResource.EnergyLimit - accountResource.EnergyUsed;
                                                        if (energy >= DefaultNeedEnergy)
                                                        {
                                                            count3 = 5;
                                                            _logger.LogInformation("能量租赁成功，当前能量：{e}，地址：{a}", energy, wallet.Address);
                                                            break;
                                                        }
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                    finally
                                                    {
                                                        if (count3 < 5)
                                                            await Task.Delay(3000);
                                                        count3++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("查询能量订单信息失败，失败原因：{msg}", feeResult2.Msg);
                                            continue;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogWarning("查询能量订单信息失败，失败原因：{msg}", e.Message);
                                        continue;
                                    }
                                    finally
                                    {
                                        if (count2 < 30)
                                            await Task.Delay(1000 * 3);
                                        count2++;
                                    }
                                }
                            }
                            else
                            {
                                CreateModel.SignedTxn = new object();
                                _logger.LogWarning("归集USDT失败，能量租赁失败，失败原因：{msg}\n请求参数：{@CreateModel}", feeResult.Msg, CreateModel);
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("归集USDT失败，失败原因：{b}", "能量租赁失败！" + msg);
                            continue;
                        }
                    }
                }
                else
                {
                    if (needNet)
                    {
                        var trx = 0.5m;
                        var nowTrx = await QueryTronAction.GetTRXAsync(wallet.Address);
                        if (nowTrx < trx)
                        {
                            if (!await CheckMainWalletTrx(mainWallet, trx - nowTrx + NetUsedTrx))
                            {
                                return;
                            }
                            var (success2, txid3) = await mainWallet.TransferTrxAsync(trx - nowTrx, wallet.Address);
                            if (success2)
                            {
                                _logger.LogInformation("转账手续费成功，地址：{a}", wallet.Address);
                            }
                            else
                            {
                                _logger.LogWarning("转账手续费失败，跳过此地址，地址：{a}", wallet.Address);
                                continue;
                            }
                        }
                    }
                }
                var RetainUSDTAmount = RetainUSDT ? 0.000001m : 0;
                var (success, txid) = await wallet.TransferUSDTAsync(item.USDT - RetainUSDTAmount, Address);
                if (success)
                {
                    _logger.LogInformation("归集USDT成功，USDT：{a}，Txid：{b}", item.USDT - RetainUSDTAmount, txid);
                    await _bot.SendTextMessageAsync(@$"归集USDT成功！

归集地址：<code>{item.Address}</code>
归集数量：{item.USDT - RetainUSDTAmount} USDT
交易哈希：{txid} <b><a href=""https://tronscan.org/#/transaction/{txid}?lang=zh"">查看交易</a></b>");
                    item.USDT = 0;
                    await _repository.UpdateAsync(item);
                }
                else
                {
                    _logger.LogWarning("归集USDT失败，失败原因：{b}", txid);
                }
            }
        }
        /// <summary>
        /// 检查手续费钱包TRX余额是否充足
        /// </summary>
        /// <param name="mainWallet"></param>
        /// <param name="minTrx"></param>
        /// <returns></returns>
        private async Task<bool> CheckMainWalletTrx(TronWallet mainWallet, decimal minTrx)
        {
            var mainTrx = await QueryTronAction.GetTRXAsync(mainWallet.Address);
            if (mainTrx < minTrx)
            {
                _logger.LogWarning("手续费钱包TRX不足！需要TRX：{minTrx}，当前TRX：{mainTrx}", minTrx, mainTrx);
                await _bot.SendTextMessageAsync(@$"手续费钱包TRX不足，无法继续进行归集任务！

手续费钱包地址：<code>{mainWallet.Address}</code>
当前TRX余额：{mainTrx} TRX


请先充值TRX，归集任务将在 {CheckTime} 小时后重试。");
                return false;
            }
            return true;
        }
    }
}
