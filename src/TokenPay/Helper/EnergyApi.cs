using Flurl;
using Flurl.Http;
using Flurl.Http.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace TokenPay.Helper
{
    public class EnergyApi
    {
        const string baseUrl = "https://energy-api.trxd.win";

        private readonly FlurlClient client;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public EnergyApi(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            client = new FlurlClient(baseUrl);
            client.WithSettings(fs => fs.JsonSerializer = new NewtonsoftJsonSerializer(null));
            client.BeforeCall(c =>
            {
                c.Request.WithHeader("Lang", "zh-CN");
                _logger.LogInformation("发起请求\nURL：{url}\n参数：{body}", c.Request.Url, c.RequestBody);
            });
            client.AfterCall(async c =>
            {
                _logger.LogInformation("收到响应\nURL：{url}\n响应：{@body}", c.Request.Url, c.Response != null ? await c.Response.GetStringAsync() : null);
            });
        }
        /// <summary>
        /// 价格估算
        /// </summary>
        /// <returns></returns>
        public async Task<EnergyResponse<OrderPriceData>> OrderPrice(int resource_value, int rent_duration = 1, string rent_time_unit = "h")
        {
            var result = await client
                .Request("OrderPrice")
                .PostJsonAsync(new
                {
                    resource_value,
                    rent_duration,
                    rent_time_unit
                })
                .ReceiveJson<EnergyResponse<OrderPriceData>>();
            return result;
        }
        /// <summary>
        /// 查询订单
        /// </summary>
        /// <returns></returns>
        public async Task<EnergyResponse<OrderData>> OrderQuery(string order_no)
        {
            var result = await client.Request($"OrderQuery/{order_no}")
                .GetJsonAsync<EnergyResponse<OrderData>>();
            return result;
        }
        /// <summary>
        /// 下单
        /// </summary>
        /// <returns></returns>
        public async Task<EnergyResponse<OrderData>> CreateOrder(CreateOrderModel model)
        {
            var result = await client.Request("CreateOrder")
                .PostJsonAsync(model)
                .ReceiveJson<EnergyResponse<OrderData>>();
            return result;
        }

    }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class CreateOrderModel
    {
        /// <summary>
        /// 支付地址
        /// </summary>
        [JsonProperty("pay_address")]
        public string PayAddress { get; set; }
        /// <summary>
        /// 支付金额
        /// </summary>
        [JsonProperty("pay_amount")]
        public decimal PayAmount { get; set; }
        /// <summary>
        /// 接收地址
        /// </summary>
        [JsonProperty("receive_address")]
        public string ReceiveAddress { get; set; }
        /// <summary>
        /// 时长
        /// </summary>
        [JsonProperty("rent_duration")]
        public int RentDuration { get; set; }
        /// <summary>
        /// 资源数量
        /// </summary>
        [JsonProperty("resource_value")]
        public int ResourceValue { get; set; }
        /// <summary>
        /// 时间单位
        /// </summary>
        [JsonProperty("rent_time_unit")]
        public string RentTimeUnit { get; set; } = "h";

        [JsonProperty("signed_txn")]
        public object SignedTxn { get; set; }
    }

    public class EnergyResponse<TData>
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("data")]
        public TData Data { get; set; }
    }

    public class OrderPrice
    {
        [JsonProperty("resource_value")]
        public int ResourceValue { get; set; }

        [JsonProperty("pay_amount")]
        public decimal PayAmount { get; set; }

        [JsonProperty("service_amount")]
        public decimal ServiceAmount { get; set; }

        [JsonProperty("rent_duration")]
        public int RentDuration { get; set; }

        [JsonProperty("rent_time_unit")]
        public string RentTimeUnit { get; set; }

        [JsonProperty("price_in_sun")]
        public decimal PriceInSun { get; set; }
    }

    public class OrderData : OrderPrice
    {
        [JsonProperty("order_no")]
        public string OrderNo { get; set; }

        [JsonProperty("order_num")]
        public int OrderNum { get; set; }

        [JsonProperty("order_type")]
        public int OrderType { get; set; }

        [JsonProperty("resource_type")]
        public int ResourceType { get; set; }

        [JsonProperty("receive_address")]
        public string ReceiveAddress { get; set; }

        [JsonProperty("price_in_sun")]
        public new int PriceInSun { get; set; }

        [JsonProperty("min_amount")]
        public int MinAmount { get; set; }

        [JsonProperty("min_payout")]
        public int MinPayout { get; set; }

        [JsonProperty("min_freeze")]
        public int MinFreeze { get; set; }

        [JsonProperty("max_amount")]
        public int MaxAmount { get; set; }

        [JsonProperty("max_payout")]
        public int MaxPayout { get; set; }

        [JsonProperty("max_freeze")]
        public int MaxFreeze { get; set; }

        [JsonProperty("freeze_time")]
        public int FreezeTime { get; set; }

        [JsonProperty("unfreeze_time")]
        public int UnfreezeTime { get; set; }

        [JsonProperty("expire_time")]
        public int ExpireTime { get; set; }

        [JsonProperty("create_time")]
        public int CreateTime { get; set; }

        [JsonProperty("resource_value")]
        public new int ResourceValue { get; set; }

        [JsonProperty("resource_split_value")]
        public int ResourceSplitValue { get; set; }

        [JsonProperty("frozen_resource_value")]
        public int FrozenResourceValue { get; set; }

        [JsonProperty("rent_duration")]
        public new int RentDuration { get; set; }

        [JsonProperty("rent_time_unit")]
        public new string RentTimeUnit { get; set; }

        [JsonProperty("rent_expire_time")]
        public int RentExpireTime { get; set; }

        [JsonProperty("frozen_balance")]
        public int FrozenBalance { get; set; }

        [JsonProperty("frozen_tx_id")]
        public string FrozenTxId { get; set; }

        [JsonProperty("unfreeze_tx_id")]
        public string UnfreezeTxId { get; set; }

        [JsonProperty("settle_amount")]
        public decimal SettleAmount { get; set; }

        [JsonProperty("settle_address")]
        public string SettleAddress { get; set; }

        [JsonProperty("settle_time")]
        public int SettleTime { get; set; }

        [JsonProperty("pay_action")]
        public int PayAction { get; set; }

        [JsonProperty("pay_address")]
        public string PayAddress { get; set; }

        [JsonProperty("pay_time")]
        public int PayTime { get; set; }

        [JsonProperty("pay_tx_id")]
        public string PayTxId { get; set; }

        [JsonProperty("pay_symbol")]
        public string PaySymbol { get; set; }

        [JsonProperty("pay_amount")]
        public new decimal PayAmount { get; set; }

        [JsonProperty("pay_status")]
        public int PayStatus { get; set; }

        [JsonProperty("refund_amount")]
        public decimal RefundAmount { get; set; }

        [JsonProperty("is_split")]
        public int IsSplit { get; set; }

        [JsonProperty("cancel_tx_id")]
        public string CancelTxId { get; set; }

        [JsonProperty("refund_tx_id")]
        public string RefundTxId { get; set; }

        [JsonProperty("status")]
        public FeeeOrderStatus Status { get; set; }
        /// <summary>
        /// 子订单
        /// </summary>
        [JsonProperty("sub_order")]
        public List<OrderData> SubOrder { get; set; }
    }
    public enum FeeeOrderStatus
    {
        未支付 = 1,
        已关闭支付 = 2,
        已支付待验证 = 3,
        已支付 = 4,
        已质押待验证 = 5,
        已质押 = 6,
        质押失败 = 7,
        已解押待验证 = 8,
        已解押 = 9,
        解押失败 = 1,
        取消待处理 = 11,
        已取消 = 12,
        待退款 = 13,
        已退款 = 14,
        质押进行中 = 15,
        暂时锁定 = 16,
        推迟回收 = 17,
    }

    public class OrderPriceData
    {
        [JsonProperty("pay_address")]
        public string PayAddress { get; set; }

        [JsonProperty("pay_amount")]
        public decimal Price { get; set; }
    }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
