using Newtonsoft.Json;
using TokenPay.Extensions;

namespace TokenPay.Models.EthModel
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class ERC20Transaction
    {
        [JsonProperty("blockNumber")]
        public string BlockNumber { get; set; }

        [JsonProperty("timeStamp")]
        public long TimeStamp { get; set; }
        public DateTime DateTime => (TimeStamp * 1000).ToDateTime();

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("blockHash")]
        public string BlockHash { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("contractAddress")]
        public string ContractAddress { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
        public decimal RealAmount => Value / (decimal)Math.Pow(10, TokenDecimal);

        [JsonProperty("tokenName")]
        public string TokenName { get; set; }

        [JsonProperty("tokenSymbol")]
        public string TokenSymbol { get; set; }

        [JsonProperty("tokenDecimal")]
        public int TokenDecimal { get; set; }

        [JsonProperty("transactionIndex")]
        public int TransactionIndex { get; set; }

        [JsonProperty("gas")]
        public decimal Gas { get; set; }

        [JsonProperty("gasPrice")]
        public decimal GasPrice { get; set; }

        [JsonProperty("gasUsed")]
        public decimal GasUsed { get; set; }

        [JsonProperty("cumulativeGasUsed")]
        public decimal CumulativeGasUsed { get; set; }

        [JsonProperty("input")]
        public string Input { get; set; }

        [JsonProperty("confirmations")]
        public decimal Confirmations { get; set; }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
