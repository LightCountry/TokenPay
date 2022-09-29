using System.ComponentModel.DataAnnotations;

namespace TokenPay.Domains
{
    public class Tokens
    {
        [Key]
        public string Id { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 密钥
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 币种
        /// </summary>
        public Currency Currency { get; set; }
        /// <summary>
        /// 本币余额
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// USDT代币余额
        /// </summary>
        public decimal USDT { get; set; }
    }
}
