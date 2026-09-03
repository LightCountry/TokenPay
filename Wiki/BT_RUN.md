# 使用宝塔面板运行 TokenPay

本文说明使用宝塔面板的进程管理功能运行 TokenPay，并通过网站反向代理提供 HTTPS 访问。

> **资产安全警告：** `TokenPay.db` 可能保存动态钱包私钥。请定期加密备份，限制目录权限，不要使用递归 `777`，也不要让网站或其他系统用户读取数据库。

## 1. 上传并配置

1. 下载与服务器 CPU 架构匹配的 Linux Release 包。普通用户建议选择不带 `framework-dependent` 的自包含包；如果服务器已经安装 .NET 8 ASP.NET Core Runtime，也可选择体积更小的 `linux-x64-framework-dependent` 或 `linux-arm64-framework-dependent` 包。
2. 解压到独立目录，例如 `/opt/tokenpay`。
3. 将 `appsettings.Example.json` 复制为 `appsettings.json`，参考 [主配置说明](appsettings.md) 完成配置。
4. 将 `EVMChains.Example.json` 复制为 `EVMChains.json`，参考 [EVM 配置说明](EVMChains.md) 启用需要的链并填写 API Key。
5. 为程序所有者增加执行权限：

```bash
chmod u+x /opt/tokenpay/TokenPay
```

不要修改不理解的链参数，尤其是 Chain ID、精度和合约地址。

如果使用 `framework-dependent` 包，请先执行 `dotnet --list-runtimes`，确认输出中存在 `Microsoft.AspNetCore.App 8.0.x`。该发布包仍可通过上面的 `./TokenPay` 命令直接启动，无需改成 DLL 启动方式。

## 2. 添加守护进程

在“进程守护管理器”或“Supervisor 管理器”中添加：

| 项目 | 示例 |
| --- | --- |
| 名称 | `TokenPay` |
| 运行用户 | 专门创建的低权限用户，不建议 root |
| 工作目录 | `/opt/tokenpay` |
| 启动命令 | `/opt/tokenpay/TokenPay --urls=http://127.0.0.1:8080` |
| 自动重启 | 开启 |

如果使用没有平台启动程序的 DLL 包，启动命令改为：

```bash
dotnet /opt/tokenpay/TokenPay.dll --urls=http://127.0.0.1:8080
```

示例使用 `8080` 端口。先启动一次并查看日志；如果该端口已被占用，请将启动命令中的 `8080` 改为其他未占用端口，后续反向代理也必须使用相同端口。

## 3. 添加网站与 HTTPS

1. 在宝塔中添加站点并绑定域名。
2. 申请并启用有效的 TLS 证书，强制使用 HTTPS。
3. 添加反向代理，目标 URL 设置为 `http://127.0.0.1:8080`；如果启动时更换了端口，此处也要使用更换后的端口。
4. 将 `appsettings.json` 中的 `WebSiteUrl` 设置为实际 HTTPS 域名。
5. 如开启后台，保持 `Admin:RequireHttps=true`，详见 [后台管理说明](admin.md)。

TokenPay 本身只监听本机地址即可，无需在防火墙开放 8080 端口。

## 4. 文件权限建议

- 程序文件：运行用户可读、可执行。
- `TokenPay.db`、配置和日志：仅运行用户及管理员可访问。
- 不要使用 `chmod -R 777`。权限问题应通过正确的文件所有者和最小权限解决。
- 不要把真实配置或数据库放在网站静态根目录中。

## 5. 验证与更新

完成后创建小额订单，检查支付页、链上识别、异步回调和 Telegram 通知。更新程序前先停止守护进程并备份 `TokenPay.db`、`appsettings.json`、`EVMChains.json`，替换程序文件后再启动。

更多通用检查见 [手动运行说明](manual_RUN.md)。
