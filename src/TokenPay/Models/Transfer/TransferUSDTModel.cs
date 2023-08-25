using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenPay.Models.Transfer
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class Contract
    {
        [JsonProperty("parameter")]
        public Parameter Parameter { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class LogData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("topics")]
        public List<string> Topics { get; set; }
    }

    public class Parameter
    {
        [JsonProperty("value")]
        public Value Value { get; set; }

        [JsonProperty("type_url")]
        public string TypeUrl { get; set; }
    }

    public class RawData
    {
        [JsonProperty("contract")]
        public List<Contract> Contract { get; set; }

        [JsonProperty("ref_block_bytes")]
        public string RefBlockBytes { get; set; }

        [JsonProperty("ref_block_hash")]
        public string RefBlockHash { get; set; }

        [JsonProperty("expiration")]
        public long Expiration { get; set; }

        [JsonProperty("fee_limit")]
        public int FeeLimit { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class ResultData
    {
        [JsonProperty("result")]
        public bool Result { get; set; }
    }

    public class RetData
    {
        public RetCode Ret { get; set; }
        [JsonProperty("contractRet")]
        public string ContractRet { get; set; }
    }
    public enum RetCode
    {
        Sucess = 0,
        Failed = 1,
    }
    public class TransferUSDTModel
    {
        [JsonProperty("result")]
        public ResultData Result { get; set; }

        [JsonProperty("energy_used")]
        public int EnergyUsed { get; set; }

        [JsonProperty("constant_result")]
        public List<string> ConstantResult { get; set; }

        [JsonProperty("logs")]
        public List<LogData> Logs { get; set; }

        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }
    }

    public class Transaction
    {
        [JsonProperty("Error")]
        public string Error { get; set; }
        [JsonProperty("ret")]
        public List<RetData> Ret { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("signature")]
        public List<string> Signature { get; set; } = new List<string>();

        [JsonProperty("txID")]
        public string TxID { get; set; }

        [JsonProperty("raw_data")]
        public RawData RawData { get; set; }

        [JsonProperty("raw_data_hex")]
        public string RawDataHex { get; set; }
    }

    public class Value
    {
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; set; }

        [JsonProperty("to_address")]
        public string ToAddress { get; set; }

        [JsonProperty("contract_address")]
        public string ContractAddress { get; set; }

        [JsonProperty("balance")]
        public long Balance { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }
        [JsonProperty("receiver_address")]
        public string ReceiverAddress { get; set; }
        [JsonProperty("lock")]
        public bool Lock { get; set; }
    }

    public class BroadcastResult
    {
        [JsonProperty("result")]
        public bool Result { get; set; }

        [JsonProperty("txid")]
        public string Txid { get; set; }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

}
