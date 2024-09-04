using Newtonsoft.Json;

namespace TokenPay.Models.TronModel
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class TronTransaction
    {
        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("token_info")]
        public TokenInfo TokenInfo { get; set; }

        [JsonProperty("block_timestamp")]
        public long BlockTimestamp { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
        /// <summary>
        /// 实际USDT金额，需要计算精度
        /// </summary>
        public decimal Amount => Value / 1_000_000;
    }

    public class TokenInfo
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
