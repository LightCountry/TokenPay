using Nethereum.ABI.FunctionEncoding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TokenPay.Extensions;

namespace TokenPay.Models
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class BlockHeader
    {
        [JsonProperty("raw_data")]
        public BlockHeaderRawData RawData { get; set; }

        [JsonProperty("witness_signature")]
        public string WitnessSignature { get; set; }
    }
    public class Contract
    {
        [JsonProperty("parameter")]
        public Parameter Parameter { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("Permission_id")]
        public int PermissionId { get; set; }
    }
    public class Parameter
    {
        [JsonProperty("value")]
        public Value Value { get; set; }

        [JsonProperty("type_url")]
        public string TypeUrl { get; set; }
    }

    public class BlockHeaderRawData
    {
        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("txTrieRoot")]
        public string TxTrieRoot { get; set; }

        [JsonProperty("witness_address")]
        public string WitnessAddress { get; set; }

        [JsonProperty("parentHash")]
        public string ParentHash { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class RetData
    {
        [JsonProperty("contractRet")]
        public string ContractRet { get; set; }
    }
    public class BlockResult
    {
        [JsonProperty("block")]
        public List<BlockRoot> Block { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class BlockRoot
    {
        [JsonProperty("blockID")]
        public string BlockID { get; set; }

        [JsonProperty("block_header")]
        public BlockHeader BlockHeader { get; set; }

        [JsonProperty("transactions")]
        public List<Transaction> Transactions { get; set; }
    }

    public class Transaction
    {
        [JsonProperty("ret")]
        public List<RetData> Ret { get; set; }

        [JsonProperty("signature")]
        public List<string> Signature { get; set; }

        [JsonProperty("txID")]
        public string TxID { get; set; }

        [JsonProperty("raw_data")]
        public TransactionRawData RawData { get; set; }

        [JsonProperty("raw_data_hex")]
        public string RawDataHex { get; set; }
    }
    public class TransactionRawData
    {
        [JsonProperty("data")]
        public string? Data { get; set; }
        public string? DataText => Data?.HexToString();
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
    }
    public enum AbiFunction
    {
        [Description("转账")]
        transfer = 10,
        [Description("授权转账")]
        transferFrom,
    }
    public class Value
    {
        private static Dictionary<string, AbiFunction> FuncDic = new Dictionary<string, AbiFunction>
        {
            {"a9059cbb", AbiFunction.transfer },
            {"23b872dd", AbiFunction.transferFrom }
        };
        [JsonProperty("amount")]
        public decimal AmountRaw { get; set; }
        public decimal Amount => AmountRaw / 1_000_000m;
        /// <summary>
        /// TRX闪兑USDT才有
        /// </summary>
        [JsonProperty("call_value")]
        public decimal CallValueRaw { get; set; }
        public decimal CallValue => CallValueRaw / 1_000_000m;
        [JsonProperty("data")]
        public string? Data { get; set; }
        [JsonProperty("asset_name")]
        public string AssetName { get; set; }

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; set; }

        [JsonProperty("to_address")]
        public string ToAddress { get; set; }

        [JsonProperty("contract_address")]
        public string ContractAddress { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("receiver_address")]
        public string ReceiverAddress { get; set; }

        [JsonProperty("frozen_duration")]
        public int FrozenDuration { get; set; }

        [JsonProperty("frozen_balance")]
        public long FrozenBalance { get; set; }
        public decimal FrozenBalanceAmount => FrozenBalance / 1_000_000m;
        /// <summary>
        /// 代理balance数量的TRX所对应的资源给目标地址, 单位为sun
        /// </summary>
        [JsonProperty("balance")]
        public long Balance { get; set; }
        /// <summary>
        /// true表示为该资源代理操作设置三天的锁定期
        /// 即资源代理给目标地址后的三天内不可以取消对其的资源代理
        /// 如果锁定期内，再次代理资源给同一目标地址，则锁定期将重新设置为3天。
        /// false表示本次资源代理没有锁定期，可随时取消对目标地址的资源代理。
        /// </summary>
        [JsonProperty("lock")]
        public bool Lock { get; set; } = false;
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
