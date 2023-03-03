using Flurl;
using Flurl.Http;
using FreeSql;
using System.Threading.Channels;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class OrderCheckEVMBaseService : BaseScheduledService
    {
        private readonly ILogger<OrderCheckEVMBaseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly Channel<TokenOrders> _channel;
        private readonly List<EVMChain> _chains;
        private readonly IServiceProvider _serviceProvider;
        private readonly FlurlClient client;

        public OrderCheckEVMBaseService(ILogger<OrderCheckEVMBaseService> logger,
            IConfiguration configuration,
            IHostEnvironment env,
            Channel<TokenOrders> channel,
            List<EVMChain> Chains,
            IServiceProvider serviceProvider) : base("ETH订单检测", TimeSpan.FromSeconds(15), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            this._env = env;
            this._channel = channel;
            _chains = Chains;
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
            foreach (var chain in _chains)
            {
                if (chain == null || !chain.Enable) continue;
                var Currency = $"EVM_{chain.ChainNameEN}";
                try
                {
                    var Address = await _repository
                        .Where(x => x.Status == OrderStatus.Pending)
                        .Where(x => x.Currency == Currency)
                        .Distinct()
                        .ToListAsync(x => x.ToAddress);

                    var BaseUrl = chain.ApiHost;

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
                        var query = new Dictionary<string, object>
                {
                    { "module", "account" },
                    { "action", "txlist" },
                    { "address", address },
                    { "page", 1 },
                    { "offset", 100 },
                    { "sort", "desc" }
                };
                        if (_env.IsProduction())
                            query.Add("apikey", chain.ApiKey);

                        var req = BaseUrl
                            .AppendPathSegment($"api")
                            .SetQueryParams(query)
                            .WithClient(client)
                            .WithTimeout(15);
                        var result = await req
                            .GetJsonAsync<BaseResponse<EthTransaction>>();

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
                                //合约地址 方法id 是否错误 确认数
                                if (!string.IsNullOrEmpty(item.ContractAddress) || item.MethodId != "0x"
                                    || item.IsError != 0 || item.Confirmations < chain.Confirmations)
                                {
                                    continue;
                                }
                                var RealAmount = item.RealAmount(chain.Decimals);
                                var order = orders.Where(x => x.Amount == RealAmount && x.ToAddress.ToLower() == item.To.ToLower() && x.CreateTime < item.DateTime)
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
                catch (Exception e)
                {
                    _logger.LogError(e, "{coin}查询交易记录出错！", Currency);
                }
            }
        }
        private async Task SendAdminMessage(TokenOrders order)
        {
            await _channel.Writer.WriteAsync(order);
        }
    }
}
