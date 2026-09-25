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

英文支付页是独立文件：

```text
Views/Home/Pay.en.cshtml
```

修改一个语言页面不会自动修改其他语言页面。

## 多语言页面

页面通过文件名后缀区分语言：

| 文件名 | 语言 | 何时使用 |
| --- | --- | --- |
| `Pay.cshtml`（无后缀） | 中文 | 中文访客（`zh`、`zh-CN`、`zh-TW` 等） |
| `Pay.en.cshtml` | 英文 | 英文访客、语言未注册的访客，以及缺少对应语言页面的访客 |
| `Pay.ru.cshtml` 等 | 其他语言 | 仅当该语言已加入 `ExtraLanguages` 配置 |

查找顺序以俄语访客为例：`Pay.ru.cshtml` → `Pay.en.cshtml` → `Pay.cshtml`。即缺少对应语言的页面时显示英文页面；只有英文页面也不存在时，才使用不带后缀的页面。

程序默认只注册中文和英文，其他语言的访客一律显示英文页面。增加其他语言（以俄语 `ru` 为例）的步骤：

1. 以源码中的**中文页面**（不带后缀的 `.cshtml`）为模板，复制到运行目录的 `Views` 下相同位置，文件名加上 `.ru` 后缀，然后把页面中的中文翻译为俄语：

   | 页面 | 模板（中文页面） | 新建文件 |
   | --- | --- | --- |
   | 默认支付页 | `Views/Home/Pay.cshtml` | `Views/Home/Pay.ru.cshtml` |
   | 主题支付页（以 `v1-cyber-dark` 为例） | `Views/Home/v1-cyber-dark/Pay.cshtml` | `Views/Home/v1-cyber-dark/Pay.ru.cshtml` |
   | 订单过期页（所有主题共用） | `Views/Home/OrderExpired.cshtml` | `Views/Home/OrderExpired.ru.cshtml` |

   翻译时除了页面正文，还要注意以下容易遗漏的位置：

   - 页面标题 `ViewData["Title"] = "支付页";`
   - 页面语言标记 `<html lang="...">`，改为对应语言，如 `<html lang="ru">`。主题页面写在页面顶部；默认支付页和订单过期页写在布局页中，见下文 [页面写法](#页面写法引用布局页或重写整个页面)
   - `<script>` 中显示给访客的文字，例如倒计时单位（天、时、分、秒）、复制成功提示和 `alert` 弹窗。默认支付页的复制成功弹窗不在页面中，而是写在所有语言共用的 `wwwroot/js/site.js` 里，见下文

2. 在 `appsettings.json` 中添加该语言，详见 [配置说明](appsettings.md#支付页语言)：

   ```json
   "ExtraLanguages": [ "ru" ]
   ```

3. 重启 TokenPay，启动日志中的“支付页语言”一行应包含 `ru`。
4. 在支付页链接末尾追加 `&culture=ru`（或将浏览器语言设为俄语）访问，确认显示的是俄语页面。

### 页面写法：引用布局页或重写整个页面

新增的语言页面可以沿用布局页，也可以自己编写完整的 HTML。两种写法的要求不同：

| | 引用布局页 | 重写整个页面 |
| --- | --- | --- |
| 现有页面示例 | 默认支付页 `Pay.cshtml`、订单过期页 `OrderExpired.cshtml` | 各主题支付页，如 `v1-cyber-dark/Pay.cshtml` |
| `Layout` 设置 | 不设置，由 `_ViewStart.cshtml` 自动使用 `_Layout` | 页面开头必须写 `Layout = null;` |
| `<html>`、`<head>` 和 CSS/JS 引用 | 由 `Views/Shared/_Layout.cshtml` 提供 | 页面自己编写 |
| 页面脚本 | 写在 `@section Scripts { }` 中，由布局页输出 | 直接写在页面的 `<script>` 中，**不能**使用 `@section` |

#### 引用布局页

布局页 `Views/Shared/_Layout.cshtml` 由所有语言共用，其中的 `<html lang="en">` 和标题后缀“TokenPay支付中心”不随语言变化。如需修改，不要直接改 `_Layout.cshtml`，否则中文、英文页面也会一起改变。应复制一份带语言后缀的布局页：

```text
Views/Shared/_Layout.ru.cshtml
```

布局页和普通页面一样按访客语言查找：`_Layout.ru.cshtml` → `_Layout.en.cshtml` → `_Layout.cshtml`。没有提供 `_Layout.ru.cshtml` 时，俄语页面继续使用共用的 `_Layout.cshtml`。

按语言查找只对按名称引用的布局生效，即 `Layout = "_Layout"`（`_ViewStart.cshtml` 的默认写法）。如果在页面中写成路径形式，如 `Layout = "~/Views/Shared/_Layout.cshtml"`，则固定使用该文件，不会查找 `_Layout.ru.cshtml`。

布局页引入的 `wwwroot/js/site.js` 负责初始化“复制”按钮，复制成功的弹窗文字“复制成功！复制内容：”也写在其中，所有语言共用。如需翻译，可以复制一份（例如 `wwwroot/js/site.ru.js`）并在 `_Layout.ru.cshtml` 中改为引用它。

#### 重写整个页面

- 页面开头必须设置 `Layout = null;`。否则 `_ViewStart.cshtml` 仍会套用 `_Layout`，页面会出现两层 `<html>`。
- 需要自己编写 `<html lang="ru">`、`<head>`，并引入页面用到的 CSS 和 JS。以默认支付页 `Pay.cshtml` 为模板时，页面还依赖布局页引入的以下内容，需要在自己的页面中引入，引用路径可参照 `_Layout.cshtml`：
  - Bootstrap 样式：页面布局所用的 class，如 `text-center`、`btn`
  - jQuery：页面脚本中的 `$`
  - clipboard.js 和 `wwwroot/js/site.js`：“复制”按钮的功能由 `site.js` 初始化。不引入时，按钮点击后没有任何反应
- **不能使用 `@section Scripts { }`**。没有布局页时，`@section` 中的内容不会输出，也不会报错。倒计时和支付状态轮询（支付完成后自动跳转）会因此静默失效。请把默认支付页 `@section Scripts { }` 中的脚本移到页面底部的 `<script>` 中，放在 jQuery 等依赖之后。

### 多语言页面的其他说明

- 可以只翻译部分页面，例如只提供 `Pay.ru.cshtml`，此时俄语访客的订单过期页仍显示英文。
- 启用主题时，程序先在主题目录中查找页面，主题目录中的 `Pay.en.cshtml` 优先于 `Views/Home/Pay.ru.cshtml`。因此每个启用的主题都需要在自己的目录中提供 `Pay.ru.cshtml`，否则俄语访客看到的是该主题的英文页面。
- 自定义主题目录如果只有不带后缀的 `Pay.cshtml`、没有 `Pay.en.cshtml`，所有语言的访客都会使用这个页面。
- 文件名大小写需与默认页面保持一致（`Pay.ru.cshtml`，而不是 `pay.ru.cshtml`）。Linux 文件系统区分大小写，大小写不一致可能导致页面找不到。
- 支付页的倒计时和过期时间由 `ExpireTimestamp`（UTC 毫秒时间戳）计算，并在浏览器中按访客本地时区显示。自定义页面时请保留这部分脚本，不要改回用 `ToString("yyyy/MM/dd ...")` 输出的日期字符串：服务器会按访客语言格式化日期，泰语会输出佛历年份，印尼语、阿拉伯语的输出浏览器无法解析，倒计时都会显示异常。

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
- 修改页面后至少测试中文、英文支付页（以及通过 `ExtraLanguages` 增加的语言）、订单过期页，以及启用后的后台页面。
