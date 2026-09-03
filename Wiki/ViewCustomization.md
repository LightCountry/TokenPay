# 页面内置与运行时覆盖

TokenPay 发布时会把仓库中的默认 Razor 页面编译进程序集，因此发布目录中没有 `Views` 文件夹也可以正常显示支付页、错误页和后台页面。

程序启动时检测到内容根目录中存在 `Views` 文件夹，才会启用 Razor 运行时编译。如果需要修改页面，请先放置 `Views` 文件夹再启动 TokenPay；同路径的物理 `.cshtml` 会覆盖程序内置的默认页面，未放置的页面继续使用内置版本。

## 工作方式

| 运行目录中的文件 | 实际使用的页面 |
| --- | --- |
| 不存在对应 `.cshtml` | 发布时编译进程序集的默认页面 |
| 存在对应 `.cshtml` | 运行目录中的物理页面，由运行时编译器编译 |

该机制只覆盖 Razor 页面，不会修改数据库或控制器业务逻辑。

## 覆盖页面

最稳妥的方式是将源码中的整个目录复制到程序目录：

```text
src/TokenPay/Views
```

部署后的结构示例：

```text
TokenPay/
├── TokenPay.exe 或 TokenPay.dll
├── appsettings.json
├── EVMChains.json
├── wwwroot/
└── Views/
    ├── _ViewImports.cshtml
    ├── _ViewStart.cshtml
    ├── Home/
    ├── Admin/
    └── Shared/
```

只覆盖个别页面时，也必须保持原有相对路径，并建议同时保留其依赖的 `_ViewImports.cshtml`、`_ViewStart.cshtml` 和布局文件。例如覆盖中文支付页：

```text
Views/Home/Pay.cshtml
```

英文和俄语支付页是独立文件：

```text
Views/Home/Pay.en.cshtml
Views/Home/Pay.ru.cshtml
```

修改一个语言页面不会自动修改其他语言页面。

## 生效方式

运行时编译会监视物理 Razor 文件，通常保存后下一次请求即可看到变化。如果部署平台的文件监视不可用、使用了原子替换文件的发布方式，或页面仍被旧进程缓存，请重启 TokenPay。

删除外部覆盖文件后，程序会重新使用内置默认页面；必要时重启服务清除已经编译的运行时页面缓存。

## 单文件发布

默认页面会随程序集进入单文件发布产物，不需要单独复制 `Views`。外部覆盖目录仍应放在程序的内容根目录。建议从程序所在目录启动 TokenPay，确保 `appsettings.json`、`EVMChains.json`、`wwwroot` 和 `Views` 使用同一个内容根目录。

## 注意事项

- 发布包必须保留运行时编译所需的依赖文件，不要在不了解影响时手工删除 `.deps.json` 或相关程序集。
- 页面文件能够执行 Razor/C# 代码，只允许可信管理员写入 `Views`，不要提供网页上传 `.cshtml` 的功能。
- 更新 TokenPay 前备份自定义 `Views`。新版模型字段或布局可能变化，更新后需要对照新版默认页面合并修改。
- 页面样式和 JavaScript 位于 `wwwroot`。只修改 `.cshtml` 不会自动覆盖对应的 CSS 或 JS；如需定制静态资源，应保持原路径并做好备份。
- 修改页面后至少测试中文、英文、俄语支付页以及启用后的后台页面。
