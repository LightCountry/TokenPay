using System.ComponentModel.DataAnnotations;

namespace TokenPay.Domains
{
    public class Tokens
    {
        [Key]
        public required string Id { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public required string Address { get; set; }
        /// <summary>
        /// 密钥
        /// </summary>
        public required string Key { get; set; }
        /// <summary>
        /// 币种
        /// </summary>
        public TokenCurrency Currency { get; set; }
        /// <summary>
        /// 本币余额
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// USDT代币余额
        /// </summary>
        public decimal USDT { get; set; }
        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime? LastCheckTime { get; set; }
    }
    public enum TokenCurrency
    {
        BTC = 10,
        EVM,
        TRX
    }
}
