using TokenPay.Domains;

namespace TokenPay.Extensions
{
    public static class EnumExtension
    {
        public static string ToCurrency(this Currency currency)
        {
            var currencyString = currency switch
            {
                Currency.USDT_ERC20 => "USDT",
                Currency.USDT_TRC20 => "USDT",
                Currency.USDC_ERC20 => "USDC",
                _ => currency.ToDescriptionOrString()
            };
            return currencyString;
        }
        public static string ToBlockchainName(this Currency currency)
        {
            var value = currency switch
            {
                Currency.USDT_ERC20 => "以太坊（ETH）",
                Currency.USDC_ERC20 => "以太坊（ETH）",
                Currency.USDT_TRC20 => "波场（TRON）",
                Currency.TRX => "波场（TRON）",
                _ => currency.ToDescriptionOrString()
            };
            return value;
        }
    }
}
