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
using MyVoltage.Models.OperationalModels.Y01_UserAdmin;
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
    public class Y01_UserAdminController : Controller
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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public Y01_UserAdminController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
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
            _signInManager = signInManager;
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserSearch")]
        public async Task<IActionResult> Y01_UserAdmin_UserSearch()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserSearch, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserSearch}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlCommand sqlCommand = new SqlCommand("sp_SiteAdmin_UserAdmin_Search", conn);
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            //sqlCommand.Parameters.AddWithValue("@SearchString", "");

            System.Data.DataTable dataTable = new System.Data.DataTable();
            conn.Open();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);
            conn.Close();

            Y01_UserAdmin_UserSearchModel model = new Y01_UserAdmin_UserSearchModel()
            {
                SearchResultsTable = dataTable,
            };

            return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserSearch.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit")]
        public async Task<IActionResult> SiteAdmin_Y01_UserAdmin_Edit()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var user = _userManager.FindByIdAsync(_operationalProvider.Y01_USERADMIN_USERID).Result;

            if (user == null)
                return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserSearch");

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

            Y01_UserAdmin_UserEditModel model = new Y01_UserAdmin_UserEditModel()
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList(),
                HasAccessToAllCompanies = operationalProfile.HasAccessToAllCompanies,
                Companies = db.Companies.OrderBy(p => p.Name).ToList(),
                UserCompanies = db.UserCompanies.Where(p => p.UserID == user.Id).ToList(),
                UserMeterSerials = new List<UserMeterItem>(),
                UserID = _operationalProvider.Y01_USERADMIN_USERID,
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
                OrganizationalStatusID = (from p in (OrganizationalStatusEnum[])Enum.GetValues(typeof(OrganizationalStatusEnum))
                                          select new SelectListItem()
                                          {
                                              Value = ((int)p).ToString(),
                                              Text = p.GetDescription(),
                                              Selected = operationalProfile.OrganizationalStatusID.HasValue && operationalProfile.OrganizationalStatusID.Value == (int)p
                                          }).ToList(),
            };

            model.SiteAdmin_Partners.Add(new SelectListItem()
            {
                Value = "",
                Text = "[NOT LINKED]",
                Selected = !operationalProfile.PartnerID.HasValue
            });

            if (!operationalProfile.OrganizationalStatusID.HasValue)
                model.OrganizationalStatusID.Add(new SelectListItem()
                {
                    Value = "",
                    Text = "[NOT LINKED]",
                    Selected = !operationalProfile.OrganizationalStatusID.HasValue
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

            return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit")]
        public async Task<IActionResult> SiteAdmin_Y01_UserAdmin_Edit(Y01_UserAdmin_UserEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var user = _userManager.FindByIdAsync(_operationalProvider.Y01_USERADMIN_USERID).Result;

            if (user == null)
                return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserSearch");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();

            if (ModelState.IsValid)
            {
                var userInDB = db.Users.Where(p => p.Id == _operationalProvider.Y01_USERADMIN_USERID).SingleOrDefault();
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

                if (!string.IsNullOrEmpty(Request.Form["OrganizationalStatusID"].ToString()))
                    operationalProfile.OrganizationalStatusID = Convert.ToInt32(Request.Form["OrganizationalStatusID"]);

                db.Update(operationalProfile);
                db.SaveChanges();

                _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES + user.Id);
                _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE + user.Id);

                return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
            }
            else
            {
                model = new Y01_UserAdmin_UserEditModel()
                {
                    Email = user.Email,
                    PhoneNumber = model.PhoneNumber,
                    UserSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList(),
                    IsSuccess = false,
                    Companies = db.Companies.OrderBy(p => p.Name).ToList(),
                    UserCompanies = db.UserCompanies.Where(p => p.UserID == user.Id).ToList(),
                    UserID = _operationalProvider.Y01_USERADMIN_USERID,
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
                    OrganizationalStatusID = (from p in (OrganizationalStatusEnum[])Enum.GetValues(typeof(OrganizationalStatusEnum))
                                              select new SelectListItem()
                                              {
                                                  Value = ((int)p).ToString(),
                                                  Text = p.GetDescription(),
                                                  Selected = operationalProfile.OrganizationalStatusID.HasValue && operationalProfile.OrganizationalStatusID.Value == (int)p
                                              }).ToList(),
                };

                model.SiteAdmin_Partners.Add(new SelectListItem()
                {
                    Value = "",
                    Text = "[NOT LINKED]",
                    Selected = !operationalProfile.PartnerID.HasValue
                });

                if (!operationalProfile.OrganizationalStatusID.HasValue)
                    model.OrganizationalStatusID.Add(new SelectListItem()
                    {
                        Value = "",
                        Text = "[NOT LINKED]",
                        Selected = !operationalProfile.OrganizationalStatusID.HasValue
                    });


                model.SiteAdmin_Partners = model.SiteAdmin_Partners.OrderBy(p => p.Text).ToList();

            }

            return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/AddSecureArea/{secureAreaID}/{secureAreaActionID}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_AddSecureArea(int secureAreaID, int secureAreaActionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.ManagementApproval))
                return Content("false");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.UserSecureAreaActions
                                where p.SecureAreaID == secureAreaID
                                && p.SecureAreaActionID == secureAreaActionID
                                && p.UserID == _operationalProvider.Y01_USERADMIN_USERID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                    {
                        SecureAreaActionID = secureAreaActionID,
                        SecureAreaID = secureAreaID,
                        UserID = _operationalProvider.Y01_USERADMIN_USERID,
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
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/RemoveSecureArea/{secureAreaID}/{secureAreaActionID}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_RemoveSecureArea(int secureAreaID, int secureAreaActionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.ManagementApproval))
                return Content("false");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.UserSecureAreaActions
                                where p.SecureAreaID == secureAreaID
                                && p.SecureAreaActionID == secureAreaActionID
                                && p.UserID == _operationalProvider.Y01_USERADMIN_USERID
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
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/Delete")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_Delete()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Delete}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var dbUser = db.Users.Where(p => p.Id == _operationalProvider.Y01_USERADMIN_USERID).SingleOrDefault();
            var customer = db.Customers.Where(tbl => tbl.UserID == _operationalProvider.Y01_USERADMIN_USERID && tbl.IsDeleted == false).FirstOrDefault();

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


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/AddCompany/{companyID}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_AddCompany(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.UserCompanies.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID && p.CompanyID == companyID).SingleOrDefault();

            if (userCompany == null)
            {
                userCompany = new UserCompany()
                {
                    UserID = _operationalProvider.Y01_USERADMIN_USERID,
                    CompanyID = companyID
                };
                db.Add(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/DeleteCompany/{companyID}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_DeleteCompany(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.UserCompanies.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID && p.CompanyID == companyID).SingleOrDefault();

            if (userCompany != null)
            {
                db.Remove(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/AddMeter/{serial}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_AddMeter(string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.UserMeterSerials.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter == null)
            {
                userMeter = new UserMeterSerial()
                {
                    UserID = _operationalProvider.Y01_USERADMIN_USERID,
                    MeterSerial = serial
                };
                db.Add(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/DeleteMeter/{serial}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_DeleteMeter(string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.UserMeterSerials.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter != null)
            {
                db.Remove(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/AddAllRights")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_AddAllRights()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            #region Assign new UserSecureAreaActions

            var secureAreaActionsInDB = db.SecureAreaActions.ToList();
            var masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID).ToList();
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
                        masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID).ToList();
                    }

                    var existing = masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa).SingleOrDefault();

                    if (existing == null)
                    {
                        UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                        {
                            SecureAreaActionID = (int)saa,
                            SecureAreaID = secureArea.SecureAreaID,
                            UserID = _operationalProvider.Y01_USERADMIN_USERID,
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

            _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + _operationalProvider.Y01_USERADMIN_USERID);

            #endregion


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit/RemoveAllRights")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_RemoveAllRights()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserEdit, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserEdit}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == _operationalProvider.Y01_USERADMIN_USERID).ToList();
            db.UserSecureAreaActions.RemoveRange(masterSecureAreas);
            db.SaveChanges();

            #region Clear cache

            _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + _operationalProvider.Y01_USERADMIN_USERID);

            #endregion


            return Redirect($"/operational/Y01_UserAdmin/Y01_UserAdmin_UserEdit");
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd")]
        public async Task<IActionResult> Y01_UserAdmin_UserAdd()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserAdd, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserAdd}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Y01_UserAdmin_UserAddModel model = new Y01_UserAdmin_UserAddModel()
            {
                DefaultCompany = (from p in db.Companies
                                  orderby p.Name
                                  select new SelectListItem()
                                  {
                                      Value = p.CompanyID.ToString(),
                                      Text = p.Name
                                  }).ToList(),
            };

            return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd")]
        public async Task<IActionResult> Y01_UserAdmin_UserAdd(Y01_UserAdmin_UserAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserAdd, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserAdd}/{(int)SecureAreaActionEnum.Add}");

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
                    return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd.cshtml", model);
                }

                int defaultCompanyID = 0;
                try { defaultCompanyID = Convert.ToInt32(Request.Form["DefaultCompany"]); }
                catch
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{Request.Form["DefaultCompany"].ToString()} - Invalid Default Company";
                    return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd.cshtml", model);
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

            return View("~/Views/Operational/Y01_UserAdmin/Y01_UserAdmin_UserAdd.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserSwitch")]
        public async Task<IActionResult> Y01_UserAdmin_UserSwitch()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Y01_UserAdmin_UserSwitch, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Y01_UserAdmin_UserSwitch}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Y01_UserAdmin_UserSwitchModel model = new Y01_UserAdmin_UserSwitchModel()
            {
                Company = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                Customer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "<< Select Company First >>", Disabled = true, },
                },
                CustomerItems = new List<Y01_UserAdmin_UserSwitchModel.CustomerItem>(),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var customers = (from p in db.Customers
                             join u in db.Users on p.UserID equals u.Id into su
                             from u in su.DefaultIfEmpty()
                             where !p.IsDeleted
                             && !u.IsDeleted
                             select new
                             {
                                 p.UserID,
                                 p.FullName,
                                 p.CompanyID,
                                 u.Email,
                                 p.CustomerNumber,
                             }).ToList();

            model.CustomerItems = (from p in customers
                                   orderby p.CustomerNumber
                                   select new Y01_UserAdmin_UserSwitchModel.CustomerItem
                                   {
                                       CompanyID = p.CompanyID,
                                       DisplayName = $"{p.CustomerNumber} - {p.FullName} ({p.Email})",
                                       ID = p.UserID,
                                   }).ToList();

            if (_operationalProvider.IsDeveloper)
            {
                model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = "Operational User",
                    Value = "0",
                });
                model.CustomerItems.AddRange(
                    (from u in db.Users
                     join p in db.OperationalProfiles on u.Id equals p.UserID into su
                     from p in su.DefaultIfEmpty()
                     where !string.IsNullOrEmpty(p.FirstName)
                     && !u.IsDeleted
                     orderby p.FirstName
                     select new Y01_UserAdmin_UserSwitchModel.CustomerItem
                     {
                         CompanyID = 0,
                         DisplayName = $"{p.FirstName} {p.LastName} ({u.Email})",
                         ID = p.UserID,
                     }).ToList());

                model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = "Lead User",
                    Value = "-1",
                });
                model.CustomerItems.AddRange(
                    (from l in db.D01_LeadGeneratorUsers
                     join lg in db.D01_LeadGenerators on l.LeadGeneratorID equals lg.ID into lgg
                     from lg in lgg.DefaultIfEmpty()
                     where !string.IsNullOrEmpty(l.LocalUserID)
                     && !string.IsNullOrEmpty(l.FullName)
                     orderby l.FullName
                     select new Y01_UserAdmin_UserSwitchModel.CustomerItem
                     {
                         CompanyID = -1,
                         DisplayName = $"{l.FullName} ({lg.LeadGeneratorName})",
                         ID = l.LocalUserID,
                     }).ToList());
            }

            var companies = (from p in db.Companies
                             orderby p.Name
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            companies = (from p in companies
                         orderby p.Name
                         where customers.Select(c => p.CompanyID).Contains(p.CompanyID)
                         select new
                         {
                             p.CompanyID,
                             p.Name,
                         }).ToList();

            foreach (var c in companies)
            {
                if (model.CustomerItems.Where(p => p.CompanyID == c.CompanyID).Count() != 0)
                    model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = c.Name,
                        Value = c.CompanyID.ToString(),
                    });
            }

            return View("~/Views/operational/Y01_UserAdmin/Y01_UserAdmin_UserSwitch.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Y01_UserAdmin/Y01_UserAdmin_UserSwitchSignIn")]
        public async Task<IActionResult> Y01_UserAdmin_UserSwitchSignIn()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["Customer"].ToString()))
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Remove(CustomerProvider.SESSION_CUSTOMER_ID);
                HttpContext.Session.Remove(CustomerProvider.SESSION_COMPANY_NAME);
                HttpContext.Session.Remove(CustomerProvider.METER_NUMBER);

                HttpContext.Session.Remove(OperationalProvider.SESSION_COMPANY_ID);
                HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL);
                HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_NUMBER);
                HttpContext.Session.Remove(OperationalProvider.SESSION_NAVIGATION_HISTORY);

                var user = _userManager.FindByIdAsync(Request.Form["Customer"].ToString()).Result;

                if (user != null)
                {
                    await _signInManager.SignInAsync(user, new Microsoft.AspNetCore.Authentication.AuthenticationProperties());
                }

                return Content("true");
            }

            return Content("false");
        }

    }
}
