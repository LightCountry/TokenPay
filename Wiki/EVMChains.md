#`EVMChains.json`说明

### 1. ETH系区块链配置参数说明
| 字段 | 类型 | 说明 |
| ---- | ----  | ---- |
| Enable  | bool | 是否启用此区块链，支持的值: `true`、`false` |
| ChainName  | string | 区块链名称 |
| ChainNameEN  | string | 区块链英文名称 |
| BaseCoin  | string | 基本币名称 |
| Decimals  | int | 基本币精度，如：18表示小数点后有18位，6表示小数点后有6位 |
| ApiHost  | string | api请求地址 |
| ApiKey  | string | api请求的授权key |
| ERC20Name  | string | ERC20代币名称，比如币安的就叫`BEP-20`，而不叫`ERC20`，所以特意加了这个 |
| ERC20  | object[] | 要支持的代币 |


### 2. ETH系区块链配置代币说明
| 字段 | 类型 | 说明 |
| ---- | ----  | ---- |
| Name  | string | 代币名称，不管是哪条链的USDT，都是写USDT。USDC同理，此参数会用于向OKX服务器查询币价，`填写了错误的名称会导致无法自动获取币价` |
| ContractAddress  | string | 合约地址，错误的合约地址将导致收款无法回调 |