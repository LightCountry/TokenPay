using Flurl.Http.Configuration;
using System.Net;

namespace TokenPay.Helper
{
    public class ProxyHttpClientFactory : DefaultHttpClientFactory
    {
        private string _address;

        public ProxyHttpClientFactory(string address)
        {
            _address = address;
        }

        public override HttpMessageHandler CreateMessageHandler()
        {
            var uri = new Uri(_address);
            var userinfo = uri.UserInfo.Split(":");
            if (_address.ToLower().StartsWith("http"))
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
            else if (_address.ToLower().StartsWith("socks"))
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
                    Proxy = new WebProxy(_address),
                    UseProxy = true
                };
            }
        }
    }
}
