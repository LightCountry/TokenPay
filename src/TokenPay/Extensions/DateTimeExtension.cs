using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenPay.Extensions
{
    public static class DateTimeExtension
    {
        public static DateTime ToDateTime(this long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        public static double ToUnixTimeStamp(this DateTime datetime)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = (datetime.ToUniversalTime() - startTime).TotalMilliseconds;
            return timeStamp;
        }

    }
}
