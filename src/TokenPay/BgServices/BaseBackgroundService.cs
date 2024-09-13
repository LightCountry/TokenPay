using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TokenPay.BgServices
{
    public abstract class BaseBackgroundService : BackgroundService
    {
        protected readonly string jobName;
        protected readonly ILogger _logger;
        protected BaseBackgroundService(string JobName, ILogger logger)
        {
            _logger = logger;
            jobName = JobName;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service {JobName} is starting.", jobName);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service {JobName} is stopping.", jobName);
            return base.StartAsync(cancellationToken);
        }
    }

}
