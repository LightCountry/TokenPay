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