using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokenPay.Models.Transfer;

namespace TokenPay.Models
{
    public class ResultData
    {
        [JsonProperty("result")]
        public bool Result { get; set; }
    }


    public class BalanceOfModel
    {
        [JsonProperty("result")]
        public ResultData Result { get; set; } = null!;

        [JsonProperty("energy_used")]
        public int EnergyUsed { get; set; }

        [JsonProperty("constant_result")]
        public List<string> ConstantResult { get; set; } = null!;

        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; } = null!;
    }
}
