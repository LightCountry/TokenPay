using Newtonsoft.Json;
using TokenPay.Extensions;

namespace TokenPay.Models.EthModel
{
    public class EthTransaction
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

        [JsonProperty("transactionIndex")]
        public string TransactionIndex { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
        public decimal RealAmount => Value / (decimal)Math.Pow(10, 18);

        [JsonProperty("gas")]
        public decimal Gas { get; set; }

        [JsonProperty("gasPrice")]
        public decimal GasPrice { get; set; }

        [JsonProperty("isError")]
        public int IsError { get; set; }

        [JsonProperty("txreceipt_status")]
        public string TxreceiptStatus { get; set; }

        [JsonProperty("input")]
        public string Input { get; set; }

        [JsonProperty("contractAddress")]
        public string ContractAddress { get; set; }

        [JsonProperty("cumulativeGasUsed")]
        public string CumulativeGasUsed { get; set; }

        [JsonProperty("gasUsed")]
        public string GasUsed { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty("methodId")]
        public string MethodId { get; set; }

        [JsonProperty("functionName")]
        public string FunctionName { get; set; }
    }
}
