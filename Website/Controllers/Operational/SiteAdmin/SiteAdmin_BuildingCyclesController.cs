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
    public class SiteAdmin_BuildingCycles : Controller
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

        public SiteAdmin_BuildingCycles(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCycles")]
        public async Task<IActionResult> SiteAdmin_BuildingCycless()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingCycles, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingCycles}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            SiteAdmin_BuildingCyclesModel model = new SiteAdmin_BuildingCyclesModel()
            {
                SiteAdmin_BuildingCyclesItems = (from p in dbCache.BuildingCycles
                                                 select new SiteAdmin_BuildingCyclesModel.SiteAdmin_BuildingCyclesItem()
                                                 {
                                                     BuildingCouncilTypeID = p.BuildingCouncilTypeID,
                                                     BuildingCycleBillingDate = p.BuildingCycleBillingDate,
                                                     BuildingCycleCode = p.BuildingCycleCode,
                                                     BuildingCycleMonth = p.BuildingCycleMonth,
                                                     BuildingCycleReadingEndDate = p.BuildingCycleReadingEndDate,
                                                     BuildingCycleReadingStartDate = p.BuildingCycleReadingStartDate,
                                                     ID = p.ID,
                                                     UpdatedByUsername = "",
                                                     BuildingCouncilType = dbCache.BuildingCouncilTypes.Where(c => c.ID == p.BuildingCouncilTypeID).SingleOrDefault(),
                                                     LinkedItems = dbCache.BuildingCouncilDetails.Where(c => c.CouncilCycleID.HasValue && c.CouncilCycleID.Value == p.ID).Count(),
                                                 }).OrderByDescending(p => p.BuildingCycleBillingDate).ThenBy(p => p.BuildingCouncilTypeID).ToList(),
                BuildingCouncilTypes = dbCache.BuildingCouncilTypes,
            };

            model.SiteAdmin_BuildingCyclesItems = model.SiteAdmin_BuildingCyclesItems.OrderByDescending(p => p.BuildingCycleMonth).ThenBy(p => p.BuildingCouncilTypeID).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingCycles/SiteAdmin_BuildingCycles.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCycles_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_BuildingCycles_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_counciltype"])
                && !string.IsNullOrEmpty(Request.Form["add_code"])
                && !string.IsNullOrEmpty(Request.Form["add_month"])
                && !string.IsNullOrEmpty(Request.Form["add_readingstartdate"])
                && !string.IsNullOrEmpty(Request.Form["add_readingenddate"])
                && !string.IsNullOrEmpty(Request.Form["add_billingdate"])
                )
            {
                try
                {
                    var cycleToEdit = (from p in db.BuildingCycles
                                       where p.BuildingCouncilTypeID == Convert.ToInt32(Request.Form["add_counciltype"])
                                       && p.BuildingCycleCode == Request.Form["add_code"].ToString()
                                       && p.BuildingCycleMonth.Year == Convert.ToDateTime(Request.Form["add_month"]).Year
                                       && p.BuildingCycleMonth.Month == Convert.ToDateTime(Request.Form["add_month"]).Month
                                       select p).FirstOrDefault();

                    if (cycleToEdit != null)
                    {
                        cycleToEdit.BuildingCycleMonth = Convert.ToDateTime(Request.Form["add_month"]);
                        cycleToEdit.BuildingCycleReadingStartDate = Convert.ToDateTime(Request.Form["add_readingstartdate"]);
                        cycleToEdit.BuildingCycleReadingEndDate = Convert.ToDateTime(Request.Form["add_readingenddate"]);
                        cycleToEdit.BuildingCycleBillingDate = Convert.ToDateTime(Request.Form["add_billingdate"]);
                        cycleToEdit.UpdatedByID = _userManager.GetUserId(User);
                        cycleToEdit.UpdatedDate = DateTime.Now;
                        db.Update(cycleToEdit);
                    }
                    else
                    {
                        cycleToEdit = new BuildingCycle()
                        {
                            BuildingCouncilTypeID = Convert.ToInt32(Request.Form["add_counciltype"]),
                            BuildingCycleBillingDate = Convert.ToDateTime(Request.Form["add_billingdate"]),
                            BuildingCycleCode = Request.Form["add_code"].ToString(),
                            BuildingCycleMonth = Convert.ToDateTime(Request.Form["add_month"]),
                            BuildingCycleReadingStartDate = Convert.ToDateTime(Request.Form["add_readingstartdate"]),
                            BuildingCycleReadingEndDate = Convert.ToDateTime(Request.Form["add_readingenddate"]),
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                        };
                        db.Add(cycleToEdit);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_BuildingCycles);

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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingCycles_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_BuildingCycles_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var company_CostSetting_Item = (from p in db.BuildingCycles
                                                where p.ID == ID
                                                select p).SingleOrDefault();

                if (company_CostSetting_Item != null)
                {
                    db.Remove(company_CostSetting_Item);
                    db.SaveChanges();
                }

                _cache.Remove(MVCache.KEY_BuildingCycles);

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
