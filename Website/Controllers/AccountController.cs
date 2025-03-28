using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MyVoltage.Models;
using MyVoltage.Models.AccountViewModels;
using MyVoltage.Services;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
using MyVoltage.Api.MyVoltage;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Factories;
using Microsoft.AspNetCore.Cors;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using MyVoltage.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Web;
using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;

namespace MyVoltage.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageLogDbContext> _Logoptions;
        private readonly IHttpContextAccessor _context;
        private readonly string _regEmail;
        private readonly string _devEmail;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly CustomerProvider _customerProvider;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageLogDbContext> Logoptions,
            IHttpContextAccessor context,
            IMemoryCache cache,
            CustomerProvider customerProvider,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _options = options;
            _context = context;
            _cache = cache;
            _regEmail = config["RegEmail:Email"];
            _devEmail = config["DevEmail:Email"];
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _customerProvider = customerProvider;
            _Logoptions = Logoptions;
            _config = config;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            if (IsValidEmail(returnUrl))
            {
                ViewData["Email"] = returnUrl;
            }
            else
            {
                ViewData["ReturnUrl"] = returnUrl;
            }
            ViewData["LogoutInfo"] = HttpContext.Session.GetString("LogoutInfo");
            HttpContext.Session.Remove("LogoutInfo");
            ViewData["Powered-by"] = "";

            string Url = _context.HttpContext.Request.Host.ToString();

            if (!_context.HttpContext.Request.IsHttps && !_context.HttpContext.Request.Host.ToString().ToUpper().Contains("localhost".ToUpper()))
                return Redirect($"https://{_context.HttpContext.Request.Host}{_context.HttpContext.Request.Path}{_context.HttpContext.Request.QueryString}");


            using (var db = new MyVoltageDbContext(_options))
            {
                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).FirstOrDefault();

                if (companySkin != null)
                {
                    ViewData["Powered-by"] = "powered-by";
                }

                #region Login Messages

                ViewData["LoginMessage1"] = HttpUtility.HtmlDecode(_config["LoginMessages:Message1"]);
                ViewData["LoginMessage2"] = HttpUtility.HtmlDecode(_config["LoginMessages:Message2"]);
                ViewData["LoginMessage3"] = HttpUtility.HtmlDecode(_config["LoginMessages:Message3"]);

                var latestLoginMessage = (from p in db.SiteAdmin_LoginMessages
                                          orderby p.DateCreated descending
                                          select p).FirstOrDefault();

                if (latestLoginMessage != null)
                {
                    ViewData["LoginMessage1"] = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage1);
                    ViewData["LoginMessage2"] = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage2);
                    ViewData["LoginMessage3"] = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage3);
                }

                #endregion

            }

            if (_userManager.GetUserAsync(User).Result != null)
                return Redirect("/");

            Response.Cookies.Delete("userpassword");
            Response.Cookies.Delete("useremail");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var db = new MyVoltageDbContext(_options);
                var dbuser = db.Users.Where(tbl => tbl.Email == model.Email && tbl.IsDeleted == false).FirstOrDefault();
                if (dbuser == null) // If user is deleted
                {
                    ModelState.AddModelError("", "User does not exist.");
                    return View(model);
                }
                else if (dbuser != null && dbuser.IsConfirmed == false) // If user is not deleted but also hasn't completed the registration process
                {
                    //ViewData["invalidlogin"] = "Invalid Login";
                    //ModelState.AddModelError("", "User does not exist");
                    //return View(model);
                    return RedirectToAction(nameof(OTPConfirmNew), new { id = dbuser.Id }); // Verify user via OTP to complete registration process
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    System.Threading.Thread thread = new System.Threading.Thread(() => MyVoltage.Data.MyVoltageLog.LoggedInLogger.LogUserLoggedIn(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), new MyVoltageDbContext(_options)));
                    thread.Start();

                    var roles = await _userManager.GetRolesAsync(user);

                    bool admin = false;
                    bool companyAdmin = false;
                    bool technician = false;
                    bool operational = false;
                    bool leadUser = false;

                    foreach (var roleName in roles)
                    {
                        if (roleName == "Admin")
                        {
                            admin = true;
                        }

                        if (roleName == "CompanyAdmin")
                        {
                            companyAdmin = true;
                        }

                        if (roleName == UserRoleEnum.Technician.ToString())
                        {
                            technician = true;
                        }
                        if (roleName == UserRoleEnum.Operational.ToString())
                        {
                            operational = true;
                        }
                        if (roleName == UserRoleEnum.Leaduser.ToString())
                        {
                            leadUser = true;
                        }
                    }
                    HttpContext.Session.SetString("logininfo", "Login successfully.");
                    HttpContext.Session.Remove(CustomerProvider.SESSION_CUSTOMER_ID);
                    HttpContext.Session.Remove(CustomerProvider.SESSION_COMPANY_NAME);
                    HttpContext.Session.Remove(CustomerProvider.METER_NUMBER);

                    HttpContext.Session.Remove(OperationalProvider.SESSION_COMPANY_ID);
                    HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL);
                    HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_NUMBER);
                    HttpContext.Session.Remove(OperationalProvider.SESSION_NAVIGATION_HISTORY);

                    if (companyAdmin)
                    {
                        return RedirectToLocal("/companyadmin/welcomepage");
                    }
                    else if (admin)
                    {
                        return RedirectToLocal("/admindashboard");
                    }
                    else if (technician)
                    {
                        return RedirectToLocal("/technician/dashboard");
                    }
                    else if (operational)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["R"]))
                            return Redirect(Request.Query["R"]);
                        return RedirectToLocal("/operational/dashboard");
                    }
                    else if (leadUser)
                    {
                        return RedirectToLocal("/leaduser");
                    }
                    else
                    {
                        string Url = _context.HttpContext.Request.Host.ToString();
                        var customer = db.Customers.Where(p => p.UserID == user.Id).FirstOrDefault();
                        if (customer != null)
                        {
                            var companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == customer.CompanyID).FirstOrDefault();
                            //if (companySkin != null && Url != companySkin.Url)
                            //{
                            //    return Redirect($"https://{companySkin.Url}/");
                            //    //return RedirectToAction("Dashboard", "Clientzone");
                            //}

                            if (!Url.Contains("localhost") && companySkin != null && !Url.Contains(companySkin.Url) 
                                && !(companySkin.Url.Contains("clientzone.myvoltage.co.za") && Url.Contains("dev.myvoltage.co.za")))
                            {
                                _signInManager.SignOutAsync();
                                return Redirect($"https://{companySkin.Url}/");
                                //return RedirectToAction("Dashboard", "Clientzone");
                            }
                        }
                        if (!String.IsNullOrEmpty(returnUrl))
                            return RedirectToLocal(returnUrl);
                        //else if (user.Email.ToLower().Contains("lendl@myvoltage.co.za") || user.Email.ToLower().Contains("zprins1@gmail.com"))
                        //    return RedirectToLocal("/clientzone");
                        else
                            return RedirectToLocal("/clientzone");
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterMeterNumber(string returnUrl = null)
        {
            return Redirect("/RegisterNew");
            ViewData["ReturnUrl"] = returnUrl;

            return View(PopulateRegisterViewModel(new RegisterViewModel()));
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/register/{meterNumber}")]
        public IActionResult Register(string meterNumber, string returnUrl = null)
        {
            return Redirect("/RegisterNew");
            ViewData["ReturnUrl"] = returnUrl;

            return View(PopulateRegisterViewModel(new RegisterViewModel()));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirm(string id)
        {
            return View(PopulateRegisterConfirmViewModel(id));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterConfirm(RegisterConfirmViewModel model, string id, string returnUrl = null)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var registeredUser = db.Users.Where(tbl => tbl.Email == model.Email || tbl.NormalizedEmail == model.Email && tbl.IsDeleted == false).FirstOrDefault();
                if (registeredUser != null)
                {
                    if (registeredUser.Id != id)
                    {
                        ModelState.AddModelError("Email", "Email already in use");
                        var newModel = PopulateRegisterConfirmViewModel(id);
                        newModel.Email = model.Email;
                        return View(newModel);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    customer.OccupancyDate = new DateTime(model.Year, model.Month, model.Day);
                    customer.PhoneNumber = model.PhoneNumber;

                    user.Email = model.Email;
                    user.NormalizedEmail = model.Email.ToUpper();
                    user.UserName = model.Email;
                    user.NormalizedUserName = model.Email.ToUpper();
                    user.PhoneNumber = model.PhoneNumber;

                    Random random = new Random();

                    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
                    var stringChars = new char[6];

                    for (int i = 0; i < stringChars.Length; i++)
                    {
                        stringChars[i] = chars[random.Next(chars.Length)];
                    }

                    var emailCode = new String(stringChars);

                    var numbers = "1234567890";
                    var stringNumbers = new char[4];

                    for (int i = 0; i < stringNumbers.Length; i++)
                    {
                        stringNumbers[i] = numbers[random.Next(numbers.Length)];
                    }

                    var OTPCode = new string(stringNumbers);

                    customer.OTPCode = OTPCode;
                    customer.EmailCode = emailCode;

                    await _emailSender.SendEmailCodeAsync(model.Email, emailCode, customer.FullName, id, _context);

                    SMS.SendSms("27" + model.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + OTPCode + ". Enter this code during registration to verify your contact details.");

                    db.SaveChanges();

                    return RedirectToLocal("~/Account/OTPConfirm?id=" + id);
                }
            }

            return View(PopulateRegisterConfirmViewModel(id));
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult OTPConfirm(string id)
        {
            ViewData["id"] = id;
            return View(PopulateOTPConfirmViewModel(id));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OTPConfirm(OTPConfirmViewModel model, string id, string returnUrl = null)
        {
            //if (ModelState.IsValid)
            //{
            //    using (var db = new MyVoltageDbContext(_options))
            //    {
            //        var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

            //        var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

            //        var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

            //        var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();

            //        if (customer.OTPCode == model.OTP || customer.EmailCode == model.EmailCode)
            //        {
            //            user.IsConfirmed = true;

            //            db.SaveChanges();

            //            String email = "New user<br/> Email: " + user.Email + "<br/>" +
            //                            "Meter Number: " + customer.MeterNumber + "<br/>" +
            //                            " Phone Number: " + user.PhoneNumber + "<br/>" +
            //                            " Email Code: " + customer.EmailCode + "<br/>" +
            //                            " OTP: " + customer.OTPCode + "<br/>" +
            //                            " Full Name: " + customer.FullName + "<br/>" +
            //                            " Company: " + company.Name + "<br/>" +
            //                        " Customer Number: " + customer.CustomerNumber + "<br/>" +
            //                        " Alternative Phone Number: " + customer.AltPhoneNumber + "<br/>" +
            //                        " ID Number Or Company Registration: " + customer.IDNumberOrCompanyReg + "<br/>" +
            //                        " Unit Number: " + customer.UnitNumber + "<br/>" +
            //                        //                                       " Complex Name: " + customer.ComplexName + "<br/>" +
            //                        " Street Address: " + customer.StreetAddress + "<br/>" +
            //                        " Suburb: " + customer.Suburb + "<br/>" +
            //                        " Town Or City: " + customer.TownOrCity + "<br/>" +
            //                        " Province: " + customer.Province + "<br/>" +
            //                        " Postal Code: " + customer.PostalCode + "<br/>" +
            //                        " Account Type: " + accountType.Name + "<br/>" +
            //                        " Occupancy Date: " + customer.OccupancyDate.ToLongDateString();

            //            await _emailSender.SendUserDetailsAsync(_regEmail, email, customer.CustomerNumber, user.Email);

            //            NotificationCustomerMeter notification = db.NotificationCustomerMeters.Where(tbl => tbl.MeterSerial == customer.MeterNumber && tbl.CustomerID == 0).FirstOrDefault();
            //            if (notification != null)
            //            {
            //                notification.LastUpdated = DateTime.Now;
            //                notification.AccountType = accountType.AccountTypeID;
            //                notification.CustomerID = customer.CustomerID;

            //                db.Update(notification);
            //                db.SaveChanges();
            //            }

            //            if (accountType.AccountTypeID == (int)AccountTypeEnum.PostPaid)
            //            {
            //                return RedirectToLocal("~/Account/RegistrationSuccess?id=" + id);
            //            }

            //            return RedirectToLocal("~/Account/NotificationsSettings?id=" + id);
            //        }
            //        else
            //        {
            //            ModelState.AddModelError("OTP", "You have entered wrong code(s)");
            //        }
            //    }
            //}
            ViewData["id"] = id;
            return View(PopulateOTPConfirmViewModel(id));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult NotificationsSettings(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                ViewData["id"] = id;
                ViewData["accountType"] = customer.AccountTypeID;
                return View(PopulatNotificationSettingsViewModel(id, false));
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotificationsSettings(NotificationSettingsViewModel model, string id, string returnUrl = null)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                if (ModelState.IsValid)
                {
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

                    customer.NotificationEmail = user.Email;
                    customer.NotificationPhoneNumber = customer.PhoneNumber;
                    customer.DisconnectionLowBalanceNotification1 = Int32.Parse(model.DisconnectionLowBalanceNotification1);
                    customer.HighUsageNotifications = model.HighUsageNotifications.Equals("1") ? true : false;
                    customer.LeakNotifications = model.LeakNotifications.Equals("1") ? true : false;
                    customer.NewsLetters = model.NewsLetters.Equals("1") ? true : false;
                    db.SaveChanges();
                    return RedirectToLocal("~/Account/RegistrationSuccess?id=" + id);
                }

                ViewData["id"] = id;
                ViewData["accountType"] = customer.AccountTypeID;
                return View(PopulatNotificationSettingsViewModel(id, false));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Contact(string sent)
        {
            if (sent != null)
            {
                ViewData["Sent"] = true;
            }

            ContactViewModel model = new ContactViewModel()
            {
                CustomerElectricityMeters = new List<ContactViewModel.CustomerElectricityMeter>()
            };
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = _userManager.GetUserAsync(User).Result;
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                if (User.IsInRole("CompanyAdmin"))
                {
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
                }

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var skybillCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);
                var customerMeters = skyBillApiClient.GetMetersByCustomer(_customerProvider.CustomerNumber);

                if (skybillCustomer != null)
                {
                    foreach (var meter in customerMeters)
                    {
                        var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                        if (m2mDevice == null || !m2mDevice.deviceType.ToUpper().Contains("ELEC"))
                            continue;

                        var isContactorConnected = _client.IsDeviceContactorConnected(m2mDevice.id);
                        float balance = skybillCustomer.Balance_LCY * -1;
                        bool showEmergencyConnectButton = false;
                        bool showSwitchingButton = false;

                        if (balance >= -50 && !isContactorConnected)
                            showEmergencyConnectButton = true;

                        if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                        {
                            if (isContactorConnected)
                                showSwitchingButton = true;
                        }
                        else if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("PREPAID"))
                        {

                        }

                        model.CustomerElectricityMeters.Add(new ContactViewModel.CustomerElectricityMeter()
                        {
                            Serial = meter.Serial_No,
                            ShowEmergencyConnectButton = showEmergencyConnectButton,
                            ShowSwitchingButton = showSwitchingButton
                        });
                    }
                }

                if (customer != null)
                {
                    model.Email = customer.NotificationEmail;
                    model.Name = customer.FullName;
                    model.PhoneNumber = customer.PhoneNumber;

                    var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).FirstOrDefault();

                }
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var user = _userManager.GetUserAsync(User).Result;


                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                    var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();

                    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                    var skybillCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);
                    var customerMeters = skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber);

                    model.CustomerElectricityMeters = new List<ContactViewModel.CustomerElectricityMeter>();

                    if (skybillCustomer != null)
                    {
                        foreach (var meter in customerMeters)
                        {
                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                            if (m2mDevice == null || !m2mDevice.deviceType.ToUpper().Contains("ELEC"))
                                continue;

                            var isContactorConnected = _client.IsDeviceContactorConnected(m2mDevice.id);
                            float balance = skybillCustomer.Balance_LCY * -1;
                            bool showEmergencyConnectButton = false;
                            bool showSwitchingButton = false;

                            if (balance >= -50 && !isContactorConnected)
                                showEmergencyConnectButton = true;

                            if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                            {
                                if (isContactorConnected)
                                    showSwitchingButton = true;
                            }
                            else if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("PREPAID"))
                            {

                            }

                            model.CustomerElectricityMeters.Add(new ContactViewModel.CustomerElectricityMeter()
                            {
                                Serial = meter.Serial_No,
                                ShowEmergencyConnectButton = showEmergencyConnectButton,
                                ShowSwitchingButton = showSwitchingButton
                            });
                        }
                    }

                    string meterStr = "";

                    foreach (SkyBillCustomer c in customerMeters)
                    {
                        meterStr = meterStr + c.Serial_No + "<br/>";
                    }

                    String email = "User<br/> Email: " + user.Email + "<br/>" +
                                  "Contact Email: " + model.Email + "<br/>" +
                                  " Contact Name: " + model.Name + "<br/>" +
                                  " Contact Phone Number: " + model.PhoneNumber + "<br/>" +
                                  " Customer Number: " + customer.CustomerNumber + "<br/>" +
                                  " Account Type: " + accountType.Name + "<br/>" +
                                  " Full Name: " + customer.FullName + "<br/>" +
                                  " ID Number Or Company Registration: " + customer.IDNumberOrCompanyReg + "<br/>" +
                                  " Meter Number(s): " + meterStr + "<br/>" +
                                  " Registration phone number: " + customer.PhoneNumber + "<br/>" +
                                  " Notification phone number: " + customer.NotificationPhoneNumber + "<br/>" +
                                  " Service Provider: " + company.Name + "<br/>" +
                                  " Street Address: " + customer.StreetAddress + "<br/>" +
                                  " Suburb: " + customer.Suburb + "<br/>" +
                                  " Town Or City: " + customer.TownOrCity + "<br/>" +
                                  " Province: " + customer.Province + "<br/>" +
                                  " Postal Code: " + customer.PostalCode + "<br/>" +
                                  " Occupancy Date: " + customer.OccupancyDate.ToLongDateString() + "<br/>" +
                                  " Message: " + model.Message + "<br/>";


                    string contentType = "";
                    byte[] fileContents = null;
                    string filename = "";
                    if (model.file != null)
                    {
                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.file.CopyTo(uploadFile);
                        fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                        if (!provider.TryGetContentType(model.file.FileName, out contentType))
                        {
                            contentType = "application/octet-stream";
                        }
                        filename = Path.GetFileName(model.file.FileName);
                    }

                    await _emailSender.SendContactEmailAsync(_devEmail, email, customer.CustomerNumber, model.Email, fileContents, filename, contentType);
                    return RedirectToLocal("~/Account/Contact?sent=true");
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult InvalidMeterContact(string sent)
        {
            if (sent != null)
            {
                ViewData["Sent"] = true;
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InvalidMeterContact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {

                using (var db = new MyVoltageDbContext(_options))
                {
                    var user = _userManager.GetUserAsync(User).Result;

                    String email = "Contact Email: " + model.Email + "<br/>" +
                                  " Contact Name: " + model.Name + "<br/>" +
                                  " Contact Phone Number: " + model.PhoneNumber + "<br/>" +
                                  " Message: " + model.Message + "<br/>";

                    await _emailSender.SendContactEmailAsync(_devEmail, email, "", email, null, "", "");
                    TempData["EmailSent"] = true;
                    return RedirectToLocal("~/Account/InvalidMeterContact?sent=true");
                }
            }
            return View(model);
        }

        [HttpGet("/notification/edit")]
        public IActionResult EditNotificationsSettings(string id)
        {
            var user = _userManager.GetUserAsync(User).Result;
            ViewData["id"] = user.Id;
            ViewData["edit"] = true;
            return View(PopulatNotificationSettingsViewModel(user.Id, true));
        }

        [HttpPost("/notification/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotificationsSettings(NotificationSettingsViewModel model)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (ModelState.IsValid)
            {
                using (var db = new MyVoltageDbContext(_options))
                {

                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

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

                    customer.NotificationEmail = user.Email;
                    customer.NotificationPhoneNumber = customer.PhoneNumber;
                    customer.DisconnectionLowBalanceNotification1 = Int32.Parse(model.DisconnectionLowBalanceNotification1);
                    customer.HighUsageNotifications = model.HighUsageNotifications.Equals("1") ? true : false;
                    customer.LeakNotifications = model.LeakNotifications.Equals("1") ? true : false;
                    customer.NewsLetters = model.NewsLetters.Equals("1") ? true : false;
                    db.SaveChanges();
                    return RedirectToLocal("~/profile");
                }
            }
            ViewData["id"] = user.Id;
            ViewData["edit"] = true;
            return View(PopulatNotificationSettingsViewModel(user.Id, true));
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrationSuccess(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OTPResend(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                await _emailSender.SendEmailCodeAsync(user.Email, customer.EmailCode, customer.FullName, id, _context);

                SMS.SendSms("27" + customer.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + customer.OTPCode + ". Enter this code during registration to verify your contact details.");
                ViewData["id"] = id;
                return View("OTPConfirm", PopulateOTPConfirmViewModel(id));
            }
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            using (var db = new MyVoltageDbContext(_options))
            {
                var registeredUser = db.Users.Where(tbl => tbl.Email == model.Email && tbl.IsDeleted == false || tbl.NormalizedEmail == model.Email.ToUpper() && tbl.IsDeleted == false).FirstOrDefault();
                if (registeredUser != null)
                {
                    // ModelState.AddModelError("Email", "Email already in use");
                    return RedirectToLocal("~/account/login?returnUrl=" + model.Email);
                }
            }

            if (ModelState.IsValid)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var companies = db.Companies.ToList();

                    var client = new SkyBillApiClient(String.Empty, _cache);

                    string customerNo = "";
                    string companyName = "";
                    int companyId = 0;

                    var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == model.MeterNumber).FirstOrDefault();

                    if (localSkybillCustomer != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).FirstOrDefault();
                        companyName = company.Name;
                        customerNo = localSkybillCustomer.Customer_No;
                        companyId = company.CompanyID;
                    }
                    else
                    {
                        var customer = client.GetCustomerByMeterNumberInAllCompanies(model.MeterNumber, companies);

                        if (customer == null)
                        {
                            ModelState.AddModelError("MeterNumber", "No customer exists for this meter number.");
                            return View(PopulateRegisterViewModel(model));
                        }

                        customerNo = customer.Customer_No;
                        companyName = customer.company.Name;
                        companyId = customer.company.CompanyID;
                    }

                    var customerDetails = client.GetCustomerDetailsByCustomerNo(customerNo, companyName);

                    if (customerDetails == null)
                    {
                        ModelState.AddModelError("MeterNumber", "No customer details for customer with this meter number.");
                        return View(PopulateRegisterViewModel(model));
                    }

                    var registeredCompanyOrID = db.Customers.Where(tbl => tbl.IDNumberOrCompanyReg == model.IDNumberOrCompanyReg && tbl.IsDeleted == false).FirstOrDefault();
                    if (registeredCompanyOrID != null)
                    {
                        ModelState.AddModelError("IDNumberOrCompanyReg", "ID Number / Company Registration Number already in use.");
                        return View(PopulateRegisterViewModel(model));
                    }

                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    var accountTypeID = 0;
                    if (string.Compare(customerDetails.Billing_Cycle, "POSTPAID", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.POSTPAID;
                    }
                    else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.POSTPAID.ToString())
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.POSTPAID;
                    }
                    else if (string.Compare(customerDetails.Billing_Cycle, "PREPAID", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.PREPAID;
                    }
                    else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.PREPAID.ToString())
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.PREPAID;
                    }
                    else if (string.Compare(customerDetails.Billing_Cycle, "WALLET", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                    }
                    else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.WALLET.ToString())
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                    }
                    else
                    {
                        accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                    }

                    if (result.Succeeded)
                    {
                        db.Customers.Add(new Data.Customer
                        {
                            CustomerNumber = customerNo,
                            AltPhoneNumber = model.PhoneNumber,
                            IDNumberOrCompanyReg = model.IDNumberOrCompanyReg,
                            //UnitNumber = model.UnitNumber,
                            //                            ComplexName = model.ComplexName,
                            StreetAddress = model.StreetAddress,
                            Suburb = model.Suburb,
                            TownOrCity = model.TownOrCity,
                            Province = model.Province,
                            PostalCode = Int32.Parse(model.PostalCode),
                            FullName = model.FirstName,
                            PhoneNumber = model.PhoneNumber,
                            UserID = user.Id,
                            CompanyID = companyId,
                            MeterNumber = model.MeterNumber,
                            AccountTypeID = accountTypeID,
                            IsDeleted = false
                        });

                        db.SaveChanges();

                        return RedirectToLocal("~/account/registerconfirm?id=" + user.Id);
                    }

                    AddErrors(result);

                }
            }

            // If we got this far, something failed, redisplay form
            return View(PopulateRegisterViewModel(model));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterMeterNumber(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (model.MeterNumber != null)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var isExisting = db.Customers.Where(tbl => tbl.MeterNumber == model.MeterNumber && tbl.IsDeleted == false).Any();

                    if (isExisting)
                    {
                        TempData["MeterExist"] = true;
                        ModelState.AddModelError("MeterNumber", "Invalid meter number.");
                        return View(PopulateRegisterViewModel(model));
                    }
                    else
                    {
                        return RedirectToLocal("/register/" + model.MeterNumber);
                    }

                }
            }

            // If we got this far, something failed, redisplay form
            return View(PopulateRegisterViewModel(model));
        }

        private RegisterViewModel PopulateRegisterViewModel(RegisterViewModel model)
        {
            using (var db = new MyVoltageDbContext(_options))
            {

                string Url = _context.HttpContext.Request.Host.ToString();
                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).SingleOrDefault();
                model.Companies = db.Companies.Where(a => a.Registrable == true).ToList();
                if (companySkin != null)
                {
                    var company = db.CompanySkins.Where(tbl => tbl.Url == Url).SingleOrDefault();
                    if (company != null)
                    {
                        if (company.CompanyID != 0)
                        {
                            model.Companies = db.Companies.Where(a => a.CompanyID == company.CompanyID).ToList();
                        }
                    }
                }

                model.CompanyOrIndividualList = new List<SelectListItem>{
                                                new SelectListItem { Selected = false, Text = "Individual", Value = "2"},
                                                new SelectListItem { Selected = false, Text = "Company", Value = "1"},
                                                };

                model.AccountTypes = db.AccountTypes.ToList();
            }

            return model;
        }

        private RegisterConfirmViewModel PopulateRegisterConfirmViewModel(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                TimeSpan ts = DateTime.Now - new DateTime(2017, DateTime.Now.Month, DateTime.Now.Day);

                int years = Convert.ToInt32(ts.TotalDays / 365.25);

                return new RegisterConfirmViewModel
                {
                    FullName = customer.FullName,
                    Email = user.Email,
                    UserId = user.Id,
                    PhoneNumber = customer.PhoneNumber,
                    Days = new SelectList(Enumerable.Range(1, 31).Select(itm => new { Text = itm, Value = itm }), "Value", "Text"),
                    Months = new SelectList(Enumerable.Range(1, 12).Select(itm => new { Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(itm), Value = itm }), "Value", "Text"),
                    Years = new SelectList(Enumerable.Range(DateTime.Now.Year - years, years + 1).Select(itm => new { Text = itm, Value = itm }), "Value", "Text")
                };
            }
        }

        private NotificationSettingsViewModel PopulatNotificationSettingsViewModel(string id, Boolean edit)
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
                        HighUsageNotifications = customer.HighUsageNotifications ? "1" : "0",
                        LeakNotifications = customer.LeakNotifications ? "1" : "0",
                        NewsLetters = customer.NewsLetters ? "1" : "0",
                        YesNoSelectListItems = new List<SelectListItem>{
                                                new SelectListItem { Selected = false, Text = "No", Value = "0"},
                                                new SelectListItem { Selected = false, Text = "Yes", Value = "1"},
                                                }
                    };
                }

                return new NotificationSettingsViewModel
                {
                    NotificationEmail = user.Email,
                    NotificationPhoneNumber = customer.PhoneNumber,
                    YesNoSelectListItems = new List<SelectListItem>{
                                                new SelectListItem { Selected = false, Text = "No", Value = "0"},
                                                new SelectListItem { Selected = false, Text = "Yes", Value = "1"},
                                                }
                };
            }
        }

        private OTPConfirmViewModel PopulateOTPConfirmViewModel(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                return new OTPConfirmViewModel
                {
                    Email = user.Email,
                    UserId = user.Id,
                    PhoneNumber = customer.PhoneNumber,
                };
            }
        }

        [HttpGet("/Profile")]
        public async Task<IActionResult> Profile()
        {
            var currentUser = _userManager.GetUserAsync(User).Result;

            ProfileViewModel model = new ProfileViewModel();
            using (var db = new MyVoltageDbContext(_options))
            {
                Data.Customer customer = null;
                if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
                {
                    var sC = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
                    model.SkybillCustomer = sC;
                    customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
                }
                else
                    customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();


                model.SelectedCustomer = customer;

                if (customer != null)
                {
                    if (customer.UserID == currentUser.Id)
                        model.IsCurrentUserCustomer = true;

                    var user = _userManager.FindByIdAsync(customer.UserID).Result;

                    var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                    var apiClient = new SkyBillApiClient(company.Name, _cache);
                    var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                    model.CustomerMeterTypes = new List<ProfileViewModel.ProfileCustomerMeterType>();

                    foreach (var meter in meters)
                    {
                        var oldCustomer = db.Customers.Where(p => p.MeterNumber == meter.Serial_No && !p.IsDeleted).FirstOrDefault();
                        if (oldCustomer != null)
                        {
                            model.OldUserForMeter = oldCustomer.FullName;
                            model.OldUserIDForMeter = oldCustomer.UserID;
                        }
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
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
                                    new SelectListItem() { Value = "1", Text = $"Balance ({balanceSymbol})", Selected = customerMeterType.Selected == 1 ? true : false },
                                    new SelectListItem() { Value = "2", Text = "Demand", Selected = customerMeterType.Selected == 2 ? true : false },
                                    new SelectListItem() { Value = "3", Text = "None", Selected = customerMeterType.Selected == 3 ? true : false },
                                    new SelectListItem() { Value = "4", Text = "Solar", Selected = customerMeterType.Selected == 4 ? true : false },
                                };
                                    model.CustomerMeterTypes.Add(new ProfileViewModel.ProfileCustomerMeterType()
                                    {
                                        MeterSerial = meter.Serial_No,
                                        selectList = selectListItems
                                    });
                                }
                            }
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

                    model.RecipientName = customer.RecipientName;
                    model.RecipientAddress = customer.RecipientAddress;
                    model.RecipientVATNumber = customer.RecipientVATNumber;
                    model.RecipientReferenceNumber = customer.RecipientReferenceNumber;

                    if (string.IsNullOrEmpty(model.PhoneNumber) && !string.IsNullOrEmpty(customer.PhoneNumber))
                        model.PhoneNumber = customer.PhoneNumber;
                    if (string.IsNullOrEmpty(model.PhoneNumber) && !string.IsNullOrEmpty(customer.AltPhoneNumber))
                        model.PhoneNumber = customer.AltPhoneNumber;

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

                    model.LowBalanceNotification = customer.DisconnectionLowBalanceNotification1.HasValue ? customer.DisconnectionLowBalanceNotification1.Value : 0;

                    List<SelectListItem> balanceNotificationSMSType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "None", Text = $"None (Free)", Selected = string.IsNullOrEmpty(customer.BalanceNotificationSMSType) || customer.BalanceNotificationSMSType == "None" ? true : false },
                    new SelectListItem() { Value = "Daily", Text = $"Daily (R2.00 per sms)", Selected = customer.BalanceNotificationSMSType == "Daily" ? true : false },
                    new SelectListItem() { Value = "Weekly", Text = $"Weekly (Free)", Selected = customer.BalanceNotificationSMSType == "Weekly" ? true : false },
                };


                    model.BalanceNotificationSMSType = balanceNotificationSMSType;
                }

            }

            if (model.IsCurrentUserCustomer)
                model.AllowCustomerEdit = true;

            return View("~/Views/Account/Profile.cshtml", model);
        }

        [HttpPost("/Profile")]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
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
            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
                model.SkybillCustomer = sC;
                customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
            }
            else
                customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();

            model.SelectedCustomer = customer;

            if (customer != null)
            {
                var user = _userManager.FindByIdAsync(customer.UserID).Result;

                var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                var apiClient = new SkyBillApiClient(company.Name, _cache);
                var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                model.CustomerMeterTypes = new List<ProfileViewModel.ProfileCustomerMeterType>();

                foreach (var meter in meters)
                {
                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
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
                                model.CustomerMeterTypes.Add(new ProfileViewModel.ProfileCustomerMeterType()
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

                model.LowBalanceNotification = Convert.ToInt32(Request.Form["LowBalanceNotification"]);

                model.BalanceNotificationSMSType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "None", Text = $"None (Free)", Selected = Request.Form["BalanceNotificationSMSType"] == "None" ? true : false },
                    new SelectListItem() { Value = "Daily", Text = $"Daily (R2.00 per sms)", Selected = Request.Form["BalanceNotificationSMSType"] == "Daily" ? true : false },
                    new SelectListItem() { Value = "Weekly", Text = $"Weekly (Free)", Selected = Request.Form["BalanceNotificationSMSType"] == "Weekly" ? true : false },
                };

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

            #region Customer Profile

            if (customer != null)
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
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
                    customer.NotificationEmail = model.NotificationEMail;
                    customer.BalanceNotificationSMSType = Request.Form["BalanceNotificationSMSType"];

                    customer.ActivateTaxInvoice = Convert.ToBoolean(Request.Form["ActivateTaxInvoice"]);
                    customer.RecipientName = model.RecipientName;
                    customer.RecipientAddress = model.RecipientAddress;
                    customer.RecipientVATNumber = model.RecipientVATNumber;
                    customer.RecipientReferenceNumber = model.RecipientReferenceNumber;
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

                    var apiClient = new SkyBillApiClient(company.Name, _cache);
                    var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                    model.CustomerMeterTypes = new List<ProfileViewModel.ProfileCustomerMeterType>();

                    foreach (var meter in meters)
                    {
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
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

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                var sC = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
                model.SkybillCustomer = sC;
                customer = db.Customers.Where(tbl => tbl.CustomerNumber == _customerProvider.CustomerNumber && tbl.IsDeleted == false).FirstOrDefault();
            }
            else
                customer = db.Customers.Where(tbl => tbl.UserID == currentUser.Id && tbl.IsDeleted == false).SingleOrDefault();

            model.SelectedCustomer = customer;

            if (customer != null && customer.UserID == currentUser.Id)
                model.IsCurrentUserCustomer = true;

            #endregion

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

            return View("~/Views/Account/Profile.cshtml", model);
        }

        [Route("/changeMeterType/{customerMeterTypeId}/{type}")]
        public async Task<IActionResult> ChangeMeterType(int customerMeterTypeId, int type)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterTypeID == customerMeterTypeId)).FirstOrDefault();
                if (customerMeterType != null)
                {
                    customerMeterType.Selected = type;
                    db.SaveChanges();
                    return Content("{\"result\":true}", "application/json");
                }
            }

            return Content("{\"result\":false}", "application/json");
        }

        [Route("/changeShowHourlyUsage")]
        public async Task<IActionResult> ChangeShowHourlyUsage()
        {
            var user = _userManager.GetUserAsync(User).Result;
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                if (customer != null)
                {
                    if (customer.ShowDailyUsage.HasValue)
                    {
                        // has a value, transpose it
                        customer.ShowDailyUsage = !customer.ShowDailyUsage.Value;
                    }
                    else
                    {
                        // No value, which means false, so make it true
                        customer.ShowDailyUsage = true;
                    }
                    db.SaveChanges();
                    return Content("{\"result\":true}", "application/json");
                }
            }

            return Content("{\"result\":false}", "application/json");
        }
        [Route("/changeCostInclVAT")]
        public async Task<IActionResult> ChangeCostInclVAT()
        {
            var user = _userManager.GetUserAsync(User).Result;
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                if (customer != null)
                {
                    if (customer.ShowCostInclVAT.HasValue)
                    {
                        // has a value, transpose it
                        customer.ShowCostInclVAT = !customer.ShowCostInclVAT.Value;
                    }
                    else
                    {
                        // No value, which means false, so make it true
                        customer.ShowCostInclVAT = true;
                    }
                    db.SaveChanges();
                    return Content("{\"result\":true}", "application/json");
                }
            }

            return Content("{\"result\":false}", "application/json");
        }
        [Route("/changeAutoConvertBalanceToUnits")]
        public async Task<IActionResult> ChangeAutoConvertBalanceToUnits()
        {
            var user = _userManager.GetUserAsync(User).Result;
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                if (customer != null)
                {
                    if (customer.AutoConvertBalanceToUnits.HasValue)
                    {
                        // has a value, transpose it
                        customer.AutoConvertBalanceToUnits = !customer.AutoConvertBalanceToUnits.Value;
                    }
                    else
                    {
                        // No value, which means false, so make it true
                        customer.AutoConvertBalanceToUnits = true;
                    }
                    db.SaveChanges();
                    return Content("{\"result\":true}", "application/json");
                }
            }

            return Content("{\"result\":false}", "application/json");
        }
        [HttpPost]
        [HttpGet("/logout")]
        public async Task<IActionResult> Logout()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.LogOff,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.None,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            await _signInManager.SignOutAsync();
            HttpContext.Session.SetString("LogoutInfo", "Logout successfully.");
            HttpContext.Session.Remove(CustomerProvider.SESSION_CUSTOMER_ID);
            HttpContext.Session.Remove(CustomerProvider.SESSION_COMPANY_NAME);
            HttpContext.Session.Remove(CustomerProvider.METER_NUMBER);

            HttpContext.Session.Remove(OperationalProvider.SESSION_COMPANY_ID);
            HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL);
            HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_NUMBER);
            HttpContext.Session.Remove(OperationalProvider.SESSION_NAVIGATION_HISTORY);

            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Redirect("/");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet("/termsandconditions")]
        [AllowAnonymous]
        public async Task<IActionResult> TermsAndConditions()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null) // If user is deleted or does not exist.
                {
                    ModelState.AddModelError("Email", "User does not exist.");
                    return View(model);
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);

                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var customer = db.Customers.Where(p => p.UserID == user.Id).FirstOrDefault();

                //await _emailSender.SendResetPasswordEmailAsync("lendl@myvoltage.co.za", $"{model.Email} - Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                //if (model.Email.ToUpper().Contains("ALL@"))
                //{
                //}
                //else
                //{
                await _emailSender.SendResetPasswordEmailNewAsync(model.Email, $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>", customer?.FullName);

                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "reset_password.txt");
                string htmlEmail = System.IO.File.ReadAllText(file);

                htmlEmail = htmlEmail.Replace("{details}", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                htmlEmail = htmlEmail.Replace("{url}", "https://www.mymetersa.co.za");

                Data.Log_Notification log_Notification = new Log_Notification()
                {
                    MessagePreview = htmlEmail,
                    Recipients = model.Email,
                    TimeSent = DateTime.Now,
                    CompanyID = customer != null ? customer.CompanyID : 0,
                    CustomerID = customer != null ? customer.CustomerID : 0,
                };
                db.Add(log_Notification);
                db.SaveChanges();

                //}
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId = null, string code = null)
        {
            if (userId == null)
            {
                throw new ApplicationException("Invalid link.");
            }

            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }

            var model = new ResetPasswordViewModel();

            var user = await _userManager.FindByIdAsync(userId);

            var isValidToken = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", code);
            if (isValidToken)
            {
                model = new ResetPasswordViewModel { Code = code, UserId = userId };

            }
            else
            {
                model = new ResetPasswordViewModel { Code = null, UserId = userId };
            }
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //var user = await _userManager.FindByEmailAsync(model.Email);
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            else
            {
                model = new ResetPasswordViewModel { Code = null, UserId = model.UserId };
            }

            AddErrors(result);
            return View();
        }

        [HttpGet]
        [HttpGet("/login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGuid([FromQuery(Name = "returnurl")] string returnUrl, [FromQuery(Name = "token")] string guid)
        {
            return Redirect("/");

            var headers = _context.HttpContext.Request.Headers;

            //headers["token"] = "129831d6-87f8-4900-8bf7-8894aced7b12";

            // Ensure that all of your properties are present in the current Request
            var user = _userManager.FindByIdAsync(guid).Result;

            if (user != null)
            {
                await _signInManager.SignInAsync(user, new AuthenticationProperties());
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet("/login/guid/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserGuid(string email)
        {
            return Json("");

            var user = _userManager.FindByEmailAsync(email).Result;

            if (user == null)
                return Json("");

            var obj = new { Id = user.Id };

            return Json(obj);
        }

        [HttpPost("/customerLookup/{searchstring}")]
        [AllowAnonymous]
        [EnableCors("ZendeskCORS")]
        public async Task<IActionResult> CustomerLookup(string searchstring)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            foreach (var header in Request.Headers)
            {
                headers.Add(header.Key.ToUpper(), header.Value.ToString());
            }

            if (!headers.ContainsKey("APIKEY"))
                return Content("");

            using (var db = new MyVoltageDbContext(_options))
            {
                //string encodedHeder = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(headers["APIKEY"]));

                var apiKeys = db.APIKEYS.ToList();
                var apiKey = apiKeys.Where(p => p.KEYGuid.ToString().ToUpper() == headers["APIKEY"].ToString().ToUpper()).SingleOrDefault();
                if (apiKey == null)
                    return Unauthorized();

                apiKey.DateLastUsed = DateTime.Now;
                db.SaveChanges();
            }

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

                    var customerDetails = new
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
                        Balance = balance
                    };

                    var localDevices = (from p in db.Devices
                                        where meterSerials.Contains(p.Serial)
                                        select p).ToList();

                    List<object> metersLinked = new List<object>();
                    List<string> serialsAdded = new List<string>();

                    foreach (var meter in skybillCustomers)
                    {
                        if (serialsAdded.Contains(meter.Serial_No))
                            continue;

                        var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                        var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                        string deviceType = "";

                        if (localDevice != null && localDevice.TypeID.HasValue)
                        {
                            deviceType = ((DeviceType)localDevice.TypeID.Value).ToString();
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



                        var meterResultItem = new
                        {
                            SerialNumber = meter.Serial_No
                            ,
                            Name = m2mDevice != null ? m2mDevice.name : ""
                            ,
                            Status = m2mDevice != null ? m2mDevice.deviceStatus : "Unknown"
                            ,
                            TypeName = deviceType
                            ,
                            LastCommunicated = m2mDevice != null ? m2mDevice.status.time : new DateTime()
                            ,
                            Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                        };

                        metersLinked.Add(meterResultItem);
                        serialsAdded.Add(meter.Serial_No);
                    }
                    List<object> paymentInfo = new List<object>();

                    var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestSage != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestSage.CreateDate
                            ,
                            PaymentAmount = latestSage.Amount
                            ,
                            PaymentMethod = "SagePay"
                        });

                    var latestUnipin = skybillCustomer != null ? db.UniPins.Where(p => p.MeterNumber == skybillCustomer.Serial_No).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestUnipin != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestUnipin.CreateDate
                            ,
                            PaymentAmount = latestUnipin.Amount
                            ,
                            PaymentMethod = "Unipin"
                        });

                    var result = new
                    {
                        CustomerDetails = customerDetails
                        ,
                        MetersLinked = metersLinked
                        ,
                        PaymentInfo = paymentInfo
                    };

                    return Json(result);
                }



                // MeterSerial (devices)
                var device = (from p in db.Devices
                              where p.Serial == searchstring
                              select p).SingleOrDefault();

                if (device != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == device.CompanyID).SingleOrDefault();
                    var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).SingleOrDefault();
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


                    var customerDetails = new
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
                        Balance = balance
                    };

                    var localDevices = (from p in db.Devices
                                        where meterSerials.Contains(p.Serial)
                                        select p).ToList();

                    List<object> metersLinked = new List<object>();
                    List<string> serialsAdded = new List<string>();

                    foreach (var meter in skybillCustomers)
                    {
                        if (serialsAdded.Contains(meter.Serial_No))
                            continue;

                        var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                        var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                        string deviceType = "";

                        if (localDevice != null && localDevice.TypeID.HasValue)
                        {
                            deviceType = ((DeviceType)localDevice.TypeID.Value).ToString();
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

                        var meterResultItem = new
                        {
                            SerialNumber = meter.Serial_No
                            ,
                            Name = m2mDevice != null ? m2mDevice.name : ""
                            ,
                            Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                            ,
                            TypeName = deviceType
                            ,
                            LastCommunicated = m2mDevice != null ? m2mDevice.status.time : new DateTime()
                            ,
                            Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                        };

                        metersLinked.Add(meterResultItem);
                        serialsAdded.Add(meter.Serial_No);
                    }

                    List<object> paymentInfo = new List<object>();

                    var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestSage != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestSage.CreateDate
                            ,
                            PaymentAmount = latestSage.Amount
                            ,
                            PaymentMethod = "SagePay"
                        });

                    var latestUnipin = device != null ? db.UniPins.Where(p => p.MeterNumber == device.Serial).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestUnipin != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestUnipin.CreateDate
                            ,
                            PaymentAmount = latestUnipin.Amount
                            ,
                            PaymentMethod = "Unipin"
                        });

                    var result = new
                    {
                        CustomerDetails = customerDetails
                        ,
                        MetersLinked = metersLinked
                        ,
                        PaymentInfo = paymentInfo
                    };

                    return Json(result);

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


                    var customerDetails = new
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
                        Balance = balance
                    };

                    var localDevices = (from p in db.Devices
                                        where meterSerials.Contains(p.Serial)
                                        select p).ToList();

                    List<object> metersLinked = new List<object>();
                    List<string> serialsAdded = new List<string>();

                    foreach (var meter in skybillCustomers)
                    {
                        if (serialsAdded.Contains(meter.Serial_No))
                            continue;

                        string meterLinked = $"{meter.Serial_No} - {meter.No}";

                        var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                        var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                        string deviceType = "";

                        if (localDevice != null && localDevice.TypeID.HasValue)
                        {
                            deviceType = ((DeviceType)localDevice.TypeID.Value).ToString();
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

                        var meterResultItem = new
                        {
                            SerialNumber = meter.Serial_No
                            ,
                            Name = m2mDevice != null ? m2mDevice.name : ""
                            ,
                            Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                            ,
                            TypeName = deviceType
                            ,
                            LastCommunicated = m2mDevice != null ? m2mDevice.status.time : new DateTime()
                            ,
                            Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                        };

                        metersLinked.Add(meterResultItem);
                        serialsAdded.Add(meter.Serial_No);
                    }

                    List<object> paymentInfo = new List<object>();

                    var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestSage != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestSage.CreateDate
                            ,
                            PaymentAmount = latestSage.Amount
                            ,
                            PaymentMethod = "SagePay"
                        });

                    var latestUnipin = customer != null ? db.UniPins.Where(p => p.MeterNumber == customer.MeterNumber).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                    if (latestUnipin != null)
                        paymentInfo.Add(new
                        {
                            PaymentDate = latestUnipin.CreateDate
                            ,
                            PaymentAmount = latestUnipin.Amount
                            ,
                            PaymentMethod = "Unipin"
                        });

                    var result = new
                    {
                        CustomerDetails = customerDetails
                        ,
                        MetersLinked = metersLinked
                        ,
                        PaymentInfo = paymentInfo
                    };

                    return Json(result);

                }

                return NotFound();
            }
        }

        public enum DeviceType : int
        {
            [Description("Electricity")]
            Electricity = 1,
            [Description("Water")]
            Water = 2,
            [Description("Valve")]
            Valve = 6,
            [Description("Gas")]
            Gas = 8
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion

        [HttpGet]
        [AllowAnonymous]
        [Route("/RegisterNew")]
        public IActionResult RegisterNew(string returnUrl = null, string type = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            RegisterViewModel model = new RegisterViewModel()
            {
                OccupancyDate = DateTime.Now.Date,
                RegisterType = type
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/Account/CheckEmail")]
        public IActionResult CheckEmail(RegisterViewModel model, string email)
        {
            var isValid = false;

            if (email != null)
            {

                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var registerUser = db.Users.Where(tbl => (tbl.Email == email) && tbl.IsDeleted == false).FirstOrDefault();
                if (registerUser != null)
                {
                    return Json(new { isValid = false, errorMessage = "Email address is already in use. Please contact info@myvoltage.co.za | 087 057 2561." });
                }
            }

            return Json(new { isValid = true });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/RegisterNew")]
        public IActionResult RegisterNew(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = model.RegisterType != null ? returnUrl + "?type=" + model.RegisterType : returnUrl;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var companies = db.Companies.ToList();

            var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(String.Empty, _cache);

            string customerNo = "";
            string companyName = "";
            int companyId = 0;

            if (ModelState.IsValid)
            {
                #region New Customer

                if (string.IsNullOrEmpty(Request.Query["U"]))
                {
                    var registeredUser = db.Users.Where(tbl => (tbl.Email == model.Email || tbl.NormalizedEmail == model.Email) && tbl.IsDeleted == false).FirstOrDefault();
                    if (registeredUser != null)
                    {
                        ModelState.AddModelError("Email", "Email already in use. Please contact info@myvoltage.co.za | 087 057 2561.");
                        return View(model);
                    }
                    if (model.MeterNumber != null)
                    {
                        var registeredCustomer = db.Customers.Where(tbl => (tbl.MeterNumber == model.MeterNumber) && tbl.IsDeleted == false).FirstOrDefault();
                        if (registeredCustomer != null)
                        {
                            ModelState.AddModelError("MeterNumber", "Meter Number already in use. Please contact info@myvoltage.co.za | 087 057 2561.");
                            TempData["isMeterNumberError"] = "true";
                            return View(model);
                        }
                    }

                    try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(model.Email); }
                    catch
                    {
                        ModelState.AddModelError("Email", "Invalid email.");
                        return View(model);
                    }

                    try
                    {
                        Convert.ToInt64(model.PhoneNumber);
                        if (model.PhoneNumber.Length != 10)
                        {
                            ModelState.AddModelError("PhoneNumber", "Invalid phone number.");
                            return View(model);
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError("PhoneNumber", "Invalid phone pumber.");
                        return View(model);
                    }

                    try
                    {
                        Convert.ToInt64(model.PostalCode);
                    }
                    catch
                    {
                        ModelState.AddModelError("PostalCode", "Invalid postal code.");
                        return View(model);
                    }

                    var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == model.MeterNumber).FirstOrDefault();
                    if (localSkybillCustomer != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).FirstOrDefault();
                        companyName = company.Name;
                        customerNo = localSkybillCustomer.Customer_No;
                        companyId = company.CompanyID;
                    }
                    else
                    {
                        var customer = client.GetCustomerByMeterNumberInAllCompanies(model.MeterNumber, companies);

                        if (customer == null)
                        {
                            ModelState.AddModelError("MeterNumber", "Invalid meter number.");
                            return View(model);
                        }
                        else
                        {
                            customerNo = customer.Customer_No;
                            companyName = customer.company.Name;
                            companyId = customer.company.CompanyID;
                        }
                    }

                    var customerDetails = client.GetCustomerDetailsByCustomerNo(customerNo, companyName);

                    if (customerDetails == null)
                    {
                        ModelState.AddModelError("MeterNumber", "Customer setup incomplete (Skybill)");
                        return View(model);
                    }

                    if (ModelState.IsValid)
                    {
                        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                        var result = _userManager.CreateAsync(user, model.Password).Result;

                        if (result.Succeeded)
                        {
                            var accountTypeID = 0;
                            if (string.Compare(customerDetails.Billing_Cycle, "POSTPAID", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.POSTPAID;
                            }
                            else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.POSTPAID.ToString())
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.POSTPAID;
                            }
                            else if (string.Compare(customerDetails.Billing_Cycle, "PREPAID", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.PREPAID;
                            }
                            else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.PREPAID.ToString())
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.PREPAID;
                            }
                            else if (string.Compare(customerDetails.Billing_Cycle, "WALLET", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                            }
                            else if (customerDetails.Billing_Cycle == CustomerAccountTypeEnum.WALLET.ToString())
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                            }
                            else
                            {
                                accountTypeID = (int)CustomerAccountTypeEnum.WALLET;
                            }
                            Random random = new Random();
                            var numbers = "1234567890";
                            var stringNumbers = new char[4];

                            for (int i = 0; i < stringNumbers.Length; i++)
                            {
                                stringNumbers[i] = numbers[random.Next(numbers.Length)];
                            }

                            var OTPCode = new string(stringNumbers);
                            var cust = new Data.Customer
                            {
                                CustomerNumber = customerNo,
                                AltPhoneNumber = model.PhoneNumber,

                                //IDNumberOrCompanyReg = customer_Register.IDNumberOrCompanyReg,
                                //UnitNumber = customer_Register.UnitNumber,
                                //                            ComplexName = model.ComplexName,
                                StreetAddress = model.StreetAddress,
                                Suburb = model.Suburb,
                                TownOrCity = model.TownOrCity,
                                Province = model.Province,
                                PostalCode = Int32.Parse(model.PostalCode),
                                FullName = model.FirstName + " " + model.LastName,
                                PhoneNumber = model.PhoneNumber,
                                UserID = user.Id,
                                CompanyID = companyId,
                                MeterNumber = model.MeterNumber,
                                AccountTypeID = accountTypeID,
                                IsDeleted = false,
                                OTPCode = OTPCode,
                                EmailCode = String.Empty,
                                OccupancyDate = model.OccupancyDate,
                                IDNumberOrCompanyReg = model.IDNumberOrCompanyReg,
                                NotificationEmail = model.Email,
                                RecipientName = model.CompanyID == "0" ? null : model.CompanyID,
                                RecipientAddress = model.StreetAddress,
                                RecipientReferenceNumber = model.CompanyID == "0" ? $"{model.FirstName} {model.LastName}" : null,
                                ShowDailyUsage = true,
                                ShowCostInclVAT = true,
                                ShowDetailedDailyBilling = true,
                                DisconnectionLowBalanceNotification1 = 100,
                                HighUsageNotifications = true,
                                ActivateTaxInvoice = true,
                            };

                            cust.RecipientAddress = string.Join(" ", cust.StreetAddress,
                                cust.Suburb,
                                cust.TownOrCity,
                                cust.Province,
                                cust.PostalCode);

                            db.Customers.Add(cust);

                            db.SaveChanges();

                            SMS.SendSms("27" + model.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + OTPCode + ". Enter this code during registration to verify your contact details.");
                            _emailSender.SendEmailCodeAsync(model.Email, OTPCode, model.FirstName + " " + model.LastName, user.Id, _context);

                            return Redirect($"/OTPConfirmNew?id={user.Id}");
                        }
                        else
                            ModelState.AddModelError("Email", String.Join(',', result.Errors.Select(p => p.Description).ToList()));

                    }

                }

                #endregion

                #region Existing Customer

                else
                {

                }

                #endregion
            }

            return View(model);
        }

        [HttpGet]
        [Route("/Account/CheckMeterNumber")]
        [AllowAnonymous]
        public IActionResult CheckMeterNumber(string meterNumber)
        {
            var isValid = false;

            if (meterNumber != null)
            {
                string customerNo = "";
                string companyName = "";
                int companyId = 0;

                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var companies = db.Companies.ToList();

                var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(String.Empty, _cache);

                var registeredCustomer = db.Customers.Where(tbl => (tbl.MeterNumber == meterNumber) && tbl.IsDeleted == false).FirstOrDefault();
                if (registeredCustomer != null)
                {
                    return Json(new { isValid = false, errorMessage = "Meter Number already in use. Please contact info@myvoltage.co.za.co.za | 087 057 2561." });
                }

                var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == meterNumber).FirstOrDefault();
                if (localSkybillCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).FirstOrDefault();
                    companyName = company.Name;
                    customerNo = localSkybillCustomer.Customer_No;
                    companyId = company.CompanyID;
                }
                else
                {
                    var customer = client.GetCustomerByMeterNumberInAllCompanies(meterNumber, companies);

                    if (customer == null)
                    {
                        return Json(new { isValid = false, errorMessage = "Invalid meter number." });
                    }
                    else
                    {
                        customerNo = customer.Customer_No;
                        companyName = customer.company.Name;
                        companyId = customer.company.CompanyID;
                    }
                }

                var customerDetails = client.GetCustomerDetailsByCustomerNo(customerNo, companyName);

                if (customerDetails == null)
                {
                    return Json(new { isValid = false, errorMessage = "Customer setup incomplete (Skybill)." });
                }

                return Json(new { isValid = true, errorMessage = "" });

            }

            return Json(new { isValid = false, errorMessage = "Meter Number is required." });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/OTPConfirmNew")]
        public async Task<IActionResult> OTPConfirmNew(string id, string returnUrl = null)
        {
            ViewData["ReturnUrlOtp"] = returnUrl;
            ViewData["id"] = id;
            var db = new MyVoltageDbContext(_options);
            var model = new OTPConfirmViewModel
            {
                Email = "email@email.com",
                UserId = id,
                PhoneNumber = "0123456789",
            };
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return RedirectToAction(nameof(Login));
                }
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();
                if (user != null)
                {
                    if (user.IsConfirmed)
                    {
                        return RedirectToAction(nameof(Login));
                    }
                    var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    if (customer == null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains(UserRoleEnum.Leaduser.ToString()))
                        {
                            return RedirectToAction("LeaduserRegisterOTPConfirm", "Leaduser", new {id = id, returnUrl = returnUrl});
                        }
                    }

                    Random random = new Random();
                    var numbers = "1234567890";
                    var stringNumbers = new char[4];

                    for (int i = 0; i < stringNumbers.Length; i++)
                    {
                        stringNumbers[i] = numbers[random.Next(numbers.Length)];
                    }

                    var OTPCode = new string(stringNumbers);
                    var phoneNo = customer.PhoneNumber;
                    if (customer != null)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["pn"]))
                        {
                            customer.OTPCode = OTPCode;
                            SMS.SendSms("27" + phoneNo.Remove(0, 1), "Your My Voltage registration confirmation code is " + customer.OTPCode + ". Enter this code during registration to verify your contact details.");
                            _emailSender.SendEmailCodeAsync(user.Email, customer.OTPCode, customer.FullName, user.Id, _context);
                            db.Customers.Update(customer);
                            db.SaveChanges();
                            return Json(new { status = true, msg = "OTP resent successfully." });
                        }
                    }
                    model = new OTPConfirmViewModel
                    {
                        Email = user.Email,
                        UserId = user.Id,
                        //PhoneNumber = !string.IsNullOrEmpty(Request.Query["pn"]) ? Request.Query["pn"].ToString() : customer.PhoneNumber,
                        PhoneNumber = user.PhoneNumber != null ? user.PhoneNumber.ToString() : phoneNo
                    };
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, msg = "Error" });
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("/OTPConfirmNew")]
        public async Task<IActionResult> OTPConfirmNew(OTPConfirmViewModel model, string id, string returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (var db = new MyVoltageDbContext(_options))
                    {
                        var user = db.Users.Where(tbl => tbl.Id == id).SingleOrDefault();

                        var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                        var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                        var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();
                        string OTP = $"{model.OTP1}{model.OTP2}{model.OTP3}{model.OTP4}";
                        if (customer.OTPCode == OTP)
                        {
                            //if (customer.PhoneNumber != model.PhoneNumber)
                            //{
                            //	customer.PhoneNumber = model.PhoneNumber;
                            //	db.Update(customer);
                            //	db.SaveChanges();
                            //	customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                            //}

                            user.IsConfirmed = true;

                            db.SaveChanges();

                            String email = "New user<br/> Email: " + user.Email + "<br/>" +
                                            "Meter Number: " + customer.MeterNumber + "<br/>" +
                                            " Phone Number: " + customer.PhoneNumber + "<br/>" +
                                            " Email Code: " + customer.EmailCode + "<br/>" +
                                            " OTP: " + customer.OTPCode + "<br/>" +
                                            " Full Name: " + customer.FullName + "<br/>" +
                                            " Company: " + company.Name + "<br/>" +
                                        " Customer Number: " + customer.CustomerNumber + "<br/>" +
                                        " Alternative Phone Number: " + customer.AltPhoneNumber + "<br/>" +
                                        " ID Number Or Company Registration: " + customer.IDNumberOrCompanyReg + "<br/>" +
                                        " Unit Number: " + customer.UnitNumber + "<br/>" +
                                        " Street Address: " + customer.StreetAddress + "<br/>" +
                                        " Suburb: " + customer.Suburb + "<br/>" +
                                        " Town Or City: " + customer.TownOrCity + "<br/>" +
                                        " Province: " + customer.Province + "<br/>" +
                                        " Postal Code: " + customer.PostalCode + "<br/>" +
                                        " Account Type: " + accountType.Name + "<br/>" +
                                        " Occupancy Date: " + customer.OccupancyDate.ToLongDateString();

                            await _emailSender.SendUserDetailsAsync(_regEmail, email, customer.CustomerNumber, user.Email);

                            var notificationOfMeter = db.NotificationCustomerMeters.Where(tbl => tbl.MeterSerial == customer.MeterNumber).FirstOrDefault();

                            if (notificationOfMeter != null && !notificationOfMeter.LowBalanceNotification1)
                            {
                                notificationOfMeter.LowBalanceNotification1 = !notificationOfMeter.LowBalanceNotification1;

                                db.Update(notificationOfMeter);
                                db.SaveChanges();
                            }

                            NotificationCustomerMeter notification = db.NotificationCustomerMeters.Where(tbl => tbl.MeterSerial == customer.MeterNumber && tbl.CustomerID == 0).FirstOrDefault();
                            if (notification != null)
                            {
                                notification.LastUpdated = DateTime.Now;
                                notification.AccountType = accountType.AccountTypeID;
                                notification.CustomerID = customer.CustomerID;
                                notification.LowBalanceNotification1 = true;

                                db.Update(notification);
                                db.SaveChanges();
                            }

                            //if (accountType.AccountTypeID == (int)AccountTypeEnum.PostPaid)
                            //{
                            //    return RedirectToLocal("~/Account/RegistrationSuccess?id=" + id);
                            //}

                            return Redirect("/RegistrationSuccessNew?id=" + id);
                        }
                        else
                        {
                            ViewData["id"] = id;
                            ModelState.AddModelError("OTP1", "Please enter a valid OTP.");
                            return View(model);
                            //return Redirect("/Account/RegistrationUnsuccessful");
                        }
                    }
                }
                ViewData["id"] = id;
                return View(PopulateOTPConfirmViewModel(id));
            }
            catch (Exception ex)
            {
                return Redirect("/Account/RegistrationUnsuccessful");
            }
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("/RegistrationSuccessNew")]
        public IActionResult RegistrationSuccessNew(string id)
        {
            ViewData["id"] = id;
            var db = new MyVoltageDbContext(_options);
            var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

            var customer = db.Customers.Where(tbl => tbl.UserID == user.Id).SingleOrDefault();
            var model = new RegistrationSuccessViewModel
            {
                SystemNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                            select new SelectListItem()
                                            {
                                                Text = p.GetDescription(),
                                                Value = ((int)p).ToString(),
                                                Selected = customer.SystemNotificationTypeID.HasValue && customer.SystemNotificationTypeID.Value == (int)p,
                                            }).ToList(),

                InfoNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                          select new SelectListItem()
                                          {
                                              Text = p.GetDescription(),
                                              Value = ((int)p).ToString(),
                                              Selected = customer.InfoNotificationTypeID.HasValue && customer.InfoNotificationTypeID.Value == (int)p,
                                          }).ToList(),
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/RegistrationSuccessNew")]
        public IActionResult RegistrationSuccessNew(RegistrationSuccessViewModel model, string id)
        {
            ViewData["id"] = id;
            var db = new MyVoltageDbContext(_options);
            var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

            var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
            model.SystemNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Form["SystemNotificationTypeID"] == ((int)p).ToString() ? true : false,
                                              }).ToList();
            model.InfoNotificationTypeID = (from p in ((NotificationTypeEnum[])Enum.GetValues(typeof(NotificationTypeEnum)))
                                            select new SelectListItem()
                                            {
                                                Text = p.GetDescription(),
                                                Value = ((int)p).ToString(),
                                                Selected = Request.Form["InfoNotificationTypeID"] == ((int)p).ToString() ? true : false,
                                            }).ToList();


            if (ModelState.IsValid)
            {
                customer.SystemNotificationTypeID = Convert.ToInt32(Request.Form["SystemNotificationTypeID"]);
                customer.InfoNotificationTypeID = Convert.ToInt32(Request.Form["InfoNotificationTypeID"]);
                db.Update(customer);
                db.SaveChanges();

                //var result = _signInManager.SignInAsync(user, new AuthenticationProperties());
                //result.Wait();

                return Redirect("/");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterOptions()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegistrationUnsuccessful()
        {
            return View();
        }

    }

}
