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
    public class SiteAdmin_MeetingAgendaGroupsController : Controller
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

        public SiteAdmin_MeetingAgendaGroupsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            SiteAdmin_MeetingAgendaGroupsModel model = new SiteAdmin_MeetingAgendaGroupsModel()
            {
                SiteAdmin_MeetingAgendaGroupItems = new List<SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem>(),
            };
            var opProfs = (from p in db.OperationalProfiles
                           select new { p.UserID, p.FirstName, p.LastName }).ToList();
            var SiteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.ToList();
            var siteAdmin_MeetingAgendas = db.SiteAdmin_MeetingAgendas.ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();

            foreach (var MeetingAgendaGroup in SiteAdmin_MeetingAgendaGroups)
            {
                SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem item = new SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem()
                {
                    CreatedByID = MeetingAgendaGroup.CreatedByID,
                    MeetingAgendaGroupName = MeetingAgendaGroup.MeetingAgendaGroupName,
                    CreatedByUsername = "",
                    CreatedDate = MeetingAgendaGroup.CreatedDate,
                    UpdatedDate = MeetingAgendaGroup.UpdatedDate,
                    ID = MeetingAgendaGroup.ID,
                    UpdatedByID = MeetingAgendaGroup.UpdatedByID,
                    UpdatedByUsername = "",
                    IsDeleted = MeetingAgendaGroup.IsDeleted,
                    SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItems = new List<SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem.SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItem>(),
                };

                var cOp = opProfs.Where(p => p.UserID == MeetingAgendaGroup.CreatedByID).SingleOrDefault();
                if (cOp != null)
                    item.CreatedByUsername = $"{cOp.FirstName} {cOp.LastName}";

                if (!string.IsNullOrEmpty(MeetingAgendaGroup.UpdatedByID))
                {
                    var uOp = opProfs.Where(p => p.UserID == MeetingAgendaGroup.UpdatedByID).SingleOrDefault();
                    if (uOp != null)
                        item.UpdatedByUsername = $"{uOp.FirstName} {uOp.LastName}";
                }

                var MeetingAgendas = siteAdmin_MeetingAgendas.Where(p => p.MeetingAgendaGroupID == MeetingAgendaGroup.ID).ToList();
                foreach (var s in MeetingAgendas)
                {
                    SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem.SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItem itemMeetingAgendaItem = new SiteAdmin_MeetingAgendaGroupsModel.SiteAdmin_MeetingAgendaGroupItem.SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItem()
                    {
                        CreatedByID = s.CreatedByID,
                        CreatedDate = s.CreatedDate,
                        ID = s.ID,
                        IsDeleted = s.IsDeleted,
                        SiteAdmin_MeetingAgendaAction = siteAdmin_MeetingAgendaActions.Where(p => p.ID == s.MeetingAgendaActionID).SingleOrDefault(),
                        SiteAdmin_MeetingAgendaReporting = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == s.MeetingAgendaReportingID).SingleOrDefault(),
                        MeetingAgendaActionID = s.MeetingAgendaActionID,
                        MeetingAgendaGroupID = s.MeetingAgendaGroupID,
                        MeetingAgendaReportingID = s.MeetingAgendaReportingID,
                        UpdatedByID = s.UpdatedByID,
                        UpdatedDate = s.UpdatedDate,
                    };

                    item.SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItems.Add(itemMeetingAgendaItem);
                }

                model.SiteAdmin_MeetingAgendaGroupItems.Add(item);
            }

            model.SiteAdmin_MeetingAgendaGroupItems = model.SiteAdmin_MeetingAgendaGroupItems.OrderBy(p => p.MeetingAgendaGroupName).ToList();

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups/SiteAdmin_MeetingAgendaGroups.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Add")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Add()
        {
            SiteAdmin_MeetingAgendaGroups_AddModel model = new SiteAdmin_MeetingAgendaGroups_AddModel()
            {

            };

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups/SiteAdmin_MeetingAgendaGroups_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Add")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Add(SiteAdmin_MeetingAgendaGroups_AddModel model)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.MeetingAgendaGroupName.ToUpper() == model.MeetingAgendaGroupName.ToUpper()).SingleOrDefault();
                if (existing == null)
                {
                    Data.SiteAdmin_MeetingAgendaGroup SiteAdmin_MeetingAgendaGroup = new Data.SiteAdmin_MeetingAgendaGroup()
                    {
                        CreatedByID = _userManager.GetUserId(User),
                        MeetingAgendaGroupName = model.MeetingAgendaGroupName,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                        UpdatedDate = null,
                        UpdatedByID = "",
                    };

                    db.SiteAdmin_MeetingAgendaGroups.Add(SiteAdmin_MeetingAgendaGroup);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.MeetingAgendaGroupName} already exists";
                }


            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups/SiteAdmin_MeetingAgendaGroups_Add.cshtml", model);
        }
        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Edit(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == ID).SingleOrDefault();

            SiteAdmin_MeetingAgendaGroups_EditModel model = new SiteAdmin_MeetingAgendaGroups_EditModel()
            {
                ErrorMessage = "",
                IsSuccess = false,
                MeetingAgendaGroupName = SiteAdmin_MeetingAgendaGroup.MeetingAgendaGroupName,
                SiteAdmin_MeetingAgendaGroups_EditItems = new List<SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem>(),
                MeetingAgendaGroupID = ID,
            };

            var siteAdmin_MeetingAgendas = db.SiteAdmin_MeetingAgendas.Where(p => p.MeetingAgendaGroupID == ID).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendas)
            {
                SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem item = new SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem()
                {
                    CreatedByID = MeetingAgenda.CreatedByID,
                    CreatedDate = MeetingAgenda.CreatedDate,
                    ID = MeetingAgenda.ID,
                    SiteAdmin_MeetingAgendaAction = siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault(),
                    SiteAdmin_MeetingAgendaReporting = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault(),
                    MeetingAgendaActionID = MeetingAgenda.MeetingAgendaActionID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                    MeetingAgendaReportingID = MeetingAgenda.MeetingAgendaReportingID,
                    UpdatedByID = MeetingAgenda.UpdatedByID,
                    UpdatedDate = MeetingAgenda.UpdatedDate,
                    IsDeleted = MeetingAgenda.IsDeleted,
                    IsResolvedMeetingAgenda = MeetingAgenda.IsResolvedMeetingAgenda,
                    WIPType = MeetingAgenda.WIPType,
                };

                model.SiteAdmin_MeetingAgendaGroups_EditItems.Add(item);
            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups/SiteAdmin_MeetingAgendaGroups_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Edit(int ID, SiteAdmin_MeetingAgendaGroups_EditModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            model.SiteAdmin_MeetingAgendaGroups_EditItems = new List<SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem>();
            model.MeetingAgendaGroupID = ID;

            var siteAdmin_MeetingAgendas = db.SiteAdmin_MeetingAgendas.Where(p => p.MeetingAgendaGroupID == ID).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendas)
            {
                SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem item = new SiteAdmin_MeetingAgendaGroups_EditModel.SiteAdmin_MeetingAgendaGroups_EditItem()
                {
                    CreatedByID = MeetingAgenda.CreatedByID,
                    CreatedDate = MeetingAgenda.CreatedDate,
                    ID = MeetingAgenda.ID,
                    SiteAdmin_MeetingAgendaAction = siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault(),
                    SiteAdmin_MeetingAgendaReporting = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault(),
                    MeetingAgendaActionID = MeetingAgenda.MeetingAgendaActionID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                    MeetingAgendaReportingID = MeetingAgenda.MeetingAgendaReportingID,
                    UpdatedByID = MeetingAgenda.UpdatedByID,
                    UpdatedDate = MeetingAgenda.UpdatedDate,
                    IsDeleted = MeetingAgenda.IsDeleted,
                    IsResolvedMeetingAgenda = MeetingAgenda.IsResolvedMeetingAgenda,
                    WIPType = MeetingAgenda.WIPType,
                };
                model.SiteAdmin_MeetingAgendaGroups_EditItems.Add(item);
            }

            if (ModelState.IsValid)
            {
                var existing = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.MeetingAgendaGroupName.ToUpper() == model.MeetingAgendaGroupName.ToUpper() && p.ID != ID).SingleOrDefault();

                if (existing == null)
                {
                    var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == ID).SingleOrDefault();

                    SiteAdmin_MeetingAgendaGroup.MeetingAgendaGroupName = model.MeetingAgendaGroupName;
                    SiteAdmin_MeetingAgendaGroup.UpdatedByID = _userManager.GetUserId(User);
                    SiteAdmin_MeetingAgendaGroup.UpdatedDate = DateTime.Now;

                    db.SiteAdmin_MeetingAgendaGroups.Update(SiteAdmin_MeetingAgendaGroup);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.MeetingAgendaGroupName} already exists";
                }

            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups/SiteAdmin_MeetingAgendaGroups_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Delete(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_MeetingAgendaGroup != null)
            {
                SiteAdmin_MeetingAgendaGroup.IsDeleted = true;
                SiteAdmin_MeetingAgendaGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_MeetingAgendaGroup.UpdatedDate = DateTime.Now;

                db.SiteAdmin_MeetingAgendaGroups.Update(SiteAdmin_MeetingAgendaGroup);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Restore/{ID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_Restore(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_MeetingAgendaGroup != null)
            {
                SiteAdmin_MeetingAgendaGroup.IsDeleted = false;
                SiteAdmin_MeetingAgendaGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_MeetingAgendaGroup.UpdatedDate = DateTime.Now;

                db.SiteAdmin_MeetingAgendaGroups.Update(SiteAdmin_MeetingAgendaGroup);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups");
        }


        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_ActionSearch")]
        public JsonResult SiteAdmin_MeetingAgendaGroups_ActionSearch(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            var siteAdmin_MeetingAgendaActions = (from p in db.SiteAdmin_MeetingAgendaActions
                                           where p.MeetingAgendaActionName.ToUpper().Contains(Prefix.ToUpper())
                                           orderby p.MeetingAgendaActionName
                                           select p).ToList();

            List<object> results = new List<object>();

            foreach (var action in siteAdmin_MeetingAgendaActions)
            {
                results.Add(new
                {
                    Text = action.MeetingAgendaActionName,
                    Value = action.MeetingAgendaActionName,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_ReportingSearch")]
        public JsonResult SiteAdmin_MeetingAgendaGroups_ReportingSearch(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            var siteAdmin_MeetingAgendaReportings = (from p in db.SiteAdmin_MeetingAgendaReportings
                                              where p.MeetingAgendaReportingName.ToUpper().Contains(Prefix.ToUpper())
                                              orderby p.MeetingAgendaReportingName
                                              select p).ToList();

            List<object> results = new List<object>();

            foreach (var Reporting in siteAdmin_MeetingAgendaReportings)
            {
                results.Add(new
                {
                    Text = Reporting.MeetingAgendaReportingName,
                    Value = Reporting.MeetingAgendaReportingName,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["addAction"])
                && !string.IsNullOrEmpty(Request.Form["addReporting"])
                && !string.IsNullOrEmpty(Request.Form["MeetingAgendaGroupID"]))
            {
                try
                {
                    var action = db.SiteAdmin_MeetingAgendaActions.Where(p => p.MeetingAgendaActionName == Request.Form["addAction"].ToString()).SingleOrDefault();

                    if (action == null)
                    {
                        action = new SiteAdmin_MeetingAgendaAction()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            MeetingAgendaActionName = Request.Form["addAction"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Add(action);
                        db.SaveChanges();
                    }

                    var reporting = db.SiteAdmin_MeetingAgendaReportings.Where(p => p.MeetingAgendaReportingName == Request.Form["addReporting"].ToString()).SingleOrDefault();

                    if (reporting == null)
                    {
                        reporting = new SiteAdmin_MeetingAgendaReporting()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            MeetingAgendaReportingName = Request.Form["addReporting"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Add(reporting);
                        db.SaveChanges();
                    }


                    var MeetingAgenda = (from p in db.SiteAdmin_MeetingAgendas
                                  where p.MeetingAgendaGroupID == Convert.ToInt32(Request.Form["MeetingAgendaGroupID"])
                                  && p.MeetingAgendaReportingID == reporting.ID
                                  && p.MeetingAgendaActionID == action.ID
                                  select p).SingleOrDefault();

                    if (MeetingAgenda == null)
                    {
                        MeetingAgenda = new SiteAdmin_MeetingAgenda()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            MeetingAgendaActionID = action.ID,
                            MeetingAgendaReportingID = reporting.ID,
                            MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroupID"]),
                            UpdatedByID = "",
                            UpdatedDate = null,
                            IsResolvedMeetingAgenda = !string.IsNullOrEmpty(Request.Form["addIsResolved"]),
                            WIPType = Convert.ToInt32(Request.Form["addWIPType"]),
                        };

                        db.Add(MeetingAgenda);
                        db.SaveChanges();
                    }
                    else
                    {
                        MeetingAgenda.IsDeleted = false;
                        MeetingAgenda.UpdatedByID = _userManager.GetUserId(User);
                        MeetingAgenda.UpdatedDate = DateTime.Now;
                        MeetingAgenda.IsResolvedMeetingAgenda = !string.IsNullOrEmpty(Request.Form["addIsResolved"]);
                        db.Update(MeetingAgenda);
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
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgenda_Delete/{ID}/{MeetingAgendaGroupID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgenda_Delete(int ID, int MeetingAgendaGroupID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendas.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_MeetingAgendaGroup != null)
            {
                SiteAdmin_MeetingAgendaGroup.IsDeleted = true;
                SiteAdmin_MeetingAgendaGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_MeetingAgendaGroup.UpdatedDate = DateTime.Now;

                db.Update(SiteAdmin_MeetingAgendaGroup);
                db.SaveChanges();
            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Edit/{MeetingAgendaGroupID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgenda_Restore/{ID}/{MeetingAgendaGroupID}")]
        public async Task<IActionResult> SiteAdmin_MeetingAgenda_Restore(int ID, int MeetingAgendaGroupID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeetingAgendaGroup = db.SiteAdmin_MeetingAgendas.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_MeetingAgendaGroup != null)
            {
                SiteAdmin_MeetingAgendaGroup.IsDeleted = false;
                SiteAdmin_MeetingAgendaGroup.UpdatedByID = _userManager.GetUserId(User);
                SiteAdmin_MeetingAgendaGroup.UpdatedDate = DateTime.Now;

                db.Update(SiteAdmin_MeetingAgendaGroup);
                db.SaveChanges();
            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_Edit/{MeetingAgendaGroupID}");
        }


        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_MeetingAgendaGroups_ItemUpdate")]
        public async Task<IActionResult> SiteAdmin_MeetingAgendaGroups_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["addAction"])
                && !string.IsNullOrEmpty(Request.Form["addReporting"])
                && !string.IsNullOrEmpty(Request.Form["MeetingAgendaGroupID"])
                && !string.IsNullOrEmpty(Request.Form["ItemID"]))
            {
                try
                {
                    var action = db.SiteAdmin_MeetingAgendaActions.Where(p => p.MeetingAgendaActionName == Request.Form["addAction"].ToString()).SingleOrDefault();

                    if (action == null)
                    {
                        action = new SiteAdmin_MeetingAgendaAction()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            MeetingAgendaActionName = Request.Form["addAction"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Update(action);
                        db.SaveChanges();
                    }

                    var reporting = db.SiteAdmin_MeetingAgendaReportings.Where(p => p.MeetingAgendaReportingName == Request.Form["addReporting"].ToString()).SingleOrDefault();

                    if (reporting == null)
                    {
                        reporting = new SiteAdmin_MeetingAgendaReporting()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            MeetingAgendaReportingName = Request.Form["addReporting"].ToString(),
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };

                        db.Update(reporting);
                        db.SaveChanges();
                    }


                    var MeetingAgenda = (from p in db.SiteAdmin_MeetingAgendas
                                  where p.ID == Convert.ToInt32(Request.Form["ItemID"])
                                  select p).SingleOrDefault();

                    var existing = (from p in db.SiteAdmin_MeetingAgendas
                                    where p.MeetingAgendaGroupID == Convert.ToInt32(Request.Form["MeetingAgendaGroupID"])
                                    && p.MeetingAgendaReportingID == reporting.ID
                                    && p.MeetingAgendaActionID == action.ID
                                    && p.ID != MeetingAgenda.ID
                                    select p).SingleOrDefault();

                    if (MeetingAgenda != null && existing == null)
                    {
                        if (MeetingAgenda.MeetingAgendaActionID != action.ID)
                            MeetingAgenda.MeetingAgendaActionID = action.ID;

                        if (MeetingAgenda.MeetingAgendaReportingID != reporting.ID)
                            MeetingAgenda.MeetingAgendaReportingID = reporting.ID;

                        MeetingAgenda.IsResolvedMeetingAgenda = !string.IsNullOrEmpty(Request.Form["addIsResolved"]);
                        MeetingAgenda.WIPType = Convert.ToInt32(Request.Form["addWIPType"]);
                        MeetingAgenda.UpdatedByID = _userManager.GetUserId(User);
                        MeetingAgenda.UpdatedDate = DateTime.Now;
                        db.Update(MeetingAgenda);
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
