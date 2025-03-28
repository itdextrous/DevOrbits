using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.Prism;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer.CustomerProtestToolModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class ProtestToolController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;
        private PrismApiClient _prismApiClient;
        private readonly OperationalBillingProvider _billingProvider;

        public ProtestToolController(IConfiguration configuration,
            OperationalBillingProvider billingProvider,
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
            _billingProvider = billingProvider;
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_ProtestTool/{year?}/{month?}")]
        public async Task<IActionResult> Customer_ProtestTool(string year, string month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Customer_ProtestToolModel model = new Customer_ProtestToolModel()
            {
                Meter = new Customer_ProtestToolMeterItem()
                {
                    Description = "",
                    MeterColor = "",
                    Serial = "",
                    Status = "",
                    Type = ""
                },
                MonthlyTotal = 0,
                ReadingDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
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

                List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(_operationalProvider.CustomerMeterSerial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_operationalProvider.AccountTypeForSelectedCustomer, _operationalProvider.ShowCostInclVAT);

                int multiplier = -1;

                if (_operationalProvider.AccountTypeForSelectedCustomer == Data.AccountTypeEnum.PostPaid)
                    multiplier = 1;

                Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


                var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

                Customer_ProtestToolMeterItem highUsageMeterItem = new Customer_ProtestToolMeterItem()
                {
                    Description = m2mDevice.name,
                    Serial = _operationalProvider.CustomerMeterSerial,
                    Status = m2mDevice.deviceStatus,
                    Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                    MeterColor = m2mDevice.type.colorType
                };

                model = new Customer_ProtestToolModel()
                {
                    Meter = highUsageMeterItem,
                    ReadingDate = readingDate,
                    MonthlyTotal = monthlyTotal
                };
            }

            return View("~/Views/Operational/Customer/ProtestTool/Customer_ProtestTool.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_ProtestTool_Step2")]
        public async Task<IActionResult> Customer_ProtestTool_Step2()
        {
            Customer_ProtestToolStep2Model model = new Customer_ProtestToolStep2Model()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

                Customer_ProtestToolMeterItem highUsageMeterItem = new Customer_ProtestToolMeterItem()
                {
                    Description = m2mDevice.name,
                    Serial = _operationalProvider.CustomerMeterSerial,
                    Status = m2mDevice.deviceStatus,
                    Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                    MeterColor = m2mDevice.type.colorType
                };

                model = new Customer_ProtestToolStep2Model()
                {
                    Meter = highUsageMeterItem
                };
            }

            return View("~/Views/Operational/Customer/ProtestTool/Customer_ProtestTool_Step2.cshtml", model);
        }

        [Route("/operational/Customer/Customer_ProtestTool/switchoff/{serial}")]
        public async Task<IActionResult> Customer_ProtestToolSwitchOff(string serial)
        {
            return Content("{\"result\":true}", "application/json");
            try
            {
                using (var db = new Data.MyVoltageDbContext(_options))
                {
                    var user = _userManager.GetUserAsync(User).Result;
                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                    var isContactorConnected = _client.IsDeviceContactorConnected(m2mDevice.id);
                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                    var skybillCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);
                    float balance = skybillCustomer.Balance_LCY * -1;

                    if (m2mDevice == null
                        || skybillCustomer == null)
                        return Content("{\"result\":false}", "application/json");

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

                                System.Threading.Thread thread = new System.Threading.Thread(() => SleepThenReconnectMeter(serial, customer.CustomerNumber, _operationalProvider.CompanyName));
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

        [Route("/operational/Customer/Customer_ProtestTool/switchon/{serial}")]
        public async Task<IActionResult> Customer_ProtestToolSwitchOn(string serial)
        {
            return Content("{\"result\":true}", "application/json");
            try
            {
                Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

                var user = _userManager.GetUserAsync(User).Result;
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var skybillCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);
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
                    var token = _prismApiClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "HU Switch Meter Reconnect", Convert.ToDecimal(balance), "", "");
                    if (!string.IsNullOrEmpty(token))
                    {
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
        [Route("/operational/Customer/Customer_ProtestTool_Step3")]
        public async Task<IActionResult> Customer_ProtestTool_Step3()
        {
            Customer_ProtestToolStep3Model model = new Customer_ProtestToolStep3Model()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

                Customer_ProtestToolMeterItem highUsageMeterItem = new Customer_ProtestToolMeterItem()
                {
                    Description = m2mDevice.name,
                    Serial = _operationalProvider.CustomerMeterSerial,
                    Status = m2mDevice.deviceStatus,
                    Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString(),
                    MeterColor = m2mDevice.type.colorType
                };

                model = new Customer_ProtestToolStep3Model()
                {
                    Meter = highUsageMeterItem,
                };

                MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                DateTime startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

                DateTime currentDate = DateTime.Now.Date;

                while (currentDate >= startOfThisMonth)
                {
                    var salesJournals = client.GetSalesInvoiceLinesByCustomer(_operationalProvider.CustomerNumber, currentDate.AddDays(-1), currentDate.AddDays(1));


                    var latestJournal = (from p in salesJournals
                                         where p.Meter_Serial_No == _operationalProvider.CustomerMeterSerial
                                         orderby p.Posting_Date descending
                                         select p).FirstOrDefault();

                    if (latestJournal != null)
                    {
                        model.LatestTariffDescription = $"{latestJournal.Description}";
                        break;
                    }
                    currentDate = currentDate.AddDays(-1);
                }
            }

            return View("~/Views/Operational/Customer/ProtestTool/Customer_ProtestTool_Step3.cshtml", model);
        }


    }
}
