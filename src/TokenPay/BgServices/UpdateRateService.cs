using Flurl;
using Flurl.Http;
using FreeSql;
using Nethereum.Signer;
using Org.BouncyCastle.Asn1.X509;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

namespace TokenPay.BgServices
{
    public class UpdateRateService : BaseScheduledService
    {
        const string baseUrl = "https://www.okx.com";
        const string User_Agent = "TokenPay/1.0 Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36";
        private readonly IConfiguration _configuration;
        private readonly List<EVMChain> _chain;
        private readonly IFreeSql freeSql;
        private readonly ILogger<UpdateRateService> _logger;
        private FiatCurrency BaseCurrency => Enum.Parse<FiatCurrency>(_configuration.GetValue("BaseCurrency", "CNY")!);
        public UpdateRateService(
            IConfiguration configuration,
            List<EVMChain> chain,
            IFreeSql freeSql,
            ILogger<UpdateRateService> logger) : base("更新汇率", TimeSpan.FromSeconds(3600), logger)
        {
            this._configuration = configuration;
            this._chain = chain;
            this.freeSql = freeSql;
            this._logger = logger;
        }

        private List<string> GetActiveCurrency()
        {
            var list = new List<string>()
            {
                "TRX","USDT_TRC20"
            };
            foreach (var chain in _chain)
            {
                if (chain == null || !chain.Enable || chain.ERC20 == null) continue;
                list.Add($"EVM_{chain.ChainNameEN}_{chain.BaseCoin}");
                foreach (var erc20 in chain.ERC20)
                {
                    list.Add($"EVM_{chain.ChainNameEN}_{erc20.Name}_{chain.ERC20Name}");
                }
            }
            return list;
        }
        protected override async Task ExecuteAsync(DateTime RunTime, CancellationToken stoppingToken)
        {
            var baseCurrencyList = new List<string>();
            var erc20Names = _chain.Select(x => x.ERC20Name).ToArray();
            foreach (var _item in GetActiveCurrency())
            {
                var item = _item;
                foreach (var name in erc20Names)
                {
                    item = item.Replace(name, "");
                }
                var currency = item.Replace("TRC20", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
                var rate = _configuration.GetValue($"Rate:{currency}", 0m);
                if (rate > 0) continue;//无需更新汇率
                baseCurrencyList.Add(currency);
            }
            baseCurrencyList = baseCurrencyList.Distinct().ToList();
            if (baseCurrencyList.Count == 0)
            {
                _logger.LogInformation("没有需要更新汇率的币种");
            }
            _logger.LogInformation("------------------{tips}------------------", "开始更新汇率");
            var _repository = freeSql.GetRepository<TokenRate>();
            var list = new List<TokenRate>();

            foreach (var item in baseCurrencyList)
            {
                var side = "buy";
                try
                {
                    var result = await baseUrl
                        .WithTimeout(5)
                        .WithHeaders(new { User_Agent })
                        .AppendPathSegment("/v3/c2c/otc-ticker/quotedPrice")
                        .SetQueryParams(new
                        {
                            side = side,
                            quoteCurrency = BaseCurrency.ToString(),
                            baseCurrency = item,
                        })
                        .GetJsonAsync<Root>();
                    if (result.code == 0)
                    {
                        list.Add(new TokenRate
                        {
                            Id = $"{item}_{BaseCurrency}",
                            Currency = item,
                            FiatCurrency = BaseCurrency,
                            LastUpdateTime = DateTime.Now,
                            Rate = result.data.First(x => x.bestOption).price,
                        });
                    }
                    else
                    {
                        _logger.LogWarning("{item} 汇率获取失败！错误信息：{msg}", item, result.msg ?? result.error_message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("{item} 汇率获取失败！错误信息：{msg}", item, e?.InnerException?.Message + "; " + e?.Message);
                }
            }


            foreach (var item in list)
            {
                var RateMove = _configuration.GetValue($"RateMove:{item.Id}", 0m);
                RateMove = RateMove.ToRound(2);//保留2位小数
                if (RateMove != 0)
                {
                    item.Rate += RateMove;
                }
                _logger.LogInformation("更新汇率，{a}=>{b} = {c}", item.Currency, item.FiatCurrency, $"{item.Rate}{(RateMove != 0 ? $" ({RateMove:+0.##;-0.##;0})" : "")}");
                await _repository.InsertOrUpdateAsync(item);
            }
            _logger.LogInformation("------------------{tips}------------------", "结束更新汇率");
        }
    }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
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

#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
}
