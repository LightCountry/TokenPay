using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TokenPay.Extensions;

namespace TokenPay.Helper;

public static class SignatureHelper
{
    /// <summary>
    /// 按签名规则拼接规范化参数：排除 Signature，忽略 null 和空字符串，字段名按区分大小写的逐字节顺序排列，
    /// bool 转为 true/false，数字等可格式化的值按 InvariantCulture 转换，不受服务器区域设置影响
    /// </summary>
    public static string BuildCanonicalParameters(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        return string.Join("&", parameters
            .Where(x => x.Key != "Signature")
            .Select(x => (x.Key, Value: FormatValue(x.Value)))
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}"));
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        // bool 未实现 IFormattable，默认 ToString() 为 True/False，需在此单独处理
        bool b => b ? "true" : "false",
        // 数值、Guid 等实现了 IFormattable 的类型，按 InvariantCulture 格式化，不受服务器区域设置影响。实际涉及的字段：
        // 创建订单的 decimal 金额（ActualAmount、MinCustomAmount、MaxCustomAmount）、回调的 int Status、查单的 Guid Id（小写带连字符）。
        // 回调中的金额和 PayTime 在 ToDic 中已格式化为字符串，走下方默认分支原样使用
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    public static string Create(string canonicalParameters, IConfiguration configuration)
    {
        var apiToken = configuration.GetValue<string>("ApiToken") ?? string.Empty;
        if (!configuration.GetValue("Signature:UseHmacSha256", false))
        {
            return (canonicalParameters + apiToken).ToMD5();
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiToken));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalParameters))).ToLowerInvariant();
    }

    public static bool Verify(string canonicalParameters, string? providedSignature, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(providedSignature)) return false;
        var expected = Create(canonicalParameters, configuration);
        var actual = providedSignature.Trim();
        if (expected.Length != actual.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(actual));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
