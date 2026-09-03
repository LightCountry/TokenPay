using System.Security.Cryptography;
using System.Text;
using TokenPay.Extensions;

namespace TokenPay.Helper;

public static class SignatureHelper
{
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
