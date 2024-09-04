using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TokenPay.Extensions;

namespace TokenPay.Models.TronModel
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class Contract
    {
        [JsonProperty("parameter")]
        public Parameter Parameter { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
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

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
    }

    public class Ret
    {
        [JsonProperty("contractRet")]
        public string ContractRet { get; set; }

        [JsonProperty("fee")]
        public int Fee { get; set; }
    }

    public class TrxTransaction
    {
        [JsonProperty("ret")]
        public List<Ret> Ret { get; set; }

        [JsonProperty("signature")]
        public List<string> Signature { get; set; }

        [JsonProperty("txID")]
        public string TxID { get; set; }

        [JsonProperty("net_usage")]
        public int NetUsage { get; set; }

        [JsonProperty("raw_data_hex")]
        public string RawDataHex { get; set; }

        [JsonProperty("net_fee")]
        public int NetFee { get; set; }

        [JsonProperty("energy_usage")]
        public int EnergyUsage { get; set; }

        [JsonProperty("blockNumber")]
        public int BlockNumber { get; set; }

        [JsonProperty("block_timestamp")]
        public long BlockTimestamp { get; set; }

        [JsonProperty("energy_fee")]
        public int EnergyFee { get; set; }

        [JsonProperty("energy_usage_total")]
        public int EnergyUsageTotal { get; set; }

        [JsonProperty("raw_data")]
        public RawData RawData { get; set; }

        [JsonProperty("internal_transactions")]
        public List<object> InternalTransactions { get; set; }
    }

    public class Value
    {

        [JsonProperty("asset_name")]
        public string? AssetName { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        /// <summary>
        /// 真实金额
        /// </summary>
        public decimal RealAmount => Amount / 1_000_000m;

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; set; }

        [JsonProperty("to_address")]
        public string ToAddress { get; set; }
        public string ToAddressBase58 => ToAddress.HexToeBase58();
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
