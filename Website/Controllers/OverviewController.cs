using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
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
using MyVoltage.Api.MailGunApi;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.NotificationApi;
using MyVoltage.Api.Prism;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.AccountViewModels;
using MyVoltage.Models.BillingViewModels;
using MyVoltage.Models.BulkSMSResponse;
using MyVoltage.Models.MailGunReponse;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Models.OverviewViewModels;
using MyVoltage.Services;
using Newtonsoft.Json;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;

namespace MyVoltage.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OverviewController : Controller
    {
        private IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private IHttpContextAccessor _context;
        private CustomerProvider _customerProvider;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public OverviewController(IMemoryCache cache, DbContextOptions<MyVoltageDbContext> options, IHttpContextAccessor context, CustomerProvider customerProvider, IHttpContextAccessor contextAccessor, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _cache = cache;
            _options = options;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _context = context;
            _customerProvider = customerProvider;
            _contextAccessor = contextAccessor;
            _config = config;
        }

        [Route("overview")]
        public async Task<IActionResult> OverView()
        {

            OverviewDataModel overviewDataModel = new OverviewDataModel();
            overviewDataModel.gateways = _client.GetGateways();
            return View(overviewDataModel);
        }

        [Route("overview/gateway/{id}")]
        public async Task<IActionResult> Gateway(string id)
        {
            GatewayDevice gatewayDevice = new GatewayDevice();
            gatewayDevice = GetGatewayDevices(id);

            // IDeviceApi client = new DeviceFactory().CreateDeviceApi(_cache, false, options);
            // var apiClient = new SkyBillApiClient(_customerProvider.CompanyName);
            //  List<SkyBillCustomer> meters = new List<SkyBillCustomer>();
            //  if (User.IsInRole("CompanyAdmin"))
            //  {
            //     meters = apiClient.GetAllCustomerMeters();
            //  }
            //  else
            //  {
            //      meters = apiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);
            //  }

            return Content(JsonConvert.SerializeObject(gatewayDevice), "application/json");
        }

        [Route("overview/gateway/{id}/devices")]
        public async Task<IActionResult> GatewayDevices(string id)
        {
            GatewayDevice gatewayDevice = new GatewayDevice();
            gatewayDevice = GetGatewayDevices(id);
            OverviewProvider provider = new OverviewProvider(_cache, _options, null);

            using (var db = new MyVoltageDbContext(_options))
            {
                if (gatewayDevice.devices != null)
                {
                    foreach (GatewayDevice gd in gatewayDevice.devices)
                    {
                        gd.autoDisconnect = true; // Default should be auto
                        var notification = db.NotificationCustomerMeters.Where(itm => itm.MeterSerial == gd.serial).FirstOrDefault();

                        gd.gatewayID = Int32.Parse(id);

                        if (notification != null)
                            gd.autoDisconnect = notification.AutoDisconnect;
                    }
                }
                else
                {
                    gatewayDevice.devices = new GatewayDevice[0];
                }
            }

            return View(gatewayDevice);
        }

        [Route("overview/gateway/deviceDetails/{deviceId}")]
        public async Task<IActionResult> GatewayDevicesDetails(string id, string deviceId)
        {
            OverviewProvider provider = new OverviewProvider(_cache, _options, null);

            NewRegisterViewModel registerViewModel = provider.GetNewMeterRigisterView(deviceId);

            GatewayDevice gatewayDevice = new GatewayDevice();

            gatewayDevice.register = registerViewModel;

            return Content(JsonConvert.SerializeObject(gatewayDevice), "application/json");
        }

        [Route("overview/gateway/deleteMeter/{gateway}/{deviceId}")]
        public async Task<IActionResult> DeleteMeter(String gateway, String deviceId)
        {
            var result = _client.DeleteMeter(gateway, deviceId);
            return Content("{\"result\":true}", "application/json");
        }

        [Route("/overview/gateway/toggleDisconnect/{serial}/{typeID}")]
        public async Task<IActionResult> ToggleDisconnect(string serial, int typeID)
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
                        emailBody.AppendLine($"<br />");
                        emailBody.AppendLine($"<a href=\"{approvalURL}\">Reject</a><br />");

                        EmailSender emailSender = new EmailSender();
                        // Anelle, Arno, Nic
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


                    return Json(true);
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

        [Route("overview/gateway/meterSTS/{sts}/{deviceId}")]
        public async Task<IActionResult> MeterSTS(String sts, String deviceId)
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

            var m2mDevice = _client.GetDeviceByID(Convert.ToInt32(deviceId));
            DateTime? meterStatusTime = null;
            if (m2mDevice.device.status != null)
                meterStatusTime = m2mDevice.device.status.time;

            Boolean result = _client.MeterSTS(sts.Trim(), deviceId, "MyMeterSA - Manual STS", null, errorMessage, m2mDevice.device.deviceStatus, meterStatusTime);
            return Content("{\"result\":true}", "application/json");
        }

        [Route("overview/gateway/meterPrism/{type}/{serial}/{deviceId}")]
        public async Task<IActionResult> MeterPrism(String type, String serial, String deviceId)
        {
            var userID = _userManager.GetUserId(User);
            string token = "";
            if (type == "set-postpaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userID);
            }
            else if (type == "set-prepaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userID);
            }
            else
            {
                PrismApiClient prismApiClient = new PrismApiClient(_options);
                token = prismApiClient.GenerateToken(serial, type, "MyMeterSA - Manual " + type, null, "", userID);
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

                var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                DateTime? meterStatusTime = null;
                if (m2mDevice.status != null)
                    meterStatusTime = m2mDevice.status.time;

                Boolean result = _client.MeterSTS(token, deviceId, "MyMeterSA - Manual " + type, null, errorMessage, m2mDevice.deviceStatus, meterStatusTime);

                return Content("{\"result\":true}", "application/json");
            }

            return Ok();
        }

        [Route("overview/gateway/connect/{actionId}/{deviceId}/{portNum}/{typeID}/{serial}")]
        public async Task<IActionResult> ConnectMeter(int actionId, String deviceId, int portNum, int typeID, String serial)
        {
            var control = 2;
            if (typeID == 1 && portNum == 2)
            {
                // if elec and port 2 then its a hexing meter
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

            var m2mDevice = _client.GetDeviceByMeterNumber(serial);
            DateTime? meterStatusTime = null;
            if (m2mDevice.status != null)
                meterStatusTime = m2mDevice.status.time;

            // this will connect meter regardless
            Boolean result = _client.ConnectMeter(actionId, deviceId, control, "MyMeterSA - Manual Connect", null, errorMessage, m2mDevice.deviceStatus, meterStatusTime);

            if (control == 3)
            {
                // prepaid and postpaid tokens if hexing meter
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

        private GatewayDevice GetGatewayDevices(string id)
        {
            GatewayDevice gatewayDevice = new GatewayDevice();
            gatewayDevice = _client.GetGateway(id);
            gatewayDevice.devices = _client.GetGatewayDevices(id);

            return gatewayDevice;
        }

        [HttpGet]
        [Route("useractivatedelete")]
        public async Task<IActionResult> UserActivateDelete(bool rememberMe, string returnUrl = null)
        {
            return View(PopulateUserViewModel(new UserViewModel()));
        }

        [HttpPost]
        [Route("useractivatedelete")]
        public async Task<IActionResult> UserActivateDelete(UserViewModel model, string submit)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Email == model.Email && tbl.IsDeleted == false).FirstOrDefault();
                switch (submit)
                {
                    case "search":

                        if (user == null)
                        {
                            @ViewData["userInfo"] = "User not found!";
                        }
                        else
                        {
                            model.Users = new List<UserViewModel>();
                            model.Users.Add(PopulateUserViewModel(model));
                            @ViewData["userInfo"] = "User info";
                        }

                        break;
                    case "activate":
                        if (user == null)
                        {
                            @ViewData["userInfo"] = "User not found!";
                        }
                        else
                        {
                            user.IsConfirmed = true;
                            db.SaveChanges();
                            @ViewData["userInfo"] = "User activated!";
                        }
                        model.Users = new List<UserViewModel>();
                        model.Users.Add(PopulateUserViewModel(model));
                        break;
                    case "customerOrNumber":
                        var customers = db.Customers.Where(tbl => tbl.CustomerNumber == model.Email || tbl.PhoneNumber == model.Email && tbl.IsDeleted == false).ToList();
                        if (customers != null && customers.Count > 0)
                        {
                            model.Users = new List<UserViewModel>();

                            foreach (var customer in customers)
                            {
                                var customerUser = db.Users.Where(tbl => tbl.Id == customer.UserID && tbl.IsDeleted == false).FirstOrDefault();

                                UserViewModel custModel = new UserViewModel
                                {
                                    Email = customerUser.Email
                                };
                                model.Users.Add(PopulateUserViewModel(custModel, customer.UserID));
                            }
                        }

                        if (customers != null && customers.Count > 0)
                        {
                            @ViewData["userInfo"] = "User information!";
                        }
                        else
                        {
                            @ViewData["userInfo"] = "Users not found!";
                        }
                        break;
                    case "delete":
                        if (user == null)
                        {
                            @ViewData["userInfo"] = "User not found!";
                        }
                        else
                        {
                            var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                            if (customer != null)
                            {
                                customer.IsDeleted = true;
                                db.Customers.Update(customer);
                            }

                            user.IsDeleted = true;

                            db.Users.Update(user);
                            db.SaveChanges();
                            @ViewData["userInfo"] = "User deleted!";
                        }
                        break;
                }
            }
            return View(model);
        }

        private UserViewModel PopulateUserViewModel(UserViewModel model, string userId = null)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                if (model.Email != null)
                {
                    var user = db.Users.Where(tbl => tbl.Email == model.Email && tbl.IsDeleted == false).FirstOrDefault();

                    if (userId != null)
                    {
                        user = db.Users.Where(tbl => tbl.Id == userId && tbl.IsDeleted == false).FirstOrDefault();
                    }

                    if (user != null)
                    {
                        var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                        model.MeterNumber = customer.MeterNumber;
                        model.CustomerNumber = customer.CustomerNumber;
                        model.IsConfirmed = user.IsConfirmed;
                        model.FullName = customer.FullName;
                        model.Email = user.Email;
                    }
                }
            }

            return model;
        }

        [HttpGet]
        [Route("userAccounts")]
        [Route("userAccounts/{keyword}")]
        public async Task<IActionResult> UserAccounts(string keyword)
        {
            var viewModels = PopulateUserAccountView();
            var filteredModels = PopulateFilteredUserAccountView(keyword, viewModels);

            return View(filteredModels);
        }

        [Route("activateUser")]
        [Route("activateUser/{userID}")]
        public async Task<bool> ActivateUser(string userID)
        {
            string deserializedUserID = Newtonsoft.Json.JsonConvert.DeserializeObject(userID).ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == deserializedUserID && tbl.IsDeleted == false).FirstOrDefault();
                user.IsConfirmed = true;

                db.Update(user);
                var result = await db.SaveChangesAsync();

                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Route("deactivateUser")]
        [Route("deactivateUser/{userID}")]
        public async Task<bool> DeactivateUser(string userID)
        {
            string deserializedUserID = Newtonsoft.Json.JsonConvert.DeserializeObject(userID).ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == deserializedUserID && tbl.IsDeleted == false).FirstOrDefault();
                user.IsConfirmed = false;

                db.Update(user);
                var result = await db.SaveChangesAsync();

                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Route("deleteUser")]
        [Route("deleteUser/{userID}")]
        public async Task<bool> DeleteUser(string userID)
        {
            string deserializedUserID = Newtonsoft.Json.JsonConvert.DeserializeObject(userID).ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == deserializedUserID && tbl.IsDeleted == false).FirstOrDefault();
                var customer = db.Customers.Where(tbl => tbl.UserID == deserializedUserID && tbl.IsDeleted == false).FirstOrDefault();
                var notificationCustomerMeters = db.NotificationCustomerMeters.Where(tbl => tbl.CustomerID == customer.CustomerID).FirstOrDefault();

                user.IsDeleted = true;
                user.IsConfirmed = false;
                user.Email = user.Email + "_" + user.Id;
                user.UserName = user.UserName + "_" + user.Id;
                user.NormalizedEmail = user.NormalizedEmail + "_" + user.Id;
                user.NormalizedUserName = user.NormalizedUserName + "_" + user.Id;
                db.Update(user);

                customer.IsDeleted = true;
                db.Update(customer);

                if (notificationCustomerMeters != null)
                {
                    notificationCustomerMeters.CustomerID = 0;
                    db.Update(notificationCustomerMeters);
                }

                var result = await db.SaveChangesAsync();

                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [HttpGet]
        [Route("/userAccounts/EditProfile/{userID}/{customerID}")]
        public IActionResult EditProfile(string userID, int customerID)
        {
            var userViewModel = PopulateSpecificUserAccountView(userID, customerID);
            List<Company> allCompanies = AllRegisteredCompanies();
            ViewData["Companies"] = allCompanies;
            return View(userViewModel);
        }

        [HttpPost]
        [Route("/userAccounts/EditProfile/{userID}/{customerID}")]
        public async Task<IActionResult> EditProfile(UserAccountViewModel model)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == model.UserId && tbl.IsDeleted == false).FirstOrDefault();
                var customer = db.Customers.Where(tbl => tbl.CustomerID == model.CustomerId && tbl.IsDeleted == false).FirstOrDefault();

                customer.CustomerNumber = model.CustomerNumber;
                customer.CompanyID = model.CompanyId;
                customer.PhoneNumber = model.CellphoneNum;
                customer.NotificationPhoneNumber = model.CellphoneNum;
                customer.NotificationEmail = model.NotificationEmailAddress;

                db.Update(customer);
                await db.SaveChangesAsync();

                user.Email = model.NotificationEmailAddress;
                user.NormalizedEmail = model.NotificationEmailAddress.ToUpper();
                user.UserName = model.NotificationEmailAddress;
                user.NormalizedUserName = model.NotificationEmailAddress.ToUpper();
                user.PhoneNumber = model.CellphoneNum;

                db.Update(user);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("UserAccounts");
        }

        [HttpGet]
        [Route("/userAccounts/EditNotifications/{userID}")]
        public IActionResult EditNotifications(string userID)
        {
            return View(PopulateNotificationSettingsViewModel(userID, true));
        }

        [HttpPost]
        [Route("/userAccounts/EditNotifications/{userID}")]
        public async Task<IActionResult> EditNotifications(NotificationSettingsViewModel model, string userID)
        {
            if (model.DisconnectionLowBalanceNotification1 != null)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var user = db.Users.Where(tbl => tbl.Id == userID && tbl.IsDeleted == false).FirstOrDefault();
                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).FirstOrDefault();

                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).ToList();

                    if (notificationSettings == null)
                    {
                        NotificationCustomerMeter notificationCustomerMeter = new NotificationCustomerMeter()
                        {
                            AccountType = customer.AccountTypeID,
                            AutoDisconnect = true,
                            CustomerID = customer.CustomerID,
                            DisconnectNotification = true,
                            LastUpdated = DateTime.Now,
                            LowBalanceNotification1 = model.LowBalanceNotifications.Equals("1") ? true : false,
                            LowBalanceNotification2 = model.LowBalanceNotifications.Equals("1") ? true : false,
                            MeterSerial = customer.MeterNumber,
                            Reading = ""
                        };

                        db.NotificationCustomerMeters.Add(notificationCustomerMeter);
                        db.SaveChanges();
                    }
                    else
                    {
                        foreach (var setting in notificationSettings)
                        {
                            setting.AccountType = customer.AccountTypeID;
                            setting.CustomerID = customer.CustomerID;
                            setting.LastUpdated = DateTime.Now;
                            setting.LowBalanceNotification1 = model.LowBalanceNotifications.Equals("1") ? true : false;
                            setting.LowBalanceNotification2 = model.LowBalanceNotifications.Equals("1") ? true : false;

                            db.NotificationCustomerMeters.Update(setting);
                            db.SaveChanges();
                        }
                    }

                    customer.DisconnectionLowBalanceNotification1 = Convert.ToInt32(model.DisconnectionLowBalanceNotification1);
                    customer.LeakNotifications = Convert.ToBoolean(model.LeakNotifications);
                    customer.HighUsageNotifications = Convert.ToBoolean(model.HighUsageNotifications);
                    customer.NewsLetters = Convert.ToBoolean(model.NewsLetters);

                    db.Update(customer);

                    await db.SaveChangesAsync();

                    return RedirectToAction("UserAccounts");
                }
            }

            return View(PopulateNotificationSettingsViewModel(userID, true));
        }

        public DateTime startDate = DateTime.Now.AddDays(-15);

        [Route("/userAccounts/NotificationDeliveryDetails/{customerNumber?}")]
        public IActionResult NotificationDeliveryDetails(string customerNumber)
        {
            var model = new NotificationDeliveryViewModel();

            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = new Data.Customer();

                if (customerNumber != null)
                {
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == customerNumber).FirstOrDefault();
                }

                if (customer != null)
                {
                    var user = db.Users.Where(tbl => tbl.Id == customer.UserID).FirstOrDefault();
                    CustomerProvider customerProvider = new CustomerProvider(user.Id, _options, _context, _cache, _config);
                    _customerProvider = customerProvider;

                    if (customer.AccountTypeID == (int)CustomerAccountTypeEnum.POSTPAID || customer.AccountTypeID == (int)CustomerAccountTypeEnum.WALLET)
                    {
                        BillingProvider billingProvider = new BillingProvider(_cache, customerProvider);
                        List<Ledger> ledgerEntries = billingProvider.GetLedgerEntriesByCustomer();

                        List<Ledger> allFilteredLedgers = new List<Ledger>();

                        for (var i = ledgerEntries.Count - 1; i > -1; i--)
                        {
                            if (ledgerEntries[i].Posting_Date >= startDate)
                            {
                                allFilteredLedgers.Add(ledgerEntries[i]);
                            }
                        }

                        model.Ledgers = allFilteredLedgers;
                        model.TotalEntries = allFilteredLedgers.Count;
                        if (allFilteredLedgers.Count > 0)
                        {
                            model.CurrentBalance = allFilteredLedgers.Last().Balance;
                        }
                        else
                        {
                            model.CurrentBalance = 0;
                        }

                        model.Email = user.Email;
                        model.PhoneNumber = user.PhoneNumber;
                        model.DisconnectionLowBalanceNotification1 = customer.DisconnectionLowBalanceNotification1.ToString();
                    }
                    else
                    {
                        var apiClient = new SkyBillApiClient(customerProvider.CompanyName, _cache);
                        var meters = apiClient.GetMetersByCustomer(customerProvider.CustomerNumber);
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
                                if (meter.Serial_No == customer.MeterNumber)
                                {
                                    name = meter.No;
                                    color = device.type.colorType;
                                    type = device.type.type;
                                    unitType = device.type.UnitType;

                                    var today = DateTime.Now;

                                    var meterJsonModel = GetMeterUsageByDayView(customer.MeterNumber, type, today.Year, today.Month, user.Id);

                                    meterJsonModel.Data = fixDemandData(customer.MeterNumber, meterJsonModel.Data, user.Id);

                                    return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
                                }
                            }
                        }
                    }
                    return View(model);
                }
            }

            return View();
        }

        [Route("/getMailGunData")]
        public IActionResult GetMailGunData(DateTime postingDate, string email)
        {
            MailGunApi mailgunApi = new MailGunApi();
            MailGun mailGun = mailgunApi.GetMailGunLogs(startDate, email);

            foreach (var message in mailGun.items)
            {
                string[] messageParts = message.message.headers.messageId.Split('.');

                var date = messageParts[0].Substring(0, 4) + "/" + messageParts[0].Substring(4, 2) + "/" + messageParts[0].Substring(6, 2) + " " + messageParts[0].Substring(8, 2) + ":" + messageParts[0].Substring(10, 2)
                           + ":" + messageParts[0].Substring(12, 2);

                message.message.headers.SendDate = DateTime.Parse(date);
            }

            List<Item> newItems = new List<Item>();
            var count = 0;

            foreach (var mail in mailGun.items)
            {
                if (mail.message.headers.SendDate.Date == postingDate.Date)
                {
                    newItems.Add(mail);
                }

                count++;
            }


            mailGun.items = newItems;

            return PartialView("/Views/Shared/_MailGunDetails.cshtml", mailGun);
        }

        [Route("/getBulkSMSData")]
        public IActionResult GetBulkSMSData(DateTime postingDate, string phoneNumber)
        {
            BulkSMSApi bulkSmsApi = new BulkSMSApi();
            var bulkSmsList = bulkSmsApi.GetBulkSmsSendLogs(startDate);
            List<BulkSMS> filteredBulkSMS = new List<BulkSMS>();

            foreach (var bulkSms in bulkSmsList)
            {
                if (phoneNumber != null)
                {
                    var bulkSMSPhoneNum = "";
                    if (bulkSms.type == "SEND")
                    {
                        bulkSMSPhoneNum = bulkSms.to;
                    }
                    else
                    {
                        bulkSMSPhoneNum = bulkSms.from;
                    }

                    if ((bulkSMSPhoneNum.Substring(0, 2) == "27") && (bulkSMSPhoneNum.Length > 10))
                    {
                        if (phoneNumber.Substring(1, phoneNumber.Length - 1) == bulkSMSPhoneNum.Substring(2, bulkSMSPhoneNum.Length - 2))
                        {
                            if (bulkSms.submission.date.Date == postingDate.Date)
                            {
                                filteredBulkSMS.Add(bulkSms);
                            }
                        }
                    }
                    else
                    {
                        if (phoneNumber == bulkSMSPhoneNum)
                        {
                            if (bulkSms.submission.date.Date == postingDate.Date)
                            {
                                filteredBulkSMS.Add(bulkSms);
                            }
                        }
                    }
                }
            }

            return PartialView("/Views/Shared/_BulkSMSDetails.cshtml", filteredBulkSMS);
        }

        [Route("billingUsage")]
        [Route("billingUsage/{keyword}")]
        public IActionResult BillingUsage(string keyword)
        {
            var viewModels = PopulateUserAccountView();
            var filteredModels = PopulateFilteredUserAccountView(keyword, viewModels);

            return View(filteredModels);
        }

        [Route("billingUsage/CustomerLedgerEntries/{companyName}/{customerNumber}")]
        public IActionResult CustomerLedgerEntries(string companyName, string customerNumber)
        {
            SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);
            List<Ledger> ledgerEntries = client.GetLedgerEntriesByCustomer(customerNumber);
            ViewData["CompanyName"] = companyName;
            ViewData["CustomerNumber"] = customerNumber;

            if (ledgerEntries.Count == 0)
            {
                ViewBag.ErrorMessage = "Valid company name or customer number could not be found on Skybill.";
                return View();
            }
            else
            {
                return View(ledgerEntries);
            }
        }

        [Route("billingUsage/SalesInvoiceLines/{companyName}/{customerNumber}")]
        public IActionResult SalesInvoiceLines(string companyName, string customerNumber)
        {
            SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);
            List<LedgerLineItem> ledgerLineItems = client.GetSalesInvoiceLinesByCustomer(customerNumber);

            ViewData["CompanyName"] = companyName;
            ViewData["CustomerNumber"] = customerNumber;

            if (ledgerLineItems.Count == 0)
            {
                ViewBag.ErrorMessage = "Valid company name or customer number could not be found on Skybill.";
                return View();
            }
            else
            {
                return View(ledgerLineItems);
            }

        }

        [Route("billingUsage/CustomerMeters/{companyName}")]
        [Route("billingUsage/CustomerMeters/{companyName}/{keyword?}")]
        public IActionResult CustomerMeters(string companyName, string keyword)
        {
            SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);
            List<SkyBillCustomer> customers = client.GetMetersByCompany();
            List<SkyBillCustomer> filteredCustomers = new List<SkyBillCustomer>();

            if (keyword != null)
            {
                foreach (var c in customers)
                {
                    if (c.Serial_No != null)
                    {
                        if (c.Serial_No.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredCustomers.Add(c);
                            continue;
                        }
                    }

                    if (c.Customer_No != null)
                    {
                        if (c.Customer_No.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredCustomers.Add(c);
                            continue;
                        }
                    }

                    if (c.Customer_Name != null)
                    {
                        if (c.Customer_Name.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredCustomers.Add(c);
                            continue;
                        }
                    }
                }
            }
            else
            {
                filteredCustomers = customers;
            }

            ViewData["Keyword"] = keyword;

            return View(filteredCustomers);
        }

        [Route("billingUsage/PaymentInfo/{customerNumber}")]
        public IActionResult PaymentInfo(string customerNumber)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var paymentsInfo = (from c in db.Customers
                                    join p in db.Payments on c.UserID equals p.UserID
                                    join ps in db.PaymentStatuses on p.PaymentStatusID equals ps.PaymentStatusID
                                    where c.CustomerNumber == customerNumber
                                    where p.CreateDate >= DateTime.Now.AddDays(-30)
                                    select new PaymentInfoViewModel
                                    {
                                        Amount = p.Amount,
                                        CreateDate = p.CreateDate,
                                        Reference = p.Reference,
                                        Status = ps.Name
                                    }).ToList();

                var rawData = db.PaymentRawData.ToList();

                foreach (var p in paymentsInfo)
                {
                    p.RawDataStatus = "Nothing received from Skybill. Please contact Skybill to clear any blocked transactions";
                    bool flag = false;
                    foreach (var item in rawData)
                    {
                        if (item.RawData != null)
                        {
                            var rawDataObject = JsonConvert.DeserializeObject<RawData>(item.RawData);

                            if (p.Reference == rawDataObject.Reference)
                            {
                                p.RawDataStatus = "Response received from Skybill";
                                flag = true;
                                break;
                            }
                        }

                    }

                    if (flag) continue;
                }

                ViewData["CustomerNumber"] = customerNumber;
                return View(paymentsInfo);

            }
        }

        [Route("billingUsage/CustomerUsage/{customerNumber}/{meterNumber}")]
        public IActionResult CustomerUsage(string customerNumber, string meterNumber)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = new Data.Customer();

                if (customerNumber != null)
                {
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == customerNumber).FirstOrDefault();
                }

                if (customer != null)
                {
                    var user = db.Users.Where(tbl => tbl.Id == customer.UserID).FirstOrDefault();
                    CustomerProvider customerProvider = new CustomerProvider(user.Id, _options, _context, _cache, _config);
                    _customerProvider = customerProvider;

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
                    var year = today.Year.ToString();
                    var month = today.Month.ToString();

                    BillingProvider billingProvider = new BillingProvider(_cache, _customerProvider);

                    List<Decimal> dailyTotals = billingProvider.GetDailyInvoiceAmountByMeter(meterNumber, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), _customerProvider.AccountType, _customerProvider.ShowCostInclVAT);

                    int multiplier = -1;

                    if (_customerProvider.AccountType == (int)AccountTypeEnum.PostPaid)
                        multiplier = 1;

                    Decimal monthlyTotal = dailyTotals.Sum() * multiplier;

                    var model = new MeterViewModel
                    {
                        MeterNumber = meterNumber,
                        Name = name,
                        ReadingDate = today,
                        MeterType = type,
                        MeterColor = color,
                        UnitType = unitType,
                        AllMeters = meters,
                        MonthlyTotal = monthlyTotal
                    };

                    return View(model);
                }
            }

            return View();
        }

        [HttpGet]
        [Route("bulkReadings")]
        public IActionResult BulkReadings()
        {
            return Redirect("/");
            BulkReadingsModel bulkReadingsModel = new BulkReadingsModel()
            {
                StartDate = DateTime.Now.AddDays(-1).Date,
                EndDate = DateTime.Now.Date,
                Interval = 3600,
                Registers = new List<Models.MeterViewModels.BulkReadingsRegister>()
                {
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 1, Name = "Active Energy", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 2, Name = "Reactive Energy", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 29, Name = "Max Demand", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 70, Name = "CT Ratio", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 80, Name = "Water Consumption", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 140, Name = "Gas Consumption", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 91, Name = "Contactor State", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 100, Name = "Internal Battery V", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 101, Name = "Signal RSSI", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 106, Name = "SNR", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 102, Name = "Temp", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 90, Name = "Remaining Credit", Selected = false },
                },
            };
            return View(bulkReadingsModel);
        }
        [HttpPost]
        [Route("bulkReadings")]
        public IActionResult BulkReadings(BulkReadingsModel bulkReadingsModel)
        {
            return Redirect("/");
            BulkReadingsModel bulkReadingsModel1 = bulkReadingsModel;
            DateTime startDate = new DateTime(bulkReadingsModel.StartDate.Year, bulkReadingsModel.StartDate.Month, bulkReadingsModel.StartDate.Day, bulkReadingsModel.StartTime.Hour, bulkReadingsModel.StartTime.Minute, bulkReadingsModel.StartTime.Second);
            DateTime endDate = new DateTime(bulkReadingsModel.EndDate.Year, bulkReadingsModel.EndDate.Month, bulkReadingsModel.EndDate.Day, bulkReadingsModel.EndTime.Hour, bulkReadingsModel.EndTime.Minute, bulkReadingsModel.EndTime.Second);

            for (int i = 0; i < bulkReadingsModel.Registers.Count; i++)
            {
                bulkReadingsModel1.Registers[i].Name = DefaultRegisters().SingleOrDefault(p => p.ID == bulkReadingsModel1.Registers[i].ID).Name;
            }
            if (!string.IsNullOrEmpty(bulkReadingsModel.SerialNos))
            {
                Dictionary<int, string> registers = new Dictionary<int, string>();
                foreach (var selectedRegister in bulkReadingsModel.Registers)
                {
                    if (selectedRegister.Selected)
                        registers.Add(selectedRegister.ID, bulkReadingsModel.SelectedType);
                }

                if (registers.Count == 0)
                {
                    bulkReadingsModel1.ErrorMessage = "No registers selected";
                    return View(bulkReadingsModel1);
                }

                Dictionary<string, string> meterRegisters = new Dictionary<string, string>();

                var meterSerials = bulkReadingsModel.SerialNos.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                #region Get results from m2m
                /// Get the results from m2m
                foreach (var serial in meterSerials)
                {
                    if (string.IsNullOrEmpty(serial) || meterRegisters.ContainsKey(serial))
                        continue;

                    Data.Device localDevice = null;
                    using (var db = new MyVoltageDbContext(_options))
                    {
                        localDevice = db.Devices.Where(p => p.Serial == serial).SingleOrDefault();
                        if (localDevice == null)
                            continue;
                    }

                    string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval={bulkReadingsModel.Interval}";
                    foreach (var register in DefaultRegisters())
                    {
                        url = url + $"&registers[{register.ID}]={bulkReadingsModel.SelectedType}";
                    }
                    var result = _client.GetString(url, 1);
                    meterRegisters.Add(serial, result);
                }

                #endregion

                if (meterRegisters.Count == 0)
                {
                    bulkReadingsModel1.ErrorMessage = "No results found";
                    return View(bulkReadingsModel1);
                }

                // Rebuild the results into pivot tables
                #region Declaration and Columns

                System.Data.DataSet dataSet = new System.Data.DataSet("Workbook");

                foreach (var register in registers)
                {
                    string registerName = DefaultRegisters().SingleOrDefault(p => p.ID == register.Key).Name;
                    System.Data.DataTable thisRegisterTable = new System.Data.DataTable(registerName);
                    thisRegisterTable.Columns.Add("MeterSerial");
                    string[] lines = meterRegisters.FirstOrDefault().Value.Split(new[] { "\n" }, StringSplitOptions.None);
                    bool isfirst = true;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;
                        if (isfirst)
                        {
                            // Serial,"Time Logged","Reactive Energy","Active Energy"
                            isfirst = false;
                            continue;
                        }
                        // Add the time logged column
                        thisRegisterTable.Columns.Add(line.Split(new[] { "," }, StringSplitOptions.None)[1], typeof(System.Decimal));
                    }
                    dataSet.Tables.Add(thisRegisterTable);
                }

                #endregion

                #region Build tables' rows

                foreach (var register in registers)
                {
                    string registerName = DefaultRegisters().SingleOrDefault(p => p.ID == register.Key).Name;
                    bool isFirst = true;
                    int thisRegisterIndex = 0;
                    foreach (var meterRegister in meterRegisters)
                    {
                        bool didFindThisRegister = false;
                        string serial = meterRegister.Key;
                        string[] fileLines = meterRegister.Value.Split(new[] { "\n" }, StringSplitOptions.None);
                        foreach (var columnName in fileLines[0].Split(new[] { "," }, StringSplitOptions.None))
                        {
                            if (columnName.Contains(registerName))
                            {
                                didFindThisRegister = true;
                                break;
                            }
                            thisRegisterIndex++;
                        }
                        //isFirst = false;
                        //continue;

                        if (!didFindThisRegister)
                        {
                            thisRegisterIndex = 0;
                            continue;
                        }
                        System.Data.DataRow dataRow = dataSet.Tables[registerName].NewRow();
                        dataRow["MeterSerial"] = serial;
                        isFirst = true;
                        foreach (var line in fileLines)
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                                continue;
                            }
                            if (string.IsNullOrEmpty(line))
                                continue;
                            var lineItems = line.Split(new[] { "," }, StringSplitOptions.None);
                            try
                            {
                                if (!string.IsNullOrEmpty(lineItems[thisRegisterIndex].ToString()))
                                    dataRow[lineItems[1]] = lineItems[thisRegisterIndex];
                            }
                            catch
                            {
                                throw new Exception(lineItems[thisRegisterIndex].ToString());
                            }
                        }
                        bool doesRowHaveValues = false;
                        foreach (object cell in dataRow.ItemArray)
                        {
                            if (cell.ToString() == serial)
                                continue;

                            if (!string.IsNullOrEmpty(cell.ToString()))
                                doesRowHaveValues = true;
                        }
                        if (doesRowHaveValues)
                            dataSet.Tables[registerName].Rows.Add(dataRow);
                        isFirst = true;
                        thisRegisterIndex = 0;
                    }
                }




                #endregion

                #region Build the Excel

                Stream stream = new MemoryStream();

                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    foreach (DataTable registerTable in dataSet.Tables)
                    {
                        if (registerTable.Rows.Count == 0)
                            continue;
                        var thisRegisterWorksheet = workbook.Worksheets.Add(registerTable.TableName);

                        thisRegisterWorksheet.Cell(1, 1).InsertTable(registerTable);

                    }
                    if (workbook.Worksheets.Count == 0)
                    {
                        bulkReadingsModel1.ErrorMessage = "No results found for selected search criteria";
                        return View(bulkReadingsModel1);
                    }

                    workbook.SaveAs(stream);

                }

                stream.Seek(0, SeekOrigin.Begin);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                #endregion

            }
            else
            {
                bulkReadingsModel1.ErrorMessage = "No serial numbers supplied";
            }

            return View(bulkReadingsModel1);
        }

        [HttpGet]
        [Route("KronikaMeterBilling")]
        public IActionResult KronikaMeterBilling()
        {
            KronikaMeterBillingModel kronikaMeterBillingModel = new KronikaMeterBillingModel()
            {
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
            };
            return View(kronikaMeterBillingModel);
        }

        [HttpPost]
        [Route("KronikaMeterBilling")]
        public IActionResult KronikaMeterBilling(KronikaMeterBillingModel kronikaMeterBillingModel)
        {
            KronikaMeterBillingModel kronikaMeterBillingModel1 = kronikaMeterBillingModel;

            var meterSerials = kronikaMeterBillingModel.MeterSerials.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            MyVoltage.Api.Kronika.KronikaAPIClient _client = new MyVoltage.Api.Kronika.KronikaAPIClient();

            #region Get the billing results

            Dictionary<string, MyVoltage.Api.Kronika.Billing_Report[]> billing_Report_Results = new Dictionary<string, MyVoltage.Api.Kronika.Billing_Report[]>();
            foreach (var meterSerial in meterSerials)
            {
                var billing_result = _client.GetMeterBilling(meterSerial, kronikaMeterBillingModel.StartDate, kronikaMeterBillingModel.EndDate);

                if (billing_result != null && billing_result.billing_report.Count() > 0)
                    billing_Report_Results.Add(meterSerial, billing_result.billing_report);
            }

            #endregion

            if (billing_Report_Results.Count == 0)
            {
                kronikaMeterBillingModel1.ErrorMessage = "No results found for selected search criteria";
                return View(kronikaMeterBillingModel1);
            }

            #region Build the data

            DataSet dataSet = new DataSet();

            foreach (var reportItem in billing_Report_Results)
            {
                DataTable dataTable = new DataTable(reportItem.Key);

                dataTable.Columns.Add("date", typeof(DateTime));
                dataTable.Columns.Add("meter_serial");
                dataTable.Columns.Add("property");
                dataTable.Columns.Add("unit");
                dataTable.Columns.Add("utility_type");
                dataTable.Columns.Add("supply_rate", typeof(int));
                dataTable.Columns.Add("reading", typeof(decimal));
                dataTable.Columns.Add("margin", typeof(decimal));
                dataTable.Columns.Add("profit", typeof(decimal));
                dataTable.Columns.Add("cost", typeof(decimal));
                dataTable.Columns.Add("billing", typeof(decimal));
                dataTable.Columns.Add("billing_consumption", typeof(decimal));
                dataTable.Columns.Add("consumption", typeof(decimal));
                dataTable.Columns.Add("unbilled_consumption", typeof(decimal));

                foreach (var billingItem in reportItem.Value)
                {
                    DataRow dataRow = dataTable.NewRow();

                    dataRow["date"] = billingItem.date;
                    dataRow["meter_serial"] = billingItem.meter_serial;
                    dataRow["property"] = billingItem.property;
                    dataRow["unit"] = billingItem.unit;
                    dataRow["utility_type"] = billingItem.utility_type;
                    dataRow["supply_rate"] = billingItem.supply_rate;
                    dataRow["reading"] = billingItem.reading;
                    dataRow["margin"] = billingItem.margin;
                    dataRow["profit"] = billingItem.profit;
                    dataRow["cost"] = billingItem.cost;
                    dataRow["billing"] = billingItem.billing;
                    dataRow["billing_consumption"] = billingItem.billing_consumption;
                    dataRow["consumption"] = billingItem.consumption;
                    dataRow["unbilled_consumption"] = billingItem.unbilled_consumption;

                    dataTable.Rows.Add(dataRow);
                    dataTable.AcceptChanges();
                }

                dataSet.Tables.Add(dataTable);
            }

            #endregion


            #region Build the Excel




            Stream stream = new MemoryStream();

            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                foreach (DataTable billingTable in dataSet.Tables)
                {
                    if (billingTable.Rows.Count == 0)
                        continue;
                    var thisRegisterWorksheet = workbook.Worksheets.Add(billingTable.TableName);

                    thisRegisterWorksheet.Cell(1, 1).InsertTable(billingTable);

                }
                if (workbook.Worksheets.Count == 0)
                {
                    kronikaMeterBillingModel1.ErrorMessage = "No results found for selected search criteria";
                    return View(kronikaMeterBillingModel1);
                }

                workbook.SaveAs(stream);

            }

            stream.Seek(0, SeekOrigin.Begin);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");



            #endregion
        }
        [HttpGet]
        [Route("KronikaMeterBillingSummary")]
        public IActionResult KronikaMeterBillingSummary()
        {
            KronikaMeterBillingSummaryModel kronikaMeterBillingModel = new KronikaMeterBillingSummaryModel()
            {
                StartDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                EndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
            };
            return View(kronikaMeterBillingModel);
        }

        [HttpPost]
        [Route("KronikaMeterBillingSummary")]
        public IActionResult KronikaMeterBillingSummary(KronikaMeterBillingSummaryModel kronikaMeterBillingModel)
        {
            KronikaMeterBillingSummaryModel kronikaMeterBillingModel1 = kronikaMeterBillingModel;

            var meterSerials = kronikaMeterBillingModel.MeterSerials.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            MyVoltage.Api.Kronika.KronikaAPIClient _client = new MyVoltage.Api.Kronika.KronikaAPIClient();

            #region Get the billing results

            Dictionary<string, MyVoltage.Api.Kronika.Billing_Summary_Report[]> billing_Report_Results = new Dictionary<string, MyVoltage.Api.Kronika.Billing_Summary_Report[]>();
            foreach (var meterSerial in meterSerials)
            {
                var billing_result = _client.GetMeterBillingSummary(meterSerial, kronikaMeterBillingModel.StartDate, kronikaMeterBillingModel.EndDate);

                if (billing_result != null && billing_result.billing_summary_report.Count() > 0)
                    billing_Report_Results.Add(meterSerial, billing_result.billing_summary_report);
            }

            #endregion

            if (billing_Report_Results.Count == 0)
            {
                kronikaMeterBillingModel1.ErrorMessage = "No results found for selected search criteria";
                return View(kronikaMeterBillingModel1);
            }

            #region Build the data

            DataSet dataSet = new DataSet();

            foreach (var reportItem in billing_Report_Results)
            {
                DataTable dataTable = new DataTable(reportItem.Key);

                dataTable.Columns.Add("year", typeof(string));
                dataTable.Columns.Add("month", typeof(string));
                dataTable.Columns.Add("meter_serial", typeof(string));
                dataTable.Columns.Add("property", typeof(string));
                dataTable.Columns.Add("unit", typeof(string));
                dataTable.Columns.Add("utility_type", typeof(string));
                dataTable.Columns.Add("supply_rate", typeof(decimal));
                dataTable.Columns.Add("reading", typeof(decimal));
                dataTable.Columns.Add("margin", typeof(decimal));
                dataTable.Columns.Add("profit", typeof(decimal));
                dataTable.Columns.Add("cost", typeof(decimal));
                dataTable.Columns.Add("billing", typeof(decimal));
                dataTable.Columns.Add("billing_consumption", typeof(decimal));
                dataTable.Columns.Add("consumption", typeof(decimal));
                dataTable.Columns.Add("unbilled_consumption", typeof(decimal));

                foreach (var billingItem in reportItem.Value)
                {
                    DataRow dataRow = dataTable.NewRow();

                    dataRow["year"] = billingItem.year;
                    dataRow["month"] = billingItem.month;
                    dataRow["meter_serial"] = billingItem.meter_serial;
                    dataRow["property"] = billingItem.property;
                    dataRow["unit"] = billingItem.unit;
                    dataRow["utility_type"] = billingItem.utility_type;
                    dataRow["supply_rate"] = billingItem.supply_rate;
                    dataRow["reading"] = billingItem.reading;
                    dataRow["margin"] = billingItem.margin;
                    dataRow["profit"] = billingItem.profit;
                    dataRow["cost"] = billingItem.cost;
                    dataRow["billing"] = billingItem.billing;
                    dataRow["billing_consumption"] = billingItem.billing_consumption;
                    dataRow["consumption"] = billingItem.consumption;
                    dataRow["unbilled_consumption"] = billingItem.unbilled_consumption;

                    dataTable.Rows.Add(dataRow);
                    dataTable.AcceptChanges();
                }

                dataSet.Tables.Add(dataTable);
            }

            #endregion


            #region Build the Excel




            Stream stream = new MemoryStream();

            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                foreach (DataTable billingTable in dataSet.Tables)
                {
                    if (billingTable.Rows.Count == 0)
                        continue;
                    var thisRegisterWorksheet = workbook.Worksheets.Add(billingTable.TableName);

                    thisRegisterWorksheet.Cell(1, 1).InsertTable(billingTable);

                }
                if (workbook.Worksheets.Count == 0)
                {
                    kronikaMeterBillingModel1.ErrorMessage = "No results found for selected search criteria";
                    return View(kronikaMeterBillingModel1);
                }

                workbook.SaveAs(stream);

            }

            stream.Seek(0, SeekOrigin.Begin);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");



            #endregion
        }

        [HttpGet]
        [Route("BulkDeviceUpdate")]
        public IActionResult BulkDeviceUpdate()
        {
            return Redirect("/");
            return View(new BulkDeviceUpdateModel());
        }
        [HttpPost]
        [Route("BulkDeviceUpdate")]
        public IActionResult BulkDeviceUpdate(BulkDeviceUpdateModel bulkDeviceUpdateModel)
        {
            return Redirect("/");
            BulkDeviceUpdateModel bulkDeviceUpdateModel1 = bulkDeviceUpdateModel;


            Dictionary<string, string> results = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(bulkDeviceUpdateModel.DeviceIDs))
            {
                var deviceIDs = bulkDeviceUpdateModel.DeviceIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (deviceIDs.Length == 0)
                {
                    bulkDeviceUpdateModel1.ErrorMessage = "No Device IDs supplied";
                    return View(bulkDeviceUpdateModel1);
                }


                #region Get results from m2m
                /// Get the results from m2m
                foreach (var deviceID in deviceIDs)
                {
                    if (string.IsNullOrEmpty(deviceID))
                        continue;

                    MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                    DeviceUpdate deviceUpdate = new DeviceUpdate()
                    {
                        name = bulkDeviceUpdateModel.NewDeviceName
                    };

                    string url = $"devices/{deviceID}";

                    var result = _client.PUT<DeviceUpdateResult, object>(url, 1, deviceUpdate);

                    results.Add(deviceID, $"{result.result} - Device {result.device.id} has been updated to {result.device.name}");
                }

                #endregion

                // Rebuild the results into pivot tables
            }
            else
            {
                bulkDeviceUpdateModel1.ErrorMessage = "No Device IDs supplied";
            }

            foreach (var result in results)
            {
                bulkDeviceUpdateModel1.ErrorMessage = bulkDeviceUpdateModel1.ErrorMessage + Environment.NewLine + result.Value;
            }

            return View(bulkDeviceUpdateModel1);
        }
        [HttpGet]
        [Route("Bulk433TestingUpload")]
        public IActionResult Bulk433TestingUpload()
        {
            return Redirect("/");
            return View(new Bulk433TestingUploadModel());
        }
        [HttpPost]
        [Route("Bulk433TestingUpload")]
        public IActionResult Bulk433TestingUpload(Bulk433TestingUploadModel bulk433TestingUploadModel)
        {
            return Redirect("/");
            Bulk433TestingUploadModel bulk433TestingUploadModel1 = bulk433TestingUploadModel;


            bulk433TestingUploadModel1.ErrorMessage = "Your meters has been submitted. Please do not resubmit. The meters will be uploaded and may take up to 30min to complete.";

            var serialNumbers = bulk433TestingUploadModel.SerialNos.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            // Run entire thing in background proceess and return immedietly 
            System.Threading.Thread thread = new System.Threading.Thread(() => Bulk433TestingUploadThread(serialNumbers.ToList(), bulk433TestingUploadModel.DoSecondInput));
            thread.Start();
            return View(bulk433TestingUploadModel1);
        }
        private void Bulk433TestingUploadThread(List<string> serialNumbers, bool doSecondPulseInput)
        {
            List<string> results = new List<string>();
            string gatewayID = "629";

            if (serialNumbers.Count == 0)
            {
                Console.WriteLine("No serials");
                return;
            }


            #region Add devices to m2m
            /// Get the results from m2m
            foreach (var serial in serialNumbers)
            {
                if (string.IsNullOrEmpty(serial))
                    continue;

                MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                {
                    devices = new CreateOrUpdateM2MDevice.Device[]
                    {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial + "-1",
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 1,
                                    process_interval = 900
                                }
                            }
                    }
                };

                string url = $"gateways/{gatewayID}/devices";

                var result = _client.Post<object, object>(url, 1, deviceUpdate);

                string strResult = $"Device {serial + "-1"} has been added to test gateway {gatewayID} - {(result != null ? result.ToString() : "")}";

                Console.WriteLine(strResult);

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDevice deviceUpdate2 = new CreateOrUpdateM2MDevice()
                    {
                        devices = new CreateOrUpdateM2MDevice.Device[]
                        {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial + "-2",
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 2,
                                    process_interval = 900
                                }
                            }
                        }
                    };

                    url = $"gateways/{gatewayID}/devices";

                    result = _client.Post<object, object>(url, 1, deviceUpdate2);

                    strResult = $"Device {serial + "-2"} has been added to test gateway {gatewayID} - {(result != null ? result.ToString() : "")}";

                    Console.WriteLine(strResult);

                    results.Add(strResult);


                }

            }

            #endregion

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
            System.Threading.Thread.Sleep(30000);

            #region double check all devices has been added

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Double checking add");

            var gatewaydevices = _client.GetGatewayDevices(gatewayID);
            bool lookAgain = false;

            foreach (var serial in serialNumbers)
            {
                if (gatewaydevices.Where(p => p.serial == serial + "-1").Count() == 0)
                {
                    // Add it again
                    lookAgain = true;
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial + "-1"} Adding it again");
                    bool addAgain = true;

                    while (addAgain)
                    {
                        CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                        {
                            devices = new CreateOrUpdateM2MDevice.Device[]
                            {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial + "-1",
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 1,
                                    process_interval = 900
                                }
                            }
                            }
                        };
                        string url = $"gateways/{gatewayID}/devices";

                        var result = _client.Post<object, object>(url, 1, deviceUpdate);

                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
                        System.Threading.Thread.Sleep(30000);

                        gatewaydevices = _client.GetGatewayDevices(gatewayID);
                        if (gatewaydevices.Where(p => p.serial == serial + "-1").Count() > 0)
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial + "-1"} Adding it again");
                            addAgain = false;
                        }
                    }


                }
                if (doSecondPulseInput)
                    if (gatewaydevices.Where(p => p.serial == serial + "-2").Count() == 0)
                    {
                        // Add it again
                        lookAgain = true;
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial + "-2"} Adding it again");
                        bool addAgain = true;

                        while (addAgain)
                        {
                            CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                            {
                                devices = new CreateOrUpdateM2MDevice.Device[]
                                {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial + "-2",
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 2,
                                    process_interval = 900
                                }
                            }
                                }
                            };
                            string url = $"gateways/{gatewayID}/devices";

                            var result = _client.Post<object, object>(url, 1, deviceUpdate);

                            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
                            System.Threading.Thread.Sleep(30000);

                            gatewaydevices = _client.GetGatewayDevices(gatewayID);
                            if (gatewaydevices.Where(p => p.serial == serial + "-2").Count() > 0)
                            {
                                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial + "-2"} Adding it again");
                                addAgain = false;
                            }
                        }


                    }
            }

            while (lookAgain)
            {
                gatewaydevices = _client.GetGatewayDevices(gatewayID);
                lookAgain = false;

                foreach (var serial in serialNumbers)
                {
                    if (gatewaydevices.Where(p => p.serial == serial + "-1").Count() == 0)
                    {
                        lookAgain = true;
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial + "-1"}");
                    }
                }
            }

            #endregion

            #region Add devices configuration to m2m

            Console.WriteLine("Config starting");
            /// Get the results from m2m
            /// 
            #region -1

            foreach (var serial in serialNumbers)
            {

                var device = gatewaydevices.Where(p => p.serial == serial + "-1").SingleOrDefault();

                if (device == null)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cannot find {serial + "-1"} to config");
                    continue;
                }
                MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDeviceConfig bulk433TestingUploadConfig = new CreateOrUpdateM2MDeviceConfig()
                {
                    action = new CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = "1"
                    }
                };

                string url = $"devices/{device.id}/configurations/6";

                var result = _client.Post<object, object>(url, 1, bulk433TestingUploadConfig);

                string strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDeviceConfig bulk433TestingUploadConfig2 = new CreateOrUpdateM2MDeviceConfig()
                    {
                        action = new CreateOrUpdateM2MDeviceConfig.Action()
                        {
                            id = 1,
                            value = "1"
                        }
                    };

                    url = $"devices/{device.id}/configurations/6";

                    result = _client.Post<object, object>(url, 1, bulk433TestingUploadConfig2);

                    strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                    results.Add(strResult);

                }
            }

            #endregion

            #region -2

            foreach (var serial in serialNumbers)
            {

                var device = gatewaydevices.Where(p => p.serial == serial + "-2").SingleOrDefault();

                if (device == null)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Cannot find {serial + "-2"} to config");
                    continue;
                }
                MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDeviceConfig bulk433TestingUploadConfig = new CreateOrUpdateM2MDeviceConfig()
                {
                    action = new CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = "1"
                    }
                };

                string url = $"devices/{device.id}/configurations/6";

                var result = _client.Post<object, object>(url, 1, bulk433TestingUploadConfig);

                string strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDeviceConfig bulk433TestingUploadConfig2 = new CreateOrUpdateM2MDeviceConfig()
                    {
                        action = new CreateOrUpdateM2MDeviceConfig.Action()
                        {
                            id = 1,
                            value = "1"
                        }
                    };

                    url = $"devices/{device.id}/configurations/6";

                    result = _client.Post<object, object>(url, 1, bulk433TestingUploadConfig2);

                    strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                    results.Add(strResult);

                }
            }

            #endregion

            #endregion

            // Rebuild the results into pivot tables

        }

        [HttpPost]
        [Route("DeleteAllDevicesOn515")]
        public async Task<bool> DeleteAllDevicesOn515()
        {
            //System.Threading.Thread thread = new System.Threading.Thread(() => DeleteAllDevicesOn515Thread());
            //thread.Start();
            return true;
        }

        private void DeleteAllDevicesOn515Thread()
        {
            string gatewayID = "629";
            var gatewaydevices = _client.GetGatewayDevices(gatewayID);

            while (gatewaydevices.Count() > 0)
            {
                foreach (var device in gatewaydevices)
                {
                    Console.WriteLine($"DELETING {device.id}");
                    _client.DeleteMeter(gatewayID, device.id.ToString());
                }
                gatewaydevices = _client.GetGatewayDevices(gatewayID);
            }

        }

        [HttpGet]
        [Route("RentalFeesUpload")]
        public IActionResult RentalFeesUpload()
        {
            return View(new RentalFeesUploadModel());
        }
        [HttpPost]
        [Route("RentalFeesUpload")]
        public IActionResult RentalFeesUpload(RentalFeesUploadModel rentalFeesUploadModel)
        {
            RentalFeesUploadModel rentalFeesUploadModel1 = rentalFeesUploadModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            var csvLines = rentalFeesUploadModel.CSVString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            bool isFirstRow = true;

            StringBuilder result = new StringBuilder();

            using (var db = new MyVoltageDbContext(_options))
            {
                var devicesToUseForImportChecking = db.Devices.ToList();
                var gatewaysToUseForImportChecking = db.Gateways.ToList();
                foreach (string fileLine in csvLines)
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        continue;
                    }
                    if (string.IsNullOrEmpty(fileLine))
                        continue;
                    try
                    {
                        string propertyLinked = fileLine.Split(',')[0];
                        int deviceIDLinked = Convert.ToInt32(fileLine.Split(',')[1]);
                        DateTime rentalMonth = DateTime.Parse(fileLine.Split(',')[2], dateTimeFormatInfo);
                        decimal standardFee = !string.IsNullOrEmpty(fileLine.Split(',')[3]) ? Convert.ToDecimal(fileLine.Split(',')[3]) : 0;
                        decimal agreedFee = !string.IsNullOrEmpty(fileLine.Split(',')[4]) ? Convert.ToDecimal(fileLine.Split(',')[4]) : 0;

                        #region Devices

                        if (rentalFeesUploadModel.SelectedRentalUploadType == 1)
                        {
                            var device = devicesToUseForImportChecking.Where(p => p.DeviceIDLinked == deviceIDLinked).SingleOrDefault();

                            if (device != null)
                            {
                                var deviceRental = (from p in db.DeviceRentalFees
                                                    where p.DeviceIDLinked == deviceIDLinked
                                                    && p.RentalMonth.Date == rentalMonth.Date
                                                    select p).SingleOrDefault();

                                if (deviceRental == null)
                                {
                                    deviceRental = new DeviceRentalFee()
                                    {
                                        AgreedFee = agreedFee,
                                        DeviceIDLinked = deviceIDLinked,
                                        RentalMonth = rentalMonth,
                                        StandardFee = standardFee
                                    };
                                    db.DeviceRentalFees.Add(deviceRental);
                                    db.SaveChanges();
                                    result.AppendLine($"<span style=\"color:Green;\">{deviceIDLinked} - {device.Name} - Created</span>;<br />");
                                }
                                else
                                {
                                    deviceRental.AgreedFee = agreedFee;
                                    deviceRental.StandardFee = standardFee;
                                    db.SaveChanges();
                                    result.AppendLine($"<span style=\"color:Orange;\">{deviceIDLinked} - {device.Name} - Updated</span>;<br />");
                                }
                            }
                            else
                            {
                                result.AppendLine($"<span style=\"color:red;\">Could not find deviceIdLinked {deviceIDLinked}</span>;<br />");
                            }
                        }
                        #endregion
                        #region Gateways
                        else if (rentalFeesUploadModel.SelectedRentalUploadType == 2)
                        {
                            var gateway = gatewaysToUseForImportChecking.Where(p => p.GatewayID == deviceIDLinked).SingleOrDefault();

                            if (gateway != null)
                            {
                                var deviceRental = (from p in db.GatewayRentalFees
                                                    where p.GatewayIDLinked == deviceIDLinked
                                                    && p.RentalMonth.Date == rentalMonth.Date
                                                    select p).SingleOrDefault();

                                if (deviceRental == null)
                                {
                                    deviceRental = new GatewayRentalFee()
                                    {
                                        AgreedFee = agreedFee,
                                        GatewayIDLinked = deviceIDLinked,
                                        RentalMonth = rentalMonth,
                                        StandardFee = standardFee
                                    };
                                    db.GatewayRentalFees.Add(deviceRental);
                                    db.SaveChanges();
                                    result.AppendLine($"<span style=\"color:Green;\">{deviceIDLinked} - {gateway.Name} - Created</span>;<br />");
                                }
                                else
                                {
                                    deviceRental.AgreedFee = agreedFee;
                                    deviceRental.StandardFee = standardFee;
                                    db.SaveChanges();
                                    result.AppendLine($"<span style=\"color:Orange;\">{deviceIDLinked} - {gateway.Name} - Updated</span>;<br />");
                                }
                            }
                            else
                            {
                                result.AppendLine($"<span style=\"color:red;\">Could not find deviceIdLinked {deviceIDLinked}</span>;<br />");
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"<span style=\"color:red;\">There was an error with line {fileLine} - {ex.ToString()}</span>;<br />");
                    }
                }
            }

            rentalFeesUploadModel1.ErrorMessage = result.ToString();

            return View(rentalFeesUploadModel1);
        }

        [HttpGet]
        [Route("DetectAndMove")]
        public IActionResult DetectAndMove()
        {
            return Redirect("/");
            return View(new DetectAndMoveModel());
        }
        [HttpPost]
        [Route("DetectAndMove")]
        public IActionResult DetectAndMove(DetectAndMoveModel DetectAndMoveModel)
        {
            return Redirect("/");
            DetectAndMoveModel DetectAndMoveModel1 = DetectAndMoveModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            if (string.IsNullOrEmpty(DetectAndMoveModel.GatewayIDs) || string.IsNullOrEmpty(DetectAndMoveModel.ToSendTo))
            {
                DetectAndMoveModel1.ErrorMessage = "Gateway IDs and Emails may not be empty";

                return View(DetectAndMoveModel1);
            }

            var gatewayIDs = DetectAndMoveModel.GatewayIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var toSendTo = DetectAndMoveModel.ToSendTo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder result = new StringBuilder();

            List<int> gatewayIDints = new List<int>();

            foreach (var gw in gatewayIDs)
            {
                if (string.IsNullOrEmpty(gw))
                    continue;
                try { gatewayIDints.Add(Convert.ToInt32(gw)); }
                catch
                {
                    result.AppendLine($"Invalid gateway id: {gw}");
                }
            }

            if (!string.IsNullOrEmpty(result.ToString()))
            {
                DetectAndMoveModel1.ErrorMessage = result.ToString();

                return View(DetectAndMoveModel1);
            }

            if (gatewayIDints.Count < 2)
            {
                result.Append("Gateway IDs must be more than 1");
                DetectAndMoveModel1.ErrorMessage = result.ToString();

                return View(DetectAndMoveModel1);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                Data.DetectAndMove detectAndMove = new DetectAndMove()
                {
                    GatewayIDs = DetectAndMoveModel.GatewayIDs,
                    ToSendTo = DetectAndMoveModel.ToSendTo
                };
                db.DetectAndMoves.Add(detectAndMove);
                db.SaveChanges();
            }


            result.AppendLine($"Request received for:{DetectAndMoveModel.GatewayIDs} - {DetectAndMoveModel.ToSendTo}");


            DetectAndMoveModel1.ErrorMessage = result.ToString();

            return View(DetectAndMoveModel1);
        }
        [HttpGet]
        [Route("SignalOptimizer")]
        public IActionResult SignalOptimizer()
        {
            return Redirect("/");
            return View(new SignalOptimizerModel());
        }
        [HttpPost]
        [Route("SignalOptimizer")]
        public IActionResult SignalOptimizer(SignalOptimizerModel SignalOptimizerModel)
        {
            return Redirect("/");
            SignalOptimizerModel SignalOptimizerModel1 = SignalOptimizerModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            if (string.IsNullOrEmpty(SignalOptimizerModel.GatewayIDs) || string.IsNullOrEmpty(SignalOptimizerModel.ToSendTo))
            {
                SignalOptimizerModel1.ErrorMessage = "Gateway IDs and Emails may not be empty";

                return View(SignalOptimizerModel1);
            }

            var gatewayIDs = SignalOptimizerModel.GatewayIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var toSendTo = SignalOptimizerModel.ToSendTo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder result = new StringBuilder();

            List<int> gatewayIDints = new List<int>();

            foreach (var gw in gatewayIDs)
            {
                if (string.IsNullOrEmpty(gw))
                    continue;
                try { gatewayIDints.Add(Convert.ToInt32(gw)); }
                catch
                {
                    result.AppendLine($"Invalid gateway id: {gw}");
                }
            }

            if (!string.IsNullOrEmpty(result.ToString()))
            {
                SignalOptimizerModel1.ErrorMessage = result.ToString();

                return View(SignalOptimizerModel1);
            }

            if (gatewayIDints.Count < 2)
            {
                result.Append("Gateway IDs must be more than 1");
                SignalOptimizerModel1.ErrorMessage = result.ToString();

                return View(SignalOptimizerModel1);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                Data.SignalOptimizer SignalOptimizer = new SignalOptimizer()
                {
                    GatewayIDs = SignalOptimizerModel.GatewayIDs,
                    ToSendTo = SignalOptimizerModel.ToSendTo,
                    DateRequested = DateTime.Now,
                    LoopCount = SignalOptimizerModel.LoopCount,
                    SleepDurationMin = SignalOptimizerModel.SleepDurationMin,
                    DontMoveAboveThisStrength = SignalOptimizerModel.DontMoveAboveThisStrength
                };
                db.SignalOptimizers.Add(SignalOptimizer);
                db.SaveChanges();
            }


            result.AppendLine($"Request received for:{SignalOptimizerModel.GatewayIDs} - {SignalOptimizerModel.ToSendTo}");


            SignalOptimizerModel1.ErrorMessage = result.ToString();

            return View(SignalOptimizerModel1);
        }

        [Route("BuildingCouncilReconReport")]
        public IActionResult BuildingCouncilReconReport()
        {
            BuildingCouncilReconReportModel buildingCouncilReconReportModel = new BuildingCouncilReconReportModel();

            using (var db = new MyVoltageDbContext(_options))
            {
                buildingCouncilReconReportModel.Companies = db.Companies.OrderBy(p => p.Name).ToList();
            }

            return View(buildingCouncilReconReportModel);
        }
        [HttpPost]
        [Route("BuildingCouncilReconReport")]
        public IActionResult BuildingCouncilReconReport(BuildingCouncilReconReportModel BuildingCouncilReconReportModel)
        {

            if (BuildingCouncilReconReportModel.Companies == null || BuildingCouncilReconReportModel.Companies.Count == 0)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    BuildingCouncilReconReportModel.Companies = db.Companies.OrderBy(p => p.Name).ToList();
                }
            }

            BuildingCouncilReconReportModel BuildingCouncilReconReportModel1 = BuildingCouncilReconReportModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            if (string.IsNullOrEmpty(BuildingCouncilReconReportModel.ToSendTo))
            {
                BuildingCouncilReconReportModel1.ErrorMessage = "Emails may not be empty";

                return View(BuildingCouncilReconReportModel1);
            }

            var toSendTo = BuildingCouncilReconReportModel.ToSendTo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder result = new StringBuilder();

            using (var db = new MyVoltageDbContext(_options))
            {
                Data.BuildingCouncilReconReportConfig buildingCouncilReconReportConfig = new BuildingCouncilReconReportConfig()
                {
                    CompanyID = BuildingCouncilReconReportModel.CompanyID,
                    DateRequested = DateTime.Now,
                    ToSendTo = BuildingCouncilReconReportModel.ToSendTo
                };

                db.BuildingCouncilReconReportConfigs.Add(buildingCouncilReconReportConfig);
                db.SaveChanges();
            }


            result.AppendLine($"Request received for:{BuildingCouncilReconReportModel.Companies.Where(p => p.CompanyID == BuildingCouncilReconReportModel.CompanyID).SingleOrDefault().Name} - {BuildingCouncilReconReportModel.ToSendTo}");

            BuildingCouncilReconReportModel1.ErrorMessage = result.ToString();

            return View(BuildingCouncilReconReportModel1);
        }

        public class DetectAndMoveConfig
        {
            public List<string> ToSendTo { get; set; }
            public List<int> GatewayIDs { get; set; }
        }

        public class SignalOptimizerConfig
        {
            public List<string> ToSendTo { get; set; }
            public List<int> GatewayIDs { get; set; }
            public int SleepDurationMin { get; set; }
            public int LoopCount { get; set; }
        }


        private List<Company> AllRegisteredCompanies()
        {
            using (var db = new MyVoltageDbContext(_options))
            {

                var registeredCompanies = (from c in db.Companies
                                           where c.Registrable == true
                                           select new Company()
                                           {
                                               CompanyID = c.CompanyID,
                                               Name = c.Name
                                           }).ToList();

                return registeredCompanies;
            }
        }

        private NotificationSettingsViewModel PopulateNotificationSettingsViewModel(string id, Boolean edit)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).FirstOrDefault();
                if (edit)
                {
                    return new NotificationSettingsViewModel
                    {
                        NotificationEmail = user.Email,
                        NotificationPhoneNumber = customer.PhoneNumber,
                        DisconnectionLowBalanceNotification1 = customer.DisconnectionLowBalanceNotification1.ToString(),
                        LowBalanceNotifications = notificationSettings != null && notificationSettings.LowBalanceNotification1 ? "1" : "0",
                        HighUsageNotifications = customer.HighUsageNotifications ? "True" : "False",
                        LeakNotifications = customer.LeakNotifications ? "True" : "False",
                        NewsLetters = customer.NewsLetters ? "True" : "False",
                        YesNoSelectListItems = new List<SelectListItem>{
                                                new SelectListItem { Selected = false, Text = "No", Value = "False"},
                                                new SelectListItem { Selected = false, Text = "Yes", Value = "True"},
                                                }
                    };
                }

                return new NotificationSettingsViewModel
                {
                    NotificationEmail = user.Email,
                    NotificationPhoneNumber = customer.PhoneNumber,
                    YesNoSelectListItems = new List<SelectListItem>{
                                                new SelectListItem { Selected = false, Text = "No", Value = "False"},
                                                new SelectListItem { Selected = false, Text = "Yes", Value = "True"},
                                                }
                };
            }
        }

        private List<UserAccountViewModel> PopulateUserAccountView()
        {
            using (var db = new MyVoltageDbContext(_options))
            {

                var userAccountsViewModel = (from c in db.Customers
                                             join u in db.Users on c.UserID equals u.Id
                                             join a in db.AccountTypes on c.AccountTypeID equals a.AccountTypeID
                                             join co in db.Companies on c.CompanyID equals co.CompanyID
                                             where u.IsDeleted == false
                                             where c.IsDeleted == false
                                             select new UserAccountViewModel()
                                             {
                                                 CustomerId = c.CustomerID,
                                                 UserId = u.Id,
                                                 Fullname = c.FullName,
                                                 NotificationEmailAddress = u.Email,
                                                 CellphoneNum = u.PhoneNumber,
                                                 Company = co.Name,
                                                 CompanyId = co.CompanyID,
                                                 CustomerNumber = c.CustomerNumber,
                                                 AccountType = a.Name,
                                                 EmailVerified = u.EmailConfirmed,
                                                 CellVerified = u.PhoneNumberConfirmed,
                                                 Active = u.IsConfirmed,
                                                 MeterNumber = c.MeterNumber,
                                                 DisconnectionLowBalanceNotification1 = string.Format("R {0}", c.DisconnectionLowBalanceNotification1),
                                             }).ToList();

                return userAccountsViewModel;
            }
        }

        private List<UserAccountViewModel> PopulateFilteredUserAccountView(string keyword, List<UserAccountViewModel> viewModels)
        {
            List<UserAccountViewModel> filteredViewModel = new List<UserAccountViewModel>();

            foreach (var model in viewModels)
            {
                if (keyword == null)
                {
                    return null;
                }
                else
                {
                    if (model.Fullname != null)
                    {
                        if (model.Fullname.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredViewModel.Add(model);
                            continue;
                        }
                    }

                    if (model.CustomerNumber != null)
                    {
                        if (model.CustomerNumber.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredViewModel.Add(model);
                            continue;
                        }
                    }

                    if (model.NotificationEmailAddress != null)
                    {
                        if (model.NotificationEmailAddress.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredViewModel.Add(model);
                            continue;
                        }
                    }

                    if (model.CellphoneNum != null)
                    {
                        if (model.CellphoneNum.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredViewModel.Add(model);
                            continue;
                        }
                    }

                    if (model.MeterNumber != null)
                    {
                        if (model.MeterNumber.ToLower().Contains(keyword.ToLower()))
                        {
                            filteredViewModel.Add(model);
                            continue;
                        }
                    }
                }
            }

            return filteredViewModel;
        }

        private UserAccountViewModel PopulateSpecificUserAccountView(string userID, int CustomerID)
        {
            using (var db = new MyVoltageDbContext(_options))
            {

                var userAccountsViewModel = (from c in db.Customers
                                             join u in db.Users on c.UserID equals u.Id
                                             join a in db.AccountTypes on c.AccountTypeID equals a.AccountTypeID
                                             join co in db.Companies on c.CompanyID equals co.CompanyID
                                             where u.Id == userID
                                             where c.CustomerID == CustomerID
                                             where u.IsDeleted == false
                                             where c.IsDeleted == false
                                             select new UserAccountViewModel()
                                             {
                                                 CustomerId = c.CustomerID,
                                                 UserId = u.Id,
                                                 Fullname = c.FullName,
                                                 NotificationEmailAddress = u.Email,
                                                 CellphoneNum = u.PhoneNumber,
                                                 Company = co.Name,
                                                 CompanyId = co.CompanyID,
                                                 CustomerNumber = c.CustomerNumber,
                                                 AccountType = a.Name,
                                                 EmailVerified = u.EmailConfirmed,
                                                 CellVerified = u.PhoneNumberConfirmed,
                                                 Active = u.IsConfirmed,
                                                 MeterNumber = c.MeterNumber,
                                                 DisconnectionLowBalanceNotification1 = string.Format("R {0}", c.DisconnectionLowBalanceNotification1),
                                             }).FirstOrDefault();

                return userAccountsViewModel;
            }
        }

        private MeterJsonModel GetMeterUsageByDayView(string meterNumber, string meterType, int year, int month, string userID)
        {
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), null, _contextAccessor, _config, _options, null);
            int metertype = provider.GetMeterType(meterNumber, userID);

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _customerProvider.AccountType == (int)AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var model = provider.GetMeterUsageByDay(meterNumber, meterType, year, month, prePaidBalance, isSolar);

            return model;

        }

        private Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> fixDemandData(string meterNumber, Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> data, string userID)
        {
            MeterProvider provider = new MeterProvider(_customerProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), null, _contextAccessor, _config, _options, null);
            int metertype = provider.GetMeterType(meterNumber, userID);

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

        public List<BulkReadingsRegister> DefaultRegisters()
        {
            return new List<Models.MeterViewModels.BulkReadingsRegister>()
                {
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 1, Name = "Active Energy", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 2, Name = "Reactive Energy", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 29, Name = "Max Demand", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 70, Name = "CT Ratio", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 80, Name = "Water Consumption", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 140, Name = "Gas Consumption", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 91, Name = "Contactor State", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 100, Name = "Internal Battery V", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 101, Name = "Signal RSSI", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 106, Name = "SNR", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 102, Name = "Temp", Selected = false },
                    new Models.MeterViewModels.BulkReadingsRegister() { ID = 90, Name = "Remaining Credit", Selected = false },
                };
        }

    }
}