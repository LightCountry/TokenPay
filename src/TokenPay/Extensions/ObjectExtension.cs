using TokenPay.Domains;
using TokenPay.Models.EthModel;

namespace TokenPay.Extensions
{
    public static class ObjectExtension
    {
        public static SortedDictionary<string, object?> ToDic(this TokenOrders order, IConfiguration configuration)
        {
            var EVMChains = configuration.GetSection("EVMChains").Get<List<EVMChain>>() ?? new List<EVMChain>();
            var BaseCurrency = configuration.GetValue<string>("BaseCurrency", "CNY");
            var ExpireTime = configuration.GetValue("ExpireTime", 10 * 60);
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
                { nameof(order.Status), (int)order.Status },
                { nameof(order.PassThroughInfo), order.PassThroughInfo },
                { "BaseCurrency", BaseCurrency },
                { "BlockChainName", order.Currency.ToBlockchainEnglishName(EVMChains) },
                { "CurrencyName", order.Currency.ToCurrency(EVMChains) },
                { nameof(order.PayAmount), order.PayAmount?.ToString() },
                { nameof(order.IsDynamicAmount), order.IsDynamicAmount ? 1 : 0 }
            };
            //此处从回调中移除为Null的字段
            var nullKey = new List<string>();
            foreach (var item in dic)
            {
                if (item.Value == null)
                    nullKey.Add(item.Key);
            }
            foreach (var item in nullKey)
            {
                if (dic.ContainsKey(item))
                    dic.Remove(item);
            }
            return dic;
        }
    }
}
