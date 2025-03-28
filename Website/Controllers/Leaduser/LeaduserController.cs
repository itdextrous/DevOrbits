using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.LeaduserModels;
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

namespace MyVoltage.Controllers.Leaduser
{
    [Authorize(Roles = "Leaduser")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LeaduserController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LeaduserProvider _leaduserProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _regEmail;
        private readonly string _devEmail;

        public LeaduserController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            LeaduserProvider leaduserProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _leaduserProvider = leaduserProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _signInManager = signInManager;
            _regEmail = configuration["RegEmail:Email"];
            _devEmail = configuration["DevEmail:Email"];
        }


        [HttpGet]
        [Route("/leaduser")]
        public async Task<IActionResult> Leaduser()
        {

            return View("~/Views/Leaduser/Leaduser.cshtml");
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("/LeaduserRegister")]
        public IActionResult LeaduserRegister(string returnUrl = null, string type = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            RegisterViewModel model = new RegisterViewModel()
            {
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/LeaduserRegister")]
        public IActionResult LeaduserRegister(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var companies = db.Companies.ToList();

            var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(String.Empty, _cache);

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

                    try { System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(model.Email); }
                    catch
                    {
                        ModelState.AddModelError("Email", "Invalid Email.");
                        return View(model);
                    }

                    try
                    {
                        Convert.ToInt64(model.PhoneNumber);
                        if (model.PhoneNumber.Length != 10)
                        {
                            ModelState.AddModelError("PhoneNumber", "Invalid Phone Number.");
                            return View(model);
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError("PhoneNumber", "Invalid Phone Number.");
                        return View(model);
                    }

                    try
                    {
                        Convert.ToInt64(model.PostalCode);
                    }
                    catch
                    {
                        ModelState.AddModelError("PostalCode", "Invalid Postal Code.");
                        return View(model);
                    }

                    if (ModelState.IsValid)
                    {
                        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                        var result = _userManager.CreateAsync(user, model.Password).Result;

                        if (result.Succeeded)
                        {
                            _userManager.AddToRoleAsync(user, UserRoleEnum.Leaduser.ToString());

                            Random random = new Random();
                            var numbers = "1234567890";
                            var stringNumbers = new char[4];

                            for (int i = 0; i < stringNumbers.Length; i++)
                            {
                                stringNumbers[i] = numbers[random.Next(numbers.Length)];
                            }

                            var OTPCode = new string(stringNumbers);

                            db.D01_LeadGeneratorUsers.Add(new Data.D01_LeadGeneratorUser
                            {
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
                                LocalUserID = user.Id,
                                IsDeleted = false,
                                OTPCode = OTPCode,
                                EmailCode = String.Empty,
                                IDNumberOrCompanyReg = model.IDNumberOrCompanyReg,
                                APIKey = Guid.NewGuid().ToString(),
                                ComplexName = "",
                                CreatedBy = "",
                                DateCreated = DateTime.Now,
                                LeadGeneratorID = 1,
                                LeadGeneratorUserName = model.FirstName + " " + model.LastName,
                                UnitNumber = "",
                            });

                            db.SaveChanges();

                            SMS.SendSms("27" + model.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + OTPCode + ". Enter this code during registration to verify your contact details.");
                            _emailSender.SendEmailCodeAsync(model.Email, OTPCode, model.FirstName + " " + model.LastName, user.Id, _context);

                            return Redirect($"/LeaduserRegisterOTPConfirm?id={user.Id}");
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
        [AllowAnonymous]
        [Route("/Leaduser/CheckEmail")]
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


        //[HttpGet]
        //[AllowAnonymous]
        //[Route("/LeaduserRegisterOTPConfirm")]
        //public IActionResult LeaduserRegisterOTPConfirm(string id, string returnUrl = null)
        //{
        //    ViewData["ReturnUrlOtp"] = returnUrl;
        //    ViewData["id"] = id;
        //    var db = new MyVoltageDbContext(_options);
        //    var model = new OTPConfirmViewModel
        //    {
        //        Email = "email@email.com",
        //        UserId = id,
        //        PhoneNumber = "0123456789",
        //    };
        //    var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();
        //    if (user != null)
        //    {
        //        var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
        //        if (customer != null)
        //        {
        //            if (!string.IsNullOrEmpty(Request.Query["pn"]))
        //            //if (!string.IsNullOrEmpty(customer.PhoneNumber))
        //            {
        //                SMS.SendSms("27" + Request.Query["pn"].ToString().Remove(0, 1), "Your My Voltage registration confirmation code is " + customer.OTPCode + ". Enter this code during registration to verify your contact details.");
        //                //Random random = new Random();
        //                //var numbers = "1234567890";
        //                //var stringNumbers = new char[4];

        //                //for (int i = 0; i < stringNumbers.Length; i++)
        //                //{
        //                //    stringNumbers[i] = numbers[random.Next(numbers.Length)];
        //                //}

        //                //var OTPCode = new string(stringNumbers);
        //                //customer.OTPCode = OTPCode;
        //                //SMS.SendSms("27" + customer.PhoneNumber.Remove(0, 1), "Your My Voltage registration confirmation code is " + customer.OTPCode + ". Enter this code during registration to verify your contact details.");
        //                //_emailSender.SendEmailCodeAsync(user.Email, customer.OTPCode, customer.FullName, user.Id, _context);
        //                //db.D01_LeadGeneratorUsers.Update(customer);
        //                //db.SaveChanges();

        //            }
        //        }
        //        model = new OTPConfirmViewModel
        //        {
        //            Email = user.Email,
        //            UserId = user.Id,
        //            //PhoneNumber = customer.PhoneNumber,
        //            PhoneNumber = !string.IsNullOrEmpty(Request.Query["pn"]) ? Request.Query["pn"].ToString() : customer.PhoneNumber,
        //        };
        //    }

        //    return View(model);
        //}

        [HttpGet]
        [AllowAnonymous]
        [Route("/LeaduserRegisterOTPConfirm")]
        public async Task<IActionResult> LeaduserRegisterOTPConfirm(string id, string returnUrl = null)
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
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();
                if (user != null)
                {
                    if (user.IsConfirmed)
                    {
                        return Redirect("/");
                    }

                    var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    if (customer == null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains(UserRoleEnum.Leaduser.ToString()))
                        {
                            return RedirectToAction("LeaduserRegisterOTPConfirm", "Leaduser", new { id = id, returnUrl = returnUrl });
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
                            db.D01_LeadGeneratorUsers.Update(customer);
                            db.SaveChanges();
                            return Json(new { status = true, msg = "OTP resent successfully." });
                        }
                    }
                    model = new OTPConfirmViewModel
                    {
                        Email = user.Email,
                        UserId = user.Id,
                        PhoneNumber = user.PhoneNumber != null ? user.PhoneNumber.ToString() : phoneNo
                    };
                }
                else
                {
                    return Redirect("/");
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
        [Route("/LeaduserRegisterOTPConfirm")]
        public async Task<IActionResult> LeaduserRegisterOTPConfirm(OTPConfirmViewModel model, string id, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    var user = db.Users.Where(tbl => tbl.Id == id).SingleOrDefault();

                    var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                    string OTP = $"{model.OTP1}{model.OTP2}{model.OTP3}{model.OTP4}";
                    if (customer.OTPCode == OTP)
                    {
                        if (!string.IsNullOrEmpty(model.PhoneNumber) && customer.PhoneNumber != model.PhoneNumber)
                        {
                            customer.PhoneNumber = model.PhoneNumber;
                            db.Update(customer);
                            db.SaveChanges();
                            customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                        }

                        user.IsConfirmed = true;

                        db.SaveChanges();

                        String email = "New user<br/> Email: " + user.Email + "<br/>" +
                                        " Phone Number: " + customer.PhoneNumber + "<br/>" +
                                        " Email Code: " + customer.EmailCode + "<br/>" +
                                        " OTP: " + customer.OTPCode + "<br/>" +
                                        " Full Name: " + customer.FullName + "<br/>" +
                                    " Alternative Phone Number: " + customer.AltPhoneNumber + "<br/>" +
                                    " ID Number Or Company Registration: " + customer.IDNumberOrCompanyReg + "<br/>" +
                                    " Unit Number: " + customer.UnitNumber + "<br/>" +
                                    " Street Address: " + customer.StreetAddress + "<br/>" +
                                    " Suburb: " + customer.Suburb + "<br/>" +
                                    " Town Or City: " + customer.TownOrCity + "<br/>" +
                                    " Province: " + customer.Province + "<br/>" +
                                    " Postal Code: " + customer.PostalCode + "<br/>";

                        //await _emailSender.SendUserDetailsAsync(_regEmail, email, customer.CustomerNumber, user.Email);

                        return Redirect("/LeaduserRegistrationSuccess?id=" + id);
                    }
                    else
                    {
                        ModelState.AddModelError("OTP1", "Please enter a valid OTP.");
                    }
                }
            }
            ViewData["id"] = id;
            return View(PopulateOTPConfirmViewModel(id));
        }

        private OTPConfirmViewModel PopulateOTPConfirmViewModel(string id)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

                var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                return new OTPConfirmViewModel
                {
                    Email = user.Email,
                    UserId = user.Id,
                    PhoneNumber = customer.PhoneNumber,
                };
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/LeaduserRegistrationSuccess")]
        public IActionResult LeaduserRegistrationSuccess(string id)
        {
            ViewData["id"] = id;
            var db = new MyVoltageDbContext(_options);
            var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

            if (user == null) return Redirect("/");

            var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id).SingleOrDefault();
            var model = new RegistrationSuccessViewModel
            {
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/LeaduserRegistrationSuccess")]
        public IActionResult LeaduserRegistrationSuccess(RegistrationSuccessViewModel model, string id)
        {
            ViewData["id"] = id;
            var db = new MyVoltageDbContext(_options);
            var user = db.Users.Where(tbl => tbl.Id == id && tbl.IsDeleted == false).SingleOrDefault();

            var customer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

            if (ModelState.IsValid)
            {
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
        [Route("/Leaduser/changeLeadUserID/{LeadUserID?}")]
        public async Task<IActionResult> ChangeLeadUserID(string LeadUserID)
        {
            HttpContext.Session.SetString(LeaduserProvider.SESSION_TASKS_SELECTEDLEADUSERID, string.IsNullOrEmpty(LeadUserID) ? "" : LeadUserID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/Leaduser/changeLeadID/{LeadID?}")]
        public async Task<IActionResult> ChangeLeadID(string LeadID)
        {
            HttpContext.Session.SetString(LeaduserProvider.SESSION_TASKS_SELECTEDLEADID, string.IsNullOrEmpty(LeadID) ? "" : LeadID);

            return Redirect(Request.Query["R"]);
        }

        #region Leads

        [HttpGet]
        [Route("/leaduser/D01_Leads_LogLead")]
        public async Task<IActionResult> D01_Leads_LogLead()
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_LogLeadModel model = new D01_Leads_LogLeadModel()
            {
                ProductID = (from p in db.D01_Products
                             select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                             {
                                 Text = p.Name,
                                 Value = p.ID.ToString(),
                             }).ToList(),
            };

            model.ProductID.Insert(0, new SelectListItem() { Value = "", Text = "[I don't know yet]" });
            model.ProductID = model.ProductID.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).SingleOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/leaduser/D01_Leads_LogLead.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_LogLead")]
        public async Task<IActionResult> D01_Leads_LogLead(int ContactID, int PropertyID, int ProductID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (ContactID == 0 && PropertyID == 0)
            {
                return Content("false:0");
            }

            try
            {
                var localUserID = _userManager.GetUserId(User);
                var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                             where p.LocalUserID == localUserID
                                             select p).FirstOrDefault();

                if (d01_LeadGeneratorUser == null)
                {
                    var localUserOp = db.OperationalProfiles.Where(p => p.UserID == localUserID).FirstOrDefault();
                    d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                    {
                        APIKey = Guid.NewGuid().ToString().ToUpper(),
                        LocalUserID = localUserID,
                        CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                        DateCreated = DateTime.Now,
                        LeadGeneratorID = 2, // Operational Referral
                        LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    };
                    db.Add(d01_LeadGeneratorUser);
                    db.SaveChanges();
                }

                Data.D01_Lead d01_Lead = new D01_Lead()
                {
                    DateCreated = DateTime.Now,
                    StatusID = (int)Data.D01_Lead.StatusEnum.New,
                    UserID = localUserID,
                    AssignedToUserID = localUserID,
                    LeadGeneratorUserID = d01_LeadGeneratorUser.ID,
                };

                if (ProductID != 0)
                    d01_Lead.ProductID = ProductID;

                db.Add(d01_Lead);
                db.SaveChanges();

                if (ContactID != 0)
                {
                    D01_Leads_Contact d01_Leads_Contact = new D01_Leads_Contact()
                    {
                        ContactID = ContactID,
                        LeadID = d01_Lead.ID,
                    };

                    db.Add(d01_Leads_Contact);
                    db.SaveChanges();
                }

                if (PropertyID != 0)
                {
                    D01_Leads_Property d01_Leads_Property = new D01_Leads_Property()
                    {
                        PropertyID = PropertyID,
                        LeadID = d01_Lead.ID,
                    };

                    db.Add(d01_Leads_Property);
                    db.SaveChanges();
                }

                return Content($"true:{d01_Lead.ID}");
            }
            catch
            {
                return Content("false:0");
            }
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_MyLeads")]
        public async Task<IActionResult> D01_Leads_MyLeads()
        {

            D01_Leads_MyLeadsModel model = new D01_Leads_MyLeadsModel()
            {
                D01_Leads_MyLeadsItems = new List<D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem>(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_leaduserProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _leaduserProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
            };

            var db = new MyVoltageDbContext(_options);
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Contacts = db.D01_Contacts.ToList();
            var d01_Properties = db.D01_Properties.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Products = db.D01_Products.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var partners = db.SiteAdmin_Partners.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = _leaduserProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();
            var leadsForUser = new List<D01_Lead>();
            leadsForUser = (from p in db.D01_Leads
                            where p.UserID == _userManager.GetUserId(User)
                            || p.AssignedToUserID == _userManager.GetUserId(User)
                            orderby p.DateCreated descending
                            select p).Take(1000).ToList();

            var d01_Leads_Properties = (from pc in db.D01_Leads_Properties
                                        select pc).ToList();
            var d01_Leads_Contacts = (from pc in db.D01_Leads_Contacts
                                      select pc).ToList();


            foreach (var lead in leadsForUser)
            {
                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem leadsItem = new D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem()
                {
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    PropertyID = lead.PropertyID,
                    ProductID = lead.ProductID,
                    ContactID = lead.ContactID,
                    DateCreated = lead.DateCreated,
                    ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                };


                #region Properties

                var this_d01_Leads_Properties = (from pc in d01_Leads_Properties
                                                 where pc.LeadID == lead.ID
                                                 select pc).ToList();

                var thisLeadD01_Properties = (from pc in d01_Properties
                                              where this_d01_Leads_Properties.Select(c => c.PropertyID).Contains(pc.ID)
                                              select pc).ToList();

                foreach (var p in thisLeadD01_Properties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        Address = p.Address,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        MunicipalityID = p.MunicipalityID,
                        NeedsIdentified = p.NeedsIdentified,
                        NextFollowUpDate = p.NextFollowUpDate,
                        OverallStatus = p.OverallStatus,
                        PartnerName = p.OverallStatus,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusChangeDate = p.StatusChangeDate,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusID = p.StatusID,
                        ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        LeadGeneratorName = "",
                        ProvinceName = "",
                        StatusChangeUserName = "",
                    };

                    if (p.PropertyTypeID.HasValue)
                    {
                        var companyType = companyTypes.Where(c => c.ID == p.PropertyTypeID.Value).SingleOrDefault();
                        if (companyType != null)
                        {
                            item.CompanyTypeName = companyType.CompanyTypeName;
                        }
                    }
                    if (p.PartnerID.HasValue)
                    {
                        var partner = partners.Where(c => c.ID == p.PartnerID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.PartnerName = partner.PartnerName;
                        }
                    }

                    var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}";


                    var thisPropertyContactsLinks = (from pc in d01_Properties_Contacts
                                                     where pc.PropertyID == p.ID
                                                     select pc).ToList();

                    var thisPropertyContacts = (from pc in d01_Contacts
                                                where thisPropertyContactsLinks.Select(c => c.ContactID).Contains(pc.ID)
                                                select pc).ToList();

                    foreach (var pc in thisPropertyContacts)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Contacts_EditModel.ContactsItem itemC = new Contacts_EditModel.ContactsItem()
                        {
                            Email = pc.Email,
                            FullName = pc.FullName,
                            PhoneNumber = pc.PhoneNumber,
                            PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                        };

                        #endregion

                        item.ContactsItems.Add(itemC);
                    }

                    if (item.ContactsItems.Count == 0)
                    {
                        item.ContactsItems.Add(new Contacts_EditModel.ContactsItem()
                        {
                            FullName = "-",
                            PhoneNumber = "-",
                            Email = "-",
                        });
                    }
                    leadsItem.PropertiesItems.Add(item);
                }


                #endregion

                #region Contacts

                var thisd01_Leads_Contacts = (from pc in d01_Leads_Contacts
                                              where pc.LeadID == lead.ID
                                              select pc).ToList();

                var thisLeadd01_Contacts = (from pc in d01_Contacts
                                            where thisd01_Leads_Contacts.Select(c => c.ContactID).Contains(pc.ID)
                                            select pc).ToList();

                foreach (var p in thisLeadd01_Contacts)
                {
                    #region Contacts_EditModel.ContactsItem

                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        ID = p.ID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusChangeDate = p.StatusChangeDate,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadGeneratorName = "",
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        NeedsIdentified = p.NeedsIdentified,
                        PartnerName = p.NeedsIdentified,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusID = p.StatusID,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        MunicipalityID = p.MunicipalityID,
                        StatusChangeUserName = "",
                        ProvinceName = "",
                        CompanyID = p.CompanyID,
                        AltPhoneNumber = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedBy = p.CreatedBy,
                        DateCreated = p.DateCreated,
                        Email = p.Email,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        Position = p.Position,
                        PostalCode = p.PostalCode,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        OverallStatus = p.OverallStatus,
                        NextFollowUpDate = p.NextFollowUpDate,
                        PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    };

                    if (p.MunicipalityID.HasValue)
                    {
                        var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.ProvinceName = partner.Province.GetDescription();
                        }
                    }

                    var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdByContact != null)
                    {
                        item.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                        item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                    }
                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

                    var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                        item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                    else
                    {
                        var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                        if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                        {
                            item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                        }
                    }

                    #region Properties_EditModel.PropertiesItem


                    var thisContactPropertiesLinks = (from pc in d01_Properties_Contacts
                                                      where pc.ContactID == p.ID
                                                      select pc).ToList();

                    var thisContactProperties = (from pc in d01_Properties
                                                 where thisContactPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                 select pc).ToList();

                    foreach (var pc in thisContactProperties)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                        {
                            Name = pc.Name,
                            ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        };

                        #endregion

                        item.PropertiesItems.Add(itemC);
                    }

                    if (item.PropertiesItems.Count == 0)
                    {
                        item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                        {
                            Name = "-",
                        });
                    }

                    #endregion



                    #endregion


                    leadsItem.ContactsItems.Add(item);
                }


                #endregion

                if (lead.ProductID.HasValue)
                {
                    leadsItem.D01_Product = d01_Products.Where(p => p.ID == lead.ProductID.Value).SingleOrDefault();
                }

                if (lead.LeadGeneratorUserID.HasValue)
                {
                    var d01_LeadGeneratorUser = d01_LeadGeneratorUsers.Where(p => p.ID == lead.LeadGeneratorUserID.Value).SingleOrDefault();
                    if (d01_LeadGeneratorUser != null)
                    {
                        var d01_LeadGenerator = d01_LeadGenerators.Where(p => p.ID == d01_LeadGeneratorUser.LeadGeneratorID).SingleOrDefault();
                        if (d01_LeadGenerator != null)
                            leadsItem.LeadGeneratorName = d01_LeadGenerator.LeadGeneratorName;
                    }
                }

                model.D01_Leads_MyLeadsItems.Add(leadsItem);
            }

            return View("~/Views/leaduser/D01_Leads_MyLeads.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead/{leadID}")]
        public async Task<IActionResult> A08_Task_Review(int leadID)
        {
            return Redirect($"/Leaduser/changeLeadID/{leadID}?R=/leaduser/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead")]
        public async Task<IActionResult> D01_Leads_ViewLead()
        {
            if (_leaduserProvider.SelectedLeadID == 0)
                return Redirect("/leaduser/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);

            D01_Leads_ViewLeadModel model = new D01_Leads_ViewLeadModel()
            {
                ResponsibleUser = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                ProductID = new List<SelectListItem>(),
                Status = new List<SelectListItem>(),
            };

            var lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var partners = db.SiteAdmin_Partners.ToList();

            if (lead != null)
            {
                var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
                var d01_Contacts = db.D01_Contacts.ToList();
                var d01_Properties = db.D01_Properties.ToList();
                var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
                var operationalProfiles = db.OperationalProfiles.ToList();

                model.Status = (from p in db.D01_Leads_Statuses
                                where p.ID >= lead.StatusID
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = ((int)p.ID).ToString(),
                                    Selected = lead.StatusID == p.ID,
                                }).ToList();

                model.ProductID = (from p in db.D01_Products
                                   select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                   {
                                       Text = p.Name,
                                       Value = p.ID.ToString(),
                                       Selected = lead.ProductID.HasValue && lead.ProductID.Value == p.ID,
                                   }).ToList();

                model.ProductID.Insert(0, new SelectListItem() { Value = "", Text = "[I don't know yet / Not applicable]", Selected = !lead.ProductID.HasValue });
                model.ProductID = model.ProductID.OrderBy(p => p.Text).ToList();


                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLeadModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    D01_Leads_ViewLead_Logs = new List<D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Log>(),
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    D01_Leads_ViewLead_Attachments = new List<D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Attachment>(),
                    ContactID = lead.ContactID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ProductID = lead.ProductID,
                    PropertyID = lead.PropertyID,
                    ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    LeadGeneratorName = "",
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                };

                #region Properties

                var d01_Leads_Properties = (from pc in db.D01_Leads_Properties
                                            where pc.LeadID == lead.ID
                                            select pc).ToList();

                var thisLeadD01_Properties = (from pc in d01_Properties
                                              where d01_Leads_Properties.Select(c => c.PropertyID).Contains(pc.ID)
                                              select pc).ToList();

                foreach (var p in thisLeadD01_Properties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        Address = p.Address,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        MunicipalityID = p.MunicipalityID,
                        NeedsIdentified = p.NeedsIdentified,
                        NextFollowUpDate = p.NextFollowUpDate,
                        OverallStatus = p.OverallStatus,
                        PartnerName = p.OverallStatus,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusChangeDate = p.StatusChangeDate,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusID = p.StatusID,
                        ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        LeadGeneratorName = "",
                        ProvinceName = "",
                        StatusChangeUserName = "",
                    };

                    if (p.PropertyTypeID.HasValue)
                    {
                        var companyType = companyTypes.Where(c => c.ID == p.PropertyTypeID.Value).SingleOrDefault();
                        if (companyType != null)
                        {
                            item.CompanyTypeName = companyType.CompanyTypeName;
                        }
                    }
                    if (p.PartnerID.HasValue)
                    {
                        var partner = partners.Where(c => c.ID == p.PartnerID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.PartnerName = partner.PartnerName;
                        }
                    }

                    var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}";


                    var thisPropertyContactsLinks = (from pc in d01_Properties_Contacts
                                                     where pc.PropertyID == p.ID
                                                     select pc).ToList();

                    var thisPropertyContacts = (from pc in d01_Contacts
                                                where thisPropertyContactsLinks.Select(c => c.ContactID).Contains(pc.ID)
                                                select pc).ToList();

                    foreach (var pc in thisPropertyContacts)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Contacts_EditModel.ContactsItem itemC = new Contacts_EditModel.ContactsItem()
                        {
                            Email = pc.Email,
                            FullName = pc.FullName,
                            PhoneNumber = pc.PhoneNumber,
                            PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                        };

                        #endregion

                        item.ContactsItems.Add(itemC);
                    }

                    if (item.ContactsItems.Count == 0)
                    {
                        item.ContactsItems.Add(new Contacts_EditModel.ContactsItem()
                        {
                            FullName = "-",
                            PhoneNumber = "-",
                            Email = "-",
                        });
                    }
                    model.D01_Leads_ViewLeadItem.PropertiesItems.Add(item);
                }


                #endregion

                #region Contacts

                var d01_Leads_Contacts = (from pc in db.D01_Leads_Contacts
                                          where pc.LeadID == lead.ID
                                          select pc).ToList();

                var thisLeadd01_Contacts = (from pc in d01_Contacts
                                            where d01_Leads_Contacts.Select(c => c.ContactID).Contains(pc.ID)
                                            select pc).ToList();

                foreach (var p in thisLeadd01_Contacts)
                {
                    #region Contacts_EditModel.ContactsItem

                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        ID = p.ID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusChangeDate = p.StatusChangeDate,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadGeneratorName = "",
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        NeedsIdentified = p.NeedsIdentified,
                        PartnerName = p.NeedsIdentified,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusID = p.StatusID,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        MunicipalityID = p.MunicipalityID,
                        StatusChangeUserName = "",
                        ProvinceName = "",
                        CompanyID = p.CompanyID,
                        AltPhoneNumber = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedBy = p.CreatedBy,
                        DateCreated = p.DateCreated,
                        Email = p.Email,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        Position = p.Position,
                        PostalCode = p.PostalCode,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        OverallStatus = p.OverallStatus,
                        NextFollowUpDate = p.NextFollowUpDate,
                        PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    };

                    if (p.MunicipalityID.HasValue)
                    {
                        var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.ProvinceName = partner.Province.GetDescription();
                        }
                    }

                    var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdByContact != null)
                    {
                        item.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                        item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                    }
                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

                    var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                        item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                    else
                    {
                        var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                        if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                        {
                            item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                        }
                    }

                    #region Properties_EditModel.PropertiesItem


                    var thisContactPropertiesLinks = (from pc in d01_Properties_Contacts
                                                      where pc.ContactID == p.ID
                                                      select pc).ToList();

                    var thisContactProperties = (from pc in d01_Properties
                                                 where thisContactPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                 select pc).ToList();

                    foreach (var pc in thisContactProperties)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                        {
                            Name = pc.Name,
                            ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        };

                        #endregion

                        item.PropertiesItems.Add(itemC);
                    }

                    if (item.PropertiesItems.Count == 0)
                    {
                        item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                        {
                            Name = "-",
                        });
                    }

                    #endregion



                    #endregion


                    model.D01_Leads_ViewLeadItem.ContactsItems.Add(item);
                }


                #endregion

                if (lead.ProductID.HasValue)
                {
                    model.D01_Leads_ViewLeadItem.D01_Product = db.D01_Products.Where(p => p.ID == lead.ProductID.Value).SingleOrDefault();
                }

                if (lead.LeadGeneratorUserID.HasValue)
                {
                    var d01_LeadGeneratorUser = d01_LeadGeneratorUsers.Where(p => p.ID == lead.LeadGeneratorUserID.Value).SingleOrDefault();
                    if (d01_LeadGeneratorUser != null)
                    {
                        var d01_LeadGenerator = d01_LeadGenerators.Where(p => p.ID == d01_LeadGeneratorUser.LeadGeneratorID).SingleOrDefault();
                        if (d01_LeadGenerator != null)
                            model.D01_Leads_ViewLeadItem.LeadGeneratorName = d01_LeadGenerator.LeadGeneratorName;
                    }
                }

                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Not Assigned]", Selected = string.IsNullOrEmpty(lead.AssignedToUserID) ? true : false });
                foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
                {
                    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{user.FullName}", Selected = lead.AssignedToUserID == user.LocalUserID ? true : false });
                }


                var d01_Leads_Logs = db.D01_Leads_Logs.Where(p => p.LeadID == lead.ID).ToList();
                foreach (var log in d01_Leads_Logs)
                {
                    string loguserName = "";
                    var logop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == log.UserID).SingleOrDefault();
                    if (logop != null)
                    {
                        loguserName = logop.FullName;
                    }
                    else
                    {
                        loguserName = _userManager.FindByIdAsync(log.UserID).Result.UserName;
                    }

                    model.D01_Leads_ViewLeadItem.D01_Leads_ViewLead_Logs.Add(new D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Log()
                    {
                        Approval_ProceedToContract = log.Approval_ProceedToContract,
                        Approval_ProfitAnalysisConducted = log.Approval_ProfitAnalysisConducted,
                        DateCreated = log.DateCreated,
                        ID = log.ID,
                        LeadID = log.LeadID,
                        Sales_DocumentsObtained = log.Sales_DocumentsObtained,
                        Sales_HeadOfficeRequired = log.Sales_HeadOfficeRequired,
                        Sales_LastContactDescription = log.Sales_LastContactDescription,
                        Sales_NextContactDate = log.Sales_NextContactDate,
                        Sales_NextContactPerson = log.Sales_NextContactPerson,
                        SystemDescription = log.SystemDescription,
                        Technical_Date = log.Technical_Date,
                        Technical_DoPreliminaryAudit = log.Technical_DoPreliminaryAudit,
                        Technical_PreliminaryNetworkAudit = log.Technical_PreliminaryNetworkAudit,
                        Technical_TechnicianInstructed = log.Technical_TechnicianInstructed,
                        UserID = log.UserID,
                        Username = loguserName,
                    });
                }

                var d01_Leads_Attachments = db.D01_Leads_Attachments.Where(p => p.LeadID == lead.ID).ToList();
                foreach (var attachment in d01_Leads_Attachments)
                {
                    string attachmentuserName = "";
                    var attachmentop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == attachment.UserID).SingleOrDefault();
                    if (attachmentop != null)
                    {
                        attachmentuserName = attachmentop.FullName;
                    }
                    else
                    {
                        attachmentuserName = _userManager.FindByIdAsync(attachment.UserID).Result.UserName;
                    }

                    model.D01_Leads_ViewLeadItem.D01_Leads_ViewLead_Attachments.Add(new D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Attachment()
                    {
                        DateCreated = attachment.DateCreated,
                        ID = attachment.ID,
                        LeadID = attachment.LeadID,
                        UserID = attachment.UserID,
                        Username = attachmentuserName,
                        AttachmentTypeID = attachment.AttachmentTypeID,
                        Filename = attachment.Filename,
                        Description = attachment.Description,
                        IsDeleted = attachment.IsDeleted,
                    });
                }

            }


            return View("~/Views/leaduser/D01_Leads_ViewLead.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_ViewLead_TrackingInformation")]
        public async Task<IActionResult> D01_Leads_ViewLead_TrackingInformation(string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Leads_Statuses = db.D01_Leads_Statuses.ToList();
            var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
            if (d01_Lead != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (string.IsNullOrEmpty(d01_Lead.AssignedToUserID)
                    || d01_Lead.AssignedToUserID != responsibleUser)
                {
                    if (!string.IsNullOrEmpty(responsibleUser))
                    {
                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                        if (string.IsNullOrEmpty(d01_Lead.AssignedToUserID))
                        {
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        else if (responsibleUser.ToString() != d01_Lead.AssignedToUserID)
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        d01_Lead.AssignedToUserID = responsibleUser.ToString();
                        //d01_Lead.ResponsibleUserTimestamp = DateTime.Now;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(d01_Lead.AssignedToUserID))
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                            d01_Lead.AssignedToUserID = "";
                            //d01_Lead.ResponsibleUserTimestamp = DateTime.Now;
                        }
                    }
                }

                if (status > 0)
                {
                    if (d01_Lead.StatusID != status)
                    {
                        D01_Lead_Status_Log d01_Lead_Status_Log = new D01_Lead_Status_Log()
                        {
                            D01_LeadID = d01_Lead.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_Lead.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),

                        };

                        var newStatus = d01_Leads_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_Lead_Status_Log.StatusAfterText = newStatus.StatusName;
                        var oldStatus = d01_Leads_Statuses.Where(p => p.ID == d01_Lead.StatusID).SingleOrDefault();
                        if (oldStatus != null)
                        {
                            sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                            d01_Lead_Status_Log.StatusBeforeText = oldStatus.StatusName;
                        }
                        else
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");

                        d01_Lead.StatusID = status;
                        //d01_Lead.StatusChangeDate = DateTime.Now;
                        //d01_Lead.StatusChangeUserID = _userManager.GetUserId(User);


                        db.Add(d01_Lead_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Lead);
                    db.SaveChanges();

                    D01_Leads_Log d01_Leads_Log = new D01_Leads_Log()
                    {
                        Approval_ProceedToContract = "",
                        Approval_ProfitAnalysisConducted = "",
                        DateCreated = DateTime.Now,
                        LeadID = d01_Lead.ID,
                        Sales_DocumentsObtained = "",
                        Sales_HeadOfficeRequired = "",
                        Sales_LastContactDescription = "",
                        Sales_NextContactDate = null,
                        Sales_NextContactPerson = "",
                        SystemDescription = sbSysLog.ToString(),
                        Technical_Date = null,
                        Technical_DoPreliminaryAudit = "",
                        Technical_PreliminaryNetworkAudit = "",
                        Technical_TechnicianInstructed = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(d01_Leads_Log);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_D01_Leads);
                    _cache.Remove(MVCache.KEY_D01_Leads_Logs);
                }
            }

            return Redirect($"/leaduser/D01_Leads_ViewLead");
        }



        [HttpPost]
        [Route("/leaduser/D01_Leads_ViewLead_Product")]
        public async Task<IActionResult> D01_Leads_ViewLead_Product(int ProductID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Leads_Statuses = db.D01_Leads_Statuses.ToList();
            var d01_Products = db.D01_Products.ToList();
            var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
            if (d01_Lead != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                {
                    var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Request.Form["ProductID"])).SingleOrDefault();
                    if (!d01_Lead.ProductID.HasValue)
                    {
                        sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ProductID"]) != d01_Lead.ProductID.Value)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Lead.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Lead.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                }
                else
                {
                    if (d01_Lead.ProductID.HasValue)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Lead.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID Removed<br />");
                        d01_Lead.ProductID = null;
                    }
                }
                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Lead);
                    db.SaveChanges();

                    D01_Leads_Log d01_Leads_Log = new D01_Leads_Log()
                    {
                        Approval_ProceedToContract = "",
                        Approval_ProfitAnalysisConducted = "",
                        DateCreated = DateTime.Now,
                        LeadID = d01_Lead.ID,
                        Sales_DocumentsObtained = "",
                        Sales_HeadOfficeRequired = "",
                        Sales_LastContactDescription = "",
                        Sales_NextContactDate = null,
                        Sales_NextContactPerson = "",
                        SystemDescription = sbSysLog.ToString(),
                        Technical_Date = null,
                        Technical_DoPreliminaryAudit = "",
                        Technical_PreliminaryNetworkAudit = "",
                        Technical_TechnicianInstructed = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(d01_Leads_Log);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_D01_Leads);
                    _cache.Remove(MVCache.KEY_D01_Leads_Logs);
                }
            }

            return Redirect($"/leaduser/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_AddAttachment")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddAttachment()
        {
            if (_leaduserProvider.SelectedLeadID == 0)
                return Redirect("/leaduser/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            D01_Leads_ViewLead_AddAttachmentModel model = new D01_Leads_ViewLead_AddAttachmentModel()
            {
                AttachmentType = (from p in ((D01_Leads_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Leads_Attachment.AttachmentTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
            };


            var lead = dbCache.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

            if (lead != null)
            {
                var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLead_AddAttachmentModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    ContactID = lead.ContactID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ProductID = lead.ProductID,
                    PropertyID = lead.PropertyID,
                };

            }


            return View("~/Views/leaduser/D01_Leads_ViewLead_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_ViewLead_AddAttachment")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddAttachment(D01_Leads_ViewLead_AddAttachmentModel model)
        {
            if (_leaduserProvider.SelectedLeadID == 0)
                return Redirect("/leaduser/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            model.AttachmentType = (from p in ((D01_Leads_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Leads_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();


            var lead = dbCache.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

            if (lead != null)
            {
                var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLead_AddAttachmentModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    PropertyID = lead.PropertyID,
                    ProductID = lead.ProductID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ContactID = lead.ContactID,
                };


                if (ModelState.IsValid)
                {
                    if (model.Attachment != null)
                    {
                        // Name of the share, directory, and file we'll create
                        string shareName = "d01-leads-attachments";
                        string dirName = $"{lead.ID}";
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.Attachment.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.UploadRange(
                            new HttpRange(0, uploadFile.Length),
                            uploadFile);

                        Data.D01_Leads_Attachment d01_Leads_Attachment = new D01_Leads_Attachment()
                        {
                            AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                            DateCreated = DateTime.Now,
                            Filename = fileName,
                            LeadID = lead.ID,
                            UserID = _userManager.GetUserId(User),
                            Description = model.Description,
                            IsDeleted = false,
                        };

                        _cache.Remove(MVCache.KEY_D01_Leads_Attachments);

                        db.Add(d01_Leads_Attachment);
                        db.SaveChanges();

                        model.IsSuccess = true;

                    }
                }
            }


            return View("~/Views/leaduser/D01_Leads_ViewLead_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_GetAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> D01_Leads_ViewLead_GetAttachment(int leadAttachmentID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var d01_Leads_Attachment = dbCache.D01_Leads_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/leaduser/D01_Leads_ViewLead");


            string shareName = "d01-leads-attachments";
            string dirName = $"{d01_Leads_Attachment.LeadID}";
            string fileName = d01_Leads_Attachment.Filename;

            // Get a reference to the file
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
            ShareFileClient file = directory.GetFileClient(fileName);

            // Download the file
            ShareFileDownloadInfo download = file.Download();
            Stream uploadFile = new MemoryStream();
            download.Content.CopyTo(uploadFile);
            uploadFile.Position = 0;
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(d01_Leads_Attachment.Filename));


            return Redirect("/leaduser/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_DeleteAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteAttachment(int leadAttachmentID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Leads_Attachment = db.D01_Leads_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/leaduser/D01_Leads_ViewLead");

            d01_Leads_Attachment.IsDeleted = true;
            db.Update(d01_Leads_Attachment);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_D01_Leads_Attachments);

            return Redirect("/leaduser/D01_Leads_ViewLead");
        }


        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_AddContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddContact()
        {
            if (_leaduserProvider.SelectedLeadID == 0)
                return Redirect("/leaduser/D01_Leads_MyLeads");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_ViewLead_AddContactModel model = new D01_Leads_ViewLead_AddContactModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/leaduser/D01_Leads_ViewLead_AddContact.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_ViewLead_AddContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddContact(D01_Leads_ViewLead_AddContactModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Form["ContactID"])
                && _leaduserProvider.SelectedLeadID != 0)
            {
                var existing = (from p in db.D01_Leads_Contacts
                                where p.ContactID == Convert.ToInt32(Request.Form["ContactID"])
                                && p.LeadID == _leaduserProvider.SelectedLeadID
                                select p).SingleOrDefault();

                var contact = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Form["ContactID"])).SingleOrDefault();
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

                if (existing == null && contact != null && d01_Lead != null)
                {
                    D01_Leads_Contact d01_Leads_Contact = new D01_Leads_Contact()
                    {
                        ContactID = Convert.ToInt32(Request.Form["ContactID"]),
                        LeadID = _leaduserProvider.SelectedLeadID,
                    };

                    db.Add(d01_Leads_Contact);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultContactID = Convert.ToInt32(Request.Form["ContactID"]);
                    model.ResultLeadID = _leaduserProvider.SelectedLeadID;
                }
            }

            return Content("false");
            return View("~/Views/leaduser/Contacts/AddContactToProperty.cshtml", model);
        }

        [Route("/leaduser/D01_Leads_ViewLead_AddContact_SearchContacts")]
        public JsonResult D01_Leads_ViewLead_AddContact_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).SingleOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_DeleteContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]) && _leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Leads_Contact = db.D01_Leads_Contacts.Where(p => p.ContactID == Convert.ToInt32(Request.Query["ContactID"]) && p.LeadID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

                if (d01_Leads_Contact != null)
                {
                    db.Remove(d01_Leads_Contact);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Contact")
                return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}");
            else
                return Redirect($"/leaduser/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_AddProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddProperty()
        {
            if (_leaduserProvider.SelectedLeadID == 0)
                return Redirect("/leaduser/D01_Leads_MyLeads");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_ViewLead_AddPropertyModel model = new D01_Leads_ViewLead_AddPropertyModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var contacts = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.PropertyID = $"{contacts.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/leaduser/D01_Leads_ViewLead_AddProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_ViewLead_AddProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddProperty(D01_Leads_ViewLead_AddPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var d01_Property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = d01_Property.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).SingleOrDefault();
                model.PropertyID = $"{d01_Property.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Form["PropertyID"])
                && _leaduserProvider.SelectedLeadID != 0)
            {
                var existing = (from p in db.D01_Leads_Properties
                                where p.PropertyID == Convert.ToInt32(Request.Form["PropertyID"])
                                && p.LeadID == _leaduserProvider.SelectedLeadID
                                select p).SingleOrDefault();

                var property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyID"])).SingleOrDefault();
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

                if (existing == null && property != null && d01_Lead != null)
                {
                    D01_Leads_Property d01_Leads_Property = new D01_Leads_Property()
                    {
                        PropertyID = Convert.ToInt32(Request.Form["PropertyID"]),
                        LeadID = _leaduserProvider.SelectedLeadID,
                    };

                    db.Add(d01_Leads_Property);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultPropertyID = Convert.ToInt32(Request.Form["PropertyID"]);
                    model.ResultLeadID = _leaduserProvider.SelectedLeadID;
                }
            }

            return Content("false");
        }

        [Route("/leaduser/D01_Leads_ViewLead_AddProperty_SearchPropertys")]
        public JsonResult D01_Leads_ViewLead_AddProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).SingleOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_ViewLead_DeleteProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]) && _leaduserProvider.SelectedLeadID != 0)
            {
                var d01_Leads_Property = db.D01_Leads_Properties.Where(p => p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"]) && p.LeadID == _leaduserProvider.SelectedLeadID).SingleOrDefault();

                if (d01_Leads_Property != null)
                {
                    db.Remove(d01_Leads_Property);
                    db.SaveChanges();
                }
            }

            return Redirect($"/leaduser/D01_Leads_ViewLead");

            if (Request.Query["R"].ToString() == "Property")
                return Redirect($"/leaduser/D01_Leads_Propertys_Edit/{Request.Query["PropertyID"]}");
            else
                return Redirect($"/leaduser/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/leaduser/contactus")]
        public async Task<IActionResult> ContactUs()
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
                UserID = _userManager.GetUserId(User),
            };

            ContactUsModel model = new ContactUsModel()
            {
                Sent = !string.IsNullOrEmpty(Request.Query["sent"].ToString()),
            };

            var user = _userManager.GetUserAsync(User).Result;

            if (!string.IsNullOrEmpty(_leaduserProvider.D01_LeadGeneratorUser.IDNumberOrCompanyReg))
            {
                var localCustomer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();
                if (localCustomer != null)
                {
                    model.Name = localCustomer.FullName;
                    model.PhoneNumber = localCustomer.PhoneNumber;
                    model.Email = user.Email;
                }
            }

            activityLog.DateEnded = DateTime.Now;

            db.Add(activityLog);
            db.SaveChanges();


            return View("~/Views/LeadUser/ContactUs.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/contactus")]
        public async Task<IActionResult> ContactUs(ContactUsModel model)
        {

            if (ModelState.IsValid)
            {
                var db = new MyVoltageDbContext(_options);
                Data.ActivityLog activityLog = new ActivityLog()
                {
                    ActionID = (int)Data.LogActionEnum.FormSubmit,
                    DateStarted = DateTime.Now,
                    Request = model.ToXML<ContactUsModel, ContactUsModel>(),
                    Response = "",
                    SourceID = (int)LogSourceEnum.Clientzone,
                    SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                    UserID = _userManager.GetUserId(User),
                };

                var user = _userManager.GetUserAsync(User).Result;

                var localCustomer = db.D01_LeadGeneratorUsers.Where(tbl => tbl.LocalUserID == user.Id && tbl.IsDeleted == false).SingleOrDefault();

                String email = "User<br/> Email: " + user.Email + "<br/>" +
                              " Contact Email: " + model.Email + "<br/>" +
                              " Contact Name: " + model.Name + "<br/>" +
                              " Contact Phone Number: " + model.PhoneNumber + "<br/>" +
                              //" Customer Number: " + localCustomer + "<br/>" +
                              " Account Type: " + "Lead User" + "<br/>" +
                              " Full Name: " + localCustomer.FullName + "<br/>" +
                              " ID Number Or Company Registration: " + localCustomer.IDNumberOrCompanyReg + "<br/>" +
                              " Registration phone number: " + localCustomer.PhoneNumber + "<br/>" +
                              //" Service Provider: " + _clientzoneProvider.CompanyName + "<br/>" +
                              " Street Address: " + localCustomer.StreetAddress + "<br/>" +
                              " Suburb: " + localCustomer.Suburb + "<br/>" +
                              " Town Or City: " + localCustomer.TownOrCity + "<br/>" +
                              " Province: " + localCustomer.Province + "<br/>" +
                              " Postal Code: " + localCustomer.PostalCode + "<br/>" +
                              //" Occupancy Date: " + localCustomer.OccupancyDate.ToLongDateString() + "<br/>" +
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

                await _emailSender.SendContactEmailAsync(_configuration["RegEmail:Email"], email, "", model.Email, fileContents, filename, contentType);

                activityLog.DateEnded = DateTime.Now;
                activityLog.Response = "Email sent";

                db.Add(activityLog);
                db.SaveChanges();

                return Redirect("/leaduser/contactus?sent=true");
            }

            return View("~/Views/leaduser/ContactUs.cshtml", model);

            #endregion
        }

    }


}
