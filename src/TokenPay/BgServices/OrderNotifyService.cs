using Flurl.Http;
using FreeSql;
using System.Net;
using TokenPay.Domains;
using TokenPay.Extensions;

namespace TokenPay.BgServices
{
    public class OrderNotifyService : BaseScheduledService
    {
        private readonly ILogger<OrderNotifyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly FlurlClient client;

        public OrderNotifyService(ILogger<OrderNotifyService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider) : base("订单通知", TimeSpan.FromSeconds(1), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            _serviceProvider = serviceProvider;
            client = new FlurlClient();
            client.Settings.Timeout = TimeSpan.FromSeconds(configuration.GetValue("NotifyTimeOut", 3));
#if DEBUG
            client.Configure(settings =>
            {
                settings.BeforeCall = c =>
                {
                    _logger.LogInformation("发起请求\nURL：{url}\n参数：{body}", c.Request.Url, c.RequestBody);
                };
                settings.AfterCallAsync = async c =>
                {
                    _logger.LogInformation("收到响应\nURL：{url}\n响应：{@body}", c.Request.Url, await c.Response.GetStringAsync());
                };
            });

#endif
        }

        protected override async Task ExecuteAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var _repository = scope.ServiceProvider.GetRequiredService<IBaseRepository<TokenOrders>>();
            var start = DateTime.Now.AddMinutes(-5);
            var Orders = await _repository
                .Where(x => x.Status == OrderStatus.Paid)
                .Where(x => !x.CallbackConfirm)
                .Where(x => x.CallbackNum < 3)
                .Where(x => x.LastNotifyTime == null || x.LastNotifyTime < start) //未通知过，或通知失败5分钟后的
                .Where(x => x.NotifyUrl.StartsWith("http"))
                .ToListAsync();
            if (Orders.Count > 0)
                _logger.LogInformation("待通知订单检测，订单数：{c}", Orders.Count);
            foreach (var order in Orders)
            {
                _logger.LogInformation("开始异步通知订单: {c}", order.Id);
                order.CallbackNum++;
                order.LastNotifyTime = DateTime.Now;
                await _repository.UpdateAsync(order);
                var result = await Notify(order);
                if (result)
                {
                    order.CallbackConfirm = true;
                    await _repository.UpdateAsync(order);
                }
                _logger.LogInformation("订单: {c}，通知结果：{d}", order.Id, result ? "成功" : "失败");
            }
        }


        private async Task<bool> Notify(TokenOrders order)
        {
            if (!string.IsNullOrEmpty(order.NotifyUrl))
            {
                try
                {
                    var dic = new SortedDictionary<string, string?>();
                    dic.Add(nameof(order.Id), order.Id.ToString());
                    dic.Add(nameof(order.BlockTransactionId), order.BlockTransactionId);
                    dic.Add(nameof(order.OutOrderId), order.OutOrderId);
                    dic.Add(nameof(order.OrderUserKey), order.OrderUserKey);
                    dic.Add(nameof(order.PayTime), order.PayTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                    dic.Add(nameof(order.Amount), order.Amount.ToString());
                    dic.Add(nameof(order.ActualAmount), order.ActualAmount.ToString());
                    dic.Add(nameof(order.Currency), order.Currency.ToDescriptionOrString());
                    dic.Add(nameof(order.FromAddress), order.FromAddress);
                    dic.Add(nameof(order.ToAddress), order.ToAddress);
                    var SignatureStr = string.Join("&", dic.Select(x => $"{x.Key}={x.Value}"));
                    var NotifyKey = _configuration.GetValue<string>("NotifyKey");
                    SignatureStr += NotifyKey;
                    var Signature = SignatureStr.ToMD5();
                    dic.Add(nameof(Signature), Signature);
                    var result = await order.NotifyUrl.WithClient(client).PostJsonAsync(dic);
                    var message = await result.GetStringAsync();
                    if (result.StatusCode == 200 && message == "ok")
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
