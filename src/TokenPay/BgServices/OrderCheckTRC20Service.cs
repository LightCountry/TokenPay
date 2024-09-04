using Flurl;
using Flurl.Http;
using FreeSql;
using System.Threading.Channels;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.TronModel;

namespace TokenPay.BgServices
{
    public class OrderCheckTRC20Service : BaseScheduledService
    {
        private readonly ILogger<OrderCheckTRC20Service> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly Channel<TokenOrders> _channel;
        private readonly IFreeSql freeSql;

        private bool UseDynamicAddress => _configuration.GetValue("UseDynamicAddress", true);
        private bool UseDynamicAddressAmountMove => _configuration.GetValue("DynamicAddressConfig:AmountMove", false);
        public OrderCheckTRC20Service(ILogger<OrderCheckTRC20Service> logger,
            IConfiguration configuration,
            IHostEnvironment env,
            Channel<TokenOrders> channel,
            IFreeSql freeSql) : base("TRC20订单检测", TimeSpan.FromSeconds(3), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            this._env = env;
            this._channel = channel;
            this.freeSql = freeSql;
        }

        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            var _repository = freeSql.GetRepository<TokenOrders>();
            var _TokensRepository = freeSql.GetRepository<Tokens>();

            var Address = await _repository
                .Where(x => x.Status == OrderStatus.Pending)
                .Where(x => x.Currency == "USDT_TRC20")
                .Distinct()
                .ToListAsync(x => x.ToAddress);
            var ContractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
            var BaseUrl = _configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            if (!_env.IsProduction())
            {
                ContractAddress = "TX8ZUpucJYgHb8wBFQYuYSJ459og32AHWW";
                BaseUrl = "https://api.shasta.trongrid.io";
            }
            var OnlyConfirmed = _configuration.GetValue("OnlyConfirmed", true);
            var start = DateTime.Now.AddMinutes(-10);
            foreach (var address in Address)
            {
                //查询此地址待支付订单
                var orders = await _repository
                    .Where(x => x.Status == OrderStatus.Pending)
                    .Where(x => x.Currency == "USDT_TRC20")
                    .Where(x => x.ToAddress == address)
                    .OrderBy(x => x.CreateTime)
                    .ToListAsync();
                if (!orders.Any())
                {
                    continue;
                }
                var query = new Dictionary<string, object>();
                if (OnlyConfirmed)
                {
                    query.Add("only_confirmed", true);
                }
                query.Add("only_to", true);
                query.Add("limit", 50);
                query.Add("min_timestamp", start.ToUnixTimeStamp());
                query.Add("contract_address", ContractAddress);
                var req = BaseUrl
                    .AppendPathSegment($"v1/accounts/{address}/transactions/trc20")
                    .SetQueryParams(query)
                    .WithTimeout(15);
                if (_env.IsProduction())
                    req = req.WithHeader("TRON-PRO-API-KEY", _configuration.GetValue("TRON-PRO-API-KEY", ""));
                var result = await req
                    .GetJsonAsync<BaseResponse<TronTransaction>>();

                if (result.Success && result.Data?.Count > 0)
                {
                    foreach (var item in result.Data)
                    {
                        //合约地址不匹配
                        if (item.TokenInfo?.Address != ContractAddress) continue;
                        var types = new string[] { "Transfer", "TransferFrom" };
                        //收款地址相同
                        if (item.To != address || !types.Contains(item.Type)) continue;
                        //没有需要匹配的订单了
                        if (!orders.Any())
                        {
                            break;
                        }
                        //此交易已被其他订单使用
                        if (await _repository.Select.AnyAsync(x => x.BlockTransactionId == item.TransactionId))
                        {
                            continue;
                        }
                        var token = await _TokensRepository.Where(x => x.Currency == TokenCurrency.TRX && x.Address == item.To).FirstAsync();
                        if (token != null)
                        {
                            token.USDT += item.Amount;
                            await _TokensRepository.UpdateAsync(token);
                        }
                        var order = orders.Where(x => x.Amount == item.Amount && x.ToAddress == item.To && x.CreateTime < item.BlockTimestamp.ToDateTime())
                            .OrderByDescending(x => x.CreateTime)//优先付最后一单
                            .FirstOrDefault();
                    recheck:
                        if (order != null)
                        {
                            order.FromAddress = item.From;
                            order.BlockTransactionId = item.TransactionId;
                            order.Status = OrderStatus.Paid;
                            order.PayTime = item.BlockTimestamp.ToDateTime();
                            order.PayAmount = item.Amount;
                            await _repository.UpdateAsync(order);
                            orders.Remove(order);
                            await SendAdminMessage(order);
                        }
                        else
                        {
                            if (UseDynamicAddress && UseDynamicAddressAmountMove)
                            {
                                //允许非准确金额支付
                                var Move = _configuration.GetSection("DynamicAddressConfig:USDT").Get<decimal[]>() ?? [];
                                if (Move.Length == 2)
                                {
                                    var Down = Move[0]; //上浮金额
                                    var Up = Move[1]; //下浮金额
                                    order = orders.Where(x => x.Amount >= item.Amount - Down && x.Amount <= item.Amount + Up)
                                        .Where(x => x.ToAddress == item.To && x.CreateTime < item.BlockTimestamp.ToDateTime())
                                       .OrderByDescending(x => x.CreateTime)//优先付最后一单
                                       .FirstOrDefault();
                                    if (order != null)
                                    {
                                        order.IsDynamicAmount = true;
                                        goto recheck;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private async Task SendAdminMessage(TokenOrders order)
        {
            await _channel.Writer.WriteAsync(order);
        }
    }
}
