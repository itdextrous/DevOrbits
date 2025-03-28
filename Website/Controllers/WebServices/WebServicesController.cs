using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Interfaces;
using Microsoft.Extensions.Configuration;
using MyVoltageApi.Data;
using MyVoltage.Api.Factories;
using MyVoltage.Models.WebServicesModels;
using Newtonsoft.Json;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System.Security.Cryptography;
using System.Collections.Generic;
using MyVoltage.Services.Operational;
using MyVoltage.Services;
using System.Web;
using System.IO;
using MyVoltage.Api.SkyBill;
using System.Data.SqlClient;
using System.Data;
using GemBox.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.RegularExpressions;
using MyVoltage.Data.Migrations;
using MyVoltage.Models.ClientzoneModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using SelectPdf;

namespace MyVoltage.Controllers.WebServices
{
    public class WebServicesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly ClientzoneProvider _clientzoneProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly string _regEmail;
        private string _userEmail;
        private string _userID;
        private readonly IHttpContextAccessor _context;
        private readonly UsageProvider _usageProvider;
        private readonly string _jsonFilePath = "appVersion.json";

        public WebServicesController(
            IHttpContextAccessor context,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            SignInManager<ApplicationUser> signInManager,
            ClientzoneProvider clientzoneProvider
            )
        {
            _context = context;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _regEmail = configuration["RegEmail:Email"];
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            _clientzoneProvider = clientzoneProvider;
            _usageProvider = new UsageProvider(clientzoneProvider.MeterTypeForUsage, clientzoneProvider.AccountTypeForSelectedCustomer, cache, clientzoneProvider.CompanyName, clientzoneProvider.CustomerNumber, clientzoneProvider.ShowCostInclVAT, options, APIoptions, configuration, clientzoneProvider.OccupancyDate);
        }

        /// <summary>
        /// Customer app version
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [ProducesResponseType(typeof(App_Version), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/AppVersion")]
        public async Task<Boolean> App_Version([FromBody]App_Details appDetails)
        {
            try
            {
                using (StreamReader sr = new StreamReader(_jsonFilePath))
                {
                    var json = await sr.ReadToEndAsync(); 
                    var appVersion = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<App_Version>>>(json);

                    if (appVersion.ContainsKey("MyVoltage"))
                    {
                        var myVoltageVersions = appVersion["MyVoltage"];
                        var myVoltageVersion = myVoltageVersions.FirstOrDefault(v => v.OS == appDetails.OSType);

                        if (myVoltageVersion != null)
                        {
                            var latestVersion = new Version(myVoltageVersion.LatestVersion);
                            var currentVersion = new Version(appDetails.CurrentVersion);
                            return latestVersion > currentVersion ? true : false;
                        }
                    }
                    else if (appVersion.ContainsKey("GasManager"))
                    {
                        var gasManagerVersions = appVersion["GasManager"];
                        var gasManagerVersion = gasManagerVersions.FirstOrDefault(v => v.OS == appDetails.OSType);

                        if (gasManagerVersion != null)
                        {
                            var latestVersion = new Version(gasManagerVersion.LatestVersion);
                            var currentVersion = new Version(appDetails.CurrentVersion);
                            return latestVersion > currentVersion ? true : false;
                        }
                    }

                    return false;
                }
            }
            catch (IOException ex)
            {
                // Log the exception or handle it as needed
                return false;
            }
        }

        [NonAction]
        public bool IsAuthenticated(out string customerNo, out int companyID)
        {
            customerNo = "";
            companyID = 0;
            try
            {
                string authHeader = HttpContext.Request.Headers["Authorization"];
                if (authHeader.StartsWith("Bearer "))
                {
                    // Get the encoded username and password
                    var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();

                    // Check if login is correct
                    var db = new Data.MyVoltageDbContext(_options);
                    var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.Token == encodedUsernamePassword).SingleOrDefault();

                    if (wSCustomerLoginToken == null)
                        return false;

                    var user = db.Users.Where(x => x.Id == wSCustomerLoginToken.UserID).SingleOrDefault();

                    if (user.IsDeleted)
                        return false;

                    if (wSCustomerLoginToken.DateExpired >= DateTime.Now.ToUniversalTime().AddHours(2))
                    {
                        customerNo = wSCustomerLoginToken.CustomerNo;
                        companyID = wSCustomerLoginToken.CompanyID;
                        _userEmail = _userManager.FindByIdAsync(wSCustomerLoginToken.UserID).Result.Email;
                        _userID = wSCustomerLoginToken.UserID;
                        return true;
                    }

                }
            }
            catch { }
            return false;
        }

        [NonAction]
        public bool IsTokenValid(string token, out string customerNo, out int companyID)
        {
            customerNo = "";
            companyID = 0;
            try
            {

                // Get the encoded username and password
                var encodedUsernamePassword = token;

                // Check if login is correct
                var db = new Data.MyVoltageDbContext(_options);
                var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.Token == encodedUsernamePassword).SingleOrDefault();

                if (wSCustomerLoginToken == null)
                    return false;

                var user = db.Users.Where(x => x.Id == wSCustomerLoginToken.UserID).SingleOrDefault();

                if (user.IsDeleted)
                    return false;

                if (wSCustomerLoginToken.DateExpired >= DateTime.Now.ToUniversalTime().AddHours(2))
                {
                    customerNo = wSCustomerLoginToken.CustomerNo;
                    companyID = wSCustomerLoginToken.CompanyID;
                    _userEmail = _userManager.FindByIdAsync(wSCustomerLoginToken.UserID).Result.Email;
                    _userID = wSCustomerLoginToken.UserID;
                    return true;
                }


            }
            catch { }
            return false;
        }

        //[HttpGet]
        //[Route("/WebServices/TermsAndConditions")]
        //public async Task<IActionResult> TermsAndConditions()
        //{
        //    var result = new
        //    {
        //        TermsAndConditions = "TermsAndConditions",
        //    };

        //    return Content(JsonConvert.SerializeObject(result), "application/json");
        //}

        /// <summary>
        /// Registers customer
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "meterNumber": "string",
        ///         "firstName": "string",
        ///         "lastName": "string",
        ///         "address": "string",
        ///         "suburb": "string",
        ///         "city": "string",
        ///         "province": "string",
        ///         "postalCode": "string",
        ///         "email": "string",
        ///         "phoneNumber": "string",
        ///         "password": "string",
        ///         "confirmPassword": "string",
        ///         "occupancyDate": "2022-05-26T04:08:11.378Z",
        ///         "companyName": "string",
        ///         "IDNumberOrCompanyReg": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(Customer_RegisterResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/Register")]
        public async Task<IActionResult> Customer_Register()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_Register customer_Register = new Models.WebServicesModels.Customer_Register();
            try
            {
                customer_Register = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Register>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_Register),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_RegisterResult customer_RegisterResult = new Customer_RegisterResult()
            {
                IsSuccess = true,
                Message = "OTP Sent",
                UserID = "",
            };

            var companies = db.Companies.ToList();

            var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(String.Empty, _cache);

            string customerNo = "";
            string companyName = "";
            int companyId = 0;


            if (string.IsNullOrEmpty(customer_Register.MeterNumber)
                || string.IsNullOrEmpty(customer_Register.Email)
                || string.IsNullOrEmpty(customer_Register.FirstName)
                || string.IsNullOrEmpty(customer_Register.LastName)
                || string.IsNullOrEmpty(customer_Register.Password)
                || string.IsNullOrEmpty(customer_Register.ConfirmPassword)
                || string.IsNullOrEmpty(customer_Register.Address)
                || string.IsNullOrEmpty(customer_Register.Suburb)
                || string.IsNullOrEmpty(customer_Register.City)
                || string.IsNullOrEmpty(customer_Register.Province)
                || string.IsNullOrEmpty(customer_Register.PostalCode)
                || string.IsNullOrEmpty(customer_Register.PhoneNumber)
                //|| string.IsNullOrEmpty(customer_Register.IDNumberOrCompanyReg)
                )
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Missing info: ",
                    UserID = "",
                };

                if (string.IsNullOrEmpty(customer_Register.MeterNumber))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "MeterNumber;";

                if (string.IsNullOrEmpty(customer_Register.Email))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "Email;";

                if (string.IsNullOrEmpty(customer_Register.FirstName))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "FirstName;";

                if (string.IsNullOrEmpty(customer_Register.LastName))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "LastName;";

                if (string.IsNullOrEmpty(customer_Register.Password))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "Password;";

                if (string.IsNullOrEmpty(customer_Register.ConfirmPassword))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "ConfirmPassword;";

                if (string.IsNullOrEmpty(customer_Register.Address))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "Address;";

                if (string.IsNullOrEmpty(customer_Register.Suburb))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "Suburb;";

                if (string.IsNullOrEmpty(customer_Register.City))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "City;";

                if (string.IsNullOrEmpty(customer_Register.Province))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "Province;";

                if (string.IsNullOrEmpty(customer_Register.PostalCode))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "PostalCode;";

                if (string.IsNullOrEmpty(customer_Register.PhoneNumber))
                    customer_RegisterResult.Message = customer_RegisterResult.Message + "PhoneNumber;";

                //if (string.IsNullOrEmpty(customer_Register.IDNumberOrCompanyReg))
                //    customer_RegisterResult.Message = customer_RegisterResult.Message + "IDNumberOrCompanyReg;";

                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }
            if (customer_Register.Password != customer_Register.ConfirmPassword)
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Passwords do not match",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            var registeredUser = db.Users.Where(tbl => (tbl.Email == customer_Register.Email || tbl.NormalizedEmail == customer_Register.Email) && tbl.IsDeleted == false).FirstOrDefault();
            if (registeredUser != null)
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Email already in use",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            var registeredCustomer = db.Customers.Where(tbl => (tbl.MeterNumber == customer_Register.MeterNumber) && tbl.IsDeleted == false).FirstOrDefault();
            if (registeredCustomer != null)
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Meter Number already in use",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(customer_Register.Email); }
            catch
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Invalid Email",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            try
            {
                Convert.ToInt64(customer_Register.PhoneNumber);
                if (customer_Register.PhoneNumber.Length != 10)
                {
                    customer_RegisterResult = new Customer_RegisterResult()
                    {
                        IsSuccess = false,
                        Message = "Invalid Phone Number",
                        UserID = "",
                    };
                    return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
                }
            }
            catch
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Invalid Phone Number",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            try
            {
                Convert.ToInt64(customer_Register.PostalCode);
            }
            catch
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Invalid Postal Code",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == customer_Register.MeterNumber).FirstOrDefault();
            if (localSkybillCustomer != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).FirstOrDefault();
                companyName = company.Name;
                customerNo = localSkybillCustomer.Customer_No;
                companyId = company.CompanyID;
            }
            else
            {
                //var customer = client.GetCustomerByMeterNumberInAllCompanies(customer_Register.MeterNumber, companies);

                //if (customer == null)
                //{
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Invalid Meter Number",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
                //}
                //else
                //{
                //    customerNo = customer.Customer_No;
                //    companyName = customer.company.Name;
                //    companyId = customer.company.CompanyID;
                //}
            }

            var customerDetails = client.GetCustomerDetailsByCustomerNo(customerNo, companyName);

            if (customerDetails == null)
            {
                customer_RegisterResult = new Customer_RegisterResult()
                {
                    IsSuccess = false,
                    Message = "Customer Setup Incomplete (Skybill)",
                    UserID = "",
                };
                return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
            }

            if (customer_RegisterResult.IsSuccess)
            {
                var user = new ApplicationUser { UserName = customer_Register.Email, Email = customer_Register.Email };
                var result = await _userManager.CreateAsync(user, customer_Register.Password);

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

                    var cust = new Data.Customer()
                    {
                        CustomerNumber = customerNo,
                        AltPhoneNumber = customer_Register.PhoneNumber,
                        IDNumberOrCompanyReg = !string.IsNullOrEmpty(customer_Register.IDNumberOrCompanyReg) ? customer_Register.IDNumberOrCompanyReg : string.Empty,
                        //UnitNumber = customer_Register.UnitNumber,
                        //                            ComplexName = model.ComplexName,
                        StreetAddress = customer_Register.Address,
                        Suburb = customer_Register.Suburb,
                        TownOrCity = customer_Register.City,
                        Province = customer_Register.Province,
                        PostalCode = Int32.Parse(customer_Register.PostalCode),
                        FullName = customer_Register.FirstName + " " + customer_Register.LastName,
                        PhoneNumber = customer_Register.PhoneNumber,
                        UserID = user.Id,
                        CompanyID = companyId,
                        MeterNumber = customer_Register.MeterNumber,
                        AccountTypeID = accountTypeID,
                        IsDeleted = false,
                        OTPCode = OTPCode,
                        EmailCode = "",
                        OccupancyDate = customer_Register.OccupancyDate.HasValue ? customer_Register.OccupancyDate.Value : DateTime.Now.Date,
                        NotificationEmail = customer_Register.Email,
                        RecipientName = !String.IsNullOrEmpty(customer_Register.CompanyName) ? customer_Register.CompanyName : String.Empty,
                        //RecipientAddress = customer_Register.Address,
                        RecipientReferenceNumber = String.IsNullOrEmpty(customer_Register.CompanyName) ? $"{customer_Register.FirstName} {customer_Register.LastName}" : String.Empty,
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
                    customer_RegisterResult.UserID = user.Id;

                    activityLog.UserID = user.Id;
                    activityLog.DateEnded = DateTime.Now;
                    activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_RegisterResult);
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }

                    db.SaveChanges();

                    SMS.SendSms("27" + customer_Register.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + OTPCode + ". Enter this code during registration to verify your contact details.");

                    await _emailSender.SendEmailCodeAsync(customer_Register.Email, OTPCode, customer_Register.FirstName + " " + customer_Register.LastName, user.Id, _context);
                }
                else
                    customer_RegisterResult = new Customer_RegisterResult()
                    {
                        IsSuccess = false,
                        Message = String.Join(',', result.Errors.Select(p => p.Description).ToList()),
                        UserID = "",
                    };

            }
            return Content(JsonConvert.SerializeObject(customer_RegisterResult), "application/json");
        }

        /// <summary>
        /// Checks User Exististance
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [ProducesResponseType(typeof(Customer_Exists), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/CheckUserExists")]
        public async Task<IActionResult> CheckUserExists()
        {
            try
            {
                var email = Request.Query["em"].ToString();
                if (string.IsNullOrEmpty(email))
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                var result = new Customer_Exists();

                var user = await _userManager.FindByEmailAsync(email);

                if (user != null && user.IsDeleted == false)
                {
                    result.IsSuccess = true;
                    result.Message = "User Exists";
                    result.IsConfirmed = user.IsConfirmed;
                    result.UserId = user.Id;
                    MyVoltageDbContext db = new MyVoltageDbContext(_options);
                    var customer = (from p in db.Customers
                                    where p.UserID == user.Id
                                    && !p.IsDeleted
                                    select p).FirstOrDefault();
                    if (customer != null)
                    {
                        result.PhoneNumber = customer.PhoneNumber ?? customer.AltPhoneNumber;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "User Not Found";
                    result.IsConfirmed = null;
                    result.UserId = null;
                }

                return Json(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Confirm customer OTP
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "UserID": "string",
        ///         "OTP": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(Customer_OTPConfirmResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/OTPConfirm")]
        public async Task<IActionResult> Customer_OTPConfirm()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            StreamReader reader = new StreamReader(Request.Body);

            Customer_OTPConfirm customer_OTPConfirm = new Models.WebServicesModels.Customer_OTPConfirm();

            try
            {
                customer_OTPConfirm = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_OTPConfirm>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPConfirm),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
            };

            Customer_OTPConfirmResult customer_OTPConfirmResult = new Customer_OTPConfirmResult()
            {
                IsSuccess = false,
                Message = "Error",
            };

            var user = db.Users.Where(tbl => tbl.Id == customer_OTPConfirm.UserID && tbl.IsDeleted == false).SingleOrDefault();
            if (user != null)
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                if (customer.OTPCode == customer_OTPConfirm.OTP)
                {
                    var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                    var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();

                    user.IsConfirmed = true;
                    db.SaveChanges();

                    String email = "New user<br/> Email: " + user.Email + "<br/>" +
                                    "Meter Number: " + customer.MeterNumber + "<br/>" +
                                    " Phone Number: " + user.PhoneNumber + "<br/>" +
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

                    customer_OTPConfirmResult = new Customer_OTPConfirmResult()
                    {
                        IsSuccess = true,
                        Message = "Customer confirmed",
                    };
                }
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPConfirmResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_OTPConfirmResult), "application/json");
        }

        /// <summary>
        /// Resend customer OTP
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "UserID": "string",
        ///         "PhoneNumber": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/OTPResend")]
        public async Task<IActionResult> Customer_OTPResend()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_OTPResend customer_OTPResend = new Models.WebServicesModels.Customer_OTPResend();

            try
            {
                customer_OTPResend = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_OTPResend>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPResend),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            GenericResult customer_OTPResendResult = new GenericResult()
            {
                IsSuccess = false,
                Message = "Error",
            };

            var user = db.Users.Where(tbl => tbl.Id == customer_OTPResend.UserID && tbl.IsDeleted == false).SingleOrDefault();
            if (user != null)
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                SMS.SendSms("27" + customer_OTPResend.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + customer.OTPCode + ". Enter this code during registration to verify your contact details.");

                customer_OTPResendResult = new GenericResult()
                {
                    IsSuccess = true,
                    Message = "OTP Resent",
                };
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPResendResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_OTPResendResult), "application/json");
        }

        /// <summary>
        /// Resend customer new OTP
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "UserID": "string",
        ///         "PhoneNumber": "string",
        ///         "Email": "string",
        ///         "IsProfile": true
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/OTPResendNew")]
        public async Task<IActionResult> Customer_OTPResendNew()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_OTPResend customer_OTPResend = new Models.WebServicesModels.Customer_OTPResend();

            try
            {
                customer_OTPResend = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_OTPResend>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPResend),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(customer_OTPResend.UserID) ? customer_OTPResend.UserID : _userManager.GetUserId(User),
            };

            GenericResult customer_OTPResendResult = new GenericResult()
            {
                IsSuccess = false,
                Message = "Error",
            };

            var user = db.Users.Where(tbl => tbl.Id == customer_OTPResend.UserID && tbl.IsDeleted == false).SingleOrDefault();
            if (user != null)
            {
                var customer = db.Customers.Where(tbl => tbl.UserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                if (customer != null)
                {

                    if (!string.IsNullOrEmpty(customer_OTPResend.PhoneNumber) || !string.IsNullOrEmpty(customer_OTPResend.Email))
                    {

                        Random random = new Random();
                        var numbers = "1234567890";
                        var stringNumbers = new char[4];

                        for (int i = 0; i < stringNumbers.Length; i++)
                        {
                            stringNumbers[i] = numbers[random.Next(numbers.Length)];
                        }

                        var OTPCode = new string(stringNumbers);
                        customer.OTPCode = OTPCode;

                        if (!string.IsNullOrEmpty(customer_OTPResend.PhoneNumber))
                        {
                            if (customer_OTPResend.IsProfile == true)
                            {
                                SMS.SendSms("27" + customer_OTPResend.PhoneNumber.Remove(0, 1), "Your My Voltage Cellphone verification code is " + OTPCode + ". Enter this code to update your cellphone detail.");
                            }
                            else
                            {
                                SMS.SendSms("27" + customer_OTPResend.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + OTPCode + ". Enter this code during registration to verify your contact details.");
                            }
                        }

                        if (!string.IsNullOrEmpty(customer_OTPResend.Email))
                        {
                            if (customer_OTPResend.IsProfile == true)
                            {
                                _emailSender.SendNotificationEmailCodeAsync(customer_OTPResend.Email, OTPCode, customer.FullName, _context);
                            }
                            else
                            {
                                _emailSender.SendEmailCodeAsync(customer_OTPResend.Email, OTPCode, customer.FullName, user.Id, _context);
                            }
                        }


                        db.Customers.Update(customer);
                        db.SaveChanges();
                        customer_OTPResendResult.IsSuccess = true;
                        customer_OTPResendResult.Message = "OTP resent successfully.";
                        return Json(customer_OTPResendResult);

                    }

                }

                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_OTPResendResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_OTPResendResult), "application/json");
        }

        /// <summary>
        /// Customer Login
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [ProducesResponseType(typeof(Customer_LoginResult), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Login")]
        public async Task<IActionResult> Customer_Login()
        {
            Customer_LoginResult customer_LoginResult = new Customer_LoginResult()
            {
                IsSuccess = false,
                Result = "Failed",
                Token = "",
            };

            var db = new Data.MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.Login,
                DateStarted = DateTime.Now.ToUniversalTime().AddHours(2),
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                // Get the encoded username and password
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                // Decode from Base64 to string
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                // Split username and password
                var username = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];


                var result = await _signInManager.PasswordSignInAsync(username, password, false, lockoutOnFailure: false);
                // Check if login is correct
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(username);
                    var customer = db.Customers.Where(p => p.UserID == user.Id).SingleOrDefault();
                    if (customer != null)
                    {
                        string key = user.Id;
                        string token = $"{key}_{DateTime.Now:yyyyMMddHHmmss}";

                        // Encrypt token
                        SHA256 sha256 = SHA256Managed.Create();
                        byte[] hashValue;
                        UTF8Encoding objUtf8 = new UTF8Encoding();
                        hashValue = sha256.ComputeHash(objUtf8.GetBytes(token));
                        string finalKey = string.Empty;
                        finalKey = Convert.ToBase64String(hashValue);

                        string encodedToken = System.Web.HttpUtility.UrlEncode(finalKey);
                        var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.UserID == key).SingleOrDefault();

                        if (wSCustomerLoginToken == null)               //User doesn't Exits in token table
                        {
                            wSCustomerLoginToken = new WSCustomerLoginToken()
                            {
                                DateCreated = DateTime.Now.ToUniversalTime().AddHours(2),
                                DateExpired = DateTime.Now.ToUniversalTime().AddHours(2).AddYears(99),
                                Token = finalKey,
                                UserID = key,
                                CompanyID = customer.CompanyID,
                                CustomerNo = customer.CustomerNumber,
                                EncodedToken = encodedToken,
                            };
                            db.Add(wSCustomerLoginToken);
                            db.SaveChanges();
                        }
                        else                                            //User Exits in token table
                        {
                            if (DateTime.Now.ToUniversalTime().AddHours(2) >= wSCustomerLoginToken.DateExpired)            //check if token is already expired, update it with new one
                            {
                                wSCustomerLoginToken.DateCreated = DateTime.Now.ToUniversalTime().AddHours(2);
                                wSCustomerLoginToken.DateExpired = DateTime.Now.ToUniversalTime().AddHours(2).AddYears(99);
                                wSCustomerLoginToken.Token = finalKey;
                                wSCustomerLoginToken.EncodedToken = encodedToken;

                                db.Update(wSCustomerLoginToken);
                                db.SaveChanges();
                            }
                            else                                                //If token is valid return it
                            {
                                finalKey = wSCustomerLoginToken.Token;
                            }

                        }

                        var companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == customer.CompanyID).FirstOrDefault();

                        customer_LoginResult = new Customer_LoginResult()
                        {
                            Result = "Success",
                            Token = finalKey,
                            IsSuccess = true,
                            EncodedToken = encodedToken,
                            CompanyDomain = companySkin.Url
                        };

                        activityLog.UserID = key;

                        activityLog.DateEnded = DateTime.Now;
                        activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_LoginResult);
                        if (!string.IsNullOrEmpty(activityLog.UserID))
                        {
                            db.Add(activityLog);
                            db.SaveChanges();
                        }

                        return Content(JsonConvert.SerializeObject(customer_LoginResult), "application/json");
                    }
                }
            }

            return Content(JsonConvert.SerializeObject(customer_LoginResult), "application/json");

        }

        /// <summary>
        /// Customer Login New
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [ProducesResponseType(typeof(Customer_LoginResult), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/LoginNew")]
        public async Task<IActionResult> Customer_LoginNew()
        {
            Customer_LoginResult customer_LoginResult = new Customer_LoginResult()
            {
                IsSuccess = false,
                Result = "Failed",
                Token = "",
            };

            var db = new Data.MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.Login,
                DateStarted = DateTime.Now.ToUniversalTime().AddHours(2),
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                // Get the encoded username and password
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                // Decode from Base64 to string
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                // Split username and password
                var username = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];


                var result = await _signInManager.PasswordSignInAsync(username, password, false, lockoutOnFailure: false);
                // Check if login is correct
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(username);
                    var customer = db.Customers.Where(p => p.UserID == user.Id).SingleOrDefault();
                    if (customer != null)
                    {
                        string key = user.Id;
                        string token = $"{key}_{DateTime.Now.ToUniversalTime().AddHours(2):yyyyMMddHHmmss}";

                        // Encrypt token
                        SHA256 sha256 = SHA256Managed.Create();
                        byte[] hashValue;
                        UTF8Encoding objUtf8 = new UTF8Encoding();
                        hashValue = sha256.ComputeHash(objUtf8.GetBytes(token));
                        string finalKey = string.Empty;
                        finalKey = Convert.ToBase64String(hashValue);

                        string encodedToken = System.Web.HttpUtility.UrlEncode(finalKey);
                        var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.UserID == key).SingleOrDefault();

                        if (wSCustomerLoginToken == null)               //User doesn't Exits in token table
                        {
                            wSCustomerLoginToken = new WSCustomerLoginToken()
                            {
                                DateCreated = DateTime.Now.ToUniversalTime().AddHours(2),
                                DateExpired = DateTime.Now.ToUniversalTime().AddHours(2).AddYears(99),
                                Token = finalKey,
                                UserID = key,
                                CompanyID = customer.CompanyID,
                                CustomerNo = customer.CustomerNumber,
                                EncodedToken = encodedToken,
                            };
                            db.Add(wSCustomerLoginToken);
                            db.SaveChanges();
                        }
                        else                                            //User Exits in token table
                        {
                            if (DateTime.Now.ToUniversalTime().AddHours(2) >= wSCustomerLoginToken.DateExpired)            //check if token is already expired, update it with new one
                            {
                                wSCustomerLoginToken.DateCreated = DateTime.Now.ToUniversalTime().AddHours(2);
                                wSCustomerLoginToken.DateExpired = DateTime.Now.ToUniversalTime().AddHours(2).AddYears(99);
                                wSCustomerLoginToken.Token = finalKey;
                                wSCustomerLoginToken.EncodedToken = encodedToken;

                                db.Update(wSCustomerLoginToken);
                                db.SaveChanges();
                            }
                            else                                                //If token is valid return it
                            {
                                finalKey = wSCustomerLoginToken.Token;
                            }

                        }

                        var companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == customer.CompanyID).FirstOrDefault();

                        customer_LoginResult = new Customer_LoginResult()
                        {
                            Result = "Success",
                            Token = finalKey,
                            IsSuccess = true,
                            EncodedToken = encodedToken,
                            CompanyDomain = companySkin.Url
                        };

                        activityLog.UserID = key;

                        activityLog.DateEnded = DateTime.Now;
                        activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_LoginResult);
                        if (!string.IsNullOrEmpty(activityLog.UserID))
                        {
                            db.Add(activityLog);
                            db.SaveChanges();
                        }

                        return Content(JsonConvert.SerializeObject(customer_LoginResult), "application/json");
                    }
                }
            }

            return Content(JsonConvert.SerializeObject(customer_LoginResult), "application/json");

        }


        /// <summary>
        /// Customer Logout
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Logout")]
        public async Task<IActionResult> Customer_Logout()
        {
            var db = new Data.MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.LogOff,
                DateStarted = DateTime.Now.ToUniversalTime().AddHours(2),
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (authHeader.StartsWith("Bearer "))
            {
                // Get the encoded username and password
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();

                // Check if login is correct
                var wSCustomerLoginToken = db.WSCustomerLoginTokens.Where(p => p.Token == encodedUsernamePassword).SingleOrDefault();

                if (wSCustomerLoginToken == null)
                    return StatusCode(401);

                var user = db.Users.Where(x => x.Id == wSCustomerLoginToken.UserID).SingleOrDefault();

                if (user.IsDeleted)
                    return StatusCode(401);

                if (wSCustomerLoginToken.DateExpired >= DateTime.Now.ToUniversalTime().AddHours(2))
                {
                    _userEmail = _userManager.FindByIdAsync(wSCustomerLoginToken.UserID).Result.Email;
                    _userID = wSCustomerLoginToken.UserID;
                    wSCustomerLoginToken.DateExpired = DateTime.Now.ToUniversalTime().AddDays(-1);
                    db.Update(wSCustomerLoginToken);
                }
                else
                {
                    return StatusCode(401);
                }

                activityLog.UserID = wSCustomerLoginToken.UserID;

            }

            var customer_LogoutResult = new GenericResult()
            {
                IsSuccess = true,
                Message = "logout successfully"
            };

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_LogoutResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_LogoutResult), "application/json");

        }


        /// <summary>
        /// Customer Profile
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_ProfileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Profile")]
        public async Task<IActionResult> Customer_Profile()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_ProfileResult customer_ProfileResult = new Customer_ProfileResult()
            {
                AccountType = "",
                CustomerNumber = "",
                FullName = "",
                LoginEmail = "",
                OccupancyDate = null,
                PhoneNumber = "",
                ServiceProvider = "",
                CustomerMeterTypes = new List<Customer_ProfileResult.ProfileCustomerMeterType>(),
                ActivateTaxInvoice = null,
                BalanceAndConsumptionSMS = "",
                HighUsageNotifications = null,
                LeakNotifications = null,
                LowBalanceNotification = null,
                LowBalanceNotifications = null,
                NewsLetters = null,
                RecipientAddress = null,
                RecipientName = null,
                RecipientReferenceNumber = null,
                RecipientVatNumber = null,
                ShowCostInclVAT = null,
                ShowHourlyUsage = null,
                ShowDetailedDailyBilling = null,
                IDNumberOrCompanyReg = "",
            };

            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customerNo);
                if (skybillApiCustomer != null)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                    customer_ProfileResult = new Customer_ProfileResult()
                    {
                        AccountType = ((AccountTypeEnum)customer.AccountTypeID).GetDescription(),
                        CustomerNumber = customer.CustomerNumber,
                        FullName = customer.FullName,
                        LoginEmail = user != null ? user.Email : "",
                        OccupancyDate = customer.OccupancyDate,
                        PhoneNumber = customer.PhoneNumber,
                        ServiceProvider = company.Name,
                        CustomerMeterTypes = new List<Customer_ProfileResult.ProfileCustomerMeterType>(),
                        ShowHourlyUsage = customer.ShowDailyUsage,
                        ShowCostInclVAT = customer.ShowCostInclVAT,
                        ShowDetailedDailyBilling = customer.ShowDetailedDailyBilling,
                        RecipientVatNumber = customer.RecipientVATNumber,
                        RecipientReferenceNumber = customer.RecipientReferenceNumber,
                        RecipientName = customer.RecipientName,
                        RecipientAddress = customer.RecipientAddress,
                        NewsLetters = customer.NewsLetters,
                        ActivateTaxInvoice = customer.ActivateTaxInvoice,
                        HighUsageNotifications = customer.HighUsageNotifications,
                        LeakNotifications = customer.LeakNotifications,
                        LowBalanceNotification = customer.DisconnectionLowBalanceNotification1,
                        LowBalanceNotifications = notificationSettings.LowBalanceNotification1,
                        BalanceAndConsumptionSMS = customer.BalanceNotificationSMSType,
                        InfoNotificationType = customer.InfoNotificationTypeID,
                        SystemNotificationType = customer.SystemNotificationTypeID,
                        NotificationEmail = customer.NotificationEmail,
                        IDNumberOrCompanyReg = customer.IDNumberOrCompanyReg
                    };

                    var meters = skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber);
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
                                    string meterType = "";

                                    switch (customerMeterType.Selected)
                                    {
                                        case 1:
                                            meterType = $"Balance ({balanceSymbol})";
                                            break;
                                        case 2:
                                            meterType = "Demand";
                                            break;
                                        case 3:
                                            meterType = "None";
                                            break;
                                        case 4:
                                            meterType = "Solar";
                                            break;
                                    }
                                    customer_ProfileResult.CustomerMeterTypes.Add(new Customer_ProfileResult.ProfileCustomerMeterType()
                                    {
                                        MeterSerial = meter.Serial_No,
                                        UsageMeterSettingsDisplayType = meterType,
                                    });
                                }
                            }
                        }
                    }

                    if (customer_ProfileResult.CustomerMeterTypes.Count == 0 && meters.Count > 0)
                    {
                        customer_ProfileResult.CustomerMeterTypes = meters.Select(x => new Customer_ProfileResult.ProfileCustomerMeterType()
                        {
                            MeterSerial = x.Serial_No,
                            UsageMeterSettingsDisplayType = "Balance",
                        }).ToList();

                    }
                }
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProfileResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }
            return Content(JsonConvert.SerializeObject(customer_ProfileResult), "application/json");

        }

        /// <summary>
        /// Customer Profile Usage Display Settings
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_ProfileUsageDisplaySettingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/ProfileUsageDisplaySettings")]
        public async Task<IActionResult> Customer_ProfileUsageDisplaySettings()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_ProfileUsageDisplaySettingsResult customer_ProfileResult = new Customer_ProfileUsageDisplaySettingsResult()
            {
                ShowCostInclVAT = null,
                ShowHourlyUsage = null,
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customerNo);
                if (skybillApiCustomer != null)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                    customer_ProfileResult = new Customer_ProfileUsageDisplaySettingsResult()
                    {
                        ShowHourlyUsage = customer.ShowDailyUsage,
                        ShowCostInclVAT = customer.ShowCostInclVAT,
                    };

                }
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProfileResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_ProfileResult), "application/json");

        }

        /// <summary>
        /// Customer Profile Tax Invoice Settings
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_TaxInvoiceSettingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/ProfileTaxInvoiceSettings")]
        public async Task<IActionResult> Customer_ProfileTaxInvoiceSettings()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_TaxInvoiceSettingsResult customer_ProfileResult = new Customer_TaxInvoiceSettingsResult()
            {
                ActivateTaxInvoice = null,
                RecipientAddress = null,
                RecipientName = null,
                RecipientReferenceNumber = null,
                RecipientVatNumber = null,
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customerNo);
                if (skybillApiCustomer != null)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                    customer_ProfileResult = new Customer_TaxInvoiceSettingsResult()
                    {
                        RecipientVatNumber = customer.RecipientVATNumber,
                        RecipientReferenceNumber = customer.RecipientReferenceNumber,
                        RecipientName = customer.RecipientName,
                        RecipientAddress = customer.RecipientAddress,
                        ActivateTaxInvoice = customer.ActivateTaxInvoice,
                    };

                }
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProfileResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_ProfileResult), "application/json");

        }

        /// <summary>
        /// Updates Customer Personal Details.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "OccupancyDate": "2022-05-26T04:08:11.378Z",
        ///         "FullName": "string",
        ///         "PhoneNumber": "string",
        ///         "NotificationEmail": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/PersonalDetails_Update")]
        public async Task<IActionResult> Customer_PersonalDetails_Update()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_PersonalDetails_Update customer_PersonalDetails_Update = new Customer_PersonalDetails_Update();

            try
            {
                customer_PersonalDetails_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_PersonalDetails_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_PersonalDetails_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                if (customer_PersonalDetails_Update.OccupancyDate.HasValue && customer_PersonalDetails_Update.OccupancyDate.Value != customer.OccupancyDate)
                {
                    customer.OccupancyDate = customer_PersonalDetails_Update.OccupancyDate.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated OccupancyDate;";
                }
                if (!string.IsNullOrEmpty(customer_PersonalDetails_Update.FullName) && customer_PersonalDetails_Update.FullName != customer.FullName)
                {
                    customer.FullName = customer_PersonalDetails_Update.FullName;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated FullName;";
                }
                if (!string.IsNullOrEmpty(customer_PersonalDetails_Update.PhoneNumber) && customer_PersonalDetails_Update.PhoneNumber != customer.PhoneNumber)
                {
                    try
                    {
                        Convert.ToInt64(customer_PersonalDetails_Update.PhoneNumber);
                        if (customer_PersonalDetails_Update.PhoneNumber.Length != 10)
                        {
                            result.IsSuccess = false;
                            result.Message = result.Message + "Invalid Phone Number;";
                        }
                        else
                        {
                            customer.PhoneNumber = customer_PersonalDetails_Update.PhoneNumber;
                            customer.NotificationPhoneNumber = customer_PersonalDetails_Update.PhoneNumber;
                            result.Message = result.Message + "Updated PhoneNumber;";
                            result.IsSuccess = true;
                        }
                    }
                    catch
                    {
                        result.IsSuccess = false;
                        result.Message = result.Message + "Invalid Phone Number;";
                    }
                }

                if (!string.IsNullOrEmpty(customer_PersonalDetails_Update.NotificationEmail) && customer_PersonalDetails_Update.NotificationEmail != customer.NotificationEmail)
                {
                    try
                    {

                        System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(customer_PersonalDetails_Update.NotificationEmail);
                        customer.NotificationEmail = customer_PersonalDetails_Update.NotificationEmail;
                        result.IsSuccess = true;
                        result.Message = result.Message + "Updated NotificationEmail;";

                    }
                    catch
                    {
                        result.IsSuccess = false;
                        result.Message += "Invalid Email";
                    }
                }

                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";


            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }
            return Json(result);
        }

        /// <summary>
        /// Customer Personal Details
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_PersonalDetailsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/PersonalDetails")]
        public async Task<IActionResult> Customer_PersonalDetails()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_PersonalDetailsResult customer_ProfileResult = new Customer_PersonalDetailsResult()
            {
                AccountType = "",
                CustomerNumber = "",
                FullName = "",
                LoginEmail = "",
                OccupancyDate = null,
                PhoneNumber = "",
                ServiceProvider = "",
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customerNo);
                if (skybillApiCustomer != null)
                {
                    var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                    customer_ProfileResult = new Customer_PersonalDetailsResult()
                    {
                        AccountType = skybillCustomer.AccountType.GetDescription(),
                        CustomerNumber = customer.CustomerNumber,
                        FullName = customer.FullName,
                        LoginEmail = user != null ? user.Email : "",
                        OccupancyDate = customer.OccupancyDate,
                        PhoneNumber = customer.PhoneNumber,
                        ServiceProvider = company.Name,
                    };

                }
                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProfileResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_ProfileResult), "application/json");

        }

        /// <summary>
        /// Customer Recharge History
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_RechargeHistoryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/RechargeHistory")]
        public async Task<IActionResult> Customer_RechargeHistory()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_RechargeHistoryResult customer_ProfileResult = new Customer_RechargeHistoryResult()
            {
                RechargeHistories = new List<Customer_RechargeHistoryResult.RechargeHistory>(),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                StringBuilder sqlQuery = new StringBuilder();

                sqlQuery.AppendLine($"exec [sp_GetReceiptLogTop5] '{company.Name}', '{customerNo}'");

                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand.CommandTimeout = 6000;

                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                List<Customer_RechargeHistoryResult.RechargeHistory> ReceiptLogItems = new List<Customer_RechargeHistoryResult.RechargeHistory>();


                foreach (DataRow dr in dataTable.Rows)
                {
                    if (ReceiptLogItems.Count == 5)
                        break;

                    Customer_RechargeHistoryResult.RechargeHistory item = new Customer_RechargeHistoryResult.RechargeHistory()
                    {
                        Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
                        Date = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
                        PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).GetDescription() : "Unknown",
                    };

                    ReceiptLogItems.Add(item);
                }

                customer_ProfileResult.RechargeHistories = ReceiptLogItems;

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProfileResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_ProfileResult), "application/json");

        }

        /// <summary>
        /// Notification Settings System Notification Types
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(NotificationTypeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/NotificationSettings_SystemNotificationTypes")]
        public async Task<IActionResult> NotificationSettings_SystemNotificationTypes()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new NotificationTypeResult()), "application/json");
        }

        /// <summary>
        /// Notification Settings Info Notification Types
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(NotificationTypeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/NotificationSettings_InfoNotificationTypes")]
        public async Task<IActionResult> NotificationSettings_InfoNotificationTypes()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new NotificationTypeResult()), "application/json");
        }

        /// <summary>
        /// Notification Settings Balance Notification SMS Types
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(BalanceNotificationSMSTypeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/NotificationSettings_BalanceNotificationSMSTypes")]
        public async Task<IActionResult> NotificationSettings_BalanceNotificationSMSTypes()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new BalanceNotificationSMSTypeResult()), "application/json");
        }

        /// <summary>
        /// Updates Customer Notification Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "NotificationEmail": "string",
        ///         "BalanceAndConsumptionSMS": 0,
        ///         "LowBalanceNotification": 0,
        ///         "LowBalanceNotifications": true,
        ///         "HighUsageNotifications": true,
        ///         "LeakNotifications": true,
        ///         "NewsLetters": true,
        ///         "SystemNotificationType": 0,
        ///         "InfoNotificationType": 0
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/NotificationSettings_Update")]
        public async Task<IActionResult> Customer_NotificationSettings_Update()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_NotificationSettings_Update customer_NotificationSettings_Update = new Customer_NotificationSettings_Update();

            try
            {
                customer_NotificationSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_NotificationSettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_NotificationSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                if (!string.IsNullOrEmpty(customer_NotificationSettings_Update.NotificationEmail) && customer_NotificationSettings_Update.NotificationEmail != customer.NotificationEmail)
                {
                    try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(customer_NotificationSettings_Update.NotificationEmail); }
                    catch
                    {
                        result.IsSuccess = false;
                        result.Message = "Invalid Email";

                        return Json(result);
                    }

                    customer.NotificationEmail = customer_NotificationSettings_Update.NotificationEmail;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated NotificationEmail;";
                }

                if (customer_NotificationSettings_Update.NewsLetters.HasValue && customer_NotificationSettings_Update.NewsLetters.Value != customer.NewsLetters)
                {
                    customer.NewsLetters = customer_NotificationSettings_Update.NewsLetters.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated NewsLetters;";
                }

                if (customer_NotificationSettings_Update.HighUsageNotifications.HasValue && customer_NotificationSettings_Update.HighUsageNotifications.Value != customer.HighUsageNotifications)
                {
                    customer.HighUsageNotifications = customer_NotificationSettings_Update.HighUsageNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated HighUsageNotifications;";
                }

                if (customer_NotificationSettings_Update.LeakNotifications.HasValue && customer_NotificationSettings_Update.LeakNotifications.Value != customer.LeakNotifications)
                {
                    customer.LeakNotifications = customer_NotificationSettings_Update.LeakNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LeakNotifications;";
                }

                if (customer_NotificationSettings_Update.LowBalanceNotification.HasValue && customer_NotificationSettings_Update.LowBalanceNotification.Value != customer.DisconnectionLowBalanceNotification1)
                {
                    customer.DisconnectionLowBalanceNotification1 = customer_NotificationSettings_Update.LowBalanceNotification.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LowBalanceNotification;";
                }

                if (customer_NotificationSettings_Update.LowBalanceNotifications.HasValue && customer_NotificationSettings_Update.LowBalanceNotifications.Value != notificationSettings.LowBalanceNotification1)
                {
                    notificationSettings.LowBalanceNotification1 = customer_NotificationSettings_Update.LowBalanceNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LowBalanceNotifications;";
                }

                if (customer_NotificationSettings_Update.BalanceAndConsumptionSMS.HasValue)
                {
                    customer.BalanceNotificationSMSType = ((Data.BalanceAndConsumptionSMSTypeEnum)(customer_NotificationSettings_Update.BalanceAndConsumptionSMS.Value)).ToString();
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated BalanceAndConsumptionSMS;";
                }

                if (customer_NotificationSettings_Update.SystemNotificationType.HasValue)
                {
                    customer.SystemNotificationTypeID = customer_NotificationSettings_Update.SystemNotificationType.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated SystemNotificationType;";
                }

                if (customer_NotificationSettings_Update.InfoNotificationType.HasValue)
                {
                    customer.InfoNotificationTypeID = customer_NotificationSettings_Update.InfoNotificationType.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated InfoNotificationType;";
                }

                db.Update(notificationSettings);
                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Updates Customer Notification Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Email": "string",
        ///         "NotificationEmail": "string",
        ///         "BalanceAndConsumptionSMS": 0,
        ///         "LowBalanceNotification": 0,
        ///         "LowBalanceNotifications": true,
        ///         "HighUsageNotifications": true,
        ///         "LeakNotifications": true,
        ///         "NewsLetters": true,
        ///         "SystemNotificationType": 0,
        ///         "InfoNotificationType": 0
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/NotificationSettings_Update_New")]
        public async Task<IActionResult> Customer_NotificationSettings_Update_New()
        {
            string customerNo = "";
            int companyID = 0;

            //if (!IsAuthenticated(out customerNo, out companyID))
            //    return StatusCode(401);


            StreamReader reader = new StreamReader(Request.Body);

            Customer_NotificationSettings_Update_New customer_NotificationSettings_Update = new Customer_NotificationSettings_Update_New();

            try
            {
                customer_NotificationSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_NotificationSettings_Update_New>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            var user = await _userManager.FindByEmailAsync(customer_NotificationSettings_Update.Email);

            if (user == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            // Check if login is correct
            var db = new Data.MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.UserID == user.Id
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer == null)
                return StatusCode(400);

            customerNo = customer.CustomerNumber;
            companyID = customer.CompanyID;
            _userEmail = user.UserName;
            _userID = user.Id;


            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_NotificationSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };


            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == customer.MeterNumber).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                if (!string.IsNullOrEmpty(customer_NotificationSettings_Update.NotificationEmail) && customer_NotificationSettings_Update.NotificationEmail != customer.NotificationEmail)
                {
                    try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(customer_NotificationSettings_Update.NotificationEmail); }
                    catch
                    {
                        result.IsSuccess = false;
                        result.Message = "Invalid Email";

                        return Json(result);
                    }

                    customer.NotificationEmail = customer_NotificationSettings_Update.NotificationEmail;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated NotificationEmail;";
                }

                if (customer_NotificationSettings_Update.NewsLetters.HasValue && customer_NotificationSettings_Update.NewsLetters.Value != customer.NewsLetters)
                {
                    customer.NewsLetters = customer_NotificationSettings_Update.NewsLetters.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated NewsLetters;";
                }

                if (customer_NotificationSettings_Update.HighUsageNotifications.HasValue && customer_NotificationSettings_Update.HighUsageNotifications.Value != customer.HighUsageNotifications)
                {
                    customer.HighUsageNotifications = customer_NotificationSettings_Update.HighUsageNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated HighUsageNotifications;";
                }

                if (customer_NotificationSettings_Update.LeakNotifications.HasValue && customer_NotificationSettings_Update.LeakNotifications.Value != customer.LeakNotifications)
                {
                    customer.LeakNotifications = customer_NotificationSettings_Update.LeakNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LeakNotifications;";
                }

                if (customer_NotificationSettings_Update.LowBalanceNotification.HasValue && customer_NotificationSettings_Update.LowBalanceNotification.Value != customer.DisconnectionLowBalanceNotification1)
                {
                    customer.DisconnectionLowBalanceNotification1 = customer_NotificationSettings_Update.LowBalanceNotification.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LowBalanceNotification;";
                }

                if (customer_NotificationSettings_Update.LowBalanceNotifications.HasValue && customer_NotificationSettings_Update.LowBalanceNotifications.Value != notificationSettings.LowBalanceNotification1)
                {
                    notificationSettings.LowBalanceNotification1 = customer_NotificationSettings_Update.LowBalanceNotifications.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated LowBalanceNotifications;";
                }

                if (customer_NotificationSettings_Update.BalanceAndConsumptionSMS.HasValue)
                {
                    customer.BalanceNotificationSMSType = ((Data.BalanceAndConsumptionSMSTypeEnum)(customer_NotificationSettings_Update.BalanceAndConsumptionSMS.Value)).ToString();
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated BalanceAndConsumptionSMS;";
                }

                if (customer_NotificationSettings_Update.SystemNotificationType.HasValue)
                {
                    customer.SystemNotificationTypeID = customer_NotificationSettings_Update.SystemNotificationType.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated SystemNotificationType;";
                }

                if (customer_NotificationSettings_Update.InfoNotificationType.HasValue)
                {
                    customer.InfoNotificationTypeID = customer_NotificationSettings_Update.InfoNotificationType.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated InfoNotificationType;";
                }

                db.Update(notificationSettings);
                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Updates Customer Tax Invoice Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "ActivateTaxInvoice": true,
        ///         "RecipientName": "string",
        ///         "RecipientAddress": "string",
        ///         "RecipientVatNumber": "string",
        ///         "RecipientReferenceNumber": "string",
        ///         "IDNumberOrCompanyReg": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/TaxInvoiceSettings_Update")]
        public async Task<IActionResult> Customer_TaxInvoiceSettings_Update()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_TaxInvoiceSettings_Update customer_TaxInvoiceSettings_Update = new Customer_TaxInvoiceSettings_Update();

            try
            {
                customer_TaxInvoiceSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_TaxInvoiceSettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_TaxInvoiceSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {

                //if (!string.IsNullOrEmpty(customer_TaxInvoiceSettings_Update.RecipientVatNumber) && customer_TaxInvoiceSettings_Update.RecipientVatNumber != customer.RecipientVATNumber)
                {
                    customer.RecipientVATNumber = customer_TaxInvoiceSettings_Update.RecipientVatNumber;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientVatNumber;";
                }

                //if (!string.IsNullOrEmpty(customer_TaxInvoiceSettings_Update.RecipientReferenceNumber) && customer_TaxInvoiceSettings_Update.RecipientReferenceNumber != customer.RecipientReferenceNumber)
                {
                    customer.RecipientReferenceNumber = customer_TaxInvoiceSettings_Update.RecipientReferenceNumber;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientReferenceNumber;";
                }

                //if (!string.IsNullOrEmpty(customer_TaxInvoiceSettings_Update.RecipientName) && customer_TaxInvoiceSettings_Update.RecipientName != customer.RecipientName)
                {
                    customer.RecipientName = customer_TaxInvoiceSettings_Update.RecipientName;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientName;";
                }

                //if (!string.IsNullOrEmpty(customer_TaxInvoiceSettings_Update.RecipientAddress) && customer_TaxInvoiceSettings_Update.RecipientAddress != customer.RecipientAddress)
                {
                    customer.RecipientAddress = customer_TaxInvoiceSettings_Update.RecipientAddress;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientAddress;";
                }

                if (customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.HasValue && customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.Value != customer.ActivateTaxInvoice)
                {
                    customer.ActivateTaxInvoice = customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated ActivateTaxInvoice;";
                }

                //if (!string.IsNullOrEmpty(customer_TaxInvoiceSettings_Update.IDNumberOrCompanyReg) && customer_TaxInvoiceSettings_Update.IDNumberOrCompanyReg != customer.IDNumberOrCompanyReg)
                {
                    customer.IDNumberOrCompanyReg = customer_TaxInvoiceSettings_Update.IDNumberOrCompanyReg;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated IDNumberOrCompanyReg;";
                }

                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Updates Customer Tax Invoice Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "ActivateTaxInvoice": true,
        ///         "RecipientName": "string",
        ///         "RecipientAddress": "string",
        ///         "RecipientVatNumber": "string",
        ///         "RecipientReferenceNumber": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/TaxInvoiceSettings_UpdateNew")]
        public async Task<IActionResult> Customer_TaxInvoiceSettings_UpdateNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_TaxInvoiceSettings_Update customer_TaxInvoiceSettings_Update = new Customer_TaxInvoiceSettings_Update();

            try
            {
                customer_TaxInvoiceSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_TaxInvoiceSettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_TaxInvoiceSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                if (customer_TaxInvoiceSettings_Update.RecipientVatNumber != null && customer_TaxInvoiceSettings_Update.RecipientVatNumber != customer.RecipientVATNumber)
                {
                    customer.RecipientVATNumber = customer_TaxInvoiceSettings_Update.RecipientVatNumber;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientVatNumber;";
                }

                if (customer_TaxInvoiceSettings_Update.RecipientReferenceNumber != null && customer_TaxInvoiceSettings_Update.RecipientReferenceNumber != customer.RecipientReferenceNumber)
                {
                    customer.RecipientReferenceNumber = customer_TaxInvoiceSettings_Update.RecipientReferenceNumber;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientReferenceNumber;";
                }

                if (customer_TaxInvoiceSettings_Update.RecipientName != null && customer_TaxInvoiceSettings_Update.RecipientName != customer.RecipientName)
                {
                    customer.RecipientName = customer_TaxInvoiceSettings_Update.RecipientName;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientName;";
                }

                if (customer_TaxInvoiceSettings_Update.RecipientAddress != null && customer_TaxInvoiceSettings_Update.RecipientAddress != customer.RecipientAddress)
                {
                    customer.RecipientAddress = customer_TaxInvoiceSettings_Update.RecipientAddress;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated RecipientAddress;";
                }

                if (customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.HasValue && customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.Value != customer.ActivateTaxInvoice)
                {
                    customer.ActivateTaxInvoice = customer_TaxInvoiceSettings_Update.ActivateTaxInvoice.Value;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated ActivateTaxInvoice;";
                }

                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Updates Customer Usage Display Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "ShowHourlyUsage": true,
        ///         "ShowCostInclVAT": true,
        ///         "showDetailedDailyBilling": true,
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/UsageDisplaySettings_Update")]
        public async Task<IActionResult> Customer_UsageDisplaySettings_Update()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_UsageDisplaySettings_Update customer_UsageSettings_Update = new Customer_UsageDisplaySettings_Update();

            try
            {
                customer_UsageSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_UsageDisplaySettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_UsageSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                if (customer_UsageSettings_Update.ShowHourlyUsage.HasValue && customer_UsageSettings_Update.ShowHourlyUsage != customer.ShowDailyUsage)
                {
                    customer.ShowDailyUsage = customer_UsageSettings_Update.ShowHourlyUsage;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated ShowHourlyUsage;";
                }

                if (customer_UsageSettings_Update.ShowCostInclVAT.HasValue && customer_UsageSettings_Update.ShowCostInclVAT != customer.ShowCostInclVAT)
                {
                    customer.ShowCostInclVAT = customer_UsageSettings_Update.ShowCostInclVAT;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated ShowCostInclVAT;";
                }

                if (customer_UsageSettings_Update.ShowDetailedDailyBilling.HasValue && customer_UsageSettings_Update.ShowDetailedDailyBilling != customer.ShowDetailedDailyBilling)
                {
                    customer.ShowDetailedDailyBilling = customer_UsageSettings_Update.ShowDetailedDailyBilling;
                    result.IsSuccess = true;
                    result.Message = result.Message + "Updated ShowDetailedDailyBilling;";
                }

                db.Update(customer);
                db.SaveChanges();
                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Gets Usage Meter Settings for Customer
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_UsageMeterSettings_CustomerResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/UsageMeterSettings_Customer")]
        public async Task<IActionResult> UsageMeterSettings_Customer()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            Customer_UsageMeterSettings_CustomerResult customer_UsageMeterSettings_CustomerResult = new Customer_UsageMeterSettings_CustomerResult()
            {
                CustomerMeterTypes = new List<Customer_UsageMeterSettings_CustomerResult.UsageMeterSettings_CustomerCustomerMeterType>(),
            };

            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customerNo);
                if (skybillApiCustomer != null)
                {
                    var meters = skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber);
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
                                    string meterType = "";

                                    switch (customerMeterType.Selected)
                                    {
                                        case 1:
                                            meterType = $"Balance ({balanceSymbol})";
                                            break;
                                        case 2:
                                            meterType = "Demand";
                                            break;
                                        case 3:
                                            meterType = "None";
                                            break;
                                        case 4:
                                            meterType = "Solar";
                                            break;
                                    }
                                    customer_UsageMeterSettings_CustomerResult.CustomerMeterTypes.Add(new Customer_UsageMeterSettings_CustomerResult.UsageMeterSettings_CustomerCustomerMeterType()
                                    {
                                        MeterSerial = meter.Serial_No,
                                        UsageMeterSettingsDisplayTypeName = meterType,
                                        UsageMeterSettingsDisplayTypeID = customerMeterType.Selected,
                                    });
                                }
                            }
                        }
                    }


                }
                activityLog.UserID = user.Id;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_UsageMeterSettings_CustomerResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }
            return Content(JsonConvert.SerializeObject(customer_UsageMeterSettings_CustomerResult), "application/json");

        }

        /// <summary>
        /// Updates Customer Usage Meter Settings
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "MeterSerial": "string",
        ///         "UsageMeterSettingsDisplayTypeID": 0
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/UsageMeterSettings_Update")]
        public async Task<IActionResult> Customer_UsageMeterSettings_Update()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_UsageMeterSettings_Update customer_UsageMeterSettings_Update = new Customer_UsageMeterSettings_Update();

            try
            {
                customer_UsageMeterSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_UsageMeterSettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_UsageMeterSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var meters = skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber);
                foreach (var meter in meters)
                {
                    if (meter.Serial_No.ToUpper() != customer_UsageMeterSettings_Update.MeterSerial.ToUpper())
                        continue;

                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                    if (device != null)
                    {
                        var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();
                        if (customerMeter != null)
                        {
                            var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                            if (customerMeterType != null)
                            {
                                if (customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.HasValue && (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value != customerMeterType.Selected)
                                {
                                    customerMeterType.Selected = (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value;
                                    result.IsSuccess = true;
                                    result.Message = result.Message + $"Updated UsageMeterSettingsDisplayTypeID - {meter.Serial_No};";
                                }

                                db.Update(customerMeterType);
                                db.SaveChanges();
                            }
                        }
                    }
                }

                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }


        /// <summary>
        /// Updates Customer Usage Meter Settings New
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "MeterSerial": "string",
        ///         "UsageMeterSettingsDisplayTypeID": 0
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/UsageMeterSettings_UpdateNew")]
        public async Task<IActionResult> Customer_UsageMeterSettings_UpdateNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);

            Customer_UsageMeterSettings_Update customer_UsageMeterSettings_Update = new Customer_UsageMeterSettings_Update();

            try
            {
                customer_UsageMeterSettings_Update = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_UsageMeterSettings_Update>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_UsageMeterSettings_Update),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (customer != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var meters = skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber);
                foreach (var meter in meters)
                {
                    if (meter.Serial_No.ToUpper() != customer_UsageMeterSettings_Update.MeterSerial.ToUpper())
                        continue;

                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
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
                                MeterTypeID = (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value,
                                Selected = (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value,
                            };
                            db.Add(customerMeterType);
                            db.SaveChanges();
                            result.IsSuccess = true;
                            result.Message = result.Message + $"Updated UsageMeterSettingsDisplayTypeID - {meter.Serial_No};";
                        }

                        else if (customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.HasValue && (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value != customerMeterType.Selected)
                        {
                            customerMeterType.Selected = (int)customer_UsageMeterSettings_Update.UsageMeterSettingsDisplayTypeID.Value;
                            result.IsSuccess = true;
                            result.Message = result.Message + $"Updated UsageMeterSettingsDisplayTypeID - {meter.Serial_No};";
                            db.Update(customerMeterType);
                            db.SaveChanges();
                        }

                    }
                }

                activityLog.UserID = customer.UserID;
            }

            if (string.IsNullOrEmpty(result.Message))
                result.Message = "No changes detected";

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);
        }

        /// <summary>
        /// Gets Usage Meter Settings Display Types for Update
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(MeterTypeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/UsageMeterSettings_DisplayTypes")]
        public async Task<IActionResult> UsageMeterSettings_DisplayTypes()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new MeterTypeResult()), "application/json");
        }

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Email": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/ForgotPassword")]
        public async Task<IActionResult> Customer_ForgotPassword()
        {
            StreamReader reader = new StreamReader(Request.Body);

            Customer_ForgotPassword customer_ForgotPassword = new Customer_ForgotPassword();

            try
            {
                customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ForgotPassword>(reader.ReadToEndAsync().Result);
            }
            catch
            {
                return StatusCode(400, "Invalid post");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };
            var db = new MyVoltageDbContext(_options);
            var user = await _userManager.FindByEmailAsync(customer_ForgotPassword.Email);
            var customer = db.Customers.Where(p => p.UserID == user.Id).FirstOrDefault();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            if (user != null)
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await _emailSender.SendResetPasswordEmailNewAsync(customer_ForgotPassword.Email, $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>", customer?.FullName);
                result = new GenericResult()
                {
                    IsSuccess = true,
                    Message = "Password reset email sent.",
                };
                activityLog.UserID = user.Id;
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

        /// <summary>
        /// Customer Account Details
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_AccountDetailsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/AccountDetails")]
        public async Task<IActionResult> Customer_AccountDetails()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);


            Customer_AccountDetailsResult customer_AccountDetailsResult = new Customer_AccountDetailsResult()
            {
                CustomerBalance = 0,
                CustomerLastPaymentAmount = null,
                CustomerLastPaymentDate = null,
                CustomerLastPaymentMethod = "",
            };

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                int multiplier = -1;
                if (((AccountTypeEnum)customer.AccountTypeID).GetDescription().ToUpper().Contains("POST"))
                {
                    multiplier = 1;
                }

                customer_AccountDetailsResult.CustomerBalance = (decimal)skyBillApiClient.GetCustomer(customer.CustomerNumber).Balance_LCY * multiplier;

                List<Tuple<string, decimal, DateTime>> payments = new List<Tuple<string, decimal, DateTime>>();

                UniPin latestUnipin = null;
                foreach (var meter in skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber))
                {
                    var thisMeterlatestUnipin = db.UniPins.Where(p => p.MeterNumber == skybillCustomer.Serial_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    if (thisMeterlatestUnipin != null)
                        latestUnipin = thisMeterlatestUnipin;
                }
                if (latestUnipin != null)
                {
                    payments.Add(new Tuple<string, decimal, DateTime>("UniPin", latestUnipin.Amount, latestUnipin.CreateDate));
                }

                var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;
                if (latestSage != null)
                {
                    payments.Add(new Tuple<string, decimal, DateTime>("SagePay", latestSage.Amount, latestSage.CreateDate));
                }

                var latestDirectDeposit = db.NetcashManualPayments.Where(p => p.CustomerNo == customer.CustomerNumber).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (latestDirectDeposit != null)
                {
                    var netcashStatement = db.NetcashStatements.Where(p => p.ID == latestDirectDeposit.NetcashStatementID).SingleOrDefault();
                    payments.Add(new Tuple<string, decimal, DateTime>("Direct Deposit", netcashStatement.Amount, netcashStatement.Date));
                }

                if (payments.Count != 0)
                {
                    var latestPayment = payments.OrderByDescending(p => p.Item3).FirstOrDefault();
                    customer_AccountDetailsResult.CustomerLastPaymentAmount = latestPayment.Item2;
                    customer_AccountDetailsResult.CustomerLastPaymentDate = latestPayment.Item3;
                    customer_AccountDetailsResult.CustomerLastPaymentMethod = latestPayment.Item1;
                }

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_AccountDetailsResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_AccountDetailsResult), "application/json");

        }

        /// <summary>
        /// Customer Product Summary
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_ProductSummaryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/ProductSummary")]
        public async Task<IActionResult> Customer_ProductSummary()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }


            Customer_ProductSummaryResult customer_ProductSummaryResult = new Customer_ProductSummaryResult()
            {
                BillingMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1),
                CustomerProductsResourceLedgersForMonthItems = new List<Customer_ProductSummaryResult.CustomerProductsResourceLedgersForMonthItem>(),
                CustomTitle = "",
                ShowIncVAT = false,
            };

            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                if (customer != null && customer.ShowCostInclVAT.HasValue)
                    customer_ProductSummaryResult.ShowIncVAT = customer.ShowCostInclVAT.Value;

                var resourceEntriesPerProductForCustomer = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                                            where p.CompanyID == customer.CompanyID
                                                            && p.CustomerNo == customer.CustomerNumber
                                                            && p.Month == customer_ProductSummaryResult.BillingMonth
                                                            select p).ToList();
                var products = db.SiteAdmin_Products.ToList();

                foreach (var entry in resourceEntriesPerProductForCustomer)
                {
                    var prod = products.Where(p => p.ID == entry.ProductID).SingleOrDefault();
                    Customer_ProductSummaryResult.CustomerProductsResourceLedgersForMonthItem item = new Customer_ProductSummaryResult.CustomerProductsResourceLedgersForMonthItem()
                    {
                        ID = prod.ID,
                        CostOfSalesLinkID = prod.CostOfSalesLinkID,
                        CreatedByID = prod.CreatedByID,
                        DateCreated = prod.DateCreated,
                        DateUpdated = prod.DateUpdated,
                        IncludeInC602x = prod.IncludeInC602x,
                        ProductName = prod.ProductName,
                        SalesLinkID = prod.SalesLinkID,
                        Total = entry.Amount * -1.0m,
                        Units = entry.Quantity * -1.0m,
                        UpdatedByID = prod.UpdatedByID,
                        ShowIncVAT = customer_ProductSummaryResult.ShowIncVAT,
                        ChargeTypeID = prod.ChargeTypeID,
                    };

                    customer_ProductSummaryResult.CustomerProductsResourceLedgersForMonthItems.Add(item);
                }

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ProductSummaryResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Update(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_ProductSummaryResult), "application/json");

        }

        /// <summary>
        /// Customer Meter List
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_MeterListResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/MeterList")]
        public async Task<IActionResult> Customer_MeterList()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            Customer_MeterListResult customer_MeterListResult = new Customer_MeterListResult()
            {
                Devices = new List<Customer_MeterListResult.MeterItem>(),
                ShowVerticalLayout = false,
            };

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var user = db.Users.Where(p => p.Id == customer.UserID).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(customer.CustomerNumber);

                int multiplier = -1;
                if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("POST"))
                {
                    multiplier = 1;
                }

                bool showInVat = false;

                if (customer != null)
                {
                    if (customer.ShowCostInclVAT.HasValue)
                        showInVat = customer.ShowCostInclVAT.Value;
                }

                OperationalBillingProvider _billingProvider = new OperationalBillingProvider(_cache, customerNo, customer.AccountTypeID, company.Name);

                foreach (var meter in skyBillApiClient.GetMetersByCustomer(customer.CustomerNumber))
                {
                    var existing = customer_MeterListResult.Devices.Where(p => p.MeterNumber == meter.Serial_No).SingleOrDefault();
                    if (existing != null)
                        continue;

                    var localDevice = db.Devices.Where(p => p.Serial == meter.Serial_No && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).FirstOrDefault();
                    if (localDevice == null)
                        continue;

                    var m2mDevice = _client.GetDeviceByID(localDevice.DeviceIDLinked);

                    if (m2mDevice != null && m2mDevice.device != null)
                    {
                        string contactorState = _client.IsDeviceContactorConnected(m2mDevice.device.id) ? "Connected" : "Disconnected";
                        // If i find hack, change here for serial number
                        string disconnectionType = "Unknown";

                        var customerNotificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == meter.Serial_No).FirstOrDefault();
                        if (customerNotificationSettings != null)
                        {
                            disconnectionType = customerNotificationSettings.AutoDisconnect ? "Auto" : "Manual";
                        }

                        List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meter.Serial_No, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)customer.AccountTypeID, showInVat);
                        Decimal monthlyTotal = dailyTotals.Sum() * multiplier;

                        if (m2mDevice.device.type.id != (int)DeviceType.DeviceTypeEnum.Electricity)
                            contactorState = "Not Applicable";

                        var aco_StatusHack = (from p in db.SiteAdmin_ContactorStateHacks
                                              where p.MeterSerial == meter.Serial_No
                                              select p).FirstOrDefault();
                        if (aco_StatusHack != null)
                            contactorState = aco_StatusHack.ContactorIsOnline ? "Connected" : "Disconnected";

                        customer_MeterListResult.Devices.Add(new Customer_MeterListResult.MeterItem()
                        {
                            ContactorState = contactorState,
                            DisconnectionType = disconnectionType,
                            LastComm = m2mDevice.device.status != null && m2mDevice.device.status.time.HasValue ? m2mDevice.device.status.time.Value : DateTime.MinValue,
                            MeterNumber = m2mDevice.device.serial,
                            MeterType = m2mDevice.device.type != null ? m2mDevice.device.type.name : "",
                            UnitType = m2mDevice.device.type != null ? m2mDevice.device.type.UnitType : "",
                            MonthlyTotal = monthlyTotal,
                            Name = m2mDevice.device.name,
                            Status = m2mDevice.device.deviceStatus
                        });
                    }
                }


                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(customer_MeterListResult);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(customer_MeterListResult), "application/json");

        }

        /// <summary>
        /// Customer Statement
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpPost]
        [Route("/WebServices/Customer/Statement")]
        public async Task<IActionResult> Customer_Statement()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Statement.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/statement_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Statement
        /// </summary>
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpGet]
        [Route("/WebServices/Customer/GetStatement")]
        public async Task<IActionResult> GetCustomer_Statement()
        {
            string customerNo = "";
            int companyID = 0;
            string token = Request.Query["token"].ToString();

            if (!string.IsNullOrEmpty(token) && !IsTokenValid(token, out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try
            {
                customer_ForgotPassword = new Customer_Statement
                {
                    Month = Convert.ToInt32(Request.Query["month"]),
                    Year = Convert.ToInt32(Request.Query["year"])
                };
            }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Statement.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/statement_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Statement New
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/StatementNew")]
        public async Task<IActionResult> Customer_StatementNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/statement_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";

                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return Json(new { data = statementBytes, filename = statementFileName });
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Tax Invoice
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpPost]
        [Route("/WebServices/Customer/TaxInvoice")]
        public async Task<IActionResult> Customer_TaxInvoice()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/invoice_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 130;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Tax Invoice
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpGet]
        [Route("/WebServices/Customer/GetTaxInvoice")]
        public async Task<IActionResult> GetCustomer_TaxInvoice()
        {
            string customerNo = "";
            int companyID = 0;
            string token = Request.Query["token"].ToString();

            if (!string.IsNullOrEmpty(token) && !IsTokenValid(token, out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try
            {
                customer_ForgotPassword = new Customer_Statement
                {
                    Month = Convert.ToInt32(Request.Query["month"]),
                    Year = Convert.ToInt32(Request.Query["year"])
                };
            }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/invoice_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 130;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Tax Invoice
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/TaxInvoiceNew")]
        public async Task<IActionResult> Customer_TaxInvoiceNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/invoice_view?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 130;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }

                    return Json(new { data = statementBytes, filename = statementFileName });
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Products
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(ProductsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Products")]
        public async Task<IActionResult> Products()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            ProductsResult result = new ProductsResult()
            {
                Product = new List<ListItem>(),
            };

            var products = db.SiteAdmin_Products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption).OrderBy(p => p.ProductName).ToList();

            #region Add Products that was billed

            foreach (var product in products.ToList())
            {
                DateTime startDateMonthly = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1);
                DateTime endDateMonthly = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                List<Models.ClientzoneModels.ConsumptionInsightsModel.ProductItem.BillingItem> billingItems = new List<Models.ClientzoneModels.ConsumptionInsightsModel.ProductItem.BillingItem>();
                DateTime current = startDateMonthly;
                var entries = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                               where p.CompanyID == companyID
                               && p.CustomerNo == customerNo
                               //&& p.Month == current
                               select p).ToList();

                while (current <= endDateMonthly)
                {
                    var entry = (from p in entries
                                 where p.CompanyID == companyID
                                 && p.CustomerNo == customerNo
                                 && p.Month == current
                                 && p.ProductID == product.ID
                                 select p).SingleOrDefault();

                    if (entry != null)
                    {
                        Models.ClientzoneModels.ConsumptionInsightsModel.ProductItem.BillingItem item = new Models.ClientzoneModels.ConsumptionInsightsModel.ProductItem.BillingItem()
                        {
                            Amount = entry.Amount,
                            Date = current,
                            Units = entry.Quantity,
                        };

                        item.Amount = item.Amount * -1.0m;
                        item.Units = item.Units * -1.0m;
                        billingItems.Add(item);
                    }
                    current = current.AddMonths(1);
                }

                if (billingItems.Count != 0)
                {
                    var productName = product.ProductName;
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                    }

                    result.Product.Add(new ListItem()
                    {
                        DisplayText = productName,
                        Value = product.ID,
                    });
                }
            }

            #endregion

            #region If no products was billed, add products for meters' types linked to customer

            if (result.Product.Count == 0)
            {
                List<string> skybillSerials = (from p in db.SkybillCustomers
                                               where p.Customer_No == customerNo
                                               select p.Serial_No).Distinct().ToList();


                if (skybillSerials.Count > 0)
                {
                    var localDevs = (from p in db.Devices
                                     where skybillSerials.Contains(p.Serial)
                                     select p).ToList();

                    foreach (var meter in localDevs)
                    {
                        SiteAdmin_Product product = null;
                        switch (meter.MeterType)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Electricity).FirstOrDefault();
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Gas).FirstOrDefault();
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Water).FirstOrDefault();
                                break;
                        }

                        if (product != null)
                        {
                            var productName = product.ProductName;
                            if (!string.IsNullOrEmpty(productName))
                            {
                                productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                            }
                            if (result.Product.Where(p => p.DisplayText == productName).Count() == 0)
                                result.Product.Add(new ListItem()
                                {
                                    DisplayText = productName,
                                    Value = product.ID,
                                });
                        }
                    }
                }
            }

            #endregion

            #region If still no products, just add all consumption products

            if (result.Product.Count == 0)
            {
                foreach (var product in products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption).ToList())
                {
                    var productName = product.ProductName;
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                    }

                    result.Product.Add(new ListItem()
                    {
                        DisplayText = productName,
                        Value = product.ID,
                    });
                }
            }

            #endregion

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Customer Consumption Insights
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///         "ProductID": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpPost]
        [Route("/WebServices/Customer/ConsumptionInsights")]
        public async Task<IActionResult> Customer_ConsumptionInsights()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ConsumptionInsights customer_ForgotPassword = new Customer_ConsumptionInsights();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ConsumptionInsights>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/consumptioninsightsdiv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&p={customer_ForgotPassword.ProductID}&PV=t&SV={customer.ShowCostInclVAT}&isDaily={customer_ForgotPassword.IsDaily}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Consumption Insights
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpGet]
        [Route("/WebServices/Customer/GetConsumptionInsights")]
        public async Task<IActionResult> GetCustomer_ConsumptionInsights()
        {
            string customerNo = "";
            int companyID = 0;

            string token = Request.Query["token"].ToString();

            if (!string.IsNullOrEmpty(token) && !IsTokenValid(token, out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ConsumptionInsights customer_ForgotPassword = new Customer_ConsumptionInsights();
            try
            {
                customer_ForgotPassword = new Customer_ConsumptionInsights
                {
                    Month = Convert.ToInt32(Request.Query["month"]),
                    Year = Convert.ToInt32(Request.Query["year"]),
                    ProductID = Convert.ToInt32(Request.Query["productid"]),
                    IsDaily = Request.Query["isDaily"],
                };
            }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/consumptioninsightsdiv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&p={customer_ForgotPassword.ProductID}&PV=t&SV={customer.ShowCostInclVAT}&isDaily={customer_ForgotPassword.IsDaily}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Consumption Insights New 
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///         "ProductID": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/ConsumptionInsightsNew")]
        public async Task<IActionResult> Customer_ConsumptionInsightsNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ConsumptionInsights customer_ForgotPassword = new Customer_ConsumptionInsights();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ConsumptionInsights>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/consumptioninsightsdiv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&p={customer_ForgotPassword.ProductID}&PV=t&SV={customer.ShowCostInclVAT}&isDaily={customer_ForgotPassword.IsDaily}";
                byte[] statementBytes = null;
                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return Json(new { data = statementBytes, filename = statementFileName });
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Billing Insights
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpPost]
        [Route("/WebServices/Customer/BillingInsights")]
        public async Task<IActionResult> Customer_BillingInsights()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/currentbillingcyclediv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Billing Insights
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/pdf")]
        [HttpGet]
        [Route("/WebServices/Customer/GetBillingInsights")]
        public async Task<IActionResult> GetCustomer_BillingInsights()
        {
            string customerNo = "";
            int companyID = 0;
            string token = Request.Query["token"].ToString();

            if (!string.IsNullOrEmpty(token) && !IsTokenValid(token, out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try
            {
                customer_ForgotPassword = new Customer_Statement
                {
                    Month = Convert.ToInt32(Request.Query["month"]),
                    Year = Convert.ToInt32(Request.Query["year"])
                };
            }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}_TI.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/currentbillingcyclediv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return File(statementBytes, "application/pdf", statementFileName);
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Billing Insights New
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/BillingInsightsNew")]
        public async Task<IActionResult> Customer_BillingInsightsNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Statement customer_ForgotPassword = new Customer_Statement();
            try { customer_ForgotPassword = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ForgotPassword == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ForgotPassword),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                DateTime StatementMonth = new DateTime(customer_ForgotPassword.Year, customer_ForgotPassword.Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(customerNo.Replace("/", "_"))}_{StatementMonth:yyyy_MM}";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/currentbillingcyclediv?d={StatementMonth.ToDateShort()}&CID={companyID}&CN={customerNo}&PV=t";

                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                if (!string.IsNullOrEmpty(converter.ConversionResult.ConsoleLog))
                    Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    activityLog.UserID = customer.UserID;
                    activityLog.DateEnded = DateTime.Now;
                    if (!string.IsNullOrEmpty(activityLog.UserID))
                    {
                        db.Add(activityLog);
                        db.SaveChanges();
                    }
                    return Json(new { data = statementBytes, filename = statementFileName });
                }

            }

            return StatusCode(404, $"Error generating file");

        }

        /// <summary>
        /// Customer Contact Us
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Name": "string",
        ///         "Email": "string",
        ///         "PhoneNumber": "string",
        ///         "Message": "string",
        ///         "FileName": "string",
        ///         "FileBytes": "string (Convert each byte to Int32 and append to string using . as seperator eg (1.2.3.4))"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/ContactUs")]
        public async Task<IActionResult> Customer_ContactUs()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ContactUs customer_ContactUs = new Customer_ContactUs();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { customer_ContactUs = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ContactUs>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ContactUs == null)
                return StatusCode(400, "Invalid post");

            if (string.IsNullOrEmpty(customer_ContactUs.Name)
                || string.IsNullOrEmpty(customer_ContactUs.Email)
                || string.IsNullOrEmpty(customer_ContactUs.PhoneNumber)
                || string.IsNullOrEmpty(customer_ContactUs.Message)
                )
            {
                result = new GenericResult()
                {
                    IsSuccess = false,
                    Message = "Missing info: ",
                };

                if (string.IsNullOrEmpty(customer_ContactUs.Name))
                    result.Message = result.Message + "Name;";

                if (string.IsNullOrEmpty(customer_ContactUs.Email))
                    result.Message = result.Message + "Email;";

                if (string.IsNullOrEmpty(customer_ContactUs.PhoneNumber))
                    result.Message = result.Message + "PhoneNumber;";

                if (string.IsNullOrEmpty(customer_ContactUs.Message))
                    result.Message = result.Message + "Message;";

                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ContactUs),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var customerMeters = skyBillApiClient.GetMetersByCustomer(customerNo);
                var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();
                string meterStr = "";
                foreach (var c in customerMeters)
                {
                    meterStr = meterStr + c.Serial_No + "<br/>";
                }

                String email = "User<br/> Email: " + _userEmail + "<br/>" +
                              " Contact Email: " + customer_ContactUs.Email + "<br/>" +
                              " Contact Name: " + customer_ContactUs.Name + "<br/>" +
                              " Contact Phone Number: " + customer_ContactUs.PhoneNumber + "<br/>" +
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
                              " Message: " + customer_ContactUs.Message + "<br/>";


                string contentType = "";
                byte[] fileContents = null;
                string filename = "";
                if (!string.IsNullOrEmpty(customer_ContactUs.AttachmentFileBytes) && !string.IsNullOrEmpty(customer_ContactUs.AttachmentFileName))
                {
                    string[] byteToConvert = customer_ContactUs.AttachmentFileBytes.Split('.');
                    List<byte> fileBytesList = new List<byte>();
                    byteToConvert.ToList<string>()
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList<string>()
                            .ForEach(x => fileBytesList.Add(Convert.ToByte(x)));

                    //fileContents = System.Text.Encoding.UTF8.GetBytes(customer_ContactUs.AttachmentFileBytes);

                    fileContents = fileBytesList.ToArray();

                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(customer_ContactUs.AttachmentFileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    filename = Path.GetFileName(customer_ContactUs.AttachmentFileName);
                }

                await _emailSender.SendContactEmailAsync(_configuration["RegEmail:Email"], email, customer.CustomerNumber, customer_ContactUs.Email, fileContents, filename, contentType);
                result = new GenericResult()
                {
                    IsSuccess = true,
                    Message = "Email sent",
                };
                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Contact Us
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Name": "string",
        ///         "Email": "string",
        ///         "PhoneNumber": "string",
        ///         "Message": "string",
        ///         "FileName": "string",
        ///         "FileBytes": "string (base64 string)"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/ContactUsNew")]
        public async Task<IActionResult> Customer_ContactUsNew()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ContactUs customer_ContactUs = new Customer_ContactUs();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { customer_ContactUs = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ContactUs>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_ContactUs == null)
                return StatusCode(400, "Invalid post");

            if (string.IsNullOrEmpty(customer_ContactUs.Name)
                || string.IsNullOrEmpty(customer_ContactUs.Email)
                || string.IsNullOrEmpty(customer_ContactUs.PhoneNumber)
                || string.IsNullOrEmpty(customer_ContactUs.Message)
                )
            {
                result = new GenericResult()
                {
                    IsSuccess = false,
                    Message = "Missing info: ",
                };

                if (string.IsNullOrEmpty(customer_ContactUs.Name))
                    result.Message = result.Message + "Name;";

                if (string.IsNullOrEmpty(customer_ContactUs.Email))
                    result.Message = result.Message + "Email;";

                if (string.IsNullOrEmpty(customer_ContactUs.PhoneNumber))
                    result.Message = result.Message + "PhoneNumber;";

                if (string.IsNullOrEmpty(customer_ContactUs.Message))
                    result.Message = result.Message + "Message;";

                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_ContactUs),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null && skybillCustomer != null)
            {
                var company = db.Companies.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var customerMeters = skyBillApiClient.GetMetersByCustomer(customerNo);
                var accountType = db.AccountTypes.Where(tbl => tbl.AccountTypeID == customer.AccountTypeID).SingleOrDefault();
                string meterStr = "";
                foreach (var c in customerMeters)
                {
                    meterStr = meterStr + c.Serial_No + "<br/>";
                }

                String email = "User<br/> Email: " + _userEmail + "<br/>" +
                              " Contact Email: " + customer_ContactUs.Email + "<br/>" +
                              " Contact Name: " + customer_ContactUs.Name + "<br/>" +
                              " Contact Phone Number: " + customer_ContactUs.PhoneNumber + "<br/>" +
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
                              " Message: " + customer_ContactUs.Message + "<br/>";


                string contentType = "";
                byte[] fileContents = null;
                string filename = "";
                if (!string.IsNullOrEmpty(customer_ContactUs.AttachmentFileBytes) && !string.IsNullOrEmpty(customer_ContactUs.AttachmentFileName))
                {
                    fileContents = Convert.FromBase64String(customer_ContactUs.AttachmentFileBytes);

                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(customer_ContactUs.AttachmentFileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    filename = Path.GetFileName(customer_ContactUs.AttachmentFileName);
                }

                await _emailSender.SendContactEmailAsync(_configuration["RegEmail:Email"], email, customer.CustomerNumber, customer_ContactUs.Email, fileContents, filename, contentType);
                result = new GenericResult()
                {
                    IsSuccess = true,
                    Message = "Email sent",
                };
                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Company Netcash Details
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(CompanyNetcashDetailsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/CompanyNetcashDetails")]
        public async Task<IActionResult> CompanyNetcashDetails()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();


            CompanyNetcashDetailsResult result = new CompanyNetcashDetailsResult()
            {
                NetcashBankAccountNo = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo,
                NetcashBankAccountType = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType,
                NetcashBankBranchCode = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode,
                NetcashBankName = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName,
                NetcashServiceKey = company.ServiceKey,
            };


            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Payment Methods
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(PaymentMethodsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/PaymentMethods")]
        public async Task<IActionResult> PaymentMethods()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new PaymentMethodsResult()), "application/json");
        }

        /// <summary>
        /// Payment Statuses
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(PaymentStatusesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/PaymentStatuses")]
        public async Task<IActionResult> PaymentStatuses()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            return Content(JsonConvert.SerializeObject(new PaymentStatusesResult()), "application/json");
        }

        /// <summary>
        /// Customer Payment
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Amount": "decimal"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_PaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/Payment")]
        public async Task<IActionResult> Customer_Payment()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Payment customer_Payment = new Customer_Payment();
            Customer_PaymentResult result = new Customer_PaymentResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { customer_Payment = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Payment>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_Payment == null)
                return StatusCode(400, "Invalid post");

            if (customer_Payment.Amount <= 0
                //|| string.IsNullOrEmpty(customer_Payment.Email)
                //|| string.IsNullOrEmpty(customer_Payment.PhoneNumber)
                //|| string.IsNullOrEmpty(customer_Payment.Message)
                )
            {
                result = new Customer_PaymentResult()
                {
                    IsSuccess = false,
                    Message = "Missing info: ",
                };

                if (customer_Payment.Amount <= 0)
                    result.Message = result.Message + "Amount less than 0;";

                //if (string.IsNullOrEmpty(customer_Payment.Email))
                //    result.Message = result.Message + "Email;";

                //if (string.IsNullOrEmpty(customer_Payment.PhoneNumber))
                //    result.Message = result.Message + "PhoneNumber;";

                //if (string.IsNullOrEmpty(customer_Payment.Message))
                //    result.Message = result.Message + "Message;";

                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_Payment),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var _prismProvider = new PrismProvider(_cache, _options);
                var _prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);

                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                var payment = new Payment
                {
                    Amount = customer_Payment.Amount,
                    CreateDate = DateTime.Now,
                    PaymentMethodID = (int)PaymentMethodEnum.MastercardVISA,
                    PaymentStatusID = (int)PaymentStatusEnum.Incomplete,
                    UserID = customer.UserID,
                    IsCompanyAdminRecharge = false,
                };

                db.Payments.Add(payment);
                db.SaveChanges();
                payment.Reference = payment.PaymentID.ToString();
                db.SaveChanges();

                result.Reference = payment.Reference;
                result.IsSuccess = true;

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Payment Notify
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Reference": "string",
        ///         "PaymentMethodID": "int",
        ///         "CardHolderIpAddr": "string",
        ///         "RequestTrace": "string",
        ///         "Extra1": "string",
        ///         "Extra2": "string",
        ///         "Extra3": "string",
        ///         "Reason": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/PaymentNotify")]
        public async Task<IActionResult> Customer_PaymentNotify()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_PaymentNotify customer_Payment = new Customer_PaymentNotify();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { customer_Payment = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_PaymentNotify>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_Payment == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_Payment),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var _prismProvider = new PrismProvider(_cache, _options);
                var _prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);

                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                var payment = db.Payments.Where(p => p.Reference == customer_Payment.Reference).FirstOrDefault();

                if (payment == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Invalid";
                    return Content(JsonConvert.SerializeObject(result), "application/json");
                }

                if (payment.PaymentStatusID == (int)PaymentStatusEnum.Approved)
                {
                    result.IsSuccess = false;
                    result.Message = "Payment already approved";
                    return Content(JsonConvert.SerializeObject(result), "application/json");
                }

                int rawPaymentDataID = 0;

                var rawPaymentData = new PaymentRawData
                {
                    RawData = JsonConvert.SerializeObject(customer_Payment),
                    PostingDate = DateTime.Now
                };

                db.PaymentRawData.Add(rawPaymentData);

                db.SaveChanges();

                rawPaymentDataID = rawPaymentData.PaymentRawDataID;

                decimal convenienceFee = (payment.Amount * (company != null && company.ConvenienceFeePerc.HasValue ? company.ConvenienceFeePerc.Value : 0.1m));
                decimal loadedAmount = payment.Amount;

                // Does not pay convenience fee

                if (payment.PaymentMethodID == (int)PaymentMethodEnum.iPay || payment.PaymentMethodID == (int)PaymentMethodEnum.EFT || payment.PaymentMethodID == (int)PaymentMethodEnum.Retail)
                {
                    convenienceFee = 0;
                }

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                #region Skybill Journals

                try
                {
                    skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = DateTime.UtcNow.Date,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Customer,
                        Account_No = customer.CustomerNumber,
                        AmountSpecified = true,
                        Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment",
                        Amount = loadedAmount * -1,
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                        Bal_Account_No = "SAGEPAY",
                    }, db, customer.UserID);
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Message = $"Error: {ex.Message}";
                    return Content(JsonConvert.SerializeObject(result), "application/json");
                }

                try
                {
                    if (convenienceFee > 0)
                    {
                        skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new SalesJournal.SalesJnl()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = DateTime.UtcNow.Date,
                            Document_TypeSpecified = true,
                            Document_Type = SalesJournal.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = SalesJournal.Account_Type.Customer,
                            Account_No = customer.CustomerNumber,
                            AmountSpecified = true,
                            Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee",
                            Amount = convenienceFee,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                            Bal_Account_No = "6810",
                        }, db, customer.UserID);
                    }
                }
                catch (Exception ex)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "Convenience error", ex.ToString(), ex.ToString(), cc: "madelyn@myvoltage.co.za");
                    Serilog.Log.Error("Convenience: " + ex.ToString());
                }

                #region New Payments for the 5%

                StringBuilder fullErrorMessage = new StringBuilder();

                var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
                string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";

                if (company.CompanyID == 13)
                {
                    vendingCustomerNo = "000-S";
                }

                try
                {
                    var bankCharges = PaymentMethodFees.GetBankCharges((PaymentMethodEnum)payment.PaymentMethodID, loadedAmount, payment.PaymentID.ToString());

                    if (bankCharges == null)
                    {
                        EmailSender emailSender = new EmailSender();
                        string ebody = $"MY VOLTAGE VENDING FAILED - Could not locate bank charges for Payment Method: {payment.PaymentMethodID} - {company.Name} - {customer.CustomerNumber} ({payment.Amount:N2})";
                        await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "5% error", ebody, ebody, cc: "madelyn@myvoltage.co.za");
                    }
                    else
                    {

                        decimal feeCalcForStep3and4 = (loadedAmount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

                        if (feeCalcForStep3and4 <= 0)
                            feeCalcForStep3and4 = 0.01m;

                        int? vending1LogID = null;
                        string vending1SkybillCompanyName = "";
                        int? vending2LogID = null;
                        string vending2SkybillCompanyName = "";
                        int? vending3LogID = null;
                        string vending3SkybillCompanyName = "";
                        int? vending4LogID = null;
                        string vending4SkybillCompanyName = "";

                        switch ((PaymentMethodEnum)payment.PaymentMethodID)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region Vending 1
                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)
                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2
                                // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    // + & -,  if 0 then 0.01
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - Mastercard/VISA Commission",
                                    // if negative then zero
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.Retail:

                                vending1LogID = 0;
                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.iPay:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                vending2LogID = 0;
                                vending3LogID = 0;
                                vending4LogID = 0;

                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region Vending 1

                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)
                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2

                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = $"{payment.PaymentID} - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MasterPass Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,

                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region Vending 1

                                vending1LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogID.HasValue)
                                    vending1SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 2

                                vending2LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);


                                if (vending2LogID.HasValue)
                                    vending2SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 3

                                vending3LogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                if (vending3LogID.HasValue)
                                    vending3SkybillCompanyName = company.Name;

                                #endregion

                                #region Vending 4

                                vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = DateTime.UtcNow.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - VisaCheckout Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                if (vending4LogID.HasValue)
                                    vending4SkybillCompanyName = vendingCompany.Name;

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }

                        payment.Vending1LogID = vending1LogID;
                        payment.Vending1SkybillCompanyName = vending1SkybillCompanyName;
                        payment.Vending2LogID = vending2LogID;
                        payment.Vending2SkybillCompanyName = vending2SkybillCompanyName;
                        payment.Vending3LogID = vending3LogID;
                        payment.Vending3SkybillCompanyName = vending3SkybillCompanyName;
                        payment.Vending4LogID = vending4LogID;
                        payment.Vending4SkybillCompanyName = vending4SkybillCompanyName;

                        db.Update(payment);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "5% error", ex.ToString(), ex.ToString(), cc: "madelyn@myvoltage.co.za");
                }

                #endregion

                #endregion

                #region Prism Token

                try
                {
                    //Send STS token
                    if (customer != null && customer.AccountTypeID == (int)AccountTypeEnum.MyWallet)
                    {
                        decimal balance = CustomerProvider.GetMyWalletBalance(_cache, company.Name, customer.CustomerNumber);

                        if (balance/* < 0 && balance + loadedAmount*/ > 0)
                        {
                            var apiClient = new SkyBillApiClient(company.Name, _cache);
                            var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);

                            foreach (var meter in meters)
                            {
                                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                                if (device != null && String.Compare(device.type.type, "elec", true) == 0)
                                {
                                    DateTime? meterStatusTime = null;
                                    if (device.status != null)
                                        meterStatusTime = device.status.time;

                                    #region Contactor State

                                    Dictionary<int, string> registers = new Dictionary<int, string>();
                                    registers.Add(91, "readings");

                                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                                    DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                                    string errorMessage = "";

                                    var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);
                                    string contactorState = "";

                                    if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                                    {
                                        errorMessage = "PaymentController.cs:1394 - deviceContactorStateData null";
                                    }
                                    else
                                    {
                                        var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                        if (validreadings == null || validreadings.Count == 0)
                                        {
                                            errorMessage = "PaymentController.cs:1402 - validreadings null";
                                        }
                                        else
                                        {
                                            contactorState = validreadings[validreadings.Count - 1].ToString();
                                        }
                                    }

                                    bool isContactorConnected = _client.IsDeviceContactorConnected(device.id);

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
                                            errorMessage = "PaymentController.cs:1423 - " + ex.ToString();
                                        }
                                    }

                                    #endregion

                                    //if (!isContactorConnected)
                                    //{

                                    if (meter.Serial_No.Trim().Length == 11)
                                    {
                                        var token = _prismApiClient.GenerateToken(meter.Serial_No, "set-postpaid", "SagePay", balance, errorMessage, customer.UserID);

                                        if (!string.IsNullOrEmpty(token))
                                        {
                                            _client.MeterSTS(token, device.id.ToString(), "SagePay", balance, errorMessage, device.deviceStatus, meterStatusTime);
                                            System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, device.id.ToString(), "SagePay", balance, errorMessage, meter.Serial_No));
                                            thread.Start();
                                        }
                                        else
                                        {
                                            System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(meter.Serial_No, "set-postpaid", "SagePay", balance, customer.UserID));
                                            thread.Start();

                                            //    string emailBody = $"There was an error generating token for {meter.Serial_No} - Payment of {loadedAmount:N}";
                                            //    EmailSender emailSender = new EmailSender();
                                            //    await emailSender.SendEmailAsync(new string[] {
                                            //"rose@myvoltage.co.za",
                                            //"riaan@myvoltage.co.za",
                                            //"lendl@myvoltage.co.za",
                                            //"nic@myvoltage.co.za",
                                            //}
                                            //    , "Token Generation Error - Sage"
                                            //    , emailBody
                                            //    , emailBody);
                                        }
                                    }
                                    //}
                                }
                            }

                        }
                    }

                    if (customer != null && customer.AccountTypeID == (int)AccountTypeEnum.PrepaidCredit
                        && (!customer.AutoConvertBalanceToUnits.HasValue
                        || customer.AutoConvertBalanceToUnits.Value))
                    {
                        var apiClient = new SkyBillApiClient(company.Name, _cache);
                        string token = "";
                        string prismDeviceID = "";
                        var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                        var skybillCustomer = apiClient.GetCustomer(customer.CustomerNumber);

                        decimal amount = Convert.ToDecimal(skybillCustomer.Balance_LCY) * -1;
                        string serial = "";
                        DateTime? meterStatusTime = null;
                        string deviceStatus = "";
                        if (amount > 0)
                        {
                            foreach (var meter in meters)
                            {
                                var prismDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                                prismDeviceID = prismDevice.id.ToString();
                                if (prismDevice != null && String.Compare(prismDevice.type.type, "elec", true) == 0)
                                {
                                    token = _prismApiClient.GenerateToken(meter.Serial_No, Convert.ToDouble(amount), "SagePay - AutoRedeem", amount, "", customer.UserID);
                                    serial = meter.Serial_No;
                                    if (prismDevice.status != null)
                                        meterStatusTime = prismDevice.status.time;
                                    deviceStatus = prismDevice.deviceStatus;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(token))
                            {
                                _client.MeterSTS(token, prismDeviceID, "SagePay - AutoRedeem", amount, "", deviceStatus, meterStatusTime);
                                System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendToken3Times(token, prismDeviceID, "SagePay - AutoRedeem", amount, "", serial));
                                thread.Start();

                                #region Journal Entry

                                SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();

                                journal.Posting_DateSpecified = true;
                                journal.Posting_Date = DateTime.UtcNow.Date;
                                journal.Document_TypeSpecified = true;
                                journal.Document_Type = SalesJournal.Document_Type.Invoice;
                                journal.Account_TypeSpecified = true;
                                journal.Account_Type = SalesJournal.Account_Type.Customer;
                                journal.Account_No = customer.CustomerNumber;
                                journal.AmountSpecified = true;
                                journal.Description = $"Balance Redeem - {customer.CustomerNumber}";
                                journal.Amount = Convert.ToDecimal(amount);
                                journal.Bal_Account_TypeSpecified = true;
                                journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                                journal.Bal_Account_No = "6610";

                                SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                                skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, journal, db, customer.UserID);

                                #endregion

                                skybillCustomer = apiClient.GetCustomer(customer.CustomerNumber);
                                var updatedBalance = skybillCustomer.Balance_LCY * -1;

                                string phoneNumber = customer.NotificationPhoneNumber;
                                if (!string.IsNullOrEmpty(phoneNumber))
                                {
                                    StringBuilder sbSMS = new StringBuilder();
                                    sbSMS.Append($"Token:{token}.");
                                    sbSMS.Append($"{Environment.NewLine}Purchased: R {loadedAmount:N}");
                                    sbSMS.Append($"{Environment.NewLine}Other charges: R {(loadedAmount - Convert.ToDecimal(amount)):N}");
                                    sbSMS.Append($"{Environment.NewLine}Balance converted: R {amount:N}");
                                    sbSMS.Append($"{Environment.NewLine}Help: 0870572561");
                                    sbSMS.Append($"{Environment.NewLine}Ref: {payment.Reference}");

                                    SMS.SendSms("27" + phoneNumber.Remove(0, 1), sbSMS.ToString());
                                }
                            }
                            else
                            {
                                System.Threading.Thread thread = new System.Threading.Thread(() => _prismProvider.SendTokenInfinite(serial, Convert.ToDouble(amount), "SagePay - AutoRedeem", amount, customer.UserID));
                                thread.Start();
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    Serilog.Log.Error("PRISM: " + ex.ToString());
                }

                #endregion


                payment.PaymentStatusID = (int)PaymentStatusEnum.Approved;
                if (string.IsNullOrEmpty(payment.Reason))
                    payment.Reason = "Success";
                result.IsSuccess = true;
                db.SaveChanges();

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Meter Status
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "MeterSerial": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_MeterStatusResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/MeterStatus")]
        public async Task<IActionResult> Customer_MeterStatus()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_MeterStatus customer_MeterStatus = new Customer_MeterStatus();
            Customer_MeterStatusResult result = new Customer_MeterStatusResult()
            {
                ContactorStatusDescription = "Not Applicable",
                ContactorStatusID = 3,
                IsOnline = false,
                StatusTime = new DateTime(2000, 01, 01),
            };

            var body = reader.ReadToEndAsync().Result;

            try { customer_MeterStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_MeterStatus>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (customer_MeterStatus == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(customer_MeterStatus),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var m2mDev = _client.GetDeviceByMeterNumber(customer_MeterStatus.MeterSerial);
                if (m2mDev != null)
                {
                    if (m2mDev.status != null)
                    {
                        result.IsOnline = m2mDev.status.id == 1;
                        result.StatusTime = m2mDev.status.time.HasValue ? m2mDev.status.time.Value : DateTime.MinValue;
                    }

                    var localDev = (from p in db.Devices
                                    where p.Serial == customer_MeterStatus.MeterSerial
                                    select p).FirstOrDefault();

                    if (localDev != null && localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    {
                        if (_client.IsDeviceContactorConnected(m2mDev.id))
                        {
                            result.ContactorStatusDescription = "Connected";
                            result.ContactorStatusID = 1;
                        }
                        else
                        {
                            result.ContactorStatusDescription = "Disconnected";
                            result.ContactorStatusID = 2;
                        }

                    }
                }


                var aco_StatusHack = (from p in db.SiteAdmin_ContactorStateHacks
                                      where p.MeterSerial == customer_MeterStatus.MeterSerial
                                      select p).FirstOrDefault();

                if (aco_StatusHack != null)
                {
                    if (aco_StatusHack.ContactorIsOnline)
                    {
                        result.ContactorStatusDescription = "Connected";
                        result.ContactorStatusID = 1;
                    }
                    else
                    {
                        result.ContactorStatusDescription = "Disconnected";
                        result.ContactorStatusID = 2;
                    }
                }

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Meter Status ACO
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "MeterSerial": "string"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_MeterStatusACOResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/MeterStatusACO")]
        public async Task<IActionResult> Customer_MeterStatusACO()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_MeterStatusACO Customer_MeterStatusACO = new Customer_MeterStatusACO();
            Customer_MeterStatusACOResult result = new Customer_MeterStatusACOResult()
            {
                ServiceStatus = "",

                ReserveStatus = "",

                ChangeOverIsOnline = false,
                ChangeOverStatusTime = new DateTime(2000, 01, 01),
                ChangeOverStatus = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { Customer_MeterStatusACO = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_MeterStatusACO>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (Customer_MeterStatusACO == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Customer_MeterStatusACO),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                string serialService = $"ACO-{Customer_MeterStatusACO.MeterSerial}-L";
                string serialReserve = $"ACO-{Customer_MeterStatusACO.MeterSerial}-R";
                string serialChangeOver = $"ACO-{Customer_MeterStatusACO.MeterSerial}-C";

                var aco_StatusHack = (from p in db.ACO_StatusHacks
                                      where p.MeterSerial == Customer_MeterStatusACO.MeterSerial
                                      select p).FirstOrDefault();

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(110, "readings"); // ?

                var registerStr = "";
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
                string start = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");
                string end = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");

                #region Service

                var m2mDevService = _client.GetDeviceByMeterNumber(serialService);
                if (m2mDevService != null)
                {
                    var localDev = (from p in db.Devices
                                    where p.Serial == serialService
                                    select p).FirstOrDefault();

                    //if (localDev != null && localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    //{
                    //    if (_client.IsDeviceContactorConnected(m2mDevService.id))
                    //    {
                    //        result.ServiceContactorStatusDescription = "Connected";
                    //        result.ServiceContactorStatusID = 1;
                    //    }
                    //    else
                    //    {
                    //        result.ServiceContactorStatusDescription = "Disconnected";
                    //        result.ServiceContactorStatusID = 2;
                    //    }
                    //}

                    #region ACO Fields

                    System.Data.DataTable dataTableService = new System.Data.DataTable();
                    string urlService = $"devices/{m2mDevService.id}/data.csv?start={start}&end={end}&interval=60{registerStr}";
                    var resultm2mService = _client.GetString(urlService, localDev != null ? localDev.DeviceAPIIDValue : 1);

                    bool firstService = true;

                    foreach (var fileLine in resultm2mService.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (firstService)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableService.Columns.Add(safeName, colType);
                            }
                            firstService = false;
                            continue;
                        }

                        DataRow row = dataTableService.NewRow();
                        int colIndex = 0;
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            if (colIndex == 1)
                                row[colIndex] = Convert.ToDateTime(safeName);
                            else
                                row[colIndex] = safeName;
                            colIndex++;
                        }

                        dataTableService.Rows.Add(row);
                        dataTableService.AcceptChanges();
                    }

                    if (dataTableService.Rows.Count > 0)
                    {
                        DataView dv = dataTableService.DefaultView;
                        dv.Sort = "[Time Logged] desc";
                        System.Data.DataTable sortedDT = dv.ToTable();

                        foreach (DataRow dataRow in sortedDT.Rows)
                        {
                            try
                            {
                                switch (Convert.ToInt32(dataRow[2]))
                                {
                                    case 0:
                                        //then “Service not selected”,
                                        result.ServiceStatus = "Service not selected";
                                        break;
                                    case 1:
                                        //then “Service” = “Left Bank” / “Reserve” = “Right bank”
                                        result.ServiceStatus = "Left Bank";
                                        break;
                                    case 2:
                                        //then “Service” = “Right Bank” / “Reserve” = “Left bank”
                                        result.ServiceStatus = "Right Bank";
                                        break;
                                    case 3:
                                        //then “Service “ Device error”
                                        result.ServiceStatus = "Device error";
                                        break;
                                }
                                break;
                            }
                            catch { }
                        }

                    }


                    #endregion

                }

                #endregion

                #region Reserve

                var m2mDevReserve = _client.GetDeviceByMeterNumber(serialReserve);
                if (m2mDevReserve != null)
                {
                    var localDev = (from p in db.Devices
                                    where p.Serial == serialReserve
                                    select p).FirstOrDefault();

                    //if (localDev != null && localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    //{
                    //    if (_client.IsDeviceContactorConnected(m2mDevReserve.id))
                    //    {
                    //        result.ReserveContactorStatusDescription = "Connected";
                    //        result.ReserveContactorStatusID = 1;
                    //    }
                    //    else
                    //    {
                    //        result.ReserveContactorStatusDescription = "Disconnected";
                    //        result.ReserveContactorStatusID = 2;
                    //    }
                    //}

                    #region ACO Fields

                    System.Data.DataTable dataTableReserve = new System.Data.DataTable();
                    string urlReserve = $"devices/{m2mDevReserve.id}/data.csv?start={start}&end={end}&interval=60{registerStr}";
                    var resultm2mReserve = _client.GetString(urlReserve, localDev != null ? localDev.DeviceAPIIDValue : 1);

                    bool firstReserve = true;

                    foreach (var fileLine in resultm2mReserve.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (firstReserve)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableReserve.Columns.Add(safeName, colType);
                            }
                            firstReserve = false;
                            continue;
                        }

                        DataRow row = dataTableReserve.NewRow();
                        int colIndex = 0;
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            if (colIndex == 1)
                                row[colIndex] = Convert.ToDateTime(safeName);
                            else
                                row[colIndex] = safeName;
                            colIndex++;
                        }

                        dataTableReserve.Rows.Add(row);
                        dataTableReserve.AcceptChanges();
                    }

                    if (dataTableReserve.Rows.Count > 0)
                    {
                        DataView dv = dataTableReserve.DefaultView;
                        dv.Sort = "[Time Logged] desc";
                        System.Data.DataTable sortedDT = dv.ToTable();

                        foreach (DataRow dataRow in sortedDT.Rows)
                        {
                            try
                            {
                                switch (Convert.ToInt32(dataRow[2]))
                                {
                                    case 0:
                                        result.ReserveStatus = "Service not selected";
                                        break;
                                    case 1:
                                        result.ReserveStatus = "Right bank";
                                        break;
                                    case 2:
                                        result.ReserveStatus = "Left bank";
                                        break;
                                    case 3:
                                        result.ReserveStatus = "Device error";
                                        break;
                                }
                                break;
                            }
                            catch { }
                        }

                    }


                    #endregion

                }

                #endregion

                #region ChangeOver

                var m2mDevChangeOver = _client.GetDeviceByMeterNumber(serialChangeOver);
                if (m2mDevChangeOver != null)
                {
                    if (m2mDevChangeOver.status != null)
                    {
                        result.ChangeOverIsOnline = m2mDevChangeOver.status.id == 1;
                        result.ChangeOverStatusTime = m2mDevChangeOver.status.time.HasValue ? m2mDevChangeOver.status.time.Value : DateTime.MinValue;
                    }

                    var localDev = (from p in db.Devices
                                    where p.Serial == serialChangeOver
                                    select p).FirstOrDefault();

                    //if (localDev != null && localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    //{
                    //    if (_client.IsDeviceContactorConnected(m2mDevChangeOver.id))
                    //    {
                    //        result.ChangeOverContactorStatusDescription = "Connected";
                    //        result.ChangeOverContactorStatusID = 1;
                    //    }
                    //    else
                    //    {
                    //        result.ChangeOverContactorStatusDescription = "Disconnected";
                    //        result.ChangeOverContactorStatusID = 2;
                    //    }
                    //}

                    #region ACO Fields

                    System.Data.DataTable dataTableChangeOver = new System.Data.DataTable();
                    string urlChangeOver = $"devices/{m2mDevChangeOver.id}/data.csv?start={start}&end={end}&interval=60{registerStr}";
                    var resultm2mChangeOver = _client.GetString(urlChangeOver, localDev != null ? localDev.DeviceAPIIDValue : 1);

                    bool firstChangeOver = true;

                    foreach (var fileLine in resultm2mChangeOver.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (firstChangeOver)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableChangeOver.Columns.Add(safeName, colType);
                            }
                            firstChangeOver = false;
                            continue;
                        }

                        DataRow row = dataTableChangeOver.NewRow();
                        int colIndex = 0;
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            if (colIndex == 1)
                                row[colIndex] = Convert.ToDateTime(safeName);
                            else
                                row[colIndex] = safeName;
                            colIndex++;
                        }

                        dataTableChangeOver.Rows.Add(row);
                        dataTableChangeOver.AcceptChanges();
                    }

                    if (dataTableChangeOver.Rows.Count > 0)
                    {
                        DataView dv = dataTableChangeOver.DefaultView;
                        dv.Sort = "[Time Logged] desc";
                        System.Data.DataTable sortedDT = dv.ToTable();

                        foreach (DataRow dataRow in sortedDT.Rows)
                        {
                            try
                            {
                                switch (Convert.ToInt32(dataRow[2]))
                                {
                                    case 0:
                                        result.ChangeOverStatus = "Depleted";
                                        break;
                                    case 1:
                                        result.ChangeOverStatus = "Active";
                                        break;
                                }
                                break;
                            }
                            catch { }
                        }

                    }


                    #endregion

                }

                #endregion

                activityLog.UserID = customer.UserID;

                if (aco_StatusHack != null)
                {
                    if (!string.IsNullOrEmpty(aco_StatusHack.ServiceStatus) && !string.IsNullOrEmpty(aco_StatusHack.ServiceStatus.Trim()))
                        result.ServiceStatus = aco_StatusHack.ServiceStatus;

                    if (!string.IsNullOrEmpty(aco_StatusHack.ReserveStatus) && !string.IsNullOrEmpty(aco_StatusHack.ReserveStatus.Trim()))
                        result.ReserveStatus = aco_StatusHack.ReserveStatus;

                    if (!string.IsNullOrEmpty(aco_StatusHack.ChangeOverStatus) && !string.IsNullOrEmpty(aco_StatusHack.ChangeOverStatus.Trim()))
                        result.ChangeOverStatus = aco_StatusHack.ChangeOverStatus;
                    if (aco_StatusHack.ChangeOverIsOnline.HasValue)
                        result.ChangeOverIsOnline = aco_StatusHack.ChangeOverIsOnline.Value;
                    if (aco_StatusHack.ChangeOverStatusTime.HasValue)
                        result.ChangeOverStatusTime = aco_StatusHack.ChangeOverStatusTime.Value;

                }
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Notifications
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "ShowOnlyUnread": "bool"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(Customer_NotificationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/Notifications")]
        public async Task<IActionResult> Customer_Notifications()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_Notifications Customer_Notifications = new Customer_Notifications();
            Customer_NotificationsResult result = new Customer_NotificationsResult()
            {
                Customer_NotificationsResultItems = new List<Customer_NotificationsResult.Customer_NotificationsResultItem>(),
            };

            var body = reader.ReadToEndAsync().Result;

            try { Customer_Notifications = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Notifications>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (Customer_Notifications == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Customer_Notifications),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var notifications = db.Customer_AppNotifications.Where(p => p.CustomerID == customer.CustomerID && p.ExpiryDate >= DateTime.Now).ToList();
                if (Customer_Notifications.ShowOnlyUnread)
                    notifications = notifications.Where(p => !p.DateRead.HasValue).ToList();

                result.Customer_NotificationsResultItems = (from p in notifications
                                                            select new Customer_NotificationsResult.Customer_NotificationsResultItem()
                                                            {
                                                                DateCreated = p.DateCreated,
                                                                DateRead = p.DateRead,
                                                                ExpiryDate = p.ExpiryDate,
                                                                ID = p.ID,
                                                                Message = p.Message,
                                                            }).ToList();

                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Customer Notifications Mark As Read
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "ID": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/NotificationsMarkAsRead")]
        public async Task<IActionResult> Customer_NotificationsMarkAsRead()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_NotificationsMarkAsRead Customer_NotificationsMarkAsRead = new Customer_NotificationsMarkAsRead();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { Customer_NotificationsMarkAsRead = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_NotificationsMarkAsRead>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (Customer_NotificationsMarkAsRead == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Customer_NotificationsMarkAsRead),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var notifications = db.Customer_AppNotifications.Where(p => p.ID == Customer_NotificationsMarkAsRead.ID).SingleOrDefault();

                if (notifications != null)
                {
                    notifications.DateRead = DateTime.Now;

                    result.IsSuccess = true;
                    result.Message = "Success";
                }
                else
                {
                    result.Message = "";
                }
                activityLog.UserID = customer.UserID;
            }

            activityLog.DateEnded = DateTime.Now;
            activityLog.Response = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }

        /// <summary>
        /// Post Lead
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "APIKey": "string",
        ///         "ContactPersonEmail": "string",
        ///         "ContactPersonPhone": "string",
        ///         "ContactPerson": "string",
        ///         "ContactPersonPosition": "string",
        ///         "ClientNeeds": "string",
        ///         "PropertyDescription": "string",
        ///         "PropertyAddress": "string",
        ///         "Municipality": "string",
        ///         "NoOfUnits": "int",
        ///         "HasShownWebsiteAndVideo": "bool"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/PostLead")]
        public async Task<IActionResult> PostLead()
        {
            StreamReader reader = new StreamReader(Request.Body);
            PostLead PostLead = new PostLead();
            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = "",
            };

            var body = reader.ReadToEndAsync().Result;

            try { PostLead = Newtonsoft.Json.JsonConvert.DeserializeObject<PostLead>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (PostLead == null)
                return StatusCode(400, "Invalid post");

            if (string.IsNullOrEmpty(PostLead.ContactPersonEmail) && string.IsNullOrEmpty(PostLead.ContactPersonPhone))
            {
                result.IsSuccess = false;
                result.Message = "ContactPersonEmail or ContactPersonPhone required";
            }
            else
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                             where p.APIKey.ToUpper() == PostLead.APIKey.ToUpper()
                                             select p).FirstOrDefault();

                if (d01_LeadGeneratorUser == null)
                    return StatusCode(401);

                //D01_Lead d01_Lead = new D01_Lead()
                //{
                //    AssignedToUserID = "",
                //    ClientNeeds = PostLead.ClientNeeds,
                //    ContactPerson = PostLead.ContactPerson,
                //    ContactPersonEmail = PostLead.ContactPersonEmail,
                //    ContactPersonPhone = PostLead.ContactPersonPhone,
                //    ContactPersonPosition = PostLead.ContactPersonPosition,
                //    DateCreated = DateTime.Now,
                //    HasShownWebsiteAndVideo = PostLead.HasShownWebsiteAndVideo.HasValue ? PostLead.HasShownWebsiteAndVideo.Value : false,
                //    LeadGeneratorUserID = d01_LeadGeneratorUser.ID,
                //    Municipality = PostLead.Municipality,
                //    NoOfUnits = PostLead.NoOfUnits,
                //    PropertyAddress = PostLead.PropertyAddress,
                //    PropertyDescription = PostLead.PropertyDescription,
                //    PropertyTypeID = (int)D01_Lead.PropertyTypeEnum.Other,
                //    StatusID = (int)D01_Lead.StatusEnum.New,
                //    UserID = !string.IsNullOrEmpty(d01_LeadGeneratorUser.LocalUserID) ? d01_LeadGeneratorUser.LocalUserID : "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                //};

                //db.Add(d01_Lead);
                //db.SaveChanges();

                result.IsSuccess = true;
                result.Message = "Success";
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");

        }
        /*
                /// <summary>
                /// Create ACO Device
                /// </summary>
                /// <remarks>
                /// Sample request:
                ///
                ///     {
                ///         "UserID": "string",
                ///         "GatewayID": "int",
                ///         "SerialNumber": "string",
                ///         "RemoteAddress": "string",
                ///         "RemoteAddressChangeOver": "string"
                ///     }
                ///
                /// </remarks>
                /// <returns></returns>
                /// <response code="200">Success</response>
                /// <response code="401">Unauthorized</response>
                [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
                [ProducesResponseType(StatusCodes.Status401Unauthorized)]
                [Produces("application/json")]
                [HttpPost]
                [Route("/WebServices/CreateACODevice")]
                public async Task<IActionResult> CreateACODevice()
                {
                    StreamReader reader = new StreamReader(Request.Body);
                    CreateACODevice CreateACODevice = new CreateACODevice();
                    GenericResult result = new GenericResult()
                    {
                        IsSuccess = false,
                        Message = "",
                    };

                    var body = reader.ReadToEndAsync().Result;

                    try { CreateACODevice = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateACODevice>(body); }
                    catch { return StatusCode(400, "Invalid post"); }
                    if (CreateACODevice == null)
                        return StatusCode(400, "Invalid post");

                    if (string.IsNullOrEmpty(CreateACODevice.SerialNumber))
                    {
                        result.Message = "SerialNumber required";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    if (string.IsNullOrEmpty(CreateACODevice.RemoteAddress))
                    {
                        result.Message = "RemoteAddress required";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    if (string.IsNullOrEmpty(CreateACODevice.RemoteAddressChangeOver))
                    {
                        result.Message = "RemoteAddressChangeOver required";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    if (string.IsNullOrEmpty(CreateACODevice.UserID))
                    {
                        result.Message = "UserID required";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                    var user = _userManager.FindByIdAsync(CreateACODevice.UserID).Result;
                    if (user == null)
                    {
                        result.Message = "UserID required";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    MyVoltageDbContext db = new MyVoltageDbContext(_options);
                    var meterType = db.MeterTypes.Where(p => p.ID == 40).SingleOrDefault();
                    var gateway = _client.GetGateway(CreateACODevice.GatewayID.ToString());
                    if (gateway == null)
                    {
                        result.Message = "GatewayID invalid";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_AddDeviceACOModel_Step3Model model = new Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_AddDeviceACOModel_Step3Model()
                    {
                        GatewayID = CreateACODevice.GatewayID,
                        GatewayName = gateway.name,
                        GatewayHardwareType = gateway.type.name,
                        MeterType = meterType.TypeName,
                        ShowPort = meterType.Port.HasValue ? false : true,
                        ShowProtocol = meterType.Protocol.HasValue ? false : true,
                        ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                        ShowRemoteAddressChangeOver = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                        ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true,
                        ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true,
                        DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "",
                        Prefix = meterType.Prefix,
                    };

                    model.RemoteAddress = CreateACODevice.RemoteAddress;
                    model.RemoteAddressChangeOver = CreateACODevice.RemoteAddressChangeOver;
                    model.SerialNumber = CreateACODevice.SerialNumber;

                    List<string> validationErrorMessages = MeterProvider.CreateACODevice(_options, _client, CreateACODevice.UserID, model.GatewayID, meterType, model);

                    if (validationErrorMessages.Count == 0)
                        result.IsSuccess = true;
                    else
                        result.Message = string.Join(' ', validationErrorMessages.ToArray());

                    return Content(JsonConvert.SerializeObject(result), "application/json");

                }
        */

        /// <summary>
        /// Check Change in Notification Email and Cellphone Number
        /// </summary>
        /// <remarks>
        /// Sample Request
        /// 
        ///     {
        ///         "PropType": "string",
        ///         "NotificationEmail": "string",
        ///         "PhoneNumber": "string"
        ///     }
        ///     
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("/WebServices/Customer/CheckProfileContact")]
        public async Task<IActionResult> ProfileContactVerification()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            var profileContact = new ProfileContactVerification();

            var result = new ProfileContactVerificationResult();

            StreamReader reader = new StreamReader(Request.Body);
            string body = await reader.ReadToEndAsync();

            try { profileContact = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfileContactVerification>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (profileContact == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(profileContact),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();


            Random random = new Random();
            var numbers = "1234567890";
            var stringNumbers = new char[4];

            for (int i = 0; i < stringNumbers.Length; i++)
            {
                stringNumbers[i] = numbers[random.Next(numbers.Length)];
            }
            var OTPCode = new string(stringNumbers);

            if (customer != null)
            {
                result.IsNumberChanged = false;
                result.IsEmailChanged = false;

                if (profileContact.PropType == "phone")
                {
                    if (!string.IsNullOrEmpty(profileContact.PhoneNumber) && customer.PhoneNumber != profileContact.PhoneNumber && profileContact.PhoneNumber.Length == 10)
                    {
                        customer.OTPCode = OTPCode;
                        SMS.SendSms("27" + profileContact.PhoneNumber.Remove(0, 1), "Your My Voltage Cellphone verification code is " + OTPCode + ". Enter this code to update your cellphone detail.");
                        result.IsNumberChanged = true;
                        result.Message = "OTP sent to cellphone";
                        db.Customers.Update(customer);
                        db.SaveChanges();
                        result.IsSuccess = true;
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.Message = "Invalid Cellphone";
                        return Json(result);
                    }
                }
                else if (profileContact.PropType == "email")
                {
                    if (!string.IsNullOrEmpty(profileContact.NotificationEmail) && customer.NotificationEmail != profileContact.NotificationEmail)
                    {
                        try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(profileContact.NotificationEmail); }
                        catch
                        {
                            result.IsSuccess = false;
                            result.Message = "Invalid Email";
                            return Json(result);
                        }
                        customer.OTPCode = OTPCode;
                        _emailSender.SendNotificationEmailCodeAsync(profileContact.NotificationEmail, OTPCode, customer.FullName, _context);
                        result.IsEmailChanged = true;
                        result.Message = "OTP sent to email";
                        db.Customers.Update(customer);
                        db.SaveChanges();
                        result.IsSuccess = true;
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.Message = "Invalid Email";
                        return Json(result);
                    }
                }

                activityLog.UserID = customer.UserID;
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

        /// <summary>
        /// Confirm Notification Email and Cellphone Number
        /// </summary>
        /// <remarks>
        /// Sample Request
        /// 
        ///     {
        ///         "PropType": "string",
        ///         "NotificationEmail": "string",
        ///         "Cellphone": "string"
        ///         "OTP": "string"
        ///     }
        ///     
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("/WebServices/Customer/ConfirmProfileContact")]
        public async Task<IActionResult> ProfileContactConfirm()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            var profileContact = new ProfileContactConfirm();
            var result = new GenericResult();

            StreamReader reader = new StreamReader(Request.Body);
            string body = await reader.ReadToEndAsync();

            try { profileContact = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfileContactConfirm>(body); }
            catch { return StatusCode(400, "Invalid post"); }
            if (profileContact == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.FormSubmit,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(profileContact),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var db = new MyVoltageDbContext(_options);
            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                if (String.IsNullOrEmpty(profileContact.OTP) || profileContact.OTP.Length != 4 || customer.OTPCode != profileContact.OTP)
                {
                    result.IsSuccess = false;
                    result.Message = "Invalid OTP";
                    return Json(result);
                }

                if (profileContact.PropType == "email")
                {
                    if (!string.IsNullOrEmpty(profileContact.NotificationEmail) && customer.NotificationEmail != profileContact.NotificationEmail)
                    {
                        try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(profileContact.NotificationEmail); }
                        catch
                        {
                            result.IsSuccess = false;
                            result.Message = "Invalid Email";

                            return Json(result);
                        }

                        customer.NotificationEmail = profileContact.NotificationEmail;
                        db.Customers.Update(customer);
                        db.SaveChanges();
                        result.IsSuccess = true;
                        result.Message = "NotificationEmail Updated";
                    }
                }
                else if (profileContact.PropType == "phone")
                {
                    if (!string.IsNullOrEmpty(profileContact.PhoneNumber) && customer.PhoneNumber != profileContact.PhoneNumber)
                    {
                        if (customer.PhoneNumber.Length != 10)
                        {
                            result.IsSuccess = false;
                            result.Message = "Invalid Cellphone";
                            return Json(result);
                        }
                        customer.PhoneNumber = profileContact.PhoneNumber;
                        db.Customers.Update(customer);
                        db.SaveChanges();
                        result.IsSuccess = true;
                        result.Message = "Cellphone Updated";
                    }
                }

                activityLog.UserID = customer.UserID;
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

        /// <summary>
        /// Consumption Insights Data Availability
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///         "ProductID": "int"
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error generating file</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPost]
        [Route("/WebServices/Customer/CheckConsumptionInsightsData")]
        public async Task<IActionResult> Customer_ConsumptionInsightsCheckData()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            StreamReader reader = new StreamReader(Request.Body);
            Customer_ConsumptionInsights Customer_ConsumptionInsights = new Customer_ConsumptionInsights();
            var result = new GenericResult();
            try { Customer_ConsumptionInsights = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_ConsumptionInsights>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }
            if (Customer_ConsumptionInsights == null)
                return StatusCode(400, "Invalid post");

            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = Newtonsoft.Json.JsonConvert.SerializeObject(Customer_ConsumptionInsights),
                Response = "",
                SourceID = (int)LogSourceEnum.WebServices,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var db = new Data.MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            ConsumptionInsightsModel model = new ConsumptionInsightsModel();

            if (Customer_ConsumptionInsights.ProductID != 0 && Customer_ConsumptionInsights.Month != 0 && Customer_ConsumptionInsights.Year != 0)
            {
                DateTime startDateMonthly = new DateTime(Customer_ConsumptionInsights.Year, 1, 1);
                DateTime endDateMonthly = new DateTime(startDateMonthly.AddMonths(11).Year, Customer_ConsumptionInsights.Month, 1);

                DateTime current = startDateMonthly;

                var currentDates = new List<DateTime>();

                while (current <= endDateMonthly)
                {
                    currentDates.Add(current);
                    current = current.AddMonths(1);
                }

                var entry = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                             where p.CompanyID == customer.CompanyID
                             && p.CustomerNo == customerNo
                             && currentDates.Contains(p.Month)
                             && p.ProductID == Customer_ConsumptionInsights.ProductID
                             select p).FirstOrDefault();

                if (entry != null)
                {
                    result.IsSuccess = true;
                    result.Message = "Data is available";
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "No Data";
                }

            }
            else
            {
                result.IsSuccess = false;
                result.Message = "No Data";
            }

            activityLog.UserID = customer.UserID;

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(result);

        }


        /// <summary>
        /// Invoice Data Availability
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [HttpPost]
        [Route("/WebServices/Customer/checkinvoicedata")]
        public async Task<IActionResult> CheckInvoice_View()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

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
                UserID = _userManager.GetUserId(User),
            };
            var Customer_Statement = new Customer_Statement();
            var result = new GenericResult();

            StreamReader reader = new StreamReader(Request.Body);

            try { Customer_Statement = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }

            Invoice_ViewModel model = new Invoice_ViewModel()
            {
                SelectedMonth = new DateTime(Customer_Statement.Year, Customer_Statement.Month, 1),
            };

            var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == customerNo).FirstOrDefault();
            if (customer == null)
                customer = new Data.Customer()
                {
                    CustomerNumber = customerNo,
                    CompanyID = companyID,
                    RecipientAddress = "Not Registered",
                    RecipientName = "Not Registered",
                    RecipientReferenceNumber = "Not Registered",
                    RecipientVATNumber = "Not Registered",
                };

            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
            model.TaxInvoiceTemplate = skyBillApiClient.GetTaxInvoiceTaxInvoiceTemplate(customer, company, model.SelectedMonth);


            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            if (model.TaxInvoiceTemplate == null)
            {
                result.IsSuccess = false;
                result.Message = "No Data";
            }
            else
            {
                result.IsSuccess = true;
                result.Message = "Data Available";
            }

            return Json(result);

        }


        /// <summary>
        /// Checks Current Billing Cycle Data Availability
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [HttpPost]
        [Route("/WebServices/Customer/checkbillingcycledata")]
        public async Task<IActionResult> CurrentBillingCycleDiv()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

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
                UserID = _userManager.GetUserId(User),
            };

            var Customer_Statement = new Customer_Statement();
            var result = new GenericResult();

            StreamReader reader = new StreamReader(Request.Body);

            try { Customer_Statement = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }

            CurrentBillingCycleModel model = new CurrentBillingCycleModel()
            {
                SelectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SummaryBlocks = new List<CurrentBillingCycleModel.SummaryBlock>(),
                CustomerNumber = customerNo,
            };

            model.SelectedMonth = new DateTime(Customer_Statement.Year, Customer_Statement.Month, 1);


            var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == customerNo).FirstOrDefault();

            if (localCustomer != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == localCustomer.CompanyID).SingleOrDefault();


                var resourceEntriesPerProductForCustomerHistory = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                                                   where p.CompanyID == companyID
                                                                   && p.CustomerNo == customerNo
                                                                   && p.Month == model.SelectedMonth
                                                                   select p).AsEnumerable();

                foreach (var entry in resourceEntriesPerProductForCustomerHistory)
                {
                    CurrentBillingCycleModel.SummaryBlock item = new CurrentBillingCycleModel.SummaryBlock()
                    {
                        Total = entry.Amount * -1.0m,
                    };

                    model.SummaryBlocks.Add(item);

                    if (model.TotalUsage > 0)
                        break;

                }

            }

            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            if (model.TotalUsage == 0)
            {
                result.IsSuccess = false;
                result.Message = "No Data";
            }
            else
            {
                result.IsSuccess = true;
                result.Message = "Data Available";
            }

            return Json(result);

        }

        /// <summary>
        /// Checks Statement Data Availability
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "Year": "int",
        ///         "Month": "int",
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [HttpPost]
        [Route("/WebServices/Customer/checkstatementdata")]
        public async Task<IActionResult> CheckStatementData()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

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
                UserID = _userManager.GetUserId(User),
            };

            var Customer_Statement = new Customer_Statement();
            var result = new GenericResult();

            StreamReader reader = new StreamReader(Request.Body);

            try { Customer_Statement = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer_Statement>(reader.ReadToEndAsync().Result); }
            catch { return StatusCode(400, "Invalid post"); }

            Statement_ViewModel model = new Statement_ViewModel()
            {
                SelectedMonth = new DateTime(Customer_Statement.Year, Customer_Statement.Month, 1)
            };


            var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == customerNo).FirstOrDefault();

            if (customer != null)
            {

                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                model.TaxInvoiceTemplate = skyBillApiClient.GetTenantConsumptionInvoiceTaxInvoiceTemplate(customerNo, company.Name, model.SelectedMonth, company, customer, true);

            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            if (model.TaxInvoiceTemplate == null)
            {
                result.IsSuccess = false;
                result.Message = "No Data";
            }
            else
            {
                result.IsSuccess = true;
                result.Message = "Data Available";
            }


            return Json(result);

        }

        /// <summary>
        /// Detailed Daily Billing
        /// </summary>
        /// <remarks></remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(BillingModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/DetailedDailyBilling")]
        public async Task<IActionResult> DetailedDailyBilling()
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

            BillingModel model = new BillingModel()
            {
                AllEntries = new PaginatedList<MyVoltage.Api.SkyBill.Ledger>(new List<MyVoltage.Api.SkyBill.Ledger>(), 0, 1, 1),
                CurrentBilling = new MyVoltage.Api.SkyBill.Ledger(),
                Customer = new MyVoltage.Api.SkyBill.Customer(),
                ExternalChargesSchedulingImports = new List<ExternalChargesSchedulingImport>(),
                Total = 0,
                TotalEntries = 0
            };

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber) && _clientzoneProvider.CompanyID > 0)
            {
                var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _clientzoneProvider.CustomerNumber && p.CompanyID == _clientzoneProvider.CompanyID).FirstOrDefault();

                if (localSkybillCustomer != null)
                    model.Customer = new MyVoltage.Api.SkyBill.Customer()
                    {
                        Address = localSkybillCustomer.Address,
                        AuxiliaryIndex1 = localSkybillCustomer.AuxiliaryIndex1,
                        AuxiliaryIndex2 = localSkybillCustomer.AuxiliaryIndex2,
                        AuxiliaryIndex3 = localSkybillCustomer.AuxiliaryIndex3,
                        AuxiliaryIndex4 = localSkybillCustomer.AuxiliaryIndex4,
                        AuxiliaryIndex5 = localSkybillCustomer.AuxiliaryIndex5,
                        Balance_LCY = (float)localSkybillCustomer.Balance_LCY,
                        BILLING_CYCLE = localSkybillCustomer.BILLING_CYCLE,
                        company = db.Companies.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault()
                    };

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                try
                {
                    var customer = skyBillApiClient.GetCustomer(_clientzoneProvider.CustomerNumber);
                    if (customer != null)
                        model.Customer = customer;
                }
                catch { }

                try
                {
                    BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                    List<MyVoltage.Api.SkyBill.Ledger> ledgerEntries = _billingProvider.GetLedgerEntriesByCustomer();
                    string page = _context.HttpContext.Request.Query["pageIndex"];

                    int? pageIndex = page != null ? Int32.Parse(page) : 1;
                    int pageSize = 1000;
                    model.AllEntries = await PaginatedList<MyVoltage.Api.SkyBill.Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);
                }
                catch { }

                MyVoltage.Api.SkyBill.Ledger currentBilling = null;
                if (model.AllEntries.Count > 0)
                {
                    currentBilling = model.AllEntries.FirstOrDefault(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo");
                    model.Total = (decimal)model.AllEntries.Sum(tbl => tbl.Original_Amount);
                }
                model.CurrentBilling = currentBilling != null ? currentBilling : new MyVoltage.Api.SkyBill.Ledger();
                model.TotalEntries = model.AllEntries.Count;

                model.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                          where p.CompanyID == _clientzoneProvider.CompanyID
                                                          && p.SkybillCustomerNo == _clientzoneProvider.CustomerNumber
                                                          select p).ToList();


            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Json(model);
        }

        /// <summary>
        /// Detailed Daily Billing
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "documentNo": "string",
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [HttpPost]
        [Route("/WebServices/Customer/{*documentNo}")]
        public async Task<IActionResult> Invoice(string documentNo)
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

            BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);

            byte[] pdf = _billingProvider.GetBillPdf(documentNo, _clientzoneProvider.CompanyName);


            string pdfName = Request.RouteValues["documentNo"].ToString();

            if (string.IsNullOrEmpty(pdfName))
            {
                pdfName = "document";
            }

            Response.Headers.Add("Content-Disposition", $"attachment;filename={pdfName}.pdf");

            if (pdf != null && pdf.Length > 0)
            {
                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }
                //return File(pdf, "application/pdf");

                return Json(new { data = pdf, name = pdfName });
            }
            else
            {
                return StatusCode(404, $"Error generating file");
            }
        }

        /// <summary>
        /// Real Time Insights
        /// </summary>
        /// <remarks> </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Realtimeinsights")]
        public async Task<IActionResult> RealTimeinsights()
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = string.Empty,
            };

            var db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();
            if (customer != null)
            {

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

                var products = db.SiteAdmin_Products.ToList();
                RealTimeConsumptionModel model = new RealTimeConsumptionModel()
                {
                    Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Text="--Select One--", Value = "", Selected = string.IsNullOrEmpty(Request.Query["p"].ToString()), },
                },
                    ShowMirrorKGReading = !string.IsNullOrEmpty(Request.Query["showmirrorkgreading"]) && Convert.ToBoolean(Request.Query["showmirrorkgreading"]) ? true : false,
                };

                model.Products.AddRange((from p in products
                                         where p.ChargeTypeID.HasValue
                                         && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption
                                         select new SelectListItem()
                                         {
                                             Text = p.ProductName,
                                             Value = p.ID.ToString(),
                                             Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == p.ID,
                                         }).ToList());

                activityLog.DateEnded = DateTime.Now;

                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                return Json(model);
            }
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Real Time Insights Yearly Usage
        /// </summary>
        /// <remarks> </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Realtimeinsights_YearlyUsage")]
        public async Task<IActionResult> YearlyUsage(int year, int month, string CustomerMeterSerial)
        {
            RealTimeConsumptionModel model = new RealTimeConsumptionModel()
            {
                AllMeters = new List<MyVoltage.Api.SkyBill.Customer>(),
                ShowMirrorKGReading = !string.IsNullOrEmpty(Request.Query["showmirrorkgreading"]) && Convert.ToBoolean(Request.Query["showmirrorkgreading"]) ? true : false,
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = string.Empty,
            };

            var db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();
            if (customer != null)
            {
                if (!string.IsNullOrEmpty(CustomerMeterSerial))
                {
                    var apiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                    var meters = apiClient.GetMetersByCustomer(customer.CustomerNumber);
                    model.AllMeters = meters;
                    model.MeterNumber = CustomerMeterSerial;

                    foreach (var meter in meters)
                    {
                        var localDev = dbCache.Devices.Where(p => p.Serial == meter.Serial_No).FirstOrDefault();
                        var device = _client.GetDeviceByMeterNumber(meter.Serial_No, localDev != null ? localDev.DeviceAPIIDValue : 1);

                        if (device != null)
                        {
                            meter.deviceType = device.type.type;
                            if (meter.Serial_No == CustomerMeterSerial)
                            {
                                model.Name = meter.No;
                                model.MeterColor = device.type.colorType;
                                model.MeterType = device.type.type;
                                model.UnitType = device.type.UnitType;
                            }
                        }
                    }

                    if (_clientzoneProvider.CustomerMeterDeviceType == DeviceType.DeviceTypeEnum.Gas)
                    {
                        var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == CustomerMeterSerial).FirstOrDefault();
                        if (mirrorDevice != null && mirrorDevice.ConvFactor.HasValue)
                        {
                            model.AllowMirrorKGReading = true;
                        }
                        else if (db.Companies.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault().ConvFactor.HasValue)
                        {
                            model.AllowMirrorKGReading = true;
                        }
                    }

                    if (!model.AllowMirrorKGReading)
                    {
                        model.ShowMirrorKGReading = false;
                    }

                    var today = DateTime.Now;

                    if (year != null && month != null)
                    {
                        try
                        {
                            today = new DateTime(Int32.Parse(year.ToString()), Int32.Parse(month.ToString()), today.Day);
                        }
                        catch (Exception e)
                        {
                            today = new DateTime(Int32.Parse(year.ToString()),
                                                 Int32.Parse(month.ToString()),
                            DateTime.DaysInMonth(Int32.Parse(year.ToString()),
                                                 Int32.Parse(month.ToString())));
                        }
                    }
                    BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                    List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(CustomerMeterSerial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_clientzoneProvider.AccountTypeForSelectedCustomer, _clientzoneProvider.ShowCostInclVAT);

                    int multiplier = -1;

                    if (_clientzoneProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                        multiplier = 1;

                    Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


                    model.ReadingDate = today;
                    model.MonthlyTotal = monthlyTotal;
                }
                return Json(model);
            }
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Real Time Insights Monthly Usage
        /// </summary>
        /// <remarks> </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Realtimeinsights_MonthlyUsage")]
        public async Task<IActionResult> MonthlyUsage(string meterNumber, string meterType, int year, bool showMirrorKGReading)
        {
            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = string.Empty,
            };
            var db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                if (meterType == "home")
                {
                    var rentModelJson = _billingProvider.GetMonthlyInvoiceAmountForRent(_clientzoneProvider.CustomerNumber, DateTime.Now.Year, (int)_clientzoneProvider.AccountTypeForSelectedCustomer, _clientzoneProvider.ShowCostInclVAT, meterNumber);

                    return Content(JsonConvert.SerializeObject(rentModelJson), "application/json");
                }

                var meterJsonModel = _usageProvider.GetMeterUsageByMonthView(meterNumber, meterType, year, 1, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, null);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Real Time Insights Daily Usage
        /// </summary>
        /// <remarks> </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Realtimeinsights_DailyUsage")]
        public async Task<IActionResult> DailyUsage(string meterNumber, string meterType, int year, int month, bool showMirrorKGReading, int? accountTypeOverride, int? meterTypeOverride)
        {

            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = string.Empty,
            };

            var db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var meterJsonModel = _usageProvider.GetMeterUsageByDayView(meterNumber, meterType, year, month, accountTypeOverride, meterTypeOverride, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, meterTypeOverride);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        /// <summary>
        /// Real Time Insights Hourly Usage
        /// </summary>
        /// <remarks> </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Error</response>
        [ProducesResponseType(typeof(GenericResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        [HttpGet]
        [Route("/WebServices/Customer/Realtimeinsights_HourlyUsage")]
        public async Task<IActionResult> HourlyUsage(string meterNumber, string meterType, int year, int month, int day, bool showMirrorKGReading)
        {

            string customerNo = "";
            int companyID = 0;

            if (!IsAuthenticated(out customerNo, out companyID))
                return StatusCode(401);

            GenericResult result = new GenericResult()
            {
                IsSuccess = false,
                Message = string.Empty,
            };

            var db = new MyVoltageDbContext(_options);

            var customer = (from p in db.Customers
                            where p.CustomerNumber == customerNo
                            && !p.IsDeleted
                            select p).FirstOrDefault();

            if (customer != null)
            {
                var meterJsonModel = _usageProvider.GetMeterUsageHourView(meterNumber, meterType, year, month, day, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, null);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

    }
}
