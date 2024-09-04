using System.ComponentModel.DataAnnotations;
using TokenPay.Domains;

namespace TokenPay.Models
{
    public class CreateOrderViewModel
    {
        /// <summary>
        /// 外部订单号
        /// </summary>
        [Display(Name = "外部订单号")]
        [Required(ErrorMessage = "{0}为必传参数")]
        public string OutOrderId { get; set; } = null!;
        /// <summary>
        /// 支付用户标识
        /// </summary>
        [Display(Name = "支付用户标识")]
        [Required(ErrorMessage = "{0}为必传参数")]
        public string OrderUserKey { get; set; } = null!;
        /// <summary>
        /// 订单实际需要支付的法币金额
        /// </summary>
        [Display(Name = "实付金额")]
        [Required(ErrorMessage = "{0}为必传参数")]
        public decimal ActualAmount { get; set; }
        /// <summary>
        /// 币种
        /// </summary>
        [Display(Name = "币种")]
        [Required(ErrorMessage = "{0}为必传参数")]
        //[(ErrorMessage = "{1}不是有效的{0}")]
        public required string Currency { get; set; }
        /// <summary>
        /// 在回调通知或订单信息中原样返回
        /// </summary>
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
        /// 参数签名
        /// </summary>
        public string? Signature { get; set; }
    }
}
