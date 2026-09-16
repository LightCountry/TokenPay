using Flurl.Http;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace TokenPay.Helper;

/// <summary>能量平台公开接口：本地签名付款，平台广播，无需 API Key。</summary>
public sealed class EnergyApi : IDisposable
{
    private readonly FlurlClient client;
    private readonly ILogger logger;

    public EnergyApi(ILogger logger, IConfiguration configuration)
    {
        this.logger = logger;
        client = new FlurlClient(configuration.GetValue("Collection:EnergyApiUrl", "https://api-energy.trxd.win"));
        client.WithSettings(s =>
        {
            s.JsonSerializer = new NewtonsoftJsonSerializer();
            s.Timeout = TimeSpan.FromSeconds(30);
        });
        client.BeforeCall(c =>
        {
            c.Request.WithHeader("Lang", "zh-CN");
            logger.LogInformation("发起请求\nURL：{url}\n参数：{body}", c.Request.Url, c.RequestBody);
        });
        client.AfterCall(async c =>
        {
            logger.LogInformation("收到响应\nURL：{url}\n响应：{@body}", c.Request.Url, c.Response != null ? await c.Response.GetStringAsync() : null);
        });
    }
    /// <summary>
    /// 获取报价
    /// </summary>
    /// <param name="energy"></param>
    /// <param name="durationSeconds"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<EnergyQuote> GetQuoteAsync(long energy, int durationSeconds, CancellationToken ct)
        => GetQuoteAsync(new EnergyQuoteRequest { Energy = energy, DurationSeconds = durationSeconds }, ct);

    /// <summary>获取报价，支持文档规定的两种租期参数。</summary>
    public async Task<EnergyQuote> GetQuoteAsync(EnergyQuoteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.ValidateRental();
        return await ReadAsync<EnergyQuote>(await client.Request("api/v1/quotes").AllowAnyHttpStatus()
            .PostJsonAsync(request, cancellationToken: ct));
    }
    /// <summary>
    /// 能量下单
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<EnergyOrder> CreatePaidOrderAsync(EnergyPaidOrder request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.ValidateRental();
        return await ReadAsync<EnergyOrder>(await client.Request("api/v1/orders/paid").AllowAnyHttpStatus()
            .PostJsonAsync(request, cancellationToken: ct));
    }
    /// <summary>
    /// 查询订单
    /// </summary>
    /// <param name="orderNo"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<EnergyOrder> GetOrderAsync(string orderNo, CancellationToken ct)
        => await ReadAsync<EnergyOrder>(await client.Request("api/v1/orders", Uri.EscapeDataString(orderNo))
            .AllowAnyHttpStatus().GetAsync(cancellationToken: ct));

    private async Task<T> ReadAsync<T>(IFlurlResponse response) where T : class
    {
        using (response)
        {
            var result = await response.GetJsonAsync<EnergyResponse<T>>();
            if (response.StatusCode is >= 200 and < 300 && result is { Success: true, Code: "OK", Data: not null })
                return result.Data;
            logger.LogWarning("能量平台请求失败：HTTP {Status}，{Code}，{Message}，requestId={RequestId}",
                response.StatusCode, result?.Code, result?.Message, result?.RequestId);
            throw new EnergyApiException(result?.Code, result?.Message, result?.RequestId);
        }
    }

    public void Dispose() => client.Dispose();
}


public sealed class EnergyApiException : InvalidOperationException
{
    public string? Code { get; }

    public EnergyApiException(string? code, string? message, string? requestId)
        : base($"能量平台：{code} {message} (requestId={requestId})")
    {
        Code = code;
    }
}

public sealed class EnergyResponse<T>
{
    [JsonProperty("success")] public bool Success { get; set; }
    [JsonProperty("code")] public string Code { get; set; } = "";
    [JsonProperty("message")] public string Message { get; set; } = "";
    [JsonProperty("data")] public T? Data { get; set; }
    [JsonProperty("requestId")] public string RequestId { get; set; } = "";
    /// <summary>Unix 毫秒时间戳。</summary>
    [JsonProperty("timestamp")] public long Timestamp { get; set; }
}

/// <summary>租期使用 durationSeconds，或 duration + timeUnit，不能同时提交。</summary>
public abstract class EnergyRentalRequest
{
    [JsonProperty("energy")] public long Energy { get; set; }
    [JsonProperty("durationSeconds", NullValueHandling = NullValueHandling.Ignore)]
    public int? DurationSeconds { get; set; }
    [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
    public int? Duration { get; set; }
    [JsonProperty("timeUnit", NullValueHandling = NullValueHandling.Ignore)]
    public string? TimeUnit { get; set; }

    internal void ValidateRental()
    {
        if (Energy <= 0) throw new ArgumentException("能量必须为正整数");
        if (DurationSeconds.HasValue)
        {
            if (DurationSeconds <= 0 || Duration.HasValue || TimeUnit != null)
                throw new ArgumentException("durationSeconds 必须为正数，且不能同时传 duration 或 timeUnit");
        }
        else if (!Duration.HasValue || Duration <= 0 ||
                 TimeUnit?.ToLowerInvariant() is not ("s" or "m" or "h" or "d"))
        {
            throw new ArgumentException("必须提供正整数 duration 和时间单位 timeUnit（s/m/h/d）");
        }
        // 不在客户端限制平台支持的租期或能量范围，以报价结果为准。
    }
}

public sealed class EnergyQuoteRequest : EnergyRentalRequest
{
}

public sealed class EnergyQuote
{
    [JsonProperty("quoteId")] public string QuoteId { get; set; } = "";
    [JsonProperty("energy")] public long Energy { get; set; }
    [JsonProperty("duration")] public int Duration { get; set; }
    [JsonProperty("timeUnit")] public string TimeUnit { get; set; } = "";
    [JsonProperty("durationSeconds")] public int DurationSeconds { get; set; }
    /// <summary>应付 TRX，最多六位小数。</summary>
    [JsonProperty("paymentAmount")] public decimal PaymentAmount { get; set; }
    [JsonProperty("paymentAddress")] public string PaymentAddress { get; set; } = "";
    /// <summary>ISO 8601 字符串，包含 UTC+8 时区偏移。</summary>
    [JsonProperty("validUntil")] public string ValidUntil { get; set; } = "";
}

public sealed class EnergyPaidOrder : EnergyRentalRequest
{
    [JsonProperty("quoteId")] public string QuoteId { get; set; } = "";
    [JsonProperty("targetAddress")] public string TargetAddress { get; set; } = "";
    /// <summary>
    /// 完整签名 JSON 对象，原样保留 txID、raw_data_hex、raw_data、signature 及节点额外字段。
    /// raw_data 内 amount 为 SUN 整数，timestamp/expiration 为毫秒整数，不能重新组装已签名数据。
    /// </summary>
    [JsonProperty("signedTransaction")] public JObject SignedTransaction { get; set; } = new();
}

/// <summary>链上付款下单、订单查询共用的完整 data 实体。</summary>
public sealed class EnergyOrder
{
    [JsonProperty("orderNo")] public string OrderNo { get; set; } = "";
    [JsonProperty("energy")] public long Energy { get; set; }
    [JsonProperty("duration")] public int Duration { get; set; }
    [JsonProperty("timeUnit")] public string TimeUnit { get; set; } = "";
    [JsonProperty("durationSeconds")] public int DurationSeconds { get; set; }
    [JsonProperty("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public EnergyOrderStatus Status { get; set; }
    /// <summary>付款 TRX 金额，最多六位小数。</summary>
    [JsonProperty("paymentAmount")] public decimal PaymentAmount { get; set; }
    [JsonProperty("paymentTxId")] public string? PaymentTxId { get; set; }
    [JsonProperty("targetAddress")] public string TargetAddress { get; set; } = "";
    [JsonProperty("delegateTxId")] public string? DelegateTxId { get; set; }
    [JsonProperty("reclaimTxId")] public string? ReclaimTxId { get; set; }
    /// <summary>以下时间字段均为平台原始 ISO 8601 字符串。</summary>
    [JsonProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [JsonProperty("activatedAt")] public string? ActivatedAt { get; set; }
    [JsonProperty("expireAt")] public string? ExpireAt { get; set; }
    [JsonProperty("completedAt")] public string? CompletedAt { get; set; }
}

/// <summary>能量平台订单状态，与公开接口文档的 OrderStatus 一一对应。</summary>
public enum EnergyOrderStatus
{
    PaymentConfirming,
    Queued,
    AssigningProvider,
    Delegating,
    Active,
    ReclaimDue,
    Reclaiming,
    Completed,
    Failed,
    RefundPending,
    Refunded,
    ManualReview
}
