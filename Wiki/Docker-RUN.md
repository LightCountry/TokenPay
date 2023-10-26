# Docker 运行

**务必挂载并保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

## 编译Docker镜像

1. 克隆仓库代码
```sh
git clone https://github.com/LightCountry/TokenPay.git
```
2. 进入源码目录
```sh
cd TokenPay/src
```
4. 执行编译镜像
```sh
docker build -t token-pay .
```
上面的`token-pay`名字可以修改为其他名字,但是运行的时候要统一名字
5. 查看编译后的镜像
```sh
docker images
```
## Docker容器运行

### main 主网 正式运营请选择主网

```
docker run -d -v /yourdir/appsettings.json:/app/appsettings.json -v /yourdir/TokenPay.db:/app/TokenPay.db --name token-pay token-pay
```

+ `yourdir`为你自己存放配置文件和数据库文件的文件夹路径

### shasta 测试网

```
docker run -d -e ASPNETCORE_ENVIRONMENT="Development" -v /yourdir/appsettings.json:/app/appsettings.json -v /yourdir/TokenPay.db:/app/TokenPay.db --name token-pay token-pay
```

+ `yourdir`为你自己存放配置文件和数据库文件的文件夹路径
+ `-e ASPNETCORE_ENVIRONMENT="Development"` 指定环境为开发环境
