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
    public class SiteAdmin_PrioritiesController : Controller
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

        public SiteAdmin_PrioritiesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities")]
        public async Task<IActionResult> SiteAdmin_Priorities()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            SiteAdmin_PrioritiesModel model = new SiteAdmin_PrioritiesModel()
            {
                SiteAdmin_PriorityItems = new List<SiteAdmin_PrioritiesModel.SiteAdmin_PriorityItem>(),
            };
            var opProfs = (from p in db.OperationalProfiles
                           select new { p.UserID, p.FirstName, p.LastName }).ToList();
            var siteAdmin_Priorities = db.SiteAdmin_Priorities.ToList();
            foreach (var priority in siteAdmin_Priorities)
            {
                SiteAdmin_PrioritiesModel.SiteAdmin_PriorityItem item = new SiteAdmin_PrioritiesModel.SiteAdmin_PriorityItem()
                {
                    CreatedBy = priority.CreatedBy,
                    PriorityName = priority.PriorityName,
                    CreatedByUsername = "",
                    DateCreated = priority.DateCreated,
                    DateUpdated = priority.DateUpdated,
                    ID = priority.ID,
                    UpdatedBy = priority.UpdatedBy,
                    UpdatedByUsername = "",
                    IsDeleted = priority.IsDeleted,
                };

                var cOp = opProfs.Where(p => p.UserID == priority.CreatedBy).SingleOrDefault();
                if (cOp != null)
                    item.CreatedByUsername = $"{cOp.FirstName} {cOp.LastName}";

                if (!string.IsNullOrEmpty(priority.UpdatedBy))
                {
                    var uOp = opProfs.Where(p => p.UserID == priority.UpdatedBy).SingleOrDefault();
                    if (uOp != null)
                        item.UpdatedByUsername = $"{uOp.FirstName} {uOp.LastName}";
                }

                model.SiteAdmin_PriorityItems.Add(item);
            }

            model.SiteAdmin_PriorityItems = model.SiteAdmin_PriorityItems.OrderBy(p => p.PriorityName).ToList();

            return View("~/Views/operational/SiteAdmin/SiteAdmin_Priorities/SiteAdmin_Priorities.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Add")]
        public async Task<IActionResult> SiteAdmin_Priorities_Add()
        {
            SiteAdmin_Priorities_AddModel model = new SiteAdmin_Priorities_AddModel()
            {

            };

            return View("~/Views/operational/SiteAdmin/SiteAdmin_Priorities/SiteAdmin_Priorities_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Add")]
        public async Task<IActionResult> SiteAdmin_Priorities_Add(SiteAdmin_Priorities_AddModel model)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.SiteAdmin_Priorities.Where(p => p.PriorityName.ToUpper() == model.PriorityName.ToUpper()).SingleOrDefault();
                if (existing == null)
                {
                    Data.SiteAdmin_Priority SiteAdmin_Priority = new Data.SiteAdmin_Priority()
                    {
                        CreatedBy = _userManager.GetUserId(User),
                        PriorityName = model.PriorityName,
                        DateCreated = DateTime.Now,
                        IsDeleted = false,
                        DateUpdated = null,
                        UpdatedBy = "",
                    };

                    db.SiteAdmin_Priorities.Add(SiteAdmin_Priority);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.PriorityName} already exists";
                }


            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_Priorities/SiteAdmin_Priorities_Add.cshtml", model);
        }
        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_Priorities_Edit(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_Priority = db.SiteAdmin_Priorities.Where(p => p.ID == ID).SingleOrDefault();

            SiteAdmin_Priorities_EditModel editSiteAdmin_PriorityViewModel = new SiteAdmin_Priorities_EditModel()
            {
                ErrorMessage = "",
                IsSuccess = false,
                PriorityName = SiteAdmin_Priority.PriorityName,
            };


            return View("~/Views/operational/SiteAdmin/SiteAdmin_Priorities/SiteAdmin_Priorities_Edit.cshtml", editSiteAdmin_PriorityViewModel);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_Priorities_Edit(int ID, SiteAdmin_Priorities_EditModel model)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.SiteAdmin_Priorities.Where(p => p.PriorityName.ToUpper() == model.PriorityName.ToUpper() && p.ID != ID).SingleOrDefault();

                if (existing == null)
                {
                    var SiteAdmin_Priority = db.SiteAdmin_Priorities.Where(p => p.ID == ID).SingleOrDefault();

                    SiteAdmin_Priority.PriorityName = model.PriorityName;
                    SiteAdmin_Priority.UpdatedBy = _userManager.GetUserId(User);
                    SiteAdmin_Priority.DateUpdated = DateTime.Now;

                    db.SiteAdmin_Priorities.Update(SiteAdmin_Priority);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"{model.PriorityName} already exists";
                }

            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_Priorities/SiteAdmin_Priorities_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Priorities_Delete(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_Priority = db.SiteAdmin_Priorities.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_Priority != null)
            {
                SiteAdmin_Priority.IsDeleted = true;
                SiteAdmin_Priority.UpdatedBy = _userManager.GetUserId(User);
                SiteAdmin_Priority.DateUpdated = DateTime.Now;

                db.SiteAdmin_Priorities.Update(SiteAdmin_Priority);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_Priorities");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Priorities_Restore/{ID}")]
        public async Task<IActionResult> SiteAdmin_Priorities_Restore(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_Priority = db.SiteAdmin_Priorities.Where(p => p.ID == ID).SingleOrDefault();

            if (SiteAdmin_Priority != null)
            {
                SiteAdmin_Priority.IsDeleted = false;
                SiteAdmin_Priority.UpdatedBy = _userManager.GetUserId(User);
                SiteAdmin_Priority.DateUpdated = DateTime.Now;

                db.SiteAdmin_Priorities.Update(SiteAdmin_Priority);
                db.SaveChanges();
            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_Priorities");
        }

    }
}
