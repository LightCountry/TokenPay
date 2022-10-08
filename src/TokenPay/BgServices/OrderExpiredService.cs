using FreeSql;
using TokenPay.Domains;

namespace TokenPay.BgServices
{
    public class OrderExpiredService : BaseScheduledService
    {
        private readonly ILogger<OrderExpiredService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public OrderExpiredService(ILogger<OrderExpiredService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider) : base("订单过期", TimeSpan.FromSeconds(10), logger)
        {
            _logger = logger;
            this._configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var _repository = scope.ServiceProvider.GetRequiredService<IBaseRepository<TokenOrders>>();

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
