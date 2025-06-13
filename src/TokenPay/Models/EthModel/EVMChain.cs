namespace TokenPay.Models.EthModel
{

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class EVMChain
    {
        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable { get; set; }
        /// <summary>
        /// 链名
        /// </summary>
        public string ChainName { get; set; }
        /// <summary>
        /// 链名-英文
        /// </summary>
        public string ChainNameEN { get; set; }
        /// <summary>
        /// 基本币名称
        /// </summary>
        public string BaseCoin { get; set; }
        /// <summary>
        /// 最少确认数
        /// </summary>
        public int Confirmations { get; set; } = 12;
        /// <summary>
        /// 基本币精确度
        /// </summary>
        public int Decimals { get; set; } = 18;
        /// <summary>
        /// 区块浏览器Host
        /// </summary>
        public string ScanHost { get; set; }
        /// <summary>
        /// Api Host
        /// </summary>
        public string ApiHost { get; set; }
        /// <summary>
        /// Api Key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// 区块链id
        /// </summary>
        public int ChainId { get; set; }
        /// <summary>
        /// ERC20Name
        /// </summary>
        public string ERC20Name { get; set; }
        /// <summary>
        /// 代币
        /// </summary>
        public List<EVMErc20> ERC20 { get; set; }
    }
    public class EVMErc20
    {
        /// <summary>
        /// 币种名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 合约地址
        /// </summary>
        public string ContractAddress { get; set; }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
