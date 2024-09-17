#币种填写说明

为了动态支持更多ETH系列的币种，现在系统内没有固定币种，都依据配置文件而来

波场币种：`TRX`、`USDT_TRC20`

ETH系区块链的币种依据`EVMChains.json`配置文件而来

原生币格式为`EVM_[ChainNameEN]_[BaseCoin]` 

ERC20代币格式为：`EVM_[ChainNameEN]_[Erc20.Name]_[ERC20Name]`

如：
> ETH的原生币为：`EVM_ETH_ETH`  
> ETH的USDT代币为：`EVM_ETH_USDT_ERC20`

> BSC的原生币为：`EVM_BSC_BNB`  
> BSC的USDT代币为：`EVM_BSC_USDT_BEP20`

以此类推。

如果开启`EVMChains.json`中的所有区块链后，TokenPay默认支持的币种如下：
```
['TRX', 'USDT_TRC20', 'EVM_ETH_ETH', 'EVM_ETH_USDT_ERC20', 'EVM_ETH_USDC_ERC20', 'EVM_BSC_BNB', 'EVM_BSC_USDT_BEP20', 'EVM_BSC_USDC_BEP20', 'EVM_Polygon_POL', 'EVM_Polygon_USDT_ERC20', 'EVM_Polygon_USDC_ERC20']
```