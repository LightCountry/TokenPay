using Flurl;
using Flurl.Http;
using Serilog;

namespace TokenPay.Helper
{
    public class TelegramBot
    {
        public const string BaseTelegramApiHost = "https://api.telegram.org";
        private readonly string _botToken;
        private readonly long _userId;
        private readonly IConfiguration _configuration;
        private readonly FlurlClient client;

        public TelegramBot(IConfiguration configuration)
        {
            _botToken = configuration.GetValue<string>("Telegram:BotToken");
            _userId = configuration.GetValue<long>("Telegram:AdminUserId");
            this._configuration = configuration;
            var WebProxy = configuration.GetValue<string>("WebProxy");
            client = new FlurlClient();
            client.Settings.Timeout = TimeSpan.FromSeconds(5);
            if (!string.IsNullOrEmpty(WebProxy))
            {
                client.Settings.HttpClientFactory = new ProxyHttpClientFactory(WebProxy);
            }
        }

        public async Task<object?> GetMeAsync(string? TelegramApiHost = null)
        {
            if (string.IsNullOrEmpty(_botToken) || _userId <= 0)
            {
                Log.Logger.Information("未配置机器人Token！");
                return null;
            }
            var ApiHost = TelegramApiHost ?? BaseTelegramApiHost;

            var request = ApiHost
                    .AppendPathSegment($"bot{_botToken}/getMe")
                    .WithClient(client)
                    .WithTimeout(10);
            var result = await request.GetJsonAsync<object>();
            Log.Logger.Information("机器人启动成功！\n{@result}", result);
            await SendTextMessageAsync("你好呀~我是TokenPay通知机器人！");
            return result;
        }
        public async Task SendTextMessageAsync(string Message, string? TelegramApiHost = null)
        {
            if (string.IsNullOrEmpty(_botToken) || _userId <= 0)
            {
                Log.Logger.Information("未配置机器人Token！");
                return;
            }
            var ApiHost = TelegramApiHost ?? BaseTelegramApiHost;

            var request = ApiHost
                    .AppendPathSegment($"bot{_botToken}/sendMessage")
                    .WithClient(client)
                    .SetQueryParams(new
                    {
                        chat_id = _userId,
                        parse_mode = "HTML",
                        text = Message,
                        disable_web_page_preview = true
                    })
                    .WithTimeout(10);
            try
            {
                var result = await request.GetStringAsync();
                Log.Logger.Information("机器人消息发送结果：{result}", result);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "机器人发送消息失败！");
            }
        }
    }
}
