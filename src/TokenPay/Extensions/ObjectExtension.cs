using System.Globalization;
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
            // decimal 字段一律以字符串输出：JSON 数字会被多数语言解析为浮点数，丢失原有小数位，导致签名原文不一致
            // 数字与日期统一按 InvariantCulture 格式化，不受服务器区域设置影响
            var dic = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                { nameof(order.Id), order.Id.ToString() },
                { nameof(order.BlockTransactionId), order.BlockTransactionId },
                { nameof(order.OutOrderId), order.OutOrderId },
                { nameof(order.OrderUserKey), order.OrderUserKey },
                { nameof(order.PayTime), order.PayTime?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                { nameof(order.Amount), order.Amount.ToString(CultureInfo.InvariantCulture) },
                { nameof(order.ActualAmount), order.ActualAmount.ToString(CultureInfo.InvariantCulture) },
                { nameof(order.Currency), order.Currency },
                { nameof(order.FromAddress), order.FromAddress },
                { nameof(order.ToAddress), order.ToAddress },
                { nameof(order.Status), (int)order.Status },
                { nameof(order.PassThroughInfo), order.PassThroughInfo },
                { "BaseCurrency", BaseCurrency },
                { "BlockChainName", order.Currency.ToBlockchainEnglishName(EVMChains) },
                { "CurrencyName", order.Currency.ToCurrency(EVMChains) },
                { nameof(order.PayAmount), order.PayAmount?.ToString(CultureInfo.InvariantCulture) },
                { nameof(order.IsDynamicAmount), order.IsDynamicAmount },
                { nameof(order.IsCustomAmount), order.IsCustomAmount },
                { nameof(order.MinCustomAmount), order.MinCustomAmount?.ToString(CultureInfo.InvariantCulture) },
                { nameof(order.MaxCustomAmount), order.MaxCustomAmount?.ToString(CultureInfo.InvariantCulture) },
                { "SignatureType",  configuration.GetValue("Signature:UseHmacSha256", false) ? "HmacSha256" : "MD5"},
            };
            // 移除 null 或空字符串
            foreach (var key in dic
                .Where(x => x.Value is null || x.Value is string s && string.IsNullOrEmpty(s))
                .Select(x => x.Key)
                .ToList())
            {
                dic.Remove(key);
            }

            return dic;
        }
    }
}
