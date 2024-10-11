# 手动运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件
### 3. 为二进制文件`TokenPay`增加可执行权限
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 4. 启动该项目，建议使用`docker`、`PM2`、`supervisor`等守护进程，防止程序异常退出后没有自动重启
### 5. nginx添加一个TokenPay的网站，配置反向代理 `http://127.0.0.1:8080`
