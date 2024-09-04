using FreeSql.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace TokenPay.Domains
{
    public class TokenRate
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// 币种
        /// </summary>
        [Column(MapType = typeof(string))]
        public required string Currency { get; set; }
        /// <summary>
        /// 法币
        /// </summary>
        [Column(MapType = typeof(string))]
        public FiatCurrency FiatCurrency { get; set; }
        /// <summary>
        /// 汇率
        /// </summary>
        [Column(Precision = 24, Scale = 12)]
        public decimal Rate { get; set; }
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }

    public enum FiatCurrency
    {
        CNY = 10,
        USD,
        EUR,
        GBP,
        AUD,
        HKD,
        TWD,
        SGD
    }
}
