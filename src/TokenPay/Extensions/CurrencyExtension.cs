using NBitcoin;
using Nethereum.Signer;
using TokenPay.Domains;
using TokenPay.Models.EthModel;

namespace TokenPay.Extensions
{
    public static class CurrencyExtension
    {
        public static string ToCurrency(this string currency, List<EVMChain> chains, bool hasSuffix = false)
        {
            if (hasSuffix)
            {
                if (currency.StartsWith("EVM"))
                {
                    currency = currency.Replace("EVM_", "");
                    var coin = currency.Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                    currency = currency.Replace($"{coin}_", "");
                }
                return currency.Replace($"_", "-");
            }
            var erc20Names = chains.Select(x => x.ERC20Name).ToArray();
            foreach (var item in erc20Names)
            {
                currency = currency.Replace(item, "");
            }
            var str = currency.Replace("TRC20", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();

            return str;
        }
        public static string ToBlockchainName(this string currency, List<EVMChain> chains)
        {
            if (currency == "TRX" || currency.EndsWith("TRC20")) return "波场(TRON)";

            var ChainNameEN = currency.Replace("EVM", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();

            var chain = chains.Where(x => x.ChainNameEN == ChainNameEN).FirstOrDefault();
            if (chain != null)
            {
                return chain.ChainName;
            }

            return $"未知区块链[{currency}]";
        }
        public static string ToBlockchainEnglishName(this string currency, List<EVMChain> chains)
        {
            if (currency == "TRX" || currency.EndsWith("TRC20")) return "TRON";

            var ChainNameEN = currency.Replace("EVM", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
            var chain = chains.Where(x => x.ChainNameEN == ChainNameEN).FirstOrDefault();
            if (chain != null)
            {
                return chain.ChainNameEN;
            }

            return $"Unknown Blockchain[{currency}]";
        }
    }
}
