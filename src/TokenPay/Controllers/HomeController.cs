using FreeSql;
using HDWallet.Tron;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using SkiaSharp.QrCode.Image;
using System.Diagnostics;
using System.Reflection;
using TokenPay.Domains;
using TokenPay.Extensions;
using TokenPay.Models;

namespace TokenPay.Controllers
{
    [Route("{action}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly IBaseRepository<TokenOrders> _repository;
        private readonly IBaseRepository<TokenRate> _rateRepository;
        private readonly IBaseRepository<Tokens> _tokenRepository;
        private readonly IConfiguration _configuration;
        private int DecimalsUSDT => _configuration.GetValue("Decimals:USDT", 4);
        private int DecimalsTRX => _configuration.GetValue("Decimals:TRX", 2);
        private int GetDecimals(Currency currency)
        {
            var decimals = currency switch
            {
                Currency.TRX => DecimalsTRX,
                Currency.USDT_TRC20 => DecimalsUSDT,
                _ => DecimalsUSDT,
            };
            return decimals;
        }
        private decimal RateUSDT => _configuration.GetValue("Rate:USDT", 0m);
        private decimal RateTRX => _configuration.GetValue("Rate:TRX", 0m);
        private decimal GetRate(Currency currency)
        {
            var value = currency switch
            {
                Currency.TRX => RateTRX,
                Currency.USDT_TRC20 => RateUSDT,
                _ => 0m,
            };
            return value;
        }
        public HomeController(IBaseRepository<TokenOrders> repository,
            IBaseRepository<TokenRate> rateRepository,
            IBaseRepository<Tokens> tokenRepository,
            IConfiguration configuration)
        {
            this._repository = repository;
            this._rateRepository = rateRepository;
            this._tokenRepository = tokenRepository;
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
            if (!VerifySignature(model))
            {
                return Json(new ReturnData
                {
                    Message = "签名验证失败！"
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
            var hasOrder = await _repository.Where(x => x.OutOrderId == model.OutOrderId && x.Currency == model.Currency).FirstAsync();
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
            var (UseTokenAdress, _) = await CreateTronWallet(model.OrderUserKey);
            var rate = GetRate(model.Currency);
            if (rate <= 0)
            {
                rate = await _rateRepository.Where(x => x.Currency == model.Currency && x.FiatCurrency == FiatCurrency.CNY).FirstAsync(x => x.Rate);
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
        /// <param name="OrderUserKey"></param>
        /// <returns></returns>
        /// <exception cref="TokenPayException"></exception>
        private async Task<(string, string)> CreateTronWallet(string OrderUserKey)
        {
            if (string.IsNullOrEmpty(OrderUserKey))
            {
                throw new TokenPayException("动态地址需传递用户标识！");
            }

            var token = await _tokenRepository.Where(x => x.Id == OrderUserKey).FirstAsync();
            if (token == null)
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var rawPrivateKey = ecKey.GetPrivateKeyAsBytes();
                var hex = Convert.ToHexString(rawPrivateKey);
                var tronWallet = new TronWallet(hex);
                var Address = tronWallet.Address;
                token = new Tokens
                {
                    Id = OrderUserKey,
                    Address = Address,
                    Key = hex
                };
                await _tokenRepository.InsertAsync(token);
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
            var TRON = _configuration.GetSection("TRON-Address").Get<string[]>() ?? new string[0];

            var CurrentAdress = model.Currency switch
            {
                Currency.USDT_TRC20 => TRON,
                Currency.TRX => TRON,
                _ => TRON
            };
            if (CurrentAdress.Length == 0)
            {
                throw new TokenPayException("未配置收款地址！");
            }
            var rate = GetRate(model.Currency);
            if (rate <= 0)
            {
                rate = await _rateRepository.Where(x => x.Currency == model.Currency && x.FiatCurrency == FiatCurrency.CNY).FirstAsync(x => x.Rate);
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
            var qrCode = new QrCode(qrcode, new Vector2Slim(256, 256), SKEncodedImageFormat.Png);
            qrCode.GenerateImage(stream);
            return stream.ToArray();
        }
    }
}
