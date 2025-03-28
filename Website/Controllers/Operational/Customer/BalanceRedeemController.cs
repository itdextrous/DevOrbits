using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer.CustomerBalanceRedeemModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class BalanceRedeemController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;
        private PrismApiClient _prismApiClient;

        public BalanceRedeemController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _configuration = configuration;
            _userManager = userManager;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _prismApiClient = new PrismApiClient(options);
        }

        [Route("/operational/Customer/Customer_BalanceRedeem")]
        [HttpGet]
        public async Task<ActionResult> BalanceRedeem()
        {
            BalanceRedeemViewModel balanceRedeemViewModel = new BalanceRedeemViewModel();

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                var skybillCustomer = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);

                balanceRedeemViewModel.CurrentWalletBalance = skybillCustomer.Balance_LCY * -1;
            }

            return View("~/Views/Operational/Customer/BalanceRedeem/BalanceRedeem.cshtml", balanceRedeemViewModel);
        }

        [Route("/operational/Customer/Customer_BalanceRedeem")]
        [HttpPost]
        public async Task<ActionResult> BalanceRedeem(BalanceRedeemViewModel balanceRedeemViewModel)
        {
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var amount = Convert.ToDecimal(Request.Form["amount"]);

                var user = await _userManager.GetUserAsync(User);

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName);

                var balance = skybillCustomerDetails.Balance_LCY * -1;

                if (balance >= amount)
                {
                    PaymentProvider provider = new PaymentProvider(_configuration, _options);

                    #region Prism Token

                    string token = "";
                    string serial = "";
                    string prismDeviceID = "";
                    var meters = skyBillApiClient.GetMetersByCustomer(_operationalProvider.CustomerNumber);
                    string deviceStatus = "";
                    DateTime? meterStatusTime = null;

                    foreach (var meter in meters)
                    {
                        var prismDevice = _client.GetDeviceByMeterNumber(meter.Serial_No, 2);
                        if (prismDevice != null && String.Compare(prismDevice.type.type, "elec", true) == 0)
                        {
                            prismDeviceID = prismDevice.id.ToString();
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
                        #region Contactor State

                        //Dictionary<int, string> registers = new Dictionary<int, string>();
                        //registers.Add(91, "readings");

                        //DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                        //DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                        string errorMessage = "";
                        //var deviceContactorStateData = _client.GetMeterUsage(serial, startTime, endTime, 900, registers);
                        string contactorState = "";

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

                        //bool isContactorConnected = _client.IsDeviceContactorConnected(prismDeviceID, 2);

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

                        #endregion


                        _client.MeterSTS(token, prismDeviceID, "BalanceRedeem", amount, errorMessage, deviceStatus, meterStatusTime);

                        #region Journal Entry

                        SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();

                        journal.Posting_DateSpecified = true;
                        journal.Posting_Date = DateTime.UtcNow.Date;
                        journal.Document_TypeSpecified = true;
                        journal.Document_Type = SalesJournal.Document_Type.Invoice;
                        journal.Account_TypeSpecified = true;
                        journal.Account_Type = SalesJournal.Account_Type.Customer;
                        journal.Account_No = _operationalProvider.CustomerNumber;
                        journal.AmountSpecified = true;
                        journal.Description = $"Balance Redeem - {_operationalProvider.CustomerNumber}";
                        journal.Amount = amount;
                        journal.Bal_Account_TypeSpecified = true;
                        journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                        journal.Bal_Account_No = "6610";

                        SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);



                        var journalEntry = await provider.CreateSalesJournalEntry(new Company() { Name = _operationalProvider.CompanyName }, journal);

                        var getRecIdFromKey = await provider.GetSalesRecIdFromKey(journalEntry, new Company() { Name = _operationalProvider.CompanyName });

                        var postReceiptJournal = await provider.PostSalesReceiptJournalAsync(getRecIdFromKey, new Company() { Name = _operationalProvider.CompanyName });

                        #endregion

                        #region Refresh Balance

                        var skybillCustomer = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);
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

                        emailBody = emailBody + $"<br>CustomerNumber:{_operationalProvider.CustomerNumber}";

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

            }

            return View("~/Views/Operational/Customer/BalanceRedeem/BalanceRedeem.cshtml", balanceRedeemViewModel);
        }

    }
}
