# 宝塔运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件
### 3. 为二进制文件`TokenPay`增加可执行权限
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 4. `宝塔应用管理器`或`Supervisor管理器`添加应用
> 应用名称：TokenPay  
> 运行身份：root
> 应用环境：无 （`Supervisor管理器`无此项）  
> 执行目录：/xxx (你解压文件的目录)  
> 启动文件：/xxx/TokenPay  
> 如有其他选项保持默认  

**如启动失败，可尝试将整个`TokenPay目录`循环设置`777`权限，再重新尝试启动**