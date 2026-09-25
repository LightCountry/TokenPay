# TokenPay API 对接文档

本文面向需要创建订单、接收支付结果并查询订单状态的商户系统。也可以参考仓库中的 [Dujiaoka 插件](../Plugs/dujiaoka/) 等现有实现。

## 接入前准备

1. 为 TokenPay 配置可公开访问的 HTTPS `WebSiteUrl`。
2. 将 `ApiToken` 修改为足够长的随机字符串，并通过安全渠道保存到商户系统。
3. 按 [币种参数说明](Currency.md) 确认可用的 `Currency`。
4. 准备 HTTPS `NotifyUrl`，实现签名验证、幂等处理和纯文本 `ok` 响应。
5. 使用小额订单完成创建、付款、回调、查单的全流程测试。

> `ApiToken` 是共享签名密钥，不能放在浏览器、App 或前端 JavaScript 中。所有 API 请求应由商户服务端发起，并使用 HTTPS。

## 通用响应

接口通常返回 JSON：

```json
{
  "success": true,
  "message": "操作成功",
  "data": null,
  "info": null
}
```

- `success=true` 表示业务处理成功。
- HTTP 200 不等于业务成功，调用方必须检查 `success`。
- 字段使用 camelCase；`info` 中的字典键可能保持文档所示的 PascalCase。

## 签名规则

创建订单、生产环境查单和异步回调使用相同的参数规范化规则：

1. 取参与签名的所有字段，排除 `Signature`。
2. 忽略值为 `null` 或空字符串的字段；数值 `0` 和布尔值不能当作空值丢弃。
3. 按字段名进行区分大小写的升序排列，即按字节（ASCII）顺序比较，例如 PHP 的 `ksort($data, SORT_STRING)`、C# 的 `StringComparer.Ordinal`。不要使用受区域设置影响的排序。
4. 拼接为 `key1=value1&key2=value2`，不进行 URL 编码。
5. 根据配置的算法生成签名，输出小写十六进制字符串。
6. 确保 bool 类型对应 true/false，部分编程语言默认可能转为1/0或者True/False
7. 金额类字段按原始文本参与签名，原因见下文 [为什么金额类字段使用字符串传递](#为什么金额类字段使用字符串传递)：
   - 回调和查单结果中的金额字段均为字符串，请按收到的字符串原样参与签名，不要转换为数字后再转回字符串，否则可能丢失小数位（如 `10.00` 变为 `10`）。
   - 创建订单时，签名中金额的写法必须与请求 JSON 中的写法完全一致：JSON 中写 `15.00`，签名中也写 `15.00`。
8. 异步回调参数后续可能增减，建议使用动态解析 JSON 的方式获取字段，避免使用固定字段数量判断签名。

### 为什么金额类字段使用字符串传递

回调和查单结果中的金额类字段（`Amount`、`ActualAmount`、`PayAmount`、`MinCustomAmount`、`MaxCustomAmount`）都以 JSON 字符串传递，例如 `"ActualAmount": "10.00"`，而不是 JSON 数字 `10.00`。

签名要求双方拼出完全相同的文本。而 JSON 数字在大多数语言中会被解析为浮点数，再转回字符串时，文本可能发生变化。下表是 PHP 8.3、Node.js、Python 3.12、Go 1.25 解析 JSON 数字后再用常见方式转为字符串的实测结果：

| JSON 中的数字 | PHP | Node.js | Python | Go（`fmt.Sprint`） | 问题 |
| --- | --- | --- | --- | --- | --- |
| `10.00` | `10` | `10` | `10.0` | `10` | 末尾的零丢失，且各语言结果不同 |
| `0.00001` | `1.0E-5` | `0.00001` | `1e-05` | `1e-05` | 小于 0.0001 时变为科学计数法。ETH 默认保留 5 位小数，小额订单会遇到 |
| `12345678.9` | 不变 | 不变 | 不变 | `1.23456789e+07` | Go 默认格式对较大的数使用科学计数法 |
| `1234567890123.45` | `1234567890123.4` | 不变 | 不变 | `1.23456789012345e+12` | PHP 超过 14 位有效数字时会截断。链上实付金额 `PayAmount` 可能有多达 18 位小数 |

以上任何一种变化都会导致签名原文不一致、验签失败。使用字符串传递后：

- 各语言解析 JSON 得到的都是原始文本，拼出的签名原文与 TokenPay 完全一致；
- 金额不经过浮点数，不会损失精度；
- TokenPay 固定使用 `.` 作为小数点、不使用千位分隔符，格式不受服务器系统语言影响。

对接建议：

- 验签时直接使用收到的字符串，不要先转换为数字；
- 数字类型的字段（如 `Status`）请使用 JSON 原文参与签名。例如 Go 可用 `json.Decoder` 的 `UseNumber()` 保留原文，C# 可用 `JsonElement.GetRawText()`；
- 验签通过后如需计算金额，请转换为十进制精确类型，例如 PHP 的 BCMath、Java 的 `BigDecimal`、Python 的 `decimal.Decimal`、C# 的 `decimal`，不要使用浮点数；
- 创建订单时，金额同样建议以字符串传递（如 `"ActualAmount": "15.00"`），并在签名中使用相同的文本。TokenPay 同时接受字符串和数字形式的金额。

伪代码：

```text
fields = removeEmptyFields(requestFields excluding Signature)
canonicalParameters = join(sortByFieldName(fields), "&", "key=value")

# 默认兼容模式：Signature:UseHmacSha256=false
Signature = md5Utf8(canonicalParameters + ApiToken).toLowerHex()

# 推荐安全模式：Signature:UseHmacSha256=true
Signature = hmacSha256Utf8(key=ApiToken, message=canonicalParameters).toLowerHex()
```

C# 签名工具类：可直接复制到项目中使用，只依赖 .NET 自带类库，不依赖 TokenPay 的任何代码。适用于 .NET 6 及以上版本，项目需启用 `ImplicitUsings` 和 `Nullable`（.NET 6 起新建项目默认启用）。

<details>
<summary>SignatureHelper.cs（点击展开）</summary>

```csharp
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// TokenPay 签名工具类
/// </summary>
public static class SignatureHelper
{
    /// <summary>
    /// 为请求参数生成签名，用于创建订单、查询订单
    /// </summary>
    /// <param name="parameters">请求参数，不含 Signature</param>
    /// <param name="apiToken">与 TokenPay 配置中的 ApiToken 相同</param>
    /// <param name="useHmacSha256">必须与 TokenPay 的 Signature:UseHmacSha256 一致（TokenPay 默认为 false，即 MD5）</param>
    public static string Sign(IEnumerable<KeyValuePair<string, object?>> parameters, string apiToken, bool useHmacSha256)
        => Create(BuildCanonicalParameters(parameters), apiToken, useHmacSha256);

    /// <summary>
    /// 验证 TokenPay 异步回调的签名
    /// </summary>
    /// <param name="json">回调请求的原始 JSON 文本</param>
    public static bool VerifyJson(string json, string apiToken, bool useHmacSha256)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            // 直接使用收到的全部字段，不要按固定字段列表拼接
            var fields = root.EnumerateObject().Select(x => KeyValuePair.Create(x.Name, (object?)x.Value));
            var signature = root.TryGetProperty("Signature", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
            return Verify(BuildCanonicalParameters(fields), signature, apiToken, useHmacSha256);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// 按签名规则拼接规范化参数：排除 Signature，忽略 null 和空字符串，
    /// 字段名按字节顺序排列，bool 写作 true/false，字符串（含金额）按原文使用
    /// </summary>
    public static string BuildCanonicalParameters(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        return string.Join("&", parameters
            .Where(x => x.Key != "Signature")
            .Select(x => (x.Key, Value: FormatValue(x.Value)))
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal) // 按字节顺序排序，不受区域设置影响
            .Select(x => $"{x.Key}={x.Value}"));
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        bool b => b ? "true" : "false",
        // 来自 JSON 的值
        JsonElement e => e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(), // 字符串按原文使用，金额字段不要转为数字
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => e.GetRawText() // 数字按 JSON 原文使用
        },
        // 数值（decimal、int、long 等）、Guid、枚举等实现了 IFormattable 的类型，按 InvariantCulture 格式化，
        // 避免服务器系统语言把小数点变成逗号。注意：
        // - 金额请使用字符串或 decimal，不要使用 double：double 的小额会变成科学计数法（如 0.00001 → 1E-05）
        // - 日期请先自行格式化为字符串：DateTime 在此会被格式化为美式的 09/25/2026 14:03:21
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    /// <summary>
    /// 根据规范化参数计算签名，输出小写十六进制字符串
    /// </summary>
    public static string Create(string canonicalParameters, string apiToken, bool useHmacSha256)
    {
        var hash = useHmacSha256
            // HMAC-SHA256：以 ApiToken 为密钥，规范化参数为消息
            ? HMACSHA256.HashData(Encoding.UTF8.GetBytes(apiToken), Encoding.UTF8.GetBytes(canonicalParameters))
            // MD5 兼容模式：md5(规范化参数 + ApiToken)
            : MD5.HashData(Encoding.UTF8.GetBytes(canonicalParameters + apiToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 校验签名，签名不区分大小写，使用固定时间比较
    /// </summary>
    public static bool Verify(string canonicalParameters, string? providedSignature, string apiToken, bool useHmacSha256)
    {
        if (string.IsNullOrWhiteSpace(providedSignature)) return false;
        var expected = Create(canonicalParameters, apiToken, useHmacSha256);
        var actual = providedSignature.Trim();
        if (expected.Length != actual.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(actual));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
```

</details>

使用示例（`apiToken`、`useHmacSha256` 请替换为自己的配置）：

<details>
<summary>创建订单</summary>

```csharp
var apiToken = "你的 ApiToken";
var useHmacSha256 = false; // 与 TokenPay 的 Signature:UseHmacSha256 保持一致

var request = new Dictionary<string, object?>
{
    ["OutOrderId"] = "AJIHK72N34BR2CWG",
    ["OrderUserKey"] = "admin@qq.com",
    ["ActualAmount"] = "15.00", // 金额建议使用字符串，签名和 JSON 使用同一文本
    ["Currency"] = "TRX",
    ["NotifyUrl"] = "https://shop.example.com/pay/tokenpay/notify",
    ["RedirectUrl"] = "https://shop.example.com/pay/tokenpay/return",
};
request["Signature"] = SignatureHelper.Sign(request, apiToken, useHmacSha256);

using var http = new HttpClient();
var response = await http.PostAsync("https://pay.example.com/CreateOrder",
    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
var result = await response.Content.ReadAsStringAsync();
// result 为 JSON：success 为 true 时，data 为支付页地址
```

</details>

<details>
<summary>查询订单</summary>

```csharp
var id = "66f9d5a8-d9c7-0224-004f-a16a1c068e08"; // TokenPay 订单 ID
var signature = SignatureHelper.Sign(new Dictionary<string, object?> { ["Id"] = id }, apiToken, useHmacSha256);

using var http = new HttpClient();
var result = await http.GetStringAsync($"https://pay.example.com/Query?Id={Uri.EscapeDataString(id)}&Signature={signature}");
```

</details>

<details>
<summary>验证异步回调（ASP.NET Core）</summary>

```csharp
[HttpPost("pay/tokenpay/notify")]
public async Task<IActionResult> TokenPayNotify()
{
    // 读取原始请求体验签，不依赖模型绑定
    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    if (!SignatureHelper.VerifyJson(body, apiToken, useHmacSha256))
    {
        return Content("fail");
    }

    using var json = JsonDocument.Parse(body);
    var root = json.RootElement;
    var outOrderId = root.GetProperty("OutOrderId").GetString();
    var status = root.GetProperty("Status").GetInt32(); // 1 表示已支付
    // 金额为字符串，需要计算时用 decimal 转换
    var actualAmount = decimal.Parse(root.GetProperty("ActualAmount").GetString()!, CultureInfo.InvariantCulture);

    // 根据 outOrderId 查询系统内订单，检查订单状态和金额后完成业务处理
    // （是否允许过期订单回调取决于你的业务设计）

    return Content("ok");
}
```

</details>

字段名、日期、数值和布尔值的字符串格式必须与实际发送内容一致。建议先构造最终请求对象，再基于该对象生成签名，避免签名后修改字段。

### 签名算法配置

```json
"Signature": {
  "UseHmacSha256": false
}
```

| 配置值 | 算法 | 签名长度 | 说明 |
| --- | --- | --- | --- |
| `false` | MD5 | 32 个十六进制字符 | 默认值，保持现有接入方完全兼容。计算内容为 `canonicalParameters + ApiToken`。 |
| `true` | HMAC-SHA256 | 64 个十六进制字符 | 推荐。以 `ApiToken` 为 HMAC 密钥，以 `canonicalParameters` 为消息；不要再把密钥追加到消息末尾。 |

该开关同时控制创建订单验签、查单验签和 TokenPay 发出的异步回调签名。启用 HMAC-SHA256 前，必须先升级所有商户端的请求签名和回调验签代码，然后再修改 TokenPay 配置。切换后旧 MD5 请求不会同时被接受，避免算法降级攻击。

使用下文原始创建订单参数、`ApiToken=666` 时，HMAC-SHA256 的规范化参数仍为：

```text
ActualAmount=15&Currency=TRX&NotifyUrl=http://localhost:1011/pay/tokenpay/notify_url&OrderUserKey=admin@qq.com&OutOrderId=AJIHK72N34BR2CWG&RedirectUrl=http://localhost:1011/pay/tokenpay/return_url?order_id=AJIHK72N34BR2CWG
```

对应的 HMAC-SHA256 签名为：

```text
c879776795a9e85ce674aa10c8315de323d6f8a20bf157f92595b71ad77f1e12
```

> 下文保留的三组签名示例均为默认 MD5 兼容模式的原始测试向量，示例参数和返回数据未作修改。

MD5 仅用于兼容现有接口，不用于保存密码。无论选择哪种算法，都必须使用高强度随机 `ApiToken`、HTTPS，并验证回调签名；不要只依赖来源 IP。

## 1. 创建订单

```text
POST /CreateOrder
Content-Type: application/json
```

### 请求字段

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `OutOrderId` | string | 是 | 商户订单号。相同币种下，未过期订单号应保持唯一。 |
| `OrderUserKey` | string | 是 | 付款用户稳定标识，建议使用用户ID或者用户邮箱地址。动态地址模式会用它关联地址；如需每单新地址，可以传唯一的外部订单号。避免传不必要的敏感个人信息。 |
| `ActualAmount` | decimal | 是 | 法币金额，必须大于 0，币种由 `BaseCurrency` 决定。业务侧建议保留两位小数。可传字符串或数字，建议使用字符串，见 [为什么金额类字段使用字符串传递](#为什么金额类字段使用字符串传递)。 |
| `Currency` | string | 是 | 完整币种标识，例如 `TRX`、`USDT_TRC20`、`EVM_BSC_BNB`。 |
| `PassThroughInfo` | string | 否 | 透传信息，会在查单和回调中原样返回。不要放入密钥。 |
| `IsCustomAmount` | bool | 否 | 是否允许动态金额，仅允许启用动态收款地址时使用。签名中写作 `true`/`false`。v1.2.0起支持 |
| `MinCustomAmount` | decimal | 否 | 动态金额下限制最小金额，低于此金额的付款会被忽略。v1.2.0起支持 |
| `MaxCustomAmount` | decimal | 否 | 动态金额下限制最大金额，高于此金额的付款会被忽略。v1.2.0起支持 |
| `NotifyUrl` | string | 否 | 支付成功异步通知地址，生产环境应使用 HTTPS。 |
| `RedirectUrl` | string | 否 | 支付完成或订单过期后的前端跳转地址。它不是支付结果依据。 |
| `Signature` | string | 是 | 请求签名。默认在所有环境校验；仅非 Production 环境显式配置 `Signature:AllowInsecureDevelopment=true` 时跳过。 |

### ①示例 POST 参数（默认 MD5 兼容模式）

```json
{
    "OutOrderId": "AJIHK72N34BR2CWG",
    "OrderUserKey": "admin@qq.com",
    "ActualAmount": 15,
    "Currency": "TRX",
    "NotifyUrl": "http://localhost:1011/pay/tokenpay/notify_url",
    "RedirectUrl": "http://localhost:1011/pay/tokenpay/return_url?order_id=AJIHK72N34BR2CWG"
}
```

### ②按照 ASCII 排序后拼接

`ActualAmount=15&Currency=TRX&NotifyUrl=http://localhost:1011/pay/tokenpay/notify_url&OrderUserKey=admin@qq.com&OutOrderId=AJIHK72N34BR2CWG&RedirectUrl=http://localhost:1011/pay/tokenpay/return_url?order_id=AJIHK72N34BR2CWG`

异步通知密钥为：`666`

拼接密钥后：

`ActualAmount=15&Currency=TRX&NotifyUrl=http://localhost:1011/pay/tokenpay/notify_url&OrderUserKey=admin@qq.com&OutOrderId=AJIHK72N34BR2CWG&RedirectUrl=http://localhost:1011/pay/tokenpay/return_url?order_id=AJIHK72N34BR2CWG666`

### ③计算签名

`e9765880db6081496456283678e70152`

### ④POST 参数增加 `Signature`

```json
{
    "OutOrderId": "AJIHK72N34BR2CWG",
    "OrderUserKey": "admin@qq.com",
    "ActualAmount": 15,
    "Currency": "TRX",
    "NotifyUrl": "http://localhost:1011/pay/tokenpay/notify_url",
    "RedirectUrl": "http://localhost:1011/pay/tokenpay/return_url?order_id=AJIHK72N34BR2CWG",
    "Signature": "e9765880db6081496456283678e70152"
}
```

### ⑤返回数据示例

创建订单成功的返回示例：

```json
{
    "success": true,
    "message": "创建订单成功！",
    "data": "http://127.0.0.1:5000/Pay?Id=6324ddd2-4677-7914-0010-702806ae9766",
    "info": {
        "ActualAmount": "15",//法币金额
        "Amount": "227.34",//支付的区块链货币金额
        "BaseCurrency": "CNY",//法币币种
        "BlockChainName": "TRON",//付款区块链
        "CurrencyName": "TRX", //付款币种
        "ExpireTime": "2023-04-28 14:04:57", //付款过期时间
        "Id": "644bc479-df0c-3f1c-00fe-9cb3012b148b", //订单Id
        "OrderUserKey": "admin@qq.com", //用户识别Key
        "OutOrderId": "AJIHK72N34BR2CWG", //商户订单号
        "QrCodeBase64": "data:image/png;base64,xxxxxxxxx", //base64格式的图片
        "QrCodeLink": "http://127.0.0.1:5000/GetQrCode?Id=644bc479-df0c-3f1c-00fe-9cb3012b148b", //二维码图片链接，如需修改图片尺寸，可拼接参数 &Size=xxx, 这里的xxx为数字，表示图片宽高，默认为300
        "ToAddress": "TKGTx4pCKiKQbk8evXHTborfZn754TGViP" //付款地址
    }
}
```

`data` 是支付页面地址；`info.Amount` 是换算后的链上应付金额；`info.ActualAmount` 是原始法币金额。

如果同一个 `OutOrderId + Currency` 已存在且状态不是 `Expired`，接口会返回原订单，`message` 为“订单已存在，查询旧订单！”。调用方应把创建接口按幂等方式处理。

创建订单失败的返回示例：

```json
{
  "success": false,
  "message": "签名验证失败！"
}
```

常见失败原因包括签名错误、金额不大于 0、币种未启用、地址未配置或换算后金额过低。

## 2. 获取二维码

```text
GET /GetQrCode?Id={TokenPay订单Id}&Size=300
```

- `Id` 为 TokenPay 内部订单 GUID。
- `Size` 可选，默认 300，表示二维码宽高。
- `Size` 有效范围为 100～1000，超出范围返回 HTTP 400。
- 订单不存在时返回空的 PNG 内容，调用方应先确认订单有效。

创建订单响应已经包含 `QrCodeBase64` 和 `QrCodeLink`，通常无需单独调用。

## 3. 异步支付回调

TokenPay 仅在订单状态为 `Paid` 时向创建订单传入的 `NotifyUrl` 发起回调。订单过期不会回调。

```text
POST {NotifyUrl}
Content-Type: application/json
```

### 回调字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Id` | string | TokenPay 内部订单 ID。 |
| `BlockTransactionId` | string | 链上交易哈希。 |
| `OutOrderId` | string | 商户订单号。 |
| `OrderUserKey` | string | 创建订单时的用户标识。 |
| `PayTime` | string | 支付时间，格式如 `yyyy-MM-dd HH:mm:ss`。 |
| `BlockChainName` | string | 区块链英文名称。 |
| `Currency` | string | 完整币种标识。 |
| `CurrencyName` | string | 面向展示的币种名称。 |
| `BaseCurrency` | string | 法币币种。 |
| `Amount` | string | 订单要求支付的链上金额。 |
| `ActualAmount` | string | 原始法币金额。 |
| `PayAmount` | string | 链上实际支付金额。 |
| `FromAddress` | string | 付款地址。 |
| `ToAddress` | string | 收款地址。 |
| `Status` | int | `0` 待支付、`1` 已支付、`2` 已过期。支付回调正常为 `1`。 |
| `PassThroughInfo` | string | 创建订单时传入的透传信息。 |
| `IsDynamicAmount` | bool | 是否通过动态金额范围匹配。签名中写作 `true`/`false`。 |
| `IsCustomAmount` | bool | 是否为任意金额订单。签名中写作 `true`/`false`。 |
| `MinCustomAmount` | string | 任意金额订单的最小金额，未设置时不出现。 |
| `MaxCustomAmount` | string | 任意金额订单的最大金额，未设置时不出现。 |
| `SignatureType` | string | 本次回调使用的签名算法：`MD5` 或 `HmacSha256`。 |
| `Signature` | string | 回调签名，必须验证。 |

金额类字段均为字符串，验签时请按原文使用，原因见 [为什么金额类字段使用字符串传递](#为什么金额类字段使用字符串传递)。值为空的可选字段不会出现在回调 JSON 中。未来版本也可能增加字段，因此不要使用固定字段数量判断签名；应解析收到的完整 JSON，移除 `Signature` 后按通用规则计算。可参考 [Dujiaoka 的 `VerifySign`](../Plugs/dujiaoka/app/Http/Controllers/Pay/TokenPayController.php)。

### ①示例 POST 参数（默认 MD5 兼容模式）

```json
{
    "ActualAmount": "15",
    "Amount": "34.91",
    "BaseCurrency": "CNY",
    "BlockChainName": "TRON",
    "BlockTransactionId": "375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5",
    "Currency": "TRX",
    "CurrencyName": "TRX",
    "FromAddress": "TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa",
    "Id": "63234df7-55bf-93fc-0010-67be493c0c27",
    "OutOrderId": "E6COE6FGZMO5AXSK",
    "PayTime": "2022-09-15 16:08:39",
    "Status": 1,
    "ToAddress": "TKGTx4pCKiKQbk8evXHTborfZn754TGViP"
}
```

### ②按照 ASCII 排序后拼接

`ActualAmount=15&Amount=34.91&BaseCurrency=CNY&BlockChainName=TRON&BlockTransactionId=375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5&Currency=TRX&CurrencyName=TRX&FromAddress=TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa&Id=63234df7-55bf-93fc-0010-67be493c0c27&OutOrderId=E6COE6FGZMO5AXSK&PayTime=2022-09-15 16:08:39&Status=1&ToAddress=TKGTx4pCKiKQbk8evXHTborfZn754TGViP`

假设异步通知密钥为：`666`

拼接密钥后：

`ActualAmount=15&Amount=34.91&BaseCurrency=CNY&BlockChainName=TRON&BlockTransactionId=375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5&Currency=TRX&CurrencyName=TRX&FromAddress=TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa&Id=63234df7-55bf-93fc-0010-67be493c0c27&OutOrderId=E6COE6FGZMO5AXSK&PayTime=2022-09-15 16:08:39&Status=1&ToAddress=TKGTx4pCKiKQbk8evXHTborfZn754TGViP666`

### ③计算签名

`e5eaa888cd9e80b5c09a0698981757c8`

对比 POST 中的 `Signature` 是否与此值一致。

### 正确响应与重试

商户处理成功后必须返回：

```text
ok
```

要求同时满足：

- HTTP 状态码为 200。
- 响应正文严格等于小写纯文本 `ok`，不能包含 JSON、HTML、引号、空格或换行。

失败时 TokenPay 至少间隔约 1 分钟再次尝试，单个订单最多发起 3 次通知（首次加最多两次重试），单次请求超时由 `NotifyTimeOut` 控制。

商户回调必须具备幂等性：先验证签名，再使用 `Id` 或 `OutOrderId` 做唯一约束；重复通知只能重复返回 `ok`，不能重复发货、充值或记账。建议记录原始请求、验签结果和处理结果，但不要记录 `ApiToken`。

## 4. 查询订单

```text
GET /Query?Id={TokenPay订单Id}&Signature={签名}
```

### 示例返回

```json
{
    "success": false,
    "message": "订单不存在！"
}
```

```json
{
    "success": true,
    "message": "订单信息获取成功！",
    "data": { //查单返回data数据字段与异步回调数据相同
        "id": "66f9d5a8-d9c7-0224-004f-a16a1c068e08",
        ......
    }
}
```

### **查单返回data数据字段与异步回调数据相同**

生产环境会验证签名。签名只包含 `Id`。

### ①示例参数（默认 MD5 兼容模式）

```text
/Query?Id=66f9d5a8-d9c7-0224-004f-a16a1c068e08
```

### ②按照 ASCII 排序后拼接

`Id=66f9d5a8-d9c7-0224-004f-a16a1c068e08`

假设异步通知密钥为：`666`

拼接密钥后：

`Id=66f9d5a8-d9c7-0224-004f-a16a1c068e08666`

### ③计算签名

`baa261cc6af3f5efbed15e17a285f653`

### ④最终请求参数

`/Query?Id=66f9d5a8-d9c7-0224-004f-a16a1c068e08&Signature=baa261cc6af3f5efbed15e17a285f653`

实际请求时应对查询参数进行 URL 编码。

## 接入建议

- 商户应以验签成功的异步回调为主要到账依据，以查单接口作为主动补偿手段。
- `RedirectUrl` 仅用于浏览器跳转，用户可以伪造或中断跳转，不能据此发货。
- 不要根据用户截图确认付款。发生未自动识别的交易时，应在区块浏览器核验；管理员补单流程见 [后台管理说明](admin.md)。
- 商户订单、TokenPay 订单 ID、交易哈希和处理状态应落库，便于审计和幂等控制。
