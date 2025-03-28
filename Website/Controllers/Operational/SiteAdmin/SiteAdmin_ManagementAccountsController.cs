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
    public class SiteAdmin_ManagementAccountsController : Controller
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

        public SiteAdmin_ManagementAccountsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingCategories")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingCategories()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_ManagementAccounts_ReportingCategories, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_ManagementAccounts_ReportingCategories}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();

            SiteAdmin_ManagementAccounts_ReportingCategoriesModel model = new SiteAdmin_ManagementAccounts_ReportingCategoriesModel()
            {
                SiteAdmin_ManagementAccounts_ReportingCategoriesItems = new List<SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingCategoriesItem>(),
                SiteAdmin_ManagementAccounts_ReportingDescriptionsItems = new List<SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingDescriptionsItem>(),
                SiteAdmin_ManagementAccounts_ReportingParentDescriptionItems = new List<SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingParentDescriptionItem>(),
            };

            var reportingCategories = (from p in db.ManagementAccounts_ReportingCategories
                                       select p).ToList();

            foreach (var p in reportingCategories)
            {
                SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingCategoriesItem item = new SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingCategoriesItem()
                {
                    ID = p.ID,
                    ChartColor = p.ChartColor,
                    ChartType = p.ChartType,
                    FinancialCategoryID = p.FinancialCategoryID,
                    ReportingCategory = p.ReportingCategory,
                };

                model.SiteAdmin_ManagementAccounts_ReportingCategoriesItems.Add(item);
            }

            model.SiteAdmin_ManagementAccounts_ReportingCategoriesItems = model.SiteAdmin_ManagementAccounts_ReportingCategoriesItems.OrderBy(p => p.ReportingCategory).ToList();

            var reportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                         select p).ToList();

            var usedItems = db.ManagementAccountsDataDumps.GroupBy(info => info.ReportingDescriptionID)
                                    .Select(group => new
                                    {
                                        ReportingDescriptionID = group.Key,
                                        Count = group.Count()
                                    }).ToList();


            foreach (var p in reportingDescriptions)
            {
                var prod = products.Where(c => c.ProductName.ToUpper() == p.ReportingDescription.ToUpper()).FirstOrDefault();

                SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingDescriptionsItem item = new SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingDescriptionsItem()
                {
                    ID = p.ID,
                    ChartColor = p.ChartColor,
                    ChartType = p.ChartType,
                    FinancialCategoryID = p.FinancialCategoryID,
                    ReportingDescription = p.ReportingDescription,
                    ParentReportingDescriptionID = p.ParentReportingDescriptionID,
                    AllowDelete = usedItems.Where(c => c.ReportingDescriptionID == p.ID).Count() == 0,
                    ProductNameToSync = prod != null ? prod.ProductName : "[WILL NOT SYNC]",
                };

                model.SiteAdmin_ManagementAccounts_ReportingDescriptionsItems.Add(item);
            }

            var ReportingParentDescriptions = (from p in db.ManagementAccounts_ReportingParentDescriptions
                                               select p).ToList();

            foreach (var p in ReportingParentDescriptions)
            {
                SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingParentDescriptionItem item = new SiteAdmin_ManagementAccounts_ReportingCategoriesModel.SiteAdmin_ManagementAccounts_ReportingParentDescriptionItem()
                {
                    ID = p.ID,
                    ReportingParentDescription = p.ReportingParentDescription,
                    ChartColor = p.ChartColor,
                    ChartType = p.ChartType,
                };

                model.SiteAdmin_ManagementAccounts_ReportingParentDescriptionItems.Add(item);
            }

            model.SiteAdmin_ManagementAccounts_ReportingDescriptionsItems = model.SiteAdmin_ManagementAccounts_ReportingDescriptionsItems.OrderBy(p => p.ReportingDescription).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingCategories/SiteAdmin_ManagementAccounts_ReportingCategories.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingCategories_FinancialCategory_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingCategories_FinancialCategory_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingCategories
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["financialCategoryReportingCategory"]))
                {
                    productToEdit.FinancialCategoryID = Convert.ToInt32(Request.Form["financialCategoryReportingCategory"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingCategories_ChartColor_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingCategories_ChartColor_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingCategories
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["chartColorReportingCategory"]))
                {
                    productToEdit.ChartColor = Request.Form["chartColorReportingCategory"].ToString();
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingCategories_ChartType_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingCategories_ChartType_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingCategories
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["chartTypeReportingCategory"]))
                {
                    productToEdit.ChartType = Request.Form["chartTypeReportingCategory"].ToString();
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_Add")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_ReportingDescription_Update()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["parentReportingDescriptionReportingDescription"])
                    && !string.IsNullOrEmpty(Request.Form["reportingDescriptionReportingDescription"])
                    && !string.IsNullOrEmpty(Request.Form["financialCategoryReportingDescription"])
                    )
                {
                    var existing = (from p in db.ManagementAccounts_ReportingDescriptions
                                    where p.ReportingDescription == Request.Form["reportingDescriptionReportingDescription"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.ManagementAccounts_ReportingDescription managementAccounts_ReportingDescription = new ManagementAccounts_ReportingDescription()
                    {
                        ParentReportingDescriptionID = Convert.ToInt32(Request.Form["parentReportingDescriptionReportingDescription"].ToString()),
                        ReportingDescription = Request.Form["reportingDescriptionReportingDescription"].ToString(),
                        FinancialCategoryID = Convert.ToInt32(Request.Form["financialCategoryReportingDescription"].ToString()),
                        ChartType = Request.Form["chartTypeReportingDescription"].ToString(),
                        ChartColor = Request.Form["chartColorReportingDescription"].ToString(),

                    };
                    db.Add(managementAccounts_ReportingDescription);
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
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_FinancialCategory_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_FinancialCategory_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["financialCategoryReportingDescription"]))
                {
                    productToEdit.FinancialCategoryID = Convert.ToInt32(Request.Form["financialCategoryReportingDescription"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_ChartColor_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_ChartColor_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["chartColorReportingDescription"]))
                {
                    productToEdit.ChartColor = Request.Form["chartColorReportingDescription"].ToString();
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_ChartType_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_ChartType_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["chartTypeReportingDescription"]))
                {
                    productToEdit.ChartType = Request.Form["chartTypeReportingDescription"].ToString();
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_ParentReportingDescription_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_ParentReportingDescription_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["parentReportingDescriptionReportingDescription"]))
                {
                    productToEdit.ParentReportingDescriptionID = Convert.ToInt32(Request.Form["parentReportingDescriptionReportingDescription"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_ReportingDescription_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_ReportingDescription_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.ManagementAccounts_ReportingDescriptions
                                where p.ID != ID
                                && p.ReportingDescription == Request.Form["reportingDescriptionReportingDescription"].ToString()
                                select p).SingleOrDefault();

                if (existing != null)
                    return Content("false");

                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["reportingDescriptionReportingDescription"]))
                {
                    productToEdit.ReportingDescription = Request.Form["reportingDescriptionReportingDescription"].ToString();
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingDescriptions_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingDescriptions_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var usedItems = (from p in db.ManagementAccountsDataDumps
                                 where p.ReportingDescriptionID == ID
                                 select p.ID).Count();

                if (usedItems > 0)
                    return Content("false");

                var productToEdit = (from p in db.ManagementAccounts_ReportingDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    db.Remove(productToEdit);
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
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingParentDescriptions_Add")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingParentDescriptions_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.ManagementAccounts_ReportingParentDescriptions
                                where p.ReportingParentDescription == Request.Form["reportingParentDescription"].ToString()
                                select p).SingleOrDefault();
                if (existing != null)
                    return Content("false");

                if (!string.IsNullOrEmpty(Request.Form["reportingParentDescription"]))
                {
                    Data.ManagementAccounts_ReportingParentDescription managementAccounts_ReportingParentDescription = new ManagementAccounts_ReportingParentDescription()
                    {
                        ReportingParentDescription = Request.Form["reportingParentDescription"].ToString(),
                        ChartType = Request.Form["chartTypeReportingParentDescription"].ToString(),
                        ChartColor = Request.Form["chartColorReportingParentDescription"].ToString(),
                    };
                    db.Add(managementAccounts_ReportingParentDescription);
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
        [Route("/operational/SiteAdmin/SiteAdmin_ManagementAccounts_ReportingParentDescriptions_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ManagementAccounts_ReportingParentDescriptions_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.ManagementAccounts_ReportingParentDescriptions
                                where p.ID != ID
                                && p.ReportingParentDescription == Request.Form["reportingParentDescription"].ToString()
                                select p).SingleOrDefault();
                if (existing != null)
                    return Content("false");

                var productToEdit = (from p in db.ManagementAccounts_ReportingParentDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["reportingParentDescription"]))
                {
                    productToEdit.ReportingParentDescription = Request.Form["reportingParentDescription"].ToString();
                    productToEdit.ChartType = Request.Form["chartTypeReportingParentDescription_"].ToString();
                    productToEdit.ChartColor = Request.Form["chartColorReportingParentDescription_"].ToString();
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
