using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVoltage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyVoltage.Services;
using MyVoltage.Models;
using MyVoltage.Api.SkyBill;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Factories;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.Prism;

namespace MyVoltage.Controllers.Api
{
    [BasicAuthorize("")]
    [Produces("application/json")]
    [Route("api/unipin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinController : Controller
    {
        private DbContextOptions<MyVoltageDbContext> _options;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private List<Company> _companies;
        private readonly IMemoryCache _cache;
        private PrismProvider _prismProvider;
        private IDeviceApi _client;
        private PrismApiClient _prismApiClient;
        private PrismVendClient _prismVendClient;

        // GET: /<controller>/
        public UniPinController(DbContextOptions<MyVoltageDbContext> options, UserManager<ApplicationUser> userManager, IConfiguration configuration, IMemoryCache cache)
        {
            _options = options;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;

            _prismProvider = new PrismProvider(cache, options);

            using (var db = new MyVoltageDbContext(_options))
            {
                // Start at the bottom of the list when looking for companies
                _companies = db.Companies.OrderByDescending(p => p.CompanyID).ToList();
            }
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _prismApiClient = new PrismApiClient(_options);
            _prismVendClient = new PrismVendClient(_options);
        }

        [HttpPost]
        [Route("verify")]
        public ActionResult Verify([FromBody] UniPinVerify verifyRequest)
        {
            UniPinVerifyResult result = new UniPinVerifyResult();

            if (verifyRequest == null)
            {
                result.ResponseStatus = 9999;
                result.ResponseStatusText = "Unknown";

                return Ok(result);
            }

            if (!IsSignatureMatch<UniPinVerify>(verifyRequest.Hash, verifyRequest))
            {
                result.ResponseStatus = 2;
                result.ResponseStatusText = "Digital signature invalid";

                return Ok(result);
            }

            if (String.IsNullOrEmpty(verifyRequest.DateTime) || String.IsNullOrEmpty(verifyRequest.MeterNumber))
            {
                result.ResponseStatus = 0101;
                result.ResponseStatusText = "Mandatory fields not present";

                return Ok(result);
            }

            Data.Customer localCustomer = null;
            using (var db = new MyVoltageDbContext(_options))
            {
                // Start at the bottom of the list when looking for companies
                try
                {
                    localCustomer = db.Customers.Where(p => !p.IsDeleted && p.MeterNumber == verifyRequest.MeterNumber).SingleOrDefault();
                }
                catch
                {
                    localCustomer = db.Customers.Where(p => !p.IsDeleted && p.MeterNumber == verifyRequest.MeterNumber).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                }
            }

            if (localCustomer == null)
            {
                SkybillCustomer skybillCustomer = null;
                using (var db = new MyVoltageDbContext(_options))
                {
                    try
                    {
                        skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == verifyRequest.MeterNumber).SingleOrDefault();
                    }
                    catch
                    {
                        skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == verifyRequest.MeterNumber).OrderByDescending(p => p.ID).FirstOrDefault();
                    }
                }
                if (skybillCustomer == null)
                {
                    var apiCustomer = FindCustomer(verifyRequest.MeterNumber);

                    if (apiCustomer == null)
                    {
                        result.ResponseStatus = 204;
                        result.ResponseStatusText = "Meter not found";

                        return Ok(result);
                    }

                    result.UserAddress = apiCustomer.Item1.Address;
                    result.MeterNumber = verifyRequest.MeterNumber;
                    result.UserName = apiCustomer.Item1.Customer_Name;
                }
                else
                {
                    result.UserAddress = skybillCustomer.Address;
                    result.MeterNumber = verifyRequest.MeterNumber;
                    result.UserName = skybillCustomer.Customer_Name;
                }
            }
            else
            {
                result.UserAddress = localCustomer.StreetAddress;
                result.MeterNumber = verifyRequest.MeterNumber;
                result.UserName = localCustomer.FullName;
            }

            /*
             MyVoltage
RCPT:20201124093749316687
AmtElec:20.00
Charges:2.00
Blu Approved - the power of prepaid 
              */

            return Ok(result);
        }

        // POST: api/UniPin
        [HttpPost]
        [Route("request")]
        public async Task<ActionResult> Request([FromBody] UniPinRequest purchase)
        {
            UniPinResult result = new UniPinResult()
            {
                Tokens = new List<string>(),
            };

            if (purchase == null)
            {
                result.ResponseStatus = 9999;
                result.ResponseStatusText = "Unknown";
                SaveUniPinResult(purchase, result);

                return Ok(result);
            }

            if (!IsSignatureMatch<UniPinRequest>(purchase.Hash, purchase))
            {
                result.ResponseStatus = 2;
                result.ResponseStatusText = "Digital signature invalid";

                SaveUniPinResult(purchase, result);

                return Ok(result);
            }

            if (String.IsNullOrEmpty(purchase.AmountInCents) || String.IsNullOrEmpty(purchase.DateTime) || String.IsNullOrEmpty(purchase.Id) || String.IsNullOrEmpty(purchase.MeterNumber))
            {
                result.ResponseStatus = 0101;
                result.ResponseStatusText = "Mandatory fields not present";

                SaveUniPinResult(purchase, result);

                return Ok(result);
            }

            DateTime transDate = DateTime.Now;
            int amountInCents = 0;

            if (!Int32.TryParse(purchase.AmountInCents, out amountInCents) || !DateTime.TryParse(purchase.DateTime, out transDate))
            {
                result.ResponseStatus = 0102;
                result.ResponseStatusText = "Invalid format for field";

                SaveUniPinResult(purchase, result);

                return Ok(result);
            }

            if (amountInCents <= 0)
            {
                result.ResponseStatus = 0102;
                result.ResponseStatusText = "Invalid format for field";

                SaveUniPinResult(purchase, result);

                return Ok(result);
            }

            Data.Customer localCustomer = null;

            //result.MeterNumber = purchase.MeterNumber;
            //result.UserAddress = localCustomer.StreetAddress;
            //result.UserName = localCustomer.FullName;

            using (var db = new MyVoltageDbContext(_options))
            {
                var unipin = (from tbl in db.UniPins
                              where tbl.ReferenceID == purchase.Id
                              select tbl).FirstOrDefault();


                if (unipin != null)
                {
                    result.ResponseStatus = 103;
                    result.ResponseStatusText = "Duplicate request ID";

                    SaveUniPinResult(purchase, result);

                    return Ok(result);
                }

                #region Try to find the customer in our local db

                try
                {
                    localCustomer = db.Customers.Where(p => !p.IsDeleted && p.MeterNumber == purchase.MeterNumber).SingleOrDefault();
                }
                catch
                {
                    localCustomer = db.Customers.Where(p => !p.IsDeleted && p.MeterNumber == purchase.MeterNumber).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                }

                #endregion

                #region Skybill & Company lookup

                MyVoltage.Data.Company company = null;
                var localSkybillCustomer = (from p in db.SkybillCustomers
                                            where p.Serial_No == purchase.MeterNumber
                                            orderby p.AuxiliaryIndex4 descending
                                            select p).FirstOrDefault();

                SkyBillApiClient skyBillApiClient = null;
                Tuple<MyVoltage.Api.SkyBill.Customer, Company> apiCustomer = null;

                if (localSkybillCustomer != null)
                {
                    company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).SingleOrDefault();
                    skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                    apiCustomer = new Tuple<MyVoltage.Api.SkyBill.Customer, Company>(
                        skyBillApiClient.GetCustomer(localSkybillCustomer.Customer_No)
                        , company);
                }
                else
                {
                    skyBillApiClient = new SkyBillApiClient("", _cache);
                    apiCustomer = FindCustomer(purchase.MeterNumber);
                }

                #endregion

                result.UserAddress = apiCustomer.Item1.Address;
                result.MeterNumber = purchase.MeterNumber;
                result.UserName = apiCustomer.Item1.Customer_Name;

                result.PaidAmount = amountInCents / 100m;
                result.ConvenienceFee = (amountInCents * (company != null && company.ConvenienceFeePerc.HasValue ? company.ConvenienceFeePerc.Value : 0.1m)) / 100m;
                result.LoadedAmount = result.PaidAmount;

                #region Skybill Journal Entries

                try
                {
                    skyBillApiClient.CreateJournalEntry(apiCustomer.Item2, apiCustomer.Item1.Customer_No, new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = DateTime.UtcNow.Date,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Customer,
                        Account_No = apiCustomer.Item1.Customer_No,
                        AmountSpecified = true,
                        Description = $"{purchase.Id} - Payment",
                        Amount = result.LoadedAmount * -1,
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,

                        Bal_Account_No = "CIGICELL",
                    }, db, "");

                }
                catch (Exception ex)
                {
                    result.Message = ex.ToString();
                    result.ResponseStatus = 302;
                    result.ResponseStatusText = "Internal error 2";

                    string emailBody = $"There was an error creating Journals for {result.MeterNumber} - R{result.PaidAmount:N}<br>{ex}";
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "madelyn@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , "302 Error - Unipin"
                    , emailBody
                    , emailBody);

                    // SMS
                    // Zelda Company Name, Customer Number
                    SMS.SendSms("27784574259", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27824443755", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");

                    SaveUniPinResult(purchase, result);

                    return Ok(result);
                }

                //Added Sales Journal for 10% convenience fee

                try
                {

                    skyBillApiClient.CreateJournalEntry(apiCustomer.Item2, apiCustomer.Item1.Customer_No, new SalesJournal.SalesJnl()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = DateTime.UtcNow.Date,
                        Document_TypeSpecified = true,
                        Document_Type = SalesJournal.Document_Type.Invoice,
                        Account_TypeSpecified = true,
                        Account_Type = SalesJournal.Account_Type.Customer,
                        Account_No = apiCustomer.Item1.Customer_No,
                        AmountSpecified = true,
                        Description = $"{purchase.Id} - Fee",
                        Amount = result.ConvenienceFee,
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,

                        Bal_Account_No = "6810",
                    }, db, "");
                }
                catch (Exception ex)
                {
                    result.Message = ex.ToString();
                    result.ResponseStatus = 302;
                    result.ResponseStatusText = "Internal error Convenience";

                    string emailBody = $"There was an error creating Journals for {result.MeterNumber} - R{result.PaidAmount:N}<br>{ex}";
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "madelyn@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , "302 Error - Unipin"
                    , emailBody
                    , emailBody);

                    // SMS
                    // Zelda Company Name, Customer Number
                    SMS.SendSms("27784574259", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27824443755", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                    SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");

                    SaveUniPinResult(purchase, result);

                    return Ok(result);
                }


                #region New Payments for the 5%

                /*System.Threading.Thread threadVending = new System.Threading.Thread(() => */
                CreateVendingJournals(purchase, result, apiCustomer.Item2, apiCustomer.Item1)/*)*/;
                //threadVending.Start();

                #endregion

                #endregion

                #region Refresh apiCustomer

                apiCustomer = new Tuple<MyVoltage.Api.SkyBill.Customer, Company>(
                        skyBillApiClient.GetCustomer(apiCustomer.Item1.Customer_No)
                        , apiCustomer.Item2);

                #endregion

                bool sendPrepaidMessageText = false;
                string prepaidTokenGenerated = "";

                try
                {
                    if (apiCustomer.Item1.BILLING_CYCLE.ToUpper().Contains("WALLET") || (localCustomer != null && localCustomer.AccountTypeID == 1))
                    {
                        decimal balance = Convert.ToDecimal(apiCustomer.Item1.Balance_LCY) * -1;// CustomerProvider.GetMyWalletBalance(_cache, apiCustomer.Item2.Name, apiCustomer.Item1.Customer_No);

                        if (balance /*< 0 && balance + result.LoadedAmount*/ > 0)
                        {
                            var apiClient = new SkyBillApiClient(apiCustomer.Item2.Name, _cache);
                            var meters = apiClient.GetMetersByCustomer(apiCustomer.Item1.Customer_No);

                            foreach (var meter in meters)
                            {
                                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
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
                                    //    errorMessage = "Meter usage not found";
                                    //}
                                    //else
                                    //{
                                    //    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                    //    if (validreadings == null || validreadings.Count == 0)
                                    //    {
                                    //        errorMessage = "Meter usage not found";
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
                                    //        errorMessage = "Invalid Contactor State";
                                    //    }
                                    //}

                                    //#endregion

                                    //if (!isContactorConnected)
                                    //{

                                    if (meter.Serial_No.Trim().Length == 11)
                                    {
                                        var token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, meter.Serial_No, 0, "Unipin", balance, errorMessage, "");
                                        //var token = _prismApiClient.GenerateToken(meter.Serial_No, "set-postpaid", "Unipin", balance, errorMessage, "");

                                        if (!string.IsNullOrEmpty(token))
                                        {
                                            // Now
                                            _client.MeterSTS(token, device.id.ToString(), "Unipin", balance, errorMessage, device.deviceStatus, meterStatusTime, deviceApiID: 2);
                                            System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, device.id.ToString(), "Unipin", balance, errorMessage, meter.Serial_No));
                                            thread.Start();
                                        }
                                        else
                                        {
                                            System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(meter.Serial_No, "set-postpaid", "Unipin", balance, ""));
                                            thread.Start();

                                            //string emailBody = $"There was an error generating token for {meter.Serial_No} - Payment of {result.LoadedAmount:N}";
                                            //EmailSender emailSender = new EmailSender();
                                            //await emailSender.SendEmailAsync(new string[] {
                                            //"rose@myvoltage.co.za",
                                            //"riaan@myvoltage.co.za",
                                            //"lendl@myvoltage.co.za",
                                            //"nic@myvoltage.co.za",
                                            //}
                                            //, "Token Generation Error - Sage"
                                            //, emailBody
                                            //, emailBody);
                                        }
                                    }
                                    //}
                                    _client.ConnectMeter(1, device.id.ToString(), 3, "Unipin", balance, errorMessage, device.deviceStatus, meterStatusTime);
                                }
                            }
                        }
                    }

                    if (apiCustomer.Item1.BILLING_CYCLE.ToUpper().Contains("PREPAID"))
                    {
                        var apiClient = new SkyBillApiClient(apiCustomer.Item2.Name, _cache);
                        var meters = apiClient.GetMetersByCustomer(apiCustomer.Item1.Customer_No);

                        string token = "";
                        string prismDeviceID = "";
                        string serial = "";
                        var skybillCustomerLive = apiClient.GetCustomer(apiCustomer.Item1.Customer_No);

                        decimal amountToRedeem = Convert.ToDecimal(skybillCustomerLive.Balance_LCY) * -1;

                        if (amountToRedeem > 0)
                        {
                            string errorMessage = "";
                            string contactorState = "";
                            string deviceStatus = "";

                            DateTime? meterStatusTime = null;

                            foreach (var meter in meters)
                            {
                                var prismDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                                prismDeviceID = prismDevice.id.ToString();
                                if (prismDevice != null && String.Compare(prismDevice.type.type, "elec", true) == 0)
                                {
                                    token = _prismApiClient.GenerateToken(meter.Serial_No, Convert.ToDouble(amountToRedeem), "Unipin - AutoRedeem", amountToRedeem, "", "");
                                    serial = meter.Serial_No;

                                    if (prismDevice.status != null)
                                        meterStatusTime = prismDevice.status.time;
                                    deviceStatus = prismDevice.deviceStatus;

                                    //#region Contactor State

                                    //Dictionary<int, string> registers = new Dictionary<int, string>();
                                    //registers.Add(91, "readings");

                                    //DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                                    //DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                                    //var deviceContactorStateData = _client.GetMeterUsage(meter.Serial_No, startTime, endTime, 900, registers);

                                    //if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                                    //{
                                    //    errorMessage = "Meter usage not found";
                                    //}
                                    //else
                                    //{
                                    //    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                    //    if (validreadings == null || validreadings.Count == 0)
                                    //    {
                                    //        errorMessage = "Meter usage not found";
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
                                    //        errorMessage = "Invalid Contactor State";
                                    //    }
                                    //}

                                    //#endregion

                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(token))
                            {
                                // First send
                                _client.MeterSTS(token, prismDeviceID, "Unipin - AutoRedeem", amountToRedeem, errorMessage, deviceStatus, meterStatusTime, deviceApiID: 2);

                                // Send 3 times
                                System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, prismDeviceID, "Unipin - AutoRedeem", amountToRedeem, errorMessage, serial));
                                thread.Start();

                                #region Journal Entry

                                SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();

                                journal.Posting_DateSpecified = true;
                                journal.Posting_Date = DateTime.UtcNow.Date;
                                journal.Document_TypeSpecified = true;
                                journal.Document_Type = SalesJournal.Document_Type.Invoice;
                                journal.Account_TypeSpecified = true;
                                journal.Account_Type = SalesJournal.Account_Type.Customer;
                                journal.Account_No = apiCustomer.Item1.Customer_No;
                                journal.AmountSpecified = true;
                                journal.Description = $"Balance Redeem - {apiCustomer.Item1.Customer_No}";
                                journal.Amount = Convert.ToDecimal(amountToRedeem);
                                journal.Bal_Account_TypeSpecified = true;
                                journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                                journal.Bal_Account_No = "6610";

                                SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                                skyBillApiClient.CreateJournalEntry(apiCustomer.Item2, apiCustomer.Item1.Customer_No, journal, db, "");

                                #endregion

                                skybillCustomerLive = apiClient.GetCustomer(apiCustomer.Item1.Customer_No);
                                var updatedBalance = skybillCustomerLive.Balance_LCY * -1;

                                // Sent a token
                                prepaidTokenGenerated = token;
                                sendPrepaidMessageText = true;

                                // Check if customer exists and send the token sms
                                if (localCustomer != null && localCustomer.CustomerNumber == skybillCustomerLive.Customer_No)
                                {
                                    string phoneNumber = localCustomer.NotificationPhoneNumber;
                                    if (!string.IsNullOrEmpty(phoneNumber))
                                    {
                                        StringBuilder sbSMS = new StringBuilder();
                                        sbSMS.Append($"Token:{token}.");
                                        sbSMS.Append($"{Environment.NewLine}Purchased: R {result.LoadedAmount:N}");
                                        sbSMS.Append($"{Environment.NewLine}Other charges: R {(result.LoadedAmount - Convert.ToDecimal(amountToRedeem)):N}");
                                        sbSMS.Append($"{Environment.NewLine}Balance converted: R {amountToRedeem:N}");
                                        sbSMS.Append($"{Environment.NewLine}Help: 0870572561");
                                        sbSMS.Append($"{Environment.NewLine}Ref: {purchase.Id}");

                                        SMS.SendSms("27" + phoneNumber.Remove(0, 1), sbSMS.ToString());
                                    }
                                }
                            }
                            else
                            {
                                System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(serial, Convert.ToDouble(amountToRedeem), "Unipin", amountToRedeem, ""));
                                thread.Start();

                                //string emailBody = $"There was an error generating token for {serial} - Payment of {result.LoadedAmount:N}";
                                //EmailSender emailSender = new EmailSender();
                                //await emailSender.SendEmailAsync(new string[] {
                                //                "rose@myvoltage.co.za",
                                //                "riaan@myvoltage.co.za",
                                //                "lendl@myvoltage.co.za",
                                //                "nic@myvoltage.co.za",
                                //                }
                                //, "Token Generation Error - Unipin"
                                //, emailBody
                                //, emailBody);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }

                result.Balance = CustomerProvider.GetMyWalletBalance(_cache, apiCustomer.Item2.Name, apiCustomer.Item1.Customer_No);
                result.Reference = purchase.Id;
                result.ResponseStatus = 0;

                // USSD REPLY MESSAGE
                // Sent a prepaid token, change text
                if (sendPrepaidMessageText)
                {
                    result.Tokens.Add(prepaidTokenGenerated);
                    result.Message = $"Token: {prepaidTokenGenerated}.";
                }
                else
                {
                    result.Tokens.Add("");
                    result.Message = $"Balance: R {result.Balance:N}.";
                }

                if (result.Message.Length > 160)
                    result.Message = result.Message.Substring(0, 160);

                SaveUniPinResult(purchase, result, "");
            }


            return Ok(result);
        }

        public void CreateVendingJournals(UniPinRequest purchase, UniPinResult result, Company company, MyVoltage.Api.SkyBill.Customer customer)
        {
            try
            {
                var db = new MyVoltageDbContext(_options);
                var vendingCompany = db.Companies.Where(p => p.CompanyID == 132).SingleOrDefault();
                string vendingCustomerNo = company.Name.Substring(0, 3) + "-U";
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                if (company.CompanyID == 13)
                {
                    vendingCustomerNo = "000-U";
                }

                StringBuilder fullErrorMessage = new StringBuilder();

                var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.Unipin, result.LoadedAmount, purchase.Id);

                #region _0_2_journalUnipinCOS


                skyBillApiClient.CreateJournalEntry(company, customer.Customer_No, new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = DateTime.UtcNow.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "7191",
                    AmountSpecified = true,
                    Description = $"{purchase.Id} - Unipin: {result.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%",
                    Amount = (result.LoadedAmount * bankCharges.MVVendingCommission),
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "CIGICELL",
                }, db, "");

                #endregion

                #region _0_3_journalVendingFull - AmountLoadedByCustomer
                // Done and tested, problem with PostReceiptJournalAsync2 (Making them "pending" posts)

                skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = DateTime.UtcNow.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.Customer,
                    Account_No = vendingCustomerNo,
                    AmountSpecified = true,
                    Description = purchase.Id + " - Payment",
                    Amount = result.LoadedAmount * -1,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "CIGICELL",
                }, db, "");

                #endregion

                #region _0_4_journalVendingCommission - VendingCommission (5%)

                skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = DateTime.UtcNow.Date,
                    Document_TypeSpecified = true,
                    Document_Type = SalesJournal.Document_Type.Invoice,
                    Account_TypeSpecified = true,
                    Account_Type = SalesJournal.Account_Type.Customer,
                    Account_No = vendingCustomerNo,
                    AmountSpecified = true,
                    Description = purchase.Id.ToString() + " - Fee",
                    Amount = (result.LoadedAmount * bankCharges.MVVendingCommission),
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                    Bal_Account_No = "6810",
                }, db, "");

                #endregion

                #region _0_1_journalUnipinExternalVendingFees

                skyBillApiClient.CreateJournalEntry(vendingCompany, "", new ServiceReference1.CashReceiptJournal()
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
                    Bal_Account_No = "CIGICELL",
                }, db, "");

                #endregion

            }
            catch (Exception ex)
            {
                result.Message = ex.ToString();
                result.ResponseStatus = 302;
                result.ResponseStatusText = "Internal error NP";

                string emailBody = $"There was an error creating Journals for {result.MeterNumber} - R{result.PaidAmount:N}<br>{ex}";
                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "madelyn@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                , "302 Error - Unipin"
                , emailBody
                , emailBody);

                // SMS
                // Zelda Company Name, Customer Number
                SMS.SendSms("27784574259", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                SMS.SendSms("27824443755", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");
                SMS.SendSms("27837816268", $"UNIPIN ERROR {result.MeterNumber} - R {result.PaidAmount:N}");

                SaveUniPinResult(purchase, result);
            }


        }

        [HttpPost]
        [Route("load")]
        public async Task<ActionResult> Load([FromBody] UniPinLoad request)
        {
            UniPinResult result = new UniPinResult();

            if (request == null)
            {
                result.ResponseStatus = 206;

                return Ok(result);
            }

            if (!IsSignatureMatch<UniPinLoad>(request.Hash, request))
            {
                result.ResponseStatus = 206;

                return Ok(result);
            }

            if (String.IsNullOrEmpty(request.DateTime) || String.IsNullOrEmpty(request.Id) || String.IsNullOrEmpty(request.Hash))
            {
                result.ResponseStatus = 206;

                return Ok(result);
            }

            DateTime transDate = DateTime.Now;

            if (!DateTime.TryParse(request.DateTime, out transDate))
            {
                result.ResponseStatus = 206;

                return Ok(result);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                var unipin = (from tbl in db.UniPins
                              where tbl.ReferenceID == request.Id
                              select tbl).FirstOrDefault();

                if (unipin == null)
                {
                    result.ResponseStatus = 206;

                    return Ok(result);
                }

                result.UserAddress = unipin.UserAddress;
                result.Balance = unipin.Balance;
                result.ConvenienceFee = unipin.ConvenienceFee;
                result.LoadedAmount = unipin.LoadedAmount;
                result.MeterNumber = unipin.MeterNumber;
                result.UserName = unipin.UserName;
                result.PaidAmount = unipin.PaidAmount;
                result.Reference = unipin.ReferenceID;
                result.ResponseStatus = unipin.ResponseStatus;

                return Ok(result);
            }
        }

        [AllowAnonymous]
        [Route("/testUnipin/{amount}")]
        public async Task<ActionResult> TestUnipin(decimal amount)
        {
            //var loggedInUser = await _userManager.GetUserAsync(User);

            //if (loggedInUser.Email != _configuration["AppSettings:MasterOperationalEmail"])
            //    return NotFound();

            //UniPinRequest purchase = new UniPinRequest()
            //{
            //    AmountInCents = Convert.ToInt32(amount * 100).ToString(),
            //    DateTime = DateTime.Now.ToString(),
            //    Hash = "",
            //    Id = DateTime.Now.ToString("yyyyMMddHHmmss"),
            //    MeterNumber = "14293422060"
            //};

            //StringBuilder builder = new StringBuilder();
            //Type type = purchase.GetType();

            //BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            //PropertyInfo[] properties = type.GetProperties(flags);

            //foreach (var prop in properties)
            //{
            //    if (String.Compare(prop.Name, "Hash", true) == 0)
            //        continue;

            //    string value = prop.GetValue(purchase, null).ToString();
            //    builder.Append(value);
            //}

            //string message = builder.ToString();

            //string key = "14ea293d-0034-4830-af00-323ed2744839";
            //string serverSignature = Hash(message + key);

            //purchase.Hash = serverSignature;

            //return await Request2(purchase);

            return Ok();
        }



        private bool IsSignatureMatch<T>(string signature, T model)
        {
            StringBuilder builder = new StringBuilder();

            Type type = model.GetType();

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            PropertyInfo[] properties = type.GetProperties(flags);

            foreach (var prop in properties)
            {
                if (String.Compare(prop.Name, "Hash", true) == 0)
                    continue;

                string value = prop.GetValue(model, null).ToString();
                builder.Append(value);
            }

            string message = builder.ToString();

            string key = "14ea293d-0034-4830-af00-323ed2744839";
            string serverSignature = Hash(message + key);

            if (String.Compare(signature, serverSignature, true) == 0)
                return true;
            else
                return false;
        }

        private string Hash(string message)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(message));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        private void SaveUniPinResult(UniPinRequest request, UniPinResult result, string userID = "")
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var unipinTbl = new UniPin();

                unipinTbl.Amount = Int32.Parse(request.AmountInCents) / 100m;
                unipinTbl.CreateDate = DateTime.Now;
                unipinTbl.MeterNumber = result.MeterNumber;
                unipinTbl.ReferenceID = request.Id;
                unipinTbl.ResponseStatus = result.ResponseStatus;
                unipinTbl.RequestDate = DateTime.Parse(request.DateTime);
                unipinTbl.UserAddress = result.UserAddress;
                unipinTbl.UserName = result.UserName;
                unipinTbl.UserID = userID;
                unipinTbl.Message = result.Message;
                unipinTbl.LoadedAmount = result.LoadedAmount;
                unipinTbl.ConvenienceFee = result.ConvenienceFee;
                unipinTbl.PaidAmount = result.PaidAmount;

                db.UniPins.Add(unipinTbl);

                db.SaveChanges();
            }
        }

        private Tuple<MyVoltage.Api.SkyBill.Customer, Company> FindCustomer(string meterNo)
        {
            foreach (var company in _companies)
            {
                SkyBillApiClient apiClient = new SkyBillApiClient(company.Name, _cache);

                var apiCustomer = apiClient.GetCustomerByMeterNumber(meterNo);

                if (apiCustomer != null)
                    return new Tuple<MyVoltage.Api.SkyBill.Customer, Company>(apiCustomer, company);
            }

            return null;
        }
    }
}
