using System.ComponentModel.DataAnnotations;
using TokenPay.Domains;

namespace TokenPay.Models;

public static class AdminCurrencyDisplay
{
    public static string GetBlockchainName(string currency)
    {
        if (currency.StartsWith("EVM_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = currency.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length >= 3 ? parts[1] : currency;
        }

        if (currency.Equals("TRX", StringComparison.OrdinalIgnoreCase) ||
            currency.EndsWith("_TRC20", StringComparison.OrdinalIgnoreCase))
            return "TRON";

        return currency;
    }

    public static string GetName(string currency)
    {
        if (currency.EndsWith("_TRC20", StringComparison.OrdinalIgnoreCase))
            return currency[..^"_TRC20".Length];
        if (!currency.StartsWith("EVM_", StringComparison.OrdinalIgnoreCase)) return currency;
        var parts = currency.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 3 ? parts[2] : currency;
    }

    public static string GetBlockchainCurrencyName(string currency) =>
        $"{GetBlockchainName(currency)}-{GetName(currency)}";
}

public static class AdminLinkDisplay
{
    public static string? GetSafeHttpUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        return uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;
    }
}

public sealed class AdminLoginModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class AdminPageModel<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long Total { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

public sealed class AdminPaginationModel
{
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int PageSize { get; init; }
    public string? Keyword { get; init; }
    public OrderStatus? Status { get; init; }
    public string? Currency { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public sealed class AdminTokenRowModel
{
    public required string Id { get; init; }
    public required string Address { get; init; }
    public TokenCurrency Currency { get; init; }
    public decimal Value { get; init; }
    public decimal USDT { get; init; }
    public DateTime? LastCheckTime { get; init; }
}

public sealed class AdminOrderPageModel : AdminPageModel<TokenOrders>
{
    public Guid? SupplementOrderId { get; init; }
    public string? Keyword { get; init; }
    public OrderStatus? Status { get; init; }
    public string? Currency { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public required IReadOnlyList<string> Currencies { get; init; }
}

public sealed class SupplementOrderModel
{
    [Required]
    public Guid Id { get; set; }

    [Required, StringLength(200)]
    public string FromAddress { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    public DateTime? PayTime { get; set; }

    [Required, Range(typeof(decimal), "0.00000001", "79228162514264337593543950335")]
    public decimal? PayAmount { get; set; }
}
