using CoinListenBot.BgServices.Base;
using Nethereum.Signer;
using System.Threading.Channels;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class OrderPaySuccessService : BaseBackgroundService
    {
        private readonly Channel<TokenOrders> _channel;
        private readonly IHostEnvironment _env;
        private readonly TelegramBot _bot;
        private readonly List<EVMChain> _chain;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderPaySuccessService> _logger;

        public OrderPaySuccessService(
            Channel<TokenOrders> channel,
            IHostEnvironment env,
            TelegramBot bot,
            List<EVMChain> chain,
            IConfiguration configuration,
            ILogger<OrderPaySuccessService> logger) : base("发送订单通知", logger)
        {
            this._channel = channel;
            this._env = env;
            this._bot = bot;
            this._chain = chain;
            this._configuration = configuration;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && await _channel.Reader.WaitToReadAsync())
            {
                while (!stoppingToken.IsCancellationRequested && _channel.Reader.TryRead(out var item))
                {
                    try
                    {
                        await SendAdminMessage(item);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "发送新订单通知失败！");
                    }
                }
            }
        }
        private async Task SendAdminMessage(TokenOrders order)
        {
            //默认货币
            var BaseCurrency = _configuration.GetValue<string>("BaseCurrency", "CNY");

            foreach (var item in _chain.Select(x => x.ERC20Name).ToArray())
            {
                order.Currency = order.Currency.Replace(item, "");
            }
            var message = @$"<b>您有新订单！({order.ActualAmount} {BaseCurrency})</b>

订单编号：<code>{order.OutOrderId}</code>
原始金额：<b>{order.ActualAmount} {BaseCurrency}</b>
订单金额：<b>{order.Amount} {order.Currency.Replace("TRC20", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last()}</b>
付款地址：<code>{order.FromAddress}</code>
收款地址：<code>{order.ToAddress}</code>
创建时间：<b>{order.CreateTime:yyyy-MM-dd HH:mm:ss}</b>
支付时间：<b>{order.PayTime:yyyy-MM-dd HH:mm:ss}</b>
交易哈希：<code>{order.BlockTransactionId}</code>";
            if (_env.IsProduction())
            {
                message += @$"  <b><a href=""https://tronscan.org/#/transaction/{order.BlockTransactionId}?lang=zh"">查看交易</a></b>";
            }
            else
            {
                message += @$"  <b><a href=""https://shasta.tronscan.org/#/transaction/{order.BlockTransactionId}?lang=zh"">查看交易</a></b>";
            }
            await _bot.SendTextMessageAsync(message);
        }
    }
}
