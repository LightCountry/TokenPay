using FreeSql;
using HDWallet.Tron;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Bcpg;
using SkiaSharp;
using SkiaSharp.QrCode.Image;
using System.Diagnostics;
using System.Reflection;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Models;
using TokenPay.Models.EthModel;

namespace TokenPay.Controllers
{
    [Route("{action}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly IBaseRepository<TokenOrders> _repository;
        private readonly IBaseRepository<TokenRate> _rateRepository;
        private readonly IBaseRepository<Tokens> _tokenRepository;
        private readonly List<EVMChain> _chain;
        private readonly IConfiguration _configuration;
        private FiatCurrency BaseCurrency => Enum.Parse<FiatCurrency>(_configuration.GetValue("BaseCurrency", "CNY"));
        private int GetDecimals(string currency)
        {
            var decimals = currency switch
            {
                "TRX" => _configuration.GetValue("Decimals:TRX", 2),
                "EVM_ETH" => _configuration.GetValue("Decimals:ETH", 5),
                _ => _configuration.GetValue($"Decimals:{currency}", 4)
            };

            return decimals;
        }
        private List<string> GetErc20Name()
        {
            var list = new List<string>();
            foreach (var item in _chain)
            {
                list.Add(item.ERC20Name);
            }
            list = list.Distinct().ToList();
            return list;
        }

        private decimal GetRate(string currency)
        {
            var erc20Names = GetErc20Name();
            foreach (var item in erc20Names)
            {
                currency = currency.Replace(item, "");
            }
            var _currency = currency.Replace("TRC20", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
            var value = _currency switch
            {
                "TRX" => _configuration.GetValue("Rate:TRX", 0m),
                "ETH" => _configuration.GetValue("Rate:ETH", 0m),
                "USDT" => _configuration.GetValue("Rate:USDT", 0m),
                "USDC" => _configuration.GetValue("Rate:USDC", 0m),
                _ => _configuration.GetValue($"Rate:{_currency}", 0m)
            };
            return value;
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
        public HomeController(IBaseRepository<TokenOrders> repository,
            IBaseRepository<TokenRate> rateRepository,
            IBaseRepository<Tokens> tokenRepository,
            List<EVMChain> chain,
            IConfiguration configuration)
        {
            this._repository = repository;
            this._rateRepository = rateRepository;
            this._tokenRepository = tokenRepository;
            this._chain = chain;
            this._configuration = configuration;
        }
        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Pay(Guid Id)
        {
            var order = await _repository.Where(x => x.Id == Id).FirstAsync();
            if (order == null)
            {
                return View(order);
            }
            ViewData["QrCode"] = Convert.ToBase64String(CreateQrCode(order.ToAddress));
            var ExpireTime = _configuration.GetValue("ExpireTime", 10 * 60);
            if (DateTime.Now > order.CreateTime.AddSeconds(ExpireTime) || order.Status == OrderStatus.Expired)
            {
                return View("OrderExpired", order);
            }
            ViewData["ExpireTime"] = order.CreateTime.AddSeconds(ExpireTime).ToString("yyyy-MM-dd HH:mm:ss");
            return View(order);
        }
        [Route("/{action}/{id}")]
        public async Task<IActionResult> Check(Guid Id)
        {
            var order = await _repository.Where(x => x.Id == Id).FirstAsync();
            if (order == null)
            {
                return Content(OrderStatus.Pending.ToString());
            }
            return Content(order.Status.ToString());
        }
        private bool VerifySignature(CreateOrderViewModel model)
        {
            if (model == null) return false;
            var dic = new SortedDictionary<string, string?>();
            PropertyInfo[] properties = model.GetType().GetProperties();
            if (properties.Length <= 0) { return false; }
            foreach (PropertyInfo item in properties)
            {
                string name = item.Name;
                string? value = item.GetValue(model, null)?.ToString();
                dic.Add(name, value);
            }
            if (dic.TryGetValue("Signature", out var Signature))
            {
                dic.Remove("Signature");
                var SignatureStr = string.Join("&", dic.Select(x => $"{x.Key}={x.Value}"));
                var ApiToken = _configuration.GetValue<string>("ApiToken");
                SignatureStr += ApiToken;
                var md5 = SignatureStr.ToMD5();
                return Signature == md5;
            }
            return false;
        }
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/" + nameof(CreateOrder))]
        [ApiExplorerSettings(IgnoreApi = false)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                string messages = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));

                return Json(new ReturnData
                {
                    Message = messages
                });
            }
#if !DEBUG
            if (!VerifySignature(model))
            {
                return Json(new ReturnData
                {
                    Message = "签名验证失败！"
                });
            }
#endif
            if (!GetActiveCurrency().Contains(model.Currency))
            {
                return Json(new ReturnData
                {
                    Message = $"不支持的币种【{model.Currency}】！"
                });
            }
            if (model.ActualAmount <= 0)
            {
                return Json(new ReturnData
                {
                    Message = "金额有误！"
                });
            }
            //订单号已存在
            var hasOrder = await _repository.Where(x => x.OutOrderId == model.OutOrderId && x.Currency == model.Currency)
                .Where(x => x.Status != OrderStatus.Expired)
                .FirstAsync();
            if (hasOrder != null)
            {
                return Json(new ReturnData<string>
                {
                    Success = true,
                    Message = "订单已存在，查询旧订单！",
                    Data = Host + Url.Action(nameof(Pay), new { Id = hasOrder.Id })
                });
            }
            var order = new TokenOrders
            {
                OutOrderId = model.OutOrderId,
                OrderUserKey = model.OrderUserKey,
                Status = OrderStatus.Pending,
                Currency = model.Currency,
                ActualAmount = model.ActualAmount,
                NotifyUrl = model.NotifyUrl,
                RedirectUrl = model.RedirectUrl
            };
            var UseDynamicAddress = _configuration.GetValue("UseDynamicAddress", true);
            try
            {
                if (UseDynamicAddress)
                {
                    var (Address, Amount) = await GetUseTokenDynamicAdress(model);
                    order.ToAddress = Address;
                    order.Amount = Amount;
                }
                else
                {
                    var (Address, Amount) = await GetUseTokenStaticAdress(model);
                    order.ToAddress = Address;
                    order.Amount = Amount;
                }
            }
            catch (TokenPayException e)
            {
                return Json(new ReturnData
                {
                    Message = e.Message
                });
            }
            if (order.Amount == 0)
            {
                return Json(new ReturnData
                {
                    Message = "此订单金额过低！"
                });
            }
            await _repository.InsertAsync(order);
            return Json(new ReturnData<string>
            {
                Success = true,
                Message = "创建订单成功！",
                Data = Host + Url.Action(nameof(Pay), new { Id = order.Id })
            });
        }
        private string Host
        {
            get
            {
                var host = _configuration.GetValue<string>("WebSiteUrl");
                if (string.IsNullOrEmpty(host))
                {
                    host = $"{Request.Scheme}://{Request.Host}";
                }
                return host;
            }
        }
        /// <summary>
        /// 动态地址
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<(string, decimal)> GetUseTokenDynamicAdress(CreateOrderViewModel model)
        {
            var (UseTokenAdress, _) = await CreateAddress(model.OrderUserKey, model.Currency);
            var rate = GetRate(model.Currency);
            if (rate <= 0)
            {
                var Currency = model.Currency.ToCurrency(_chain);
                rate = await _rateRepository.Where(x => x.Currency == Currency && x.FiatCurrency == BaseCurrency).FirstAsync(x => x.Rate);
            }
            if (rate <= 0)
            {
                throw new TokenPayException("汇率有误！");
            }
            var Amount = (model.ActualAmount / rate).ToRound(GetDecimals(model.Currency));
            return (UseTokenAdress, Amount);
        }
        /// <summary>
        /// 根据唯一Id获取一个地址
        /// </summary>
        /// <exception cref="TokenPayException"></exception>
        private async Task<(string, string)> CreateAddress(string OrderUserKey, string currency)
        {
            if (string.IsNullOrEmpty(OrderUserKey))
            {
                throw new TokenPayException("动态地址需传递用户标识！");
            }
            var BaseCurrency = TokenCurrency.TRX;
            // 币种以EVM开头判定为EVM
            if (currency.StartsWith("EVM"))
            {
                BaseCurrency = TokenCurrency.EVM;
            }
            var TokenId = $"{BaseCurrency}_{OrderUserKey}";
            var token = await _tokenRepository.Where(x => x.Id == TokenId && x.Currency == BaseCurrency).FirstAsync();
            if (token == null)
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var rawPrivateKey = ecKey.GetPrivateKeyAsBytes();
                var hex = Convert.ToHexString(rawPrivateKey);
                if (BaseCurrency == TokenCurrency.EVM)
                {
                    var Address = ecKey.GetPublicAddress();
                    token = new Tokens
                    {
                        Id = TokenId,
                        Address = Address,
                        Key = hex,
                        Currency = TokenCurrency.EVM
                    };
                    await _tokenRepository.InsertAsync(token);
                }
                else
                {
                    var tronWallet = new TronWallet(hex);
                    var Address = tronWallet.Address;
                    token = new Tokens
                    {
                        Id = TokenId,
                        Address = Address,
                        Key = hex,
                        Currency = TokenCurrency.TRX
                    };
                    await _tokenRepository.InsertAsync(token);
                }
            }
            return (token.Address, token.Key);
        }
        /// <summary>
        /// 静态地址
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="TokenPayException"></exception>
        private async Task<(string, decimal)> GetUseTokenStaticAdress(CreateOrderViewModel model)
        {
            var TRON = _configuration.GetSection("Address:TRON").Get<string[]>() ?? new string[0];
            var EVM = _configuration.GetSection("Address:EVM").Get<string[]>() ?? new string[0];
            var CurrencyAddress = _configuration.GetSection($"Address:{model.Currency.Replace("EVM", "").Split("_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First()}").Get<string[]>() ?? new string[0];

            var CurrentAdress = CurrencyAddress;

            if (CurrentAdress.Length == 0 && (model.Currency == "TRX" || model.Currency.EndsWith("TRC20")))
            {
                CurrentAdress = TRON;
            }
            if (CurrentAdress.Length == 0 && model.Currency.StartsWith("EVM"))
            {
                CurrentAdress = EVM;
            }
            if (CurrentAdress.Length == 0)
            {
                throw new TokenPayException("未配置收款地址！");
            }
            var rate = GetRate(model.Currency);
            if (rate <= 0)
            {
                var Currency = model.Currency.ToCurrency(_chain);
                rate = await _rateRepository.Where(x => x.Currency == Currency && x.FiatCurrency == BaseCurrency).FirstAsync(x => x.Rate);
            }
            if (rate <= 0)
            {
                throw new TokenPayException("汇率有误！");
            }
            var Amount = (model.ActualAmount / rate).ToRound(GetDecimals(model.Currency));
            //随机排序所有收款地址
            CurrentAdress = CurrentAdress.OrderBy(x => Guid.NewGuid()).ToArray();
            var UseTokenAdress = string.Empty;
            foreach (var token in CurrentAdress)
            {
                //判断是否存在此金额、此地址、此币种的待付款
                var has = await _repository
                    .Where(x => x.ToAddress == token)
                    //.Where(x => x.ActualAmount == order.ActualAmount) //原始金额
                    .Where(x => x.Currency == model.Currency)//虚拟币币种
                    .Where(x => x.Amount == Amount) //实际支付的虚拟币金额
                    .Where(x => x.Status == OrderStatus.Pending) //代支付
                    .AnyAsync();
                if (!has)
                {
                    UseTokenAdress = token;
                    break;
                }
            }
            //所有地址都存在此金额
            if (string.IsNullOrEmpty(UseTokenAdress))
            {
                var decimals = GetDecimals(model.Currency);
                var maxLoop = Math.Pow(10, decimals);
                var AddAmount = Convert.ToDecimal(1 / maxLoop);//初始递增量
                for (int i = 0; i < maxLoop; i++)//最多递增100次
                {
                    foreach (var token in CurrentAdress)
                    {
                        //判断是否存在此金额、此地址、此币种的待付款
                        var currentAmount = Amount + AddAmount * (i + 1);
                        var query = _repository
                            .Where(x => x.ToAddress == token)
                            //.Where(x => x.ActualAmount == order.ActualAmount) //原始金额
                            .Where(x => x.Currency == model.Currency)//虚拟币币种
                            .Where(x => x.Amount == currentAmount) //实际支付的虚拟币金额
                            .Where(x => x.Status == OrderStatus.Pending);
                        var has = await query//代支付
                            .AnyAsync();
                        if (!has)
                        {
                            UseTokenAdress = token;
                            Amount = currentAmount;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(UseTokenAdress))
                    {
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(UseTokenAdress))
            {
                throw new TokenPayException("无可用收款地址！");
            }
            return (UseTokenAdress, Amount);
        }
        [Route("/error-development")]
        public IActionResult HandleErrorDevelopment([FromServices] IHostEnvironment hostEnvironment)
        {
            var exceptionHandlerFeature =
                HttpContext.Features.Get<IExceptionHandlerFeature>()!;
            var e = exceptionHandlerFeature.Error;

            if (!hostEnvironment.IsDevelopment())
            {
                return Json(new ReturnData
                {
                    Message = e.Message
                });
            }

            return Json(new ReturnData<object>
            {
                Message = e.Message,
                Data = new
                {
                    title = exceptionHandlerFeature.Error.Message,
                    detail = exceptionHandlerFeature.Error.StackTrace,
                }
            });
        }

        [Route("/error")]
        public IActionResult HandleError() => Problem();
        /// <summary>
        /// 创建二维码
        /// </summary>
        /// <param name="qrcode"></param>
        /// <returns></returns>
        private static byte[] CreateQrCode(string qrcode)
        {
            using var stream = new MemoryStream();
            var qrCode = new QrCode(qrcode, new Vector2Slim(300, 300), SKEncodedImageFormat.Png);
            qrCode.GenerateImage(stream);
            return stream.ToArray();
        }
    }
}
