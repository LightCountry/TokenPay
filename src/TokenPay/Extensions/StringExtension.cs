using System.Security.Cryptography;
using System.Text;

namespace TokenPay.Extensions
{
    public static class StringExtension
    {
        public static string ToMD5(this string value)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
