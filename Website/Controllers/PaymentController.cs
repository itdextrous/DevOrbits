using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltage.Models;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models.PaymentViewModels;
using MyVoltage.Services;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Data.Services.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Security.Authentication;
using System.ServiceModel;
using System.ServiceModel.Channels;
using MyVoltage.Api.SkyBill;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.Factories;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using MyVoltage.Api.Prism;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MyVoltage.Controllers
{

    [ApiExplorerSettings(IgnoreApi = true)]
    public class PaymentController : Controller
    {
        private DbContextOptions<MyVoltageDbContext> _options;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private PrismProvider _prismProvider;
        private CustomerProvider _customerProvider;
        private IDeviceApi _client;
        private PrismApiClient _prismApiClient;
        private PrismVendClient _prismVendClient;
        private readonly IHttpContextAccessor _context;

        // GET: /<controller>/
        public PaymentController(IMemoryCache cache, DbContextOptions<MyVoltageDbContext> options, UserManager<ApplicationUser> userManager, IConfiguration configuration, CustomerProvider customerProvider, IHttpContextAccessor context)
        {
            _context = context;
            _options = options;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
            _prismProvider = new PrismProvider(_cache, options);
            _customerProvider = customerProvider;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _prismApiClient = new PrismApiClient(options);
            _prismVendClient = new PrismVendClient(options);
        }

        [Route("recharge")]
        public async Task<ActionResult> Recharge(int amount)
        {
            return View();
        }

        [Route("topup")]
        public async Task<ActionResult> Topup(int amount)
        {
            return Redirect("recharge");
        }

        [Route("rechargemobile")]
        public async Task<ActionResult> RechargeMobile(int amount)
        {
            var query_string_value = Request.Query["btn"];
            foreach (var item in query_string_value)
            {
                var paymentType = item;
                ViewData["PaymentType"] = paymentType;
            }
            return View("~/Views/Payment/RechargeModile.cshtml");
        }

        [Route("payment")]
        [HttpPost]
        public async Task<ActionResult> Payment(string isCompanyAdminRecharge)
        {
            var amount = Convert.ToDecimal(Request.Form["amount"]);

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    Data.ActivityLog activityLog = new ActivityLog()
                    {
                        ActionID = (int)Data.LogActionEnum.PageLoad,
                        DateStarted = DateTime.Now,
                        Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                        Response = "",
                        SourceID = (int)LogSourceEnum.Clientzone,
                        SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                        UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
                    };

                    PaymentViewModel paymentModel = SavePayment(user, db, DateTime.Now, amount, isCompanyAdminRecharge);

                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }

                    return View(paymentModel);
                }
            }
            else
                return Unauthorized();
        }

        [Route("paymentmobile")]
        [HttpPost]
        public async Task<ActionResult> PaymentMobile(string isCompanyAdminRecharge)
        {
            var db = new MyVoltageDbContext(_options);
            var amount = Convert.ToDecimal(Request.Form["amount"]);

            string encodedUsernamePassword = Request.Query["token"];
            var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.Token == encodedUsernamePassword
            || p.Token == System.Web.HttpUtility.HtmlDecode(encodedUsernamePassword)
            || p.EncodedToken == encodedUsernamePassword
            || p.EncodedToken == System.Web.HttpUtility.HtmlDecode(encodedUsernamePassword)
            ).FirstOrDefault();
            if (wSCustomerLoginToken != null)
            {
                var user = await _userManager.FindByIdAsync(wSCustomerLoginToken.UserID);

                if (user != null)
                {
                    Data.ActivityLog activityLog = new ActivityLog()
                    {
                        ActionID = (int)Data.LogActionEnum.PageLoad,
                        DateStarted = DateTime.Now,
                        Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                        Response = "",
                        SourceID = (int)LogSourceEnum.Clientzone,
                        SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                        UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
                    };
                    PaymentViewModel paymentModel = SavePayment(user, db, DateTime.Now, amount, isCompanyAdminRecharge, isMobile: true);

                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }

                    return View("~/Views/Payment/PaymentMobile.cshtml", paymentModel);
                }
            }

            return Unauthorized();
        }



        [HttpPost("payment/redirect")]
        public ActionResult Redirect(PaymentNotifyModel model)
        {
            if (model == null)
                return BadRequest();

            PaymentProvider provider = new PaymentProvider(_configuration, _options);

            var db = new MyVoltageDbContext(_options);
            var payment = (from tbl in db.Payments
                           where tbl.PaymentID == int.Parse(model.Reference)
                           select tbl).FirstOrDefault();

            if (payment == null)
                return BadRequest();

            TransactionStatus status = provider.IsSuccessfulPayment(model, payment);

            payment.CardHolderIpAddr = model.CardHolderIpAddr;
            payment.Extra1 = model.Extra1;
            payment.Extra2 = model.Extra2;
            payment.Extra3 = model.Extra3;
            payment.Reason = model.Reason;
            db.SaveChanges();

            var result = new PaymentResultModel
            {
                PayRequestID = Int32.Parse(model.Reference),
                PaymentStatus = (int)TransactionStatus.Incomplete,
                Reason = model.Reason,
                Payment = payment
            };

            return View("Result", result);
        }


        [HttpPost("payment/accept")]
        public ActionResult Accept(PaymentNotifyModel notifyModel)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var payment = (from tbl in db.Payments
                               where tbl.PaymentID == int.Parse(notifyModel.Reference)
                               select tbl).FirstOrDefault();

                PaymentProvider provider = new PaymentProvider(_configuration, _options);

                TransactionStatus status = provider.IsSuccessfulPayment(notifyModel, payment);

                var result = new PaymentResultModel
                {
                    PayRequestID = payment.PaymentID,
                    PaymentStatus = (int)GetPaymentStatus(status),
                    Reason = notifyModel.Reason,
                };

                if (payment.IsMobile.HasValue && payment.IsMobile.Value)
                {
                    return View("~/Views/Payment/ResultMobile.cshtml", result);
                }

                return View("Result", result);
            }
        }

        [HttpPost("payment/decline")]
        public ActionResult Decline(PaymentNotifyModel notifyModel)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var payment = (from tbl in db.Payments
                               where tbl.PaymentID == int.Parse(notifyModel.Reference)
                               select tbl).FirstOrDefault();

                PaymentProvider provider = new PaymentProvider(_configuration, _options);

                TransactionStatus status = provider.IsSuccessfulPayment(notifyModel, payment);

                var result = new PaymentResultModel
                {
                    PayRequestID = payment.PaymentID,
                    PaymentStatus = (int)GetPaymentStatus(status),
                    Reason = notifyModel.Reason,
                };

                if (payment.IsMobile.HasValue && payment.IsMobile.Value)
                {
                    return View("~/Views/Payment/ResultMobile.cshtml", result);
                }

                return View("Result", result);
            }
        }


        [HttpPost("payment/notify")]
        public async Task<ActionResult> Notify(PaymentNotifyModel model)
        {
            if (model == null)
                return BadRequest();

            int rawPaymentDataID = 0;

            using (var db = new MyVoltageDbContext(_options))
            {
                var rawPaymentData = new PaymentRawData
                {
                    RawData = JsonConvert.SerializeObject(model),
                    PostingDate = DateTime.Now
                };

                db.PaymentRawData.Add(rawPaymentData);

                db.SaveChanges();

                rawPaymentDataID = rawPaymentData.PaymentRawDataID;
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                var payment = (from tbl in db.Payments
                               where tbl.PaymentID == int.Parse(model.Reference)
                               select tbl).FirstOrDefault();

                if (payment == null)
                {
                    SavePaymentError(rawPaymentDataID, "No payment entry found!");

                    return BadRequest();
                }

                if (payment.PaymentStatusID == (int)PaymentStatusEnum.Approved)
                {
                    SavePaymentError(rawPaymentDataID, "Payment already approved!");

                    return Ok();
                }

                Data.Customer customer = null;
                Company company = null;

                if (payment.IsCompanyAdminRecharge.HasValue && payment.IsCompanyAdminRecharge.Value)
                {
                    var companyAdminCustomer = db.Customers.Where(p => p.UserID == payment.UserID).SingleOrDefault();
                    var companyAdminCompany = db.Companies.Where(p => p.CompanyID == companyAdminCustomer.CompanyID).SingleOrDefault();

                    customer = new Data.Customer()
                    {
                        CustomerNumber = companyAdminCompany.BalanceCheckSkybillCustomerNo
                    };

                    company = new Company()
                    {
                        Name = _customerProvider.MyMeterSASkybill
                    };
                }
                else if (payment.IsOperationalRecharge.HasValue && payment.IsOperationalRecharge.Value && !string.IsNullOrEmpty(payment.SkybillCustomerNo))
                {
                    customer = new Data.Customer()
                    {
                        CustomerNumber = payment.SkybillCustomerNo
                    };

                    company = new Company()
                    {
                        Name = payment.SkybillCompanyName
                    };
                }
                else
                {

                    customer = (from tbl in db.Customers
                                where tbl.UserID == payment.UserID
                                where tbl.IsDeleted == false
                                select tbl).FirstOrDefault();

                    company = (from tbl in db.Companies
                               where tbl.CompanyID == customer.CompanyID
                               select tbl).FirstOrDefault();

                }

                if (customer == null)
                {
                    SavePaymentError(rawPaymentDataID, "No customer entry found!");

                    return BadRequest();
                }

                if (company == null)
                {
                    SavePaymentError(rawPaymentDataID, "No company entry found!");

                    return BadRequest();
                }

                PaymentProvider provider = new PaymentProvider(_configuration, _options);

                TransactionStatus status = provider.IsSuccessfulPayment(model, payment);

                payment.CardHolderIpAddr = model.CardHolderIpAddr;
                payment.RequestTrace = model.RequestTrace;
                payment.Extra1 = model.Extra1;
                payment.Extra2 = model.Extra2;
                payment.Extra3 = model.Extra3;
                payment.Reason = model.Reason;
                payment.PaymentMethodID = model.Method;
                payment.PaymentStatusID = (int)GetPaymentStatus(status);

                if (model.Method == 0)
                    payment.PaymentMethodID = 9;

                db.SaveChanges();

                SavePaymentError(rawPaymentDataID, "Saved Payment Info!");

                if (status != TransactionStatus.Approved)
                    return Ok();

                decimal convenienceFee = (payment.Amount * (company != null && company.ConvenienceFeePerc.HasValue ? company.ConvenienceFeePerc.Value : 0.1m));
                decimal loadedAmount = payment.Amount;

                // Does not pay convenience fee

                if (payment.PaymentMethodID == (int)PaymentMethodEnum.iPay || payment.PaymentMethodID == (int)PaymentMethodEnum.EFT || payment.PaymentMethodID == (int)PaymentMethodEnum.Retail)
                {
                    convenienceFee = 0;
                }

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                #region Skybill Journals

                try
                {
                    skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = DateTime.UtcNow.Date,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Customer,
                        Account_No = customer.CustomerNumber,
                        AmountSpecified = true,
                        Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment",
                        Amount = loadedAmount * -1,
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                        Bal_Account_No = "SAGEPAY",
                    }, db, customer.UserID);
                }
                catch (Exception ex)
                {
                    SavePaymentError(rawPaymentDataID, ex.ToString());
                    return BadRequest();
                }

                try
                {
                    if (convenienceFee > 0)
                    {
                        skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new SalesJournal.SalesJnl()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = DateTime.UtcNow.Date,
                            Document_TypeSpecified = true,
                            Document_Type = SalesJournal.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = SalesJournal.Account_Type.Customer,
                            Account_No = customer.CustomerNumber,
                            AmountSpecified = true,
                            Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee",
                            Amount = convenienceFee,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                            Bal_Account_No = "6810",
                        }, db, customer.UserID);
                    }
                }
                catch (Exception ex)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "Convenience error", ex.ToString(), ex.ToString(), cc: "madelyn@myvoltage.co.za");
                }

                #region New Payments for the 5%

                StringBuilder fullErrorMessage = new StringBuilder();

                var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
                string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";

                if (company.CompanyID == 13)
                {
                    vendingCustomerNo = "000-S";
                }

                try
                {
                    var bankCharges = PaymentMethodFees.GetBankCharges((PaymentMethodEnum)payment.PaymentMethodID, loadedAmount, payment.PaymentID.ToString());

                    if (bankCharges == null)
                    {
                        EmailSender emailSender = new EmailSender();
                        string body = $"MY VOLTAGE VENDING FAILED - Could not locate bank charges for Payment Method: {payment.PaymentMethodID} - {company.Name} - {customer.CustomerNumber} ({payment.Amount:N2})";
                        await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "5% error", body, body, cc: "madelyn@myvoltage.co.za");
                    }
                    else
                    {

                        decimal feeCalcForStep3and4 = (loadedAmount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

                        if (feeCalcForStep3and4 <= 0)
                            feeCalcForStep3and4 = 0.01m;

                        int? vending1LogID = null;
                        string vending1SkybillCompanyName = "";
                        int? vending2LogID = null;
                        string vending2SkybillCompanyName = "";
                        int? vending3LogID = null;
                        string vending3SkybillCompanyName = "";
                        int? vending4LogID = null;
                        string vending4SkybillCompanyName = "";

                        switch ((PaymentMethodEnum)payment.PaymentMethodID)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region Vending 1
                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)
                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2
                                // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    // + & -,  if 0 then 0.01
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - Mastercard/VISA Commission",
                                    // if negative then zero
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.Retail:

                                vending1LogID = 0;
                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.iPay:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region Vending 1

                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)
                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2

                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = $"{payment.PaymentID} - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MasterPass Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,

                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2

                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);


                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - VisaCheckout Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }

                        payment.Vending1LogID = vending1LogID;
                        payment.Vending1SkybillCompanyName = vending1SkybillCompanyName;
                        payment.Vending2LogID = vending2LogID;
                        payment.Vending2SkybillCompanyName = vending2SkybillCompanyName;
                        payment.Vending3LogID = vending3LogID;
                        payment.Vending3SkybillCompanyName = vending3SkybillCompanyName;
                        payment.Vending4LogID = vending4LogID;
                        payment.Vending4SkybillCompanyName = vending4SkybillCompanyName;

                        db.Update(payment);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "5% error", ex.ToString(), ex.ToString(), cc: "madelyn@myvoltage.co.za");
                }

                #endregion

                #endregion

                if (!payment.IsCompanyAdminRecharge.HasValue || !payment.IsCompanyAdminRecharge.Value)
                {
                    #region Prism Token

                    try
                    {
                        //Send STS token
                        if (customer != null && customer.AccountTypeID == (int)AccountTypeEnum.MyWallet)
                        {
                            decimal balance = CustomerProvider.GetMyWalletBalance(_cache, company.Name, customer.CustomerNumber);

                            if (balance/* < 0 && balance + loadedAmount*/ > 0)
                            {
                                var apiClient = new SkyBillApiClient(company.Name, _cache);
                                var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);

                                foreach (var meter in meters)
                                {
                                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No, 2);
                                    if (device != null && String.Compare(device.type.type, "elec", true) == 0)
                                    {
                                        DateTime? meterStatusTime = null;
                                        if (device.status != null)
                                            meterStatusTime = device.status.time;

                                        //#region Contactor State

                                        //Dictionary<int, string> registers = new Dictionary<int, string>();
                                        //registers.Add(91, "readings");

                                        //DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                                        //DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                                        string errorMessage = "";

                                        //var deviceContactorStateData = _client.GetMeterUsage(meter.Serial_No, startTime, endTime, 900, registers);
                                        //string contactorState = "";

                                        //if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                                        //{
                                        //    errorMessage = "PaymentController.cs:1394 - deviceContactorStateData null";
                                        //}
                                        //else
                                        //{
                                        //    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                        //    if (validreadings == null || validreadings.Count == 0)
                                        //    {
                                        //        errorMessage = "PaymentController.cs:1402 - validreadings null";
                                        //    }
                                        //    else
                                        //    {
                                        //        contactorState = validreadings[validreadings.Count - 1].ToString();
                                        //    }
                                        //}

                                        //bool isContactorConnected = false;

                                        //if (!string.IsNullOrEmpty(contactorState))
                                        //{
                                        //    try
                                        //    {
                                        //        if (Convert.ToInt32(contactorState) == 1)
                                        //            isContactorConnected = true;
                                        //        else if (Convert.ToInt32(contactorState) == 0)
                                        //            isContactorConnected = false;
                                        //        errorMessage = contactorState;
                                        //    }
                                        //    catch (Exception ex)
                                        //    {
                                        //        errorMessage = "PaymentController.cs:1423 - " + ex.ToString();
                                        //    }
                                        //}

                                        //#endregion

                                        //if (!isContactorConnected)
                                        //{

                                        if (meter.Serial_No.Trim().Length == 11)
                                        {
                                            var token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, meter.Serial_No, 0, "SagePay", balance, errorMessage, customer.UserID);
                                            //var token = _prismApiClient.GenerateToken(meter.Serial_No, "set-postpaid", "SagePay", balance, errorMessage, customer.UserID);

                                            if (!string.IsNullOrEmpty(token))
                                            {
                                                _client.MeterSTS(token, device.id.ToString(), "SagePay", balance, errorMessage, device.deviceStatus, meterStatusTime, deviceApiID: 2);
                                                //System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, device.id.ToString(), "SagePay", balance, errorMessage, meter.Serial_No));
                                                //thread.Start();
                                            }
                                            //else
                                            //{
                                            //    System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(meter.Serial_No, "set-postpaid", "SagePay", balance, customer.UserID));
                                            //    thread.Start();

                                            //    //    string emailBody = $"There was an error generating token for {meter.Serial_No} - Payment of {loadedAmount:N}";
                                            //    //    EmailSender emailSender = new EmailSender();
                                            //    //    await emailSender.SendEmailAsync(new string[] {
                                            //    //"rose@myvoltage.co.za",
                                            //    //"riaan@myvoltage.co.za",
                                            //    //"lendl@myvoltage.co.za",
                                            //    //"nic@myvoltage.co.za",
                                            //    //}
                                            //    //    , "Token Generation Error - Sage"
                                            //    //    , emailBody
                                            //    //    , emailBody);
                                            //}
                                        }
                                        //}
                                        _client.ConnectMeter(1, device.id.ToString(), 3, "SagePay", balance, errorMessage, device.deviceStatus, meterStatusTime);
                                    }
                                }

                            }
                        }

                        if (customer != null && customer.AccountTypeID == (int)AccountTypeEnum.PrepaidCredit
                            && (!customer.AutoConvertBalanceToUnits.HasValue
                            || customer.AutoConvertBalanceToUnits.Value))
                        {
                            var apiClient = new SkyBillApiClient(company.Name, _cache);
                            string token = "";
                            string prismDeviceID = "";
                            var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                            var skybillCustomer = apiClient.GetCustomer(customer.CustomerNumber);

                            decimal amount = Convert.ToDecimal(skybillCustomer.Balance_LCY) * -1;
                            string serial = "";
                            DateTime? meterStatusTime = null;
                            string deviceStatus = "";
                            if (amount > 0)
                            {
                                foreach (var meter in meters)
                                {
                                    var prismDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                                    prismDeviceID = prismDevice.id.ToString();
                                    if (prismDevice != null && String.Compare(prismDevice.type.type, "elec", true) == 0)
                                    {
                                        token = _prismApiClient.GenerateToken(meter.Serial_No, Convert.ToDouble(amount), "SagePay - AutoRedeem", amount, "", customer.UserID);
                                        serial = meter.Serial_No;
                                        if (prismDevice.status != null)
                                            meterStatusTime = prismDevice.status.time;
                                        deviceStatus = prismDevice.deviceStatus;
                                        break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(token))
                                {
                                    _client.MeterSTS(token, prismDeviceID, "SagePay - AutoRedeem", amount, "", deviceStatus, meterStatusTime);
                                    //System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, prismDeviceID, "SagePay - AutoRedeem", amount, "", serial));
                                    //thread.Start();

                                    #region Journal Entry

                                    SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();

                                    journal.Posting_DateSpecified = true;
                                    journal.Posting_Date = DateTime.UtcNow.Date;
                                    journal.Document_TypeSpecified = true;
                                    journal.Document_Type = SalesJournal.Document_Type.Invoice;
                                    journal.Account_TypeSpecified = true;
                                    journal.Account_Type = SalesJournal.Account_Type.Customer;
                                    journal.Account_No = customer.CustomerNumber;
                                    journal.AmountSpecified = true;
                                    journal.Description = $"Balance Redeem - {customer.CustomerNumber}";
                                    journal.Amount = Convert.ToDecimal(amount);
                                    journal.Bal_Account_TypeSpecified = true;
                                    journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                                    journal.Bal_Account_No = "6610";

                                    SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                                    skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, journal, db, customer.UserID);

                                    #endregion

                                    skybillCustomer = apiClient.GetCustomer(customer.CustomerNumber);
                                    var updatedBalance = skybillCustomer.Balance_LCY * -1;

                                    string phoneNumber = customer.NotificationPhoneNumber;
                                    if (!string.IsNullOrEmpty(phoneNumber))
                                    {
                                        StringBuilder sbSMS = new StringBuilder();
                                        sbSMS.Append($"Token:{token}.");
                                        sbSMS.Append($"{Environment.NewLine}Purchased: R {loadedAmount:N}");
                                        sbSMS.Append($"{Environment.NewLine}Other charges: R {(loadedAmount - Convert.ToDecimal(amount)):N}");
                                        sbSMS.Append($"{Environment.NewLine}Balance converted: R {amount:N}");
                                        sbSMS.Append($"{Environment.NewLine}Help: 0870572561");
                                        sbSMS.Append($"{Environment.NewLine}Ref: {model.Reference}");

                                        SMS.SendSms("27" + phoneNumber.Remove(0, 1), sbSMS.ToString());
                                    }
                                }
                                //else
                                //{
                                //    System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(serial, Convert.ToDouble(amount), "SagePay - AutoRedeem", amount, customer.UserID));
                                //    thread.Start();
                                //}
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    #endregion
                }
            }

            return Ok();
        }

        private void SavePaymentError(int id, string error)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var rawPaymentData = db.PaymentRawData.Find(id);
                rawPaymentData.Error = error;

                db.SaveChanges();
            }
        }

        [HttpPost("payment/result")]
        public ActionResult Result(PaymentNotifyModel notifyModel)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var payment = (from tbl in db.Payments
                               where tbl.PaymentID == int.Parse(notifyModel.Reference)
                               select tbl).FirstOrDefault();

                PaymentProvider provider = new PaymentProvider(_configuration, _options);

                TransactionStatus status = provider.IsSuccessfulPayment(notifyModel, payment);

                var result = new PaymentResultModel
                {
                    PayRequestID = payment.PaymentID,
                    PaymentStatus = (int)GetPaymentStatus(status),
                    Reason = notifyModel.Reason,
                };

                if (payment.IsMobile.HasValue && payment.IsMobile.Value)
                {
                    return View("~/Views/Payment/ResultMobile.cshtml", result);
                }

                return View(result);
            }
        }

        [Route("payment/status/{payRequestID}")]
        public ActionResult Result(string payRequestID)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var payment = (from tbl in db.Payments
                               where tbl.PaymentID == int.Parse(payRequestID)
                               select tbl).FirstOrDefault();

                var result = new PaymentResultModel
                {
                    Reason = payment.Reason
                };

                return Ok(result);
            }
        }

        private PaymentStatusEnum GetPaymentStatus(TransactionStatus status)
        {
            switch (status)
            {
                case TransactionStatus.Approved:
                    {
                        return PaymentStatusEnum.Approved;
                    }


                case TransactionStatus.Declined:
                    {
                        return PaymentStatusEnum.Declined;
                    }

                case TransactionStatus.Hacked:
                    {
                        return PaymentStatusEnum.Incomplete;
                    }
            }

            return PaymentStatusEnum.Incomplete;
        }

        private PaymentViewModel SavePayment(ApplicationUser user, MyVoltageDbContext db, DateTime now, decimal amountInRand, string isCompanyAdminRecharge, bool isMobile = false)
        {
            var payment = new Payment
            {
                Amount = amountInRand,
                CreateDate = DateTime.Now,
                PaymentMethodID = (int)PaymentMethodEnum.MastercardVISA,
                PaymentStatusID = (int)PaymentStatusEnum.Incomplete,
                UserID = user.Id,
                IsCompanyAdminRecharge = string.IsNullOrEmpty(isCompanyAdminRecharge) ? false : true,
                IsMobile = isMobile,
            };

            db.Payments.Add(payment);

            db.SaveChanges();

            string reference = payment.PaymentID.ToString();

            PaymentProvider provider = new PaymentProvider(_configuration, _options);
            var paymentModel = provider.GetPaymentModel(amountInRand, reference, user, isCompanyAdminRecharge);

            payment.Reference = paymentModel.p2;

            db.SaveChanges();
            return paymentModel;
        }

        [Route("/payment/testVendingToken")]
        public void TestVendingToken()
        {
            //double amount = 500.00;
            //_prismProvider.SendToken("14271435373", amount, "TestVendingToken", null, "");
        }

        [Route("/payment/testSage")]
        public async Task<ActionResult> TestSage()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);

            if (loggedInUser.Email != _configuration["AppSettings:MasterOperationalEmail"])
                return NotFound();

            // 14293422060@myvoltage.co.za - 264f8a4b-8909-44a3-b5ba-9ac13d147ec6
            // Zelda Prins - db5497a2-ab62-4187-98bb-0f2a07c6e87f
            var user = _userManager.FindByIdAsync("db5497a2-ab62-4187-98bb-0f2a07c6e87f").Result;

            decimal amount = 1.0m;
            PaymentViewModel paymentModel = new PaymentViewModel();
            using (var db = new MyVoltageDbContext(_options))
            {
                paymentModel = SavePayment(user, db, DateTime.Now, amount, "");
            }

            //UniPin - R580
            int paymentMethod = ((int)PaymentMethodEnum.MastercardVISA); // 10
            //int paymentMethod = ((int)PaymentMethodEnum.EFT); // 100
            //int paymentMethod = ((int)PaymentMethodEnum.Retail); // 150
            //int paymentMethod = ((int)PaymentMethodEnum.iPay); // 200
            //int paymentMethod = ((int)PaymentMethodEnum.MasterPass); // 300
            //int paymentMethod = ((int)PaymentMethodEnum.VisaCheckout); // 400
            //int paymentMethod = ((int)PaymentMethodEnum.NotInUse); // 500

            PaymentNotifyModel model = new PaymentNotifyModel()
            {
                Amount = Convert.ToDecimal(paymentModel.p4),
                CardHolderIpAddr = "127.0.0.1",
                Extra1 = "",
                Extra2 = "",
                Extra3 = "",
                Method = paymentMethod,
                Reason = "Success",
                Reference = paymentModel.p2,
                RequestTrace = "",
                TransactionAccepted = "true"
            };

            return await Notify(model);

            return Ok();
        }

        //[Route("/balancetopup")]
        //[HttpGet]
        //public async Task<ActionResult> BalanceTopup()
        //{

        //    #region Journal Entry

        //    using (var db = new MyVoltageDbContext(_options))
        //    {
        //        var user = await _userManager.GetUserAsync(User);

        //        var customer = (from tbl in db.Customers
        //                        where tbl.UserID == user.Id
        //                        where tbl.IsDeleted == false
        //                        select tbl).FirstOrDefault();

        //        PaymentProvider provider = new PaymentProvider(_configuration, _options);

        //        var jornalEntry = await provider.CreateJournalEntry(1000, "SAGEPAY", new Company() { Name = _customerProvider.CompanyName }, customer, $"1 - {PaymentMethodEnum.EFT.ToString()}: Payment");

        //        var getRecIdFromKey = await provider.GetRecIdFromKey(jornalEntry, new Company() { Name = _customerProvider.CompanyName });

        //        var postReceiptJournal = await provider.PostReceiptJournalAsync(getRecIdFromKey, new Company() { Name = _customerProvider.CompanyName });
        //    }

        //    #endregion

        //    return Ok();
        //}

        [Route("/balanceredeem")]
        [HttpGet]
        public async Task<ActionResult> BalanceRedeem()
        {
            BalanceRedeemViewModel balanceRedeemViewModel = new BalanceRedeemViewModel();

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            var skybillCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);

            balanceRedeemViewModel.CurrentWalletBalance = skybillCustomer.Balance_LCY * -1;

            return View(balanceRedeemViewModel);
        }

        [Route("balanceredeem")]
        [HttpPost]
        public async Task<ActionResult> BalanceRedeem(BalanceRedeemViewModel balanceRedeemViewModel)
        {
            var amount = Convert.ToDecimal(Request.Form["amount"]);

            var user = await _userManager.GetUserAsync(User);

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(_customerProvider.CustomerNumber, _customerProvider.CompanyName);

            var balance = skybillCustomerDetails.Balance_LCY * -1;

            if (balance >= amount)
            {
                PaymentProvider provider = new PaymentProvider(_configuration, _options);

                #region Prism Token

                string token = "";
                string serial = "";
                string prismDeviceID = "";
                var meters = skyBillApiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

                DateTime? meterStatusTime = null;
                string deviceStatus = "";

                foreach (var meter in meters)
                {
                    var prismDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                    prismDeviceID = prismDevice.id.ToString();
                    if (prismDevice != null && String.Compare(prismDevice.type.type, "elec", true) == 0)
                    {
                        token = _prismApiClient.GenerateToken(meter.Serial_No, Convert.ToDouble(amount), "BalanceRedeem", amount, "", user.Id);
                        serial = meter.Serial_No;
                        if (prismDevice.status != null)
                            meterStatusTime = prismDevice.status.time;
                        deviceStatus = prismDevice.deviceStatus;
                        break;
                    }
                }


                #endregion

                if (!String.IsNullOrEmpty(token))
                {
                    _client.MeterSTS(token, prismDeviceID, "BalanceRedeem", amount, "", deviceStatus, meterStatusTime, deviceApiID: 2);

                    #region Journal Entry

                    SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();

                    journal.Posting_DateSpecified = true;
                    journal.Posting_Date = DateTime.UtcNow.Date;
                    journal.Document_TypeSpecified = true;
                    journal.Document_Type = SalesJournal.Document_Type.Invoice;
                    journal.Account_TypeSpecified = true;
                    journal.Account_Type = SalesJournal.Account_Type.Customer;
                    journal.Account_No = _customerProvider.CustomerNumber;
                    journal.AmountSpecified = true;
                    journal.Description = $"Balance Redeem - {_customerProvider.CustomerNumber}";
                    journal.Amount = amount;
                    journal.Bal_Account_TypeSpecified = true;
                    journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                    journal.Bal_Account_No = "6610";

                    SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);



                    var journalEntry = await provider.CreateSalesJournalEntry(new Company() { Name = _customerProvider.CompanyName }, journal);

                    var getRecIdFromKey = await provider.GetSalesRecIdFromKey(journalEntry, new Company() { Name = _customerProvider.CompanyName });

                    var postReceiptJournal = await provider.PostSalesReceiptJournalAsync(getRecIdFromKey, new Company() { Name = _customerProvider.CompanyName });

                    #endregion

                    #region Refresh Balance

                    var skybillCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);
                    if (skybillCustomer != null)
                        balanceRedeemViewModel.CurrentWalletBalance = skybillCustomer.Balance_LCY * -1;

                    #endregion

                    #region Send SMS

                    var db = new MyVoltageDbContext(_options);

                    var customer = (from tbl in db.Customers
                                    where tbl.UserID == user.Id
                                    where tbl.IsDeleted == false
                                    select tbl).FirstOrDefault();

                    if (customer != null && !string.IsNullOrEmpty(customer.NotificationPhoneNumber))
                    {
                        string phoneNumber = customer.NotificationPhoneNumber;

                        SMS.SendSms("27" + phoneNumber.Remove(0, 1), $"Your Balance of R {amount:N} has been redeemed. Current Balance: R {balanceRedeemViewModel.CurrentWalletBalance:N}. Your token is {token}.");
                    }

                    #endregion

                    balanceRedeemViewModel.Result = $"Your Balance of R {amount:N} has been redeemed. Current Balance: R {balanceRedeemViewModel.CurrentWalletBalance:N}. Your token is {token}.";
                }
                else
                {

                    string emailBody = $"There was an error generating token for {serial} - Balance Redeem";

                    emailBody = emailBody + $"<br>CustomerNumber:{_customerProvider.CustomerNumber}";

                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , "Token Generation Error - Balance Redeem"
                    , emailBody
                    , emailBody);

                    balanceRedeemViewModel.Result = $"An error occured. Please try again. Contact Customer Care if problem persists.";

                }
            }

            return View(balanceRedeemViewModel);
        }


    }
}
