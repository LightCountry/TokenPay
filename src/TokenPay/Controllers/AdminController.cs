using System.Security.Claims;
using System.Threading.Channels;
using FreeSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TokenPay.Domains;
using TokenPay.Models;
using TokenPay.Helper;
using TokenPay.Models.EthModel;
using TokenPay.BgServices;

namespace TokenPay.Controllers;

[Route("admin")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[AutoValidateAntiforgeryToken]
public sealed class AdminController : Controller
{
    private const int DefaultPageSize = 20;
    private const int BalanceCheckMaxConcurrency = 3;
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];
    private static readonly SemaphoreSlim SupplementLock = new(1, 1);
    private static readonly SemaphoreSlim CallbackRetryLock = new(1, 1);
    private readonly IFreeSql _freeSql;
    private readonly IConfiguration _configuration;
    private readonly Channel<TokenOrders> _channel;
    private readonly ILogger<AdminController> _logger;
    private readonly List<EVMChain> _chains;

    public AdminController(IFreeSql freeSql, IConfiguration configuration, Channel<TokenOrders> channel,
        ILogger<AdminController> logger, List<EVMChain> chains)
    {
        _freeSql = freeSql;
        _configuration = configuration;
        _channel = channel;
        _logger = logger;
        _chains = chains;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login()
    {
        if (!IsEnabled()) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Orders));
        return View(new AdminLoginModel());
    }

    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(AdminLoginModel model)
    {
        if (!IsEnabled()) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var configuredUser = _configuration["Admin:Username"] ?? string.Empty;
        var configuredHash = _configuration["Admin:PasswordHash"] ?? string.Empty;
        var userMatches = string.Equals(model.Username, configuredUser, StringComparison.Ordinal);
        var passwordMatches = false;
        if (!string.IsNullOrWhiteSpace(configuredHash))
        {
            try
            {
                passwordMatches = new PasswordHasher<object>().VerifyHashedPassword(new object(), configuredHash, model.Password)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { }
        }

        if (!userMatches || !passwordMatches)
        {
            _logger.LogWarning("后台登录失败，来源 IP：{RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            await Task.Delay(Random.Shared.Next(150, 350));
            ModelState.AddModelError(string.Empty, "用户名或密码错误。");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, configuredUser), new Claim(ClaimTypes.Role, "Administrator")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
        _logger.LogInformation("后台用户 {User} 登录成功，来源 IP：{RemoteIp}", configuredUser, HttpContext.Connection.RemoteIpAddress);
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("")]
    public IActionResult Index() => IsEnabled() ? RedirectToAction(nameof(Orders)) : NotFound();

    [HttpGet("orders")]
    public async Task<IActionResult> Orders(int page = 1, int pageSize = DefaultPageSize, Guid? supplement = null,
        string? keyword = null, OrderStatus? status = null, string? currency = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!IsEnabled()) return NotFound();
        page = Math.Max(1, page);
        pageSize = NormalizePageSize(pageSize);
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        currency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim();
        if (keyword?.Length > 200) keyword = keyword[..200];

        var select = _freeSql.Select<TokenOrders>();
        if (keyword != null)
        {
            select.Where(x => x.OutOrderId.Contains(keyword) || x.OrderUserKey.Contains(keyword) ||
                (x.FromAddress != null && x.FromAddress.Contains(keyword)) || x.ToAddress.Contains(keyword) ||
                (x.BlockTransactionId != null && x.BlockTransactionId.Contains(keyword)));
        }
        if (status.HasValue) select.Where(x => x.Status == status.Value);
        if (currency != null) select.Where(x => x.Currency == currency);
        if (startDate.HasValue) select.Where(x => x.CreateTime >= startDate.Value);
        if (endDate.HasValue) select.Where(x => x.CreateTime <= endDate.Value);

        var total = await select.CountAsync();
        var items = await select.OrderByDescending(x => x.CreateTime).Page(page, pageSize).ToListAsync();
        var currencies = HomeController.GetActiveCurrency(_chains);
        return View(new AdminOrderPageModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
            SupplementOrderId = supplement,
            Keyword = keyword,
            Status = status,
            Currency = currency,
            StartDate = startDate,
            EndDate = endDate,
            Currencies = currencies
        });
    }

    [HttpPost("orders/supplement")]
    public async Task<IActionResult> Supplement(SupplementOrderModel model)
    {
        if (!IsEnabled()) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "请完整填写有效的补单信息。";
            return RedirectToAction(nameof(Orders), new { supplement = model.Id });
        }

        await SupplementLock.WaitAsync();
        try
        {
            var repository = _freeSql.GetRepository<TokenOrders>();
            var order = await repository.Where(x => x.Id == model.Id).FirstAsync();
            if (order == null || order.Status != OrderStatus.Expired)
            {
                TempData["AdminError"] = "订单不存在或已不再是失败状态。";
                return RedirectToAction(nameof(Orders));
            }
            var transactionId = model.TransactionId.Trim();
            if (await repository.Select.AnyAsync(x => x.BlockTransactionId == transactionId && x.Id != model.Id))
            {
                TempData["AdminError"] = "该交易哈希已被其他订单使用。";
                return RedirectToAction(nameof(Orders), new { supplement = model.Id });
            }

            order.FromAddress = model.FromAddress.Trim();
            order.BlockTransactionId = transactionId;
            order.Status = OrderStatus.Paid;
            order.PayTime = model.PayTime!.Value;
            order.PayAmount = model.PayAmount!.Value;
            await repository.UpdateAsync(order);
            await _channel.Writer.WriteAsync(order);
            _logger.LogWarning("后台用户 {User} 完成补单，订单 {OrderId}，交易 {TransactionId}", User.Identity?.Name, order.Id, transactionId);
            TempData["AdminSuccess"] = "补单成功，回调与管理员消息已进入处理队列。";
        }
        finally
        {
            SupplementLock.Release();
        }
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost("orders/retry-callback")]
    public async Task<IActionResult> RetryCallback(Guid id)
    {
        if (!IsEnabled()) return NotFound();

        await CallbackRetryLock.WaitAsync(HttpContext.RequestAborted);
        try
        {
            var repository = _freeSql.GetRepository<TokenOrders>();
            var order = await repository.Where(x => x.Id == id).FirstAsync();
            if (order == null || order.Status != OrderStatus.Paid || order.CallbackConfirm || order.CallbackNum < 3 ||
                !Uri.TryCreate(order.NotifyUrl, UriKind.Absolute, out var notifyUri) ||
                (notifyUri.Scheme != Uri.UriSchemeHttp && notifyUri.Scheme != Uri.UriSchemeHttps))
            {
                TempData["AdminError"] = "该订单不满足手动重试回调的条件。";
                return RedirectToAction(nameof(Orders));
            }
            var notifyService = ActivatorUtilities.CreateInstance<OrderNotifyService>(HttpContext.RequestServices);
            var succeeded = await notifyService.ProgressOrderAsync(order, HttpContext.RequestAborted);
            if (succeeded)
            {
                TempData["AdminSuccess"] = "回调重试成功。";
            }
            else
            {
                TempData["AdminError"] = "回调重试失败，请检查商户回调地址和日志。";
            }
            _logger.LogWarning("后台用户 {User} 手动重试订单 {OrderId} 的回调，结果：{Result}",
                User.Identity?.Name, order.Id, succeeded ? "成功" : "失败");
        }
        finally
        {
            CallbackRetryLock.Release();
        }
        return RedirectToAction(nameof(Orders));
    }
    [HttpGet("tokens")]
    public async Task<IActionResult> Tokens(int page = 1, int pageSize = DefaultPageSize)
    {
        if (!IsEnabled()) return NotFound();
        page = Math.Max(1, page);
        pageSize = NormalizePageSize(pageSize);
        var total = await _freeSql.Select<Tokens>().CountAsync();
        var items = await _freeSql.Select<Tokens>()
            .OrderBy(x => x.Id)
            .Page(page, pageSize)
            .ToListAsync(x => new AdminTokenRowModel
            {
                Id = x.Id,
                Address = x.Address,
                Currency = x.Currency,
                Value = x.Value,
                USDT = x.USDT,
                LastCheckTime = x.LastCheckTime
            });
        return View(new AdminPageModel<AdminTokenRowModel> { Items = items, Page = page, PageSize = pageSize, Total = total });
    }

    [HttpPost("tokens/check")]
    public async Task<IActionResult> CheckTronAddress(List<string>? addresses, string? address, int page = 1, int pageSize = DefaultPageSize)
    {
        if (!IsEnabled()) return NotFound();
        var selectedAddresses = (addresses ?? [])
            .Append(address)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();
        if (selectedAddresses.Count == 0)
        {
            TempData["AdminError"] = "请至少选择一个 TRON 钱包。";
            return RedirectToAction(nameof(Tokens), new { page, pageSize });
        }

        var items = await _freeSql.Select<Tokens>()
            .Where(x => x.Currency == TokenCurrency.TRX && selectedAddresses.Contains(x.Address))
            .ToListAsync(x => new AdminTokenRowModel
            {
                Id = x.Id,
                Address = x.Address,
                Currency = x.Currency,
                Value = x.Value,
                USDT = x.USDT,
                LastCheckTime = x.LastCheckTime
            });
        var successCount = 0;
        var failedCount = selectedAddresses.Count - items.Count;
        await Parallel.ForEachAsync(items, new ParallelOptions
        {
            MaxDegreeOfParallelism = BalanceCheckMaxConcurrency,
            CancellationToken = HttpContext.RequestAborted
        }, async (item, cancellationToken) =>
        {
            try
            {
                var trx = await QueryTronAction.GetTRXAsync(item.Address, cancellationToken);
                var usdt = await QueryTronAction.GetUsdtAmountAsync(item.Address, cancellationToken);
                await _freeSql.Update<Tokens>()
                    .Set(x => x.Value, trx)
                    .Set(x => x.USDT, usdt)
                    .Set(x => x.LastCheckTime, DateTime.Now)
                    .Where(x => x.Id == item.Id)
                    .ExecuteAffrowsAsync();
                Interlocked.Increment(ref successCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedCount);
                _logger.LogWarning(ex, "后台检查钱包 {Address} 余额失败", item.Address);
            }
        });

        if (successCount > 0)
            TempData["AdminSuccess"] = $"余额检查完成：成功 {successCount} 个，失败 {failedCount} 个。";
        else
            TempData["AdminError"] = $"余额检查失败，共 {failedCount} 个钱包未能更新。";
        return RedirectToAction(nameof(Tokens), new { page, pageSize });
    }

    [HttpGet("rates")]
    public async Task<IActionResult> Rates(int page = 1, int pageSize = DefaultPageSize)
    {
        if (!IsEnabled()) return NotFound();
        page = Math.Max(1, page);
        pageSize = NormalizePageSize(pageSize);
        var total = await _freeSql.Select<TokenRate>().CountAsync();
        var items = await _freeSql.Select<TokenRate>().OrderBy(x => x.Id).Page(page, pageSize).ToListAsync();
        return View(new AdminPageModel<TokenRate> { Items = items, Page = page, PageSize = pageSize, Total = total });
    }

    private bool IsEnabled() => _configuration.GetValue("Admin:Enabled", false);
    private static int NormalizePageSize(int pageSize) => AllowedPageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
}
