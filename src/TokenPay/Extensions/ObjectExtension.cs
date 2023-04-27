using TokenPay.Domains;

namespace TokenPay.Extensions
{
    public static class ObjectExtension
    {
        public static SortedDictionary<string, object?> ToDic(this TokenOrders order)
        {
            var dic = new SortedDictionary<string, object?>
            {
                { nameof(order.Id), order.Id.ToString() },
                { nameof(order.BlockTransactionId), order.BlockTransactionId },
                { nameof(order.OutOrderId), order.OutOrderId },
                { nameof(order.OrderUserKey), order.OrderUserKey },
                { nameof(order.PayTime), order.PayTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                { nameof(order.Amount), order.Amount.ToString() },
                { nameof(order.ActualAmount), order.ActualAmount.ToString() },
                { nameof(order.Currency), order.Currency },
                { nameof(order.FromAddress), order.FromAddress },
                { nameof(order.ToAddress), order.ToAddress },
                { nameof(order.Status), (int)order.Status }
            };
            return dic;
        }
    }
}
