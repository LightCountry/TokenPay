# 其他系统对接`TokenPay`
> 也可参考仓库内现有的独角数卡插件对接

## 1. 创建`TokenPay`订单  

URL: `/CreateOrder`  

类型： `POST`  

接受 `application/x-www-url-formencoded` 和 `application/json`

| 字段 | 类型 | 必填 | 说明 |
| ---- | ---- | ---- | ---- |
| OutOrderId  | string | 是 | 外部订单号 |
| OrderUserKey  | string | 是 | 支付用户标识，建议传用户联系方式或用户ID等`能识别用户身份的字符串`。使用动态地址时，会根据此字段关联收款地址，传递用户ID等，可以保证系统后续还会为此用户分配此地址。如需要每个订单一个新地址，可向此字段传递`外部订单号`。 |
| ActualAmount | decimal | 是 | 订单实际支付的人民币金额，`保留两位小数` |
| Currency | Enum,支持`USDT_TRC20`、`TRX`等 | 是 | 加密货币的币种，直接以`原样字符串`传递即可 |
| NotifyUrl | string? | 否 | 异步通知URL |
| RedirectUrl | string? | 否 | 订单支付或过期后跳转的URL |


## 2. `TokenPay`的异步回调参数
>如接口返回的状态码不是`200`，或者响应的内容不是字符串`ok`，视为回调失败。  
>回调失败后将会在一分钟后重试，总共重试两次。  

URL: `创建订单`接口传递的`NotifyUrl`字段内的URL  

类型： `POST` `application/json`  

| 字段 | 类型 |说明 |
| ---- | ---- | ---- |
| Id | string | TokenPay内部订单号 |
| BlockTransactionId | string | 区块哈希 |
| OutOrderId | string | 外部订单号，调用 `创建订单` 接口时传递的外部订单号 |
| OrderUserKey | string | 支付用户标识，调用 `创建订单` 接口时传递的支付用户标识 |
| PayTime | string | 支付时间，示例：`2022-09-15 16:00:00` |
| Currency | string | 币种，`USDT_TRC20`、`TRX`等 |
| Amount | string | 订单金额，此金额为人民币转换为`Currency`字段指定的币种后的金额 |
| ActualAmount | string | 订单金额，此金额为人民币金额 |
| FromAddress | string | 付款地址 |
| ToAddress | string | 收款地址 |
| Signature | string | 签名，`接口请务必验证此参数！！！`将除`Signature`字段外的所有字段，按照字母升序排序。按顺序拼接为`key1=value1&key2=value2`形式，然后在末尾拼接上`异步通知密钥`，将此字符串计算MD5，即为签名。 |

### ①示例POST参数
```json
{
    "ActualAmount": "15",
    "Amount": "34.91",
    "BlockTransactionId": "375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5",
    "Currency": "TRX",
    "FromAddress": "TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa",
    "Id": "63234df7-55bf-93fc-0010-67be493c0c27",
    "OrderUserKey": null,
    "OutOrderId": "E6COE6FGZMO5AXSK",
    "PayTime": "2022-09-15 16:08:39",
    "ToAddress": "TLUF41C386CMU1Wc8pTSCE4QaiZ2xkhTCb"
}
```
### ②排序后拼接
`ActualAmount=15&Amount=34.91&BlockTransactionId=375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5&Currency=TRX&FromAddress=TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa&Id=63234df7-55bf-93fc-0010-67be493c0c27&OrderUserKey=&OutOrderId=E6COE6FGZMO5AXSK&PayTime=2022-09-15 16:08:39&ToAddress=TLUF41C386CMU1Wc8pTSCE4QaiZ2xkhTCb`

异步通知密钥为：`666`

拼接密钥后
`ActualAmount=15&Amount=34.91&BlockTransactionId=375859c36dc5f5d227b10912b5ec70d36dd34446028064956cb60cdbb74432f5&Currency=TRX&FromAddress=TYYjzt6AWhe9hAg9DrhiYXEWKDksyohgQa&Id=63234df7-55bf-93fc-0010-67be493c0c27&OrderUserKey=&OutOrderId=E6COE6FGZMO5AXSK&PayTime=2022-09-15 16:08:39&ToAddress=TLUF41C386CMU1Wc8pTSCE4QaiZ2xkhTCb666`

### ③计算MD5
`9426a6596b6bdf9a8684cf77572e1b94`

对比POST中的`Signature`是否与此值一致