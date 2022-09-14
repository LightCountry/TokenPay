using FreeSql.DataAnnotations;
using System.ComponentModel;

namespace TokenPay.Domains
{
    /// <summary>
    /// 支付订单
    /// </summary>
    public class TokenOrders
    {
        /// <summary>
        /// 交易订单号
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// 外部订单号
        /// </summary>
        public string OutOrderId { get; set; } = null!;
        /// <summary>
        /// 支付用户标识
        /// </summary>
        public string OrderUserKey { get; set; } = null!;
        /// <summary>
        /// 区块唯一编号
        /// </summary>
        public string? BlockTransactionId { get; set; }
        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PayTime { get; set; }
        /// <summary>
        /// 来源地址
        /// </summary>
        public string? FromAddress { get; set; } = null!;
        /// <summary>
        /// 订单实际需要支付的法币金额，保留2位小数
        /// </summary>
        [Column(Precision = 15, Scale = 2)]
        public decimal ActualAmount { get; set; }
        /// <summary>
        /// 币种
        /// </summary>
        [Column(MapType = typeof(string))]
        public Currency Currency { get; set; }
        /// <summary>
        /// 订单金额，保留4位小数
        /// </summary>
        [Column(Precision = 15, Scale = 4)]
        public decimal Amount { get; set; }
        /// <summary>
        /// 钱包地址
        /// </summary>
        public string ToAddress { get; set; } = null!;
        /// <summary>
        /// 订单状态
        /// </summary>
        public OrderStatus Status { get; set; }
        /// <summary>
        /// 异步通知Url
        /// </summary>
        public string? NotifyUrl { get; set; }
        /// <summary>
        /// 同步跳转Url
        /// </summary>
        public string? RedirectUrl { get; set; }
        /// <summary>
        /// 异步回调次数
        /// </summary>
        public int CallbackNum { get; set; }
        /// <summary>
        /// 异步回调确认状态
        /// </summary>
        public bool CallbackConfirm { get; set; }
        /// <summary>
        /// 最后通知时间
        /// </summary>
        public DateTime? LastNotifyTime { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
    public enum Currency
    {
        BTC = 10,
        ETH,
        [Description("USDT-TRC20")]
        USDT_TRC20,
        [Description("USDT-ERC20")]
        USDT_ERC20,
        TRX,
    }
    public enum OrderStatus
    {
        Pending,
        Paid,
        Expired
    }
}
