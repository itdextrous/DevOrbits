using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
using MyVoltage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Services;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using MyVoltage.Models.CompanyAdminViewModels;
using Microsoft.AspNetCore.Http;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Factories;
using MyVoltage.Jobs;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Web;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltageApi.Data;
using MoreLinq;
using MyVoltage.Extensions;
using System.Reflection;
using MyVoltage.Api.Prism;

namespace MyVoltage.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MeterController : Controller
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerProvider _customerProvider;
        private readonly BillingProvider _billingProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;

        public MeterController(IMemoryCache cache, UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options, CustomerProvider customerProvider, BillingProvider billingProvider, IHttpContextAccessor contextAccessor, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IConfiguration configuration)
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

        [Route("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");

            var user = _userManager.GetUserAsync(User).Result;

            if (user == null || user.Id == null)
                return Redirect("/logout");

            if (User.IsInRole(UserRoleEnum.Technician.ToString()))
                return Redirect("/technician/dashboard");

            if (User.IsInRole(UserRoleEnum.Admin.ToString()))
                return Redirect("/admindashboard");

            return Redirect("/");

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);
            var meterModels = new List<MeterViewModel>();

            int multiplier = -1;

            if (_customerProvider.AccountType == (int)AccountTypeEnum.PostPaid)
                multiplier = 1;

            foreach (SkyBillCustomer meter in meters)
            {
                List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meter.Serial_No, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);
                Decimal monthlyTotal = dailyTotals.Sum() * multiplier;
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {

                    MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

                    provider.PopulateCustomerMeter(device.id, user.Id, device.deviceType);

                    string type = device.type != null ? device.type.type : "";
                    string unitType = device.type != null ? device.type.UnitType : "";

                    meterModels.Add(new MeterViewModel { MeterNumber = meter.Serial_No, Name = meter.No, MeterType = type, UnitType = unitType, MonthlyTotal = monthlyTotal });
                }
            }

            var rentData = _billingProvider.GetMonthlyInvoiceAmountForRent(_customerProvider.CustomerNumber, DateTime.Now.Year, _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);

            if (rentData != null)
            {
                var rentMonthData = _billingProvider.GetMonthlyInvoiceAmountForRent(_customerProvider.CustomerNumber, DateTime.Now.Year, _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);
                Decimal rentMonthDataTotal = rentMonthData.Data.Item1.Sum() * multiplier;
                meterModels.Add(new MeterViewModel { MeterType = "home", MeterColor = "green", Name = rentData.MeterName, MeterNumber = rentData.MeterNumber, MonthlyTotal = rentMonthDataTotal });
            }

            return View(meterModels);
        }

        [Route("map")]
        [Route("map/{keyword?}")]
        public async Task<IActionResult> Map(string keyword)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            MapViewModel viewModel = new MapViewModel();

            List<MyVoltage.Api.MyVoltage.Device> devices = getDevices();
            List<MyVoltage.Api.MyVoltage.Device> newDevices = new List<MyVoltage.Api.MyVoltage.Device>();

            if (keyword != null)
            {
                foreach (var d in devices)
                {
                    if (d.serial.ToLower().Contains(keyword.ToLower()) || d.name.ToLower().Contains(keyword.ToLower()))
                    {
                        newDevices.Add(d);
                    }
                }
            }
            else
            {
                newDevices = devices;
            }

            viewModel.total = newDevices.Count();

            if (User.IsInRole("CompanyAdmin"))
            {
                viewModel.pageSize = 20;
            }
            else
            {
                viewModel.pageSize = newDevices.Count();
            }

            string page = _contextAccessor.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;

            viewModel.pageIndex = pageIndex ?? 1;

            viewModel.devices = newDevices.Skip((viewModel.pageIndex - 1) * viewModel.pageSize).Take(viewModel.pageSize).ToList();

            return View(viewModel);
        }

        [Authorize(Roles = "CompanyAdmin")]
        [Route("administration")]
        [Route("administration/{keyword?}")]
        public async Task<IActionResult> Administration(string keyword)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            MapViewModel viewModel = new MapViewModel();

            List<MyVoltage.Api.MyVoltage.Device> devices = getDevices();
            List<MyVoltage.Api.MyVoltage.Device> newDevices = new List<MyVoltage.Api.MyVoltage.Device>();

            if (keyword != null)
            {
                foreach (var d in devices)
                {
                    if (d.serial.ToLower().Contains(keyword.ToLower()) || d.name.ToLower().Contains(keyword.ToLower()))
                    {
                        if (string.IsNullOrEmpty(d.account))
                        {
                            d.balance = "N/A";
                        }
                        else if (d.account == "WALLET" || d.account == "PREPAID")
                        {
                            double balance = Convert.ToDouble(d.balance);

                            double newBalance = balance * -1;

                            d.balance = "R " + newBalance.ToString();

                        }

                        newDevices.Add(d);
                    }
                }
            }
            else
            {
                foreach (var d in devices)
                {
                    if (string.IsNullOrEmpty(d.account))
                    {
                        d.balance = "N/A";
                    }
                    else if (d.account == "WALLET" || d.account == "PREPAID")
                    {
                        double balance = Convert.ToDouble(d.balance);
                        double newBalance = balance * -1;

                        d.balance = "R " + newBalance.ToString();
                    }

                    newDevices.Add(d);
                }
            }

            viewModel.total = newDevices.Count();

            if (User.IsInRole("CompanyAdmin"))
            {
                viewModel.pageSize = 20;
            }
            else
            {
                viewModel.pageSize = newDevices.Count();
            }

            string page = _contextAccessor.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;

            viewModel.pageIndex = pageIndex ?? 1;

            viewModel.devices = newDevices.Skip((viewModel.pageIndex - 1) * viewModel.pageSize).Take(viewModel.pageSize).ToList();

            foreach (MyVoltage.Api.MyVoltage.Device d in viewModel.devices)
            {
                NotificationCustomerMeter notification;
                d.autoDisconnect = true; // Default should be auto

                using (var db = new MyVoltageDbContext(_options))
                {
                    notification = db.NotificationCustomerMeters.Where(itm => itm.MeterSerial == d.serial).FirstOrDefault();
                }

                if (notification != null)
                {
                    d.autoDisconnect = notification.AutoDisconnect;
                }
            }

            return View(viewModel);
        }
        [Authorize(Roles = "CompanyAdmin")]
        [Route("CreditControlView")]
        [Route("CreditControlView/{keyword?}")]
        public async Task<IActionResult> CreditControlView(string keyword)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            MapViewModel viewModel = new MapViewModel();

            List<MyVoltage.Api.MyVoltage.Device> devices = getDevices();
            List<MyVoltage.Api.MyVoltage.Device> newDevices = new List<MyVoltage.Api.MyVoltage.Device>();

            if (keyword != null)
            {
                foreach (var d in devices)
                {
                    if (d.serial.ToLower().Contains(keyword.ToLower()) || d.name.ToLower().Contains(keyword.ToLower()))
                    {
                        if (string.IsNullOrEmpty(d.account))
                        {
                            d.balance = "N/A";
                            continue;
                        }
                        else if (d.account == "WALLET" || d.account == "PREPAID")
                        {
                            double balance = Convert.ToDouble(d.balance);

                            double newBalance = balance * -1;

                            if (newBalance >= 0)
                                continue;

                            d.balance = "R " + newBalance.ToString();

                        }

                        newDevices.Add(d);
                    }
                }
            }
            else
            {
                foreach (var d in devices)
                {
                    if (string.IsNullOrEmpty(d.account))
                    {
                        d.balance = "N/A";
                        continue;
                    }
                    else if (d.account == "WALLET" || d.account == "PREPAID")
                    {
                        double balance = Convert.ToDouble(d.balance);
                        double newBalance = balance * -1;

                        if (newBalance >= 0)
                            continue;

                        d.balance = "R " + newBalance.ToString();
                    }

                    newDevices.Add(d);
                }
            }

            viewModel.total = newDevices.Count();

            if (User.IsInRole("CompanyAdmin"))
            {
                viewModel.pageSize = 20;
            }
            else
            {
                viewModel.pageSize = newDevices.Count();
            }

            string page = _contextAccessor.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;

            viewModel.pageIndex = pageIndex ?? 1;

            viewModel.devices = newDevices.Skip((viewModel.pageIndex - 1) * viewModel.pageSize).Take(viewModel.pageSize).ToList();

            foreach (MyVoltage.Api.MyVoltage.Device d in viewModel.devices)
            {
                NotificationCustomerMeter notification;
                d.autoDisconnect = true; // Default should be auto

                using (var db = new MyVoltageDbContext(_options))
                {
                    notification = db.NotificationCustomerMeters.Where(itm => itm.MeterSerial == d.serial).FirstOrDefault();
                }

                if (notification != null)
                {
                    d.autoDisconnect = notification.AutoDisconnect;
                }
            }

            return View(viewModel);
        }

        [Authorize(Roles = "CompanyAdmin")]
        [Route("Connector")]
        [Route("Connector/{keyword?}")]
        public async Task<IActionResult> Connector(string keyword)
        {
            return Redirect("/");

            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            MapViewModel viewModel = new MapViewModel();

            List<MyVoltage.Api.MyVoltage.Device> devices = getDevices();
            List<MyVoltage.Api.MyVoltage.Device> newDevices = new List<MyVoltage.Api.MyVoltage.Device>();

            if (keyword != null)
            {
                foreach (var d in devices)
                {
                    if (d.serial.ToLower().Contains(keyword.ToLower()) || d.name.ToLower().Contains(keyword.ToLower()))
                    {
                        if (string.IsNullOrEmpty(d.account))
                        {
                            d.balance = "N/A";
                            continue;
                        }
                        else if (d.account == "WALLET")
                        {
                            double balance = Convert.ToDouble(d.balance);

                            double newBalance = balance * -1;

                            d.balance = "R " + newBalance.ToString();

                            if (newBalance > 0 && d.deviceType.ToUpper().Contains("ELEC"))
                            {

                                // Get contactor state
                                var readingResult = _client.GetRegisters(d.id.ToString());
                                string contactorState = "";

                                foreach (var register in readingResult.device.registers)
                                    if (register.id == 91)
                                        contactorState = register.reading != null ? register.reading.value.ToString() : "";


                                // If balance > 0 and disconnected
                                if (contactorState == "0")
                                {
                                    newDevices.Add(d);
                                }

                            }

                        }
                    }
                }
            }
            else
            {
                foreach (var d in devices)
                {
                    if (string.IsNullOrEmpty(d.account))
                    {
                        d.balance = "N/A";
                        continue;
                    }
                    else if (d.account == "WALLET")
                    {
                        double balance = Convert.ToDouble(d.balance);
                        double newBalance = balance * -1;

                        d.balance = "R " + newBalance.ToString();
                        if (newBalance > 0 && d.deviceType.ToUpper().Contains("ELEC"))
                        {

                            // Get contactor state
                            var readingResult = _client.GetRegisters(d.id.ToString());
                            string contactorState = "";

                            foreach (var register in readingResult.device.registers)
                                if (register.id == 91)
                                    contactorState = register.reading != null ? register.reading.value.ToString() : "";


                            // If balance > 0 and disconnected
                            if (contactorState == "0")
                            {
                                newDevices.Add(d);
                            }

                        }
                    }

                }
            }

            viewModel.total = newDevices.Count();

            if (User.IsInRole("CompanyAdmin"))
            {
                viewModel.pageSize = 20;
            }
            else
            {
                viewModel.pageSize = newDevices.Count();
            }

            string page = _contextAccessor.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;

            viewModel.pageIndex = pageIndex ?? 1;

            viewModel.devices = newDevices.Skip((viewModel.pageIndex - 1) * viewModel.pageSize).Take(viewModel.pageSize).ToList();

            foreach (MyVoltage.Api.MyVoltage.Device d in viewModel.devices)
            {
                NotificationCustomerMeter notification;
                d.autoDisconnect = true; // Default should be auto

                using (var db = new MyVoltageDbContext(_options))
                {
                    notification = db.NotificationCustomerMeters.Where(itm => itm.MeterSerial == d.serial).FirstOrDefault();
                }

                if (notification != null)
                {
                    d.autoDisconnect = notification.AutoDisconnect;
                }
            }

            return View(viewModel);
        }

        [Authorize(Roles = "CompanyAdmin")]
        [Route("/administrator/toggleDisconnect/{serial}/{typeID}")]
        public async Task<IActionResult> ToggleDisconnect(string serial, int typeID)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var accountType = 0;
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = db.Customers.Where(tbl => tbl.MeterNumber == serial && tbl.IsDeleted == false).FirstOrDefault();

                if (customer != null)
                {
                    accountType = customer.AccountTypeID;
                }
            }

            if (typeID != 1 || accountType == (int)AccountTypeEnum.PrepaidCredit) // Water meters and Pre-paid meters cannot be toggled
            {
                return Json(true);
            }
            else
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var notifications = db.NotificationCustomerMeters.Where(itm => itm.MeterSerial == serial).ToList();
                    var localDevice = db.Devices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (notifications.Count > 0)
                    {
                        bool autoDisconnect = notifications[0].AutoDisconnect;

                        if (autoDisconnect)
                        {
                            var user = _userManager.GetUserAsync(User).Result;
                            MeterConnectionStatusChange meterConnectionStatusChange = new MeterConnectionStatusChange()
                            {
                                DateRequested = DateTime.Now,
                                MeterSerial = serial,
                                RequestedAction = "Manual",
                                UserID = user.Id
                            };
                            db.MeterConnectionStatusChanges.Add(meterConnectionStatusChange);
                            db.SaveChanges();

                            string approvalURL = $"https://mymetersa.co.za/approveMeterConnectionStatusChange/{meterConnectionStatusChange.ID}";
                            string rejectURL = $"https://mymetersa.co.za/rejectMeterConnectionStatusChange/{meterConnectionStatusChange.ID}";

                            StringBuilder emailBody = new StringBuilder();

                            emailBody.AppendLine($"It has been requested that the following meter be changed from Auto to Manual.<br />");
                            emailBody.AppendLine($"Serial: {serial}<br />");
                            if (localDevice != null)
                            {
                                emailBody.AppendLine($"Name: {localDevice.Name}<br />");
                            }
                            emailBody.AppendLine($"<a href=\"{approvalURL}\">Approve</a><br />");
                            emailBody.AppendLine($"<a href=\"{approvalURL}\">Reject</a><br />");




                            EmailSender emailSender = new EmailSender();
                            List<string> emails = new List<string>()
                            {
                                "arno@myvoltage.co.za",
                                "nic@myvoltage.co.za",
                                "anelle@myvoltage.co.za",
                                //"lendl@myvoltage.co.za"
                            };


                            if (localDevice != null && localDevice.CompanyID.HasValue)
                            {
                                List<string> buildingAuthEmails = new List<string>();

                                var company = db.Companies.Where(p => p.CompanyID == localDevice.CompanyID.Value).SingleOrDefault();
                                var buildingDetails = (from p in db.BuildingDetails
                                                       where p.BuildingSkybillName == company.Name
                                                       select p).SingleOrDefault();


                                if (buildingDetails != null)
                                {
                                    if (!string.IsNullOrEmpty(buildingDetails.BuildingMeterStatusChangeAuthEmail1))
                                        buildingAuthEmails.Add(buildingDetails.BuildingMeterStatusChangeAuthEmail1);
                                    if (!string.IsNullOrEmpty(buildingDetails.BuildingMeterStatusChangeAuthEmail2))
                                        buildingAuthEmails.Add(buildingDetails.BuildingMeterStatusChangeAuthEmail2);
                                    if (!string.IsNullOrEmpty(buildingDetails.BuildingMeterStatusChangeAuthEmail3))
                                        buildingAuthEmails.Add(buildingDetails.BuildingMeterStatusChangeAuthEmail3);
                                }

                                if (buildingAuthEmails.Count > 0)
                                    emails = buildingAuthEmails;
                            }

                            await emailSender.SendEmailAsync(emails.ToArray(), "Meter Connection Status Change Request", emailBody.ToString(), emailBody.ToString());



                            return Json(true);

                        }
                        else
                        {
                            // Switching back to auto does not require approval, right?
                            foreach (var notification in notifications)
                            {
                                notification.AutoDisconnect = !autoDisconnect;
                            }

                            await db.SaveChangesAsync();

                            return Json(!autoDisconnect);
                        }
                    }
                    else
                    {
                        var notificationCustomerMeter = new NotificationCustomerMeter
                        {
                            CustomerID = 0,
                            MeterSerial = serial,
                            LowBalanceNotification1 = false,
                            LowBalanceNotification2 = false,
                            DisconnectNotification = false,
                            LastUpdated = DateTime.Now,
                            AccountType = 0,
                            AutoDisconnect = true
                        };

                        db.Add(notificationCustomerMeter);

                        await db.SaveChangesAsync();

                        return Json(false);
                    }
                }
            }
        }

        [Route("/approveMeterConnectionStatusChange/{ID}")]
        public async Task<IActionResult> ApproveMeterConnectionStatusChange(int ID)
        {
            string result = "";

            using (var db = new MyVoltageDbContext(_options))
            {
                var entry = (from p in db.MeterConnectionStatusChanges
                             where p.ID == ID
                             select p).SingleOrDefault();

                if (entry == null || entry.Approved.HasValue)
                    return Json("Request not found or already approved.");

                var notificationSettings = (from p in db.NotificationCustomerMeters
                                            where p.MeterSerial == entry.MeterSerial
                                            select p).ToList();

                int customerID = 0;

                string action = entry.RequestedAction.ToUpper();
                foreach (var item in notificationSettings)
                {

                    switch (action)
                    {
                        case "MANUAL":
                            item.AutoDisconnect = false;
                            break;
                        case "AUTO":
                            item.AutoDisconnect = false;
                            break;
                    }
                    db.NotificationCustomerMeters.Update(item);
                    if (item.CustomerID > 0)
                        customerID = item.CustomerID;
                }

                entry.Approved = true;
                entry.DateApproved = DateTime.Now;
                db.MeterConnectionStatusChanges.Update(entry);

                db.SaveChanges();

                result = $"{entry.MeterSerial} change to {action} has been APPROVED";

                // Email and SMS to client
                if (customerID > 0)
                {
                    // Your meter {14385984} has been changed from {AUTO} to {MANUAL}
                    // SMS and email for both actions on /overview/gateway/{GWID}/devices
                }
            }

            return Json(result);
        }
        [Route("/rejectMeterConnectionStatusChange/{ID}")]
        public async Task<IActionResult> RejectMeterConnectionStatusChange(int ID)
        {
            string result = "";

            using (var db = new MyVoltageDbContext(_options))
            {
                var entry = (from p in db.MeterConnectionStatusChanges
                             where p.ID == ID
                             select p).SingleOrDefault();

                if (entry == null || entry.Approved.HasValue)
                    return Json("Request not found or already approved.");

                entry.Approved = false;
                entry.DateApproved = DateTime.Now;
                db.MeterConnectionStatusChanges.Update(entry);

                db.SaveChanges();
                result = $"{entry.MeterSerial} change to {entry.RequestedAction.ToUpper()} has been REJECTED.";
            }

            return Json(result);
        }
        [Route("map/usage/{meterNumber}/{type}")]
        public async Task<IActionResult> MapUsage(string meterNumber, string meterType, int year, string type)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            OverviewProvider provider = new OverviewProvider(_cache, _options, _APIoptions);

            string reading = provider.GetMeterRigister(meterNumber, new String[] { "Active Energy", "Active Energy Import", "Water Consumption" }, type);

            return Content(JsonConvert.SerializeObject(reading), "application/json");
        }


        [Route("Usage")]
        public async Task<IActionResult> UsageMain()
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

            var meter = meters[0];

            var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
            if (device != null)
            {
                return Redirect("/usage/" + meter.Serial_No);
            }
            else
            {
                return NotFound();
            }
        }

        [Route("usage/{meterNumber}/{year?}/{month?}")]
        public async Task<IActionResult> Usage(string meterNumber, string year, string month)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");

            if (_userManager.IsInRoleAsync(_userManager.FindByIdAsync(_userManager.GetUserId(User)).Result, UserRoleEnum.Operational.ToString()).Result)
                return Redirect($"/operational/changeActiveMeter/{meterNumber}?R={HttpUtility.UrlEncode($"/operational/customer/Customer_Usage/{year}/{month}")}");

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);
            var name = "";
            var unitType = "";
            var color = "";
            var type = "";

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);

                if (device != null)
                {
                    meter.deviceType = device.type.type;
                    if (meter.Serial_No == meterNumber)
                    {
                        name = meter.No;
                        color = device.type.colorType;
                        type = device.type.type;
                        unitType = device.type.UnitType;
                    }
                }
            }

            var today = DateTime.Now;

            if (year != null && month != null)
            {
                try
                {
                    today = new DateTime(Int32.Parse(year), Int32.Parse(month), today.Day);
                }
                catch (Exception e)
                {
                    today = new DateTime(Int32.Parse(year), Int32.Parse(month), DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)));
                }
            }

            List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meterNumber, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);

            int multiplier = -1;

            if (_customerProvider.AccountType == (int)AccountTypeEnum.PostPaid)
                multiplier = 1;

            Decimal monthlyTotal = dailyTotals.Sum() * multiplier;

            return View(new MeterViewModel { MeterNumber = meterNumber, Name = name, ReadingDate = today, MeterType = type, MeterColor = color, UnitType = unitType, AllMeters = meters, MonthlyTotal = monthlyTotal });
        }

        [Route("monthlyUsage/{meterNumber}/{meterType}/{year}")]
        public async Task<IActionResult> MonthlyUsage(string meterNumber, string meterType, int year)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            if (meterType == "home")
            {
                var rentModelJson = _billingProvider.GetMonthlyInvoiceAmountForRent(_customerProvider.CustomerNumber, DateTime.Now.Year, _customerProvider.AccountType, _customerProvider.ShowCostInclVAT, meterNumber);

                return Content(JsonConvert.SerializeObject(rentModelJson), "application/json");
            }

            var meterJsonModel = GetMeterUsageByMonthView(meterNumber, meterType, year, 1);

            meterJsonModel.Data = fixDemandData(meterNumber, meterJsonModel.Data);

            return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
        }

        [Route("dailyUsage/{meterNumber}/{meterType}/{year}/{month}")]
        public async Task<IActionResult> DailyUsage(string meterNumber, string meterType, int year, int month)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var meterJsonModel = GetMeterUsageByDayView(meterNumber, meterType, year, month);

            meterJsonModel.Data = fixDemandData(meterNumber, meterJsonModel.Data);

            return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
        }

        [Route("hourlyUsage/{meterNumber}/{meterType}/{year}/{month}/{day}")]
        public async Task<IActionResult> HourlyUsage(string meterNumber, string meterType, int year, int month, int day)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

            var meterJsonModel = GetMeterUsageHourView(meterNumber, meterType, year, month, day);

            meterJsonModel.Data = fixDemandData(meterNumber, meterJsonModel.Data);

            return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
        }


        [Route("historicaldata/dailyTotals/{month}")]
        public async Task<IActionResult> HistoricaldataMothlyDailyTotals(string month)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            string[] yearMonth = month.Split('-');

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

            HistoricalDataModel combinedHistoricalDataModel = HistoricaldataMothlyDailyTotals(meters, Int32.Parse(yearMonth[0]), Int32.Parse(yearMonth[1]), 2);

            return Content(JsonConvert.SerializeObject(combinedHistoricalDataModel), "application/json");
        }

        [Route("deviceDetails/{deviceId}")]
        public async Task<IActionResult> GatewayDevicesDetails(string id, string deviceId)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            OverviewProvider provider = new OverviewProvider(_cache, _options, _APIoptions);

            NewRegisterViewModel registerViewModel = provider.GetNewMeterRigisterView(deviceId);

            GatewayDevice gatewayDevice = new GatewayDevice();

            gatewayDevice.register = registerViewModel;

            return Content(JsonConvert.SerializeObject(gatewayDevice), "application/json");
        }

        [Route("customerDetailsByCustomerNo/{customerNumber}")]
        public async Task<IActionResult> GetCustomerDetailsByCustomerNo(string customerNumber)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            CustomerDetails customerDetails = apiClient.GetCustomerDetailsByCustomerNo(customerNumber, _customerProvider.CompanyName);

            return Content(JsonConvert.SerializeObject(customerDetails), "application/json");

        }

        [Route("meter/meterSTS/{sts}/{deviceId}")]
        public async Task<IActionResult> MeterSTS(String sts, String deviceId)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");

            #region Contactor State

            Dictionary<int, string> registers = new Dictionary<int, string>();
            registers.Add(91, "readings");

            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

            string errorMessage = "";
            var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
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

            var device = _client.GetDeviceByID(Convert.ToInt32(deviceId));

            Boolean result = _client.MeterSTS(sts.Trim(), deviceId, "MyMeterSA - Manual STS", null, errorMessage, device.device.deviceStatus, device.device.status.time);
            return Content("{\"result\":true}", "application/json");
        }

        [Route("meter/meterPrism/{type}/{serial}/{deviceId}")]
        public async Task<IActionResult> MeterPrism(String type, String serial, String deviceId)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var userId = _userManager.GetUserId(User);
            string token = "";
            if (type == "set-postpaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userId);
            }
            else if (type == "set-prepaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userId);
            }
            else
            {
                PrismApiClient prismApiClient = new PrismApiClient(_options);
                token = prismApiClient.GenerateToken(serial, type, "MyMeterSA - Manual " + type, null, "", userId);
            }

            if (!String.IsNullOrEmpty(token))
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
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

                var device = _client.GetDeviceByMeterNumber(serial);

                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

                Boolean result = _client.MeterSTS(token, deviceId, "MyMeterSA - Manual " + type, null, errorMessage, device.deviceStatus, meterStatusTime);

                return Content("{\"result\":true}", "application/json");
            }

            return Ok();
        }

        [Route("connect/{actionId}/{deviceId}/{typeID}/{serial}")]
        public async Task<IActionResult> ConnectMeter(int actionId, String deviceId, int typeID, String serial)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            GatewayDevice[] gateways = _client.GetGateways();

            GatewayDevice gatewayDevice = new GatewayDevice();

            foreach (var gateway in gateways)
            {
                GatewayDevice[] gatewayDevices = _client.GetGatewayDevices(gateway.id.ToString());

                foreach (var gd in gatewayDevices)
                {
                    if (gd.serial == serial)
                    {
                        gatewayDevice = gd;
                        continue;
                    }
                }
            }

            var portNum = gatewayDevice.mapping.port;
            var control = 2;

            if (typeID == 1 && portNum == 2)
            {
                control = 3;
            }

            #region Contactor State

            Dictionary<int, string> registers = new Dictionary<int, string>();
            registers.Add(91, "readings");

            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

            string errorMessage = "";
            var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
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
            if (gatewayDevice.status != null)
                try
                {
                    meterStatusTime = Convert.ToDateTime(gatewayDevice.status.time);
                }
                catch { }

            Boolean result = _client.ConnectMeter(actionId, deviceId, control, "MyMeterSA - Manual Connect", null, errorMessage, gatewayDevice.deviceStatus, meterStatusTime);

            if (control == 3)
            {
                if (actionId == 0) // Disconnect meter. Contractor State is Connected
                {
                    var sendSTSResult = await MeterPrism("set-prepaid", serial, deviceId);
                }
                else if (actionId == 1) // Connect meter. Contractor State is Disconnected
                {
                    var sendSTSResult = await MeterPrism("set-postpaid", serial, deviceId);
                }
            }

            return Content("{\"result\":true}", "application/json");
        }

        [Route("historicaldata")]
        public async Task<IActionResult> HistoricalData()
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            HistoricalDataModel combinedHistoricalDataModel = new HistoricalDataModel();
            combinedHistoricalDataModel.MeterData = new List<HistoricalDataModel>();

            if (hasMeter(meters, "elec"))
            {
                HistoricalDataModel historicalElecDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "elec", DateTime.Now.Year);
                combinedHistoricalDataModel.ElecTotals = historicalElecDataMonthlyTotals.Totals;
                HistoricalDataModel electricityMeterTotals = HistoricalDataMeterTotals(meters, 2, "elec", DateTime.Now.Year);
                electricityMeterTotals.MeterType = "elec";
                combinedHistoricalDataModel.MeterData.Add(electricityMeterTotals);
            }

            if (hasMeter(meters, "water"))
            {
                HistoricalDataModel historicalWaterDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "water", DateTime.Now.Year);
                combinedHistoricalDataModel.WaterTotals = historicalWaterDataMonthlyTotals.Totals;

                HistoricalDataModel waterMeterTotals = HistoricalDataMeterTotals(meters, 2, "water", DateTime.Now.Year);
                waterMeterTotals.MeterType = "water";
                combinedHistoricalDataModel.MeterData.Add(waterMeterTotals);
            }

            if (hasMeter(meters, "gas"))
            {
                HistoricalDataModel historicalGasDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "gas", DateTime.Now.Year);
                combinedHistoricalDataModel.GasTotals = historicalGasDataMonthlyTotals.Totals;
                HistoricalDataModel gasMeterTotals = HistoricalDataMeterTotals(meters, 2, "gas", DateTime.Now.Year);
                gasMeterTotals.MeterType = "gas";
                combinedHistoricalDataModel.MeterData.Add(gasMeterTotals);
            }

            List<DateTime> months = Enumerable
                                .Range(0, (12 * 2))
                                .Select(i => toDate.AddMonths(i - (12 * 2)))
                                .Select(date => date).ToList();

            var rentData = _billingProvider.GetMonthlyInvoiceAmountForRent(months, _customerProvider.ShowCostInclVAT);

            if (rentData != null)
            {
                HistoricalDataModel historicalRentDataMonthlyTotals = new HistoricalDataModel();
                combinedHistoricalDataModel.RentTotals = rentData.Data.Item1.ToList();
                combinedHistoricalDataModel.RentTotals.Insert(0, 0m);

                HistoricalDataModel rentTotals = new HistoricalDataModel();
                rentTotals.MeterType = "rent";
                rentTotals.Totals = rentData.Data.Item1;
                rentTotals.RentTotals = rentData.Data.Item1;

                rentTotals.Months = months;

                combinedHistoricalDataModel.MeterData.Add(rentTotals);
            }

            var otherMonths = Enumerable
                                .Range(0, (25))
                                .Select(i => toDate.AddMonths(i - (25)))
                                .Select(date => date).ToList();


            combinedHistoricalDataModel.Months = otherMonths;

            return View(combinedHistoricalDataModel);
        }

        [Route("historicaldata/monthlyUsage")]
        public async Task<IActionResult> monthlyUsage()
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

            HistoricalDataModel combinedHistoricalDataModel = new HistoricalDataModel();

            if (hasMeter(meters, "elec"))
            {
                HistoricalDataModel historicalElecDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "elec", DateTime.Now.Year);

                HistoricalDataModel historicalElecDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "elec", DateTime.Now.Year);

                combinedHistoricalDataModel.ElecTotals = historicalElecDataModelTotals.Totals;
                combinedHistoricalDataModel.ElecUsages = historicalElecDataModelUsages.Usages;
            }

            if (hasMeter(meters, "water"))
            {
                HistoricalDataModel historicalWaterDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "water", DateTime.Now.Year);

                HistoricalDataModel historicalWaterDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "water", DateTime.Now.Year);

                combinedHistoricalDataModel.WaterTotals = historicalWaterDataModelTotals.Totals;
                combinedHistoricalDataModel.WaterUsages = historicalWaterDataModelUsages.Usages;
            }

            if (hasMeter(meters, "gas"))
            {
                HistoricalDataModel historicalGasDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "gas", DateTime.Now.Year);

                HistoricalDataModel historicalGasDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "gas", DateTime.Now.Year);

                combinedHistoricalDataModel.GasTotals = historicalGasDataModelTotals.Totals;
                combinedHistoricalDataModel.GasUsages = historicalGasDataModelUsages.Usages;
            }

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            var months = Enumerable
                                .Range(0, (25))
                                .Select(i => toDate.AddMonths(i - (25)))
                                .Select(date => date).ToList();

            var rentData = _billingProvider.GetMonthlyInvoiceAmountForRent(months, _customerProvider.ShowCostInclVAT);

            if (rentData != null)
            {
                combinedHistoricalDataModel.RentTotals = rentData.Data.Item1.ToList();
                combinedHistoricalDataModel.RentTotals.Insert(0, 0m);
            }

            combinedHistoricalDataModel.Months = months;

            return Content(JsonConvert.SerializeObject(combinedHistoricalDataModel), "application/json");
        }

        [Authorize(Roles = "CompanyAdmin")]
        [Route("odoreadings")]
        public async Task<IActionResult> OdoReadings()
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            List<OdoReadingsModel> odoReadings = new List<OdoReadingsModel>();
            var _MVcontext = new MyVoltageDbContext(_options);
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            var meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);


            var _APIDataContext = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

            foreach (var meter in meters)
            {
                var apiDevice = _APIDataContext.Devices.Where(p => p.Serial == meter.Serial_No).OrderByDescending(p => p.Id).FirstOrDefault();
                if (apiDevice != null)
                {
                    var mvDevice = _MVcontext.Devices.Where(p => p.Serial == apiDevice.Serial).SingleOrDefault();
                    string company = mvDevice != null && mvDevice.CompanyID.HasValue ? _MVcontext.Companies.Where(p => p.CompanyID == mvDevice.CompanyID.Value).SingleOrDefault().Name : "00Unknown";
                    MyVoltage.Data.SkybillCustomer skybillCustomer = mvDevice != null ? _MVcontext.SkybillCustomers.Where(p => p.DeviceID.HasValue && p.DeviceID.Value == mvDevice.Id).FirstOrDefault()
                    : null;

                    #region Create company folder

                    string dirUrl = $"{company}";

                    if (skybillCustomer != null)
                        dirUrl = dirUrl + $"/{skybillCustomer.No}";

                    #endregion

                    string FTPUserName = "photouploader";
                    string FTPPassword = "mRdgMQC3Tw7j";
                    var filesOnServer = FTPProvider.GetFilesInFolder(dirUrl, FTPUserName, FTPPassword);

                    var odosInDB = _APIDataContext.OdoReadings.Where(itm => itm.DeviceId == apiDevice.Id).OrderByDescending(p => p.CreateDate).ToList();


                    foreach (var odo in odosInDB)
                    {
                        string fileName = apiDevice.Serial + odo.TimeLogged.ToString("_yyyy_MM_dd_HH_mm");

                        var filenameOnServer = (from p in filesOnServer
                                                where p.ToUpper().Contains(fileName.ToUpper())
                                                select p).SingleOrDefault();

                        Console.WriteLine("Local:" + fileName);
                        Console.WriteLine("ServerSearch:" + filenameOnServer);

                        odoReadings.Add(new OdoReadingsModel
                        {
                            CreateDate = odo.CreateDate.ToString("yyyy/MM/dd HH:mm:ss"),
                            Serial = apiDevice.Serial,
                            Id = odo.Id,
                            OdometerReading = (int)odo.OdometerReading,
                            TimeLogged = odo.TimeLogged.ToString("yyyy/MM/dd HH:mm"),
                            PhotoFtpUrl = !string.IsNullOrEmpty(filenameOnServer) ? $"{_customerProvider.CompanyName}/{filenameOnServer}" : ""
                        });
                    }
                }
            }

            return View(odoReadings);
        }

        [HttpGet, ActionName("GetPhoto")]
        //[HttpGet("odoreadings/getphoto/{id}")]
        [Route("odoreadings/getphoto/{id}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            if (_customerProvider.RedirectToRecharge)
                return Redirect("/companyadmin/recharge");
            var _MVcontext = new MyVoltageDbContext(_options);
            var _context = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

            var odo = _context.OdoReadings.Where(p => p.Id == id).SingleOrDefault();
            var device = _context.Devices.Where(p => p.Id == odo.DeviceId).SingleOrDefault();
            var mvDevice = _MVcontext.Devices.Where(p => p.Serial == device.Serial).SingleOrDefault();
            string company = mvDevice != null && mvDevice.CompanyID.HasValue ? _MVcontext.Companies.Where(p => p.CompanyID == mvDevice.CompanyID.Value).SingleOrDefault().Name : "00Unknown";
            MyVoltage.Data.SkybillCustomer skybillCustomer = mvDevice != null ? _MVcontext.SkybillCustomers.Where(p => p.DeviceID.HasValue && p.DeviceID.Value == mvDevice.Id).FirstOrDefault()
            : null;

            #region Create company folder

            string dirUrl = $"{company}";

            if (skybillCustomer != null)
                dirUrl = dirUrl + $"/{skybillCustomer.No}";

            #endregion

            string FTPUserName = "photouploader";
            string FTPPassword = "mRdgMQC3Tw7j";
            var filesOnServer = FTPProvider.GetFilesInFolder(dirUrl, FTPUserName, FTPPassword);

            string fileName = device.Serial + odo.TimeLogged.ToString("_yyyy_MM_dd_HH_mm");

            var filenameOnServer = (from p in filesOnServer
                                    where p.ToUpper().Contains(fileName.ToUpper())
                                    select p).SingleOrDefault();

            var file = FTPProvider.DownloadFile(dirUrl + "/" + Path.GetFileName(filenameOnServer), FTPUserName, FTPPassword);

            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(filenameOnServer, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (file != null)
                return File(file, contentType, Path.GetFileName(filenameOnServer));
            else
                return NotFound();

            //return Redirect($"~/odoreadings/{deviceId}");
        }

        private HistoricalDataModel HistoricalDataMeterTotals(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {
            HistoricalDataModel historicalDataModelTotals = HistoricaldataMonthlyTotals(meters, years, meterType, year);

            HistoricalDataModel historicalDataModelUsages = HistoricaldataMonthlyUsage(meters, years, meterType, year);

            List<Decimal> monthlyAvgs = new List<Decimal>();

            for (int i = 0; i < historicalDataModelTotals.Totals.Count; i++)
            {
                Decimal usage = historicalDataModelUsages.Usages[i];

                if (usage > 0)
                {
                    Decimal total = historicalDataModelTotals.Totals[i];
                    var avg = (total / usage);
                    monthlyAvgs.Add(avg);
                }
                else
                {
                    monthlyAvgs.Add(0);
                }
            }

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, (12 * years))
                                .Select(i => toDate.AddMonths(i - (12 * years)))
                                .Select(date => date).ToList();

            HistoricalDataModel combinedHistoricalDataModel = new HistoricalDataModel();
            combinedHistoricalDataModel.Totals = historicalDataModelTotals.Totals;
            combinedHistoricalDataModel.Months = historicalDataModelTotals.Months;
            combinedHistoricalDataModel.Usages = historicalDataModelUsages.Usages;
            combinedHistoricalDataModel.Avgs = monthlyAvgs;

            return combinedHistoricalDataModel;
        }

        private MeterJsonModel GetMeterUsageByMonthView(string meterNumber, string meterType, int year, int years)
        {
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

            var user = _userManager.GetUserAsync(User).Result;

            int metertype = provider.GetMeterType(meterNumber, user.Id);

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var meterJsonModel = provider.GetMeterUsageByMonth(meterNumber, meterType, year, prePaidBalance, isSolar);

            // Code for fixing balances. Skybill heavy (gets all ledger entries for customer and calculates)
            //if (_customerProvider.AccountType != (int)AccountTypeEnum.PrepaidCredit)
            //{
            //    var balances = _billingProvider.Getm(_customerProvider.CustomerNumber, year, month, _customerProvider.AccountType);
            //    meterJsonModel.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(meterJsonModel.Data.Item1, balances, meterJsonModel.Data.Item3, meterJsonModel.Data.Item4);
            //}

            List<Decimal> monthlyInvoiceTotals = _billingProvider.GetMonthlyInvoiceAmountByMeter(meterNumber, year, _customerProvider.AccountType, years, _customerProvider.ShowCostInclVAT);
            List<Decimal> monthlyAvgs = new List<Decimal>();
            List<Decimal> mothhlyTotals = new List<Decimal>();

            var usage = meterJsonModel.Data.Item1;

            for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
            {
                Decimal monthyTotal = monthlyInvoiceTotals[i];

                if (monthlyInvoiceTotals[i] < 0)
                {
                    monthyTotal = monthlyInvoiceTotals[i] * -1;
                }

                mothhlyTotals.Add(monthyTotal);

                if (meterJsonModel.Data.Item1.Count > i && meterJsonModel.Data.Item1[i] > 0)
                {
                    var avg = (monthyTotal / meterJsonModel.Data.Item1[i]);
                    monthlyAvgs.Add(avg);
                }
                else
                {
                    monthlyAvgs.Add(0);
                }
            }

            meterJsonModel.Totals = mothhlyTotals;
            meterJsonModel.Avgs = monthlyAvgs;
            return meterJsonModel;
        }

        private HistoricalDataModel HistoricaldataMonthlyUsage(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    if (meterType != null && meter.deviceType != meterType)
                    {
                        continue;
                    }

                    MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = provider.GetMeterType(meter.Serial_No, user.Id);

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = provider.GetMeterUsageByMonth(meter.Serial_No, meterType, year, years, prePaidBalance, isSolar);

                    meterJsonModels.Add(meterJsonModel);
                }
            }

            var combinedHistoricalDataModel = new HistoricalDataModel();


            Decimal[] array = new Decimal[(12 * years) + 1];
            for (int i = 0; i < array.Length; i++)
                array[i] = 0;

            combinedHistoricalDataModel.Usages = array.ToList();

            foreach (MeterJsonModel meterJsonModel in meterJsonModels)
            {
                for (var y = 0; y < combinedHistoricalDataModel.Usages.Count; y++)
                {
                    combinedHistoricalDataModel.Usages[y] = combinedHistoricalDataModel.Usages[y] + meterJsonModel.Data.Item1[y];
                }
            }

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, (12 * years))
                                .Select(i => toDate.AddMonths(i - (12 * years)))
                                .Select(date => date).ToList();

            combinedHistoricalDataModel.Months = months;

            return combinedHistoricalDataModel;

        }

        private HistoricalDataModel HistoricaldataMonthlyTotals(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            DateTime lastMonth = DateTime.Now;
            lastMonth = lastMonth.AddMonths(-1);
            int days = DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month);

            Dictionary<string, float> dailyTotals = new Dictionary<string, float>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);

                if (device != null)
                {

                    meter.deviceType = device.type.type;

                    if (meterType != null && meter.deviceType != meterType)
                    {
                        continue;
                    }

                    MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = provider.GetMeterType(meter.Serial_No, user.Id);

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = provider.GetMeterUsageByMonth(meter.Serial_No, meterType, year, years, prePaidBalance, isSolar);

                    List<Decimal> monthlyInvoiceTotals = _billingProvider.GetHistoricalMonthlyInvoiceAmountByMeter(meter.Serial_No, DateTime.Now.Year, _customerProvider.AccountType, years, _customerProvider.ShowCostInclVAT);
                    List<Decimal> motnhlyTotals = new List<Decimal>();

                    for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
                    {
                        Decimal monthyTotal = 0;
                        if (monthlyInvoiceTotals[i] < 0)
                        {
                            monthyTotal = monthlyInvoiceTotals[i] * -1;
                        }
                        else
                        {
                            monthyTotal = monthlyInvoiceTotals[i];
                        }

                        motnhlyTotals.Add(monthyTotal);
                    }

                    meterJsonModel.Totals = motnhlyTotals;

                    meterJsonModels.Add(meterJsonModel);
                }

            }

            var combinedHistoricalDataModel = new HistoricalDataModel();

            Decimal[] monthlyTotalsArray = new Decimal[(12 * years) + 1];
            for (int i = 0; i < monthlyTotalsArray.Length; i++)
                monthlyTotalsArray[i] = 0;

            combinedHistoricalDataModel.Totals = monthlyTotalsArray.ToList();

            foreach (MeterJsonModel meterJsonModel in meterJsonModels)
            {
                for (var y = 0; y < monthlyTotalsArray.Length; y++)
                {
                    combinedHistoricalDataModel.Totals[y] = combinedHistoricalDataModel.Totals[y] + meterJsonModel.Totals[y];
                }
            }


            DateTime startDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, 24)
                                .Select(i => startDate.AddMonths(i - 24))
                                .Select(date => date).ToList();

            combinedHistoricalDataModel.Months = months;
            return combinedHistoricalDataModel;
        }

        private MeterJsonModel GetMeterUsageByDayView(string meterNumber, string meterType, int year, int month)
        {
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);
            var user = _userManager.GetUserAsync(User).Result;
            int metertype = provider.GetMeterType(meterNumber, user.Id);

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var model = provider.GetMeterUsageByDay(meterNumber, meterType, year, month, prePaidBalance, isSolar);

            if (_customerProvider.AccountType != (int)AccountTypeEnum.PrepaidCredit)
            {
                var balances = _billingProvider.GetDailyBalances(_customerProvider.CustomerNumber, year, month, _customerProvider.AccountType);
                model.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(model.Data.Item1, balances, model.Data.Item3, model.Data.Item4);
            }

            List<Decimal> invoiceDailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meterNumber, year, month, _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);

            List<Decimal> dailyAvgs = new List<Decimal>();
            List<Decimal> dailyTotals = new List<Decimal>();

            for (int i = 0; i < invoiceDailyTotals.Count; i++)
            {
                Decimal dailyTotal = invoiceDailyTotals[i];

                if (invoiceDailyTotals[i] < 0)
                {
                    dailyTotal = invoiceDailyTotals[i] * -1;
                }

                dailyTotals.Add(dailyTotal);

                if (model.Data.Item1[i] > 0)
                {
                    var avg = (dailyTotal / model.Data.Item1[i]) * 1000;
                    dailyAvgs.Add(avg);
                }
                else
                {
                    dailyAvgs.Add(0);
                }
            }

            model.Totals = dailyTotals;
            model.Avgs = dailyAvgs;
            return model;

        }


        private MeterJsonModel GetMeterUsageHourView(string meterNumber, string meterType, int year, int month, int day)
        {

            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);
            var user = _userManager.GetUserAsync(User).Result;
            int metertype = provider.GetMeterType(meterNumber, user.Id);

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var model = provider.GetMeterUsageByHour(meterNumber, meterType, year, month, day, prePaidBalance, isSolar);

            if (_customerProvider.AccountType != (int)AccountTypeEnum.PrepaidCredit)
            {
                if (model.Data.Item2 != null)
                {
                    var balances = _billingProvider.GetDailyBalances(_customerProvider.CustomerNumber, year, month, _customerProvider.AccountType);
                    Decimal[] n = new Decimal[24];

                    for (int i = 0; i < 24; i++)
                    {
                        n[i] = balances[day - 1];
                    }

                    balances = n.ToList();

                    model.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(model.Data.Item1, balances, model.Data.Item3, model.Data.Item4);
                }
            }

            return model;
        }

        private Boolean hasMeter(List<SkyBillCustomer> meters, string meterType)
        {
            Boolean hasMeter = false;

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);


            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    if (meter.deviceType == meterType)
                    {
                        return true;
                    }
                }
            }

            return hasMeter;

        }

        private HistoricalDataModel HistoricaldataMothlyDailyTotals(List<SkyBillCustomer> meters, int year, int month, int years)
        {
            int days = DateTime.DaysInMonth(year, month);

            DateTime monthDateTime = new DateTime(year, month, 1);

            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            Dictionary<string, Decimal> dailyTotals = new Dictionary<string, Decimal>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = provider.GetMeterType(meter.Serial_No, user.Id);

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = provider.GetMeterUsageByMonth(meter.Serial_No, meter.deviceType, year, years, prePaidBalance, isSolar);

                    List<Decimal> monthlyInvoiceTotals = _billingProvider.GetMonthlyInvoiceAmountByMeter(meter.No, year, _customerProvider.AccountType, 2, _customerProvider.ShowCostInclVAT);
                    List<Decimal> mothhlyTotals = new List<Decimal>();

                    for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
                    {
                        Decimal monthyTotal = 0;
                        if (monthlyInvoiceTotals[i] < 0)
                        {
                            monthyTotal = monthlyInvoiceTotals[i] * -1;
                        }

                        mothhlyTotals.Add(monthyTotal);
                    }

                    meterJsonModel.Totals = mothhlyTotals;

                    meterJsonModels.Add(meterJsonModel);

                    MeterJsonModel dailyMeterJsonModel = GetMeterUsageByDayView(meter.No, meter.deviceType, monthDateTime.Year, monthDateTime.Month);

                    for (var a = 0; a < days; a++)
                    {
                        DateTime day = new DateTime(monthDateTime.Year, monthDateTime.Month, a + 1);

                        var key = day.ToString("ddd dd");

                        if (!dailyTotals.ContainsKey(key))
                        {
                            dailyTotals.Add(key, dailyMeterJsonModel.Totals[a]);
                        }
                        else
                        {
                            dailyTotals[key] = dailyTotals[key] + dailyMeterJsonModel.Totals[a];
                        }
                    }
                }
            }

            var combinedHistoricalDataModel = new HistoricalDataModel();

            combinedHistoricalDataModel.dailyTotals = new Tuple<List<String>, List<Decimal>>(new List<string>(dailyTotals.Keys), dailyTotals.Values.ToList());

            return combinedHistoricalDataModel;
        }

        private Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> fixDemandData(string meterNumber, Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> data)
        {
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);
            var user = _userManager.GetUserAsync(User).Result;
            int metertype = provider.GetMeterType(meterNumber, user.Id);

            if (metertype != 0)
            {
                if (metertype == (int)MeterTypeEnum.Balance)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, null, null);
                }
                else if (metertype == (int)MeterTypeEnum.Demand)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, null);
                }
                else if (metertype == (int)MeterTypeEnum.Solar)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, data.Item4);
                }
                else if (metertype == (int)MeterTypeEnum.None)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, null, null, null);
                }
            }
            return data;
        }

        private List<MyVoltage.Api.MyVoltage.Device> getDevices(string page = null, string limit = null)
        {
            var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
            List<SkyBillCustomer> meters = new List<SkyBillCustomer>();
            if (User.IsInRole("CompanyAdmin"))
            {
                meters = apiClient.GetAllCustomerMeters();
            }
            else
            {
                meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);
            }


            List<MyVoltage.Api.MyVoltage.Device> devices = new List<MyVoltage.Api.MyVoltage.Device>();

            MyVoltage.Api.MyVoltage.Device[] allDevicesActual = _client.GetAllDevices();

            IDeviceApi client = new DeviceFactory().CreateDeviceApi(_cache, true, _options, null);

            MyVoltage.Api.MyVoltage.Device[] allDevicesMirror = _client.GetAllDevices();

            foreach (SkyBillCustomer c in meters)
            {
                var d = allDevicesActual.Where(tbl => (tbl.serial == c.Serial_No)).FirstOrDefault();

                if (d != null)
                {
                    MyVoltage.Api.MyVoltage.Device newDevice = (MyVoltage.Api.MyVoltage.Device)d.Clone();
                    newDevice.serial = c.Serial_No;
                    newDevice.gps_coordinates = c.GPS_Coordinates;
                    newDevice.partner_code = c.Partner_Code;
                    newDevice.name = c.No;
                    newDevice.account_type = c.customerDetails != null ? c.customerDetails.Billing_Cycle : "";
                    newDevice.customer_number = c.Customer_No;
                    newDevice.account = c.BILLING_CYCLE;
                    newDevice.balance = c.Balance_LCY.ToString("N2", new CultureInfo("en-GB"));
                    newDevice.mapClickable = true;
                    if (devices.Where(p => p.serial == newDevice.serial).Count() == 0)
                        devices.Add(newDevice);
                }
                else
                {
                    //Device mirrorDevice = client.GetDeviceByMeterNumber(c.Serial_No);
                    var mirrorDevice = allDevicesMirror.Where(tbl => (tbl.serial == c.Serial_No)).FirstOrDefault();

                    if (mirrorDevice != null)
                    {
                        var device = allDevicesActual.Where(tbl => (tbl.serial == mirrorDevice.name)).FirstOrDefault();
                        if (device != null)
                        {
                            MyVoltage.Api.MyVoltage.Device newDevice = (MyVoltage.Api.MyVoltage.Device)device.Clone();
                            newDevice.serial = c.Serial_No;
                            newDevice.gps_coordinates = c.GPS_Coordinates;
                            newDevice.partner_code = c.Partner_Code;
                            newDevice.mapClickable = false;
                            newDevice.customer_number = c.Customer_No;
                            newDevice.customer_number = c.Customer_No;
                            newDevice.account_type = c.customerDetails != null ? c.customerDetails.Billing_Cycle : "";
                            newDevice.account = c.BILLING_CYCLE;
                            newDevice.balance = c.Balance_LCY.ToString("N2", new CultureInfo("en-GB"));
                            newDevice.name = c.No;
                            if (devices.Where(p => p.serial == newDevice.serial).Count() == 0)
                                devices.Add(newDevice);
                        }
                    }
                    else
                    {
                        MyVoltage.Api.MyVoltage.Device newDevice = new MyVoltage.Api.MyVoltage.Device();
                        newDevice.serial = c.Serial_No;
                        newDevice.gps_coordinates = c.GPS_Coordinates;
                        newDevice.partner_code = "NOT LINKED";
                        newDevice.customer_number = c.Customer_No;
                        newDevice.account = c.BILLING_CYCLE;
                        newDevice.account_type = c.customerDetails != null ? c.customerDetails.Billing_Cycle : "";
                        newDevice.balance = c.Balance_LCY.ToString("N2", new CultureInfo("en-GB"));
                        newDevice.mapClickable = false;
                        newDevice.name = c.No;
                        if (devices.Where(p => p.serial == newDevice.serial).Count() == 0)
                            devices.Add(newDevice);
                    }
                }
            }

            return devices;
        }


        [HttpGet]
        [Route("/emergencyConnectMeter/{serialNo}")]
        public async Task<IActionResult> EmergencyConnectMeter(string serialNo)
        {
            if (string.IsNullOrEmpty(serialNo))
                return View("~/Views/Meter/EmergencyConnectMeter.cshtml");

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = _userManager.GetUserAsync(User).Result;
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                if (User.IsInRole("CompanyAdmin"))
                {
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
                }

                var m2mDevice = _client.GetDeviceByMeterNumber(serialNo);
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var skybillCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);
                float balance = skybillCustomer.Balance_LCY * -1;

                if (m2mDevice == null
                    || skybillCustomer == null)
                    return View("~/Views/Meter/EmergencyConnectMeter.cshtml");

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

                if (balance >= -50 && !isContactorConnected)
                {
                    var _prismApiClient = new PrismApiClient(_options);

                    if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                    {
                        //var token = _prismApiClient.GenerateToken(serialNo, "set-postpaid", "Emergency Connect Meter", Convert.ToDecimal(balance), "", user.Id);
                        var _prismVendClient = new PrismVendClient(_options);
                        var token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serialNo, 0, "Emergency Connect Meter", Convert.ToDecimal(balance), "", user.Id);
                        if (!string.IsNullOrEmpty(token))
                        {
                            _client.MeterSTS(token, m2mDevice.id.ToString(), "Emergency Connect Meter", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);
                        }
                        else
                        {
                            string emailBody = $"There was an error generating token for {serialNo}";
                            EmailSender emailSender = new EmailSender();
                            await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                            , "Token Generation Error - EmergencyConnectMeter"
                            , emailBody
                            , emailBody);
                        }

                    }
                    else if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("PREPAID"))
                    {
                        decimal amount = 50;
                        var token = _prismApiClient.GenerateToken(serialNo, Convert.ToDouble(amount), "Emergency Connect Meter", amount, "", user.Id);

                        if (!string.IsNullOrEmpty(token))
                        {
                            _client.MeterSTS(token, m2mDevice.id.ToString(), "Emergency Connect Meter", amount, errorMessage, m2mDevice.deviceStatus, meterStatusTime);

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
                            journal.Description = $"Emergency Connect Meter - {_customerProvider.CustomerNumber}";
                            journal.Amount = Convert.ToDecimal(amount);
                            journal.Bal_Account_TypeSpecified = true;
                            journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                            journal.Bal_Account_No = "6610";

                            SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);


                            PaymentProvider provider = new PaymentProvider(_configuration, _options);

                            var journalEntry = await provider.CreateSalesJournalEntry(new Company() { Name = _customerProvider.CompanyName }, journal);

                            var getRecIdFromKey = await provider.GetSalesRecIdFromKey(journalEntry, new Company() { Name = _customerProvider.CompanyName });

                            var postReceiptJournal = await provider.PostSalesReceiptJournalAsync(getRecIdFromKey, new Company() { Name = _customerProvider.CompanyName });

                            #endregion



                        }
                        else
                        {
                            string emailBody = $"There was an error generating token for {serialNo}";
                            EmailSender emailSender = new EmailSender();
                            await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                            , "Token Generation Error - Emergency Connect Meter"
                            , emailBody
                            , emailBody);
                        }
                    }

                }
            }

            return View("~/Views/Meter/EmergencyConnectMeter.cshtml");
        }

        [HttpGet]
        [Route("/switchMeter/{serialNo}")]
        public async Task<IActionResult> SwitchMeter(string serialNo)
        {
            if (string.IsNullOrEmpty(serialNo))
                return View("~/Views/Meter/SwitchMeter.cshtml");

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = _userManager.GetUserAsync(User).Result;
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                if (User.IsInRole("CompanyAdmin"))
                {
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
                }

                var m2mDevice = _client.GetDeviceByMeterNumber(serialNo);
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
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
                        var token = _prismApiClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serialNo, 0, "Switch Meter", Convert.ToDecimal(balance), "", user.Id);
                        if (!string.IsNullOrEmpty(token))
                        {
                            _client.MeterSTS(token, m2mDevice.id.ToString(), "Switch Meter", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);

                            System.Threading.Thread thread = new System.Threading.Thread(() => SleepThenReconnectMeter(serialNo, customer.CustomerNumber, _customerProvider.CompanyName));
                            thread.Start();


                        }
                        else
                        {
                            string emailBody = $"There was an error generating token for {serialNo}";
                            EmailSender emailSender = new EmailSender();
                            await emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                            , "Token Generation Error - Switch Meter"
                            , emailBody
                            , emailBody);
                        }

                    }
                }
            }

            return View("~/Views/Meter/SwitchMeter.cshtml");
        }

        public void SleepThenReconnectMeter(string serialNo, string customerNo, string companyName)
        {
            int oneSec = 1000;
            int oneMin = oneSec * 60;
            int sleepDuration = oneMin * 3;

            System.Threading.Thread.Sleep(sleepDuration);

            var m2mDevice = _client.GetDeviceByMeterNumber(serialNo);
            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(companyName, _cache);
            var skybillCustomer = skyBillApiClient.GetCustomer(customerNo);
            float balance = skybillCustomer.Balance_LCY * -1;

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

            var _prismApiClient = new PrismApiClient(_options);

            if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
            {
                var _prismVendClient = new PrismVendClient(_options);
                var token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serialNo, 0, "Switch Meter Reconnect", Convert.ToDecimal(balance), "", "");
                //var token = _prismApiClient.GenerateToken(serialNo, "set-postpaid", "Switch Meter Reconnect", Convert.ToDecimal(balance), "", "");
                if (!string.IsNullOrEmpty(token))
                {
                    _client.MeterSTS(token, m2mDevice.id.ToString(), "Switch Meter Reconnect", Convert.ToDecimal(balance), errorMessage, m2mDevice.deviceStatus, meterStatusTime);
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
                    , "Token Generation Error - SwitchMeterReconnect"
                    , emailBody
                    , emailBody);
                }

            }

        }

        //[HttpPost]
        //[Route("/adddevicegatewaysearch")]
        //public JsonResult AddDeviceGatewaySearch(string Prefix)
        //{
        //    MyVoltageDbContext db = new MyVoltageDbContext(_options);

        //    List<object> results = new List<object>();

        //    var devices = (from p in db.Devices
        //                   where
        //                   (
        //                   p.Serial.Contains(Prefix)
        //                   || p.Name.Contains(Prefix)
        //                   )
        //                   && p.TypeID.HasValue
        //                   && p.TypeID.Value == (int)DeviceType.Electricity
        //                   && p.ActiveStatusID.HasValue
        //                   && p.ActiveStatusID.Value == 1
        //                   select p).Take(10);

        //    foreach (var d in devices)
        //    {
        //        string text = $"{d.Serial} ({d.Name})";

        //        results.Add(new
        //        {
        //            Text = text,
        //            Value = d.Serial
        //        });
        //    }

        //    return Json(results);//, JsonRequestBehavior.AllowGet);
        //}


        [Authorize(Roles = "Technician")]
        [HttpGet]
        [Route("/adddevice")]
        public async Task<IActionResult> AddDevice()
        {

            return View("~/Views/Meter/AddDevice.cshtml");
        }

        [Authorize(Roles = "Technician")]
        [HttpPost]
        [Route("/adddevice")]
        public async Task<IActionResult> AddDevice(AddDeviceViewModel model)
        {
            var gateway = _client.GetGateway(model.GatewayID.ToString());

            if (gateway == null)
                model.ErrorMessage = $"Gateway with ID {model.GatewayID} does not exist";
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                model.ErrorMessage = $"Gateway with ID {model.GatewayID} ({gateway.name}) is offline";
            else
                return Redirect("/adddevicestep2/" + model.GatewayID);


            return View("~/Views/Meter/AddDevice.cshtml", model);
        }

        [Authorize(Roles = "Technician")]
        [HttpGet]
        [Route("/adddevicestep2/{GWID}")]
        public async Task<IActionResult> AddDeviceStep2(int GWID)
        {
            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/adddevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/adddevice");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterTypes = db.MeterTypes.Where(p => p.GatewayHardwareType == gateway.type.name).ToList();

            List<SelectListItem> meterTypesList = new List<SelectListItem>();

            foreach (var mt in meterTypes)
                meterTypesList.Add(new SelectListItem()
                {
                    Value = mt.ID.ToString(),
                    Text = mt.TypeName
                });

            AddDeviceStep2ViewModel model = new AddDeviceStep2ViewModel()
            {
                GatewayID = GWID,
                GatewayName = gateway.name,
                GatewayHardwareType = gateway.type.name,
                MeterTypes = meterTypesList
            };


            return View("~/Views/Meter/AddDeviceStep2.cshtml", model);
        }

        [Authorize(Roles = "Technician")]
        [HttpPost]
        [Route("/adddevicestep2/{GWID}")]
        public async Task<IActionResult> AddDeviceStep2(int GWID, AddDeviceStep2ViewModel model)
        {
            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/adddevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/adddevice");

            var meterTypeID = Request.Form["MeterType"];

            if (!string.IsNullOrEmpty(meterTypeID))
            {
                return Redirect($"/adddevicestep3/{GWID}/{meterTypeID}");
            }
            else
            {
                model.ErrorMessage = "Please choose a meter type";

                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var meterTypes = db.MeterTypes.Where(p => p.GatewayHardwareType == gateway.type.name).ToList();

                List<SelectListItem> meterTypesList = new List<SelectListItem>();

                foreach (var mt in meterTypes)
                    meterTypesList.Add(new SelectListItem()
                    {
                        Value = mt.ID.ToString(),
                        Text = mt.TypeName,
                        Selected = meterTypeID == mt.ID.ToString() ? true : false
                    });

                model.GatewayID = GWID;
                model.GatewayName = gateway.name;
                model.GatewayHardwareType = gateway.type.name;
                model.MeterTypes = meterTypesList;

            }

            return View("~/Views/Meter/AddDeviceStep2.cshtml", model);
        }

        [Authorize(Roles = "Technician")]
        [HttpGet]
        [Route("/adddevicestep3/{GWID}/{MTID}")]
        public async Task<IActionResult> AddDeviceStep3(int GWID, int MTID)
        {
            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/adddevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/adddevice");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/adddevice");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/adddevice");

            AddDeviceStep3ViewModel model = new AddDeviceStep3ViewModel()
            {
                GatewayID = GWID,
                GatewayName = gateway.name,
                GatewayHardwareType = gateway.type.name,
                MeterType = meterType.TypeName,
                ShowPort = meterType.Port.HasValue ? false : true,
                ShowProtocol = meterType.Protocol.HasValue ? false : true,
                ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true,
                ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true,
                ShowOdo = meterType.RequiresOdo.HasValue ? meterType.RequiresOdo.Value : false,
                DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "",
                Prefix = meterType.Prefix,
            };




            return View("~/Views/Meter/AddDeviceStep3.cshtml", model);
        }

        [Authorize(Roles = "Technician")]
        [HttpPost]
        [Route("/adddevicestep3/{GWID}/{MTID}")]
        public async Task<IActionResult> AddDeviceStep3(int GWID, int MTID, AddDeviceStep3ViewModel model)
        {
            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/adddevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/adddevice");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/adddevice");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/adddevice");

            model.GatewayID = GWID;
            model.GatewayName = gateway.name;
            model.GatewayHardwareType = gateway.type.name;
            model.MeterType = meterType.TypeName;
            model.ShowPort = meterType.Port.HasValue ? false : true;
            model.ShowProtocol = meterType.Protocol.HasValue ? false : true;
            model.ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true;
            model.ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true;
            model.ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true;
            model.ShowOdo = meterType.RequiresOdo.HasValue ? meterType.RequiresOdo.Value : false;
            model.DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "";
            model.Prefix = meterType.Prefix;


            if (ModelState.IsValid)
            {
                string serial = meterType.Prefix + model.SerialNumber;
                //// Check if device already exist using serial
                //var existingDevice = _client.GetDeviceByMeterNumber(model.SerialNumber);

                //if (existingDevice == null)
                //{
                List<string> validationErrorMessages = new List<string>();

                #region Validation

                #region File

                if (model.file == null)
                {
                    validationErrorMessages.Add("Please supply a picture of the meter.");
                }

                #endregion

                #region Port

                if (model.ShowPort && string.IsNullOrEmpty(model.Port))
                {
                    validationErrorMessages.Add("Port may not be empty.");
                }
                int nPort = 0;
                if (!string.IsNullOrEmpty(model.Port))
                {
                    try { nPort = Convert.ToInt32(model.Port); }
                    catch { validationErrorMessages.Add("Port is invalid."); }
                }

                #endregion

                #region Protocol

                if (model.ShowProtocol && string.IsNullOrEmpty(model.Protocol))
                {
                    validationErrorMessages.Add("Protocol may not be empty.");
                }
                int nProtocol = 0;
                if (!string.IsNullOrEmpty(model.Protocol))
                {
                    try { nProtocol = Convert.ToInt32(model.Protocol); }
                    catch { validationErrorMessages.Add("Protocol is invalid."); }
                }

                #endregion

                #region RemoteAddress

                if (model.ShowRemoteAddress && string.IsNullOrEmpty(model.RemoteAddress))
                {
                    validationErrorMessages.Add("RemoteAddress may not be empty.");
                }
                int nRemoteAddress = 0;
                if (!string.IsNullOrEmpty(model.RemoteAddress))
                {
                    try { nRemoteAddress = Convert.ToInt32(model.RemoteAddress); }
                    catch { validationErrorMessages.Add("RemoteAddress is invalid."); }
                }

                #endregion

                #region RemoteIndex

                if (model.ShowRemoteIndex && string.IsNullOrEmpty(model.RemoteIndex))
                {
                    validationErrorMessages.Add("RemoteIndex may not be empty.");
                }
                int nRemoteIndex = 0;
                if (!string.IsNullOrEmpty(model.RemoteIndex))
                {
                    try { nRemoteIndex = Convert.ToInt32(model.RemoteIndex); }
                    catch { validationErrorMessages.Add("RemoteIndex is invalid."); }
                }

                #endregion

                #region ProcessInterval

                if (model.ShowProcessInterval && string.IsNullOrEmpty(model.ProcessInterval))
                {
                    validationErrorMessages.Add("ProcessInterval may not be empty.");
                }
                int nProcessInterval = 0;
                if (!string.IsNullOrEmpty(model.ProcessInterval))
                {
                    try { nProcessInterval = Convert.ToInt32(model.ProcessInterval); }
                    catch { validationErrorMessages.Add("ProcessInterval is invalid."); }
                }

                #endregion

                #region Odo

                if (model.ShowOdo && string.IsNullOrEmpty(model.Odo))
                {
                    validationErrorMessages.Add("Odo may not be empty.");
                }
                decimal nOdo = 0;
                if (!string.IsNullOrEmpty(model.Odo))
                {
                    try { nOdo = Convert.ToDecimal(model.Odo); }
                    catch { validationErrorMessages.Add("Odo is invalid."); }
                }

                if (model.ShowOdo && string.IsNullOrEmpty(model.OdoReadingTime))
                {
                    validationErrorMessages.Add("OdoReadingTime may not be empty.");
                }
                DateTime nOdoReadingTime = DateTime.Now;
                if (!string.IsNullOrEmpty(model.OdoReadingTime))
                {
                    try { nOdoReadingTime = Convert.ToDateTime(model.OdoReadingTime); }
                    catch { validationErrorMessages.Add("OdoReadingTime is invalid."); }
                }

                #endregion

                #endregion


                if (validationErrorMessages.Count == 0)
                {
                    var user = _userManager.GetUserAsync(User).Result;


                    Log_CreatedDevice log = new Log_CreatedDevice()
                    {
                        CreateDate = DateTime.Now,
                        UserID = user.Id,
                        MeterTypeID = meterType.ID,
                        Serial = serial,
                    };

                    #region M2M

                    CreateOrUpdateM2MDevice m2MDevice = new CreateOrUpdateM2MDevice()
                    {
                        devices = new CreateOrUpdateM2MDevice.Device[]
                        {
                                new CreateOrUpdateM2MDevice.Device()
                                {
                                    serial = serial,
                                    type_id = meterType.DeviceTypeID.Value,
                                    name = model.Name,
                                    mapping = new CreateOrUpdateM2MDevice.Mapping()
                                    {
                                        port = meterType.Port.HasValue ? meterType.Port.Value : nPort,
                                        process_interval = meterType.ProcessInterval.HasValue ? meterType.ProcessInterval.Value : nProcessInterval,
                                        protocol_id = meterType.Protocol.HasValue ? meterType.Protocol.Value : nProtocol,
                                        remote_address = !string.IsNullOrEmpty(meterType.RemoteAddress) ? meterType.RemoteAddress : "0x" + model.RemoteAddress,
                                        remote_index = meterType.RemoteIndex.HasValue ? meterType.RemoteIndex.Value : nRemoteIndex
                                    }
                                }
                        }
                    };

                    log.CreateDeviceRequest = m2MDevice.ToXML<CreateOrUpdateM2MDevice, CreateOrUpdateM2MDevice>();
                    var createResult = _client.CreateDevice(GWID, m2MDevice);
                    log.CreateDeviceResponse = createResult.ToXML<CreateOrUpdateM2MDeviceResult, CreateOrUpdateM2MDeviceResult>();

                    #endregion

                    #region Mirror

                    if (model.ShowOdo)
                    {
                        MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

                        var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).FirstOrDefault();

                        if (mirrorDevice == null)
                        {
                            mirrorDevice = new MyVoltageApi.Data.Device()
                            {
                                CorrectingFactor = 1,
                                CreateDate = DateTime.Now,
                                DeviceIDLinked = 1,
                                DeviceSerialLinked = serial,
                                Name = model.Name,
                                Serial = serial
                            };
                            apiDB.Devices.Add(mirrorDevice);
                            apiDB.SaveChanges();
                        }

                        log.MirrorDeviceID = Convert.ToInt32(mirrorDevice.Id);

                        var odoReading = apiDB.OdoReadings.Where(p => p.DeviceId == mirrorDevice.Id && p.TimeLogged == nOdoReadingTime).FirstOrDefault();

                        if (odoReading == null)
                        {
                            odoReading = new OdoReading()
                            {
                                AuditName = user.Email,
                                AuditUploadName = user.Email,
                                CreateDate = DateTime.Now,
                                DeviceId = mirrorDevice.Id,
                                OdometerReading = nOdo,
                                TimeLogged = nOdoReadingTime
                            };

                            apiDB.OdoReadings.Add(odoReading);
                            apiDB.SaveChanges();
                        }
                        else
                        {
                            odoReading.AuditName = user.Email;
                            odoReading.AuditUploadName = user.Email;
                            odoReading.CreateDate = DateTime.Now;
                            odoReading.OdometerReading = nOdo;
                            odoReading.TimeLogged = nOdoReadingTime;

                            apiDB.SaveChanges();
                        }
                    }

                    #endregion

                    #region FTPUpload

                    string un = "websitedevicesphotos";
                    string pwd = "8hHqHThPz88V";

                    #region Create company folder

                    string dirUrl = $"{((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID).ToString()}/{meterType.TypeName}/{serial}";

                    #endregion


                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + Path.GetExtension(model.file.FileName);
                    log.UploadURL = $"{dirUrl}/{fileName}";
                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.file.CopyTo(uploadFile);
                    byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    FTPProvider.UploadFile(dirUrl, fileName, fileContents, un, pwd);

                    #endregion

                    model.IsSuccess = true;

                    List<string> formVariables = new List<string>();

                    foreach (PropertyInfo p in model.GetType().GetProperties())
                    {
                        if (p.PropertyType == typeof(IFormFile))
                            continue;
                        object value = p.GetValue(model, null);
                        if (value != null)
                            formVariables.Add($"{p.Name}: {value}");
                    }


                    log.SubmittedForm = formVariables.ToXML<List<string>, List<string>>();

                    db.Log_CreatedDevices.Add(log);
                    db.SaveChanges();


                    #region Background Threads

                    System.Threading.Thread threadM2MDeviceID = new System.Threading.Thread(() => AssignMeterIDToLog(log.ID, serial));
                    threadM2MDeviceID.Start();

                    if (!string.IsNullOrEmpty(meterType.Config))
                    {
                        System.Threading.Thread threadConfig = new System.Threading.Thread(() => AddMeterConfig(log.ID, serial));
                        threadConfig.Start();
                    }


                    #endregion
                }
                else
                {
                    model.ErrorMessage = string.Join(' ', validationErrorMessages.ToArray());
                }
                //}
                //else
                //{
                //    model.ErrorMessage = "Device already exists";
                //}
            }

            return View("~/Views/Meter/AddDeviceStep3.cshtml", model);
        }

        public void AssignMeterIDToLog(int logID, string serial)
        {
            int maxRetryCount = 100;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();

            int retryCount = 0;
            while (!log.M2MDeviceID.HasValue || log.M2MDeviceID.Value == 0)
            {
                retryCount++;
                // Run this in a BG thread until its found
                var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);
                if (m2mDeviceAfterAdd != null)
                {
                    log.M2MDeviceID = m2mDeviceAfterAdd.id;

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();

                }

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }
        }

        public void AddMeterConfig(int logID, string serial)
        {
            int maxRetryCount = 100;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();
            var meterType = db.MeterTypes.Where(p => p.ID == log.MeterTypeID).SingleOrDefault();
            var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);


            int retryCount = 0;
            while (m2mDeviceAfterAdd == null)
            {
                retryCount++;
                // Background thread for config, pass serial to get deviceID

                CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig = new CreateOrUpdateM2MDeviceConfig()
                {
                    action = new CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 6,
                        value = meterType.Config
                    }
                };

                log.CreateConfigRequest = createOrUpdateM2MDeviceConfig.ToXML<CreateOrUpdateM2MDeviceConfig, CreateOrUpdateM2MDeviceConfig>();
                var configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig);
                log.CreateConfigResponse = configResult.ToString();//.ToXML<object, object>();

                db.Log_CreatedDevices.Update(log);
                db.SaveChanges();

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }

        }




    }
}