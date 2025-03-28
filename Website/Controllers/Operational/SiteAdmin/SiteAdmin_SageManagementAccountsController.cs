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
    public class SiteAdmin_SageManagementAccountsController : Controller
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

        public SiteAdmin_SageManagementAccountsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_SageManagementAccounts_ReportingCategories")]
        public async Task<IActionResult> SiteAdmin_SageManagementAccounts_ReportingCategories()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SageManagementAccounts_ReportingCategories, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SageManagementAccounts_ReportingCategories}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();

            SiteAdmin_SageManagementAccounts_ReportingCategoriesModel model = new SiteAdmin_SageManagementAccounts_ReportingCategoriesModel()
            {
                SiteAdmin_SageManagementAccounts_ReportingCategoriesItems = new List<SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingCategoriesItem>(),
                SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItems = new List<SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItem>(),
                SiteAdmin_SageManagementAccounts_SageAccounting_Accounts = new List<SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_SageAccounting_Account>(),
            };

            #region SageAccounting_AccountCategories

            var reportingCategories = (from p in db.SageAccounting_AccountCategories
                                       select p).ToList();

            foreach (var p in reportingCategories)
            {
                SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingCategoriesItem item = new SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingCategoriesItem()
                {
                    ID = p.ID,
                    Comment = p.Comment,
                    Description = p.Description,
                    IsBalanceSheet = p.IsBalanceSheet,
                    Order = p.Order,
                    SageID = p.SageID,
                };

                model.SiteAdmin_SageManagementAccounts_ReportingCategoriesItems.Add(item);
            }

            model.SiteAdmin_SageManagementAccounts_ReportingCategoriesItems = model.SiteAdmin_SageManagementAccounts_ReportingCategoriesItems.OrderBy(p => p.Order).ToList();

            #endregion

            #region SageManagementAccounts_ReportingParentDescriptions

            var ReportingParentDescriptions = (from p in db.SageManagementAccounts_ReportingParentDescriptions
                                               select p).ToList();

            foreach (var p in ReportingParentDescriptions)
            {
                SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItem item = new SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItem()
                {
                    ID = p.ID,
                    ReportingParentDescription = p.ReportingParentDescription,
                    ChartColor = p.ChartColor,
                    ChartType = p.ChartType,
                    ReportingCategoryID = p.ReportingCategoryID,
                };

                model.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItems.Add(item);
            }
            model.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItems = model.SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItems.OrderBy(p => p.ReportingParentDescription).ToList();

            #endregion

            #region SageAccounting_Accounts

            var SageAccounting_Accounts = (from p in db.SageAccounting_Accounts
                                           select p).ToList();
            var sageAccounting_AccountTaxTypes = db.SageAccounting_AccountTaxTypes.ToList();
            var sageAccounting_AccountCategories = db.SageAccounting_AccountCategories.ToList();
            var sageAccounting_Companies = db.SageAccounting_Companies.ToList();

            foreach (var p in SageAccounting_Accounts)
            {
                string AccountTypeName = "";
                string CategoryName = "";
                if (p.Category.HasValue)
                {
                    var sageAccounting_AccountCategory = sageAccounting_AccountCategories.Where(c => c.SageID == p.Category).FirstOrDefault();
                    if (sageAccounting_AccountCategory != null)
                        CategoryName = sageAccounting_AccountCategory.Description;
                }
                string CompanyName = "";
                if (p.CompanyId.HasValue)
                {
                    var sageAccounting_Company = sageAccounting_Companies.Where(c => c.SageID == p.CompanyId).FirstOrDefault();
                    if (sageAccounting_Company != null)
                        CompanyName = sageAccounting_Company.Name;
                }
                string TaxTypeName = "";
                if (p.DefaultTaxType.HasValue)
                {
                    var sageAccounting_AccountTaxType = sageAccounting_AccountTaxTypes.Where(c => c.SageID == p.DefaultTaxType).FirstOrDefault();
                    if (sageAccounting_AccountTaxType != null)
                        TaxTypeName = sageAccounting_AccountTaxType.Name;
                }

                SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_SageAccounting_Account item = new SiteAdmin_SageManagementAccounts_ReportingCategoriesModel.SiteAdmin_SageManagementAccounts_SageAccounting_Account()
                {
                    ID = p.ID,
                    AccountType = p.AccountType,
                    Active = p.Active,
                    Balance = p.Balance,
                    Category = p.Category,
                    CompanyId = p.CompanyId,
                    Created = p.Created,
                    DefaultTaxType = p.DefaultTaxType,
                    DefaultTaxTypeId = p.DefaultTaxType,
                    Description = p.Description,
                    HasActivity = p.HasActivity,
                    IsTaxLocked = p.IsTaxLocked,
                    Name = p.Name,
                    ReportingParentDescriptionID = p.ReportingParentDescriptionID,
                    SageID = p.SageID,
                    UnallocatedAccount = p.UnallocatedAccount,
                    AccountTypeName = AccountTypeName,
                    CategoryName = CategoryName,
                    CompanyName = CompanyName,
                    TaxTypeName = TaxTypeName,
                };

                model.SiteAdmin_SageManagementAccounts_SageAccounting_Accounts.Add(item);
            }
            model.SiteAdmin_SageManagementAccounts_SageAccounting_Accounts = model.SiteAdmin_SageManagementAccounts_SageAccounting_Accounts.OrderBy(p => p.CompanyName).ThenBy(p => p.CategoryName).ThenBy(p => p.ReportingParentDescriptionID).ThenBy(p => p.Name).ToList();

            #endregion

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SageManagementAccounts_ReportingCategories/SiteAdmin_SageManagementAccounts_ReportingCategories.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_SageManagementAccounts_ReportingParentDescriptions_Add")]
        public async Task<IActionResult> SiteAdmin_SageManagementAccounts_ReportingParentDescriptions_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.SageManagementAccounts_ReportingParentDescriptions
                                where p.ReportingParentDescription == Request.Form["reportingParentDescription"].ToString()
                                select p).SingleOrDefault();
                if (existing != null)
                    return Content("false");

                if (!string.IsNullOrEmpty(Request.Form["reportingParentDescription"]))
                {
                    Data.SageManagementAccounts_ReportingParentDescription SageManagementAccounts_ReportingParentDescription = new SageManagementAccounts_ReportingParentDescription()
                    {
                        ReportingParentDescription = Request.Form["reportingParentDescription"].ToString(),
                        ChartType = Request.Form["chartTypeReportingParentDescription"].ToString(),
                        ChartColor = Request.Form["chartColorReportingParentDescription"].ToString(),
                        ReportingCategoryID = Convert.ToInt32(Request.Form["categoryIDParentDescription"]),
                    };
                    db.Add(SageManagementAccounts_ReportingParentDescription);
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
        [Route("/operational/SiteAdmin/SiteAdmin_SageManagementAccounts_ReportingParentDescriptions_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_SageManagementAccounts_ReportingParentDescriptions_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.SageManagementAccounts_ReportingParentDescriptions
                                where p.ID != ID
                                && p.ReportingParentDescription == Request.Form["reportingParentDescription"].ToString()
                                select p).SingleOrDefault();
                if (existing != null)
                    return Content("false");

                var productToEdit = (from p in db.SageManagementAccounts_ReportingParentDescriptions
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["reportingParentDescription"]))
                {
                    productToEdit.ReportingParentDescription = Request.Form["reportingParentDescription"].ToString();
                    productToEdit.ChartType = Request.Form["chartTypeReportingParentDescription_"].ToString();
                    productToEdit.ChartColor = Request.Form["chartColorReportingParentDescription_"].ToString();
                    productToEdit.ReportingCategoryID = Convert.ToInt32(Request.Form["categoryIDParentDescription"]);
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
        [Route("/operational/SiteAdmin/SiteAdmin_SageManagementAccounts_SageAccount_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_SageManagementAccounts_SageAccount_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SageAccounting_Accounts
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["sageAccountReportingParentDescriptionID"]))
                {
                    productToEdit.ReportingParentDescriptionID = Convert.ToInt32(Request.Form["sageAccountReportingParentDescriptionID"]);
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
