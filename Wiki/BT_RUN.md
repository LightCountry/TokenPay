# 宝塔运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件，为二进制文件`TokenPay`增加可执行权限
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 3. `宝塔应用管理器`或`Supervisor管理器`添加应用
> 应用名称：TokenPay
> 应用环境：无 （`Supervisor管理器`无此项）
> 启动文件：./TokenPay
> 执行目录：你解压文件的目录
> 如有其他选项保持默认
