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
    public class SiteAdmin_BuildingCouncilTypes : Controller
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

        public SiteAdmin_BuildingCouncilTypes(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCouncilTypes")]
        public async Task<IActionResult> SiteAdmin_BuildingCouncilTypess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingCouncilTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingCouncilTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            SiteAdmin_BuildingCouncilTypesModel model = new SiteAdmin_BuildingCouncilTypesModel()
            {
                SiteAdmin_BuildingCouncilTypesItems = (from p in dbCache.BuildingCouncilTypes
                                                 select new SiteAdmin_BuildingCouncilTypesModel.SiteAdmin_BuildingCouncilTypesItem()
                                                 {
                                                     BuildingCouncilTypeCode = p.BuildingCouncilTypeCode,
                                                     BuildingCouncilTypeName = p.BuildingCouncilTypeName,
                                                     ID = p.ID,
                                                     UpdatedByUsername = "",
                                                     LinkedItems = (
                                                     dbCache.BuildingCouncilDetails.Where(c => c.CouncilTypeID.HasValue && c.CouncilTypeID.Value == p.ID).Count()
                                                     +
                                                     dbCache.BuildingCycles.Where(c => c.BuildingCouncilTypeID == p.ID).Count()
                                                     )
                                                     ,
                                                 }).OrderByDescending(p => p.BuildingCouncilTypeCode).ThenBy(p => p.BuildingCouncilTypeName).ToList(),
                BuildingCouncilTypes = dbCache.BuildingCouncilTypes,
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingCouncilTypes/SiteAdmin_BuildingCouncilTypes.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCouncilTypes_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_BuildingCouncilTypes_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_name"])
                && !string.IsNullOrEmpty(Request.Form["add_code"])
                )
            {
                try
                {
                    var cycleToEdit = (from p in db.BuildingCouncilTypes
                                       where p.BuildingCouncilTypeCode == Request.Form["add_code"]
                                       select p).SingleOrDefault();

                    if (cycleToEdit != null)
                    {
                        cycleToEdit.BuildingCouncilTypeName = Request.Form["add_name"];
                        cycleToEdit.UpdatedByID = _userManager.GetUserId(User);
                        cycleToEdit.UpdatedDate = DateTime.Now;
                        db.Update(cycleToEdit);
                    }
                    else
                    {
                        cycleToEdit = new BuildingCouncilType()
                        {
                            BuildingCouncilTypeName = Request.Form["add_name"],
                            BuildingCouncilTypeCode = Request.Form["add_code"],
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                        };
                        db.Add(cycleToEdit);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_BuildingCouncilTypes);

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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCouncilTypes_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_BuildingCouncilTypes_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var company_CostSetting_Item = (from p in db.BuildingCouncilTypes
                                                where p.ID == ID
                                                select p).SingleOrDefault();

                if (company_CostSetting_Item != null)
                {
                    db.Remove(company_CostSetting_Item);
                    db.SaveChanges();
                }

                _cache.Remove(MVCache.KEY_BuildingCouncilTypes);

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
