#`appsettings.json`说明

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

**为保证安全性，务必修改`ApiToken`**

```
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DB": "Data Source=|DataDirectory|TokenPay.db; Pooling=true;Min Pool Size=1"
  },
  "TRON-PRO-API-KEY": "xxxxxx-xxxx-xxxx-xxxxxxxxxxxx", // 避免接口请求频繁被限制，此处申请 https://www.trongrid.io/dashboard/keys
  "BaseCurrency": "CNY", //默认货币，支持 CNY、USD、EUR、GBP、AUD、HKD、TWD、SGD
  "Rate": { //汇率 设置0将使用自动汇率
    "USDT": 0,
    "TRX": 0,
    "ETH": 0,
    "USDC": 0
  },
  "ExpireTime": 1800, //单位秒
  "UseDynamicAddress": false, //是否使用动态地址，设为false时，与EPUSDT表现类似；设为true时，为每个下单用户分配单独的收款地址
  "Address": { // UseDynamicAddress设为false时在此配置TRON收款地址，EVM可以替代所有ETH系列的收款地址，支持单独配置某条链的收款地址
    "TRON": [ "Txxxx1" ],
    "EVM": [ "0x111" ]
  },
  "OnlyConfirmed": true, //默认仅查询已确认的数据，如果想要回调更快，可以设置为false
  "NotifyTimeOut": 3, //异步通知超时时间
  "ApiToken": "666666", //异步通知密钥，请务必修改此密钥为随机字符串，脸滚键盘即可
  "WebSiteUrl": "http://token-pay.xxxxx.com", //配置服务器外网域名
  "Telegram": {
    "AdminUserId": 12345678, // 你的账号ID，如不知道ID，可给https://t.me/EShopFakaBot 发送 /me 获取用户ID
    "BotToken": "1234567890:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" //从https://t.me/BotFather 创建机器人时，会给你BotToken
  }
}

```
# FAQ
### 如果配置了多条ETH系的区块链，如何独立指定某条区块链的收款地址？
> 为这条区块链配置Address参数即可，下方示例中新增了BSC的收款地址

```
"Address": {
    "TRON": [ "Txxxx1" ],
    "EVM": [ "0x00001" ],
    "BSC": [ "0x00002" ]
  },
```