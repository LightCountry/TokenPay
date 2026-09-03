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
3. 按字段名进行区分大小写的升序排列。
4. 拼接为 `key1=value1&key2=value2`，不进行 URL 编码。
5. 根据配置的算法生成签名，输出小写十六进制字符串。

伪代码：

```text
fields = removeEmptyFields(requestFields excluding Signature)
canonicalParameters = join(sortByFieldName(fields), "&", "key=value")

# 默认兼容模式：Signature:UseHmacSha256=false
Signature = md5Utf8(canonicalParameters + ApiToken).toLowerHex()

# 推荐安全模式：Signature:UseHmacSha256=true
Signature = hmacSha256Utf8(key=ApiToken, message=canonicalParameters).toLowerHex()
```

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
| `ActualAmount` | decimal | 是 | 法币金额，必须大于 0，币种由 `BaseCurrency` 决定。业务侧建议保留两位小数。 |
| `Currency` | string | 是 | 完整币种标识，例如 `TRX`、`USDT_TRC20`、`EVM_BSC_BNB`。 |
| `PassThroughInfo` | string | 否 | 透传信息，会在查单和回调中原样返回。不要放入密钥。 |
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

### ③计算 MD5

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
| `IsDynamicAmount` | int | 是否通过动态金额范围匹配：`0` 否、`1` 是。 |
| `Signature` | string | 回调签名，必须验证。 |

值为空的可选字段不会出现在回调 JSON 中。未来版本也可能增加字段，因此不要使用固定字段数量判断签名；应解析收到的完整 JSON，移除 `Signature` 后按通用规则计算。可参考 [Dujiaoka 的 `VerifySign`](../Plugs/dujiaoka/app/Http/Controllers/Pay/TokenPayController.php)。

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

### ③计算 MD5

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
    "data": {
        "id": "66f9d5a8-d9c7-0224-004f-a16a1c068e08",
        ......
    }
}
```

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

### ③计算 MD5

`baa261cc6af3f5efbed15e17a285f653`

### ④最终请求参数

`/Query?Id=66f9d5a8-d9c7-0224-004f-a16a1c068e08&Signature=baa261cc6af3f5efbed15e17a285f653`

实际请求时应对查询参数进行 URL 编码。

查单返回 `TokenOrders` 的订单字段。枚举由当前 JSON 配置输出为名称，例如 `Pending`、`Paid`、`Expired`。调用方应兼容新增字段，不要依赖 JSON 字段顺序。

## 订单状态与接入建议

| 状态 | 数值 | 含义 |
| --- | --- | --- |
| `Pending` | 0 | 等待支付。 |
| `Paid` | 1 | 已识别付款，将执行或已经执行异步通知。 |
| `Expired` | 2 | 超过订单有效期。不要仅凭前端跳转判断付款失败，必要时结合查单和链上记录处理。 |

- 商户应以验签成功的异步回调为主要到账依据，以查单接口作为主动补偿手段。
- `RedirectUrl` 仅用于浏览器跳转，用户可以伪造或中断跳转，不能据此发货。
- 不要根据用户截图确认付款。发生未自动识别的交易时，应在区块浏览器核验；管理员补单流程见 [后台管理说明](admin.md)。
- 商户订单、TokenPay 订单 ID、交易哈希和处理状态应落库，便于审计和幂等控制。
