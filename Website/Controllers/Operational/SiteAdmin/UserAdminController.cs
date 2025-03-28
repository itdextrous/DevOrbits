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
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational
{
    [Authorize(Roles = "Operational")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserAdminController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UserAdminController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin")]
        public async Task<IActionResult> SiteAdmin_UserAdmin()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/SiteAdmin/UserAdmin/UserAdmin.cshtml");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/UserAdmin")]
        public async Task<IActionResult> SiteAdmin_UserAdmin(UserAdminModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(model.SearchString))
            {
                return Redirect($"/operational/SiteAdmin/UserAdmin/SearchResults/{model.SearchString}");
            }

            return View("~/Views/Operational/SiteAdmin/UserAdmin/UserAdmin.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/SearchResults/{searchString}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_SearchResults(string searchString)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlCommand sqlCommand = new SqlCommand("sp_SiteAdmin_UserAdmin_Search", conn);
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCommand.Parameters.AddWithValue("@SearchString", searchString);


            System.Data.DataTable dataTable = new System.Data.DataTable();

            conn.Open();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);
            conn.Close();

            UserAdminSearchResultsModel model = new UserAdminSearchResultsModel()
            {
                SearchResultsTable = dataTable,
                SearchString = searchString
            };


            return View("~/Views/Operational/SiteAdmin/UserAdmin/SearchResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/Edit/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_Edit(string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var user = _userManager.FindByIdAsync(userID).Result;

            if (user == null)
                return Redirect($"/operational/SiteAdmin/UserAdmin");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();

            if (operationalProfile == null)
            {
                OperationalProfile profile = new OperationalProfile()
                {
                    UserID = user.Id,
                    HasAccessToAllCompanies = false
                };
                db.Add(profile);
                db.SaveChanges();

                operationalProfile = profile;
            }

            UserAdminEditModel model = new UserAdminEditModel()
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList(),
                HasAccessToAllCompanies = operationalProfile.HasAccessToAllCompanies,
                Companies = db.Companies.OrderBy(p => p.Name).ToList(),
                UserCompanies = db.UserCompanies.Where(p => p.UserID == user.Id).ToList(),
                UserMeterSerials = new List<UserMeterItem>(),
                UserID = userID,
                MeterSerials = new List<KeyValuePair<string, string>>(),
                HasAccessToAllMetersInLinkedCompanies = operationalProfile.HasAccessToAllMetersInLinkedCompanies,
                ExternalChargeOutRatePerHour = operationalProfile.ExternalChargeOutRatePerHour.HasValue ? operationalProfile.ExternalChargeOutRatePerHour.Value : 0,
                FirstName = operationalProfile.FirstName,
                InternalChargeOutRatePerHour = operationalProfile.InternalChargeOutRatePerHour.HasValue ? operationalProfile.InternalChargeOutRatePerHour.Value : 0,
                JobTitle = operationalProfile.JobTitle,
                LastName = operationalProfile.LastName,
                SiteAdmin_Partners = (from p in db.SiteAdmin_Partners
                                      select new SelectListItem()
                                      {
                                          Value = p.ID.ToString(),
                                          Text = p.PartnerName,
                                          Selected = operationalProfile.PartnerID.HasValue && operationalProfile.PartnerID.Value == p.ID
                                      }).ToList(),
                ThreeCxRatePerMin = operationalProfile.ThreeCxRatePerMin,
                ThreeCxVOIPExt = operationalProfile.ThreeCxVOIPExt,
            };

            model.SiteAdmin_Partners.Add(new SelectListItem()
            {
                Value = "",
                Text = "[NOT LINKED]",
                Selected = !operationalProfile.PartnerID.HasValue
            });

            model.SiteAdmin_Partners = model.SiteAdmin_Partners.OrderBy(p => p.Text).ToList();

            List<int> companyIDsLinked = new List<int>();
            companyIDsLinked.AddRange(model.UserCompanies.Select(d => d.CompanyID));

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);

            var skybillCustomersForUser = (from p in dbCache.SkybillCustomers
                                           where companyIDsLinked.Contains(p.CompanyID)
                                           orderby p.Customer_No
                                           select p).ToList();
            if (model.HasAccessToAllCompanies)
            {
                skybillCustomersForUser = (from p in dbCache.SkybillCustomers
                                           orderby p.Customer_No
                                           select p).ToList();
            }

            foreach (var item in db.UserMeterSerials.Where(p => p.UserID == user.Id).ToList())
            {
                var skybillCustomer = skybillCustomersForUser.Where(p => p.Serial_No == item.MeterSerial).FirstOrDefault();
                if (skybillCustomer == null)
                    continue;
                model.UserMeterSerials.Add(new UserMeterItem()
                {
                    DisplayName = $"{skybillCustomer.Customer_No} - {skybillCustomer.Serial_No} {skybillCustomer.deviceType}",
                    MeterSerial = item.MeterSerial,
                    ID = item.ID,
                    UserID = item.UserID,
                });
            }

            if (!model.HasAccessToAllCompanies)
            {
                var metersToAdd = (from p in skybillCustomersForUser
                                   select p).ToList();

                foreach (var item in metersToAdd)
                {
                    if (model.MeterSerials.Where(p => p.Key == item.Serial_No).Count() == 0)
                        model.MeterSerials.Add(
                            new KeyValuePair<string, string>(item.Serial_No, $"{item.Customer_No} - {item.Serial_No} {item.deviceType}")
                            );
                }
            }

            return View("~/Views/Operational/SiteAdmin/UserAdmin/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/UserAdmin/Edit/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_Edit(string userID, UserAdminEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var user = _userManager.FindByIdAsync(userID).Result;

            if (user == null)
                return Redirect($"/operational/SiteAdmin/UserAdmin");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();

            if (ModelState.IsValid)
            {
                var userInDB = db.Users.Where(p => p.Id == userID).SingleOrDefault();
                userInDB.PhoneNumber = model.PhoneNumber;
                db.Update(userInDB);
                db.SaveChanges();

                operationalProfile.HasAccessToAllCompanies = model.HasAccessToAllCompanies;
                operationalProfile.HasAccessToAllMetersInLinkedCompanies = model.HasAccessToAllMetersInLinkedCompanies;
                operationalProfile.FirstName = model.FirstName;
                operationalProfile.LastName = model.LastName;
                operationalProfile.JobTitle = model.JobTitle;
                operationalProfile.InternalChargeOutRatePerHour = model.InternalChargeOutRatePerHour;
                operationalProfile.ExternalChargeOutRatePerHour = model.ExternalChargeOutRatePerHour;
                operationalProfile.ThreeCxVOIPExt = model.ThreeCxVOIPExt;
                operationalProfile.ThreeCxRatePerMin = model.ThreeCxRatePerMin;

                if (!string.IsNullOrEmpty(Request.Form["SiteAdmin_Partners"].ToString()))
                    operationalProfile.PartnerID = Convert.ToInt32(Request.Form["SiteAdmin_Partners"]);
                else
                    operationalProfile.PartnerID = null;

                db.Update(operationalProfile);
                db.SaveChanges();

                _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES + user.Id);
                _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE + user.Id);

                return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
            }
            else
            {
                model = new UserAdminEditModel()
                {
                    Email = user.Email,
                    PhoneNumber = model.PhoneNumber,
                    UserSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList(),
                    IsSuccess = false,
                    Companies = db.Companies.OrderBy(p => p.Name).ToList(),
                    UserCompanies = db.UserCompanies.Where(p => p.UserID == user.Id).ToList(),
                    UserID = userID,
                    HasAccessToAllCompanies = operationalProfile.HasAccessToAllCompanies,
                    HasAccessToAllMetersInLinkedCompanies = operationalProfile.HasAccessToAllMetersInLinkedCompanies,
                    ExternalChargeOutRatePerHour = operationalProfile.ExternalChargeOutRatePerHour.HasValue ? operationalProfile.ExternalChargeOutRatePerHour.Value : 0,
                    FirstName = operationalProfile.FirstName,
                    InternalChargeOutRatePerHour = operationalProfile.InternalChargeOutRatePerHour.HasValue ? operationalProfile.InternalChargeOutRatePerHour.Value : 0,
                    JobTitle = operationalProfile.JobTitle,
                    LastName = operationalProfile.LastName,
                    SiteAdmin_Partners = (from p in db.SiteAdmin_Partners
                                          select new SelectListItem()
                                          {
                                              Value = p.ID.ToString(),
                                              Text = p.PartnerName,
                                              Selected = operationalProfile.PartnerID.HasValue && operationalProfile.PartnerID.Value == p.ID
                                          }).ToList(),
                    ThreeCxRatePerMin = operationalProfile.ThreeCxRatePerMin,
                    ThreeCxVOIPExt = operationalProfile.ThreeCxVOIPExt,
                };
                model.SiteAdmin_Partners.Add(new SelectListItem()
                {
                    Value = "",
                    Text = "[NOT LINKED]",
                    Selected = !operationalProfile.PartnerID.HasValue
                });

                model.SiteAdmin_Partners = model.SiteAdmin_Partners.OrderBy(p => p.Text).ToList();

            }

            return View("~/Views/Operational/SiteAdmin/UserAdmin/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/UserAdmin/AddSecureArea/{secureAreaID}/{secureAreaActionID}/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddSecureArea(int secureAreaID, int secureAreaActionID, string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.ManagementApproval))
                return Content("false");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.UserSecureAreaActions
                                where p.SecureAreaID == secureAreaID
                                && p.SecureAreaActionID == secureAreaActionID
                                && p.UserID == userID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                    {
                        SecureAreaActionID = secureAreaActionID,
                        SecureAreaID = secureAreaID,
                        UserID = userID,
                    };
                    db.Add(userSecureAreaAction);
                    db.SaveChanges();

                    return Content("true");
                }
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/UserAdmin/RemoveSecureArea/{secureAreaID}/{secureAreaActionID}/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_RemoveSecureArea(int secureAreaID, int secureAreaActionID, string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.ManagementApproval))
                return Content("false");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.UserSecureAreaActions
                                where p.SecureAreaID == secureAreaID
                                && p.SecureAreaActionID == secureAreaActionID
                                && p.UserID == userID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
                    db.SaveChanges();

                    return Content("true");
                }
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/Add")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            UserAdminAddModel model = new UserAdminAddModel()
            {
                DefaultCompany = (from p in db.Companies
                                  orderby p.Name
                                  select new SelectListItem()
                                  {
                                      Value = p.CompanyID.ToString(),
                                      Text = p.Name
                                  }).ToList(),
            };

            return View("~/Views/Operational/SiteAdmin/UserAdmin/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/UserAdmin/Add")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_Add(UserAdminAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            model.DefaultCompany = (from p in db.Companies
                                    orderby p.Name
                                    select new SelectListItem()
                                    {
                                        Value = p.CompanyID.ToString(),
                                        Text = p.Name,
                                        Selected = p.CompanyID.ToString() == Request.Form["DefaultCompany"].ToString() ? true : false
                                    }).ToList();

            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Where(p => p.Email == model.Email && !p.IsDeleted).SingleOrDefault();

                if (existingUser != null)
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"User with email {model.Email} already exists";
                    return View("~/Views/Operational/SiteAdmin/UserAdmin/Add.cshtml", model);
                }

                int defaultCompanyID = 0;
                try { defaultCompanyID = Convert.ToInt32(Request.Form["DefaultCompany"]); }
                catch
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{Request.Form["DefaultCompany"].ToString()} - Invalid Default Company";
                    return View("~/Views/Operational/SiteAdmin/UserAdmin/Add.cshtml", model);
                }

                ApplicationUser user = new ApplicationUser()
                {
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    UserName = model.Email
                };

                // Create user
                var createUserResult = _userManager.CreateAsync(user).Result;

                if (createUserResult.Succeeded)
                {
                    // Add user to operational role
                    await _userManager.AddToRoleAsync(user, UserRoleEnum.Operational.ToString());

                    // Get user from DB after created
                    var dbUser = db.Users.Where(p => p.Email == user.Email && !p.IsDeleted).SingleOrDefault();
                    // Mark user as confirmed (else 2fa required)
                    dbUser.IsConfirmed = true;
                    db.SaveChanges();

                    #region Generate Code / Send user password reset email

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);

                    await _emailSender.SendResetPasswordEmailAsync(user.Email, $"New Operational Account registered. Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                    #endregion

                    #region UserSecureAreaActions

                    var userSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList();

                    foreach (var ps in _operationalProvider.ParentSecureAreas)
                    {
                        foreach (var sa in _operationalProvider.SecureAreas.Where(p => p.ParentSecureAreaID == ps.ParentSecureAreaID).ToList())
                        {
                            foreach (var saa in _operationalProvider.SecureAreaActions)
                            {
                                string checkedValueOnForm = Request.Form[$"saa_{sa.SecureAreaID}_{saa.SecureAreaActionID}"].ToString();

                                if (!string.IsNullOrEmpty(checkedValueOnForm))
                                {
                                    // Checked
                                    var existing = userSecureAreaActions.Where(p => p.SecureAreaID == sa.SecureAreaID && p.SecureAreaActionID == saa.SecureAreaActionID).SingleOrDefault();

                                    if (existing == null)
                                    {
                                        // Create New
                                        existing = new UserSecureAreaAction()
                                        {
                                            SecureAreaActionID = saa.SecureAreaActionID,
                                            SecureAreaID = sa.SecureAreaID,
                                            UserID = dbUser.Id
                                        };
                                        db.Add(existing);
                                    }
                                }
                                else
                                {
                                    // Unchecked
                                    var existing = userSecureAreaActions.Where(p => p.SecureAreaID == sa.SecureAreaID && p.SecureAreaActionID == saa.SecureAreaActionID).SingleOrDefault();

                                    if (existing != null)
                                    {
                                        // Remove from DB
                                        db.Remove(existing);
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region OperationalProfile

                    var existingOperationalProfile = db.OperationalProfiles.Where(p => p.UserID == dbUser.Id).SingleOrDefault();

                    if (existingOperationalProfile == null)
                    {
                        OperationalProfile operatonalProfile = new OperationalProfile()
                        {
                            HasAccessToAllCompanies = model.HasAccessToAllCompanies,
                            HasAccessToAllMetersInLinkedCompanies = model.HasAccessToAllMetersInLinkedCompanies,
                            ExternalChargeOutRatePerHour = model.ExternalChargeOutRatePerHour,
                            FirstName = model.FirstName,
                            InternalChargeOutRatePerHour = model.InternalChargeOutRatePerHour,
                            JobTitle = model.JobTitle,
                            LastName = model.LastName,
                            UserID = dbUser.Id
                        };

                        db.Add(operatonalProfile);
                        db.SaveChanges();
                    }

                    #endregion

                    #region DefaultCompany

                    var existingDefaultCompany = db.UserCompanies.Where(p => p.UserID == dbUser.Id && p.CompanyID == defaultCompanyID).SingleOrDefault();

                    if (existingDefaultCompany == null)
                    {
                        UserCompany defaultCompany = new UserCompany()
                        {
                            CompanyID = defaultCompanyID,
                            UserID = dbUser.Id
                        };
                        db.UserCompanies.Add(defaultCompany);
                        db.SaveChanges();
                    }

                    #endregion

                    model.ResultUserID = dbUser.Id;
                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"There was a problem creating user.";
                    foreach (var error in createUserResult.Errors)
                        model.ErrorMessage = model.ErrorMessage + " " + error.Description;
                }
            }
            else
            {
                model.IsSuccess = false;
            }

            return View("~/Views/Operational/SiteAdmin/UserAdmin/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/Delete/{userID}/{searchString}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_Delete(string userID, string searchString)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Delete}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var dbUser = db.Users.Where(p => p.Id == userID).SingleOrDefault();
            var customer = db.Customers.Where(tbl => tbl.UserID == userID && tbl.IsDeleted == false).FirstOrDefault();

            if (dbUser != null)
            {
                dbUser.IsDeleted = true;
                dbUser.IsConfirmed = false;
                dbUser.Email = dbUser.Email + "_" + dbUser.Id;
                dbUser.UserName = dbUser.UserName + "_" + dbUser.Id;
                dbUser.NormalizedEmail = dbUser.NormalizedEmail + "_" + dbUser.Id;
                dbUser.NormalizedUserName = dbUser.NormalizedUserName + "_" + dbUser.Id;
                db.Update(dbUser);
                db.SaveChanges();
            }
            if (customer != null)
            {
                customer.IsDeleted = true;
                db.Update(customer);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/UserAdmin/SearchResults/{searchString}");
        }


        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/AddCompany/{userID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddCompany(string userID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.UserCompanies.Where(p => p.UserID == userID && p.CompanyID == companyID).SingleOrDefault();

            if (userCompany == null)
            {
                userCompany = new UserCompany()
                {
                    UserID = userID,
                    CompanyID = companyID
                };
                db.Add(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/DeleteCompany/{userID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_DeleteCompany(string userID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.UserCompanies.Where(p => p.UserID == userID && p.CompanyID == companyID).SingleOrDefault();

            if (userCompany != null)
            {
                db.Remove(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/AddMeter/{userID}/{serial}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddMeter(string userID, string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.UserMeterSerials.Where(p => p.UserID == userID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter == null)
            {
                userMeter = new UserMeterSerial()
                {
                    UserID = userID,
                    MeterSerial = serial
                };
                db.Add(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/DeleteMeter/{userID}/{serial}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_DeleteMeter(string userID, string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.UserMeterSerials.Where(p => p.UserID == userID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter != null)
            {
                db.Remove(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/AddAllRights/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddAllRights(string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            #region Assign new UserSecureAreaActions

            var secureAreaActionsInDB = db.SecureAreaActions.ToList();
            var masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == userID).ToList();
            var parentSecureAreasInDB = db.ParentSecureAreas.ToList();
            var secureAreasInDB = db.SecureAreas.ToList();

            foreach (var secureArea in secureAreasInDB)
            {
                foreach (SecureAreaActionEnum saa in Enum.GetValues(typeof(SecureAreaActionEnum)))
                {
                    if (masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa).Count() > 1)
                    {
                        db.UserSecureAreaActions.RemoveRange(masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa));
                        db.SaveChanges();
                        masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == userID).ToList();
                    }

                    var existing = masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa).SingleOrDefault();

                    if (existing == null)
                    {
                        UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                        {
                            SecureAreaActionID = (int)saa,
                            SecureAreaID = secureArea.SecureAreaID,
                            UserID = userID
                        };

                        db.UserSecureAreaActions.Add(userSecureAreaAction);
                        db.SaveChanges();

                    }
                }
            }

            #endregion

            //#region Operational Profile

            //var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == masterUser.Id).SingleOrDefault();

            //if (operationalProfile == null)
            //{
            //    operationalProfile = new OperationalProfile()
            //    {
            //        HasAccessToAllCompanies = true,
            //        UserID = masterUser.Id
            //    };
            //    db.Add(operationalProfile);
            //    db.SaveChanges();
            //}

            //#endregion

            #region Clear cache

            _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + userID);

            #endregion


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/UserAdmin/RemoveAllRights/{userID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_RemoveAllRights(string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == userID).ToList();
            db.UserSecureAreaActions.RemoveRange(masterSecureAreas);
            db.SaveChanges();

            #region Clear cache

            _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + userID);

            #endregion


            return Redirect($"/operational/SiteAdmin/UserAdmin/Edit/{userID}");
        }


    }
}
