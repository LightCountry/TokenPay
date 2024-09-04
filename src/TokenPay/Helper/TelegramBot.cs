using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Serilog;

namespace TokenPay.Helper
{
    public class TelegramBot
    {
        public const string BaseTelegramApiHost = "https://api.telegram.org";
        private readonly string _botToken;
        private readonly long _userId;
        private readonly IConfiguration _configuration;
        public TelegramBot(IConfiguration configuration)
        {
            _botToken = configuration.GetValue<string>("Telegram:BotToken")!;
            _userId = configuration.GetValue<long>("Telegram:AdminUserId");
            this._configuration = configuration;
        }
        public static TelegramBotInfo BotInfo = null!;
        public async Task<TelegramResult<TelegramBotInfo>?> GetMeAsync(string? TelegramApiHost = null)
        {
            if (string.IsNullOrEmpty(_botToken) || _userId == 0)
            {
                Log.Logger.Information("未配置机器人Token！");
                return null;
            }
            var ApiHost = TelegramApiHost ?? BaseTelegramApiHost;

            var request = ApiHost
                    .WithTimeout(5)
                    .AppendPathSegment($"bot{_botToken}/getMe")
                    .WithTimeout(10);
            var result = await request.GetJsonAsync<TelegramResult<TelegramBotInfo>>();
            Log.Logger.Information("机器人启动成功！我是{@result}。", result.Result.FirstName);
            BotInfo = result.Result;
            await SendTextMessageAsync("你好呀~我是TokenPay通知机器人！");
            return result;
        }
        public async Task<TelegramResult<SendMessageResult>?> SendTextMessageAsync(string Message, string? TelegramApiHost = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(_botToken) || _userId == 0)
            {
                Log.Logger.Information("未配置机器人Token！");
                return null;
            }
            var ApiHost = TelegramApiHost ?? BaseTelegramApiHost;

            var request = ApiHost
                    .WithTimeout(5)
                    .AppendPathSegment($"bot{_botToken}/sendMessage")
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
                var result = await request.GetJsonAsync<TelegramResult<SendMessageResult>>(cancellationToken: cancellationToken ?? default);
                Log.Logger.Information("机器人消息发送结果：{result}", result.Ok);
                return result;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "机器人发送消息失败！");
            }
            return null;
        }
    }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class TelegramBotInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_bot")]
        public bool IsBot { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("can_join_groups")]
        public bool CanJoinGroups { get; set; }

        [JsonProperty("can_read_all_group_messages")]
        public bool CanReadAllGroupMessages { get; set; }

        [JsonProperty("supports_inline_queries")]
        public bool SupportsInlineQueries { get; set; }
    }
    public class MessageChat
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class MessageFrom
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_bot")]
        public bool IsBot { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }

    public class SendMessageResult
    {
        [JsonProperty("message_id")]
        public long MessageId { get; set; }

        [JsonProperty("from")]
        public MessageFrom From { get; set; }

        [JsonProperty("chat")]
        public MessageChat Chat { get; set; }

        [JsonProperty("date")]
        public int Date { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
    public class TelegramResult<T>
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("result")]
        public T Result { get; set; }
    }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
