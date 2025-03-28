using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.ModulesModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ModulesController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public ModulesController(
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/Modules/AddTimeOfWorkPlanned")]
        public async Task<IActionResult> AddTimeOfWorkPlanned()
        {
            AddTimeOfWorkPlannedModel model = new AddTimeOfWorkPlannedModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(Request.Query["AT"]),
                ActivityID = Convert.ToInt32(Request.Query["AID"]),
            };

            HttpContext.Session.SetString("TimeOfWorkPlannedActivityType", Request.Query["AT"]);
            HttpContext.Session.SetString("TimeOfWorkPlannedActivityID", Request.Query["AID"]);

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/AddTimeOfWorkPlanned.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/AddTimeOfWorkPlanned")]
        public async Task<IActionResult> AddTimeOfWorkPlanned(AddTimeOfWorkPlannedModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkPlannedActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkPlannedActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                Data.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new Module_TimeOfWorkPlanned()
                {
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    DateCreated = DateTime.Now,
                    CreatedByUserID = _userManager.GetUserId(User),
                    DescriptionOfWorkPlanned = model.Description,
                    ActivityID = model.ActivityID,
                    ActivityTypeID = (int)model.ActivityType,
                    DateOfWorkPlanned = model.DateOfWorkPlanned.Value,
                    ExternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.ExternalChargeOutRatePerHour : null,
                    InternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.InternalChargeOutRatePerHour : null,
                    MinOfWorkPlanned = model.MinOfWorkPlanned.Value,
                    IsDeleted = false,
                };

                db.Add(module_TimeOfWorkPlanned);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/AddTimeOfWorkPlanned.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/EditTimeOfWorkPlanned/{ID}")]
        public async Task<IActionResult> EditTimeOfWorkPlanned(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.Module_TimeOfWorkPlanneds.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            EditTimeOfWorkPlannedModel model = new EditTimeOfWorkPlannedModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)item.ActivityTypeID,
                ActivityID = item.ActivityID,
                DateOfWorkPlanned = item.DateOfWorkPlanned,
                Description = item.DescriptionOfWorkPlanned,
                MinOfWorkPlanned = item.MinOfWorkPlanned,
            };

            HttpContext.Session.SetString("TimeOfWorkPlannedActivityType", item.ActivityTypeID.ToString());
            HttpContext.Session.SetString("TimeOfWorkPlannedActivityID", item.ActivityID.ToString());

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.ResponsibleUserID == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/EditTimeOfWorkPlanned.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/EditTimeOfWorkPlanned/{ID}")]
        public async Task<IActionResult> EditTimeOfWorkPlanned(int ID, EditTimeOfWorkPlannedModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            var item = db.Module_TimeOfWorkPlanneds.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkPlannedActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkPlannedActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                item.ResponsibleUserID = Request.Form["ResponsibleUser"];
                item.DescriptionOfWorkPlanned = model.Description;
                item.ActivityID = model.ActivityID;
                item.ActivityTypeID = (int)model.ActivityType;
                item.DateOfWorkPlanned = model.DateOfWorkPlanned.Value;
                item.ExternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.ExternalChargeOutRatePerHour : null;
                item.InternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.InternalChargeOutRatePerHour : null;
                item.MinOfWorkPlanned = model.MinOfWorkPlanned.Value;
                item.IsDeleted = false;

                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/EditTimeOfWorkPlanned.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/DeleteTimeOfWorkPlanned/{timeOfWorkPlannedID}")]
        public async Task<IActionResult> DeleteTimeOfWorkPlanned(int timeOfWorkPlannedID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_TimeOfWorkPlanned = db.Module_TimeOfWorkPlanneds.Where(p => p.ID == timeOfWorkPlannedID).SingleOrDefault();

            if (module_TimeOfWorkPlanned == null)
                return Redirect(Request.Query["R"]);

            module_TimeOfWorkPlanned.IsDeleted = true;
            db.Update(module_TimeOfWorkPlanned);
            db.SaveChanges();

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Modules/AddTimeOfWorkAllocated")]
        public async Task<IActionResult> AddTimeOfWorkAllocated()
        {
            AddTimeOfWorkAllocatedModel model = new AddTimeOfWorkAllocatedModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(Request.Query["AT"]),
                ActivityID = Convert.ToInt32(Request.Query["AID"]),
            };

            HttpContext.Session.SetString("TimeOfWorkAllocatedActivityType", Request.Query["AT"]);
            HttpContext.Session.SetString("TimeOfWorkAllocatedActivityID", Request.Query["AID"]);

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/AddTimeOfWorkAllocated.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/AddTimeOfWorkAllocated")]
        public async Task<IActionResult> AddTimeOfWorkAllocated(AddTimeOfWorkAllocatedModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkAllocatedActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkAllocatedActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                Data.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    DateCreated = DateTime.Now,
                    CreatedByUserID = _userManager.GetUserId(User),
                    DescriptionOfWorkAllocated = model.Description,
                    ActivityID = model.ActivityID,
                    ActivityTypeID = (int)model.ActivityType,
                    ExternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.ExternalChargeOutRatePerHour : null,
                    InternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.InternalChargeOutRatePerHour : null,
                    IsDeleted = false,
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                };

                db.Add(module_TimeOfWorkAllocated);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/AddTimeOfWorkAllocated.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/EditTimeOfWorkAllocated/{ID}")]
        public async Task<IActionResult> EditTimeOfWorkAllocated(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            EditTimeOfWorkAllocatedModel model = new EditTimeOfWorkAllocatedModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)item.ActivityTypeID,
                ActivityID = item.ActivityID,
                StartTime = item.StartTime,
                Description = item.DescriptionOfWorkAllocated,
                EndTime = item.EndTime,
            };

            HttpContext.Session.SetString("TimeOfWorkAllocatedActivityType", item.ActivityTypeID.ToString());
            HttpContext.Session.SetString("TimeOfWorkAllocatedActivityID", item.ActivityID.ToString());

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.ResponsibleUserID == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/EditTimeOfWorkAllocated.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/EditTimeOfWorkAllocated/{ID}")]
        public async Task<IActionResult> EditTimeOfWorkAllocated(int ID, EditTimeOfWorkAllocatedModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            var item = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkAllocatedActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("TimeOfWorkAllocatedActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Id == _userManager.GetUserId(User)).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                item.ResponsibleUserID = Request.Form["ResponsibleUser"];
                item.DescriptionOfWorkAllocated = model.Description;
                item.ActivityID = model.ActivityID;
                item.ActivityTypeID = (int)model.ActivityType;
                item.StartTime = model.StartTime.Value;
                item.ExternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.ExternalChargeOutRatePerHour : null;
                item.InternalChargeOutRatePerHour = responsibleUser != null ? responsibleUser.InternalChargeOutRatePerHour : null;
                item.EndTime = model.EndTime.Value;
                item.IsDeleted = false;

                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/EditTimeOfWorkAllocated.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/DeleteTimeOfWorkAllocated/{timeOfWorkAllocatedID}")]
        public async Task<IActionResult> DeleteTimeOfWorkAllocated(int timeOfWorkAllocatedID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_TimeOfWorkAllocated = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == timeOfWorkAllocatedID).SingleOrDefault();

            if (module_TimeOfWorkAllocated == null)
                return Redirect(Request.Query["R"]);

            module_TimeOfWorkAllocated.IsDeleted = true;
            db.Update(module_TimeOfWorkAllocated);
            db.SaveChanges();

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Modules/AddTravelAllocation")]
        public async Task<IActionResult> AddTravelAllocation()
        {
            AddTravelAllocationModel model = new AddTravelAllocationModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(Request.Query["AT"]),
                ActivityID = Convert.ToInt32(Request.Query["AID"]),
                VehicleTypeID = new List<SelectListItem>(),
                VehicleID = new List<SelectListItem>(),
            };

            HttpContext.Session.SetString("TravelAllocationActivityType", Request.Query["AT"]);
            HttpContext.Session.SetString("TravelAllocationActivityID", Request.Query["AID"]);

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var vehicles = db.Vehicles.ToList();
            foreach (var v in vehicles)
            {
                var opProfRegularDriver = opProfs.Where(p => p.UserID == v.RegularDriverID).SingleOrDefault();
                model.VehicleID.Add(new SelectListItem() { Value = v.ID.ToString(), Text = $"{v.RegistrationNumber} - {v.VehicleTypeName} {(opProfRegularDriver != null ? $"({opProfRegularDriver.FirstName} {opProfRegularDriver.LastName})" : "")}" });
            }
            model.VehicleID = model.VehicleID.OrderBy(p => p.Text).ToList();

            foreach (var vT in (VehicleTypeEnum[])Enum.GetValues(typeof(VehicleTypeEnum)))
            {
                model.VehicleTypeID.Add(new SelectListItem() { Value = ((int)vT).ToString(), Text = $"{vT.GetDescription()}" });
            }
            model.VehicleTypeID = model.VehicleTypeID.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/AddTravelAllocation.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/AddTravelAllocation")]
        public async Task<IActionResult> AddTravelAllocation(AddTravelAllocationModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            model.ResponsibleUser = new List<SelectListItem>();
            model.VehicleID = new List<SelectListItem>();
            model.VehicleTypeID = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("TravelAllocationActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("TravelAllocationActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var vehicles = db.Vehicles.ToList();
            foreach (var v in vehicles)
            {
                var opProfRegularDriver = opProfs.Where(p => p.UserID == v.RegularDriverID).SingleOrDefault();
                model.VehicleID.Add(new SelectListItem() { Value = v.ID.ToString(), Text = $"{v.RegistrationNumber} - {v.VehicleTypeName} {(opProfRegularDriver != null ? $"({opProfRegularDriver.FirstName} {opProfRegularDriver.LastName})" : "")}", Selected = Request.Form["VehicleID"] == v.ID.ToString() });
            }
            model.VehicleID = model.VehicleID.OrderBy(p => p.Text).ToList();

            foreach (var vT in (VehicleTypeEnum[])Enum.GetValues(typeof(VehicleTypeEnum)))
            {
                model.VehicleTypeID.Add(new SelectListItem() { Value = ((int)vT).ToString(), Text = $"{vT.GetDescription()}", Selected = Request.Form["VehicleTypeID"] == ((int)vT).ToString() });
            }
            model.VehicleTypeID = model.VehicleTypeID.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                Data.Module_TravelAllocation module_TravelAllocation = new Module_TravelAllocation()
                {
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    DateCreated = DateTime.Now,
                    CreatedByUserID = _userManager.GetUserId(User),
                    DescriptionOfTravelAllocation = model.DescriptionOfTravelAllocation,
                    ActivityID = model.ActivityID,
                    ActivityTypeID = (int)model.ActivityType,
                    DateOfTravelAllocation = model.DateOfTravelAllocation.Value,
                    RatePerKm = vehicles.Where(p => p.ID == Convert.ToInt32(Request.Form["VehicleID"])).SingleOrDefault().RatePerKM,
                    VehicleDistanceKm = model.VehicleDistanceKm.Value,
                    VehicleID = Convert.ToInt32(Request.Form["VehicleID"]),
                    VehicleTypeID = Convert.ToInt32(Request.Form["VehicleTypeID"]),
                    VehicleOdoEnd = model.VehicleOdoEnd.Value,
                    VehicleOdoStart = model.VehicleOdoStart.Value,
                    IsDeleted = false,
                    PhotoURL = "",
                };

                db.Add(module_TravelAllocation);
                db.SaveChanges();

                if (model.PhotoURL != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "modules-travelallocation";
                    string dirName = $"{module_TravelAllocation.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.PhotoURL.FileName);

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
                    model.PhotoURL.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);

                    module_TravelAllocation.PhotoURL = fileName;
                    db.Update(module_TravelAllocation);
                    db.SaveChanges();
                }

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/AddTravelAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/DeleteTravelAllocation/{TravelAllocationID}")]
        public async Task<IActionResult> DeleteTravelAllocation(int TravelAllocationID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_TravelAllocation = db.Module_TravelAllocations.Where(p => p.ID == TravelAllocationID).SingleOrDefault();

            if (module_TravelAllocation == null)
                return Redirect(Request.Query["R"]);

            module_TravelAllocation.IsDeleted = true;
            db.Update(module_TravelAllocation);
            db.SaveChanges();

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Modules/GetTravelAllocationAttachment/{ID}")]
        public async Task<IActionResult> A09_Flags_CompanyReview_GetAttachment(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_TravelAllocation = db.Module_TravelAllocations.Where(p => p.ID == ID).SingleOrDefault();

            if (module_TravelAllocation == null)
                return Content("The file you are looking for cannot be found.");


            string shareName = "modules-travelallocation";
            string dirName = $"{module_TravelAllocation.ID}";

            // Get a reference to the file
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
            ShareFileClient file = directory.GetFileClient(module_TravelAllocation.PhotoURL);

            // Download the file
            ShareFileDownloadInfo download = file.Download();
            Stream uploadFile = new MemoryStream();
            download.Content.CopyTo(uploadFile);
            uploadFile.Position = 0;
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(module_TravelAllocation.PhotoURL, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(module_TravelAllocation.PhotoURL));


            return Content("The file you are looking for cannot be found.");
        }


        [HttpGet]
        [Route("/operational/Modules/AddStockAllocation")]
        public async Task<IActionResult> AddStockAllocation()
        {
            AddStockAllocationModel model = new AddStockAllocationModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(Request.Query["AT"]),
                ActivityID = Convert.ToInt32(Request.Query["AID"]),
            };

            HttpContext.Session.SetString("StockAllocationActivityType", Request.Query["AT"]);
            HttpContext.Session.SetString("StockAllocationActivityID", Request.Query["AID"]);

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/AddStockAllocation.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/AddStockAllocation")]
        public async Task<IActionResult> AddStockAllocation(AddStockAllocationModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("StockAllocationActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("StockAllocationActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                Data.Module_StockAllocation module_StockAllocation = new Module_StockAllocation()
                {
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    DateCreated = DateTime.Now,
                    CreatedByUserID = _userManager.GetUserId(User),
                    DescriptionOfStockAllocation = model.DescriptionOfStockAllocation,
                    ActivityID = model.ActivityID,
                    ActivityTypeID = (int)model.ActivityType,
                    DateOfStockAllocation = model.DateOfStockAllocation.Value,
                    IsDeleted = false,
                };

                db.Add(module_StockAllocation);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/AddStockAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/DeleteStockAllocation/{StockAllocationID}")]
        public async Task<IActionResult> DeleteStockAllocation(int StockAllocationID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_StockAllocation = db.Module_StockAllocations.Where(p => p.ID == StockAllocationID).SingleOrDefault();

            if (module_StockAllocation == null)
                return Redirect(Request.Query["R"]);

            module_StockAllocation.IsDeleted = true;
            db.Update(module_StockAllocation);
            db.SaveChanges();

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Modules/AddInvoiceAllocation")]
        public async Task<IActionResult> AddInvoiceAllocation()
        {
            AddInvoiceAllocationModel model = new AddInvoiceAllocationModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(Request.Query["AT"]),
                ActivityID = Convert.ToInt32(Request.Query["AID"]),
            };

            HttpContext.Session.SetString("InvoiceAllocationActivityType", Request.Query["AT"]);
            HttpContext.Session.SetString("InvoiceAllocationActivityID", Request.Query["AID"]);

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/AddInvoiceAllocation.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/AddInvoiceAllocation")]
        public async Task<IActionResult> AddInvoiceAllocation(AddInvoiceAllocationModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("InvoiceAllocationActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("InvoiceAllocationActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                Data.Module_InvoiceAllocation module_InvoiceAllocation = new Module_InvoiceAllocation()
                {
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    DateCreated = DateTime.Now,
                    CreatedByUserID = _userManager.GetUserId(User),
                    DescriptionOfInvoiceAllocation = model.DescriptionOfInvoiceAllocation,
                    ActivityID = model.ActivityID,
                    ActivityTypeID = (int)model.ActivityType,
                    DateOfInvoiceAllocation = model.DateOfInvoiceAllocation.Value,
                    CustomerNo = model.CustomerNo,
                    InvoiceNo = model.InvoiceNo,
                    InvoiceAmount = model.InvoiceAmount.Value,
                    IsDeleted = false,
                };

                db.Add(module_InvoiceAllocation);
                db.SaveChanges();

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/Modules/AddInvoiceAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Modules/DeleteInvoiceAllocation/{InvoiceAllocationID}")]
        public async Task<IActionResult> DeleteInvoiceAllocation(int InvoiceAllocationID)
        {
            var db = new MyVoltageDbContext(_options);
            var module_InvoiceAllocation = db.Module_InvoiceAllocations.Where(p => p.ID == InvoiceAllocationID).SingleOrDefault();

            if (module_InvoiceAllocation == null)
                return Redirect(Request.Query["R"]);

            module_InvoiceAllocation.IsDeleted = true;
            db.Update(module_InvoiceAllocation);
            db.SaveChanges();

            return Redirect(Request.Query["R"]);
        }

        [HttpPost]
        [Route("/operational/Modules/AddInvoiceAllocation_Search_Customer_No")]
        public JsonResult A09_Flags_Create_Search_Customer_No(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    orderby p.Customer_No
                                    select p).ToList();

            List<object> results = new List<object>();
            List<string> customersAdded = new List<string>();
            foreach (var skybillCustomer in skybillCustomers)
            {
                if (results.Count == 10)
                    break;

                if (customersAdded.Contains(skybillCustomer.Customer_No))
                    continue;

                customersAdded.Add(skybillCustomer.Customer_No);

                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Customer_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/Modules/EditNonCompliance/{ID}")]
        public async Task<IActionResult> EditNonCompliance(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.Module_NonCompliances.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            EditNonComplianceModel model = new EditNonComplianceModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                ActivityType = (Data.ActivityTypeEnum)item.ActivityTypeID,
                ActivityID = item.ActivityID,
                UserComment = item.UserComment,
                EnforcerComment = item.EnforcerComment,
                HasBeenResolved = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Not Selected--]", Selected = !item.HasBeenResolved.HasValue },
                    new SelectListItem() { Value = "true", Text = "Yes", Selected = item.HasBeenResolved.HasValue && item.HasBeenResolved.Value },
                    new SelectListItem() { Value = "false", Text = "No", Selected = item.HasBeenResolved.HasValue && !item.HasBeenResolved.Value },
                }
            };

            HttpContext.Session.SetString("NonComplianceActivityType", item.ActivityTypeID.ToString());
            HttpContext.Session.SetString("NonComplianceActivityID", item.ActivityID.ToString());

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.ResponsibleUserID == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/Modules/EditNonCompliance.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Modules/EditNonCompliance/{ID}")]
        public async Task<IActionResult> EditNonCompliance(int ID, EditNonComplianceModel model)
        {
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            var item = db.Module_NonCompliances.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/");

            model.ResponsibleUser = new List<SelectListItem>();
            model.ActivityType = (Data.ActivityTypeEnum)Convert.ToInt32(HttpContext.Session.GetString("NonComplianceActivityType"));
            model.ActivityID = Convert.ToInt32(HttpContext.Session.GetString("NonComplianceActivityID"));

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                switch (model.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                    case ActivityTypeEnum.A09_Flag:
                        if ((from p in userSecureAreas
                             where p.UserID == user.Id
                             && p.SecureAreaID == (int)SecureAreaEnum.A09_Flags_CompanyReview
                             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                             select p).Count() == 0)
                            continue;
                        break;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                //var responsibleUser = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();
                //item.ResponsibleUserID = Request.Form["ResponsibleUser"];

                if (!string.IsNullOrEmpty(Request.Form["HasBeenResolved"]))
                    item.HasBeenResolved = Convert.ToBoolean(Request.Form["HasBeenResolved"]);
                else
                    item.HasBeenResolved = null;

                if (!string.IsNullOrEmpty(model.UserComment) && item.UserComment != model.UserComment)
                {
                    item.UserComment = model.UserComment;
                    item.UserCommentID = _userManager.GetUserId(User);
                    item.UserCommentDate = DateTime.Now;
                }

                item.ActivityID = model.ActivityID;
                item.ActivityTypeID = (int)model.ActivityType;

                if (!string.IsNullOrEmpty(model.EnforcerComment) && item.EnforcerComment != model.EnforcerComment)
                {
                    item.EnforcerComment = model.EnforcerComment;
                    item.EnforcerUserID = _userManager.GetUserId(User);
                    item.EnforcerCommentDate = DateTime.Now;
                }

                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;
            }



            return View("~/Views/Operational/Modules/EditNonCompliance.cshtml", model);
        }

    }
}
