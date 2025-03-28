using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Models.OperationalModels.Customer.CustomerProfileModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class ProfileController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _options = options;
            _operationalProvider = operationalProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, null);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
        }

        [HttpGet("/operational/Customer/Customer_Profile")]
        public async Task<IActionResult> Profile()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Profile}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var currentUser = _userManager.GetUserAsync(User).Result;

            Customer_ProfileModel model = new Customer_ProfileModel()
            {
                SystemNotificationTypeID = new List<SelectListItem>(),
            };
            using (var db = new MyVoltageDbContext(_options))
            {
                #region Opretational Profile

                model.OperationalProfile = _operationalProvider.OperationalProfile;
                //model.OperationalEmail = currentUser.Email;
                //model.OperationalFirstName = _operationalProvider.OperationalProfile.FirstName;
                //model.OperationalLastName = _operationalProvider.OperationalProfile.LastName;
                //model.OperationalPhoneNumber = currentUser.PhoneNumber;
                //model.OperationalJobRole = _operationalProvider.OperationalProfile.JobTitle;

                #endregion

                Data.Customer customer = null;
                if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
                {
                    var sC = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber && p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                    model.SkybillCustomer = sC;
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _operationalProvider.CustomerNumber && tbl.MeterNumber == _operationalProvider.CustomerMeterSerial && tbl.IsDeleted == false).FirstOrDefault();
                    if (customer == null)
                        customer = db.Customers.Where(tbl => tbl.CustomerNumber == _operationalProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
                    if (sC == null)
                    {
                        sC = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).FirstOrDefault();
                        model.SkybillCustomer = sC;
                    }
                }
                else
                    customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();


                model.SelectedCustomer = customer;

                if (customer != null)
                {
                    if (customer.UserID == _operationalProvider.OperationalProfile.UserID)
                        model.IsCurrentUserCustomer = true;

                    var user = _userManager.FindByIdAsync(customer.UserID).Result;

                    var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                    var apiClient = new SkyBillApiClient(company.Name, _cache);
                    var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                    var localDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == customer.CompanyID).ToList();

                    model.CustomerMeterTypes = new List<Customer_ProfileModel.ProfileCustomerMeterType>();

                    foreach (var meter in meters)
                    {
                        var oldCustomer = db.Customers.Where(p => p.MeterNumber == meter.Serial_No && !p.IsDeleted).FirstOrDefault();
                        if (oldCustomer != null)
                        {
                            model.OldUserForMeter = oldCustomer.FullName;
                            model.OldUserIDForMeter = oldCustomer.UserID;
                        }
                        var localDev = localDevices.Where(p => p.Serial == meter.Serial_No).FirstOrDefault();
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No, localDev != null ? localDev.DeviceAPIIDValue : 1);
                        if (device != null)
                        {
                            var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();

                            if (customerMeter == null)
                            {
                                customerMeter = new CustomerMeter()
                                {
                                    CustomerID = customer.CustomerID,
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
                                    MeterTypeID = (int)MeterTypeEnum.None,
                                    Selected = (int)MeterTypeEnum.None,
                                };
                                db.Add(customerMeterType);
                                db.SaveChanges();
                            }


                            string balanceSymbol = "R";
                            switch (customer.AccountTypeID)
                            {
                                case (int)AccountTypeEnum.PrepaidCredit:
                                    balanceSymbol = "kWh";
                                    break;
                            }

                            List<SelectListItem> selectListItems = new List<SelectListItem>()
                            {
                                new SelectListItem() { Value = "1", Text = $"Balance ({balanceSymbol})", Selected = customerMeterType.Selected == 1 ? true : false },
                                new SelectListItem() { Value = "2", Text = "Demand", Selected = customerMeterType.Selected == 2 ? true : false },
                                new SelectListItem() { Value = "3", Text = "None", Selected = customerMeterType.Selected == 3 ? true : false },
                                new SelectListItem() { Value = "4", Text = "Solar", Selected = customerMeterType.Selected == 4 ? true : false },
                            };
                            model.CustomerMeterTypes.Add(new Customer_ProfileModel.ProfileCustomerMeterType()
                            {
                                MeterSerial = meter.Serial_No,
                                selectList = selectListItems
                            });
                        }
                    }

                    //profileViewModel.User = user;
                    //profileViewModel.Customer = customer;
                    //profileViewModel.Company = company;
                    model.AccountType = new List<SelectListItem>();
                    foreach (AccountTypeEnum accountType in (AccountTypeEnum[])Enum.GetValues(typeof(AccountTypeEnum)))
                        model.AccountType.Add(new SelectListItem()
                        {
                            Value = ((int)accountType).ToString(),
                            Text = accountType.GetDescription(),
                            Selected = customer.AccountTypeID == (int)accountType ? true : false,
                        });

                    model.Email = user.Email;
                    model.CustomerNumber = customer.CustomerNumber;
                    model.OccupancyDate = customer.OccupancyDate;
                    model.CompanyName = company.Name;
                    model.IsConfirmed = user.IsConfirmed;

                    model.FullName = customer.FullName;
                    model.AutoConvertBalanceToUnits = !customer.AutoConvertBalanceToUnits.HasValue || customer.AutoConvertBalanceToUnits.Value ? true : false;
                    model.PhoneNumber = customer.NotificationPhoneNumber;
                    model.AltPhoneNumber = customer.AltPhoneNumber;

                    model.RecipientName = customer.RecipientName;
                    model.RecipientAddress = customer.RecipientAddress;
                    model.RecipientVATNumber = customer.RecipientVATNumber;
                    model.RecipientReferenceNumber = customer.RecipientReferenceNumber;

                    if (string.IsNullOrEmpty(model.PhoneNumber) && !string.IsNullOrEmpty(customer.PhoneNumber))
                        model.PhoneNumber = customer.PhoneNumber;


                    model.NotificationEMail = customer.NotificationEmail;
                    if (string.IsNullOrEmpty(model.NotificationEMail))
                        model.NotificationEMail = user.Email;

                    var serials = meters.Select(p => p.Serial_No).Distinct().ToList();

                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                    model.LowBalanceNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes" },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = true },
                        };
                    if (notificationSettings != null)
                        model.LowBalanceNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = notificationSettings.LowBalanceNotification1 ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = notificationSettings.LowBalanceNotification1? false : true },
                        };

                    model.LeakNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.LeakNotifications ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.LeakNotifications? false : true },
                        };

                    model.HighUsageNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.HighUsageNotifications ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.HighUsageNotifications? false : true },
                        };

                    model.NewsLetters = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.NewsLetters ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.NewsLetters? false : true },
                        };

                    model.ShowHourlyUsage = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes" },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = true },
                        };
                    if (customer.ShowDailyUsage.HasValue)
                        model.ShowHourlyUsage = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.ShowDailyUsage.Value ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.ShowDailyUsage.Value ? false : true },
                        };

                    model.ShowCostInclVAT = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes" },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = true },
                        };
                    if (customer.ShowCostInclVAT.HasValue)
                        model.ShowCostInclVAT = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.ShowCostInclVAT.Value ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.ShowCostInclVAT.Value ? false : true },
                        };

                    model.ActivateTaxInvoice = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes" },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = true },
                        };
                    if (customer.ActivateTaxInvoice.HasValue)
                        model.ActivateTaxInvoice = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.ActivateTaxInvoice.Value ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.ActivateTaxInvoice.Value ? false : true },
                        };

                    model.ShowCustomBankingDetails = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes" },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = true },
                        };
                    if (customer.ShowCustomBankingDetails.HasValue)
                        model.ShowCustomBankingDetails = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = customer.ShowCustomBankingDetails.Value ? true : false },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = customer.ShowCustomBankingDetails.Value ? false : true },
                        };

                    model.LowBalanceNotification = customer.DisconnectionLowBalanceNotification1.HasValue ? customer.DisconnectionLowBalanceNotification1.Value : 0;

                    List<SelectListItem> balanceNotificationSMSType = new List<SelectListItem>()
                    {
                        new SelectListItem() { Value = "None", Text = $"None (Free)", Selected = string.IsNullOrEmpty(customer.BalanceNotificationSMSType) || customer.BalanceNotificationSMSType == "None" ? true : false },
                        new SelectListItem() { Value = "Daily", Text = $"Daily (R2.00 per sms)", Selected = customer.BalanceNotificationSMSType == "Daily" ? true : false },
                        new SelectListItem() { Value = "Weekly", Text = $"Weekly (Free)", Selected = customer.BalanceNotificationSMSType == "Weekly" ? true : false },
                    };


                    model.BalanceNotificationSMSType = balanceNotificationSMSType;

                    model.SystemNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                                      select new SelectListItem()
                                                      {
                                                          Text = p.GetDescription(),
                                                          Value = ((int)p).ToString(),
                                                          Selected = customer.SystemNotificationTypeID.HasValue && customer.SystemNotificationTypeID.Value == (int)p,
                                                      }).ToList();

                    model.InfoNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                                    select new SelectListItem()
                                                    {
                                                        Text = p.GetDescription(),
                                                        Value = ((int)p).ToString(),
                                                        Selected = customer.InfoNotificationTypeID.HasValue && customer.InfoNotificationTypeID.Value == (int)p,
                                                    }).ToList();
                }

            }


            if (_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.Edit))
                model.AllowCustomerEdit = true;

            if (model.IsCurrentUserCustomer)
                model.AllowCustomerEdit = true;

            return View("~/Views/Operational/Customer/Profile/Profile.cshtml", model);
        }

        [HttpPost("/operational/Customer/Customer_Profile")]
        public async Task<IActionResult> Profile(Customer_ProfileModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Profile}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var currentUser = _userManager.GetUserAsync(User).Result;

            var db = new MyVoltageDbContext(_options);
            bool hasErrors = false;
            if (!ModelState.IsValid)
                hasErrors = true;

            //StringBuilder sbError = new StringBuilder();
            //foreach (var p in Request.Form)
            //{
            //    sbError.AppendLine($"{p.Key}:{p.Value}");
            //}

            //throw new Exception(sbError.ToString());

            #region Populate Model from Posted Model

            Data.Customer customer = null;
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber && p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                model.SkybillCustomer = sC;
                customer = db.Customers.Where(tbl => tbl.CustomerNumber == _operationalProvider.CustomerNumber && tbl.MeterNumber == _operationalProvider.CustomerMeterSerial && tbl.IsDeleted == false).FirstOrDefault();
                if (customer == null)
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _operationalProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
            }
            else
                customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();

            model.SelectedCustomer = customer;

            if (customer != null)
            {
                var user = _userManager.FindByIdAsync(customer.UserID).Result;

                var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                var localDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == customer.CompanyID).ToList();

                var apiClient = new SkyBillApiClient(company.Name, _cache);
                var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                model.CustomerMeterTypes = new List<Customer_ProfileModel.ProfileCustomerMeterType>();

                foreach (var meter in meters)
                {
                    var localDev = localDevices.Where(p => p.Serial == meter.Serial_No).FirstOrDefault();
                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No, localDev != null ? localDev.DeviceAPIIDValue : 1);

                    if (device != null)
                    {
                        var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();
                        if (customerMeter != null)
                        {
                            var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                            string balanceSymbol = "R";
                            switch (customer.AccountTypeID)
                            {
                                case (int)AccountTypeEnum.PrepaidCredit:
                                    balanceSymbol = "kWh";
                                    break;
                            }

                            if (customerMeterType != null)
                            {
                                List<SelectListItem> selectListItems = new List<SelectListItem>()
                                {
                                    new SelectListItem() { Value = "1", Text = $"Balance ({balanceSymbol})", Selected = Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"] == "1" ? true : false },
                                    new SelectListItem() { Value = "2", Text = "Demand", Selected = Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"] == "2" ? true : false },
                                    new SelectListItem() { Value = "3", Text = "None", Selected = Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"] == "3" ? true : false },
                                    new SelectListItem() { Value = "4", Text = "Solar", Selected = Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"] == "4" ? true : false },
                                };
                                model.CustomerMeterTypes.Add(new Customer_ProfileModel.ProfileCustomerMeterType()
                                {
                                    MeterSerial = meter.Serial_No,
                                    selectList = selectListItems
                                });
                            }
                        }
                    }
                }

                model.AccountType = new List<SelectListItem>();
                foreach (AccountTypeEnum accountType in (AccountTypeEnum[])Enum.GetValues(typeof(AccountTypeEnum)))
                    model.AccountType.Add(new SelectListItem()
                    {
                        Value = ((int)accountType).ToString(),
                        Text = accountType.GetDescription(),
                        Selected = Request.Form["AccountType"] == ((int)accountType).ToString() ? true : false,
                    });

                model.LowBalanceNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["LowBalanceNotifications"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["LowBalanceNotifications"]) ? false : true) },
                        };

                model.LeakNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["LeakNotifications"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["LeakNotifications"]) ? false : true) },
                        };

                model.HighUsageNotifications = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["HighUsageNotifications"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["HighUsageNotifications"]) ? false : true) },
                        };

                model.NewsLetters = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["NewsLetters"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["NewsLetters"]) ? false : true) },
                        };

                model.ShowHourlyUsage = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["ShowHourlyUsage"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["ShowHourlyUsage"]) ? false : true) },
                        };

                model.ShowCostInclVAT = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["ShowCostInclVAT"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["ShowCostInclVAT"]) ? false : true) },
                        };

                model.ShowCustomBankingDetails = new List<SelectListItem>()
                        {
                            new SelectListItem(){ Value = "true", Text = "Yes", Selected = (Convert.ToBoolean(Request.Form["ShowCustomBankingDetails"]) ? true : false) },
                            new SelectListItem(){ Value = "false", Text = "No", Selected = (Convert.ToBoolean(Request.Form["ShowCustomBankingDetails"]) ? false : true) },
                        };

                model.LowBalanceNotification = Convert.ToInt32(Request.Form["LowBalanceNotification"]);

                model.BalanceNotificationSMSType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "None", Text = $"None (Free)", Selected = Request.Form["BalanceNotificationSMSType"] == "None" ? true : false },
                    new SelectListItem() { Value = "Daily", Text = $"Daily (R2.00 per sms)", Selected = Request.Form["BalanceNotificationSMSType"] == "Daily" ? true : false },
                    new SelectListItem() { Value = "Weekly", Text = $"Weekly (Free)", Selected = Request.Form["BalanceNotificationSMSType"] == "Weekly" ? true : false },
                };

                model.SystemNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                                  select new SelectListItem()
                                                  {
                                                      Text = p.GetDescription(),
                                                      Value = ((int)p).ToString(),
                                                      Selected = Request.Query["SystemNotificationTypeID"] == ((int)p).ToString() ? true : false,
                                                  }).ToList();
                model.InfoNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                                select new SelectListItem()
                                                {
                                                    Text = p.GetDescription(),
                                                    Value = ((int)p).ToString(),
                                                    Selected = Request.Query["InfoNotificationTypeID"] == ((int)p).ToString() ? true : false,
                                                }).ToList();
            }
            else
            {
                hasErrors = false;

                if (ModelState.ContainsKey("FullName"))
                {
                    ModelState["FullName"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                    ModelState["FullName"].Errors.Clear();
                }

                if (ModelState.ContainsKey("PhoneNumber"))
                {
                    ModelState["PhoneNumber"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                    ModelState["PhoneNumber"].Errors.Clear();
                }

                if (ModelState.ContainsKey("NotificationEMail"))
                {
                    ModelState["NotificationEMail"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                    ModelState["NotificationEMail"].Errors.Clear();
                }

                if (ModelState.ContainsKey("LowBalanceNotification"))
                {
                    ModelState["LowBalanceNotification"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                    ModelState["LowBalanceNotification"].Errors.Clear();
                }

                if (ModelState.ContainsKey("OccupancyDate"))
                {
                    ModelState["OccupancyDate"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                    ModelState["OccupancyDate"].Errors.Clear();
                }

                ModelState.Root.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            }

            #endregion

            #region Save Changes

            #region User Profile

            //if (model.OperationalEmail != currentUser.Email)
            //{
            //    var existing = (from p in db.Users
            //                    where p.Email == model.OperationalEmail
            //                    && p.Id != currentUser.Id
            //                    select p).SingleOrDefault();

            //    if (existing != null)
            //    {
            //        ModelState.AddModelError("OperationalEmail", "Email already used for registration. Please use another.");
            //        hasErrors = true;
            //    }
            //    else
            //    {
            //        var token = await _userManager.GenerateChangeEmailTokenAsync(currentUser, model.OperationalEmail);
            //        var changeEmailResult = await _userManager.ChangeEmailAsync(currentUser, model.OperationalEmail, token);
            //        if (!changeEmailResult.Succeeded)
            //        {
            //            foreach (var error in changeEmailResult.Errors)
            //                ModelState.AddModelError("OperationalEmail", error.Description);
            //            hasErrors = true;
            //        }
            //    }
            //}

            //if (model.OperationalPhoneNumber != currentUser.PhoneNumber)
            //{
            //    var token = await _userManager.GenerateChangePhoneNumberTokenAsync(currentUser, model.OperationalPhoneNumber);
            //    var changePhoneNumber = await _userManager.ChangePhoneNumberAsync(currentUser, model.OperationalPhoneNumber, token);
            //    if (!changePhoneNumber.Succeeded)
            //    {
            //        foreach (var error in changePhoneNumber.Errors)
            //            ModelState.AddModelError("OperationalPhoneNumber", error.Description);
            //        hasErrors = true;
            //    }
            //}

            #endregion

            #region Opretational Profile

            var liveOpProfile = db.OperationalProfiles.Where(p => p.UserID == currentUser.Id).SingleOrDefault();

            //if (liveOpProfile != null)
            //{
            //    if (liveOpProfile.FirstName != model.OperationalFirstName)
            //        liveOpProfile.FirstName = model.OperationalFirstName;

            //    if (liveOpProfile.LastName != model.OperationalLastName)
            //        liveOpProfile.LastName = model.OperationalLastName;

            //    if (liveOpProfile.JobTitle != model.OperationalJobRole)
            //        liveOpProfile.JobTitle = model.OperationalJobRole;

            //    db.Update(liveOpProfile);
            //    db.SaveChanges();

                model.OperationalProfile = liveOpProfile;
            //}

            #endregion

            #region Customer Profile

            if (customer != null && _operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.Edit))
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).FirstOrDefault();
                if (ModelState.ErrorCount == 0)
                {
                    AccountTypeEnum accountType = (AccountTypeEnum)Convert.ToInt32(Request.Form["AccountType"]);
                    if (accountType != sC.AccountType)
                    {
                        ModelState.AddModelError("AccountType", "Account Type mismatch.");
                        hasErrors = true;
                    }
                    else
                    {
                        customer.AccountTypeID = (int)accountType;
                    }
                }


                if (ModelState.ErrorCount == 0)
                {
                    customer.OccupancyDate = model.OccupancyDate;
                    customer.FullName = model.FullName;
                    customer.NotificationPhoneNumber = model.PhoneNumber;
                    customer.PhoneNumber = model.PhoneNumber;
                    customer.AltPhoneNumber = model.AltPhoneNumber;
                    customer.NotificationEmail = model.NotificationEMail;
                    customer.BalanceNotificationSMSType = Request.Form["BalanceNotificationSMSType"];

                    customer.ActivateTaxInvoice = Convert.ToBoolean(Request.Form["ActivateTaxInvoice"]);
                    customer.ShowCustomBankingDetails = Convert.ToBoolean(Request.Form["ShowCustomBankingDetails"]);
                    customer.RecipientName = model.RecipientName;
                    customer.RecipientAddress = model.RecipientAddress;
                    customer.RecipientVATNumber = model.RecipientVATNumber;
                    customer.RecipientReferenceNumber = model.RecipientReferenceNumber;
                    customer.SystemNotificationTypeID = Convert.ToInt32(Request.Form["SystemNotificationTypeID"]);
                    customer.InfoNotificationTypeID = Convert.ToInt32(Request.Form["InfoNotificationTypeID"]);

                    db.Update(customer);
                    db.SaveChanges();
                }

                #region Notification Settings

                if (ModelState.ErrorCount == 0)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                    if (notificationSettings == null)
                    {
                        notificationSettings = new NotificationCustomerMeter()
                        {
                            AccountType = customer.AccountTypeID,
                            AutoDisconnect = true,
                            CustomerID = customer.CustomerID,
                            DisconnectNotification = true,
                            LastUpdated = DateTime.Now,
                            LowBalanceNotification1 = Convert.ToBoolean(Request.Form["LowBalanceNotifications"]),
                            LowBalanceNotification2 = Convert.ToBoolean(Request.Form["LowBalanceNotifications"]),
                            MeterSerial = customer.MeterNumber,
                            Reading = "",
                            SwitchBackToAutoDate = null,
                        };
                        db.Add(notificationSettings);
                    }
                    else
                    {
                        notificationSettings.LowBalanceNotification1 = Convert.ToBoolean(Request.Form["LowBalanceNotifications"]);
                        db.Update(notificationSettings);
                    }

                    db.SaveChanges();

                    customer.LeakNotifications = Convert.ToBoolean(Request.Form["LeakNotifications"]);
                    customer.HighUsageNotifications = Convert.ToBoolean(Request.Form["HighUsageNotifications"]);
                    customer.NewsLetters = Convert.ToBoolean(Request.Form["NewsLetters"]);
                    customer.DisconnectionLowBalanceNotification1 = Convert.ToInt32(Request.Form["LowBalanceNotification"]);
                    db.SaveChanges();
                }

                #endregion

                #region Usage Settings

                if (ModelState.ErrorCount == 0)
                {
                    customer.ShowDailyUsage = Convert.ToBoolean(Request.Form["ShowHourlyUsage"]);
                    customer.ShowCostInclVAT = Convert.ToBoolean(Request.Form["ShowCostInclVAT"]);
                }

                #endregion

                #region Customer Meters

                if (ModelState.ErrorCount == 0)
                {
                    var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                    var localDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID == customer.CompanyID).ToList();

                    var apiClient = new SkyBillApiClient(company.Name, _cache);
                    var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                    model.CustomerMeterTypes = new List<Customer_ProfileModel.ProfileCustomerMeterType>();

                    foreach (var meter in meters)
                    {
                        var localDev = localDevices.Where(p => p.Serial == meter.Serial_No).FirstOrDefault();
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No, localDev != null ? localDev.DeviceAPIIDValue : 1);
                        if (device != null)
                        {
                            var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();
                            if (customerMeter != null)
                            {
                                var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                                if (customerMeterType != null && customerMeterType.Selected != Convert.ToInt32(Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"]))
                                {
                                    customerMeterType.Selected = Convert.ToInt32(Request.Form[$"CustomerMeterTypes_{meter.Serial_No}"]);
                                    db.Update(customerMeterType);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }

                }

                #endregion


                if (ModelState.ErrorCount == 0)
                {
                    db.Update(customer);
                    db.SaveChanges();
                }
            }

            #endregion

            #endregion

            #region Repopulate Model

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).FirstOrDefault();
                model.SkybillCustomer = sC;
                customer = db.Customers.Where(tbl => tbl.CustomerNumber == _operationalProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
            }
            else
                customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();

            model.SelectedCustomer = customer;

            if (customer != null && customer.UserID == _operationalProvider.OperationalProfile.UserID)
                model.IsCurrentUserCustomer = true;

            #endregion

            if (_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.Edit))
                model.AllowCustomerEdit = true;

            if (model.IsCurrentUserCustomer)
                model.AllowCustomerEdit = true;

            if (ModelState.IsValid)
                model.IsSuccessfull = true;
            else
            {

                StringBuilder sbError = new StringBuilder();
                sbError.AppendLine($"{ModelState.IsValid}:{ModelState.ErrorCount}");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var err in ModelState[key].Errors)
                    {
                        sbError.AppendLine($"{key}:{err.ErrorMessage}");
                    }
                }

                foreach (var modelValue in ModelState.Values)
                {
                    foreach (var err in modelValue.Errors)
                    {
                        sbError.AppendLine($"{modelValue.RawValue}:{err.ErrorMessage}");
                    }
                }


                Console.WriteLine("=================================");
                Console.WriteLine(sbError.ToString());
                Console.WriteLine("=================================");
            }

            return View("~/Views/Operational/Customer/Profile/Profile.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Customer/Customer_Profile_Delete/{userID}")]
        public async Task<IActionResult> Customer_Profile_Delete(string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.Delete))
                return Content("false");

            #endregion

            string deserializedUserID = userID;//Newtonsoft.Json.JsonConvert.DeserializeObject(userID).ToString();

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
                return Content("true");
            }
        }

        [HttpPost]
        [Route("/operational/Customer/Customer_Profile_Activate/{userID}")]
        public async Task<IActionResult> Customer_Profile_Activate(string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Profile, SecureAreaActionEnum.Approval))
                return Content("false");

            #endregion

            string deserializedUserID = userID;//Newtonsoft.Json.JsonConvert.DeserializeObject(userID).ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == deserializedUserID && tbl.IsDeleted == false).FirstOrDefault();
                if (user != null && !user.IsDeleted)
                {
                    user.IsConfirmed = true;
                }

                var result = await db.SaveChangesAsync();

                return Content("true");
            }
        }



    }
}
