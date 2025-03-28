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
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_VehiclesModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [Authorize(Roles = "Operational")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_VehiclesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private IDeviceApi _client;

        public SiteAdmin_VehiclesController(IMemoryCache cache,
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
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles")]
        public async Task<IActionResult> SiteAdmin_Vehicles()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Vehicles, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Vehicles}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var vehicles = db.Vehicles.ToList();

            SiteAdmin_VehiclesModel model = new SiteAdmin_VehiclesModel()
            {
                SiteAdmin_VehiclesItems = new List<SiteAdmin_VehiclesModel.SiteAdmin_VehiclesItem>(),
            };

            bool doRedirect = false;

            foreach (var veh in vehicles)
            {
                var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == veh.RegularDriverID).SingleOrDefault();
                var opProfOwner = dbCache.OperationalProfiles.Where(p => p.UserID == veh.OwnerID).SingleOrDefault();
                SiteAdmin_VehiclesModel.SiteAdmin_VehiclesItem item = new SiteAdmin_VehiclesModel.SiteAdmin_VehiclesItem()
                {
                    ID = veh.ID,
                    DeviceIDLinked = veh.DeviceIDLinked,
                    M2MDevice = null,
                    RatePerKM = veh.RatePerKM,
                    RegistrationNumber = veh.RegistrationNumber,
                    RegularDriverID = veh.RegularDriverID,
                    VehicleTypeName = veh.VehicleTypeName,
                    RegularDriverUsername = !string.IsNullOrEmpty(veh.RegularDriverID) ? (opProfRegularDriver != null ? $"{opProfRegularDriver.FirstName} {opProfRegularDriver.LastName}" : _userManager.FindByIdAsync(veh.RegularDriverID).Result.UserName) : "Not Linked",
                    OwnerUsername = !string.IsNullOrEmpty(veh.OwnerID) ? (opProfOwner != null ? $"{opProfOwner.FirstName} {opProfOwner.LastName}" : _userManager.FindByIdAsync(veh.OwnerID).Result.UserName) : "Owned By Company",
                };

                var m2mDev = _client.GetDeviceByID(veh.DeviceIDLinked);
                if (m2mDev != null && m2mDev.device != null)
                {
                    if (string.IsNullOrEmpty(veh.RegistrationNumber))
                    {
                        var itemToUpdate = db.Vehicles.Where(p => p.ID == veh.ID).SingleOrDefault();
                        itemToUpdate.RegistrationNumber = m2mDev.device.serial;
                        db.Update(itemToUpdate);
                        db.SaveChanges();
                        doRedirect = true;
                    }

                    if (string.IsNullOrEmpty(veh.VehicleTypeName))
                    {
                        var itemToUpdate = db.Vehicles.Where(p => p.ID == veh.ID).SingleOrDefault();
                        itemToUpdate.VehicleTypeName = m2mDev.device.name;
                        db.Update(itemToUpdate);
                        db.SaveChanges();
                        doRedirect = true;
                    }

                    item.M2MDevice = m2mDev.device;
                }

                model.SiteAdmin_VehiclesItems.Add(item);
            }

            if (doRedirect)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_Vehicles");

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Vehicles/SiteAdmin_Vehicles.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles/SearchMeterID")]
        public JsonResult SiteAdmin_Vehicles_SearchMeterID(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var device = _client.GetDeviceByMeterNumber(Prefix);
            if (device != null)
            {
                string text = $"{device.id} ({device.serial} - {device.name})";

                results.Add(new
                {
                    Text = text,
                    Value = device.id.ToString()
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles_M2MDetails/{deviceIDLinked}")]
        public async Task<IActionResult> SiteAdmin_Vehicles_M2MDetails(int deviceIDLinked)
        {
            var device = _client.GetDeviceByID(deviceIDLinked);

            StringBuilder sbHtml = new StringBuilder();

            if (device != null && device.device != null)
            {
                sbHtml.AppendLine("<div class=\"row\">");
                sbHtml.AppendLine("    <div class=\"col-6\">Device ID</div>");
                sbHtml.AppendLine($"    <div class=\"col-6\">{device.device.id}</div>");
                sbHtml.AppendLine("</div>");
                sbHtml.AppendLine("<div class=\"row\">");
                sbHtml.AppendLine("    <div class=\"col-6\">Device Serial</div>");
                sbHtml.AppendLine($"    <div class=\"col-6\">{device.device.serial}</div>");
                sbHtml.AppendLine("</div>");
                sbHtml.AppendLine("<div class=\"row\">");
                sbHtml.AppendLine("    <div class=\"col-6\">Device Name</div>");
                sbHtml.AppendLine($"    <div class=\"col-6\">{device.device.name}</div>");
                sbHtml.AppendLine("</div>");
            }

            return Content(sbHtml.ToString(), "text/html");
        }



        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_Vehicles_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Vehicles, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Vehicles}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.Vehicles.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_Vehicles");

            SiteAdmin_VehiclesEditModel model = new SiteAdmin_VehiclesEditModel()
            {
                RegularDriverID = new List<SelectListItem>(),
                OwnerID = new List<SelectListItem>(),
                DeviceIDLinked = item.DeviceIDLinked,
                IsSuccess = false,
                RatePerKM = item.RatePerKM,
                RegistrationNumber = item.RegistrationNumber,
                Vehicle = item,
                VehicleTypeName = item.VehicleTypeName,
            };

            var m2mDev = _client.GetDeviceByID(item.DeviceIDLinked);
            if (m2mDev != null && m2mDev.device != null)
            {
                model.M2MDevice = m2mDev.device;
            }


            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            model.RegularDriverID.Add(new SelectListItem() { Value = "", Text = "[Not Linked]", Selected = string.IsNullOrEmpty(item.RegularDriverID) });
            model.OwnerID.Add(new SelectListItem() { Value = "", Text = "[Owned By Company]", Selected = string.IsNullOrEmpty(item.OwnerID) });

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.RegularDriverID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.RegularDriverID == user.Id ? true : false });
                model.OwnerID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.OwnerID == user.Id ? true : false });
            }

            model.RegularDriverID = model.RegularDriverID.OrderBy(p => p.Text).ToList();
            model.OwnerID = model.OwnerID.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Vehicles/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_Vehicles_Edit(int ID, SiteAdmin_VehiclesEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Vehicles, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Vehicles}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.Vehicles.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_Vehicles");

            model.RegularDriverID = new List<SelectListItem>();
            model.OwnerID = new List<SelectListItem>();
            model.Vehicle = item;

            var m2mDev = _client.GetDeviceByID(item.DeviceIDLinked);
            if (m2mDev != null && m2mDev.device != null)
            {
                model.M2MDevice = m2mDev.device;
            }
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            model.RegularDriverID.Add(new SelectListItem() { Value = "", Text = "Not Linked", Selected = string.IsNullOrEmpty(Request.Form["RegularDriverID"]) });
            model.OwnerID.Add(new SelectListItem() { Value = "", Text = "Owned By Company", Selected = string.IsNullOrEmpty(Request.Form["OwnerID"]) });

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.RegularDriverID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["RegularDriverID"] == user.Id ? true : false });
                model.OwnerID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["OwnerID"] == user.Id ? true : false });
            }

            if (ModelState.IsValid)
            {
                var alreadyExistingDevice = db.Vehicles.Where(p => p.DeviceIDLinked == model.DeviceIDLinked.Value && p.ID != item.ID).SingleOrDefault();
                if (alreadyExistingDevice != null)
                    ModelState.AddModelError("DeviceIDLinked", "This device is already linked to another vehicle.");
            }

            if (ModelState.IsValid)
            {
                var itemToUpdate = db.Vehicles.Where(p => p.ID == item.ID).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    itemToUpdate.DeviceIDLinked = model.DeviceIDLinked.Value;
                    itemToUpdate.OwnerID = Request.Form["OwnerID"];
                    itemToUpdate.RatePerKM = model.RatePerKM;
                    itemToUpdate.RegularDriverID = Request.Form["RegularDriverID"];
                    itemToUpdate.VehicleTypeName = model.VehicleTypeName;
                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Vehicles/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles/Add")]
        public async Task<IActionResult> SiteAdmin_Vehicles_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Vehicles, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Vehicles}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_VehiclesAddModel model = new SiteAdmin_VehiclesAddModel()
            {
                RegularDriverID = new List<SelectListItem>(),
                OwnerID = new List<SelectListItem>(),
                IsSuccess = false,
            };

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            model.RegularDriverID.Add(new SelectListItem() { Value = "", Text = "[Not Linked]" });
            model.OwnerID.Add(new SelectListItem() { Value = "", Text = "[Owned By Company]" });

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.RegularDriverID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
                model.OwnerID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }

            model.RegularDriverID = model.RegularDriverID.OrderBy(p => p.Text).ToList();
            model.OwnerID = model.OwnerID.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Vehicles/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Vehicles/Add")]
        public async Task<IActionResult> SiteAdmin_Vehicles_Add(SiteAdmin_VehiclesAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Vehicles, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Vehicles}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.RegularDriverID = new List<SelectListItem>();
            model.OwnerID = new List<SelectListItem>();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            model.RegularDriverID.Add(new SelectListItem() { Value = "", Text = "[Not Linked]", Selected = string.IsNullOrEmpty(Request.Form["RegularDriverID"]) });
            model.OwnerID.Add(new SelectListItem() { Value = "", Text = "[Owned By Company]", Selected = string.IsNullOrEmpty(Request.Form["OwnerID"]) });

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.RegularDriverID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["RegularDriverID"] == user.Id ? true : false });
                model.OwnerID.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["OwnerID"] == user.Id ? true : false });
            }
            model.RegularDriverID = model.RegularDriverID.OrderBy(p => p.Text).ToList();
            model.OwnerID = model.OwnerID.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var alreadyExistingDevice = db.Vehicles.Where(p => p.DeviceIDLinked == model.DeviceIDLinked.Value).SingleOrDefault();
                if (alreadyExistingDevice != null)
                    ModelState.AddModelError("DeviceIDLinked", "This device is already linked to another vehicle.");
            }

            if (ModelState.IsValid)
            {
                Data.Vehicle vehicle = new Vehicle()
                {
                    DeviceIDLinked = model.DeviceIDLinked.Value,
                    OwnerID = Request.Form["OwnerID"],
                    RatePerKM = model.RatePerKM,
                    RegularDriverID = Request.Form["RegularDriverID"],
                    VehicleTypeName = model.VehicleTypeName,
                };
                db.Add(vehicle);
                db.SaveChanges();

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Vehicles/Add.cshtml", model);
        }

    }
}
