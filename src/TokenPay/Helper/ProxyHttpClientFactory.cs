using Flurl.Http.Configuration;
using System.Net;

namespace TokenPay.Helper
{
    public class ProxyHttpClientFactory : DelegatingHandler
    {

        public ProxyHttpClientFactory(string address) : base(CreateMessageHandler(address))
        {
        }
        public static HttpMessageHandler CreateMessageHandler(string address)
        {
            var uri = new Uri(address);
            var userinfo = uri.UserInfo.Split(":");
            if (address.ToLower().StartsWith("http"))
            {
                var webProxy = new WebProxy(Host: uri.Host, Port: uri.Port)
                {
                    Credentials = string.IsNullOrEmpty(uri.UserInfo) ? null : new NetworkCredential(userinfo[0], userinfo[1])
                };

                return new HttpClientHandler
                {
                    Proxy = webProxy,
                    UseProxy = true
                };

            }
            else if (address.ToLower().StartsWith("socks"))
            {
                var proxy = new WebProxy($"{uri.Scheme}://{uri.Authority}")
                {
                    Credentials = string.IsNullOrEmpty(uri.UserInfo) ? null : new NetworkCredential(userinfo[0], userinfo[1])
                };
                return new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };
            }
            else
            {
                return new HttpClientHandler
                {
                    Proxy = new WebProxy(address),
                    UseProxy = true
                };
            }
        }
    }
}
