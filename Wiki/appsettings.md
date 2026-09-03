# `appsettings.json` 配置说明

`appsettings.json` 是 TokenPay 的主配置文件。建议从 [`appsettings.Example.json`](../src/TokenPay/appsettings.Example.json) 复制后修改，不要直接覆盖保存有真实配置的文件。

> **资产安全警告：** `TokenPay.db` 中可能保存动态收款地址及其私钥。请定期加密备份，并严格限制文件权限。数据库或备份泄露可能导致资产被盗；数据库丢失则可能导致动态地址中的资产无法找回。

## 必须修改的配置

- `ApiToken`：API 请求及异步回调签名密钥，必须改为足够长的随机字符串。
- `WebSiteUrl`：用户能够访问的 TokenPay 外部 HTTPS 地址。
- `Address`：使用静态地址收款时，填写自己控制的真实收款地址。
- `Telegram`：填写自己的机器人 Token 和管理员用户 ID。
- `TRON-PRO-API-KEY`：生产环境建议配置 TronGrid API Key。
- `Admin`：仅在需要后台管理页面时配置，详见 [后台管理说明](admin.md)。

不要把包含真实密钥、机器人 Token、钱包地址私钥或管理员密码哈希的配置文件提交到公开仓库。

## 基础配置

| 配置项 | 类型 | 默认值/示例 | 说明 |
| --- | --- | --- | --- |
| `AllowedHosts` | string | `*` | ASP.NET Core Host 过滤。公网部署可改成实际域名。 |
| `ConnectionStrings:DB` | string | `Data Source=TokenPay.db;` | SQLite 数据库连接字符串。相对路径以程序工作目录为基准。 |
| `BaseCurrency` | string | `CNY` | 法币基准，支持 `CNY`、`USD`、`EUR`、`GBP`、`AUD`、`HKD`、`TWD`、`SGD`。 |
| `ExpireTime` | int | `1800` | 订单有效期，单位为秒。 |
| `OnlyConfirmed` | bool | `true` | TRON 查询是否只读取已确认交易。设为 `false` 回调可能更快，但需要自行承担未确认交易风险。 |
| `NotifyTimeOut` | number | `3` | 商户异步通知 HTTP 超时时间，单位为秒。 |
| `ApiToken` | string | 无安全默认值 | 创建订单、查单和回调签名使用的共享密钥。 |
| `Signature:UseHmacSha256` | bool | `false` | `false` 使用兼容旧客户端的 MD5；`true` 使用推荐的 HMAC-SHA256。 |
| `Signature:AllowInsecureDevelopment` | bool | `false` | 是否允许非 Production 环境跳过请求签名。仅限隔离的本机调试。 |
| `WebSiteUrl` | string | `https://pay.example.com` | TokenPay 的外部访问地址，用于生成支付页和二维码链接。 |
| `WebProxy` | string? | 空 | 可选的 HTTP 代理地址。仅在运行环境确实需要代理时配置。 |
| `TRON-PRO-API-KEY` | string | 空 | TronGrid 生产环境 API Key。 |
| `TronApiHost` | string | `https://api.trongrid.io` | 可选的 TRON API 地址，通常无需修改。 |
| `ContractAddress` | string | TRON USDT 合约 | 可选的 TRC-20 合约地址覆盖项，通常无需修改。 |

## 固定汇率与显示精度

`Rate` 中某币种设为 `0` 时使用自动汇率；设为非零值时使用配置的固定汇率：

```json
"Rate": {
  "USDT": 0,
  "TRX": 0,
  "ETH": 0,
  "USDC": 0
}
```

`RateMove` 用于在自动或固定汇率结果上做微调，键名格式为 `币种_法币`：

```json
"RateMove": {
  "TRX_CNY": 0,
  "USDT_CNY": 0
}
```

`Decimals` 控制订单金额展示和取整精度，可按币种补充：

```json
"Decimals": {
  "TRX": 2,
  "ETH": 5,
  "USDT_TRC20": 4
}
```

## API 签名算法

```json
"Signature": {
  "UseHmacSha256": false,
  "AllowInsecureDevelopment": false
}
```

- `false`：默认值，继续使用旧版 MD5 签名，现有接入方无需修改。
- `true`：使用推荐的 HMAC-SHA256，签名为 64 位小写十六进制字符串。

此开关会同时改变创建订单、查单和异步回调签名。启用 HMAC-SHA256 前，必须先让所有商户端支持新算法；切换后 TokenPay 不再接受 MD5 请求，发出的回调也会改用 HMAC-SHA256。完整算法和测试向量见 [API 对接文档](docs.md#签名规则)。

接口默认在所有环境验证签名。只有显式设置 `AllowInsecureDevelopment=true` 时，非 Production 环境才会跳过验证；Production 环境始终验证。禁止在可被他人访问的环境中开启此选项。

`ApiToken` 在 MD5 模式中追加到规范化参数末尾；在 HMAC-SHA256 模式中作为 HMAC 密钥，不能再追加到消息中。两种模式都必须使用足够长的随机密钥并通过 HTTPS 传输。

## 收款地址模式

### 静态地址

设置 `UseDynamicAddress=false`，并配置：

```json
"Address": {
  "TRON": [ "你的 TRON 地址" ],
  "EVM": [ "你的 EVM 地址" ],
  "BSC": [ "可选：BSC 专用地址" ]
}
```

- `TRON` 用于 TRX 和 TRC-20。
- `EVM` 是所有 EVM 链的默认地址。
- 可以用 `ChainNameEN`（例如 `BSC`、`ETH`、`Polygon`）配置某条链的专用地址；专用配置优先于通用 `EVM` 地址。
- 地址必须属于你并且能够控制私钥。先用小额交易验证网络和地址。

### 动态地址

设置 `UseDynamicAddress=true` 后，TokenPay 会按 `OrderUserKey` 分配和复用动态收款地址。若需要每个订单一个新地址，创建订单时可将唯一的外部订单号作为 `OrderUserKey`。

动态地址及私钥保存在 `TokenPay.db`，因此数据库备份尤为重要。

## 动态金额匹配

动态地址模式下可开启一定范围内的金额偏差匹配：

```json
"DynamicAddressConfig": {
  "AmountMove": false,
  "TRX": [ 0, 2 ],
  "USDT": [ 1, 2 ],
  "ETH": [ 0.1, 0.15 ]
}
```

数组格式为 `[允许少付金额, 允许多付金额]`。例如订单应付 100 USDT，配置 `[1, 2]` 时，99 至 102 USDT 都可能匹配成功。此功能可能增加错配风险，启用前必须结合订单量、地址模式和业务容错范围评估。

## TRON 自动归集

归集只对动态地址有意义：

| 配置项 | 说明 |
| --- | --- |
| `Collection:Enable` | 是否启用归集。 |
| `UseEnergy` | 是否租用能量后归集 USDT。 |
| `ForceCheckAllAddress` | 是否强制检查所有动态地址。地址多时会增加 API 请求。 |
| `RetainUSDT` | 归集时是否保留极少量 USDT。 |
| `CheckTime` | 归集任务间隔，单位为小时。 |
| `MinUSDT` | 余额大于该值时才归集。 |
| `NeedEnergy` | 归集所需能量估计值，通常无需修改。 |
| `EnergyMinValue` | 能量最低目标值，可选，通常无需修改。 |
| `EnergyPrice` | 能量单价配置，通常无需修改。 |
| `RentDuration` | 租用时长，可选。 |
| `RentTimeUnit` | 租用时长单位，可选，例如 `m`。 |
| `Address` | 最终归集地址，必须由你控制。 |

启用归集会产生链上手续费或能量租用费用。请先在小额环境验证，并为动态地址准备足够的 TRX/能量。

## Telegram 通知

```json
"Telegram": {
  "AdminUserId": 12345678,
  "BotToken": "从 BotFather 获取的 Token"
}
```

- `AdminUserId` 是接收到账通知的 Telegram 用户 ID。
- `BotToken` 属于敏感凭据，泄露后应立即通过 BotFather 吊销并重新生成。

## 后台管理

```json
"Admin": {
  "Enabled": false,
  "Username": "admin",
  "PasswordHash": "",
  "SessionMinutes": 30,
  "RequireHttps": true
}
```

后台默认关闭。密码必须以哈希形式配置，生产环境必须使用 HTTPS。完整配置、启停方式和人工补单风险说明见 [后台管理说明](admin.md)。

## 配置生效与排错

- `appsettings.json` 默认支持变更重载，多数业务配置会在后续任务或请求中读取。
- 部分启动时注册的配置需要重启，例如后台的 `SessionMinutes`、`RequireHttps`，以及启动时载入的 EVM 链列表。
- JSON 支持注释，但仍需保证逗号、引号和括号正确。
- 修改前备份配置与数据库；修改地址、链或金额匹配规则后，先创建小额订单测试完整支付和回调流程。
