using FreeSql;
using TokenPay.Domains;

namespace TokenPay.BgServices
{
    public class OrderExpiredService : BaseScheduledService
    {
        private readonly ILogger<OrderExpiredService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFreeSql freeSql;

        public OrderExpiredService(ILogger<OrderExpiredService> logger,
            IConfiguration configuration,
            IFreeSql freeSql) : base("订单过期", TimeSpan.FromSeconds(10), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            this.freeSql = freeSql;
        }

        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            var _repository = freeSql.GetRepository<TokenOrders>();

            var ExpireTime = _configuration.GetValue("ExpireTime", 10 * 60);
            var ExpireDateTime = DateTime.Now.AddSeconds(-1 * ExpireTime);
            var ExpiredOrders = await _repository.Where(x => x.CreateTime < ExpireDateTime && x.Status == OrderStatus.Pending).ToListAsync();
            foreach (var order in ExpiredOrders)
            {
                _logger.LogInformation("订单[{c}]过期了！", order.Id);
                order.Status = OrderStatus.Expired;
                await _repository.UpdateAsync(order);
            }
        }
    }
}
