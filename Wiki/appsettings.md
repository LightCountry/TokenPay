#`appsettings.json`说明

**务必保存好`TokenPay.db`文件，此文件内保存了系统生成的收款地址和私钥，一旦丢失，你将损失所收取的款项**

**为保证安全性，务必修改`NotifyKey`**

```json
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
  "TRON-PRO-API-KEY": "xxxxxx-xxxx-xxxx-xxxxxxxxxxxx", // 此处申请 https://www.trongrid.io/dashboard/keys
  "Rate": { //汇率 设置0将使用自动汇率
    "USDT": 0,
    "TRX": 0
  },
  "ExpireTime": 3600, //单位秒
  "UseDynamicAddress": true, //是否使用动态地址，设为false时，与EPUSDT表现类似
  //"TRON-Address": [ "Txxxxxx1", "Txxxxxx2" ], // UseDynamicAddress设为false时在此配置收款地址
  "OnlyConfirmed": true, //默认仅查询已确认的数据，如果想要回调更快，可以设置为false
  "NotifyTimeOut": 3, //异步通知超时时间
  "NotifyKey": "666666" //异步通知密钥，请务必修改此密钥为随机字符串，脸滚键盘即可！！！
}
```