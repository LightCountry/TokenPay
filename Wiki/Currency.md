# 币种参数填写说明

创建订单时，`Currency` 必须填写 TokenPay 能识别的完整币种标识。标识区分链和代币网络，不要只填写代币简称。

## TRON

| 资产 | `Currency` 值 |
| --- | --- |
| TRX | `TRX` |
| TRON 上的 USDT | `USDT_TRC20` |

## EVM 兼容链

EVM 币种由 `EVMChains.json` 生成。

- 原生币格式：`EVM_[ChainNameEN]_[BaseCoin]`
- 代币格式：`EVM_[ChainNameEN]_[Token.Name]_[ERC20Name]`

示例：

| 资产 | `Currency` 值 |
| --- | --- |
| Ethereum 原生 ETH | `EVM_ETH_ETH` |
| Ethereum USDT | `EVM_ETH_USDT_ERC20` |
| BNB Smart Chain 原生 BNB | `EVM_BSC_BNB` |
| BNB Smart Chain USDT | `EVM_BSC_USDT_BEP20` |
| Polygon 原生 POL | `EVM_Polygon_POL` |
| Polygon USDC | `EVM_Polygon_USDC_ERC20` |

默认样例中全部链和代币都启用时，可用值为：

```text
TRX
USDT_TRC20
EVM_ETH_ETH
EVM_ETH_USDT_ERC20
EVM_ETH_USDC_ERC20
EVM_BSC_BNB
EVM_BSC_USDT_BEP20
EVM_BSC_USDC_BEP20
EVM_Polygon_POL
EVM_Polygon_USDT_ERC20
EVM_Polygon_USDC_ERC20
```

实际可用币种以你的 `EVMChains.json` 为准。链的 `Enable=false`、代币未配置或币种字符串拼写不一致时，创建订单会失败。更多字段说明见 [`EVMChains.json` 配置](EVMChains.md)。

> 注意：同名代币在不同链上不是同一种收款网络。例如 `USDT_TRC20` 与 `EVM_BSC_USDT_BEP20` 不能互换。必须向用户明确展示链，并确认收款地址支持对应网络。
