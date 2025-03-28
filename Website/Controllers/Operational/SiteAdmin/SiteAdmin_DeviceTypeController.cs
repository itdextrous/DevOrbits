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
    public class SiteAdmin_DeviceTypesController : Controller
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

        public SiteAdmin_DeviceTypesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceTypes")]
        public async Task<IActionResult> SiteAdmin_DeviceTypess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_DeviceTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_DeviceTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var skybillResourceLists = db.SkybillResourceLists.ToList();

            SiteAdmin_DeviceTypesModel model = new SiteAdmin_DeviceTypesModel()
            {
                SiteAdmin_DeviceTypesItems = new List<SiteAdmin_DeviceTypesModel.SiteAdmin_DeviceTypesItem>(),
                BuildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList(),
            };

            var products = (from p in db.SiteAdmin_DeviceTypes
                            select p).ToList();

            foreach (var p in products)
            {
                SiteAdmin_DeviceTypesModel.SiteAdmin_DeviceTypesItem item = new SiteAdmin_DeviceTypesModel.SiteAdmin_DeviceTypesItem()
                {
                    ID = p.ID,
                    DeviceTypeID = p.DeviceTypeID,
                    CalibrationDiff = p.CalibrationDiff,
                };

                //if (!string.IsNullOrEmpty(p.CreatedByID))
                //{
                //    var cBy = opProfs.Where(c => c.UserID == p.CreatedByID).SingleOrDefault();
                //    if (cBy != null)
                //        item.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                //}

                //if (!string.IsNullOrEmpty(p.UpdatedByID))
                //{
                //    var uBy = opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault();
                //    if (uBy != null)
                //        item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                //}

                model.SiteAdmin_DeviceTypesItems.Add(item);
            }

            model.SiteAdmin_DeviceTypesItems = model.SiteAdmin_DeviceTypesItems.OrderBy(p => p.DeviceType.GetDescription()).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_DeviceTypes/SiteAdmin_DeviceTypes.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceTypes_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_DeviceTypes_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["deviceType_"].ToString())
                && !string.IsNullOrEmpty(Request.Form["calibrationDiff_"].ToString())
                )
            {
                try
                {
                    var productToEdit = (from p in db.SiteAdmin_DeviceTypes
                                         where p.DeviceTypeID == Convert.ToInt32(Request.Form["deviceType_"])
                                         select p).SingleOrDefault();

                    if (productToEdit != null)
                    {
                        productToEdit.CalibrationDiff = Convert.ToDecimal(Request.Form["calibrationDiff_"]);
                        db.Update(productToEdit);
                    }
                    else
                    {
                        productToEdit = new SiteAdmin_DeviceType()
                        {
                            DeviceTypeID = Convert.ToInt32(Request.Form["deviceType_"]),
                            CalibrationDiff = Convert.ToDecimal(Request.Form["calibrationDiff_"]),
                        };
                        db.Add(productToEdit);
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
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceTypes_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_DeviceTypes_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToRemove = (from p in db.SiteAdmin_DeviceTypes
                                       where p.ID == ID
                                       select p).SingleOrDefault();

                if (productToRemove != null)
                {
                    db.Remove(productToRemove);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_DeviceTypes_ItemUpdate/{ID}")]
        public async Task<IActionResult> SiteAdmin_DeviceTypes_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_DeviceTypes
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["deviceType_"].ToString()))
                        productToEdit.DeviceTypeID = Convert.ToInt32(Request.Form["deviceType_"]);
                    if (!string.IsNullOrEmpty(Request.Form["calibrationDiff_"].ToString()))
                        productToEdit.CalibrationDiff = Convert.ToDecimal(Request.Form["calibrationDiff_"]);

                    db.Update(productToEdit);
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
