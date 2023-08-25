using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenPay.Models
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public partial class GetAccountModel
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("balance")]
        public long Balance { get; set; }

        [JsonProperty("votes")]
        public Vote[] Votes { get; set; }

        [JsonProperty("frozen")]
        public Frozen[] Frozen { get; set; }

        [JsonProperty("net_usage")]
        public long NetUsage { get; set; }

        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        [JsonProperty("latest_opration_time")]
        public long LatestOprationTime { get; set; }

        [JsonProperty("free_net_usage")]
        public long FreeNetUsage { get; set; }

        [JsonProperty("latest_consume_time")]
        public long LatestConsumeTime { get; set; }

        [JsonProperty("latest_consume_free_time")]
        public long LatestConsumeFreeTime { get; set; }

        [JsonProperty("account_resource")]
        public AccountResource AccountResource { get; set; }

        [JsonProperty("owner_permission")]
        public OwnerPermission OwnerPermission { get; set; }

        [JsonProperty("active_permission")]
        public ActivePermission[] ActivePermission { get; set; }

        [JsonProperty("asset_optimized")]
        public bool AssetOptimized { get; set; }
    }

    public partial class AccountResource
    {
        [JsonProperty("energy_usage")]
        public long EnergyUsage { get; set; }

        [JsonProperty("frozen_balance_for_energy")]
        public Frozen FrozenBalanceForEnergy { get; set; }

        [JsonProperty("latest_consume_time_for_energy")]
        public long LatestConsumeTimeForEnergy { get; set; }
    }

    public partial class Frozen
    {
        [JsonProperty("frozen_balance")]
        public long FrozenBalance { get; set; }

        [JsonProperty("expire_time")]
        public long ExpireTime { get; set; }
    }

    public partial class ActivePermission
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("permission_name")]
        public string PermissionName { get; set; }

        [JsonProperty("threshold")]
        public long Threshold { get; set; }

        [JsonProperty("operations")]
        public string Operations { get; set; }

        public ContractType[] OperationEnums
        {
            get
            {
                var list = new List<ContractType>();
                var number10 = long.Parse(Operations.TrimEnd('0'), System.Globalization.NumberStyles.HexNumber);
                var number2 = Convert.ToString(number10, 2);
                foreach (var item in number2)
                {

                }
                return list.ToArray();
            }
        }

        [JsonProperty("keys")]
        public Key[] Keys { get; set; }
    }

    public partial class Key
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("weight")]
        public long Weight { get; set; }
    }

    public partial class OwnerPermission
    {
        [JsonProperty("permission_name")]
        public string PermissionName { get; set; }

        [JsonProperty("threshold")]
        public long Threshold { get; set; }

        [JsonProperty("keys")]
        public Key[] Keys { get; set; }
    }

    public partial class Vote
    {
        [JsonProperty("vote_address")]
        public string VoteAddress { get; set; }

        [JsonProperty("vote_count")]
        public long VoteCount { get; set; }
    }
    public enum ContractType
    {
        [Description("账号创建")]
        AccountCreateContract = 0,
        [Description("TRX转账")]
        TransferContract = 1,
        [Description("TRC10转账")]
        TransferAssetContract = 2,
        [Description("投票")]
        VoteAssetContract = 3,
        VoteWitnessContract = 4,
        WitnessCreateContract = 5,
        AssetIssueContract = 6,
        WitnessUpdateContract = 8,
        ParticipateAssetIssueContract = 9,
        AccountUpdateContract = 10,
        FreezeBalanceContract = 11,
        UnfreezeBalanceContract = 12,
        WithdrawBalanceContract = 13,
        UnfreezeAssetContract = 14,
        UpdateAssetContract = 15,
        ProposalCreateContract = 16,
        ProposalApproveContract = 17,
        ProposalDeleteContract = 18,
        SetAccountIdContract = 19,
        CustomContract = 20,
        CreateSmartContract = 30,
        [Description("TRC20/TRC721/TRC1155转账")]
        TriggerSmartContract = 31,
        GetContract = 32,
        UpdateSettingContract = 33,
        ExchangeCreateContract = 41,
        ExchangeInjectContract = 42,
        ExchangeWithdrawContract = 43,
        ExchangeTransactionContract = 44,
        UpdateEnergyLimitContract = 45,
        AccountPermissionUpdateContract = 46,
        ClearABIContract = 48,
        UpdateBrokerageContract = 49,
        ShieldedTransferContract = 51,
        MarketSellAssetContract = 52,
        MarketCancelOrderContract = 53,
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
