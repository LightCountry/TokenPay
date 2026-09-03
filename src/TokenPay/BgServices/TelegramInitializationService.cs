using TokenPay.Helper;

namespace TokenPay.BgServices;

public sealed class TelegramInitializationService : BackgroundService
{
    private readonly TelegramBot _bot;
    private readonly ILogger<TelegramInitializationService> _logger;

    public TelegramInitializationService(TelegramBot bot, ILogger<TelegramInitializationService> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _bot.GetMeAsync(cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "机器人连接失败，TokenPay 将继续运行；发送通知时会再次尝试连接");
        }
    }
}
