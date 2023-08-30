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

        public static string ToHexString(this string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            string hexString = Convert.ToHexString(bytes);
            return hexString;
        }
        public static string Base58ToHex(this string value)
        {
            return Convert.ToHexString(NokitaKaze.Base58Check.Base58CheckEncoding.Decode(value));
        }
        public static byte[] FromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
        public static string HexToeBase58(this string value)
        {
            var bytes = value.FromHexString();
            return NokitaKaze.Base58Check.Base58CheckEncoding.Encode(bytes);
        }
        public static string HexToString(this string value)
        {
            return Encoding.UTF8.GetString(Convert.FromHexString(value));
        }
    }
}
