# 手动运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件，为二进制文件`TokenPay`增加可执行权限
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 3. 启动该项目，建议使用`docker`或`supervisor`守护进程