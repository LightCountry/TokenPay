# `EVMChains.json` 配置说明

`EVMChains.json` 用于定义 TokenPay 支持的 EVM 兼容链及代币。首次部署时，从 [`EVMChains.Example.json`](../src/TokenPay/EVMChains.Example.json) 复制并按需修改。

EVM 链列表在应用启动时加载，修改此文件后需要重启 TokenPay。

## 链配置字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Enable` | bool | 是否启用该链。只有 `true` 时才会生成对应币种并检查订单。 |
| `ChainName` | string | 面向用户显示的链名称。 |
| `ChainNameEN` | string | 链的稳定英文标识，用于币种字符串和 `Address` 专用地址键，例如 `ETH`、`BSC`。修改后会改变 API 的 `Currency` 值。 |
| `BaseCoin` | string | 原生币符号，例如 `ETH`、`BNB`、`POL`。 |
| `Confirmations` | int | 代币交易要求的最少确认数，未配置时默认 12。确认数越低，到账越快但链重组风险越高。 |
| `Decimals` | int | 原生币小数位数，EVM 原生币通常为 18。 |
| `ScanHost` | string | 区块浏览器站点地址，用于生成交易查看链接，例如 `https://etherscan.io`。 |
| `ApiHost` | string? | Etherscan 兼容 API 根地址。留空时使用 `https://api.etherscan.io/v2/`。 |
| `ApiKey` | string | Etherscan V2 API Key。生产环境启用链前必须正确配置。 |
| `ChainId` | int | EVM Chain ID，例如 Ethereum 为 1、BSC 为 56、Polygon 为 137。 |
| `ERC20Name` | string | 代币网络后缀，例如 `ERC20` 或 `BEP20`，会进入币种标识。 |
| `ERC20` | array | 此链支持的代币列表。 |

Etherscan V2 支持的 Chain ID 和 API Key 申请方式，以 [Etherscan 官方文档](https://docs.etherscan.io/etherscan-v2/supported-chains) 为准。

## 代币配置字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Name` | string | 代币符号，例如 `USDT`、`USDC`。该值也用于查询汇率，并进入 `Currency`。 |
| `ContractAddress` | string | 当前链上的代币合约地址。必须从项目方或区块浏览器核对；错误地址会导致无法识别付款。 |

示例：

```json
{
  "Enable": true,
  "ChainName": "币安智能链",
  "ChainNameEN": "BSC",
  "BaseCoin": "BNB",
  "Confirmations": 12,
  "Decimals": 18,
  "ScanHost": "https://www.bscscan.com",
  "ChainId": 56,
  "ApiKey": "你的 Etherscan V2 API Key",
  "ERC20Name": "BEP20",
  "ERC20": [
    {
      "Name": "USDT",
      "ContractAddress": "0x55d398326f99059ff775485246999027b3197955"
    }
  ]
}
```

上述配置会生成：

- 原生币：`EVM_BSC_BNB`
- USDT：`EVM_BSC_USDT_BEP20`

完整命名规则见 [币种参数填写说明](Currency.md)。

## 新增链检查清单

1. 确认区块浏览器 API 与 Etherscan V2 接口兼容。
2. 从可信来源核对 `ChainId`、原生币精度、浏览器地址和代币合约。
3. 使用独立的生产 API Key，不要将 Key 提交到公开仓库。
4. 先保持 `Enable=false`，完成配置检查后再启用并重启。
5. 用小额原生币和代币订单分别测试创建、付款识别、回调和交易链接。
6. 不要随意修改已经投入使用的 `ChainNameEN`、`BaseCoin` 或 `ERC20Name`，否则新旧订单的币种标识可能不一致。
