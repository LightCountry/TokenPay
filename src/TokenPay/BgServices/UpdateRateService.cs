using Flurl;
using Flurl.Http;
using FreeSql;
using TokenPay.Domains;
using TokenPay.Helper;

namespace TokenPay.BgServices
{
    public class UpdateRateService : BaseScheduledService
    {
        const string baseUrl = "https://www.okx.com";
        const string User_Agent = "TokenPay/1.0 Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36";
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateRateService> _logger;
        private readonly FlurlClient client;
        private FiatCurrency BaseCurrency => Enum.Parse<FiatCurrency>(_configuration.GetValue("BaseCurrency", "CNY"));
        public UpdateRateService(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<UpdateRateService> logger) : base("更新汇率", TimeSpan.FromSeconds(3600), logger)
        {
            this._configuration = configuration;
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            var WebProxy = configuration.GetValue<string>("WebProxy");
            client = new FlurlClient();
            client.Settings.Timeout = TimeSpan.FromSeconds(5);
            if (!string.IsNullOrEmpty(WebProxy))
            {
                client.Settings.HttpClientFactory = new ProxyHttpClientFactory(WebProxy);
            }

        }

        protected override async Task ExecuteAsync()
        {
            var USDT = _configuration.GetValue("Rate:USDT", 0m);
            var TRX = _configuration.GetValue("Rate:TRX", 0m);
            var ETH = _configuration.GetValue("Rate:ETH", 0m);
            var USDC = _configuration.GetValue("Rate:USDC", 0m);
            if (USDT > 0 && TRX > 0 && ETH > 0 && USDC > 0)
            {
                // 无需更新汇率
                return;
            }
            _logger.LogInformation("------------------{tips}------------------", "开始更新汇率");
            using IServiceScope scope = _serviceProvider.CreateScope();
            var _repository = scope.ServiceProvider.GetRequiredService<IBaseRepository<TokenRate>>();

            var list = new List<TokenRate>();
            var side = "buy";

            if (USDT <= 0)
            {
                try
                {
                    var result = await baseUrl
                        .WithClient(client)
                        .WithHeaders(new { User_Agent = User_Agent })
                        .AppendPathSegment("/v3/c2c/otc-ticker/quotedPrice")
                        .SetQueryParams(new
                        {
                            side = side,
                            quoteCurrency = BaseCurrency.ToString(),
                            baseCurrency = "USDT",
                        })
                        .GetJsonAsync<Root>();
                    if (result.code == 0)
                    {
                        list.Add(new TokenRate
                        {
                            Id = $"{Currency.USDT_TRC20}_{BaseCurrency}",
                            Currency = Currency.USDT_TRC20,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        }); 
                        list.Add(new TokenRate
                        {
                            Id = $"{Currency.USDT_ERC20}_{BaseCurrency}",
                            Currency = Currency.USDT_ERC20,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        });
                    }
                    else
                    {
                        _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", result.msg ?? result.error_message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", e?.InnerException?.Message + "; " + e?.Message);
                }
            }
            if (TRX <= 0)
            {
                try
                {
                    var result = await baseUrl
                        .WithClient(client)
                        .WithHeaders(new { User_Agent = User_Agent })
                        .AppendPathSegment("/v3/c2c/otc-ticker/quotedPrice")
                        .SetQueryParams(new
                        {
                            side = side,
                            quoteCurrency = BaseCurrency.ToString(),
                            baseCurrency = "TRX",
                        })
                        .GetJsonAsync<Root>();
                    if (result.code == 0)
                    {
                        list.Add(new TokenRate
                        {
                            Id = $"{Currency.TRX}_{BaseCurrency}",
                            Currency = Currency.TRX,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        });
                    }
                    else
                    {
                        _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", result.msg ?? result.error_message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", e?.InnerException?.Message + "; " + e?.Message);
                }
            }

            if (ETH <= 0)
            {
                try
                {
                    var result = await baseUrl
                        .WithClient(client)
                        .WithHeaders(new { User_Agent = User_Agent })
                        .AppendPathSegment("/v3/c2c/otc-ticker/quotedPrice")
                        .SetQueryParams(new
                        {
                            side = side,
                            quoteCurrency = BaseCurrency.ToString(),
                            baseCurrency = "ETH",
                        })
                        .GetJsonAsync<Root>();
                    if (result.code == 0)
                    {
                        list.Add(new TokenRate
                        {
                            Id = $"{Currency.ETH}_{BaseCurrency}",
                            Currency = Currency.ETH,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        });
                    }
                    else
                    {
                        _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", result.msg ?? result.error_message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", e?.InnerException?.Message + "; " + e?.Message);
                }
            }
            if (USDC <= 0)
            {
                try
                {
                    var result = await baseUrl
                        .WithClient(client)
                        .WithHeaders(new { User_Agent = User_Agent })
                        .AppendPathSegment("/v3/c2c/otc-ticker/quotedPrice")
                        .SetQueryParams(new
                        {
                            side = side,
                            quoteCurrency = BaseCurrency.ToString(),
                            baseCurrency = "USDC",
                        })
                        .GetJsonAsync<Root>();
                    if (result.code == 0)
                    {
                        list.Add(new TokenRate
                        {
                            Id = $"{Currency.USDC_ERC20}_{BaseCurrency}",
                            Currency = Currency.USDC_ERC20,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        });
                    }
                    else
                    {
                        _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", result.msg ?? result.error_message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("USDT 汇率获取失败！错误信息：{msg}", e?.InnerException?.Message + "; " + e?.Message);
                }
            }
            foreach (var item in list)
            {
                _logger.LogInformation("更新汇率，{a}=>{b} = {c}", item.Currency, item.FiatCurrency, item.Rate);
                await _repository.InsertOrUpdateAsync(item);
            }
            _logger.LogInformation("------------------{tips}------------------", "结束更新汇率");
        }
    }

    class Datum
    {
        public bool bestOption { get; set; }
        public string payment { get; set; }
        public decimal price { get; set; }
    }

    class Root
    {
        public int code { get; set; }
        public List<Datum> data { get; set; }
        public string detailMsg { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public string msg { get; set; }
    }

    enum OkxSide
    {
        Buy,
        Sell
    }

}
