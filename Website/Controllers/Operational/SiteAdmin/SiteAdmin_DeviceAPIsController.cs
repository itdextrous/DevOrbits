using System;
using System.Collections.Generic;
using System.Linq;
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
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_DeviceAPIsController : Controller
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

        public SiteAdmin_DeviceAPIsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIss()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceAPIs, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceAPIs}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_DeviceAPIsModel model = new SiteAdmin_DeviceAPIsModel()
            {
                SiteAdmin_DeviceAPIsItems = new List<SiteAdmin_DeviceAPIsModel.SiteAdmin_DeviceAPIsItem>(),
            };

            var siteAdmin_DeviceAPIs = (from p in db.SiteAdmin_DeviceAPIs
                                        select p).ToList();

            foreach (var p in siteAdmin_DeviceAPIs)
            {
                SiteAdmin_DeviceAPIsModel.SiteAdmin_DeviceAPIsItem item = new SiteAdmin_DeviceAPIsModel.SiteAdmin_DeviceAPIsItem()
                {
                    Description = p.Description,
                    ID = p.ID,
                    LatestSyncDate = p.LatestSyncDate,
                    LatestSyncDeviceCount = p.LatestSyncDeviceCount,
                    LatestSyncGWCount = p.LatestSyncGWCount,
                    URL = p.URL,
                    Password = p.Password,
                    Username = p.Username,
                    UseSkybill = p.UseSkybill,
                    IsSyncActive = p.IsSyncActive,
                };

                model.SiteAdmin_DeviceAPIsItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/SiteAdmin_DeviceAPIs.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Add")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceAPIs, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceAPIs}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            SiteAdmin_DeviceAPIs_AddModel model = new SiteAdmin_DeviceAPIs_AddModel()
            {
                UseSkybill = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString() },
                    new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString() },
                },
                IsSyncActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString() },
                    new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString() },
                },
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Add")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Add(SiteAdmin_DeviceAPIs_AddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceAPIs, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceAPIs}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            model.UseSkybill = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["UseSkybill"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = !Convert.ToBoolean(Request.Form["UseSkybill"]) },
            };

            model.IsSyncActive = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["IsSyncActive"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = !Convert.ToBoolean(Request.Form["IsSyncActive"]) },
            };

            if (!string.IsNullOrEmpty(model.Description))
            {
                if (model.Description.Contains("/"))
                {
                    ModelState.AddModelError("Name", $"Invalid character: /");
                    return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Add.cshtml", model);
                }

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.Description.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Add.cshtml", model);
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.Description.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Add.cshtml", model);
                    }
                }

                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                Data.SiteAdmin_DeviceAPI siteAdmin_DeviceAPI = new SiteAdmin_DeviceAPI()
                {
                    Description = model.Description,
                    URL = model.URL,
                    Password = model.Password,
                    Username = model.Username,
                    UseSkybill = Convert.ToBoolean(Request.Form["UseSkybill"]),
                    IsSyncActive = Convert.ToBoolean(Request.Form["IsSyncActive"]),
                };

                db.Add(siteAdmin_DeviceAPI);
                db.SaveChanges();

                model.IsSuccess = true;
                model.ResultDeviceAPIID = siteAdmin_DeviceAPI.ID;

            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Edit/{deviceAPIID}")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Edit(int deviceAPIID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceAPIs, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceAPIs}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var siteAdmin_DeviceAPI = db.SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceAPIID).SingleOrDefault();

            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_DeviceAPIs_EditModel model = new SiteAdmin_DeviceAPIs_EditModel()
            {
                Description = siteAdmin_DeviceAPI.Description,
                DeviceAPIID = deviceAPIID,
                URL = siteAdmin_DeviceAPI.URL,
                Username = siteAdmin_DeviceAPI.Username,
                Password = siteAdmin_DeviceAPI.Password,
                SiteAdmin_DeviceAPIs_CustomURLs = db.SiteAdmin_DeviceAPIs_CustomURLs.Where(p => p.SiteAdmin_DeviceAPIID == siteAdmin_DeviceAPI.ID).ToList(),
                UseSkybill = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = !siteAdmin_DeviceAPI.UseSkybill.HasValue || siteAdmin_DeviceAPI.UseSkybill.Value },
                    new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = siteAdmin_DeviceAPI.UseSkybill.HasValue && !siteAdmin_DeviceAPI.UseSkybill.Value },
                },
                IsSyncActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = !siteAdmin_DeviceAPI.IsSyncActive.HasValue || siteAdmin_DeviceAPI.IsSyncActive.Value },
                    new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = siteAdmin_DeviceAPI.IsSyncActive.HasValue && !siteAdmin_DeviceAPI.IsSyncActive.Value },
                },
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Edit/{deviceAPIID}")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Edit(int deviceAPIID, SiteAdmin_DeviceAPIs_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceAPIs, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceAPIs}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            model.DeviceAPIID = deviceAPIID;

            model.UseSkybill = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["UseSkybill"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = !Convert.ToBoolean(Request.Form["UseSkybill"]) },
            };

            model.IsSyncActive = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["IsSyncActive"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = !Convert.ToBoolean(Request.Form["IsSyncActive"]) },
            };

            if (ModelState.IsValid)
            {
                var siteAdmin_DeviceAPI = db.SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceAPIID).SingleOrDefault();
                if (siteAdmin_DeviceAPI != null)
                {
                    StringBuilder sbSysLog = new StringBuilder();

                    if (!string.IsNullOrEmpty(model.Description) && siteAdmin_DeviceAPI.Description != model.Description)
                    {
                        sbSysLog.AppendLine($"Description from '{siteAdmin_DeviceAPI.Description}' to '{model.Description}'<br />");
                        siteAdmin_DeviceAPI.Description = model.Description;
                    }

                    if (!string.IsNullOrEmpty(model.URL) && siteAdmin_DeviceAPI.URL != model.URL)
                    {
                        sbSysLog.AppendLine($"URL from '{siteAdmin_DeviceAPI.URL}' to '{model.URL}'<br />");
                        siteAdmin_DeviceAPI.URL = model.URL;
                    }

                    if (!string.IsNullOrEmpty(model.Username) && siteAdmin_DeviceAPI.Username != model.Username)
                    {
                        sbSysLog.AppendLine($"Username from '{siteAdmin_DeviceAPI.Username}' to '{model.Username}'<br />");
                        siteAdmin_DeviceAPI.Username = model.Username;
                    }

                    if (!string.IsNullOrEmpty(model.Password) && siteAdmin_DeviceAPI.Password != model.Password)
                    {
                        sbSysLog.AppendLine($"Password from '{siteAdmin_DeviceAPI.Password}' to '{model.Password}'<br />");
                        siteAdmin_DeviceAPI.Password = model.Password;
                    }

                    //if (siteAdmin_DeviceAPI.UseSkybill != Convert.ToBoolean(Request.Query["UseSkybill"]))
                    //{
                    //    sbSysLog.AppendLine($"UseSkybill from '{siteAdmin_DeviceAPI.UseSkybill}' to '{Convert.ToBoolean(Request.Query["UseSkybill"])}'<br />");
                    siteAdmin_DeviceAPI.UseSkybill = Convert.ToBoolean(Request.Form["UseSkybill"]);
                    //}

                    //if (siteAdmin_DeviceAPI.IsSyncActive != Convert.ToBoolean(Request.Query["IsSyncActive"]))
                    //{
                    //    sbSysLog.AppendLine($"IsSyncActive from '{siteAdmin_DeviceAPI.IsSyncActive}' to '{Convert.ToBoolean(Request.Query["IsSyncActive"])}'<br />");
                    siteAdmin_DeviceAPI.IsSyncActive = Convert.ToBoolean(Request.Form["IsSyncActive"]);
                    //}

                    db.Update(siteAdmin_DeviceAPI);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceAPIs/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Edit_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Edit_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_CustomURLID"])
                && !string.IsNullOrEmpty(Request.Form["add_CustomURL"])
                && !string.IsNullOrEmpty(Request.Form["deviceAPIID"])
                )
            {
                try
                {
                    var cycleToEdit = (from p in db.SiteAdmin_DeviceAPIs_CustomURLs
                                       where p.SiteAdmin_DeviceAPIID == Convert.ToInt32(Request.Form["deviceAPIID"])
                                       && p.CustomURLID == Convert.ToInt32(Request.Form["add_CustomURLID"])
                                       select p).SingleOrDefault();

                    if (cycleToEdit != null)
                    {
                        cycleToEdit.CustomURLID = Convert.ToInt32(Request.Form["add_CustomURLID"]);
                        cycleToEdit.CustomURL = Request.Form["add_CustomURL"].ToString();
                        db.Update(cycleToEdit);
                    }
                    else
                    {
                        cycleToEdit = new SiteAdmin_DeviceAPIs_CustomURL()
                        {
                            CustomURLID = Convert.ToInt32(Request.Form["add_CustomURLID"]),
                            SiteAdmin_DeviceAPIID = Convert.ToInt32(Request.Form["deviceAPIID"]),
                            CustomURL = Request.Form["add_CustomURL"].ToString(),
                        };
                        db.Add(cycleToEdit);
                    }

                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceAPIs_Edit_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_DeviceAPIs_Edit_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var company_CostSetting_Item = (from p in db.SiteAdmin_DeviceAPIs_CustomURLs
                                                where p.ID == ID
                                                select p).SingleOrDefault();

                if (company_CostSetting_Item != null)
                {
                    db.Remove(company_CostSetting_Item);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }
    }
}
