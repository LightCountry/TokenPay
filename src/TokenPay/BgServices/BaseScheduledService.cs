namespace TokenPay.BgServices
{
    public abstract class BaseScheduledService : BackgroundService
    {
        protected readonly string jobName;
        protected readonly ILogger Logger;
        private readonly PeriodicTimer _timer;

        protected BaseScheduledService(string JobName, TimeSpan period, ILogger logger)
        {
            Logger = logger;
            jobName = JobName;
            _timer = new PeriodicTimer(period);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Service {JobName} is starting.", jobName);
            do
            {
                try
                {
                    await ExecuteAsync(DateTime.Now, stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"定时任务[{jobName}]执行出现错误");
                }
            } while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken));
        }
        protected abstract Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken);
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Service {JobName} is stopping.", jobName);
            _timer.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
