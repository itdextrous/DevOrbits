using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Prism;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UsageController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerProvider _customerProvider;
        private readonly BillingProvider _billingProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;

        public UsageController(IMemoryCache cache, UserManager<ApplicationUser> userManager, DbContextOptions<Data.MyVoltageDbContext> options, CustomerProvider customerProvider, BillingProvider billingProvider, IHttpContextAccessor contextAccessor, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IConfiguration configuration)
        {
            _userManager = userManager;
            _options = options;
            _customerProvider = customerProvider;
            _billingProvider = billingProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/highusagecalc")]
        public async Task<IActionResult> HighUsageCalc()
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var user = _userManager.GetUserAsync(User).Result;
            var customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();

            if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();

            if (customer == null)
                return Redirect("/dashboard");

            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();

            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
            var customerMeters = client.GetMetersByCustomer(customer.CustomerNumber);

            List<HighUsageMeterItem> meters = new List<HighUsageMeterItem>();

            foreach (var meter in customerMeters)
            {
                var existing = meters.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                if (existing == null)
                {
                    var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                    meters.Add(new HighUsageMeterItem()
                    {
                        Description = m2mDevice.name,
                        Serial = meter.Serial_No,
                        Status = m2mDevice.deviceStatus,
                        Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
                    });
                }
            }

            HighUsageViewModel model = new HighUsageViewModel()
            {
                Customer = customer,
                Meters = meters
            };

            return View("~/Views/Usage/HighUsageCalc.cshtml", model);
        }

        [HttpPost]
        [Route("/highusagecalc")]
        public async Task<IActionResult> HighUsageCalc(HighUsageViewModel model)
        {
            var serial = Request.Form["Meter"];
            if (string.IsNullOrEmpty(serial))
                return Redirect("/highusagecalc");
            else
                return Redirect($"/highusagecalcstep2/{serial}");
        }

        [HttpGet]
        [Route("/highusagecalcstep2/{serial}/{year?}/{month?}")]
        public async Task<IActionResult> HighUsageCalcStep2(string serial, string year, string month)
        {
            var readingDate = DateTime.Now;

            if (year != null && month != null)
            {
                try
                {
                    readingDate = new DateTime(Int32.Parse(year), Int32.Parse(month), readingDate.Day);
                }
                catch (Exception e)
                {
                    readingDate = new DateTime(Int32.Parse(year), Int32.Parse(month), DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)));
                }
            }

            List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(serial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);

            int multiplier = -1;

            if (_customerProvider.AccountType == (int)Data.AccountTypeEnum.PostPaid)
                multiplier = 1;

            Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

            HighUsageMeterItem highUsageMeterItem = new HighUsageMeterItem()
            {
                Description = m2mDevice.name,
                Serial = serial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                MeterColor = m2mDevice.type.colorType
            };

            HighUsageStep2ViewModel model = new HighUsageStep2ViewModel()
            {
                Meter = highUsageMeterItem,
                BackURL = "/highusagecalc",
                ReadingDate = readingDate,
                MonthlyTotal = monthlyTotal
            };

            return View("~/Views/Usage/HighUsageCalcStep2.cshtml", model);
        }

        [HttpGet]
        [Route("/highusagecalcstep3/{serial}")]
        public async Task<IActionResult> HighUsageCalcStep3(string serial)
        {
            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

            HighUsageMeterItem highUsageMeterItem = new HighUsageMeterItem()
            {
                Description = m2mDevice.name,
                Serial = serial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                MeterColor = m2mDevice.type.colorType
            };

            HighUsageStep3ViewModel model = new HighUsageStep3ViewModel()
            {
                Meter = highUsageMeterItem,
                BackURL = $"/highusagecalcstep2/{serial}"
            };

            return View("~/Views/Usage/HighUsageCalcStep3.cshtml", model);
        }

        [Route("/highusagecalc/switchoff/{serial}")]
        public async Task<IActionResult> HighUsageCalcSwitchOff(string serial)
        {
            return Content("{\"result\":true}", "application/json");
            try
            {
                using (var db = new Data.MyVoltageDbContext(_options))
                {
                    var user = _userManager.GetUserAsync(User).Result;
                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);
                    var skybillCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);
                    float balance = skybillCustomer.Balance_LCY * -1;

                    if (m2mDevice == null
                        || skybillCustomer == null)
                        return View("~/Views/Meter/SwitchMeter.cshtml");

                    #region Contactor State

                    Dictionary<int, string> registers = new Dictionary<int, string>();
                    registers.Add(91, "readings");

                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                    DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                    string errorMessage = "";
                    var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                    string contactorState = "";

                    if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                        if (validreadings == null || validreadings.Count == 0)
                        {
                            errorMessage = "Meter usage not found";
                        }
                        else
                        {
                            contactorState = validreadings[validreadings.Count - 1].ToString();
                        }
                    }

                    bool isContactorConnected = false;

                    if (!string.IsNullOrEmpty(contactorState))
                    {
                        try
                        {
                            if (Convert.ToInt32(contactorState) == 1)
                                isContactorConnected = true;
                            else if (Convert.ToInt32(contactorState) == 0)
                                isContactorConnected = false;
                            errorMessage = contactorState;
                        }
                        catch (Exception ex)
                        {
                            errorMessage = "Invalid Contactor State";
                        }
                    }

                    #endregion

                    DateTime? meterStatusTime = null;
                    if (m2mDevice.status != null)
                        meterStatusTime = m2mDevice.status.time;

                    if (isContactorConnected)
                    {
                        var _prismApiClient = new PrismVendClient(_options);

                        if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                        {
                            var token = _prismApiClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "HU Switch Meter", Convert.ToDecimal(balance), "", user.Id);
                            if (!string.IsNullOrEmpty(token))
                            {
                                _client.MeterSTS(token, m2mDevice.id.ToString(), "HU Switch Meter", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);

                                System.Threading.Thread thread = new System.Threading.Thread(() => SleepThenReconnectMeter(serial, customer.CustomerNumber, _customerProvider.CompanyName));
                                thread.Start();
                            }
                            else
                            {
                                string emailBody = $"There was an error generating token for {serial}";
                                EmailSender emailSender = new EmailSender();
                                await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                                , "Token Generation Error - HU Switch Meter"
                                , emailBody
                                , emailBody);
                            }

                        }
                    }
                }
                return Content("{\"result\":true}", "application/json");
            }
            catch
            {
                return Content("{\"result\":false}", "application/json");
            }
        }

        public void SleepThenReconnectMeter(string serialNo, string customerNo, string companyName)
        {
            int oneSec = 1000;
            int oneMin = oneSec * 60;
            int sleepDuration = oneMin * 3;

            System.Threading.Thread.Sleep(sleepDuration);

            var m2mDevice = _client.GetDeviceByMeterNumber(serialNo);
            MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(companyName, _cache);
            var skybillCustomer = skyBillApiClient.GetCustomer(customerNo);
            float balance = skybillCustomer.Balance_LCY * -1;

            var _prismApiClient = new PrismVendClient(_options);

            if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                string contactorState = "";

                if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                {
                    errorMessage = "Meter usage not found";
                }
                else
                {
                    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                    if (validreadings == null || validreadings.Count == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        contactorState = validreadings[validreadings.Count - 1].ToString();
                    }
                }

                bool isContactorConnected = false;

                if (!string.IsNullOrEmpty(contactorState))
                {
                    try
                    {
                        if (Convert.ToInt32(contactorState) == 1)
                            isContactorConnected = true;
                        else if (Convert.ToInt32(contactorState) == 0)
                            isContactorConnected = false;
                        errorMessage = contactorState;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = "Invalid Contactor State";
                    }
                }

                #endregion

                DateTime? meterStatusTime = null;
                if (m2mDevice.status != null)
                    meterStatusTime = m2mDevice.status.time;

                var token = _prismApiClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serialNo, 0, "HU Switch Meter Reconnect", Convert.ToDecimal(balance), "", "");
                if (!string.IsNullOrEmpty(token))
                {
                    _client.MeterSTS(token, m2mDevice.id.ToString(), "HU Switch Meter Reconnect", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);
                }
                else
                {
                    string emailBody = $"There was an error generating token for {serialNo}";
                    EmailSender emailSender = new EmailSender();
                    emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , "Token Generation Error - HUSwitchMeterReconnect"
                    , emailBody
                    , emailBody);
                }

            }

        }

        [Route("/highusagecalc/switchon/{serial}")]
        public async Task<IActionResult> HighUsageCalcSwitchOn(string serial)
        {
            return Content("{\"result\":true}", "application/json");
            try
            {
                Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

                var user = _userManager.GetUserAsync(User).Result;
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var skybillCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);
                float balance = skybillCustomer.Balance_LCY * -1;

                var _prismApiClient = new PrismVendClient(_options);

                if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                {
                    var token = _prismApiClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "HU Switch Meter Reconnect", Convert.ToDecimal(balance), "", "");
                    if (!string.IsNullOrEmpty(token))
                    {
                        #region Contactor State

                        Dictionary<int, string> registers = new Dictionary<int, string>();
                        registers.Add(91, "readings");

                        DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                        DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                        string errorMessage = "";
                        var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                        string contactorState = "";

                        if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                        {
                            errorMessage = "Meter usage not found";
                        }
                        else
                        {
                            var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                            if (validreadings == null || validreadings.Count == 0)
                            {
                                errorMessage = "Meter usage not found";
                            }
                            else
                            {
                                contactorState = validreadings[validreadings.Count - 1].ToString();
                            }
                        }

                        bool isContactorConnected = false;

                        if (!string.IsNullOrEmpty(contactorState))
                        {
                            try
                            {
                                if (Convert.ToInt32(contactorState) == 1)
                                    isContactorConnected = true;
                                else if (Convert.ToInt32(contactorState) == 0)
                                    isContactorConnected = false;
                                errorMessage = contactorState;
                            }
                            catch (Exception ex)
                            {
                                errorMessage = "Invalid Contactor State";
                            }
                        }

                        #endregion

                        DateTime? meterStatusTime = null;
                        if (m2mDevice.status != null)
                            meterStatusTime = m2mDevice.status.time;

                        _client.MeterSTS(token, m2mDevice.id.ToString(), "HU Switch Meter Reconnect", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);
                    }
                    else
                    {
                        string emailBody = $"There was an error generating token for {serial}";
                        EmailSender emailSender = new EmailSender();
                        emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                        , "Token Generation Error - HUSwitchMeterReconnect"
                        , emailBody
                        , emailBody);
                    }

                }

                return Content("{\"result\":true}", "application/json");
            }
            catch
            {
                return Content("{\"result\":false}", "application/json");
            }
        }

        [HttpGet]
        [Route("/highusagecalcstep4/{serial}")]
        public async Task<IActionResult> HighUsageCalcStep4(string serial)
        {
            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

            HighUsageMeterItem highUsageMeterItem = new HighUsageMeterItem()
            {
                Description = m2mDevice.name,
                Serial = serial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                MeterColor = m2mDevice.type.colorType
            };

            HighUsageStep4ViewModel model = new HighUsageStep4ViewModel()
            {
                Meter = highUsageMeterItem,
                BackURL = $"/highusagecalcstep2/{serial}"
            };

            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);

            DateTime startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime endOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            DateTime currentDate = DateTime.Now.Date;

            while (currentDate >= startOfThisMonth)
            {
                var salesJournals = client.GetSalesInvoiceLinesByCustomer(_customerProvider.CustomerNumber, currentDate.AddDays(-1), currentDate.AddDays(1));


                var latestJournal = (from p in salesJournals
                                     where p.Meter_Serial_No == serial
                                     orderby p.Posting_Date descending
                                     select p).FirstOrDefault();

                if (latestJournal != null)
                {
                    model.LatestTariffDescription = $"{latestJournal.Description}";
                    break;
                }
                currentDate = currentDate.AddDays(-1);
            }

            return View("~/Views/Usage/HighUsageCalcStep4.cshtml", model);
        }

        [HttpGet]
        [Route("/usagecalc")]
        public async Task<IActionResult> UsageCalc()
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var user = _userManager.GetUserAsync(User).Result;
            var customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();

            if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();


            if (customer == null)
                return Redirect("/dashboard");

            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();

            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
            var customerMeters = client.GetMetersByCustomer(customer.CustomerNumber);

            List<UsageCalcMeterItem> meters = new List<UsageCalcMeterItem>();

            foreach (var meter in customerMeters)
            {
                var existing = meters.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                if (existing == null)
                {
                    var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                    meters.Add(new UsageCalcMeterItem()
                    {
                        Description = m2mDevice.name,
                        Serial = meter.Serial_No,
                        Status = m2mDevice.deviceStatus,
                        Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
                    });
                }
            }

            UsageCalcViewModel model = new UsageCalcViewModel()
            {
                Customer = customer,
                Meters = meters
            };

            if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                model.UsageCalcs = db.UsageCalcs.Where(p => p.UserID == user.Id).ToList();
            else
            {
                model.UsageCalcs = db.UsageCalcs.Where(p => p.CompanyID == company.CompanyID).ToList();
            }

            return View("~/Views/Usage/UsageCalc.cshtml", model);
        }

        [HttpGet]
        [Route("/usagecalccreate/{serial}")]
        public async Task<IActionResult> UsageCalcCreate(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return Redirect("/usagecalc");

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var user = _userManager.GetUserAsync(User).Result;
            var customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();

            if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();

            if (customer == null)
                return Redirect("/dashboard");

            UsageCalcCreateModel model = new UsageCalcCreateModel()
            {
                Customer = customer,
                BackURL = "/usagecalc"
            };

            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = serial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            model.UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();

            return View("~/Views/Usage/UsageCalcCreate.cshtml", model);
        }

        [HttpPost]
        [Route("/usagecalccreate/{serial}")]
        public async Task<IActionResult> UsageCalcCreate(string serial, UsageCalcCreateModel model)
        {
            if (string.IsNullOrEmpty(serial))
                return Redirect("/usagecalc");

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var user = _userManager.GetUserAsync(User).Result;
            var customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();

            if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();

            if (customer == null)
                return Redirect("/dashboard");

            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();

            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = serial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            var UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();
            Dictionary<int, Tuple<int, int>> assetsFilledIn = new Dictionary<int, Tuple<int, int>>();
            foreach (var asset in UsageCalc_Assets)
            {
                int quantity = 0;
                try { quantity = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Quantity"]); }
                catch { }
                int hoursUsed = 0;
                try { hoursUsed = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Hours"]); }
                catch { }

                assetsFilledIn.Add(asset.UsageCalc_AssetID, new Tuple<int, int>(quantity, hoursUsed));
            }

            if (assetsFilledIn.Count == 0)
                return View("~/Views/Usage/UsageCalcCreate.cshtml", model);
            else
            {
                Data.UsageCalc calc = new Data.UsageCalc()
                {
                    CreateDate = DateTime.Now,
                    MeterSerial = serial,
                    UserID = user.Id,
                    CompanyID = company.CompanyID
                };

                db.UsageCalcs.Add(calc);
                db.SaveChanges();

                #region Get Units to be used for calc

                decimal unitForFirstOfMonth = 0;
                decimal unitForLastOfMonth = 0;
                decimal unitForMiddleOfMonth = 0;

                DateTime startOfTheMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
                DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
                DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

                MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var invoices = client.GetTenantConsumptionInvoicePerDay(customer.CustomerNumber, serial, startOfTheMonth);

                var firstInvoiceOfMonth = (from p in invoices
                                           where p.CurrentDate <= startOfTheMonth
                                           orderby p.CurrentDate descending
                                           select p).FirstOrDefault();
                if (firstInvoiceOfMonth != null)
                    unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

                var middleInvoiceOfMonth = (from p in invoices
                                            where p.CurrentDate <= middleOfMonth
                                            orderby p.CurrentDate descending
                                            select p).FirstOrDefault();
                if (middleInvoiceOfMonth != null)
                    unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

                var endInvoiceOfMonth = (from p in invoices
                                         where p.CurrentDate <= endOfMonth
                                         orderby p.CurrentDate descending
                                         select p).FirstOrDefault();
                if (endInvoiceOfMonth != null)
                    unitForLastOfMonth = endInvoiceOfMonth.Tariff;


                #endregion

                foreach (var asset in assetsFilledIn)
                {
                    Data.UsageCalc_LinkedAsset linkedAsset = new Data.UsageCalc_LinkedAsset()
                    {
                        HoursRunningPerDay = asset.Value.Item2,
                        Quantity = asset.Value.Item1,
                        UnitForFirstOfMonth = unitForFirstOfMonth,
                        UnitForLastOfMonth = unitForLastOfMonth,
                        UnitForMiddleOfMonth = unitForMiddleOfMonth,
                        UsageCalc_AssetID = asset.Key,
                        UsageCalcID = calc.UsageCalcID
                    };

                    db.UsageCalc_LinkedAssets.Add(linkedAsset);
                    db.SaveChanges();
                }

                return Redirect($"/usagecalcedit/{calc.UsageCalcID}");
            }
        }

        [HttpGet]
        [Route("/usagecalcedit/{Id}")]
        public async Task<IActionResult> UsageCalcEdit(int Id)
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var usagecalc = db.UsageCalcs.Where(p => p.UsageCalcID == Id).SingleOrDefault();
            if (usagecalc == null)
                return Redirect("/usagecalc");

            var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == usagecalc.MeterSerial).FirstOrDefault();
            var user = _userManager.GetUserAsync(User).Result;


            Data.Customer customer = null;
            if (skybillCustomer == null)
            {
                customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();
                if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                    customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
            }
            else
            {
                customer = db.Customers.Where(p => p.CustomerNumber == skybillCustomer.Customer_No && !p.IsDeleted).SingleOrDefault();
            }

            if (customer == null)
                return Redirect("/dashboard");


            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();

            UsageCalcEditModel model = new UsageCalcEditModel()
            {
                Customer = customer,
                BackURL = "/usagecalc",
                UsageCalc = usagecalc,
                UsageCalc_LinkedAssets = new List<UsageCalcLinkedAssetItem>(),
                MonthUsedForTariff = usagecalc.UpdateDate.HasValue ? usagecalc.UpdateDate.Value.AddMonths(-1) : usagecalc.CreateDate.AddMonths(-1)
            };

            #region Get Units to be used for calc

            decimal unitForFirstOfMonth = 0;
            decimal unitForLastOfMonth = 0;
            decimal unitForMiddleOfMonth = 0;

            DateTime startOfTheMonth = new DateTime(model.MonthUsedForTariff.Year, model.MonthUsedForTariff.Month, 1);
            DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
            DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var invoices = client.GetTenantConsumptionInvoicePerDay(customer.CustomerNumber, usagecalc.MeterSerial, startOfTheMonth);

            var firstInvoiceOfMonth = (from p in invoices
                                       where p.CurrentDate <= startOfTheMonth
                                       && p.Tariff > 0
                                       orderby p.CurrentDate descending
                                       select p).FirstOrDefault();

            if (firstInvoiceOfMonth != null)
                unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

            var middleInvoiceOfMonth = (from p in invoices
                                        where p.CurrentDate <= middleOfMonth
                                        && p.Tariff > 0
                                        orderby p.CurrentDate descending
                                        select p).FirstOrDefault();
            if (middleInvoiceOfMonth != null)
                unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

            var endInvoiceOfMonth = (from p in invoices
                                     where p.CurrentDate <= endOfMonth
                                     && p.Tariff > 0
                                     orderby p.CurrentDate descending
                                     select p).FirstOrDefault();

            if (endInvoiceOfMonth != null)
                unitForLastOfMonth = endInvoiceOfMonth.Tariff;

            model.ActualTotalConsumptionPerMonth = invoices.Select(p => p.Consumption).Sum();

            model.UnitForFirstOfTheMonth = unitForFirstOfMonth;
            model.UnitForLastOfTheMonth = unitForLastOfMonth;
            model.UnitForMiddleOfTheMonth = unitForMiddleOfMonth;


            #endregion


            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                model.UsageCalc_LinkedAssets.Add(new UsageCalcLinkedAssetItem()
                {
                    HoursRunningPerDay = item.HoursRunningPerDay,
                    Quantity = item.Quantity,
                    UnitForFirstOfMonth = item.UnitForFirstOfMonth,
                    UnitForLastOfMonth = item.UnitForLastOfMonth,
                    UnitForMiddleOfMonth = item.UnitForMiddleOfMonth,
                    UsageCalcID = item.UsageCalcID,
                    UsageCalc_Asset = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == item.UsageCalc_AssetID).SingleOrDefault(),
                    UsageCalc_AssetID = item.UsageCalc_AssetID,
                    UsageCalc_LinkedAssetID = item.UsageCalc_LinkedAssetID,
                });
            }

            var m2mDevice = _client.GetDeviceByMeterNumber(usagecalc.MeterSerial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = usagecalc.MeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            return View("~/Views/Usage/UsageCalcEdit.cshtml", model);
        }

        [HttpPost]
        [Route("/usagecalcedit/{Id}")]
        public async Task<IActionResult> UsageCalcEdit(int Id, UsageCalcEditModel model)
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);


            var usagecalc = db.UsageCalcs.Where(p => p.UsageCalcID == Id).SingleOrDefault();
            if (usagecalc == null)
                return Redirect("/usagecalc");

            var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == usagecalc.MeterSerial).FirstOrDefault();
            var user = _userManager.GetUserAsync(User).Result;


            Data.Customer customer = null;
            if (skybillCustomer == null)
            {
                customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();
                if (User.IsInRole(Data.UserRoleEnum.CompanyAdmin.ToString()))
                    customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
            }
            else
            {
                customer = db.Customers.Where(p => p.CustomerNumber == skybillCustomer.Customer_No && !p.IsDeleted).SingleOrDefault();
            }

            if (customer == null)
                return Redirect("/dashboard");


            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();

            var m2mDevice = _client.GetDeviceByMeterNumber(usagecalc.MeterSerial);

            var UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();
            Dictionary<int, Tuple<int, int>> assetsFilledIn = new Dictionary<int, Tuple<int, int>>();
            foreach (var asset in UsageCalc_Assets)
            {
                int quantity = 0;
                try { quantity = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Quantity"]); }
                catch { }
                int hoursUsed = 0;
                try { hoursUsed = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Hours"]); }
                catch { }

                assetsFilledIn.Add(asset.UsageCalc_AssetID, new Tuple<int, int>(quantity, hoursUsed));
            }


            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = usagecalc.MeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            usagecalc.UpdateDate = DateTime.Now;
            usagecalc.UpdatedByID = user.Id;

            db.Update(usagecalc);
            db.SaveChanges();

            model.Customer = customer;
            model.BackURL = "/usagecalc";
            model.UsageCalc = usagecalc;
            model.UsageCalc_LinkedAssets = new List<UsageCalcLinkedAssetItem>();
            model.MonthUsedForTariff = usagecalc.UpdateDate.HasValue ? usagecalc.UpdateDate.Value.AddMonths(-1) : usagecalc.CreateDate.AddMonths(-1);

            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                #region Get Units to be used for calc

                decimal unitForFirstOfMonth = 0;
                decimal unitForLastOfMonth = 0;
                decimal unitForMiddleOfMonth = 0;

                DateTime startOfTheMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
                DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
                DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

                MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var invoices = client.GetTenantConsumptionInvoicePerDay(customer.CustomerNumber, usagecalc.MeterSerial, startOfTheMonth);

                var firstInvoiceOfMonth = (from p in invoices
                                           where p.CurrentDate <= startOfTheMonth
                                           && p.Tariff > 0
                                           orderby p.CurrentDate descending
                                           select p).FirstOrDefault();
                if (firstInvoiceOfMonth != null)
                    unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

                var middleInvoiceOfMonth = (from p in invoices
                                            where p.CurrentDate <= middleOfMonth
                                            && p.Tariff > 0
                                            orderby p.CurrentDate descending
                                            select p).FirstOrDefault();
                if (middleInvoiceOfMonth != null)
                    unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

                var endInvoiceOfMonth = (from p in invoices
                                         where p.CurrentDate <= endOfMonth
                                         && p.Tariff > 0
                                         orderby p.CurrentDate descending
                                         select p).FirstOrDefault();
                if (endInvoiceOfMonth != null)
                    unitForLastOfMonth = endInvoiceOfMonth.Tariff;

                model.ActualTotalConsumptionPerMonth = invoices.Select(p => p.Consumption).Sum();
                model.UnitForFirstOfTheMonth = unitForFirstOfMonth;
                model.UnitForLastOfTheMonth = unitForLastOfMonth;
                model.UnitForMiddleOfTheMonth = unitForMiddleOfMonth;

                #endregion

                #region Update Values

                var itemFromFrom = assetsFilledIn[item.UsageCalc_AssetID];

                if (item.Quantity != itemFromFrom.Item1)
                    item.Quantity = itemFromFrom.Item1;
                if (item.HoursRunningPerDay != itemFromFrom.Item2)
                    item.HoursRunningPerDay = itemFromFrom.Item2;

                if (item.UnitForFirstOfMonth != unitForFirstOfMonth)
                    item.UnitForFirstOfMonth = unitForFirstOfMonth;

                if (item.UnitForMiddleOfMonth != unitForMiddleOfMonth)
                    item.UnitForMiddleOfMonth = unitForMiddleOfMonth;

                if (item.UnitForLastOfMonth != unitForLastOfMonth)
                    item.UnitForLastOfMonth = unitForLastOfMonth;

                db.Update(item);
                db.SaveChanges();

                #endregion
            }

            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                model.UsageCalc_LinkedAssets.Add(new UsageCalcLinkedAssetItem()
                {
                    HoursRunningPerDay = item.HoursRunningPerDay,
                    Quantity = item.Quantity,
                    UnitForFirstOfMonth = item.UnitForFirstOfMonth,
                    UnitForLastOfMonth = item.UnitForLastOfMonth,
                    UnitForMiddleOfMonth = item.UnitForMiddleOfMonth,
                    UsageCalcID = item.UsageCalcID,
                    UsageCalc_Asset = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == item.UsageCalc_AssetID).SingleOrDefault(),
                    UsageCalc_AssetID = item.UsageCalc_AssetID,
                    UsageCalc_LinkedAssetID = item.UsageCalc_LinkedAssetID,
                });
            }

            return View("~/Views/Usage/UsageCalcEdit.cshtml", model);
        }



    }
}
