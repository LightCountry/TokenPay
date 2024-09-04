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
        /// 订单实际支付的金额，保留2位小数
        /// </summary>
        [Column(Precision = 15, Scale = 2)]
        public decimal? PayAmount { get; set; }
        /// <summary>
        /// 是否动态金额订单
        /// </summary>
        public bool IsDynamicAmount { get; set; }
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
        /// 区块链币种
        /// </summary>
        public required string Currency { get; set; }
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
        /// 在回调通知或订单信息中原样返回
        /// </summary>
        [Column(StringLength = -1)]
        public string? PassThroughInfo { get; set; }
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
    public enum OrderStatus
    {
        Pending,
        Paid,
        Expired
    }
}
