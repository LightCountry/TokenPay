using System;

namespace TokenPay.BgServices
{
    public abstract class BaseScheduledService : BackgroundService
    {
        protected readonly string jobName;
        private readonly TimeSpan period;
        protected readonly ILogger _logger;
        private PeriodicTimer? _timer;

        protected BaseScheduledService(string JobName, TimeSpan period, ILogger logger)
        {
            _logger = logger;
            jobName = JobName;
            this.period = period;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service {JobName} is starting.", jobName);
            await Task.Delay(3000);//延迟3秒再启动任务
            _timer = new PeriodicTimer(period);
            try
            {
                do
                {
                    try
                    {
                        await ExecuteAsync(DateTime.Now, stoppingToken);
                    }
                    catch (Flurl.Http.FlurlHttpException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
                    {
                        _logger.LogError(ex, "定时任务[{jobName}]执行Api请求出现错误，返回：{code}，通常为Api鉴权出现问题或者调用次数超出限制", jobName, ex.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"定时任务[{jobName}]执行出现错误");
                    }
                } while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service {JobName} has been cancelled.", jobName);
            }
        }
        protected abstract Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken);
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service {JobName} is stopping.", jobName);
            _timer?.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
