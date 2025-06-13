using Flurl;
using Flurl.Http;
using FreeSql;
using Nethereum.Signer;
using System.Threading.Channels;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class OrderCheckEVMERC20Service : BaseScheduledService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly List<EVMChain> _chains;
        private readonly Channel<TokenOrders> _channel;
        private readonly IFreeSql freeSql;
        private bool UseDynamicAddress => _configuration.GetValue("UseDynamicAddress", true);
        private bool UseDynamicAddressAmountMove => _configuration.GetValue("DynamicAddressConfig:AmountMove", false);
        public OrderCheckEVMERC20Service(ILogger<OrderCheckEVMERC20Service> logger,
            IConfiguration configuration,
            IHostEnvironment env,
            List<EVMChain> Chains,
            Channel<TokenOrders> channel,
            IFreeSql freeSql) : base("EVM代币订单检测", TimeSpan.FromSeconds(15), logger)
        {
            this._configuration = configuration;
            this._env = env;
            _chains = Chains;
            this._channel = channel;
            this.freeSql = freeSql;
        }

        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            var _repository = freeSql.GetRepository<TokenOrders>();
            foreach (var chain in _chains)
            {
                if (chain == null || !chain.Enable || chain.ERC20 == null) continue;
                foreach (var erc20 in chain.ERC20)
                {
                    var Currency = $"EVM_{chain.ChainNameEN}_{erc20.Name}_{chain.ERC20Name}";
                    try
                    {
                        await ERC20(_repository, Currency, chain, erc20);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "{Currency}查询交易记录出错", Currency);
                    }
                }

            }

        }
        /// <summary>
        /// 查询交易记录
        /// </summary>
        /// <returns></returns>
        private async Task ERC20(IBaseRepository<TokenOrders> _repository, string Currency, EVMChain chain, EVMErc20 erc20)
        {
            var Address = await _repository
                .Where(x => x.Status == OrderStatus.Pending)
                .Where(x => x.Currency == Currency)
                .Distinct()
                .ToListAsync(x => x.ToAddress);

            var BaseUrl = chain.ApiHost ?? "https://api.etherscan.io/v2/";

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
                    { "chainid", chain.ChainId },
                    { "module", "account" },
                    { "action", "tokentx" },
                    { "contractaddress", erc20.ContractAddress },
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
                    .WithTimeout(15);
                var result = await req
                    .GetJsonAsync<BaseResponseList<ERC20Transaction>>();

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
                        if (item.ContractAddress.ToLower() != erc20.ContractAddress.ToLower() || item.Confirmations < chain.Confirmations)
                        {
                            continue;
                        }
                        var order = orders.Where(x => x.Amount == item.RealAmount && x.ToAddress.ToLower() == item.To.ToLower() && x.CreateTime < item.DateTime)
                            .OrderByDescending(x => x.CreateTime)//优先付最后一单
                            .FirstOrDefault();
                    recheck:
                        if (order != null)
                        {
                            order.FromAddress = item.From;
                            order.BlockTransactionId = item.Hash;
                            order.Status = OrderStatus.Paid;
                            order.PayTime = item.DateTime;
                            order.PayAmount = item.RealAmount;
                            await _repository.UpdateAsync(order);
                            orders.Remove(order);
                            await SendAdminMessage(order);
                        }
                        else
                        {
                            if (UseDynamicAddress && UseDynamicAddressAmountMove)
                            {
                                //允许非准确金额支付
                                var Move = _configuration.GetSection($"DynamicAddressConfig:{erc20.Name}").Get<decimal[]>() ?? [];
                                if (Move.Length == 2)
                                {
                                    var Down = Move[0]; //上浮金额
                                    var Up = Move[1]; //下浮金额
                                    order = orders.Where(x => item.RealAmount >= x.Amount - Down && item.RealAmount <= x.Amount + Up)
                                        .Where(x => x.ToAddress.ToLower() == item.To.ToLower() && x.CreateTime < item.DateTime)
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
