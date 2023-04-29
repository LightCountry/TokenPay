namespace TokenPay.Models;

/// <summary>
/// The model of the order cancellation.
/// </summary>
public class OrderCancellationModel
{
    public OrderCancellationModel(Guid orderId, string orderCode, string returnUrl)
        => (OrderId, OrderCode, ReturnUrl) = (orderId, orderCode, returnUrl);

    /// <summary>
    /// The id of the order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// The code of the order. Here is an alias for `外部订单号`.
    /// </summary>
    public string OrderCode { get; set; }

    /// <summary>
    /// The URL set by the API caller.
    /// </summary>
    public string ReturnUrl { get; set; }
}