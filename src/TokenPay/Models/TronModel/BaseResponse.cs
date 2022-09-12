using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TokenPay.Models.TronModel
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class Meta
    {
        [JsonProperty("at")]
        public long At { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }
    }

    public class BaseResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
