# 使用 Docker 运行 TokenPay

无需下载源码、编译项目或自行构建镜像。直接使用微软官方 ASP.NET Core 8.0 Runtime 镜像，并把 TokenPay 的 `framework-dependent` 发布包挂载到容器中即可。

> **资产安全警告：** `TokenPay.db` 可能保存动态钱包私钥。必须持久化并备份整个运行目录，同时严格限制目录权限。不要让多个 TokenPay 容器同时读写同一个数据库。

## 1. 选择发布包

从 Release 下载与服务器 CPU 架构匹配的包：

- 常见 Intel/AMD 64 位服务器：`linux-x64-framework-dependent`
- ARM64 服务器：`linux-arm64-framework-dependent`

名称带 `framework-dependent` 的包不携带 .NET 运行时，容器中的 `mcr.microsoft.com/dotnet/aspnet:8.0` 会提供所需运行时。不要在 Linux 容器中使用 `win-x64-framework-dependent` 包。

## 2. 准备运行目录

以下示例在当前目录创建 `publish`，请将下载的压缩包完整解压到该目录：

```bash
mkdir -p publish
unzip TokenPay-*-linux-x64-framework-dependent.zip -d publish
chmod u+x publish/TokenPay
```

ARM64 服务器请下载并解压 `linux-arm64-framework-dependent` 包。

确认 `publish/appsettings.json` 和 `publish/EVMChains.json` 配置正确。配置方法见[主配置说明](appsettings.md)和 [EVM 配置说明](EVMChains.md)。

## 3. 使用 Docker 命令直接启动（推荐新手）

在 `publish` 目录的上一级执行：

```bash
docker run -d \
  --name TokenPay \
  --restart always \
  --entrypoint ./TokenPay \
  --workdir /app \
  -e TZ=Asia/Shanghai \
  -e LC_ALL=zh_CN.UTF-8 \
  -e LANG=zh_CN.UTF-8 \
  -p 127.0.0.1:8080:8080 \
  -v "$(pwd)/publish:/app" \
  mcr.microsoft.com/dotnet/aspnet:8.0
```

命令执行成功后，可以查看容器状态和日志：

```bash
docker ps --filter name=TokenPay
docker logs -f TokenPay
```

ASP.NET Core 8.0 容器默认监听 `8080`，因此无需设置 `ASPNETCORE_URLS`。`127.0.0.1:8080:8080` 只允许宿主机本地访问，反向代理目标应填写 `http://127.0.0.1:8080`。

默认不启用额外的 GC 内存节约策略。如果服务器内存较小，可在 `docker run` 命令中自行增加 `-e DOTNET_GCConserveMemory=5`。有效范围为 `0`～`9`：`0` 表示使用默认策略，数值越大越倾向于节省内存，但回收会更频繁，可能降低高负载时的性能。修改后需要重新创建容器才能生效。

需要停止或重新启动时执行：

```bash
docker stop TokenPay
docker start TokenPay
```

## 4. 使用 Docker Compose 启动（可选，docker熟手推荐）

在 `publish` 同级目录创建 `compose.yml`：

```yaml
services:
  tokenpay:
    image: mcr.microsoft.com/dotnet/aspnet:8.0
    container_name: TokenPay
    restart: always
    entrypoint: ["./TokenPay"]
    working_dir: /app
    environment:
      TZ: Asia/Shanghai
      LC_ALL: zh_CN.UTF-8
      LANG: zh_CN.UTF-8
    ports:
      - "127.0.0.1:8080:8080"
    volumes:
      - ./publish:/app
```

目录结构应类似：

```text
tokenpay/
├── compose.yml
└── publish/
    ├── TokenPay
    ├── appsettings.json
    ├── EVMChains.json
    └── ...
```

启动容器：

```bash
docker compose up -d
```

查看状态和日志：

```bash
docker compose ps
docker logs -f TokenPay
```

生产环境应使用 Nginx、Caddy 或其他反向代理提供 HTTPS，不建议把容器的 HTTP 端口直接暴露到公网。

## 5. 更新

1. 停止并删除旧容器。直接运行方式执行 `docker stop TokenPay && docker rm TokenPay`；Compose 方式执行 `docker compose down`。
2. 备份整个 `publish` 目录，尤其是 `TokenPay.db`、`appsettings.json` 和 `EVMChains.json`。
3. 下载相同架构的新版 `framework-dependent` 包。
4. 保留数据库和真实配置，替换其他程序文件，并重新执行 `chmod u+x publish/TokenPay`。
5. 重新执行第 3 节的 `docker run` 命令，或执行 `docker compose up -d`，然后查看日志并完成小额支付测试。

若需要固定补丁版本，可把镜像标签由 `8.0` 改为经过验证的具体版本；使用 `8.0` 时，重新拉取镜像可以获得该产品线后续的运行时更新：

```bash
docker compose pull
docker compose up -d
```
