using Flurl;
using Flurl.Http;
using HDWallet.Core;
using HDWallet.Tron;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer.Crypto;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parameter = Nethereum.ABI.Model.Parameter;
using Org.BouncyCastle.Asn1.Ocsp;
using Signature = HDWallet.Core.Signature;
using TokenPay.Extensions;
using TokenPay.Models;
using TokenPay.Models.Transfer;
using HDWallet.Secp256k1;

namespace TokenPay.Helper
{

    public static partial class QueryTronAction
    {
        public static IConfiguration configuration { get; set; } = null!;
        /// <summary>
        /// 获取USDT余额
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public static async Task<decimal> GetUsdtAmountAsync(string Address)
        {
            var hex = Address.Base58ToHex();
            var encoded = new FunctionCallEncoder().EncodeParameters(new Parameter[] {
                new Parameter("address","who")
                }, new string[] { "0x" + hex.Substring(2, hex.Length - 2) });

            var encodedHex = Convert.ToHexString(encoded);
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request = BaseUrl
                .AppendPathSegment("wallet/triggerconstantcontract")
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request = request.WithHeader("TRON-PRO-API-KEY", apiKey);
            }

            var ContractAddress = configuration.GetValue("ContractAddress", "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t");
            var result = await request.PostJsonAsync(new
            {
                owner_address = Address,
                contract_address = ContractAddress,
                function_selector = "balanceOf(address)",
                parameter = encodedHex,
                visible = true
            }).ReceiveJson<BalanceOfModel>();

            if (result.Result.Result)
            {
                Log.Logger.Information("检查余额：{address}", Address);
                //Log.Logger.Information("金额：{@result}", result);
                var amountAbi = result.ConstantResult.FirstOrDefault();
                if (!string.IsNullOrEmpty(amountAbi))
                {
                    amountAbi = amountAbi.TrimStart('0');
                    if (long.TryParse(amountAbi, NumberStyles.HexNumber, null, out var amount))
                    {
                        return amount / 1_000_000m;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 转TRX
        /// </summary>
        /// <returns></returns>
        public static async Task<(bool, string?)> TransferTrxAsync(this TronWallet wallet0, decimal value, string ToAddress)
        {
            var address = wallet0.Address;
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request1 = BaseUrl
                .WithTimeout(5);
            var request2 = BaseUrl
                .WithTimeout(5);
            var request3 = BaseUrl
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request1 = request1.WithHeader("TRON-PRO-API-KEY", apiKey);
                request2 = request2.WithHeader("TRON-PRO-API-KEY", apiKey);
                request3 = request3.WithHeader("TRON-PRO-API-KEY", apiKey);
            }
            var resultData = request1
                .AppendPathSegment("wallet/createtransaction")
                .PostJsonAsync(new
                {
                    owner_address = address,
                    to_address = ToAddress,
                    amount = (long)(value * 1_000_000),
                    visible = true
                });
            var result = await resultData.ReceiveJson<Models.Transfer.Transaction>();
            if (result.Error != null)
            {
                return (false, result.Error);
            }
            var transaction = result;
            // Sign
            var txId = HDWallet.Core.Helper.FromHexToByteArray(transaction.TxID);
            Signature signature = wallet0.Sign(txId);
            TronSignature tronSignature = new TronSignature(signature);
            var signatureHex = tronSignature.SignatureBytes.ToHexString();
            transaction.Signature.Add(signatureHex);
            //Broadcast
            var req = new
            {
                raw_data = Newtonsoft.Json.JsonConvert.SerializeObject(transaction.RawData),
                txID = transaction.TxID,
                signature = new List<string>(),
                visible = true
            };
            req.signature.Add(transaction.Signature.First());

            var result2Data = request2.AppendPathSegment("wallet/broadcasttransaction").PostJsonAsync(req);
            var result2 = await result2Data.ReceiveJson<BroadcastResult>();

            if (result2.Result)
            {
                var result3Data = request3.AppendPathSegment("wallet/gettransactionbyid");
                var result3 = await result3Data.PostJsonAsync(new
                {
                    value = result2.Txid,
                    visible = true
                }).ReceiveJson<Models.Transaction>();
                if (result3.RawData == null)
                {
                    var count = 0;
                    while (result3.RawData == null && count < 10)
                    {
                        count++;
                        result3 = await result3Data.PostJsonAsync(new
                        {
                            value = result2.Txid,
                            visible = true
                        }).ReceiveJson<Models.Transaction>();
                        Log.Logger.Information("等待交易数据上链，TXID={a}", result2.Txid);
                        await Task.Delay(1000);
                    }
                }
                var success = result3?.Ret?.First()?.ContractRet == "SUCCESS";
                if (!success)
                {
                    return (success, result3?.Ret?.First()?.ContractRet);
                }
            }

            return (result2.Result, result2.Result ? result2.Txid : "广播失败！");
        }
        /// <summary>
        /// 转USDT
        /// </summary>
        /// <returns></returns>
        public static async Task<(bool, string?)> TransferUSDTAsync(this TronWallet wallet0, decimal value, string ToAddress)
        {
            var address = wallet0.Address;
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request1 = BaseUrl
                .WithTimeout(5);
            var request2 = BaseUrl
                .WithTimeout(5);
            var request3 = BaseUrl
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request1 = request1.WithHeader("TRON-PRO-API-KEY", apiKey);
                request2 = request2.WithHeader("TRON-PRO-API-KEY", apiKey);
                request3 = request3.WithHeader("TRON-PRO-API-KEY", apiKey);
            }
            var hex = ToAddress.Base58ToHex();
            var encoded = new FunctionCallEncoder().EncodeParameters(new Parameter[] {
                new Parameter("address","_to"),new Parameter("uint256","_value")
                }, new object[] { "0x" + hex.Substring(2, hex.Length - 2), (ulong)(value * (decimal)Math.Pow(10, 6)) });

            var encodedHex = Convert.ToHexString(encoded);
            var ContractAddress = configuration.GetValue("ContractAddress", "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t");
            var resultData = request1
                .AppendPathSegment("wallet/triggersmartcontract")
                .PostJsonAsync(new
                {
                    owner_address = address,
                    contract_address = ContractAddress,
                    function_selector = "transfer(address,uint256)",
                    fee_limit = 30_000_000,
                    parameter = encodedHex,
                    visible = true
                });

            var result = await resultData.ReceiveJson<TransferUSDTModel>();
            var transaction = result.Transaction;
            if (!result.Result.Result || transaction.Ret != null && transaction.Ret.First()?.Ret != RetCode.Sucess)
            {
                return (false, transaction.Ret.First()?.Ret.ToString());
            }

            // Sign
            var txId = HDWallet.Core.Helper.FromHexToByteArray(transaction.TxID);
            Signature signature = wallet0.Sign(txId);
            TronSignature tronSignature = new TronSignature(signature);
            var signatureHex = tronSignature.SignatureBytes.ToHexString();
            transaction.Signature.Add(signatureHex);
            //Broadcast
            var req = new
            {
                raw_data = Newtonsoft.Json.JsonConvert.SerializeObject(transaction.RawData),
                txID = transaction.TxID,
                signature = new List<string>(),
                visible = true
            };
            req.signature.Add(transaction.Signature.First());

            var result2Data = request2.AppendPathSegment("wallet/broadcasttransaction").PostJsonAsync(req);
            var result2 = await result2Data.ReceiveJson<BroadcastResult>();

            if (result2.Result)
            {
                var result3Data = request3.AppendPathSegment("wallet/gettransactionbyid");
                var result3 = await result3Data.PostJsonAsync(new
                {
                    value = result2.Txid,
                    visible = true
                }).ReceiveJson<Models.Transaction>();
                if (result3.RawData == null)
                {
                    var count = 0;
                    while (result3.RawData == null && count < 10)
                    {
                        count++;
                        result3 = await result3Data.PostJsonAsync(new
                        {
                            value = result2.Txid,
                            visible = true
                        }).ReceiveJson<Models.Transaction>();
                        Log.Logger.Information("等待交易数据上链，TXID={a}", result2.Txid);
                        await Task.Delay(1000);
                    }
                }
                var success = result3?.Ret?.First()?.ContractRet == "SUCCESS";
                if (!success)
                {
                    return (success, result3?.Ret?.First()?.ContractRet);
                }
            }

            return (result2.Result, result2.Result ? result2.Txid : "广播失败！");
        }
        /// <summary>
        /// 获取账户资源
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<GetAccountResourceModel> GetAccountResourceAsync(string address)
        {
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request = BaseUrl
                .AppendPathSegment("wallet/getaccountresource")
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request = request.WithHeader("TRON-PRO-API-KEY", apiKey);
            }

            var result = await request.PostJsonAsync(new
            {
                address,
                visible = true
            }).ReceiveJson<GetAccountResourceModel>();
            return result;
        }

        /// <summary>
        /// 获取账户信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<GetAccountModel> GetAccountAsync(string address)
        {
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request = BaseUrl
                .AppendPathSegment("wallet/getaccount")
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request = request.WithHeader("TRON-PRO-API-KEY", apiKey);
            }

            var result = await request.PostJsonAsync(new
            {
                address,
                visible = true
            }).ReceiveJson<GetAccountModel>();
            return result;
        }
        /// <summary>
        /// 获取TRX余额
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<decimal> GetTRXAsync(string address)
        {
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request = BaseUrl
                .AppendPathSegment("wallet/getaccount")
                .WithTimeout(5);
            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request = request.WithHeader("TRON-PRO-API-KEY", apiKey);
            }

            var result = await request.PostJsonAsync(new
            {
                address,
                visible = true
            }).ReceiveJson<GetAccountModel>();
            return (result?.Balance ?? 0) / 1_000_000m;
        }

        /// <summary>
        /// 获取转账带签名的原始数据
        /// </summary>
        /// <param name="OwnerAddress"></param>
        /// <param name="privateKey"></param>
        /// <param name="value"></param>
        /// <param name="ToAddress"></param>
        /// <returns></returns>
        public static async Task<(bool, string?, JObject?)> GetTransferTrxSignedTxnToJobjectAsync(string OwnerAddress, string privateKey, decimal value, string ToAddress)
        {
            var BaseUrl = configuration.GetValue("TronApiHost", "https://api.trongrid.io");
            var request = BaseUrl
                .WithTimeout(5);

            var apiKey = configuration.GetValue<string>("TRON-PRO-API-KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request = request.WithHeader("TRON-PRO-API-KEY", apiKey);
            }
            var postData = new
            {
                owner_address = OwnerAddress.Base58ToHex(),
                to_address = ToAddress.Base58ToHex(),
                amount = (long)(value * 1_000_000),
                visible = false
            };
            var resultData = request
                .AppendPathSegment("wallet/createtransaction")
                .PostJsonAsync(postData);
            var result = await resultData.ReceiveJson<JObject>();
            if (result.ContainsKey("Error") && result["Error"] != null)
            {
                return (false, result["Error"]?.ToString(), null);
            }
            var transaction = result;
            // Sign
            var txId = transaction["txID"]?.ToString().FromHexToByteArray();
            var wallet0 = new TronWallet(privateKey);
            Signature signature = wallet0.Sign(txId);
            TronSignature tronSignature = new TronSignature(signature);
            var signatureHex = tronSignature.SignatureBytes.ToHexString();
            transaction.Add("signature", JToken.FromObject(new string[] { signatureHex }));
            return (true, null, transaction);
        }
    }
}
