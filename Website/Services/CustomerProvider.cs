using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Factories;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MyVoltage.Controllers;

namespace MyVoltage.Services
{
    public class CustomerProvider
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private DateTime _occupancyDate;
        private string _customerNumber;
        private string _companyName;
        private int _accountType;
        private bool _ShowHourlyUsage;
        private bool _ShowCostInclVAT;
        private ClaimsPrincipal _user;
        IHttpContextAccessor _context;
        private IMemoryCache _cache;
        public static readonly string SESSION_CUSTOMER_ID = "CustomerID";
        public static readonly string SESSION_COMPANY_NAME = "CompanyName";
        public static readonly string METER_NUMBER = "MeterNumber";
        private readonly IDeviceApi _client;
        private bool _didPayAccount;
        private string _companyBalanceCheckSkybillCustomerNo;
        private string _MyMeterSASkybill;
        private bool _RedirectToRecharge;

        public CustomerProvider(UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options, IHttpContextAccessor context, IMemoryCache cache, IConfiguration config)
        {
            _userManager = userManager;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _user = context.HttpContext.User;
            _ShowHourlyUsage = false;
            _ShowCostInclVAT = false;
            _occupancyDate = DateTime.MinValue;
            _didPayAccount = true;
            _RedirectToRecharge = false;
            // customerId = CustomerNumber
            var customerId = context.HttpContext.Session.GetString(SESSION_CUSTOMER_ID);
            var companyName = context.HttpContext.Session.GetString(SESSION_COMPANY_NAME);
            string userID = userManager.GetUserId(_user);
            _MyMeterSASkybill = config["MyMeterSASkybill:Name"];

            if (string.IsNullOrEmpty(userID))
                return;

            using (var db = new MyVoltageDbContext(_options))
            {
                if (_user.IsInRole(MyVoltage.Data.UserRoleEnum.Technician.ToString()))
                {
                    _companyName = companyName;
                }
                else
                {
                    var allAtCustomer = db.Customers.Where(tbl => tbl.UserID == userID && tbl.IsDeleted == false).FirstOrDefault();
                    if (allAtCustomer != null)
                    {
                        var company = db.Companies.Where(tbl => tbl.CompanyID == allAtCustomer.CompanyID).SingleOrDefault();
                        _companyName = company.Name;

                        if (_user.IsInRole(MyVoltage.Data.UserRoleEnum.CompanyAdmin.ToString()))
                        {
                            _companyBalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo;
                            if (!string.IsNullOrEmpty(company.BalanceCheckSkybillCustomerNo) && company.BalanceMustBeAbove.HasValue)
                            {
                                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(config["MyMeterSASkybill:Name"], _cache);
                                var customerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(company.BalanceCheckSkybillCustomerNo, config["MyMeterSASkybill:Name"]);

                                if (customerDetails != null)
                                {
                                    var balance = customerDetails.Balance_LCY * -1;

                                    if (balance < company.BalanceMustBeAbove.Value)
                                    {
                                        _didPayAccount = false;
                                        _RedirectToRecharge = true;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(customerId))
                        {
                            _customerNumber = customerId;
                            // Customer is selected
                            var localCustomer = db.Customers.Where(p => p.CustomerNumber == _customerNumber && p.IsDeleted == false).FirstOrDefault();
                            if (localCustomer != null)
                            {
                                _occupancyDate = localCustomer.OccupancyDate;
                                _accountType = localCustomer.AccountTypeID;
                                _ShowHourlyUsage = localCustomer.ShowDailyUsage.HasValue ? localCustomer.ShowDailyUsage.Value : false;
                                _ShowCostInclVAT = localCustomer.ShowCostInclVAT.HasValue ? localCustomer.ShowCostInclVAT.Value : false;
                            }
                            else
                            {
                                // Get from skybill
                                SkyBillApiClient client = new SkyBillApiClient(company.Name, cache);
                                var skybillCustomer = client.GetCustomerDetailsByCustomerNo(customerId, company.Name);
                                if (skybillCustomer != null)
                                {
                                    if (skybillCustomer.Billing_Cycle.ToUpper().Contains("WALELT") || skybillCustomer.Billing_Cycle.ToUpper().Contains("WALLET"))
                                    {
                                        _accountType = (int)AccountTypeEnum.MyWallet;
                                    }
                                    else if (skybillCustomer.Billing_Cycle.ToUpper().Contains("POSTPAID"))
                                    {
                                        _accountType = (int)AccountTypeEnum.PostPaid;
                                    }
                                    else if (skybillCustomer.Billing_Cycle.ToUpper().Contains("PREPAID"))
                                    {
                                        _accountType = (int)AccountTypeEnum.PrepaidCredit;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // All@ or customer is selected
                            _customerNumber = allAtCustomer.CustomerNumber;
                            _occupancyDate = allAtCustomer.OccupancyDate;
                            _accountType = allAtCustomer.AccountTypeID;
                            _ShowHourlyUsage = allAtCustomer.ShowDailyUsage.HasValue ? allAtCustomer.ShowDailyUsage.Value : false;
                            _ShowCostInclVAT = allAtCustomer.ShowCostInclVAT.HasValue ? allAtCustomer.ShowCostInclVAT.Value : false;
                        }
                    }
                    else
                    {
                    }
                }
            }
        }

        public CustomerProvider(string userID, DbContextOptions<MyVoltageDbContext> options, IHttpContextAccessor context, IMemoryCache cache, IConfiguration config)
        {
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _user = context.HttpContext.User;
            _didPayAccount = true;

            var customerId = context.HttpContext.Session.GetString(SESSION_CUSTOMER_ID);
            var companyName = context.HttpContext.Session.GetString(SESSION_COMPANY_NAME);

            using (var db = new MyVoltageDbContext(_options))
            {
                if (userID == null)
                    return;

                if (_user.IsInRole(MyVoltage.Data.UserRoleEnum.Technician.ToString()))
                {
                    _companyName = companyName;
                }
                else
                {
                    var allAtCustomer = db.Customers.Where(tbl => tbl.UserID == userID && tbl.IsDeleted == false).FirstOrDefault();
                    var company = db.Companies.Where(tbl => tbl.CompanyID == allAtCustomer.CompanyID).SingleOrDefault();
                    _companyName = company.Name;


                    if (_user.IsInRole(MyVoltage.Data.UserRoleEnum.CompanyAdmin.ToString()))
                    {
                        _companyBalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo;
                        if (!string.IsNullOrEmpty(company.BalanceCheckSkybillCustomerNo) && company.BalanceMustBeAbove.HasValue)
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(config["MyMeterSASkybill:Name"], _cache);
                            var customerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(company.BalanceCheckSkybillCustomerNo, config["MyMeterSASkybill:Name"]);

                            if (customerDetails != null)
                            {
                                var balance = customerDetails.Balance_LCY * -1;

                                if (balance < company.BalanceMustBeAbove.Value)
                                {
                                    _didPayAccount = false;
                                    _RedirectToRecharge = true;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(customerId))
                    {
                        _customerNumber = customerId;
                        // Customer is selected
                        var localCustomer = db.Customers.Where(p => p.CustomerNumber == _customerNumber && p.IsDeleted == false).FirstOrDefault();
                        if (localCustomer != null)
                        {
                            _occupancyDate = localCustomer.OccupancyDate;
                            _accountType = localCustomer.AccountTypeID;
                            _ShowHourlyUsage = localCustomer.ShowDailyUsage.HasValue ? localCustomer.ShowDailyUsage.Value : false;
                            _ShowCostInclVAT = localCustomer.ShowCostInclVAT.HasValue ? localCustomer.ShowCostInclVAT.Value : false;
                        }
                        else
                        {
                            // Get from skybill
                            SkyBillApiClient client = new SkyBillApiClient(company.Name, cache);
                            var skybillCustomer = client.GetCustomerDetailsByCustomerNo(customerId, company.Name);
                            if (skybillCustomer != null)
                            {
                                if (skybillCustomer.Billing_Cycle.ToUpper().Contains("WALELT") || skybillCustomer.Billing_Cycle.ToUpper().Contains("WALLET"))
                                {
                                    _accountType = (int)AccountTypeEnum.MyWallet;
                                }
                                else if (skybillCustomer.Billing_Cycle.ToUpper().Contains("POSTPAID"))
                                {
                                    _accountType = (int)AccountTypeEnum.PostPaid;
                                }
                                else if (skybillCustomer.Billing_Cycle.ToUpper().Contains("PREPAID"))
                                {
                                    _accountType = (int)AccountTypeEnum.PrepaidCredit;
                                }
                            }
                        }
                    }
                    else
                    {
                        // All@ is selected
                        _customerNumber = allAtCustomer.CustomerNumber;
                        _occupancyDate = allAtCustomer.OccupancyDate;
                        _accountType = allAtCustomer.AccountTypeID;
                        _ShowHourlyUsage = allAtCustomer.ShowDailyUsage.HasValue ? allAtCustomer.ShowDailyUsage.Value : false;
                        _ShowCostInclVAT = allAtCustomer.ShowCostInclVAT.HasValue ? allAtCustomer.ShowCostInclVAT.Value : false;
                    }
                }
            }
        }

        public static decimal GetMyWalletBalance(IMemoryCache cache, string companyName, string customerNumber)
        {
            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(companyName, cache);
            Api.SkyBill.Customer customer = skyBillApiClient.GetCustomer(customerNumber);

            decimal customerBalance = 0;

            int multiplier = 1;

            if (customer != null)
            {
                customerBalance = (decimal)customer.Balance_LCY;

                if (customer.BILLING_CYCLE.ToUpper().Contains("WALLET".ToUpper()))
                    multiplier = -1;
                else if (customer.BILLING_CYCLE.ToUpper().Contains("PREPAID".ToUpper()))
                    multiplier = -1;
            }
            else
            {
                customerBalance = 0;
            }


            customerBalance = customerBalance * multiplier;

            return customerBalance;
        }

        public string GetBalance(bool getCompanyAdminBalance = false)
        {

            if (getCompanyAdminBalance)
            {
                decimal customerBalance = 0;
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_MyMeterSASkybill, _cache);
                var customerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(_companyBalanceCheckSkybillCustomerNo, _MyMeterSASkybill);

                var balance = customerDetails.Balance_LCY * -1;


                return "R " + balance.ToString("N2", new CultureInfo("en-GB"));
            }
            else
            {
                decimal customerBalance = 0;
                try
                {
                    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_companyName, _cache);
                    Api.SkyBill.Customer _customer = null;
                    _customer = skyBillApiClient.GetCustomer(_customerNumber);

                    var userID = _userManager.GetUserId(_user);

                    MeterProvider provider = new MeterProvider(_occupancyDate, _cache, new MyVoltageDbContext(_options), null, _context, null, _options, null);

                    int multiplier = 1;

                    if (_customer != null)
                    {
                        customerBalance = (decimal)_customer.Balance_LCY;

                        if (_customer.BILLING_CYCLE.ToUpper().Contains("WALLET".ToUpper()))
                            multiplier = -1;
                        else if (_customer.BILLING_CYCLE.ToUpper().Contains("PREPAID".ToUpper()))
                            multiplier = -1;
                    }
                    else
                    {
                        customerBalance = 0;
                    }

                    customerBalance = customerBalance * multiplier;
                }
                catch { }

                return "R " + customerBalance.ToString("N2", new CultureInfo("en-GB"));
            }

        }

        public string GetRemainingCredit()
        {
            if (_accountType == (int)AccountTypeEnum.PrepaidCredit)
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_companyName, _cache);
                var meters = skyBillApiClient.GetMetersByCustomer(_customerNumber);

                if (meters.Count > 0)
                {
                    var device = _client.GetDeviceByMeterNumber(meters[0].Serial_No);
                    int deviceId = device.id;
                    string start = DateTime.Now.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = DateTime.Now.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    string url = $"devices/{deviceId}/data?start={start}&end={end}&interval=3600&registers[90]=readings";

                    var result = _client.Get<MeterUsageResult>(url, 1);
                    decimal customerBalance = 0;

                    foreach (Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.name.Equals("Remaining Credit"))
                        {
                            foreach (var registerEntry in readingRegister.readings)
                            {
                                if (registerEntry.HasValue)
                                    customerBalance = registerEntry.Value / 1000;
                            }
                        }
                    }

                    return customerBalance.ToString("N2", new CultureInfo("en-GB")) + " kWh";
                }
            }

            return "N/A";
        }

        public int AccountType
        {
            get
            {
                return _accountType;
            }
        }

        public DateTime OccupancyDate
        {
            get
            {
                return _occupancyDate;
            }
        }

        public string CustomerNumber
        {
            get
            {
                return _customerNumber;
            }
        }

        public string CompanyName
        {
            get
            {
                return _companyName;
            }
        }

        public bool ShowHourlyUsage
        {
            get
            {
                return _ShowHourlyUsage;
            }
        }
        public bool ShowCostInclVAT
        {
            get
            {
                return _ShowCostInclVAT;
            }
        }
        public bool DidPayAccount
        {
            get
            {
                return _didPayAccount;
            }
        }
        public string CompanyBalanceCheckSkybillCustomerNo
        {
            get
            {
                return _companyBalanceCheckSkybillCustomerNo;
            }
        }

        public string MyMeterSASkybill
        {
            get
            {
                return _MyMeterSASkybill;
            }
        }
        public bool RedirectToRecharge
        {
            get
            {
                return _RedirectToRecharge;
            }
        }

        public CustomerLookupResult CustomerLookup(string searchstring)
        {
            CustomerLookupResult result = new CustomerLookupResult();

            if (!string.IsNullOrEmpty(searchstring))
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    // SkybillCustomerNo (SkybillCustomers)
                    // This can be separate as the result of skybill is seperate fields
                    // If more than one found - then check below if devices found
                    var skybillCustomers = (from p in db.SkybillCustomers
                                            where p.Customer_No.ToUpper() == searchstring.ToUpper()
                                            || p.No.ToUpper() == searchstring.ToUpper()
                                            select p).ToList();

                    if (skybillCustomers.Count > 0)
                    {
                        var skybillCustomer = skybillCustomers[0];

                        var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();
                        List<string> meterSerials = (from p in skybillCustomers select p.Serial_No).ToList();
                        // Get live balance
                        SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                        var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                        var balance = skybillCustomerDetails.Balance_LCY * -1;

                        var customer = db.Customers.Where(p => !p.IsDeleted && (p.MeterNumber == skybillCustomer.Serial_No || p.CustomerNumber == skybillCustomer.Customer_No || p.CustomerNumber == skybillCustomer.No)).FirstOrDefault();

                        var customerDetails = new CustomerLookupResult.Customerdetails
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((AccountController.DeviceType)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
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
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion



                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : "Unknown"
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };
                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }
                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = skybillCustomer != null ? db.UniPins.Where(p => p.MeterNumber == skybillCustomer.Serial_No).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;
                    }



                    // MeterSerial (devices)
                    var device = (from p in db.Devices
                                  where p.Serial == searchstring
                                  select p).SingleOrDefault();

                    if (device != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == device.CompanyID).SingleOrDefault();
                        var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
                        decimal balance = 0;
                        skybillCustomers = skybillCustomer != null ? (from p in db.SkybillCustomers where p.Customer_No == skybillCustomer.Customer_No select p).ToList() : new List<SkybillCustomer>();
                        List<string> meterSerials = skybillCustomer != null ? (from p in skybillCustomers select p.Serial_No).ToList() : new List<string>();

                        if (skybillCustomer != null)
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                            var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                            balance = skybillCustomerDetails.Balance_LCY * -1;
                        }


                        var customer = db.Customers.Where(p => !p.IsDeleted && (p.MeterNumber == device.Serial)).FirstOrDefault();


                        var customerDetails = new CustomerLookupResult.Customerdetails
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((AccountController.DeviceType)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
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
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion

                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };

                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }

                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = device != null ? db.UniPins.Where(p => p.MeterNumber == device.Serial).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;
                    }


                    // Telephone (Customers)
                    // Email (Customers)

                    var customerFound = (from p in db.Customers
                                         where !p.IsDeleted &&
                                         (
                                         p.NotificationEmail.ToUpper() == searchstring.ToUpper()
                                         || p.PhoneNumber.ToUpper() == searchstring.ToUpper()
                                         || p.AltPhoneNumber.ToUpper() == searchstring.ToUpper()
                                         || p.NotificationPhoneNumber.ToUpper() == searchstring.ToUpper()
                                         )
                                         select p).FirstOrDefault();



                    if (customerFound != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == customerFound.CompanyID).SingleOrDefault();
                        var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == customerFound.MeterNumber).FirstOrDefault();
                        decimal balance = 0;
                        skybillCustomers = skybillCustomer != null ? (from p in db.SkybillCustomers where p.Customer_No == skybillCustomer.Customer_No select p).ToList() : new List<SkybillCustomer>();
                        List<string> meterSerials = skybillCustomer != null ? (from p in skybillCustomers select p.Serial_No).ToList() : new List<string>();

                        if (skybillCustomer != null)
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                            var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                            balance = skybillCustomerDetails.Balance_LCY * -1;
                        }


                        var customer = customerFound;


                        var customerDetails = new CustomerLookupResult.Customerdetails()
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            string meterLinked = $"{meter.Serial_No} - {meter.No}";

                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((AccountController.DeviceType)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
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
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion

                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };

                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }

                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = customer != null ? db.UniPins.Where(p => p.MeterNumber == customer.MeterNumber).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;

                    }

                }

            }

            return result;
        }

        public class CustomerLookupResult
        {
            public Customerdetails CustomerDetails { get; set; }
            public Meterslinked[] MetersLinked { get; set; }
            public Paymentinfo[] PaymentInfo { get; set; }
            public class Customerdetails
            {
                public string CustomerName { get; set; }
                public string CustomerEmail { get; set; }
                public string CustomerPhone { get; set; }
                public string CustomerAltPhone { get; set; }
                public string CustomerNotificationPhone { get; set; }
                public string CustomerCompanyName { get; set; }
                public string CustomerNo { get; set; }
                public float Balance { get; set; }
            }

            public class Meterslinked
            {
                public string SerialNumber { get; set; }
                public string Name { get; set; }
                public string Status { get; set; }
                public string TypeName { get; set; }
                public DateTime LastCommunicated { get; set; }
                public string Connected { get; set; }
            }

            public class Paymentinfo
            {
                public DateTime PaymentDate { get; set; }
                public float PaymentAmount { get; set; }
                public string PaymentMethod { get; set; }
            }
        }

        public Company GetCompany()
        {
            Company company = null;

            using (var db = new MyVoltageDbContext(_options))
            {
                company = (from p in db.Companies
                           where p.Name == _companyName
                           select p).FirstOrDefault();
            }

            return company;
        }

    }
}
