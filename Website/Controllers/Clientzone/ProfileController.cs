using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Api.Zendesk;
using MyVoltage.Data;
using MyVoltage.Data.Migrations;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.ClientzoneModels;
using MyVoltage.Models.OperationalModels;
using MyVoltage.Models.OperationalModels.SearchModels;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static MyVoltage.Models.ClientzoneModels.ProfileModel;

namespace MyVoltage.Controllers.Operational
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ProfileController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClientzoneProvider _clientzoneProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _context;

        public ProfileController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            ClientzoneProvider clientzoneProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _clientzoneProvider = clientzoneProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/clientzone/profile")]
        public async Task<IActionResult> Profile()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            ProfileModel model = new ProfileModel()
            {
                Customer_UsageMeterSettings = new List<ProfileModel.Customer_UsageMeterSetting>(),
            };

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber && p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                if (localCustomer != null)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == localCustomer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                    model.ShowDetailedDailyBilling = localCustomer.ShowDetailedDailyBilling.HasValue ? localCustomer.ShowDetailedDailyBilling.Value : false;
                    model.ShowCostInclVAT = localCustomer.ShowCostInclVAT.HasValue ? localCustomer.ShowCostInclVAT.Value : false;
                    model.ShowHourlyUsage = localCustomer.ShowDailyUsage.HasValue ? localCustomer.ShowDailyUsage.Value : false;
                    model.FullName = localCustomer.FullName;
                    model.OccupancyDate = localCustomer.OccupancyDate != null ? localCustomer.OccupancyDate : DateTime.Now;
                    model.PhoneNumber = localCustomer.PhoneNumber != null ? localCustomer.PhoneNumber : localCustomer.AltPhoneNumber;
                    model.CompanyName = _clientzoneProvider.CompanyName;
                    model.CustomerNumber = _clientzoneProvider.CustomerNumber;
                    model.AccountType = _clientzoneProvider.AccountTypeForSelectedCustomer.GetDescription();
                    model.LoginEmail = _userManager.GetUserName(User);
                    model.NotificationEmail = localCustomer.NotificationEmail;

                    model.LowBalanceNotifications = notificationSettings.LowBalanceNotification1;
                    model.HighUsageNotifications = localCustomer.HighUsageNotifications;
                    model.LeakNotifications = localCustomer.LeakNotifications;
                    model.NewsLetters = localCustomer.NewsLetters;
                    model.BalanceAndConsumptionSMS = localCustomer.BalanceNotificationSMSType;
                    model.LowBalanceNotification = localCustomer.DisconnectionLowBalanceNotification1.HasValue ? localCustomer.DisconnectionLowBalanceNotification1.Value : 0;
                    model.InfoNotificationType = localCustomer.InfoNotificationTypeID.HasValue ? localCustomer.InfoNotificationTypeID.Value : 0;
                    model.SystemNotificationType = localCustomer.SystemNotificationTypeID.HasValue ? localCustomer.SystemNotificationTypeID.Value : 0;

                    model.ActivateTaxInvoice = localCustomer.ActivateTaxInvoice.HasValue ? localCustomer.ActivateTaxInvoice.Value : false;
                    model.RecipientAddress = localCustomer.RecipientAddress;
                    model.RecipientName = localCustomer.RecipientName;
                    model.RecipientReferenceNumber = localCustomer.RecipientReferenceNumber;
                    model.RecipientVatNumber = localCustomer.RecipientVATNumber;


                    //StringBuilder fullAddress = new StringBuilder();

                    //if (!string.IsNullOrEmpty(localCustomer.StreetAddress))
                    //{
                    //    fullAddress.Append(localCustomer.StreetAddress);
                    //}

                    //if (!string.IsNullOrEmpty(localCustomer.Suburb))
                    //{
                    //    if (fullAddress.Length > 0) fullAddress.Append(", ");
                    //    fullAddress.Append(localCustomer.Suburb);
                    //}

                    //if (!string.IsNullOrEmpty(localCustomer.TownOrCity))
                    //{
                    //    if (fullAddress.Length > 0) fullAddress.Append(", ");
                    //    fullAddress.Append(localCustomer.TownOrCity);
                    //}

                    //if (!string.IsNullOrEmpty(localCustomer.Province))
                    //{
                    //    if (fullAddress.Length > 0) fullAddress.Append(", ");
                    //    fullAddress.Append(localCustomer.Province);
                    //}

                    //if (localCustomer.PostalCode.HasValue)
                    //{
                    //    if (fullAddress.Length > 0) fullAddress.Append(", ");
                    //    fullAddress.Append(localCustomer.PostalCode.Value);
                    //}

                    //model.RecipientAddress = fullAddress.ToString();

                    var apiClient = new SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                    var meters = apiClient.GetMetersByCustomer(_clientzoneProvider.CustomerNumber);
                    var company = db.Companies.FirstOrDefault(x => x.CompanyID == _clientzoneProvider.CompanyID);
                    foreach (var meter in meters)
                    {
                        var customer_UsageMeterSetting = new ProfileModel.Customer_UsageMeterSetting()
                        {
                            DeviceType = DeviceType.DeviceTypeEnum.Unknown,
                            DisplayType = (int)MyVoltage.Data.MeterTypeEnum.Balance,
                            MeterSerial = meter.Serial_No,
                        };

                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                        if (device != null)
                        {
                            var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == localCustomer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();
                            if (customerMeter != null)
                            {
                                var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();
                                if (customerMeterType != null)
                                {
                                    customer_UsageMeterSetting = new ProfileModel.Customer_UsageMeterSetting()
                                    {
                                        DeviceType = (DeviceType.DeviceTypeEnum)device.type.id,
                                        DisplayType = customerMeterType.Selected,
                                        MeterSerial = meter.Serial_No,
                                    };
                                }
                            }
                        }

                        model.Customer_UsageMeterSettings.Add(customer_UsageMeterSetting);
                    }

                    var customer_UsageMeterSettings = new List<ProfileModel.Customer_UsageMeterSetting>();

                    foreach (var customerMeter in model.Customer_UsageMeterSettings)
                    {
                        if(customerMeter.DeviceType == DeviceType.DeviceTypeEnum.Unknown)
                        {
                            var customer_meter = _clientzoneProvider.Meters.FirstOrDefault(x => x.SerialNo == customerMeter.MeterSerial);
                            customerMeter.DeviceType = customer_meter != null ? customer_meter.DeviceType : DeviceType.DeviceTypeEnum.Unknown;
                        }

                        customer_UsageMeterSettings.Add(customerMeter);
                    }

                    model.Customer_UsageMeterSettings = customer_UsageMeterSettings;

                    model.DisplayType = model.Customer_UsageMeterSettings.FirstOrDefault().DisplayType;
                }
            }


            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["notUpdate"]))
            {
                return Json(model);
            }
            return View("~/Views/Clientzone/Profile.cshtml", model);
        }

        [HttpPost]
        [Route("/clientzone/profile/update_usagedisplay")]
        public async Task<IActionResult> Profile_Update_UsageDisplay()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var result = new
            {
                isSuccess = true,
                result = "",
            };
            var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
            if (localCustomer != null)
            {
                if (!string.IsNullOrEmpty(Request.Form["showDetailedDailyBilling"].ToString()))
                {
                    localCustomer.ShowDetailedDailyBilling = Convert.ToBoolean(Request.Form["showDetailedDailyBilling"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["showCostInclVAT"].ToString()))
                {
                    localCustomer.ShowCostInclVAT = Convert.ToBoolean(Request.Form["showCostInclVAT"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["showHourlyUsage"].ToString()))
                {
                    localCustomer.ShowDailyUsage = Convert.ToBoolean(Request.Form["showHourlyUsage"]);
                }

                db.Update(localCustomer);
                db.SaveChanges();

                if (!string.IsNullOrEmpty(Request.Form["meters"].ToString()))
                {
                    //Dictionary<string, int> postedMeters = new Dictionary<string, int>();
                    //foreach (string meter in Request.Form["meters"].ToString().Split(';'))
                    //{
                    //    if (string.IsNullOrEmpty(meter))
                    //        continue;
                    //    postedMeters.Add(meter.Split('=')[0], Convert.ToInt32(meter.Split('=')[1]));
                    //}
                    var selectedMeterType = Convert.ToInt32(Request.Form["meters"].ToString());
                    var apiClient = new SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                    var meters = apiClient.GetMetersByCustomer(_clientzoneProvider.CustomerNumber);
                    foreach (var meter in meters)
                    {
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                        if (device != null)
                        {
                            var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == localCustomer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();
                            if (customerMeter == null)
                            {
                                customerMeter = new CustomerMeter()
                                {
                                    CustomerID = localCustomer.CustomerID,
                                    MeterNumber = device.id,
                                };
                                db.Add(customerMeter);
                                db.SaveChanges();
                            }
                            var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();
                            if (customerMeterType == null)
                            {
                                customerMeterType = new CustomerMeterType()
                                {
                                    CustomerMeterID = customerMeter.CustomerMeterID,
                                    //MeterTypeID = postedMeters[meter.Serial_No],
                                    //Selected = postedMeters[meter.Serial_No],
                                    MeterTypeID = selectedMeterType,
                                    Selected = selectedMeterType,
                                };
                                db.Add(customerMeterType);
                                db.SaveChanges();
                            }
                            //else if (customerMeterType.Selected != postedMeters[meter.Serial_No])
                            else if (customerMeterType.Selected != selectedMeterType)
                            {
                                //customerMeterType.Selected = postedMeters[meter.Serial_No];
                                customerMeterType.Selected = selectedMeterType;
                                db.Update(customerMeterType);
                                db.SaveChanges();
                            }
                        }

                    }
                }
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        [HttpPost]
        [Route("/clientzone/profile/update_personalprofile")]
        public async Task<IActionResult> Profile_Update_PersonalProfile()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var result = new
            {
                isSuccess = true,
                result = "",
            };
            var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
            if (localCustomer != null)
            {
                if (!string.IsNullOrEmpty(Request.Form["occupancyDate"].ToString()))
                {
                    localCustomer.OccupancyDate = Convert.ToDateTime(Request.Form["occupancyDate"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["fullName"].ToString()))
                {
                    localCustomer.FullName = Request.Form["fullName"].ToString();
                }

                if (!string.IsNullOrEmpty(Request.Form["phoneNumber"].ToString()))
                {
                    localCustomer.PhoneNumber = Request.Form["phoneNumber"].ToString();
                    localCustomer.NotificationPhoneNumber = Request.Form["phoneNumber"].ToString();
                }

                if (!string.IsNullOrEmpty(Request.Form["notificationEmail"].ToString()))
                {
                    localCustomer.NotificationEmail = Request.Form["notificationEmail"].ToString();
                }

                db.Update(localCustomer);
                db.SaveChanges();
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        [HttpPost]
        [Route("/clientzone/profile/update_notifications")]
        public async Task<IActionResult> Profile_Update_Notifications()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var result = new
            {
                isSuccess = true,
                result = "",
            };
            var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
            if (localCustomer != null)
            {
                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == localCustomer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Form["lowBalanceNotifications"].ToString()))
                {
                    notificationSettings.LowBalanceNotification1 = Convert.ToBoolean(Request.Form["lowBalanceNotifications"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["highUsageNotifications"].ToString()))
                {
                    localCustomer.HighUsageNotifications = Convert.ToBoolean(Request.Form["highUsageNotifications"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["leakNotifications"].ToString()))
                {
                    localCustomer.LeakNotifications = Convert.ToBoolean(Request.Form["leakNotifications"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["newsLetters"].ToString()))
                {
                    localCustomer.NewsLetters = Convert.ToBoolean(Request.Form["newsLetters"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["lowBalanceNotification"].ToString()))
                {
                    localCustomer.DisconnectionLowBalanceNotification1 = Convert.ToInt32(Request.Form["lowBalanceNotification"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["systemNotificationType"].ToString()))
                {
                    localCustomer.SystemNotificationTypeID = Convert.ToInt32(Request.Form["systemNotificationType"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["infoNotificationType"].ToString()))
                {
                    localCustomer.InfoNotificationTypeID = Convert.ToInt32(Request.Form["infoNotificationType"]);
                }

                db.Update(localCustomer);
                db.Update(notificationSettings);
                db.SaveChanges();
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        [HttpPost]
        [Route("/clientzone/profile/update_taxinvoice")]
        public async Task<IActionResult> Profile_Update_TaxInvoice()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Request.Form),
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var result = new
            {
                isSuccess = true,
                result = "",
            };
            var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
            if (localCustomer != null)
            {
                if (!string.IsNullOrEmpty(Request.Form["activateTaxInvoice"].ToString()))
                {
                    localCustomer.ActivateTaxInvoice = Convert.ToBoolean(Request.Form["activateTaxInvoice"]);
                }
                localCustomer.RecipientName = Request.Form["recipientName"].ToString();
                localCustomer.RecipientAddress = Request.Form["recipientAddress"].ToString();
                localCustomer.RecipientVATNumber = Request.Form["recipientVatNumber"].ToString();
                localCustomer.RecipientReferenceNumber = Request.Form["recipientReferenceNumber"].ToString();

                db.Update(localCustomer);
                db.SaveChanges();
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        [HttpGet("/clientzone/profile/check_number_status")]
        public IActionResult GetVerificationStatus()
        {
            try
            {
                var db = new MyVoltageDbContext(_options);
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
                bool isNumberChanged = false;
                bool isEmailChanged = false;

                Random random = new Random();
                var numbers = "1234567890";
                var stringNumbers = new char[4];

                for (int i = 0; i < stringNumbers.Length; i++)
                {
                    stringNumbers[i] = numbers[random.Next(numbers.Length)];
                }
                var OTPCode = new string(stringNumbers);

                if (!string.IsNullOrEmpty(Request.Query["ph"].ToString()))
                {
                    if (localCustomer != null && localCustomer.PhoneNumber != Request.Query["ph"].ToString().Trim())
                    {
                        if (localCustomer.PhoneNumber == null && localCustomer.AltPhoneNumber == Request.Query["ph"].ToString().Trim()) //if phone number is null
                        {
                            localCustomer.PhoneNumber = localCustomer.AltPhoneNumber;
                        }
                        else
                        {
                            isNumberChanged = true;
                            localCustomer.OTPCode = OTPCode;
                            SMS.SendSms("27" + Request.Query["ph"].ToString().Remove(0, 1), "Your My Voltage Cellphone verification code is " + OTPCode + ". Enter this code to update your cellphone detail.");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(Request.Query["em"].ToString()))
                {
                    if (localCustomer != null && localCustomer.NotificationEmail != Request.Query["em"].ToString())
                    {
                        var username = _userManager.GetUserName(User);
                        if (localCustomer.NotificationEmail == null && username == Request.Query["em"].ToString())
                        {
                            localCustomer.NotificationEmail = username;
                        }
                        else
                        {
                            isEmailChanged = true;
                            localCustomer.OTPCode = OTPCode;
                            _emailSender.SendNotificationEmailCodeAsync(Request.Query["em"].ToString(), OTPCode, localCustomer.FullName, _context);
                        }

                    }
                }
                db.Customers.Update(localCustomer);
                db.SaveChanges();
                return Json(new { isNumberChanged = isNumberChanged, isEmailChanged = isEmailChanged });
            }
            catch (Exception ex)
            {
                return Json(new { isNumberChanged = false, isEmailChanged = false });
                throw;
            }

        }

        [HttpPost("clientzone/profile/confirm_profile_otp")]
        public IActionResult ConfirmProfileOTP()
        {
            try
            {
                if (!String.IsNullOrEmpty(Request.Form["otp"].ToString()) && Request.Form["otp"].ToString().Length == 4)
                {
                    var db = new MyVoltageDbContext(_options);
                    var localCustomer = db.Customers.FirstOrDefault(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber);
                    if (localCustomer.OTPCode == Request.Form["otp"].ToString())
                    {

                        if (!string.IsNullOrEmpty(Request.Form["authType"].ToString()))
                        {
                            if (Request.Form["authType"].ToString() == "email")
                            {
                                if (!string.IsNullOrEmpty(Request.Form["email"].ToString()))
                                {
                                    localCustomer.NotificationEmail = Request.Form["email"].ToString();
                                }
                            }
                            else if (Request.Form["authType"].ToString() == "phone")
                            {
                                if (!string.IsNullOrEmpty(Request.Form["phone"].ToString()))
                                {
                                    localCustomer.PhoneNumber = Request.Form["phone"].ToString();
                                }
                            }
                            db.Customers.Update(localCustomer);
                            db.SaveChanges();
                        }
                        return Json(new { status = true, msg = "User profile updated successfully." });
                    }
                }
                return Json(new { status = false, msg = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, msg = "failed" });
            }
        }

        [HttpGet("clientzone/profile/resend-otp")]
        public async Task<IActionResult> OTPResend()
        {
            try
            {
                var db = new MyVoltageDbContext(_options);
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
                bool isNumberChanged = false;
                bool isEmailChanged = false;

                Random random = new Random();
                var numbers = "1234567890";
                var stringNumbers = new char[4];

                for (int i = 0; i < stringNumbers.Length; i++)
                {
                    stringNumbers[i] = numbers[random.Next(numbers.Length)];
                }

                var OTPCode = new string(stringNumbers);

                if (localCustomer != null && !string.IsNullOrEmpty(Request.Query["em"].ToString()))
                {
                    isEmailChanged = true;
                    localCustomer.OTPCode = OTPCode;
                    _emailSender.SendNotificationEmailCodeAsync(Request.Query["em"].ToString(), localCustomer.OTPCode, localCustomer.FullName, _context);
                    db.Customers.Update(localCustomer);
                    db.SaveChanges();
                    return Json(new { status = true, msg = "OTP resent successfully." });
                }

                if (localCustomer != null && !string.IsNullOrEmpty(Request.Query["ph"].ToString()))
                {
                    isNumberChanged = true;
                    localCustomer.OTPCode = OTPCode;
                    SMS.SendSms("27" + Request.Query["ph"].ToString().Remove(0, 1), "Your My Voltage Cellphone verification code is " + localCustomer.OTPCode + ". Enter this code to update your cellphone detail.");
                    db.Customers.Update(localCustomer);
                    db.SaveChanges();
                    return Json(new { status = true, msg = "OTP resent successfully." });
                }
                return Json(new { status = false, msg = "Error" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, msg = "Error" });
            }
        }
    }
}
