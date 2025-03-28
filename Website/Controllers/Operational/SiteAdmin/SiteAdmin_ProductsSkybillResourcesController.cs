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
    public class SiteAdmin_ProductsSkybillResourcesSkybillResourcesController : Controller
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

        public SiteAdmin_ProductsSkybillResourcesSkybillResourcesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_ProductsSkybillResources")]
        public async Task<IActionResult> SiteAdmin_ProductsSkybillResourcess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_ProductsSkybillResources, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_ProductsSkybillResources}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_ProductsSkybillResourcesModel model = new SiteAdmin_ProductsSkybillResourcesModel()
            {
                SiteAdmin_ProductsSkybillResourcesItems = new List<SiteAdmin_ProductsSkybillResourcesModel.SiteAdmin_ProductsSkybillResourcesItem>(),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
            };

            var resources = (from p in db.SkybillResourceLists
                             select p).ToList();

            foreach (var r in resources)
            {
                SiteAdmin_ProductsSkybillResourcesModel.SiteAdmin_ProductsSkybillResourcesItem item = new SiteAdmin_ProductsSkybillResourcesModel.SiteAdmin_ProductsSkybillResourcesItem()
                {
                    Base_Unit_Of_Measure = r.Base_Unit_Of_Measure,
                    CompanyID = r.CompanyID,
                    County = r.County,
                    DateUpdated = r.DateUpdated,
                    Default_Deferral_Template_Code = r.Default_Deferral_Template_Code,
                    Direct_Unit_Cost = r.Direct_Unit_Cost,
                    ETag = r.ETag,
                    Gen_Prod_Posting_Group = r.Gen_Prod_Posting_Group,
                    ID = r.ID,
                    Indirect_Cost_Percent = r.Indirect_Cost_Percent,
                    Name = r.Name,
                    No = r.No,
                    Price_Profit_Calculation = r.Price_Profit_Calculation,
                    ProductID = r.ProductID,
                    Profit_Percent = r.Profit_Percent,
                    Resource_Group_No = r.Resource_Group_No,
                    Search_Name = r.Search_Name,
                    Type = r.Type,
                    Unit_Cost = r.Unit_Cost,
                    Unit_Price = r.Unit_Price,
                    UpdatedByID = r.UpdatedByID,
                    UpdatedByUsername = "",
                    Usage_Calculation_Type = r.Usage_Calculation_Type,
                    VAT_Prod_Posting_Group = r.VAT_Prod_Posting_Group,
                    CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == r.CompanyID).SingleOrDefault().Name,
                    ExcludeUnitsFromBilling = r.ExcludeUnitsFromBilling,
                };

                if (!string.IsNullOrEmpty(r.UpdatedByID))
                {
                    var uBy = opProfs.Where(c => c.UserID == r.UpdatedByID).SingleOrDefault();
                    if (uBy != null)
                        item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                }

                model.SiteAdmin_ProductsSkybillResourcesItems.Add(item);
            }

            model.SiteAdmin_ProductsSkybillResourcesItems = model.SiteAdmin_ProductsSkybillResourcesItems.OrderBy(p => p.ProductID.HasValue).ThenBy(p => p.CompanyName).ThenBy(p => p.ProductID).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_ProductsSkybillResources/SiteAdmin_ProductsSkybillResources.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ProductsSkybillResources_ItemUpdate/{ID}")]
        public async Task<IActionResult> SiteAdmin_ProductsSkybillResources_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var resourceToUpdate = (from p in db.SkybillResourceLists
                                        where p.ID == ID
                                        select p).SingleOrDefault();

                if (resourceToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["productid"]))
                    {
                        resourceToUpdate.ProductID = Convert.ToInt32(Request.Form["productid"]);
                        resourceToUpdate.UpdatedByID = _userManager.GetUserId(User);
                        resourceToUpdate.DateUpdated = DateTime.Now;

                        db.Update(resourceToUpdate);
                        db.SaveChanges();
                    }
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
        [Route("/operational/SiteAdmin/SiteAdmin_ProductsSkybillResources_ItemUpdateExcludeUnitsFromBilling/{ID}")]
        public async Task<IActionResult> SiteAdmin_ProductsSkybillResources_ItemUpdateExcludeUnitsFromBilling(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var resourceToUpdate = (from p in db.SkybillResourceLists
                                        where p.ID == ID
                                        select p).SingleOrDefault();

                if (resourceToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["productid"]))
                    {
                        resourceToUpdate.ExcludeUnitsFromBilling = Convert.ToBoolean(Request.Form["productid"]);
                        resourceToUpdate.UpdatedByID = _userManager.GetUserId(User);
                        resourceToUpdate.DateUpdated = DateTime.Now;

                        db.Update(resourceToUpdate);
                        db.SaveChanges();
                    }
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
