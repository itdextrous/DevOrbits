using GemBox.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class ClientzoneProvider
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _context;
        public static readonly string SESSION_CUSTOMER_NUMBER = "CustomerNumber";
        public static readonly string SESSION_COMPANY_ID = "CompanyID";
        public static readonly string SESSION_CUSTOMER_METER_SERIAL = "CustomerMeterSerial";
        private readonly IDeviceApi _client;
        private readonly IMemoryCache _cache;

        #region ClientzoneProvider Cache Entries

        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS = "CLIENTZONEPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_USERCOMPANIES = "CLIENTZONEPROVIDER_CACHE_ENTRY_USERCOMPANIES";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_USERMETERSERIALS = "CLIENTZONEPROVIDER_CACHE_ENTRY_USERMETERSERIALS";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_CLIENTZONEPROFILE = "CLIENTZONEPROVIDER_CACHE_ENTRY_CLIENTZONEPROFILE";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY = "CLIENTZONEPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER = "CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALCUSTOMER = "CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALCUSTOMER";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALDEVICE = "CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALDEVICE";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_STATEMENT = "CLIENTZONEPROVIDER_CACHE_ENTRY_STATEMENT";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_MIRRORDEVICES = "CLIENTZONEPROVIDER_CACHE_ENTRY_MIRRORDEVICES";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_COMPANYMIRRORDEVICESCOUNT = "CLIENTZONEPROVIDER_CACHE_ENTRY_COMPANYMIRRORDEVICESCOUNT";
        public static readonly string CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLSERIALS = "CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLSERIALS";

        #endregion

        public ClientzoneProvider(UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            SelectPdf.GlobalProperties.LicenseKey = "58zWx9XS1sfe0dLH1dXJ18fU1snW1cne3t7e";
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _config = config;
            _context = context;
            AccountTypeForSelectedCustomer = AccountTypeEnum.Unknown;
            MeterTypeForUsage = MeterTypeEnum.None;
            ShowHourlyUsage = false;
            ShowDetailedDailyBilling = false;
            ShowCostInclVAT = false;
            ActivateTaxInvoice = false;
            OccupancyDate = DateTime.MinValue;
            CustomerMeterDeviceType = DeviceType.DeviceTypeEnum.Unknown;
            Meters = new List<MeterItem>();
            DeviceAPIIDValue = 1;

            CustomerNumber = context.HttpContext.Session.GetString(SESSION_CUSTOMER_NUMBER);

            CustomerMeterSerial = context.HttpContext.Session.GetString(SESSION_CUSTOMER_METER_SERIAL);

            using (MyVoltageDbContext db = new MyVoltageDbContext(options))
            {
            }

            using (MyVoltageApi.Data.MyVoltageApiDbContext db = new MyVoltageApi.Data.MyVoltageApiDbContext(APIoptions))
            {
            }

            var currentUser = _context.HttpContext.User;
            if (currentUser != null)
            {
                var user = _userManager.GetUserAsync(currentUser).Result;
                if (user != null)
                {
                    using (MyVoltageDbContext db = new MyVoltageDbContext(options))
                    {
                        var customer = db.Customers.Where(p => p.UserID == user.Id && !p.IsDeleted).SingleOrDefault();
                        if (customer != null)
                        {
                            var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                            if (CompanyID == 0)
                                CompanyID = customer.CompanyID;
                            if (string.IsNullOrEmpty(CompanyName))
                                CompanyName = company.Name;
                            if (string.IsNullOrEmpty(NetcashBankName))
                                NetcashBankName = company.NetcashBankName;
                            if (string.IsNullOrEmpty(NetcashBankAccountType))
                                NetcashBankAccountType = company.NetcashBankAccountType;
                            if (string.IsNullOrEmpty(NetcashBankAccountNo))
                                NetcashBankAccountNo = company.NetcashBankAccountNo;
                            if (string.IsNullOrEmpty(NetcashBankBranchCode))
                                NetcashBankBranchCode = company.NetcashBankBranchCode;
                            if (string.IsNullOrEmpty(CustomerMeterSerial))
                                CustomerMeterSerial = customer.MeterNumber;
                            if (string.IsNullOrEmpty(CustomerName))
                                CustomerName = customer.FullName;


                            List<string> skybillSerials = (from p in db.SkybillCustomers
                                                           where p.Customer_No == customer.CustomerNumber
                                                           select p.Serial_No).Distinct().ToList();


                            if (skybillSerials.Count > 0)
                            {
                                var localDevs = (from p in db.Devices
                                                 where skybillSerials.Contains(p.Serial)
                                                 select p).ToList();
                                foreach (var d in localDevs)
                                {
                                    Meters.Add(new MeterItem()
                                    {
                                        DeviceType = d.MeterType,
                                        SerialNo = d.Serial,
                                    });
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(CustomerMeterSerial))
                        {
                            #region CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALDEVICE

                            Data.Device _LocalDevice = null;

                            if (!cache.TryGetValue(CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALDEVICE + CustomerMeterSerial, out _LocalDevice))
                            {
                                _LocalDevice = db.Devices.Where(p => p.Serial == CustomerMeterSerial).FirstOrDefault();

                                var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                                cache.Set(CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALDEVICE + CustomerMeterSerial, _LocalDevice, cacheEntryOptions);
                            }

                            #endregion

                            if (_LocalDevice != null)
                            {
                                #region Device Type & Device Name

                                if (_LocalDevice.TypeID.HasValue)
                                {
                                    CustomerMeterDeviceType = ((DeviceType.DeviceTypeEnum)_LocalDevice.TypeID.Value);
                                }

                                CustomerMeterName = _LocalDevice.Name;
                                DeviceAPIIDValue = _LocalDevice.DeviceAPIIDValue;

                                #endregion
                            }

                            #region CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER

                            SkybillCustomer _SkybillCustomer = null;

                            if (!cache.TryGetValue(CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER + CustomerMeterSerial, out _SkybillCustomer))
                            {
                                _SkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == CustomerMeterSerial).FirstOrDefault();

                                var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                                cache.Set(CLIENTZONEPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER + CustomerMeterSerial, _SkybillCustomer, cacheEntryOptions);
                            }


                            #endregion

                            if (_SkybillCustomer != null)
                            {
                                CustomerNumber = _SkybillCustomer.Customer_No;
                                CustomerMeterNo = _SkybillCustomer.No;
                                if (string.IsNullOrEmpty(CustomerName))
                                    CustomerName = _SkybillCustomer.Customer_Name;

                                #region CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALCUSTOMER

                                Customer _LocalCustomer = null;

                                //if (!cache.TryGetValue(CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALCUSTOMER + CustomerMeterSerial, out _LocalCustomer))
                                //{
                                _LocalCustomer = db.Customers.Where(p => p.CustomerNumber == _SkybillCustomer.Customer_No && !p.IsDeleted).FirstOrDefault();

                                //    var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                                //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                                //    cache.Set(CLIENTZONEPROVIDER_CACHE_ENTRY_LOCALCUSTOMER + CustomerMeterSerial, _LocalCustomer, cacheEntryOptions);
                                //}


                                #endregion

                                if (_LocalCustomer != null)
                                {
                                    AccountTypeForSelectedCustomer = (AccountTypeEnum)_LocalCustomer.AccountTypeID;
                                    ShowHourlyUsage = _LocalCustomer.ShowDailyUsage.HasValue ? _LocalCustomer.ShowDailyUsage.Value : false;
                                    ShowCostInclVAT = _LocalCustomer.ShowCostInclVAT.HasValue ? _LocalCustomer.ShowCostInclVAT.Value : false;
                                    ShowDetailedDailyBilling = _LocalCustomer.ShowDetailedDailyBilling.HasValue ? _LocalCustomer.ShowDetailedDailyBilling.Value : false;
                                    OccupancyDate = _LocalCustomer.OccupancyDate;
                                    ActivateTaxInvoice = _LocalCustomer.ActivateTaxInvoice.HasValue ? _LocalCustomer.ActivateTaxInvoice.Value : false;
                                    if (_LocalDevice != null)
                                    {
                                        var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == _LocalCustomer.CustomerID && tbl.MeterNumber == _LocalDevice.DeviceIDLinked)).FirstOrDefault();

                                        if (customerMeter != null)
                                        {
                                            var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                                            if (customerMeterType != null)
                                            {
                                                MeterTypeForUsage = ((MeterTypeEnum)customerMeterType.Selected);
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    AccountTypeForSelectedCustomer = _SkybillCustomer.AccountType;

                                    #region Defaults if no customer registered

                                    ShowHourlyUsage = true;

                                    switch (_SkybillCustomer.AccountType)
                                    {
                                        case AccountTypeEnum.Unknown:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.MyWallet:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.PrepaidCredit:
                                            MeterTypeForUsage = MeterTypeEnum.Demand;
                                            break;
                                        case AccountTypeEnum.PostPaid:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.Metering:
                                            MeterTypeForUsage = MeterTypeEnum.Demand;
                                            break;
                                    }

                                    #endregion
                                }

                            }
                        }

                    }
                }
            }

        }

        public AccountTypeEnum AccountTypeForSelectedCustomer { get; set; }

        public string CustomerNumber { get; }
        public string CustomerName { get; }
        public int CompanyID { get; }
        public string CustomerMeterSerial { get; }
        public DeviceType.DeviceTypeEnum CustomerMeterDeviceType { get; set; }
        public string CustomerMeterName { get; set; }
        public int DeviceAPIIDValue { get; set; }
        public string CustomerMeterNo { get; set; }
        public DateTime OccupancyDate { get; }
        public bool ShowHourlyUsage { get; }
        public bool ShowCostInclVAT { get; }
        public bool ShowDetailedDailyBilling { get; }
        public MeterTypeEnum MeterTypeForUsage { get; }
        public bool ActivateTaxInvoice { get; }
        public string CompanyName { get; }
        public string NetcashBankName { get; set; }
        public string NetcashBankAccountType { get; set; }
        public string NetcashBankAccountNo { get; set; }
        public string NetcashBankBranchCode { get; set; }
        public List<MeterItem> Meters { get; }
        public class MeterItem
        {
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string SerialNo { get; set; }
        }

        public string AccountBalanceForSelectedCustomer
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomerNumber) && !string.IsNullOrEmpty(CompanyName))
                {
                    try
                    {
                        MyVoltage.Api.SkyBill.SkyBillApiClient client = new Api.SkyBill.SkyBillApiClient(CompanyName, _cache);
                        Api.SkyBill.Customer customer = client.GetCustomer(CustomerNumber);

                        decimal customerBalance = 0;
                        int multiplier = 1;
                        if (customer != null)
                        {
                            customerBalance = (decimal)customer.Balance_LCY;

                            if (AccountTypeForSelectedCustomer == AccountTypeEnum.MyWallet)
                                multiplier = -1;
                            else if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit)
                                multiplier = -1;
                        }
                        customerBalance = customerBalance * multiplier;

                        return "R " + customerBalance.ToString("N2", new CultureInfo("en-GB"));
                    }
                    catch { }
                }

                return "N/A";
            }
        }
        public string AccountRemainingCreditForSelectedCustomer
        {
            get
            {
                if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit && !string.IsNullOrEmpty(CustomerMeterSerial))
                {
                    // Remaining Credit Balance

                    var device = _client.GetDeviceByMeterNumber(CustomerMeterSerial);
                    int deviceId = device.id;
                    string start = DateTime.Now.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = DateTime.Now.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    string url = $"devices/{deviceId}/data?start={start}&end={end}&interval=3600&registers[90]=readings";

                    var result = _client.Get<MeterUsageResult>(url, DeviceAPIIDValue);
                    decimal prepaidBalance = 0;

                    foreach (Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.name.Equals("Remaining Credit"))
                        {
                            foreach (var registerEntry in readingRegister.readings)
                            {
                                if (registerEntry.HasValue)
                                    prepaidBalance = registerEntry.Value / 1000;
                            }
                        }
                    }

                    return prepaidBalance.ToString("N2", new CultureInfo("en-GB")) + " kWh";
                }
                return "N/A";
            }
        }

        public string CustomerBalance
        {
            get
            {
                if (!string.IsNullOrEmpty(CompanyName) && !string.IsNullOrEmpty(CustomerNumber))
                {
                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(CompanyName, _cache);
                    var customer = skyBillApiClient.GetCustomer(CustomerNumber);
                    if (customer != null)
                    {
                        string balance = "";

                        if (customer.Balance_LCY == 0 || AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                            balance = $"R {customer.Balance_LCY.ToString("N0", new CultureInfo("en-US")).Replace(",", " "):N0}";
                        else
                            balance = $"R {(customer.Balance_LCY * -1).ToString("N0", new CultureInfo("en-US")).Replace(",", " "):N0}";


                        if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit && !string.IsNullOrEmpty(CustomerMeterSerial))
                        {
                            var remainingCredit = _client.GetRemainingCredit(CustomerMeterSerial);

                            balance = $"{balance} ({remainingCredit})";
                        }

                        return balance;
                    }
                }

                return "";
            }
        }
    }
}
