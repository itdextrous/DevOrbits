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
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_UsageToolCalcAssetsModels;
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
    public class SiteAdmin_UsageToolCalcAssetsController : Controller
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

        public SiteAdmin_UsageToolCalcAssetsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets")]
        public async Task<IActionResult> SiteAdmin_UsageToolCalcAssets()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            SiteAdmin_UsageToolCalcAssetsModel model = new SiteAdmin_UsageToolCalcAssetsModel()
            {
                UsageCalc_Assets = db.UsageCalc_Assets.ToList(),
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/SiteAdmin_UsageToolCalcAssets.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Add")]
        public async Task<IActionResult> SiteAdmin_UsageToolCalcAssets_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_UsageToolCalcAssetsAddModel model = new SiteAdmin_UsageToolCalcAssetsAddModel()
            {
                AssetType = (from p in ((AccountController.DeviceType[])Enum.GetValues(typeof(AccountController.DeviceType))).ToList()
                             where p != AccountController.DeviceType.Valve
                             select new SelectListItem()
                             {
                                 Text = p.GetDescription(),
                                 Value = ((int)p).ToString(),
                             }).ToList(),
                IsSuccess = false,
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Add")]
        public async Task<IActionResult> SiteAdmin_UsageToolCalcAssets_Add(SiteAdmin_UsageToolCalcAssetsAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.AssetType = (from p in ((AccountController.DeviceType[])Enum.GetValues(typeof(AccountController.DeviceType))).ToList()
                               where p != AccountController.DeviceType.Valve
                               select new SelectListItem()
                               {
                                   Text = p.GetDescription(),
                                   Value = ((int)p).ToString(),
                                   Selected = Request.Form["AssetType"] == ((int)p).ToString()
                               }).ToList();

            if (ModelState.IsValid)
            {
                var existing = db.UsageCalc_Assets.Where(p => p.AssetName == model.AssetName).SingleOrDefault();

                if (existing == null)
                {
                    UsageCalc_Asset asset = new UsageCalc_Asset()
                    {
                        AssetIcon = string.IsNullOrEmpty(model.AssetIcon) ? "" : model.AssetIcon,
                        AssetName = model.AssetName,
                        AverageKWH = model.AverageKWH,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        AssetTypeID = Convert.ToInt32(Request.Form["AssetType"]),
                        IsDeleted = false
                    };

                    db.UsageCalc_Assets.Add(asset);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    ModelState.AddModelError("AssetName", $"{model.AssetName} already exists");
                }

            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_UsageToolCalcAssets_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");

            SiteAdmin_UsageToolCalcAssetsEditModel model = new SiteAdmin_UsageToolCalcAssetsEditModel()
            {
                AssetType = (from p in ((AccountController.DeviceType[])Enum.GetValues(typeof(AccountController.DeviceType))).ToList()
                             where p != AccountController.DeviceType.Valve
                             select new SelectListItem()
                             {
                                 Text = p.GetDescription(),
                                 Value = ((int)p).ToString(),
                                 Selected = item.AssetTypeID == ((int)p)
                             }).ToList(),
                IsSuccess = false,
                AssetIcon = item.AssetIcon,
                AssetName = item.AssetName,
                AverageKWH = item.AverageKWH,
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_UsageToolCalcAssets_Edit(int ID, SiteAdmin_UsageToolCalcAssetsEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");

            model.AssetType = (from p in ((AccountController.DeviceType[])Enum.GetValues(typeof(AccountController.DeviceType))).ToList()
                               where p != AccountController.DeviceType.Valve
                               select new SelectListItem()
                               {
                                   Text = p.GetDescription(),
                                   Value = ((int)p).ToString(),
                                   Selected = Request.Form["AssetType"] == ((int)p).ToString()
                               }).ToList();

            if (ModelState.IsValid)
            {
                var itemToUpdate = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    var existing = (from p in db.UsageCalc_Assets
                                    where p.UsageCalc_AssetID != ID
                                    && p.AssetName == model.AssetName
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        itemToUpdate.AssetIcon = string.IsNullOrEmpty(model.AssetIcon) ? "lightbulb" : model.AssetIcon;
                        itemToUpdate.AssetName = model.AssetName;
                        itemToUpdate.AverageKWH = model.AverageKWH;
                        itemToUpdate.AssetTypeID = Convert.ToInt32(Request.Form["AssetType"]);

                        db.Update(itemToUpdate);
                        db.SaveChanges();
                    }
                    else
                    {
                        ModelState.AddModelError("AssetName", $"{model.AssetName} already exists");
                    }

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/delete/{ID}")]
        public IActionResult UsageCalcAssets_Delete(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var existing = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (existing == null)
                return Redirect("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");

            existing.IsDeleted = true;
            db.Update(existing);
            db.SaveChanges();


            return Redirect("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets/restore/{ID}")]
        public IActionResult UsageCalcAssets_Restore(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_UsageToolCalcAssets}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var existing = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (existing == null)
                return Redirect("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");

            existing.IsDeleted = false;
            db.Update(existing);
            db.SaveChanges();


            return Redirect("/operational/SiteAdmin/SiteAdmin_UsageToolCalcAssets");
        }

    }
}
