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
        private readonly IFreeSql freeSql;
        private readonly FlurlClient client;

        public OrderNotifyService(ILogger<OrderNotifyService> logger,
            IConfiguration configuration,
            IFreeSql freeSql) : base("订单通知", TimeSpan.FromSeconds(3), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            this.freeSql = freeSql;
            client = new FlurlClient();
            client.Settings.Timeout = TimeSpan.FromSeconds(configuration.GetValue("NotifyTimeOut", 3));
            client.BeforeCall(c =>
            {
                c.Request.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36 TokenPay/1.0");
                _logger.LogInformation("发起请求\nURL：{url}\n参数：{body}", c.Request.Url, c.RequestBody);
            });
            client.AfterCall(async c =>
            {
                if (c.Response != null)
                {
                    _logger.LogInformation("收到响应\nURL：{url}\n响应：{@body}", c.Request.Url, await c.Response.GetStringAsync());
                }
            });
        }

        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            var _repository = freeSql.GetRepository<TokenOrders>();
            var start = DateTime.Now.AddMinutes(-1);
            var Orders = await _repository
                .Where(x => x.Status == OrderStatus.Paid)
                .Where(x => !x.CallbackConfirm)
                .Where(x => x.CallbackNum < 3)
                .Where(x => x.LastNotifyTime == null || x.LastNotifyTime < start) //未通知过，或通知失败N分钟后的
                .Where(x => x.NotifyUrl!.StartsWith("http"))
                .ToListAsync();
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
                    var dic = order.ToDic(_configuration);
                    var SignatureStr = string.Join("&", dic.Select(x => $"{x.Key}={x.Value}"));
                    var ApiToken = _configuration.GetValue<string>("ApiToken");
                    SignatureStr += ApiToken;
                    var Signature = SignatureStr.ToMD5();
                    dic.Add(nameof(Signature), Signature);
                    var result = await client.Request(order.NotifyUrl).PostJsonAsync(dic);
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
                    _logger.LogInformation(e, "订单异步通知失败：{msg}", e.Message);
                }
            }
            return false;
        }
    }
}
