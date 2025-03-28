using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C04_OperationalProfitReportModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.C04_OperationalProfitReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C04_OperationalProfitReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C04_OperationalProfitReportController(
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
            //_client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/C04_OperationalProfitReport/C04_OperationalProfitReport_Summary")]
        public async Task<IActionResult> C04_OperationalProfitReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C04_OperationalProfitReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C04_OperationalProfitReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C04_OperationalProfitReport_SummaryModel model = new C04_OperationalProfitReport_SummaryModel()
            {
                C04_OperationalProfitReport_SummaryItems = new List<C04_OperationalProfitReport_SummaryModel.C04_OperationalProfitReport_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                C04_OperationalProfitReport_SummaryModel.C04_OperationalProfitReport_SummaryItem item = new C04_OperationalProfitReport_SummaryModel.C04_OperationalProfitReport_SummaryItem()
                {
                    CompanyID = company.CompanyID,
                    Name = company.Name,
                    BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = company.BalanceMustBeAbove,
                    ExistsInSkybill = company.ExistsInSkybill,
                    Registrable = company.Registrable,
                    ServiceKey = company.ServiceKey,
                    C04_OperationalProfitReport_SummarySubItems = new List<C04_OperationalProfitReport_SummaryModel.C04_OperationalProfitReport_SummaryItem.C04_OperationalProfitReport_SummarySubItem>(),
                };


                foreach (var product in products)
                {
                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        decimal? amountProduct = null;
                        decimal? quantityProduct = null;

                        switch (product.SalesLink)
                        {
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                           where p.Month == current
                                                                           && p.ProductID == product.ID
                                                                           && p.CompanyID == company.CompanyID
                                                                           select p).SingleOrDefault();

                                if (report_ProductsResourceLedgerMonthy != null)
                                {
                                    amountProduct = report_ProductsResourceLedgerMonthy.Amount;
                                    quantityProduct = report_ProductsResourceLedgerMonthy.Quantity;
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                       where p.RentalMonth == current
                                                       && p.PropertyLinked == company.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == company.CompanyID
                                                                   select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_7191.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_7191.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8640.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_8640.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6610.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_6610.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8620.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_8620.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6811.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_6811.Quantity;
                                }

                                break;
                        }


                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        decimal? amountSupplyCost = null;
                        decimal? quantitySupplyCost = null;

                        switch (product.CostOfSalesLink)
                        {
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                                                                where p.Month == current
                                                                && p.ProductID == product.ID
                                                                && p.CompanyID == company.CompanyID
                                                                select p).SingleOrDefault();
                                if (report_SupplyCostMonthly != null)
                                {
                                    amountSupplyCost = report_SupplyCostMonthly.Amount;
                                    quantitySupplyCost = report_SupplyCostMonthly.Quantity;
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                       where p.RentalMonth == current
                                                       && p.PropertyLinked == company.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == company.CompanyID
                                                                   select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_7191.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_7191.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8640.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8640.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6610.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6610.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8620.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8620.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6811.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6811.Quantity;
                                }

                                break;
                        }

                        if (amountSupplyCost.HasValue)
                            amountSupplyCost = amountSupplyCost.Value * -1.0m;
                        if (quantitySupplyCost.HasValue)
                            quantitySupplyCost = quantitySupplyCost.Value * -1.0m;


                        if (item.C04_OperationalProfitReport_SummarySubItems.Where(p => p.Month == current).Count() > 0)
                        {
                            var itemToUpdate = item.C04_OperationalProfitReport_SummarySubItems.Where(p => p.Month == current).SingleOrDefault();
                            if (amountProduct.HasValue)
                                item.C04_OperationalProfitReport_SummarySubItems[item.C04_OperationalProfitReport_SummarySubItems.IndexOf(itemToUpdate)].TotalSales = item.C04_OperationalProfitReport_SummarySubItems[item.C04_OperationalProfitReport_SummarySubItems.IndexOf(itemToUpdate)].TotalSales + amountProduct.Value;
                            if (amountSupplyCost.HasValue)
                                item.C04_OperationalProfitReport_SummarySubItems[item.C04_OperationalProfitReport_SummarySubItems.IndexOf(itemToUpdate)].TotalCOS = item.C04_OperationalProfitReport_SummarySubItems[item.C04_OperationalProfitReport_SummarySubItems.IndexOf(itemToUpdate)].TotalCOS + amountSupplyCost.Value;
                        }
                        else
                            item.C04_OperationalProfitReport_SummarySubItems.Add(new C04_OperationalProfitReport_SummaryModel.C04_OperationalProfitReport_SummaryItem.C04_OperationalProfitReport_SummarySubItem()
                            {
                                Month = current,
                                TotalCOS = amountSupplyCost.HasValue ? amountSupplyCost.Value : 0,
                                TotalSales = amountProduct.HasValue ? amountProduct.Value : 0,
                            });

                        current = current.AddMonths(1);
                    }

                }

                model.C04_OperationalProfitReport_SummaryItems.Add(item);
            }

            return View("~/Views/Operational/C04_OperationalProfitReport/C04_OperationalProfitReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C04_OperationalProfitReport/C04_OperationalProfitReport_Details")]
        public async Task<IActionResult> C04_OperationalProfitReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C04_OperationalProfitReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C04_OperationalProfitReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C04_OperationalProfitReport_DetailsModel model = new C04_OperationalProfitReport_DetailsModel()
            {
                C04_OperationalProfitReport_DetailsProductItems = new List<C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem>(),
                C04_OperationalProfitReport_DetailsHeadOfficeFeeItems = new List<C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem>(),
                C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems = new List<C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem>(),
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                ProductIDs = new List<string>(),
                HideNoData = false,
                AvailCompanies = db.Companies.OrderBy(p => p.Name).ToList(),
                Companies = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                PartnerID = null,
                Partners = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "0", Text = "All Partners", Selected = string.IsNullOrEmpty(Request.Query["partner"]) },
                },
            };

            model.Partners.AddRange((from p in model.SiteAdmin_Partners
                                     orderby p.PartnerName
                                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                     {
                                         Value = p.ID.ToString(),
                                         Text = p.PartnerName,
                                         Selected = !string.IsNullOrEmpty(Request.Query["partner"]) && Request.Query["partner"].ToString() == p.ID.ToString(),
                                     }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["hideNoData"]))
                model.HideNoData = true;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["partner"]))
                model.PartnerID = Convert.ToInt32(Request.Query["partner"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["company"]))
                model.CompanyID = Request.Query["company"];

            if (model.PartnerID.HasValue)
            {
                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
                var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
                var companies = db.Companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == model.PartnerID.Value).ToList();
                if (!string.IsNullOrEmpty(Request.Query["company"]))
                    companies = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["company"])).ToList();

                List<C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem> c04_OperationalProfitReport_DetailsProductItems = new List<C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem>();

                foreach (var product in model.SiteAdmin_Products)
                {
                    if (model.ProductIDs.Count == 0 || model.ProductIDs.Contains(product.ID.ToString()))
                    { }
                    else
                        continue;

                    C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem productItem = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem()
                    {
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    };


                    C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem supplyCostItem = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem()
                    {
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    };

                    C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem grossAmountItem = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsProductItem()
                    {
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        decimal? amountProduct = null;
                        decimal? quantityProduct = null;

                        switch (product.SalesLink)
                        {
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                           where p.Month == current
                                                                           && p.ProductID == product.ID
                                                                           && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                           select p).ToList();

                                if (report_ProductsResourceLedgerMonthy.Count > 0)
                                {
                                    amountProduct = report_ProductsResourceLedgerMonthy.Select(p => p.Amount).Sum();
                                    quantityProduct = report_ProductsResourceLedgerMonthy.Select(p => p.Quantity).Sum();
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                       where p.RentalMonth == current
                                                       && companies.Select(c => c.Name).Contains(p.PropertyLinked)
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                           && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                   select p).ToList();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum();
                                    quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                }

                                break;
                        }


                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        productItem.MonthlyValues.Add(current, amountProduct);

                        decimal? amountSupplyCost = null;
                        decimal? quantitySupplyCost = null;

                        switch (product.CostOfSalesLink)
                        {
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                                                                where p.Month == current
                                                                && p.ProductID == product.ID
                                                                && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                select p).ToList();
                                if (report_SupplyCostMonthly != null)
                                {
                                    amountSupplyCost = report_SupplyCostMonthly.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_SupplyCostMonthly.Select(p => p.Quantity).Sum();
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                       where p.RentalMonth == current
                                                       && companies.Select(c => c.Name).Contains(p.PropertyLinked)
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                   && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                   select p).ToList();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && companies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum();
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                }

                                break;
                        }

                        if (amountSupplyCost.HasValue)
                            amountSupplyCost = amountSupplyCost.Value * -1.0m;
                        if (quantitySupplyCost.HasValue)
                            quantitySupplyCost = quantitySupplyCost.Value * -1.0m;

                        supplyCostItem.MonthlyValues.Add(current, amountSupplyCost);

                        decimal? amountGrossAmount = null;
                        if (amountProduct.HasValue || amountSupplyCost.HasValue)
                            amountGrossAmount = (amountProduct.HasValue ? amountProduct.Value : 0) + (amountSupplyCost.HasValue ? amountSupplyCost.Value : 0);

                        decimal? quantityGrossAmount = null;
                        if (quantityProduct.HasValue || quantitySupplyCost.HasValue)
                            quantityGrossAmount = (quantityProduct.HasValue ? quantityProduct.Value : 0) + (quantitySupplyCost.HasValue ? quantitySupplyCost.Value : 0);

                        grossAmountItem.MonthlyValues.Add(current, amountGrossAmount);

                        current = current.AddMonths(1);
                    }



                    if (product.ID == 7)
                    {
                        if (model.HideNoData)
                        {
                            if (productItem.MonthlyValues.Where(p => p.Value.HasValue).Count() != 0)
                            {
                                model.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems.Add(new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem()
                                {
                                    MonthlyValues = productItem.MonthlyValues,
                                    ProductID = productItem.ProductID,
                                    ProductName = productItem.ProductName,
                                    IsSalesItem = true,
                                });
                            }
                        }
                        else
                        {
                            model.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems.Add(new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem()
                            {
                                MonthlyValues = productItem.MonthlyValues,
                                ProductID = productItem.ProductID,
                                ProductName = productItem.ProductName,
                                IsSalesItem = true,
                            });
                        }

                        if (model.HideNoData)
                        {
                            if (supplyCostItem.MonthlyValues.Where(p => p.Value.HasValue).Count() != 0)
                            {
                                model.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems.Add(new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem()
                                {
                                    MonthlyValues = supplyCostItem.MonthlyValues,
                                    ProductID = supplyCostItem.ProductID,
                                    ProductName = supplyCostItem.ProductName,
                                    IsSalesItem = false,
                                });
                            }
                        }
                        else
                        {
                            model.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems.Add(new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem()
                            {
                                MonthlyValues = supplyCostItem.MonthlyValues,
                                ProductID = supplyCostItem.ProductID,
                                ProductName = supplyCostItem.ProductName,
                                IsSalesItem = false,
                            });
                        }
                    }
                    else
                    {
                        if (model.HideNoData)
                        {
                            if (grossAmountItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0)
                            {
                                continue;
                            }
                        }
                        model.C04_OperationalProfitReport_DetailsProductItems.Add(grossAmountItem);
                    }



                }

                #region Fees

                C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem()
                {
                    ProductName = "Bracket 1 (R0 to R100 000) - 40%",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2 = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem()
                {
                    ProductName = "Bracket 2 (R100 001 to R200 000) - 35%",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3 = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem()
                {
                    ProductName = "Bracket 3 (R200 001 to R300 000) - 30%",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4 = new C04_OperationalProfitReport_DetailsModel.C04_OperationalProfitReport_DetailsHeadOfficeFeeItem()
                {
                    ProductName = "Bracket 4 (R300 001 + ) - 25%",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };


                DateTime currentFee = model.FromDate;
                while (currentFee <= model.ToDate)
                {
                    decimal? amountProduct = null;

                    var productEntries = (from p in model.C04_OperationalProfitReport_DetailsProductItems
                                          where p.MonthlyValues.ContainsKey(currentFee)
                                          && p.MonthlyValues[currentFee].HasValue
                                          select p.MonthlyValues[currentFee]).ToList();

                    if (productEntries.Count > 0)
                    {
                        amountProduct = productEntries.Select(p => p.Value).Sum();
                    }

                    decimal amount1 = 0;
                    decimal amount2 = 0;
                    decimal amount3 = 0;
                    decimal amount4 = 0;
                    if (amountProduct.HasValue)
                    {
                        if (amountProduct.Value < 100000)
                            amount1 = amountProduct.Value * 0.4m;
                        else
                            amount1 = 100000.0m * 0.4m;

                        if (amountProduct.Value > 100000)
                        {
                            if (amountProduct.Value < 200000)
                                amount2 = (amountProduct.Value - 100000.0m) * 0.35m;
                            else
                                amount2 = 35000;
                        }

                        if (amountProduct.Value > 200000)
                        {
                            if (amountProduct.Value < 300000)
                                amount3 = (amountProduct.Value - 200000.0m) * 0.3m;
                            else
                                amount3 = 30000.0m;
                        }

                        if (amountProduct.Value > 300000.0m)
                            amount4 = (amountProduct.Value - 300000.0m) * 0.25m;
                    }

                    c04_OperationalProfitReport_DetailsHeadOfficeFeeItem.MonthlyValues.Add(currentFee, amount1);
                    c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2.MonthlyValues.Add(currentFee, amount2);
                    c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3.MonthlyValues.Add(currentFee, amount3);
                    c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4.MonthlyValues.Add(currentFee, amount4);

                    currentFee = currentFee.AddMonths(1);
                }

                model.C04_OperationalProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem);
                model.C04_OperationalProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2);
                model.C04_OperationalProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3);
                model.C04_OperationalProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4);

                #endregion


            }

            return View("~/Views/Operational/C04_OperationalProfitReport/C04_OperationalProfitReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C04_OperationalProfitReport/C04_OperationalPropertyProfitReport_Details")]
        public async Task<IActionResult> C04_OperationalPropertyProfitReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C04_OperationalProfitReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C04_OperationalProfitReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));

            C04_OperationalPropertyProfitReport_DetailsModel model = new C04_OperationalPropertyProfitReport_DetailsModel()
            {
                C04_OperationalPropertyProfitReport_DetailsProductItems = new List<C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsProductItem>(),
                C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems = new List<C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem>(),
                C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItems = new List<C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem>(),
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                ProductIDs = new List<string>(),
                HideNoData = false,
                AvailCompanies = db.Companies.OrderBy(p => p.Name).ToList(),
                Companies = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                PartnerID = null,
                Partners = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "0", Text = "All Partners", Selected = string.IsNullOrEmpty(Request.Query["partner"]) },
                },
            };

            model.Partners.AddRange((from p in model.SiteAdmin_Partners
                                     orderby p.PartnerName
                                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                     {
                                         Value = p.ID.ToString(),
                                         Text = p.PartnerName,
                                         Selected = !string.IsNullOrEmpty(Request.Query["partner"]) && Request.Query["partner"].ToString() == p.ID.ToString(),
                                     }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["hideNoData"]))
                model.HideNoData = true;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["partner"]))
                model.PartnerID = Convert.ToInt32(Request.Query["partner"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["company"]))
                model.CompanyID = Request.Query["company"];

            var companies = _operationalProvider.Companies.Where(p => _operationalProvider.UserCompanies.Select(c => c.CompanyID).Contains(p.CompanyID)).ToList();

            if (model.PartnerID.HasValue)
            {
                companies = db.Companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == model.PartnerID.Value).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["company"]))
                companies = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["company"])).ToList();

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();

            List<C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsProductItem> c04_OperationalProfitReport_DetailsProductItems = new List<C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsProductItem>();

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem()
            {
                IsSalesItem = true,
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
                ProductID = 7,
                ProductName = "Monthly Meter Rentals",
            };

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem()
            {
                IsSalesItem = false,
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
                ProductID = 7,
                ProductName = "Monthly Meter Rentals",
            };

            var rentalDataDumpsAll = (from p in db.RentalDataDumps
                                      where p.RentalMonth >= model.FromDate
                                      && p.RentalMonth <= model.ToDate
                                      select new
                                      {
                                          p.PropertyLinked,
                                          p.RentalMonth,
                                          p.AgreedMonthlyRentalExclVAT,
                                      }).ToList();

            foreach (var company in companies)
            {
                C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsProductItem grossAmountItem = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsProductItem()
                {
                    CompanyID = company.CompanyID,
                    Name = company.Name,
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                var rentalDataDumpsCompany = (from p in rentalDataDumpsAll
                                              where p.RentalMonth >= model.FromDate
                                       && p.RentalMonth <= model.ToDate
                                       && p.PropertyLinked == company.Name
                                              select p).ToList();

                foreach (var product in model.SiteAdmin_Products)
                {
                    //if (model.ProductIDs.Count == 0 || model.ProductIDs.Contains(product.ID.ToString()))
                    //{ }
                    //else
                    //    continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        decimal? amountProduct = null;
                        decimal? quantityProduct = null;

                        switch (product.SalesLink)
                        {
                            default:
                            case 0:
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                           where p.Month == current
                                                                           && p.ProductID == product.ID
                                                                           && p.CompanyID == company.CompanyID
                                                                           select p).SingleOrDefault();

                                if (report_ProductsResourceLedgerMonthy != null)
                                {
                                    amountProduct = report_ProductsResourceLedgerMonthy.Amount;
                                    quantityProduct = report_ProductsResourceLedgerMonthy.Quantity;
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in rentalDataDumpsCompany
                                                       where p.RentalMonth == current
                                                       && p.PropertyLinked == company.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == company.CompanyID
                                                                   select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_7191.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_7191.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8640.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_8640.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6610.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_6610.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8620.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_8620.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6811.Amount;
                                    quantityProduct = report_GeneralLedgerMonthly_6811.Quantity;
                                }

                                break;
                        }


                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        //productItem.MonthlyValues.Add(current, amountProduct);

                        decimal? amountSupplyCost = null;
                        decimal? quantitySupplyCost = null;

                        switch (product.CostOfSalesLink)
                        {
                            default:
                            case 0:
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                                                                where p.Month == current
                                                                && p.ProductID == product.ID
                                                                && p.CompanyID == company.CompanyID
                                                                select p).SingleOrDefault();
                                if (report_SupplyCostMonthly != null)
                                {
                                    amountSupplyCost = report_SupplyCostMonthly.Amount;
                                    quantitySupplyCost = report_SupplyCostMonthly.Quantity;
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in rentalDataDumpsCompany
                                                       where p.RentalMonth == current
                                                       && p.PropertyLinked == company.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month == current
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == company.CompanyID
                                                                   select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_7191 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_7191.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_7191.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8640 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8640.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8640.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6610 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6610.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6610.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_8620 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8620.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_8620.Quantity;
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month == current
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == company.CompanyID
                                                                        select p).SingleOrDefault();

                                if (report_GeneralLedgerMonthly_6811 != null)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6811.Amount;
                                    quantitySupplyCost = report_GeneralLedgerMonthly_6811.Quantity;
                                }

                                break;
                        }

                        if (amountSupplyCost.HasValue)
                            amountSupplyCost = amountSupplyCost.Value * -1.0m;
                        if (quantitySupplyCost.HasValue)
                            quantitySupplyCost = quantitySupplyCost.Value * -1.0m;

                        //supplyCostItem.MonthlyValues.Add(current, amountSupplyCost);

                        decimal amountGrossAmount = 0;
                        if (amountProduct.HasValue || amountSupplyCost.HasValue)
                            amountGrossAmount = (amountProduct.HasValue ? amountProduct.Value : 0) + (amountSupplyCost.HasValue ? amountSupplyCost.Value : 0);

                        decimal quantityGrossAmount = 0;
                        if (quantityProduct.HasValue || quantitySupplyCost.HasValue)
                            quantityGrossAmount = (quantityProduct.HasValue ? quantityProduct.Value : 0) + (quantitySupplyCost.HasValue ? quantitySupplyCost.Value : 0);


                        if (product.ID == 7 && amountSupplyCost.HasValue)
                        {
                            // skip c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS item
                        }
                        else
                        {
                            if (grossAmountItem.MonthlyValues.ContainsKey(current))
                                grossAmountItem.MonthlyValues[current] = grossAmountItem.MonthlyValues[current] + amountGrossAmount;
                            else
                                grossAmountItem.MonthlyValues.Add(current, amountGrossAmount);
                        }

                        if (product.ID == 7)
                        {
                            if (amountProduct.HasValue)
                            {
                                if (c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem.MonthlyValues.ContainsKey(current))
                                {
                                    c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem.MonthlyValues[current] = c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem.MonthlyValues[current] + amountProduct;
                                }
                                else
                                {
                                    c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem.MonthlyValues.Add(current, amountProduct);
                                }
                            }
                            if (amountSupplyCost.HasValue)
                            {
                                if (c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS.MonthlyValues.ContainsKey(current))
                                {
                                    c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS.MonthlyValues[current] = c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS.MonthlyValues[current] + amountSupplyCost;
                                }
                                else
                                {
                                    c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS.MonthlyValues.Add(current, amountSupplyCost);
                                }
                            }

                        }
                        current = current.AddMonths(1);
                    }
                }

                if (model.HideNoData)
                {
                    if (grossAmountItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0)
                    {
                        continue;
                    }
                }
                model.C04_OperationalPropertyProfitReport_DetailsProductItems.Add(grossAmountItem);
            }
            model.C04_OperationalPropertyProfitReport_DetailsProductItems = model.C04_OperationalPropertyProfitReport_DetailsProductItems.OrderBy(p => p.Name).ToList();
            model.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItems.Add(c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem);
            model.C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItems.Add(c04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItemCOS);

            #region Fees

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem()
            {
                ProductName = "Bracket 1 (R0 to R100 000) - 40%",
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
            };

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2 = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem()
            {
                ProductName = "Bracket 2 (R100 001 to R200 000) - 35%",
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
            };

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3 = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem()
            {
                ProductName = "Bracket 3 (R200 001 to R300 000) - 30%",
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
            };

            C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4 = new C04_OperationalPropertyProfitReport_DetailsModel.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem()
            {
                ProductName = "Bracket 4 (R300 001 + ) - 25%",
                MonthlyValues = new Dictionary<DateTime, decimal?>(),
            };


            DateTime currentFee = model.FromDate;
            while (currentFee <= model.ToDate)
            {
                decimal? amountProduct = null;

                var productEntries = (from p in model.C04_OperationalPropertyProfitReport_DetailsProductItems
                                      where p.MonthlyValues.ContainsKey(currentFee)
                                      && p.MonthlyValues[currentFee].HasValue
                                      select p.MonthlyValues[currentFee]).ToList();

                if (productEntries.Count > 0)
                {
                    amountProduct = productEntries.Select(p => p.Value).Sum();
                }

                decimal amount1 = 0;
                decimal amount2 = 0;
                decimal amount3 = 0;
                decimal amount4 = 0;
                if (amountProduct.HasValue)
                {
                    if (amountProduct.Value < 100000)
                        amount1 = amountProduct.Value * 0.4m;
                    else
                        amount1 = 100000.0m * 0.4m;

                    if (amountProduct.Value > 100000)
                    {
                        if (amountProduct.Value < 200000)
                            amount2 = (amountProduct.Value - 100000.0m) * 0.35m;
                        else
                            amount2 = 35000;
                    }

                    if (amountProduct.Value > 200000)
                    {
                        if (amountProduct.Value < 300000)
                            amount3 = (amountProduct.Value - 200000.0m) * 0.3m;
                        else
                            amount3 = 30000.0m;
                    }

                    if (amountProduct.Value > 300000.0m)
                        amount4 = (amountProduct.Value - 300000.0m) * 0.25m;
                }

                c04_OperationalProfitReport_DetailsHeadOfficeFeeItem.MonthlyValues.Add(currentFee, amount1);
                c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2.MonthlyValues.Add(currentFee, amount2);
                c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3.MonthlyValues.Add(currentFee, amount3);
                c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4.MonthlyValues.Add(currentFee, amount4);

                currentFee = currentFee.AddMonths(1);
            }

            model.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem);
            model.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem2);
            model.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem3);
            model.C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems.Add(c04_OperationalProfitReport_DetailsHeadOfficeFeeItem4);

            #endregion

            return View("~/Views/Operational/C04_OperationalProfitReport/C04_OperationalPropertyProfitReport_Details.cshtml", model);
        }

    }
}
