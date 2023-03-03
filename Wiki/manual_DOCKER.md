# 手动运行

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

### 1. 修改 Wiki/down 下的 build.sh ,设置第一行版本号为最新

### 2. 修改 build.sh 文件权限 755

./build.sh 执行

### 3. 配置 prod/appsettings.json

> `appsettings.json`说明参见：[appsettings.json](appsettings.md)

### 4. 容器命令

启动

```shell
docker compose up -d
```

删除

```shell
docker compose down
```

查看容器日志

```shell
docker compose logs -f
```

### 5. caddy2或者 nginx，配置反向代理 `http://127.0.0.1:5001`