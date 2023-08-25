using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TokenPay.BgServices
{
    public abstract class BaseBackgroundService : IHostedService, IDisposable
    {
        private Task? _executingTask;
        protected readonly string? jobName;
        protected readonly ILogger? __logger;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        protected BaseBackgroundService()
        {
        }
        protected BaseBackgroundService(string JobName, ILogger logger)
        {
            __logger = logger;
            jobName = JobName;
        }
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            if (jobName != null && __logger != null)
                __logger.LogInformation("Background Service {JobName} is starting.", jobName);

            _executingTask = ExecuteAsync(_stoppingCts.Token);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                if (jobName != null && __logger != null)
                    __logger.LogInformation("Background Service {JobName} is stopping.", jobName);
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

        }

        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
        }
    }

}
