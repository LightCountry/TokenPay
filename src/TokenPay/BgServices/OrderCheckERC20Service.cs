using Flurl;
using Flurl.Http;
using FreeSql;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class OrderCheckERC20Service : BaseScheduledService
    {
        private readonly ILogger<OrderCheckERC20Service> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment env;
        private readonly TelegramBot _bot;
        private readonly IServiceProvider _serviceProvider;
        private readonly FlurlClient client;

        public OrderCheckERC20Service(ILogger<OrderCheckERC20Service> logger,
            IConfiguration configuration,
            IHostEnvironment env,
            TelegramBot bot,
            IServiceProvider serviceProvider) : base("ERC20订单检测", TimeSpan.FromSeconds(15), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            this.env = env;
            this._bot = bot;
            _serviceProvider = serviceProvider;
            var WebProxy = configuration.GetValue<string>("WebProxy");
            client = new FlurlClient();
            if (!string.IsNullOrEmpty(WebProxy))
            {
                client.Settings.HttpClientFactory = new ProxyHttpClientFactory(WebProxy);
            }
        }

        protected override async Task ExecuteAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var _repository = scope.ServiceProvider.GetRequiredService<IBaseRepository<TokenOrders>>();
            var list = new List<(Currency, string)>();
            if (!env.IsProduction())
            {
                //测试网
                list.Add((Currency.USDT_ERC20, "0xBA62BCfcAaFc6622853cca2BE6Ac7d845BC0f2Dc"));
                list.Add((Currency.USDC_ERC20, "0xBA62BCfcAaFc6622853cca2BE6Ac7d845BC0f2Dc"));
            }
            else
            {
                list.Add((Currency.USDT_ERC20, "0xdAC17F958D2ee523a2206206994597C13D831ec7"));
                list.Add((Currency.USDC_ERC20, "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"));
            }
            foreach (var (Currency, ContractAddress) in list)
            {
                try
                {
                    await ERC20(_repository, Currency, ContractAddress);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{Currency}处理出错");
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_repository"></param>
        /// <param name="Currency">币种</param>
        /// <param name="ContractAddress">合约地址</param>
        /// <returns></returns>
        private async Task ERC20(IBaseRepository<TokenOrders> _repository, Currency Currency, string ContractAddress)
        {
            var Address = await _repository
                .Where(x => x.Status == OrderStatus.Pending)
                .Where(x => x.Currency == Currency)
                .Distinct()
                .ToListAsync(x => x.ToAddress);
            var BaseUrl = "https://api.etherscan.io";
            if (!env.IsProduction())
            {
                BaseUrl = "https://api-goerli.etherscan.io";
            }

            foreach (var address in Address)
            {
                //查询此地址待支付订单
                var orders = await _repository
                    .Where(x => x.Status == OrderStatus.Pending)
                    .Where(x => x.Currency == Currency)
                    .Where(x => x.ToAddress == address)
                    .OrderBy(x => x.CreateTime)
                    .ToListAsync();
                if (!orders.Any())
                {
                    continue;
                }
                var query = new Dictionary<string, object>();
                query.Add("module", "account");
                query.Add("action", "tokentx");
                query.Add("contractaddress", ContractAddress);
                query.Add("address", address);
                query.Add("page", 1);
                query.Add("offset", 100);
                query.Add("sort", "desc");
                if (env.IsProduction())
                    query.Add("apikey", _configuration.GetValue("ETH-API-KEY", ""));

                var req = BaseUrl
                    .AppendPathSegment($"api")
                    .SetQueryParams(query)
                    .WithClient(client)
                    .WithTimeout(15);
                var result = await req
                    .GetJsonAsync<BaseResponse<ERC20Transaction>>();

                if (result.Status == "1" && result.Result?.Count > 0)
                {
                    foreach (var item in result.Result)
                    {
                        //没有需要匹配的订单了
                        if (!orders.Any())
                        {
                            break;
                        }
                        //此交易已被其他订单使用
                        if (await _repository.Select.AnyAsync(x => x.BlockTransactionId == item.Hash))
                        {
                            continue;
                        }
                        //合约地址 确认数
                        if (item.ContractAddress.ToLower() != ContractAddress.ToLower() || item.Confirmations < 12)
                        {
                            continue;
                        }
                        var order = orders.Where(x => x.Amount == item.RealAmount && x.ToAddress.ToLower() == item.To.ToLower() && x.CreateTime < item.DateTime)
                            .OrderByDescending(x => x.CreateTime)//优先付最后一单
                            .FirstOrDefault();
                        if (order != null)
                        {
                            order.FromAddress = item.From;
                            order.BlockTransactionId = item.Hash;
                            order.Status = OrderStatus.Paid;
                            order.PayTime = DateTime.Now;
                            await _repository.UpdateAsync(order);
                            orders.Remove(order);
                            await SendAdminMessage(order);
                        }
                    }
                }
            }
        }
        private async Task SendAdminMessage(TokenOrders order)
        {

            var message = @$"<b>您有新订单！({order.ActualAmount} 元)</b>

订单编号：<code>{order.OutOrderId}</code>
原始金额：<b>{order.ActualAmount} 元</b>
订单金额：<b>{order.Amount} {order.Currency.ToCurrency()}</b>
付款地址：<code>{order.FromAddress}</code>
收款地址：<code>{order.ToAddress}</code>
创建时间：<b>{order.CreateTime:yyyy-MM-dd HH:mm:ss}</b>
支付时间：<b>{order.PayTime:yyyy-MM-dd HH:mm:ss}</b>
区块哈希：<code>{order.BlockTransactionId}</code>";
            if (env.IsProduction())
            {
                message += @$"  <b><a href=""https://etherscan.io/tx/{order.BlockTransactionId}?lang=zh"">查看区块</a></b>";
            }
            else
            {
                message += @$"  <b><a href=""https://goerli.etherscan.io/tx/{order.BlockTransactionId}?lang=zh"">查看区块</a></b>";
            }
            await _bot.SendTextMessageAsync(message);
        }

        private async Task<bool> Notify(TokenOrders order)
        {
            if (!string.IsNullOrEmpty(order.NotifyUrl))
            {
                try
                {
                    var result = await order.NotifyUrl.PostJsonAsync(order);
                    var message = await result.GetStringAsync();
                    if (result.StatusCode == 200)
                    {
                        _logger.LogInformation("订单异步通知成功！\n{msg}", message);
                        return true;
                    }
                    else
                    {
                        _logger.LogInformation("订单异步通知失败：{msg}", message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation("订单异步通知失败：{msg}", e.Message);
                }
            }
            return false;
        }
    }
}
