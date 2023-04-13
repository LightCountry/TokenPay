# 宝塔运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 2. 重命名`EVMChains.Example.json`为`EVMChains.json`，并配置需要支持的区块链。
# 只需修改配置中的`Enable`和`ApiKey`，其他配置项请勿修改！！！
>配置文件中已添加`ETH`、`BSC`、`Polygon`三条区块链，如需其他ETH系的区块链可自由拓展。每条区块链配置都带有一个`Enable`参数，表示是否启用此区块链，默认的三条区块链的此项配置都为`false`，请将需要启用的区块链`Enable`参数更改为`true`
> `EVMChains.json`说明参见：[EVMChains.json](EVMChains.md)
### 3. 为二进制文件`TokenPay`增加可执行权限
### 5. `宝塔应用管理器`或`Supervisor管理器`添加应用
> 应用名称：TokenPay  
> 运行身份：root
> 应用环境：无 （`Supervisor管理器`无此项）  
> 执行目录：/xxx (你解压文件的目录)  
> 启动文件：/xxx/TokenPay  
> 如有其他选项保持默认  
### 5. 添加一个纯静态网站，配置反向代理 `http://127.0.0.1:5000`

**如启动失败，可尝试将整个`TokenPay目录`循环设置`777`权限，再重新尝试启动**
