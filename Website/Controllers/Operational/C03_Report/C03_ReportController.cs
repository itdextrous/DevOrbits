using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C03_ReportModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.C01_ProductReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C03_ReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C03_ReportController(
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
        [Route("/operational/C03_Report/C03_AverageAndPerUnitReport_Summary")]
        public async Task<IActionResult> C03_AverageAndPerUnitReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_AverageAndPerUnitReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_AverageAndPerUnitReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            C03_AverageAndPerUnitReport_SummaryModel model = new C03_AverageAndPerUnitReport_SummaryModel()
            {
                C03_AverageAndPerUnitReport_SummaryItems = new List<C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem>(),
                Partner = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "[All Partners VERY SLOW]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
            };

            var partners = db.SiteAdmin_Partners.ToList();
            model.Partner.AddRange(
                (from p in partners
                 orderby p.PartnerName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Text = p.PartnerName,
                     Value = p.ID.ToString(),
                     Selected = _operationalProvider.PartnerID == p.ID,
                 }).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (model.FromDate.HasValue && model.ToDate.HasValue)
            {
                decimal months = 0;
                DateTime current = model.FromDate.Value;
                while (current <= model.ToDate.Value)
                {
                    months++;
                    current = current.AddMonths(1);
                }
                var companies = db.Companies.ToList();
                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
                var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
                var products = db.SiteAdmin_Products.ToList();

                foreach (var uC in _operationalProvider.UserCompanies)
                {
                    var c = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                    if (_operationalProvider.PartnerID != 0)
                    {
                        if (!c.PartnerID.HasValue || c.PartnerID.Value != _operationalProvider.PartnerID)
                            continue;
                    }
                    var allProducts_Total = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = 0,
                        Sales = 0,
                    };
                    var combinedTotal_Total = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = 0,
                        Sales = 0,
                    };
                    var meterCharges_Total = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = 0,
                        Sales = 0,
                    };

                    #region Calculate values

                    foreach (var product in products)
                    {
                        decimal amountProduct = 0;
                        //decimal quantityProduct = 0;
                        List<KeyValuePair<DateTime, decimal>> allAmountProducts = new List<KeyValuePair<DateTime, decimal>>();
                        //List<KeyValuePair<DateTime, decimal>> allQuantityProduct = new List<KeyValuePair<DateTime, decimal>>();

                        switch (product.SalesLink)
                        {
                            default:
                            case 0:
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                           where p.Month >= model.FromDate.Value
                                                                           && p.Month <= model.ToDate.Value
                                                                           && p.ProductID == product.ID
                                                                           && p.CompanyID == c.CompanyID
                                                                           select p).ToList();

                                if (report_ProductsResourceLedgerMonthy != null && report_ProductsResourceLedgerMonthy.Count > 0)
                                {
                                    amountProduct = report_ProductsResourceLedgerMonthy.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_ProductsResourceLedgerMonthy.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_ProductsResourceLedgerMonthy
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                    //allQuantityProduct  = (from p in report_ProductsResourceLedgerMonthy 
                                    //                      select new KeyValuePair<DateTime, decimal>(p.Month, p.Quantity)).ToList();
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in db.RentalDataDumps
                                                       where p.RentalMonth >= model.FromDate.Value
                                                       && p.RentalMonth <= model.ToDate.Value
                                                       && p.PropertyLinked == c.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                    allAmountProducts = (from p in rentalDataDumps
                                                         select new KeyValuePair<DateTime, decimal>(p.RentalMonth, p.AgreedMonthlyRentalExclVAT)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month >= model.FromDate.Value
                                                                   && p.Month <= model.ToDate.Value
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == c.CompanyID
                                                                   select p).ToList();

                                if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly_7191
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly_8640
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly_6610
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly_8620
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                {
                                    amountProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum();
                                    //quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                    allAmountProducts = (from p in report_GeneralLedgerMonthly_6811
                                                         select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                        }

                        amountProduct = amountProduct * -1.0m;
                        //quantityProduct = quantityProduct * -1.0m;

                        decimal amountSupplyCost = 0;
                        //decimal quantitySupplyCost = 0;
                        List<KeyValuePair<DateTime, decimal>> allAmountSupplyCosts = new List<KeyValuePair<DateTime, decimal>>();

                        switch (product.CostOfSalesLink)
                        {
                            default:
                            case 0:
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                                                                where p.Month >= model.FromDate.Value
                                                                && p.Month <= model.ToDate.Value
                                                                && p.ProductID == product.ID
                                                                && p.CompanyID == c.CompanyID
                                                                select p).ToList();
                                if (report_SupplyCostMonthly != null && report_SupplyCostMonthly.Count > 0)
                                {
                                    amountSupplyCost = report_SupplyCostMonthly.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_SupplyCostMonthly.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_SupplyCostMonthly
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                var rentalDataDumps = (from p in db.RentalDataDumps
                                                       where p.RentalMonth >= model.FromDate.Value
                                                       && p.RentalMonth <= model.ToDate.Value
                                                       && p.PropertyLinked == c.Name
                                                       select p).ToList();

                                if (rentalDataDumps.Count > 0)
                                {
                                    amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                    var q = from i in rentalDataDumps
                                            group i by i.RentalMonth into grp
                                            select new { RentalMonth = grp.Key, AgreedMonthlyRentalExclVAT = grp.Sum(i => i.AgreedMonthlyRentalExclVAT) };

                                    allAmountSupplyCosts = (from p in q
                                                            select new KeyValuePair<DateTime, decimal>(p.RentalMonth, p.AgreedMonthlyRentalExclVAT)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                   where p.Month >= model.FromDate.Value
                                                                   && p.Month <= model.ToDate.Value
                                                                   && p.GenLedgerNo == 6810
                                                                   && p.CompanyID == c.CompanyID
                                                                   select p).ToList();

                                if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 7191
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly_7191
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 8640
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly_8640
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 6610
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly_6610
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 8620
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly_8620
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                        where p.Month >= model.FromDate.Value
                                                                        && p.Month <= model.ToDate.Value
                                                                        && p.GenLedgerNo == 6811
                                                                        && p.CompanyID == c.CompanyID
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                {
                                    amountSupplyCost = report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum();
                                    //quantitySupplyCost = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                    allAmountSupplyCosts = (from p in report_GeneralLedgerMonthly_6811
                                                            select new KeyValuePair<DateTime, decimal>(p.Month, p.Amount)).ToList();
                                }

                                break;
                        }

                        amountSupplyCost = amountSupplyCost * -1.0m;
                        //quantitySupplyCost = quantitySupplyCost * -1.0m;

                        if (product.ID != 7)
                        {
                            allProducts_Total.Sales += (amountProduct / months);
                            allProducts_Total.CostOfSales += (amountSupplyCost / months);
                        }

                        List<decimal> meterCharges_Total_Sales = new List<decimal>();
                        List<decimal> meterCharges_Total_CostOfSales = new List<decimal>();
                        if (product.ID == 7)
                        {
                            foreach (var kvp in allAmountProducts)
                            {
                                meterCharges_Total_Sales.Add(((kvp.Value * -1.0m) /*/ Convert.ToDecimal(c.NoOfRegisteredUnits.Value)*/));
                            }

                            foreach (var kvp in allAmountSupplyCosts)
                            {
                                meterCharges_Total_CostOfSales.Add(((kvp.Value * -1.0m) /*/ Convert.ToDecimal(c.NoOfRegisteredUnits.Value)*/));
                            }

                            if (meterCharges_Total_Sales.Count > 0)
                                meterCharges_Total.Sales += meterCharges_Total_Sales.Average();
                            if (meterCharges_Total_CostOfSales.Count > 0)
                                meterCharges_Total.CostOfSales += meterCharges_Total_CostOfSales.Average();

                        }
                    }

                    combinedTotal_Total.Sales = allProducts_Total.Sales + meterCharges_Total.Sales;
                    combinedTotal_Total.CostOfSales = allProducts_Total.CostOfSales + meterCharges_Total.CostOfSales;

                    var allProducts_PerUnit = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? allProducts_Total.CostOfSales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                        Sales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? allProducts_Total.Sales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                    };
                    var combinedTotal_PerUnit = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? combinedTotal_Total.CostOfSales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                        Sales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? combinedTotal_Total.Sales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                    };
                    var meterCharges_PerUnit = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem.C03_AverageAndPerUnitReport_SummaryItemSubItem()
                    {
                        CostOfSales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? meterCharges_Total.CostOfSales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                        Sales = c.NoOfRegisteredUnits.HasValue && c.NoOfRegisteredUnits.Value != 0 ? meterCharges_Total.Sales / Convert.ToDecimal(c.NoOfRegisteredUnits) : 0,
                    };

                    #endregion

                    C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem item = new C03_AverageAndPerUnitReport_SummaryModel.C03_AverageAndPerUnitReport_SummaryItem()
                    {
                        CompanyID = c.CompanyID,
                        ActionID = c.ActionID,
                        AllProducts_PerUnit = allProducts_PerUnit,
                        AllProducts_Total = allProducts_Total,
                        BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = c.BalanceMustBeAbove,
                        Batch = c.Batch,
                        CombinedTotal_PerUnit = combinedTotal_PerUnit,
                        CombinedTotal_Total = combinedTotal_Total,
                        ConvFactor = c.ConvFactor,
                        Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
                        Division_VBAK_SPART = c.Division_VBAK_SPART,
                        ExistsInSkybill = c.ExistsInSkybill,
                        IsCeilingActiveOnMidnightSync = c.IsCeilingActiveOnMidnightSync,
                        IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
                        IsFlagStatusActive = c.IsFlagStatusActive,
                        ItemID = c.ItemID,
                        MasterServiceKey = c.MasterServiceKey,
                        MeterCharges_PerUnit = meterCharges_PerUnit,
                        MeterCharges_Total = meterCharges_Total,
                        Name = c.Name,
                        NetcashBalance = c.NetcashBalance,
                        NetcashBalanceDate = c.NetcashBalanceDate,
                        NetcashBankAccountNo = c.NetcashBankAccountNo,
                        NetcashBankAccountType = c.NetcashBankAccountType,
                        NetcashBankBranchCode = c.NetcashBankBranchCode,
                        NetcashBankName = c.NetcashBankName,
                        NoOfMeteringPoints = c.NoOfMeteringPoints,
                        NoOfRegisteredUnits = c.NoOfRegisteredUnits,
                        PartnerID = c.PartnerID,
                        PlantNo = c.PlantNo,
                        Registrable = c.Registrable,
                        ResponsibleUserID = c.ResponsibleUserID,
                        ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
                        Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
                        Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
                        Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
                        Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
                        ServiceKey = c.ServiceKey,
                        Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                        StockRefNo = c.StockRefNo,
                        SupplierAddress = c.SupplierAddress,
                        SupplierName = c.SupplierName,
                        SupplierVATNumber = c.SupplierVATNumber,
                        TargetDate = c.TargetDate,
                    };



                    model.C03_AverageAndPerUnitReport_SummaryItems.Add(item);
                }
            }

            return View("~/Views/Operational/C03_Report/C03_AverageAndPerUnitReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C03_Report/C03_Report_UserAndMeter_Summary")]
        public async Task<IActionResult> C03_Report_UserAndMeter_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_Report_UserAndMeter_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_Report_UserAndMeter_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C03_Report_UserAndMeter_Summary.GetDescription();

            C03_Report_UserAndMeter_SummaryModel model = new C03_Report_UserAndMeter_SummaryModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems_RegisteredUnits = new List<C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem>(),
                Total_RegisteredUnits = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Registered Units",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_MeteringPoints = new List<C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem>(),
                Total_MeteringPoints = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Metering Points",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#35BEAD",
                    ChartType = "line",
                    ChartyAxisID = "y-axis-1",
                },
                ReportingParentDescriptionItems_GrossProfit = new List<C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem>(),
                Total_GrossProfit = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Difference",
                    ReportingDescriptionID = 0,
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartStack = "Stack 2",
                    ChartColor = "#F7931D",
                },
                AmountType = new List<SelectListItem>(),
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Companies = companies,
            };

            foreach (var partner in partners)
                model.Partner.Add(new SelectListItem()
                {
                    Text = partner.PartnerName,
                    Value = ((int)partner.ID).ToString(),
                    Selected = ((int)partner.ID).ToString() == Request.Query["Partner"],
                });

            foreach (var amountTypeEnum in (ManagementAccounts_AmountTypeEnum[])Enum.GetValues(typeof(ManagementAccounts_AmountTypeEnum)))
                model.AmountType.Add(new SelectListItem()
                {
                    Text = amountTypeEnum.GetDescription(),
                    Value = ((int)amountTypeEnum).ToString(),
                    Selected = ((int)amountTypeEnum).ToString() == Request.Query["AmountType"],
                });
            ManagementAccounts_AmountTypeEnum managementAccounts_AmountTypeEnum = ManagementAccounts_AmountTypeEnum.Actual;
            if (!string.IsNullOrEmpty(Request.Query["AmountType"]))
                managementAccounts_AmountTypeEnum = (ManagementAccounts_AmountTypeEnum)Convert.ToInt32(Request.Query["AmountType"]);


            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (Request.QueryString.HasValue)
            {
                List<int> companyIDs = companies.Select(p => p.CompanyID).ToList();

                if (!string.IsNullOrEmpty(Request.Query["Partner"]))
                    companyIDs = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == Convert.ToInt32(Request.Query["Partner"])).Select(p => p.CompanyID).ToList();
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                    companyIDs = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).Select(p => p.CompanyID).ToList();


                var filteredData = db.ManagementAccountsDataDumps
                    .Where(data => data.Date >= model.FromDate.Date
                                && data.Date <= model.ToDate.Date
                                && companyIDs.Contains(data.CompanyID)).ToList();

                // Step 2: Group and select top record for each group
                var query = from data in filteredData
                            group data by new { data.CompanyID, data.Date } into groupedData
                            let topRecord = groupedData.OrderBy(record => record.Date).FirstOrDefault()
                            select new
                            {
                                topRecord.CompanyID,
                                topRecord.Date,
                                topRecord.ActualRegisteredUnits,
                                topRecord.Forecast1RegisteredUnits,
                                topRecord.Forecast2RegisteredUnits,
                                topRecord.Forecast3RegisteredUnits,
                                topRecord.Forecast4RegisteredUnits,
                                topRecord.Forecast5RegisteredUnits,
                                topRecord.ActualMeteringPoints,
                                topRecord.Forecast1MeteringPoints,
                                topRecord.Forecast2MeteringPoints,
                                topRecord.Forecast3MeteringPoints,
                                topRecord.Forecast4MeteringPoints,
                                topRecord.Forecast5MeteringPoints,
                            };

                var managementAccountsDataDumps = query.ToList();

                #region RegisteredUnits

                C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem RegisteredUnitsItem = new C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "RegisteredUnits",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_RegisteredUnits = (from p in managementAccountsDataDumps
                                                     where p.Date >= model.FromDate.Date
                                                     && p.Date <= model.ToDate.Date
                                                     && p.CompanyID == company.CompanyID
                                                     select p).ToList();

                    if (datadumps_RegisteredUnits.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_RegisteredUnits
                                                   where p.Date.Date == current.Date
                                                   && p.CompanyID == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            switch (managementAccounts_AmountTypeEnum)
                            {
                                case ManagementAccounts_AmountTypeEnum.Actual:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.ActualRegisteredUnits);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast1:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast1RegisteredUnits.HasValue ? EmployeeCosts_Admin.Forecast1RegisteredUnits.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast2:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast2RegisteredUnits.HasValue ? EmployeeCosts_Admin.Forecast2RegisteredUnits.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast3:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast3RegisteredUnits.HasValue ? EmployeeCosts_Admin.Forecast3RegisteredUnits.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast4:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast4RegisteredUnits.HasValue ? EmployeeCosts_Admin.Forecast4RegisteredUnits.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast5:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast5RegisteredUnits.HasValue ? EmployeeCosts_Admin.Forecast5RegisteredUnits.Value : 0);
                                    break;
                            }
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (RegisteredUnitsItem.Total.MonthlyValues.ContainsKey(current))
                            RegisteredUnitsItem.Total.MonthlyValues[current] = RegisteredUnitsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            RegisteredUnitsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_RegisteredUnits.MonthlyValues.ContainsKey(current))
                            model.Total_RegisteredUnits.MonthlyValues[current] = model.Total_RegisteredUnits.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_RegisteredUnits.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        RegisteredUnitsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_RegisteredUnits.Add(RegisteredUnitsItem);

                #endregion

                #region MeteringPoints

                C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem MeteringPointsItem = new C03_Report_UserAndMeter_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "MeteringPoints",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_UserAndMeter_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_MeteringPoints = (from p in managementAccountsDataDumps
                                                    where p.Date >= model.FromDate.Date
                                                    && p.Date <= model.ToDate.Date
                                                    && p.CompanyID == company.CompanyID
                                                    select p).ToList();

                    if (datadumps_MeteringPoints.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_MeteringPoints
                                                   where p.Date.Date == current.Date
                                                   && p.CompanyID == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            switch (managementAccounts_AmountTypeEnum)
                            {
                                case ManagementAccounts_AmountTypeEnum.Actual:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.ActualMeteringPoints);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast1:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast1MeteringPoints.HasValue ? EmployeeCosts_Admin.Forecast1MeteringPoints.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast2:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast2MeteringPoints.HasValue ? EmployeeCosts_Admin.Forecast2MeteringPoints.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast3:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast3MeteringPoints.HasValue ? EmployeeCosts_Admin.Forecast3MeteringPoints.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast4:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast4MeteringPoints.HasValue ? EmployeeCosts_Admin.Forecast4MeteringPoints.Value : 0);
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast5:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Forecast5MeteringPoints.HasValue ? EmployeeCosts_Admin.Forecast5MeteringPoints.Value : 0);
                                    break;
                            }
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (MeteringPointsItem.Total.MonthlyValues.ContainsKey(current))
                            MeteringPointsItem.Total.MonthlyValues[current] = MeteringPointsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            MeteringPointsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_MeteringPoints.MonthlyValues.ContainsKey(current))
                            model.Total_MeteringPoints.MonthlyValues[current] = model.Total_MeteringPoints.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_MeteringPoints.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        MeteringPointsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_MeteringPoints.Add(MeteringPointsItem);

                #endregion
            }

            return View("~/Views/Operational/C03_Report/C03_Report_UserAndMeter_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C03_Report/C03_Report_ReceiptPerProperty_Summary")]
        public async Task<IActionResult> C03_Report_ReceiptPerProperty_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_Report_ReceiptPerProperty_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_Report_ReceiptPerProperty_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C03_Report_ReceiptPerProperty_Summary.GetDescription();

            C03_Report_ReceiptPerProperty_SummaryModel model = new C03_Report_ReceiptPerProperty_SummaryModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems_UniPins = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem>(),
                Total_UniPins = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "UniPins",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_Payments = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem>(),
                Total_Payments = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Netcash",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#35BEAD",
                },
                ReportingParentDescriptionItems_NetcashManualPayments = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem>(),
                Total_NetcashManualPayments = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Direct Deposits",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#F7931D",
                },
                ReportingParentDescriptionItems_Totals = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem>(),
                Total_Totals = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Totals",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Companies = companies,
            };

            foreach (var partner in partners)
                model.Partner.Add(new SelectListItem()
                {
                    Text = partner.PartnerName,
                    Value = ((int)partner.ID).ToString(),
                    Selected = ((int)partner.ID).ToString() == Request.Query["Partner"],
                });

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (Request.QueryString.HasValue)
            {
                List<int> companyIDs = companies.Select(p => p.CompanyID).ToList();

                if (!string.IsNullOrEmpty(Request.Query["Partner"]))
                    companyIDs = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == Convert.ToInt32(Request.Query["Partner"])).Select(p => p.CompanyID).ToList();
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                    companyIDs = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).Select(p => p.CompanyID).ToList();


                #region UniPins

                SqlConnection connUniPins = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandUniPins = new SqlCommand("sp_GetUniPinsPerMonth", connUniPins);
                sqlCommandUniPins.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandUniPins.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandUniPins.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableUniPins = new System.Data.DataTable();

                connUniPins.Open();
                new SqlDataAdapter(sqlCommandUniPins).Fill(dataTableUniPins);
                connUniPins.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesUniPins = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableUniPins.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesUniPins.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem UniPinsItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "UniPins",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_UniPins = (from p in tuplesUniPins
                                             where p.Item2 >= model.FromDate.Date
                                             && p.Item2 <= model.ToDate.Date
                                             && p.Item1.HasValue
                                             && p.Item1.Value == company.CompanyID
                                             select p).ToList();

                    if (datadumps_UniPins.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_UniPins
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (UniPinsItem.Total.MonthlyValues.ContainsKey(current))
                            UniPinsItem.Total.MonthlyValues[current] = UniPinsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            UniPinsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_UniPins.MonthlyValues.ContainsKey(current))
                            model.Total_UniPins.MonthlyValues[current] = model.Total_UniPins.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_UniPins.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        UniPinsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_UniPins.Add(UniPinsItem);

                #endregion

                #region Payments

                SqlConnection connPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandPayments = new SqlCommand("sp_GetPaymentsPerMonth", connPayments);
                sqlCommandPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTablePayments = new System.Data.DataTable();

                connPayments.Open();
                new SqlDataAdapter(sqlCommandPayments).Fill(dataTablePayments);
                connPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTablePayments.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem PaymentsItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Netcash",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_Payments = (from p in tuplesPayments
                                              where p.Item2 >= model.FromDate.Date
                                              && p.Item2 <= model.ToDate.Date
                                              && p.Item1.HasValue
                                              && p.Item1.Value == company.CompanyID
                                              select p).ToList();

                    if (datadumps_Payments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_Payments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (PaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            PaymentsItem.Total.MonthlyValues[current] = PaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            PaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Payments.MonthlyValues.ContainsKey(current))
                            model.Total_Payments.MonthlyValues[current] = model.Total_Payments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Payments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        PaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Payments.Add(PaymentsItem);

                #endregion

                #region NetcashManualPayments

                SqlConnection connNetcashManualPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandNetcashManualPayments = new SqlCommand("sp_GetNetcashManualPaymentsPerMonth", connNetcashManualPayments);
                sqlCommandNetcashManualPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableNetcashManualPayments = new System.Data.DataTable();

                connNetcashManualPayments.Open();
                new SqlDataAdapter(sqlCommandNetcashManualPayments).Fill(dataTableNetcashManualPayments);
                connNetcashManualPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesNetcashManualPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableNetcashManualPayments.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesNetcashManualPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem NetcashManualPaymentsItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Direct Deposits",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_NetcashManualPayments = (from p in tuplesNetcashManualPayments
                                                           where p.Item2 >= model.FromDate.Date
                                                           && p.Item2 <= model.ToDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).ToList();

                    if (datadumps_NetcashManualPayments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_NetcashManualPayments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (NetcashManualPaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            NetcashManualPaymentsItem.Total.MonthlyValues[current] = NetcashManualPaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            NetcashManualPaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_NetcashManualPayments.MonthlyValues.ContainsKey(current))
                            model.Total_NetcashManualPayments.MonthlyValues[current] = model.Total_NetcashManualPayments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_NetcashManualPayments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        NetcashManualPaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_NetcashManualPayments.Add(NetcashManualPaymentsItem);

                #endregion

                #region Totals

                C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem TotalsItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Totals",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        decimal amount = 0;

                        var unipin = (from p in tuplesUniPins
                                      where p.Item2.Date == current.Date
                                      && p.Item1.HasValue
                                      && p.Item1.Value == company.CompanyID
                                      select p).FirstOrDefault();
                        if (unipin != null)
                            amount += unipin.Item3;

                        var Payment = (from p in tuplesPayments
                                       where p.Item2.Date == current.Date
                                       && p.Item1.HasValue
                                       && p.Item1.Value == company.CompanyID
                                       select p).FirstOrDefault();
                        if (Payment != null)
                            amount += Payment.Item3;

                        var NetcashManualPayment = (from p in tuplesNetcashManualPayments
                                                    where p.Item2.Date == current.Date
                                                    && p.Item1.HasValue
                                                    && p.Item1.Value == company.CompanyID
                                                    select p).FirstOrDefault();
                        if (NetcashManualPayment != null)
                            amount += NetcashManualPayment.Item3;

                        reportingCategoryItem.MonthlyValues.Add(current, amount);

                        if (TotalsItem.Total.MonthlyValues.ContainsKey(current))
                            TotalsItem.Total.MonthlyValues[current] = TotalsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            TotalsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Totals.MonthlyValues.ContainsKey(current))
                            model.Total_Totals.MonthlyValues[current] = model.Total_Totals.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Totals.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        TotalsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Totals.Add(TotalsItem);

                #endregion
            }

            return View("~/Views/Operational/C03_Report/C03_Report_ReceiptPerProperty_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C03_Report/C03_Report_ReceiptPerProperty_Details")]
        public async Task<IActionResult> C03_Report_ReceiptPerProperty_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_Report_ReceiptPerProperty_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_Report_ReceiptPerProperty_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C03_Report_ReceiptPerProperty_Details.GetDescription();

            C03_Report_ReceiptPerProperty_DetailsModel model = new C03_Report_ReceiptPerProperty_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = DateTime.Now.Date,
                ReportingParentDescriptionItems_UniPins = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem>(),
                Total_UniPins = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "UniPins",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_Payments = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem>(),
                Total_Payments = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Netcash",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#35BEAD",
                },
                ReportingParentDescriptionItems_NetcashManualPayments = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem>(),
                Total_NetcashManualPayments = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Direct Deposits",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#F7931D",
                },
                ReportingParentDescriptionItems_Totals = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem>(),
                Total_Totals = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Totals",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Companies = companies,
                AmountType = new List<SelectListItem>(),
            };

            model.AmountType.Add(new SelectListItem()
            {
                Text = ManagementAccounts_AmountTypeEnum.Actual.GetDescription(),
                Value = ((int)ManagementAccounts_AmountTypeEnum.Actual).ToString(),
                Selected = ((int)ManagementAccounts_AmountTypeEnum.Actual).ToString() == Request.Query["AmountType"],
            });
            model.AmountType.Add(new SelectListItem()
            {
                Text = ManagementAccounts_AmountTypeEnum.Forecast1.GetDescription(),
                Value = ((int)ManagementAccounts_AmountTypeEnum.Forecast1).ToString(),
                Selected = ((int)ManagementAccounts_AmountTypeEnum.Forecast1).ToString() == Request.Query["AmountType"],
            });

            ManagementAccounts_AmountTypeEnum managementAccounts_AmountTypeEnum = ManagementAccounts_AmountTypeEnum.Actual;
            if (!string.IsNullOrEmpty(Request.Query["AmountType"]))
                managementAccounts_AmountTypeEnum = (ManagementAccounts_AmountTypeEnum)Convert.ToInt32(Request.Query["AmountType"]);

            foreach (var partner in partners)
                model.Partner.Add(new SelectListItem()
                {
                    Text = partner.PartnerName,
                    Value = ((int)partner.ID).ToString(),
                    Selected = ((int)partner.ID).ToString() == Request.Query["Partner"],
                });

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (Request.QueryString.HasValue)
            {
                List<int> companyIDs = companies.Select(p => p.CompanyID).ToList();

                if (!string.IsNullOrEmpty(Request.Query["Partner"]))
                    companyIDs = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == Convert.ToInt32(Request.Query["Partner"])).Select(p => p.CompanyID).ToList();
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                    companyIDs = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).Select(p => p.CompanyID).ToList();


                #region UniPins

                SqlConnection connUniPins = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandUniPins = new SqlCommand("sp_GetUniPinsPerDay", connUniPins);
                sqlCommandUniPins.CommandType = System.Data.CommandType.StoredProcedure;
                if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                    sqlCommandUniPins.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                else
                    sqlCommandUniPins.Parameters.AddWithValue("@FromDate", model.FromDate.AddYears(-1).ToString("yyyy-MM-dd"));
                sqlCommandUniPins.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableUniPins = new System.Data.DataTable();

                connUniPins.Open();
                new SqlDataAdapter(sqlCommandUniPins).Fill(dataTableUniPins);
                connUniPins.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesUniPins = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableUniPins.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesUniPins.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem UniPinsItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "UniPins",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_UniPins = (from p in tuplesUniPins
                                             where p.Item2 >= model.FromDate.Date
                                             && p.Item2 <= model.ToDate.Date
                                             && p.Item1.HasValue
                                             && p.Item1.Value == company.CompanyID
                                             select p).ToList();

                    if (datadumps_UniPins.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_UniPins
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                            reportingCategoryItem.MonthlyValues.Add(current, 0);
                        else
                        {
                            if (current.Date > DateTime.Now.Date)
                            {
                                var toCheckToDate = (from p in tuplesUniPins
                                                     where p.Item1.HasValue
                                                     && p.Item1.Value == company.CompanyID
                                                     orderby p.Item2
                                                     select p.Item2).FirstOrDefault();

                                var currentCheckDate = current.AddMonths(-1);

                                while (currentCheckDate >= toCheckToDate)
                                {
                                    EmployeeCosts_Admin = (from p in tuplesUniPins
                                                           where p.Item2.Date == currentCheckDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).FirstOrDefault();

                                    if (EmployeeCosts_Admin != null)
                                    {
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                                        break;
                                    }

                                    currentCheckDate = currentCheckDate.AddMonths(-1);
                                }
                            }
                            if (!reportingCategoryItem.MonthlyValues.ContainsKey(current))
                                reportingCategoryItem.MonthlyValues.Add(current, 0);
                        }

                        if (UniPinsItem.Total.MonthlyValues.ContainsKey(current))
                            UniPinsItem.Total.MonthlyValues[current] = UniPinsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            UniPinsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_UniPins.MonthlyValues.ContainsKey(current))
                            model.Total_UniPins.MonthlyValues[current] = model.Total_UniPins.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_UniPins.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        UniPinsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_UniPins.Add(UniPinsItem);

                #endregion

                #region Payments

                SqlConnection connPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandPayments = new SqlCommand("sp_GetPaymentsPerDay", connPayments);
                sqlCommandPayments.CommandType = System.Data.CommandType.StoredProcedure;
                if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                    sqlCommandPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                else
                    sqlCommandPayments.Parameters.AddWithValue("@FromDate", model.FromDate.AddYears(-1).ToString("yyyy-MM-dd"));
                sqlCommandPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTablePayments = new System.Data.DataTable();

                connPayments.Open();
                new SqlDataAdapter(sqlCommandPayments).Fill(dataTablePayments);
                connPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTablePayments.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem PaymentsItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Netcash",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_Payments = (from p in tuplesPayments
                                              where p.Item2 >= model.FromDate.Date
                                              && p.Item2 <= model.ToDate.Date
                                              && p.Item1.HasValue
                                              && p.Item1.Value == company.CompanyID
                                              select p).ToList();

                    if (datadumps_Payments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_Payments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                            reportingCategoryItem.MonthlyValues.Add(current, 0);
                        else
                        {
                            if (current.Date > DateTime.Now.Date)
                            {
                                var toCheckToDate = (from p in tuplesPayments
                                                     where p.Item1.HasValue
                                                     && p.Item1.Value == company.CompanyID
                                                     orderby p.Item2
                                                     select p.Item2).FirstOrDefault();

                                var currentCheckDate = current.AddMonths(-1);

                                while (currentCheckDate >= toCheckToDate)
                                {
                                    EmployeeCosts_Admin = (from p in tuplesPayments
                                                           where p.Item2.Date == currentCheckDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).FirstOrDefault();

                                    if (EmployeeCosts_Admin != null)
                                    {
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                                        break;
                                    }

                                    currentCheckDate = currentCheckDate.AddMonths(-1);
                                }
                            }
                            if (!reportingCategoryItem.MonthlyValues.ContainsKey(current))
                                reportingCategoryItem.MonthlyValues.Add(current, 0);
                        }

                        if (PaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            PaymentsItem.Total.MonthlyValues[current] = PaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            PaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Payments.MonthlyValues.ContainsKey(current))
                            model.Total_Payments.MonthlyValues[current] = model.Total_Payments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Payments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        PaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Payments.Add(PaymentsItem);

                #endregion

                #region NetcashManualPayments

                SqlConnection connNetcashManualPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandNetcashManualPayments = new SqlCommand("sp_GetNetcashManualPaymentsPerDay", connNetcashManualPayments);
                sqlCommandNetcashManualPayments.CommandType = System.Data.CommandType.StoredProcedure;
                if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                    sqlCommandNetcashManualPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                else
                    sqlCommandNetcashManualPayments.Parameters.AddWithValue("@FromDate", model.FromDate.AddYears(-1).ToString("yyyy-MM-dd"));
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableNetcashManualPayments = new System.Data.DataTable();

                connNetcashManualPayments.Open();
                new SqlDataAdapter(sqlCommandNetcashManualPayments).Fill(dataTableNetcashManualPayments);
                connNetcashManualPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesNetcashManualPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableNetcashManualPayments.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[2]);

                    tuplesNetcashManualPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem NetcashManualPaymentsItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Direct Deposits",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_NetcashManualPayments = (from p in tuplesNetcashManualPayments
                                                           where p.Item2 >= model.FromDate.Date
                                                           && p.Item2 <= model.ToDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).ToList();

                    if (datadumps_NetcashManualPayments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_NetcashManualPayments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else if (managementAccounts_AmountTypeEnum == ManagementAccounts_AmountTypeEnum.Actual)
                            reportingCategoryItem.MonthlyValues.Add(current, 0);
                        else
                        {
                            if (current.Date > DateTime.Now.Date)
                            {
                                var toCheckToDate = (from p in tuplesNetcashManualPayments
                                                     where p.Item1.HasValue
                                                     && p.Item1.Value == company.CompanyID
                                                     orderby p.Item2
                                                     select p.Item2).FirstOrDefault();

                                var currentCheckDate = current.AddMonths(-1);

                                while (currentCheckDate >= toCheckToDate)
                                {
                                    EmployeeCosts_Admin = (from p in tuplesNetcashManualPayments
                                                           where p.Item2.Date == currentCheckDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).FirstOrDefault();

                                    if (EmployeeCosts_Admin != null)
                                    {
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                                        break;
                                    }

                                    currentCheckDate = currentCheckDate.AddMonths(-1);
                                }
                            }
                            if (!reportingCategoryItem.MonthlyValues.ContainsKey(current))
                                reportingCategoryItem.MonthlyValues.Add(current, 0);
                        }

                        if (NetcashManualPaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            NetcashManualPaymentsItem.Total.MonthlyValues[current] = NetcashManualPaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            NetcashManualPaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_NetcashManualPayments.MonthlyValues.ContainsKey(current))
                            model.Total_NetcashManualPayments.MonthlyValues[current] = model.Total_NetcashManualPayments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_NetcashManualPayments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        NetcashManualPaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_NetcashManualPayments.Add(NetcashManualPaymentsItem);

                #endregion

                #region Totals

                C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem TotalsItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Totals",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerProperty_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        decimal amount = 0;

                        var unipin = (from p in tuplesUniPins
                                      where p.Item2.Date == current.Date
                                      && p.Item1.HasValue
                                      && p.Item1.Value == company.CompanyID
                                      select p).FirstOrDefault();
                        if (unipin != null)
                            amount += unipin.Item3;

                        var Payment = (from p in tuplesPayments
                                       where p.Item2.Date == current.Date
                                       && p.Item1.HasValue
                                       && p.Item1.Value == company.CompanyID
                                       select p).FirstOrDefault();
                        if (Payment != null)
                            amount += Payment.Item3;

                        var NetcashManualPayment = (from p in tuplesNetcashManualPayments
                                                    where p.Item2.Date == current.Date
                                                    && p.Item1.HasValue
                                                    && p.Item1.Value == company.CompanyID
                                                    select p).FirstOrDefault();
                        if (NetcashManualPayment != null)
                            amount += NetcashManualPayment.Item3;

                        reportingCategoryItem.MonthlyValues.Add(current, amount);

                        if (TotalsItem.Total.MonthlyValues.ContainsKey(current))
                            TotalsItem.Total.MonthlyValues[current] = TotalsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            TotalsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Totals.MonthlyValues.ContainsKey(current))
                            model.Total_Totals.MonthlyValues[current] = model.Total_Totals.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Totals.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        TotalsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Totals.Add(TotalsItem);

                #endregion
            }

            return View("~/Views/Operational/C03_Report/C03_Report_ReceiptPerProperty_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C03_Report/C03_Report_ReceiptPerPropertyVolumes_Summary")]
        public async Task<IActionResult> C03_Report_ReceiptPerPropertyVolumes_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Summary.GetDescription();

            C03_Report_ReceiptPerPropertyVolumes_SummaryModel model = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems_UniPins = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem>(),
                Total_UniPins = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "UniPins",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_Payments = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem>(),
                Total_Payments = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Netcash",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#35BEAD",
                },
                ReportingParentDescriptionItems_NetcashManualPayments = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem>(),
                Total_NetcashManualPayments = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Direct Deposits",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#F7931D",
                },
                ReportingParentDescriptionItems_Totals = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem>(),
                Total_Totals = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Totals",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Companies = companies,
            };

            foreach (var partner in partners)
                model.Partner.Add(new SelectListItem()
                {
                    Text = partner.PartnerName,
                    Value = ((int)partner.ID).ToString(),
                    Selected = ((int)partner.ID).ToString() == Request.Query["Partner"],
                });

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (Request.QueryString.HasValue)
            {
                List<int> companyIDs = companies.Select(p => p.CompanyID).ToList();

                if (!string.IsNullOrEmpty(Request.Query["Partner"]))
                    companyIDs = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == Convert.ToInt32(Request.Query["Partner"])).Select(p => p.CompanyID).ToList();
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                    companyIDs = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).Select(p => p.CompanyID).ToList();


                #region UniPins

                SqlConnection connUniPins = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandUniPins = new SqlCommand("sp_GetUniPinsPerMonth", connUniPins);
                sqlCommandUniPins.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandUniPins.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandUniPins.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableUniPins = new System.Data.DataTable();

                connUniPins.Open();
                new SqlDataAdapter(sqlCommandUniPins).Fill(dataTableUniPins);
                connUniPins.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesUniPins = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableUniPins.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesUniPins.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem UniPinsItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "UniPins",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_UniPins = (from p in tuplesUniPins
                                             where p.Item2 >= model.FromDate.Date
                                             && p.Item2 <= model.ToDate.Date
                                             && p.Item1.HasValue
                                             && p.Item1.Value == company.CompanyID
                                             select p).ToList();

                    if (datadumps_UniPins.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_UniPins
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (UniPinsItem.Total.MonthlyValues.ContainsKey(current))
                            UniPinsItem.Total.MonthlyValues[current] = UniPinsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            UniPinsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_UniPins.MonthlyValues.ContainsKey(current))
                            model.Total_UniPins.MonthlyValues[current] = model.Total_UniPins.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_UniPins.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        UniPinsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_UniPins.Add(UniPinsItem);

                #endregion

                #region Payments

                SqlConnection connPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandPayments = new SqlCommand("sp_GetPaymentsPerMonth", connPayments);
                sqlCommandPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTablePayments = new System.Data.DataTable();

                connPayments.Open();
                new SqlDataAdapter(sqlCommandPayments).Fill(dataTablePayments);
                connPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTablePayments.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem PaymentsItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Netcash",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_Payments = (from p in tuplesPayments
                                              where p.Item2 >= model.FromDate.Date
                                              && p.Item2 <= model.ToDate.Date
                                              && p.Item1.HasValue
                                              && p.Item1.Value == company.CompanyID
                                              select p).ToList();

                    if (datadumps_Payments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_Payments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (PaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            PaymentsItem.Total.MonthlyValues[current] = PaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            PaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Payments.MonthlyValues.ContainsKey(current))
                            model.Total_Payments.MonthlyValues[current] = model.Total_Payments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Payments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        PaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Payments.Add(PaymentsItem);

                #endregion

                #region NetcashManualPayments

                SqlConnection connNetcashManualPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandNetcashManualPayments = new SqlCommand("sp_GetNetcashManualPaymentsPerMonth", connNetcashManualPayments);
                sqlCommandNetcashManualPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableNetcashManualPayments = new System.Data.DataTable();

                connNetcashManualPayments.Open();
                new SqlDataAdapter(sqlCommandNetcashManualPayments).Fill(dataTableNetcashManualPayments);
                connNetcashManualPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesNetcashManualPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableNetcashManualPayments.Rows)
                {
                    DateTime month = new DateTime(Convert.ToInt32(dr[0].ToString().Split('-')[0]), Convert.ToInt32(dr[0].ToString().Split('-')[1]), 1);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesNetcashManualPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem NetcashManualPaymentsItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Direct Deposits",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_NetcashManualPayments = (from p in tuplesNetcashManualPayments
                                                           where p.Item2 >= model.FromDate.Date
                                                           && p.Item2 <= model.ToDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).ToList();

                    if (datadumps_NetcashManualPayments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_NetcashManualPayments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (NetcashManualPaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            NetcashManualPaymentsItem.Total.MonthlyValues[current] = NetcashManualPaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            NetcashManualPaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_NetcashManualPayments.MonthlyValues.ContainsKey(current))
                            model.Total_NetcashManualPayments.MonthlyValues[current] = model.Total_NetcashManualPayments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_NetcashManualPayments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        NetcashManualPaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_NetcashManualPayments.Add(NetcashManualPaymentsItem);

                #endregion

                #region Totals

                C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem TotalsItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Totals",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_SummaryModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        decimal amount = 0;

                        var unipin = (from p in tuplesUniPins
                                      where p.Item2.Date == current.Date
                                      && p.Item1.HasValue
                                      && p.Item1.Value == company.CompanyID
                                      select p).FirstOrDefault();
                        if (unipin != null)
                            amount += unipin.Item3;

                        var Payment = (from p in tuplesPayments
                                       where p.Item2.Date == current.Date
                                       && p.Item1.HasValue
                                       && p.Item1.Value == company.CompanyID
                                       select p).FirstOrDefault();
                        if (Payment != null)
                            amount += Payment.Item3;

                        var NetcashManualPayment = (from p in tuplesNetcashManualPayments
                                                    where p.Item2.Date == current.Date
                                                    && p.Item1.HasValue
                                                    && p.Item1.Value == company.CompanyID
                                                    select p).FirstOrDefault();
                        if (NetcashManualPayment != null)
                            amount += NetcashManualPayment.Item3;

                        reportingCategoryItem.MonthlyValues.Add(current, amount);

                        if (TotalsItem.Total.MonthlyValues.ContainsKey(current))
                            TotalsItem.Total.MonthlyValues[current] = TotalsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            TotalsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Totals.MonthlyValues.ContainsKey(current))
                            model.Total_Totals.MonthlyValues[current] = model.Total_Totals.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Totals.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddMonths(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        TotalsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Totals.Add(TotalsItem);

                #endregion
            }

            return View("~/Views/Operational/C03_Report/C03_Report_ReceiptPerPropertyVolumes_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C03_Report/C03_Report_ReceiptPerPropertyVolumes_Details")]
        public async Task<IActionResult> C03_Report_ReceiptPerPropertyVolumes_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Details.GetDescription();

            C03_Report_ReceiptPerPropertyVolumes_DetailsModel model = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = DateTime.Now,
                ReportingParentDescriptionItems_UniPins = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem>(),
                Total_UniPins = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "UniPins",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_Payments = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem>(),
                Total_Payments = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Netcash",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#35BEAD",
                },
                ReportingParentDescriptionItems_NetcashManualPayments = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem>(),
                Total_NetcashManualPayments = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Direct Deposits",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#F7931D",
                },
                ReportingParentDescriptionItems_Totals = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem>(),
                Total_Totals = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Totals",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["Partner"]) }
                },
                Companies = companies,
            };

            foreach (var partner in partners)
                model.Partner.Add(new SelectListItem()
                {
                    Text = partner.PartnerName,
                    Value = ((int)partner.ID).ToString(),
                    Selected = ((int)partner.ID).ToString() == Request.Query["Partner"],
                });

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (Request.QueryString.HasValue)
            {
                List<int> companyIDs = companies.Select(p => p.CompanyID).ToList();

                if (!string.IsNullOrEmpty(Request.Query["Partner"]))
                    companyIDs = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == Convert.ToInt32(Request.Query["Partner"])).Select(p => p.CompanyID).ToList();
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                    companyIDs = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).Select(p => p.CompanyID).ToList();


                #region UniPins

                SqlConnection connUniPins = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandUniPins = new SqlCommand("sp_GetUniPinsPerDay", connUniPins);
                sqlCommandUniPins.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandUniPins.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandUniPins.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableUniPins = new System.Data.DataTable();

                connUniPins.Open();
                new SqlDataAdapter(sqlCommandUniPins).Fill(dataTableUniPins);
                connUniPins.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesUniPins = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableUniPins.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesUniPins.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem UniPinsItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "UniPins",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_UniPins = (from p in tuplesUniPins
                                             where p.Item2 >= model.FromDate.Date
                                             && p.Item2 <= model.ToDate.Date
                                             && p.Item1.HasValue
                                             && p.Item1.Value == company.CompanyID
                                             select p).ToList();

                    if (datadumps_UniPins.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_UniPins
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (UniPinsItem.Total.MonthlyValues.ContainsKey(current))
                            UniPinsItem.Total.MonthlyValues[current] = UniPinsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            UniPinsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_UniPins.MonthlyValues.ContainsKey(current))
                            model.Total_UniPins.MonthlyValues[current] = model.Total_UniPins.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_UniPins.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        UniPinsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_UniPins.Add(UniPinsItem);

                #endregion

                #region Payments

                SqlConnection connPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandPayments = new SqlCommand("sp_GetPaymentsPerDay", connPayments);
                sqlCommandPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTablePayments = new System.Data.DataTable();

                connPayments.Open();
                new SqlDataAdapter(sqlCommandPayments).Fill(dataTablePayments);
                connPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTablePayments.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem PaymentsItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Netcash",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_Payments = (from p in tuplesPayments
                                              where p.Item2 >= model.FromDate.Date
                                              && p.Item2 <= model.ToDate.Date
                                              && p.Item1.HasValue
                                              && p.Item1.Value == company.CompanyID
                                              select p).ToList();

                    if (datadumps_Payments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_Payments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (PaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            PaymentsItem.Total.MonthlyValues[current] = PaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            PaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Payments.MonthlyValues.ContainsKey(current))
                            model.Total_Payments.MonthlyValues[current] = model.Total_Payments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Payments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        PaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Payments.Add(PaymentsItem);

                #endregion

                #region NetcashManualPayments

                SqlConnection connNetcashManualPayments = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandNetcashManualPayments = new SqlCommand("sp_GetNetcashManualPaymentsPerDay", connNetcashManualPayments);
                sqlCommandNetcashManualPayments.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@FromDate", model.FromDate.ToString("yyyy-MM-dd"));
                sqlCommandNetcashManualPayments.Parameters.AddWithValue("@ToDate", model.ToDate.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTableNetcashManualPayments = new System.Data.DataTable();

                connNetcashManualPayments.Open();
                new SqlDataAdapter(sqlCommandNetcashManualPayments).Fill(dataTableNetcashManualPayments);
                connNetcashManualPayments.Close();

                List<Tuple<int?, DateTime, decimal>> tuplesNetcashManualPayments = new List<Tuple<int?, DateTime, decimal>>();

                foreach (DataRow dr in dataTableNetcashManualPayments.Rows)
                {
                    DateTime month = Convert.ToDateTime(dr[0]);
                    int? companyID = null;
                    if (dr[1] != DBNull.Value)
                        companyID = Convert.ToInt32(dr[1]);
                    decimal amount = Convert.ToDecimal(dr[3]);

                    tuplesNetcashManualPayments.Add(new Tuple<int?, DateTime, decimal>(companyID, month, amount));
                }

                C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem NetcashManualPaymentsItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Direct Deposits",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    var datadumps_NetcashManualPayments = (from p in tuplesNetcashManualPayments
                                                           where p.Item2 >= model.FromDate.Date
                                                           && p.Item2 <= model.ToDate.Date
                                                           && p.Item1.HasValue
                                                           && p.Item1.Value == company.CompanyID
                                                           select p).ToList();

                    if (datadumps_NetcashManualPayments.Count == 0)
                        continue;

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in datadumps_NetcashManualPayments
                                                   where p.Item2.Date == current.Date
                                                   && p.Item1.HasValue
                                                   && p.Item1.Value == company.CompanyID
                                                   select p).FirstOrDefault();

                        if (EmployeeCosts_Admin != null)
                            reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Item3);
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);

                        if (NetcashManualPaymentsItem.Total.MonthlyValues.ContainsKey(current))
                            NetcashManualPaymentsItem.Total.MonthlyValues[current] = NetcashManualPaymentsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            NetcashManualPaymentsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_NetcashManualPayments.MonthlyValues.ContainsKey(current))
                            model.Total_NetcashManualPayments.MonthlyValues[current] = model.Total_NetcashManualPayments.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_NetcashManualPayments.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        NetcashManualPaymentsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_NetcashManualPayments.Add(NetcashManualPaymentsItem);

                #endregion

                #region Totals

                C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem TotalsItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingParentDescriptionItem()
                {
                    ReportingCategoryItems = new List<C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem>(),
                    Total = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = "Totals",
                        ReportingDescriptionID = 0,
                    },
                };

                foreach (var company in companies.Where(p => companyIDs.Contains(p.CompanyID)).ToList())
                {
                    C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C03_Report_ReceiptPerPropertyVolumes_DetailsModel.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        decimal amount = 0;

                        var unipin = (from p in tuplesUniPins
                                      where p.Item2.Date == current.Date
                                      && p.Item1.HasValue
                                      && p.Item1.Value == company.CompanyID
                                      select p).FirstOrDefault();
                        if (unipin != null)
                            amount += unipin.Item3;

                        var Payment = (from p in tuplesPayments
                                       where p.Item2.Date == current.Date
                                       && p.Item1.HasValue
                                       && p.Item1.Value == company.CompanyID
                                       select p).FirstOrDefault();
                        if (Payment != null)
                            amount += Payment.Item3;

                        var NetcashManualPayment = (from p in tuplesNetcashManualPayments
                                                    where p.Item2.Date == current.Date
                                                    && p.Item1.HasValue
                                                    && p.Item1.Value == company.CompanyID
                                                    select p).FirstOrDefault();
                        if (NetcashManualPayment != null)
                            amount += NetcashManualPayment.Item3;

                        reportingCategoryItem.MonthlyValues.Add(current, amount);

                        if (TotalsItem.Total.MonthlyValues.ContainsKey(current))
                            TotalsItem.Total.MonthlyValues[current] = TotalsItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            TotalsItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        if (model.Total_Totals.MonthlyValues.ContainsKey(current))
                            model.Total_Totals.MonthlyValues[current] = model.Total_Totals.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_Totals.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                        current = current.AddDays(1);
                    }

                    if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                        TotalsItem.ReportingCategoryItems.Add(reportingCategoryItem);
                }

                model.ReportingParentDescriptionItems_Totals.Add(TotalsItem);

                #endregion
            }

            return View("~/Views/Operational/C03_Report/C03_Report_ReceiptPerPropertyVolumes_Details.cshtml", model);
        }

    }
}
