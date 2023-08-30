using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TokenPay.Models
{
    public class GetAccountResourceModel
    {
        [JsonProperty("freeNetUsed")]
        public long FreeNetUsed { get; set; }

        [JsonProperty("freeNetLimit")]
        public long FreeNetLimit { get; set; }

        [JsonProperty("NetUsed")]
        public long NetUsed { get; set; }

        [JsonProperty("NetLimit")]
        public long NetLimit { get; set; }

        [JsonProperty("TotalNetLimit")]
        public long TotalNetLimit { get; set; }

        [JsonProperty("TotalNetWeight")]
        public long TotalNetWeight { get; set; }

        [JsonProperty("tronPowerUsed")]
        public long TronPowerUsed { get; set; }

        [JsonProperty("tronPowerLimit")]
        public long TronPowerLimit { get; set; }

        [JsonProperty("EnergyUsed")]
        public long EnergyUsed { get; set; }

        [JsonProperty("EnergyLimit")]
        public long EnergyLimit { get; set; }

        [JsonProperty("TotalEnergyLimit")]
        public long TotalEnergyLimit { get; set; }

        [JsonProperty("TotalEnergyWeight")]
        public long TotalEnergyWeight { get; set; }
    }
}
