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

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_StatusGroupsController : Controller
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

        public SiteAdmin_StatusGroupsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups")]
        public async Task<IActionResult> SiteAdmin_StatusGroups()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            SiteAdmin_StatusGroupsModel model = new SiteAdmin_StatusGroupsModel()
            {
                SiteAdmin_StatusGroupItems = new List<SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem>(),
            };
            var opProfs = (from p in db.OperationalProfiles
                           select new { p.UserID, p.FirstName, p.LastName }).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();

            foreach (var StatusGroup in SiteAdmin_StatusGroups)
            {
                SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem item = new SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem()
                {
                    CreatedByID = StatusGroup.CreatedByID,
                    StatusGroupName = StatusGroup.StatusGroupName,
                    CreatedByUsername = "",
                    CreatedDate = StatusGroup.CreatedDate,
                    UpdatedDate = StatusGroup.UpdatedDate,
                    ID = StatusGroup.ID,
                    UpdatedByID = StatusGroup.UpdatedByID,
                    UpdatedByUsername = "",
                    IsDeleted = StatusGroup.IsDeleted,
                    SiteAdmin_StatusGroupItemStatusItems = new List<SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem.SiteAdmin_StatusGroupItemStatusItem>(),
                };

                var cOp = opProfs.Where(p => p.UserID == StatusGroup.CreatedByID).SingleOrDefault();
                if (cOp != null)
                    item.CreatedByUsername = $"{cOp.FirstName} {cOp.LastName}";

                if (!string.IsNullOrEmpty(StatusGroup.UpdatedByID))
                {
                    var uOp = opProfs.Where(p => p.UserID == StatusGroup.UpdatedByID).SingleOrDefault();
                    if (uOp != null)
                        item.UpdatedByUsername = $"{uOp.FirstName} {uOp.LastName}";
                }

                var statuses = siteAdmin_Statuses.Where(p => p.StatusGroupID == StatusGroup.ID).ToList();
                foreach (var s in statuses)
                {
                    SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem.SiteAdmin_StatusGroupItemStatusItem itemStatusItem = new SiteAdmin_StatusGroupsModel.SiteAdmin_StatusGroupItem.SiteAdmin_StatusGroupItemStatusItem()
                    {
                        CreatedByID = s.CreatedByID,
                        CreatedDate = s.CreatedDate,
                        ID = s.ID,
                        IsDeleted = s.IsDeleted,
                        SiteAdmin_StatusAction = siteAdmin_StatusActions.Where(p => p.ID == s.StatusActionID).SingleOrDefault(),
                        SiteAdmin_StatusReporting = siteAdmin_StatusReportings.Where(p => p.ID == s.StatusReportingID).SingleOrDefault(),
                        StatusActionID = s.StatusActionID,
                        StatusGroupID = s.StatusGroupID,
                        StatusReportingID = s.StatusReportingID,
                        UpdatedByID = s.UpdatedByID,
                        UpdatedDate = s.UpdatedDate,
                    };

                    item.SiteAdmin_StatusGroupItemStatusItems.Add(itemStatusItem);
                }

                model.SiteAdmin_StatusGroupItems.Add(item);
            }

            model.SiteAdmin_StatusGroupItems = model.SiteAdmin_StatusGroupItems.OrderBy(p => p.StatusGroupName).ToList();

            return View("~/Views/operational/SiteAdmin/SiteAdmin_StatusGroups/SiteAdmin_StatusGroups.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Add")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Add()
        {
            SiteAdmin_StatusGroups_AddModel model = new SiteAdmin_StatusGroups_AddModel()
            {

            };

            return View("~/Views/operational/SiteAdmin/SiteAdmin_StatusGroups/SiteAdmin_StatusGroups_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Add")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Add(SiteAdmin_StatusGroups_AddModel model)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.SiteAdmin_StatusGroups.Where(p => p.StatusGroupName.ToUpper() == model.StatusGroupName.ToUpper()).SingleOrDefault();
                if (existing == null)
                {
                    Data.SiteAdmin_StatusGroup SiteAdmin_StatusGroup = new Data.SiteAdmin_StatusGroup()
                    {
                        CreatedByID = _userManager.GetUserId(User),
                        StatusGroupName = model.StatusGroupName,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                        UpdatedDate = null,
                        UpdatedByID = "",
                    };

                    db.SiteAdmin_StatusGroups.Add(SiteAdmin_StatusGroup);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.StatusGroupName} already exists";
                }


            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_StatusGroups/SiteAdmin_StatusGroups_Add.cshtml", model);
        }
        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Edit(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_StatusGroup = db.SiteAdmin_StatusGroups.Where(p => p.ID == ID).SingleOrDefault();

            SiteAdmin_StatusGroups_EditModel model = new SiteAdmin_StatusGroups_EditModel()
            {
                ErrorMessage = "",
                IsSuccess = false,
                StatusGroupName = SiteAdmin_StatusGroup.StatusGroupName,
                SiteAdmin_StatusGroups_EditItems = new List<SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem>(),
                StatusGroupID = ID,
            };

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => p.StatusGroupID == ID).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem item = new SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem()
                {
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    ID = status.ID,
                    SiteAdmin_StatusAction = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault(),
                    SiteAdmin_StatusReporting = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault(),
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    WIPType = status.WIPType,
                };

                model.SiteAdmin_StatusGroups_EditItems.Add(item);
            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_StatusGroups/SiteAdmin_StatusGroups_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Edit(int ID, SiteAdmin_StatusGroups_EditModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            model.SiteAdmin_StatusGroups_EditItems = new List<SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem>();
            model.StatusGroupID = ID;

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => p.StatusGroupID == ID).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem item = new SiteAdmin_StatusGroups_EditModel.SiteAdmin_StatusGroups_EditItem()
                {
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    ID = status.ID,
                    SiteAdmin_StatusAction = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault(),
                    SiteAdmin_StatusReporting = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault(),
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    WIPType = status.WIPType,
                };
                model.SiteAdmin_StatusGroups_EditItems.Add(item);
            }

            if (ModelState.IsValid)
            {
                var existing = db.SiteAdmin_StatusGroups.Where(p => p.StatusGroupName.ToUpper() == model.StatusGroupName.ToUpper() && p.ID != ID).SingleOrDefault();

                if (existing == null)
                {
                    var SiteAdmin_StatusGroup = db.SiteAdmin_StatusGroups.Where(p => p.ID == ID).SingleOrDefault();

                    SiteAdmin_StatusGroup.StatusGroupName = model.StatusGroupName;
                    SiteAdmin_StatusGroup.UpdatedByID = _userManager.GetUserId(User);
                    SiteAdmin_StatusGroup.UpdatedDate = DateTime.Now;

                    db.SiteAdmin_StatusGroups.Update(SiteAdmin_StatusGroup);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.StatusGroupName} already exists";
                }

            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_StatusGroups/SiteAdmin_StatusGroups_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Delete(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_StatusGroup = db.SiteAdmin_StatusGroups.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_StatusGroup != null)
            {
                SiteAdmin_StatusGroup.IsDeleted = true;
                SiteAdmin_StatusGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_StatusGroup.UpdatedDate = DateTime.Now;

                db.SiteAdmin_StatusGroups.Update(SiteAdmin_StatusGroup);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_StatusGroups");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_Restore/{ID}")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_Restore(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_StatusGroup = db.SiteAdmin_StatusGroups.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_StatusGroup != null)
            {
                SiteAdmin_StatusGroup.IsDeleted = false;
                SiteAdmin_StatusGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_StatusGroup.UpdatedDate = DateTime.Now;

                db.SiteAdmin_StatusGroups.Update(SiteAdmin_StatusGroup);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_StatusGroups");
        }


        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_ActionSearch")]
        public JsonResult SiteAdmin_StatusGroups_ActionSearch(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            var siteAdmin_StatusActions = (from p in db.SiteAdmin_StatusActions
                                           where p.StatusActionName.ToUpper().Contains(Prefix.ToUpper())
                                           orderby p.StatusActionName
                                           select p).ToList();

            List<object> results = new List<object>();

            foreach (var action in siteAdmin_StatusActions)
            {
                results.Add(new
                {
                    Text = action.StatusActionName,
                    Value = action.StatusActionName,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_ReportingSearch")]
        public JsonResult SiteAdmin_StatusGroups_ReportingSearch(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            var siteAdmin_StatusReportings = (from p in db.SiteAdmin_StatusReportings
                                              where p.StatusReportingName.ToUpper().Contains(Prefix.ToUpper())
                                              orderby p.StatusReportingName
                                              select p).ToList();

            List<object> results = new List<object>();

            foreach (var Reporting in siteAdmin_StatusReportings)
            {
                results.Add(new
                {
                    Text = Reporting.StatusReportingName,
                    Value = Reporting.StatusReportingName,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["addAction"])
                && !string.IsNullOrEmpty(Request.Form["addReporting"])
                && !string.IsNullOrEmpty(Request.Form["StatusGroupID"]))
            {
                try
                {
                    var action = db.SiteAdmin_StatusActions.Where(p => p.StatusActionName == Request.Form["addAction"].ToString()).SingleOrDefault();

                    if (action == null)
                    {
                        action = new SiteAdmin_StatusAction()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            StatusActionName = Request.Form["addAction"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Add(action);
                        db.SaveChanges();
                    }

                    var reporting = db.SiteAdmin_StatusReportings.Where(p => p.StatusReportingName == Request.Form["addReporting"].ToString()).SingleOrDefault();

                    if (reporting == null)
                    {
                        reporting = new SiteAdmin_StatusReporting()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            StatusReportingName = Request.Form["addReporting"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Add(reporting);
                        db.SaveChanges();
                    }


                    var status = (from p in db.SiteAdmin_Statuses
                                  where p.StatusGroupID == Convert.ToInt32(Request.Form["StatusGroupID"])
                                  && p.StatusReportingID == reporting.ID
                                  && p.StatusActionID == action.ID
                                  select p).SingleOrDefault();

                    if (status == null)
                    {
                        status = new SiteAdmin_Status()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            StatusActionID = action.ID,
                            StatusReportingID = reporting.ID,
                            StatusGroupID = Convert.ToInt32(Request.Form["StatusGroupID"]),
                            UpdatedByID = "",
                            UpdatedDate = null,
                            IsResolvedStatus = !string.IsNullOrEmpty(Request.Form["addIsResolved"]),
                            WIPType = Convert.ToInt32(Request.Form["addWIPType"]),
                        };

                        db.Add(status);
                        db.SaveChanges();
                    }
                    else
                    {
                        status.IsDeleted = false;
                        status.UpdatedByID = _userManager.GetUserId(User);
                        status.UpdatedDate = DateTime.Now;
                        status.IsResolvedStatus = !string.IsNullOrEmpty(Request.Form["addIsResolved"]);
                        db.Update(status);
                        db.SaveChanges();
                    }


                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Status_Delete/{ID}/{StatusGroupID}")]
        public async Task<IActionResult> SiteAdmin_Status_Delete(int ID, int StatusGroupID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_StatusGroup = db.SiteAdmin_Statuses.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_StatusGroup != null)
            {
                SiteAdmin_StatusGroup.IsDeleted = true;
                SiteAdmin_StatusGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_StatusGroup.UpdatedDate = DateTime.Now;

                db.Update(SiteAdmin_StatusGroup);
                db.SaveChanges();
            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_StatusGroups_Edit/{StatusGroupID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Status_Restore/{ID}/{StatusGroupID}")]
        public async Task<IActionResult> SiteAdmin_Status_Restore(int ID, int StatusGroupID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_StatusGroup = db.SiteAdmin_Statuses.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_StatusGroup != null)
            {
                SiteAdmin_StatusGroup.IsDeleted = false;
                SiteAdmin_StatusGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_StatusGroup.UpdatedDate = DateTime.Now;

                db.Update(SiteAdmin_StatusGroup);
                db.SaveChanges();
            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_StatusGroups_Edit/{StatusGroupID}");
        }


        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_StatusGroups_ItemUpdate")]
        public async Task<IActionResult> SiteAdmin_StatusGroups_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["addAction"])
                && !string.IsNullOrEmpty(Request.Form["addReporting"])
                && !string.IsNullOrEmpty(Request.Form["StatusGroupID"])
                && !string.IsNullOrEmpty(Request.Form["ItemID"]))
            {
                try
                {
                    var action = db.SiteAdmin_StatusActions.Where(p => p.StatusActionName == Request.Form["addAction"].ToString()).SingleOrDefault();

                    if (action == null)
                    {
                        action = new SiteAdmin_StatusAction()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            StatusActionName = Request.Form["addAction"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Update(action);
                        db.SaveChanges();
                    }

                    var reporting = db.SiteAdmin_StatusReportings.Where(p => p.StatusReportingName == Request.Form["addReporting"].ToString()).SingleOrDefault();

                    if (reporting == null)
                    {
                        reporting = new SiteAdmin_StatusReporting()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            StatusReportingName = Request.Form["addReporting"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Update(reporting);
                        db.SaveChanges();
                    }


                    var status = (from p in db.SiteAdmin_Statuses
                                  where p.ID == Convert.ToInt32(Request.Form["ItemID"])
                                  select p).SingleOrDefault();

                    var existing = (from p in db.SiteAdmin_Statuses
                                    where p.StatusGroupID == Convert.ToInt32(Request.Form["StatusGroupID"])
                                    && p.StatusReportingID == reporting.ID
                                    && p.StatusActionID == action.ID
                                    && p.ID != status.ID
                                    select p).SingleOrDefault();

                    if (status != null && existing == null)
                    {
                        if (status.StatusActionID != action.ID)
                            status.StatusActionID = action.ID;

                        if (status.StatusReportingID != reporting.ID)
                            status.StatusReportingID = reporting.ID;

                        status.IsResolvedStatus = !string.IsNullOrEmpty(Request.Form["addIsResolved"]);
                        status.WIPType = Convert.ToInt32(Request.Form["addWIPType"]);
                        status.UpdatedByID = _userManager.GetUserId(User);
                        status.UpdatedDate = DateTime.Now;
                        db.Update(status);
                        db.SaveChanges();
                    }


                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }


    }
}
