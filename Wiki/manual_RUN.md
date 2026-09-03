# 手动运行 TokenPay

本文适用于直接运行 Release 压缩包。生产环境建议使用 systemd、Supervisor、宝塔进程守护或其他服务管理器，并在前方配置 HTTPS 反向代理。

> **重要：** `TokenPay.db` 可能保存动态钱包私钥。请备份数据库并限制文件权限，不要将程序目录设置为全员可读写。

## 1. 准备文件

1. 从 Release 下载与操作系统及 CPU 架构匹配的包。
2. 解压到独立目录，例如 Linux 的 `/opt/tokenpay`。
3. 将 `appsettings.Example.json` 复制为 `appsettings.json`。
4. 如需 EVM 链，将 `EVMChains.Example.json` 复制为 `EVMChains.json`。
5. 按 [主配置说明](appsettings.md) 和 [EVM 配置说明](EVMChains.md) 修改配置。

Release 同时提供两类发布包：

- `linux-x64`、`linux-arm64`、`win-x64`：自包含版本，已经携带 .NET 运行时，无需另外安装。
- 名称带 `framework-dependent`：不携带 .NET 运行时，文件更小，但运行机器必须预先安装 **.NET 8 ASP.NET Core Runtime**，且操作系统和 CPU 架构必须与包名一致。

使用 `framework-dependent` 包前，可执行以下命令检查运行时：

```bash
dotnet --list-runtimes
```

输出中必须存在 `Microsoft.AspNetCore.App 8.0.x`。只有 `Microsoft.NETCore.App` 不足以运行 ASP.NET Core Web 应用。

### 安装 .NET 8 ASP.NET Core Runtime

Ubuntu/Debian 系统：

```bash
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0
```

RHEL/CentOS Stream 系统：

```bash
sudo dnf install -y aspnetcore-runtime-8.0
```

Windows（使用管理员终端）：

```powershell
winget install Microsoft.DotNet.AspNetCore.8
```

如果 Linux 提示找不到 `aspnetcore-runtime-8.0`，说明当前系统的软件源尚未提供该软件包。请按照微软对应发行版的 [.NET Linux 安装说明](https://learn.microsoft.com/dotnet/core/install/linux) 添加官方软件源后再安装；Windows 也可以从 [.NET 8 下载页面](https://dotnet.microsoft.com/download/dotnet/8.0) 手动下载 **ASP.NET Core Runtime** 安装程序。

安装完成后重新打开终端，再次执行 `dotnet --list-runtimes`。看到 `Microsoft.AspNetCore.App 8.0.x` 后，即可运行对应平台和架构的 `framework-dependent` 包。

## 2. 启动

Linux/macOS 自包含版本：

```bash
chmod u+x TokenPay
./TokenPay --urls=http://127.0.0.1:8080
```

Linux `framework-dependent` 版本的启动命令相同：

```bash
chmod u+x TokenPay
./TokenPay --urls=http://127.0.0.1:8080
```

.NET DLL 版本：

```bash
dotnet TokenPay.dll --urls=http://127.0.0.1:8080
```

Windows：

```powershell
.\TokenPay.exe --urls=http://127.0.0.1:8080
```

示例统一使用 `8080` 端口。如果启动日志提示端口已被占用，请将命令中 `--urls` 的 `8080` 改为其他未占用端口，并把反向代理目标同步改为相同端口。生产环境只监听 `127.0.0.1`，不要直接把未加密的 HTTP 端口暴露到公网。

### 可选：低内存运行模式，内存充足可忽略本节

默认不启用额外的 GC 内存节约策略，小内存服务器可以通过环境变量自行设置：

```bash
DOTNET_GCConserveMemory=5 ./TokenPay --urls=http://127.0.0.1:8080
```

PowerShell：

```powershell
$env:DOTNET_GCConserveMemory = "5"
.\TokenPay.exe --urls=http://127.0.0.1:8080
```

有效范围是 `0`～`9`，数值越大越倾向于节省内存，但 GC 更频繁，可能降低高负载时的性能。建议从 `5` 开始测试。该设置只在进程启动时读取，修改后必须重启 TokenPay。

## 3. 配置反向代理

将公网 HTTPS 域名反向代理到：

```text
http://127.0.0.1:8080
```

同时把 `WebSiteUrl` 设置为该公网 HTTPS 地址。上传大小、超时和代理头可以保持常规 ASP.NET Core Web 应用设置。

## 4. 配置进程守护

进程守护至少应包含：

- 工作目录为 TokenPay 解压目录。
- 启动命令与手动测试成功的命令一致。
- 异常退出后自动重启。
- 使用专门的低权限系统用户运行，不要使用 root/Administrator。
- 该用户对程序文件只需读取和执行权限，对数据库、日志目录需要写权限。

## 5. 验证

1. 查看启动日志，确认 SQLite、Telegram 和链 API 初始化没有报错。
2. 打开 TokenPay 首页或支付接口。
3. 分别创建所启用币种的小额订单。
4. 验证付款识别、商户回调和 Telegram 通知。
5. 配置了后台时，按 [后台管理说明](admin.md) 验证 HTTPS 登录。

## 更新与备份

更新前备份：

- `TokenPay.db`
- `appsettings.json`
- `EVMChains.json`

停止旧进程后替换程序文件，保留上述数据和配置，再启动并检查日志。不要同时运行两个实例读写同一个 SQLite 数据库。
