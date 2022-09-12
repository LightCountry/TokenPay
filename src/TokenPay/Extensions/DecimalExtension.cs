namespace TokenPay.Extensions
{
    public static class DecimalExtension
    {
        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals">小数位数</param>
        /// <returns></returns>
        public static decimal ToRound(this decimal value, int decimals = 4)
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
        public static decimal ToRound(this double value, int decimals = 4)
        {
            return Math.Round((decimal)value, decimals, MidpointRounding.AwayFromZero);
        }
        public static decimal ToRound(this float value, int decimals = 4)
        {
            return Math.Round((decimal)value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
