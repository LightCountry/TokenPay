namespace TokenPay.Models
{
    public class TokenModel
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; } = string.Empty;
        /// <summary>
        /// 交易hash
        /// </summary>
        public string Txid { get; set; } = string.Empty;
        /// <summary>
        /// 交易时间
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// 交易首次通知时的确认数
        /// </summary>
        public int Confirmations { get; set; }
        /// <summary>
        /// 余额变化数量
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// 交易发生的区块链
        /// </summary>
        public Coin Coin { get; set; }
        /// <summary>
        /// 交易被打包的区块高度
        /// </summary>
        public long Height { get; set; }
        /// <summary>
        /// token的地址
        /// </summary>
        public string TokenAddress { get; set; } = string.Empty;
        /// <summary>
        /// token的符号单位
        /// </summary>
        public string? TokenSymbol { get; set; }
        /// <summary>
        /// token的数量变化
        /// </summary>
        public decimal TokenValue { get; set; } 
    }

    public enum Coin
    { 
        BTC,
        ETH,
        TRX
    }
    public enum TokenSymbol
    {
        USDT
    }
}
