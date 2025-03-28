using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C01_ProductReportModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.C01_ProductReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C01_ProductReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C01_ProductReportController(
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
        [Route("/operational/C01_ProductReport/C01_ProductReport_Summary")]
        public async Task<IActionResult> C01_ProductReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C01_ProductReport_SummaryModel model = new C01_ProductReport_SummaryModel()
            {
                C01_ProductReport_SummaryItems = new List<C01_ProductReport_SummaryModel.C01_ProductReport_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                C01_ProductReport_SummaryModel.C01_ProductReport_SummaryItem item = new C01_ProductReport_SummaryModel.C01_ProductReport_SummaryItem()
                {
                    CompanyID = company.CompanyID,
                    Name = company.Name,
                    BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = company.BalanceMustBeAbove,
                    ExistsInSkybill = company.ExistsInSkybill,
                    Registrable = company.Registrable,
                    ServiceKey = company.ServiceKey,
                    C01_ProductReport_SummarySubItems = new List<C01_ProductReport_SummaryModel.C01_ProductReport_SummaryItem.C01_ProductReport_SummarySubItem>(),
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


                        if (item.C01_ProductReport_SummarySubItems.Where(p => p.Month == current).Count() > 0)
                        {
                            var itemToUpdate = item.C01_ProductReport_SummarySubItems.Where(p => p.Month == current).SingleOrDefault();
                            if (amountProduct.HasValue)
                                item.C01_ProductReport_SummarySubItems[item.C01_ProductReport_SummarySubItems.IndexOf(itemToUpdate)].TotalSales = item.C01_ProductReport_SummarySubItems[item.C01_ProductReport_SummarySubItems.IndexOf(itemToUpdate)].TotalSales + amountProduct.Value;
                            if (amountSupplyCost.HasValue)
                                item.C01_ProductReport_SummarySubItems[item.C01_ProductReport_SummarySubItems.IndexOf(itemToUpdate)].TotalCOS = item.C01_ProductReport_SummarySubItems[item.C01_ProductReport_SummarySubItems.IndexOf(itemToUpdate)].TotalCOS + amountSupplyCost.Value;
                        }
                        else
                            item.C01_ProductReport_SummarySubItems.Add(new C01_ProductReport_SummaryModel.C01_ProductReport_SummaryItem.C01_ProductReport_SummarySubItem()
                            {
                                Month = current,
                                TotalCOS = amountSupplyCost.HasValue ? amountSupplyCost.Value : 0,
                                TotalSales = amountProduct.HasValue ? amountProduct.Value : 0,
                            });

                        current = current.AddMonths(1);
                    }

                }

                model.C01_ProductReport_SummaryItems.Add(item);
            }

            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Details")]
        public async Task<IActionResult> C01_ProductReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));

            C01_ProductReport_DetailsModel model = new C01_ProductReport_DetailsModel()
            {
                C01_ProductReport_DetailsProductItems = new List<C01_ProductReport_DetailsModel.C01_ProductReport_DetailsProductItem>(),
                C01_ProductReport_DetailsSupplyCostItems = new List<C01_ProductReport_DetailsModel.C01_ProductReport_DetailsSupplyCostItem>(),
                C01_ProductReport_DetailsGrossAmountItems = new List<C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossAmountItem>(),
                C01_ProductReport_DetailsGrossPercItems = new List<C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossPercItem>(),
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                ProductIDs = new List<string>(),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var latestRequest = (from p in db.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new C01_ProductReport_DetailsModel.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latestRequest.CreatedBy,
                        CompanyID = latestRequest.CompanyID,
                        CreatedDate = latestRequest.CreatedDate,
                        DateEnded = latestRequest.DateEnded,
                        DateStarted = latestRequest.DateStarted,
                        FromDate = latestRequest.FromDate,
                        ID = latestRequest.ID,
                        Progress = latestRequest.Progress,
                        SystemReportID = latestRequest.SystemReportID,
                        ToDate = latestRequest.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }
                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                foreach (var uC in _operationalProvider.UserCompanies)
                {
                    if (_operationalProvider.CompanyID > 0 && _operationalProvider.CompanyID != uC.CompanyID)
                        continue;

                    var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                    foreach (var product in model.SiteAdmin_Products)
                    {
                        if (model.ProductIDs.Count == 0 || model.ProductIDs.Contains(product.ID.ToString()))
                        { }
                        else
                            continue;

                        C01_ProductReport_DetailsModel.C01_ProductReport_DetailsProductItem productItem = new C01_ProductReport_DetailsModel.C01_ProductReport_DetailsProductItem()
                        {
                            CompanyID = company.CompanyID,
                            CompanyName = company.Name,
                            ProductID = product.ID,
                            ProductName = product.ProductName,
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        };


                        C01_ProductReport_DetailsModel.C01_ProductReport_DetailsSupplyCostItem supplyCostItem = new C01_ProductReport_DetailsModel.C01_ProductReport_DetailsSupplyCostItem()
                        {
                            CompanyID = company.CompanyID,
                            CompanyName = company.Name,
                            ProductID = product.ID,
                            ProductName = product.ProductName,
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        };

                        C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossAmountItem grossAmountItem = new C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossAmountItem()
                        {
                            CompanyID = company.CompanyID,
                            CompanyName = company.Name,
                            ProductID = product.ID,
                            ProductName = product.ProductName,
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        };

                        C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossPercItem grossPercItem = new C01_ProductReport_DetailsModel.C01_ProductReport_DetailsGrossPercItem()
                        {
                            CompanyID = company.CompanyID,
                            CompanyName = company.Name,
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
                                    var rentalDataDumps = (from p in db.RentalDataDumps
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

                            productItem.MonthlyValues.Add(current, amountProduct);

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
                                    var rentalDataDumps = (from p in db.RentalDataDumps
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

                            supplyCostItem.MonthlyValues.Add(current, amountSupplyCost);

                            decimal? amountGrossAmount = null;
                            if (amountProduct.HasValue || amountSupplyCost.HasValue)
                                amountGrossAmount = (amountProduct.HasValue ? amountProduct.Value : 0) + (amountSupplyCost.HasValue ? amountSupplyCost.Value : 0);

                            decimal? quantityGrossAmount = null;
                            if (quantityProduct.HasValue || quantitySupplyCost.HasValue)
                                quantityGrossAmount = (quantityProduct.HasValue ? quantityProduct.Value : 0) + (quantitySupplyCost.HasValue ? quantitySupplyCost.Value : 0);

                            grossAmountItem.MonthlyValues.Add(current, amountGrossAmount);

                            decimal? amountGrossPerc = null;
                            decimal? quantityGrossPerc = null;

                            if (amountGrossAmount.HasValue && amountProduct.HasValue && amountProduct.Value != 0)
                                amountGrossPerc = (amountGrossAmount.Value / amountProduct.Value) * 100.0m;

                            if (amountGrossAmount.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                                quantityGrossPerc = (amountGrossAmount.Value / quantityProduct.Value) * 100.0m;

                            grossPercItem.MonthlyValues.Add(current, amountGrossPerc);


                            current = current.AddMonths(1);
                        }


                        if (model.HideNoData)
                        {
                            if (productItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                                && supplyCostItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                                && grossAmountItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                                && grossPercItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0)
                            {
                                continue;
                            }
                        }
                        model.C01_ProductReport_DetailsProductItems.Add(productItem);
                        model.C01_ProductReport_DetailsSupplyCostItems.Add(supplyCostItem);
                        model.C01_ProductReport_DetailsGrossAmountItems.Add(grossAmountItem);
                        model.C01_ProductReport_DetailsGrossPercItems.Add(grossPercItem);
                    }

                }
            }

            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Details_RequestRerun")]
        public async Task<IActionResult> C01_ProductReport_Details_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request = new F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(2018, 01, 1),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                };

                db.Add(F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Details");
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Daily")]
        public async Task<IActionResult> C01_ProductReport_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C01_ProductReport_DailyModel model = new C01_ProductReport_DailyModel()
            {
                C01_ProductReport_DailyProductItems = new List<C01_ProductReport_DailyModel.C01_ProductReport_DailyProductItem>(),
                //C01_ProductReport_DailySupplyCostItems = new List<C01_ProductReport_DailyModel.C01_ProductReport_DailySupplyCostItem>(),
                //C01_ProductReport_DailyGrossAmountItems = new List<C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossAmountItem>(),
                //C01_ProductReport_DailyGrossPercItems = new List<C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossPercItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                ProductIDs = new List<string>(),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.FromDate.Year, model.FromDate.Month, DateTime.DaysInMonth(model.FromDate.Year, model.FromDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID == 0 || _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                //var resourceLedgers = skyBillApiClient.GetResourceLedgerEntries(model.FromDate, model.ToDate);

                //var ledgers = skyBillApiClient.GetGeneralLedgerEntries(model.FromDate.Date, model.ToDate.Date, "", "");
                //if (ledgers == null || ledgers.value == null)
                //    continue;
                //var generalLedgersForCompany = (from p in ledgers.value
                //                                select new
                //                                {
                //                                    p.G_L_Account_No,
                //                                    p.Posting_Date,
                //                                    p.Amount,
                //                                    p.Quantity
                //                                }).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date <= model.ToDate.Date
                                                && p.CompanyID == company.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var product in model.SiteAdmin_Products)
                {
                    if (model.ProductIDs.Count == 0 || model.ProductIDs.Contains(product.ID.ToString()))
                    { }
                    else
                        continue;

                    C01_ProductReport_DailyModel.C01_ProductReport_DailyProductItem productItem = new C01_ProductReport_DailyModel.C01_ProductReport_DailyProductItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    };


                    //C01_ProductReport_DailyModel.C01_ProductReport_DailySupplyCostItem supplyCostItem = new C01_ProductReport_DailyModel.C01_ProductReport_DailySupplyCostItem()
                    //{
                    //    CompanyID = company.CompanyID,
                    //    CompanyName = company.Name,
                    //    ProductID = product.ID,
                    //    ProductName = product.ProductName,
                    //    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    //};

                    //C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossAmountItem grossAmountItem = new C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossAmountItem()
                    //{
                    //    CompanyID = company.CompanyID,
                    //    CompanyName = company.Name,
                    //    ProductID = product.ID,
                    //    ProductName = product.ProductName,
                    //    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    //};

                    //C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossPercItem grossPercItem = new C01_ProductReport_DailyModel.C01_ProductReport_DailyGrossPercItem()
                    //{
                    //    CompanyID = company.CompanyID,
                    //    CompanyName = company.Name,
                    //    ProductID = product.ID,
                    //    ProductName = product.ProductName,
                    //    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    //};

                    var resourcesForProduct = (from p in db.SkybillResourceLists
                                               where p.ProductID.HasValue
                                               && p.ProductID.Value == product.ID
                                               && p.CompanyID == company.CompanyID
                                               select p.No).ToList();

                    //var resourceLedgersForProduct = (from p in resourceLedgers
                    //                                 where resourcesForProduct.Contains(p.Resource_No)
                    //                                 select new
                    //                                 {
                    //                                     p.Posting_Date,
                    //                                     p.Total_Price,
                    //                                     p.Quantity
                    //                                 }).ToList();

                    var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                     where resourcesForProduct.Contains(p.Resource_No)
                                                     && p.Posting_Date.Date >= model.FromDate.Date
                                                     && p.Posting_Date <= model.ToDate.Date
                                                     && p.CompanyID == company.CompanyID
                                                     select new
                                                     {
                                                         p.Posting_Date,
                                                         p.Total_Price,
                                                         p.Quantity
                                                     }).ToList();


                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        decimal? amountProduct = null;
                        decimal? quantityProduct = null;
                        var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                     where p.Posting_Date.Date == current.Date
                                                     select
                                                     new
                                                     {
                                                         Amount = p.Total_Price,
                                                         Quantity = p.Quantity
                                                     }
                                                     ).ToList();

                        switch (product.SalesLink)
                        {
                            default:
                            case 0:
                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                {
                                    amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                    quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                }
                                break;
                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                //                       where p.RentalMonth == current
                                //                       && p.PropertyLinked == company.Name
                                //                       select p).ToList();

                                //if (rentalDataDumps.Count > 0)
                                //{
                                //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                //}

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                   where p.Posting_Date == current.Date
                                                                   && p.G_L_Account_No == "6810"
                                                                   select p).ToList();

                                if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                    quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                        where p.Posting_Date == current.Date
                                                                        && p.G_L_Account_No == "7191"
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                    quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                        where p.Posting_Date == current.Date
                                                                        && p.G_L_Account_No == "8640"
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                    quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                        where p.Posting_Date == current.Date
                                                                        && p.G_L_Account_No == "6610"
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                    quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                        where p.Posting_Date == current.Date
                                                                        && p.G_L_Account_No == "8620"
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                    quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                }

                                break;
                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                        where p.Posting_Date == current.Date
                                                                        && p.G_L_Account_No == "6811"
                                                                        select p).ToList();

                                if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                {
                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                    quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                }

                                break;
                        }


                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        productItem.MonthlyValues.Add(current, amountProduct);

                        //decimal? amountSupplyCost = null;
                        //decimal? quantitySupplyCost = null;

                        //switch (product.CostOfSalesLink)
                        //{
                        //    case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                        //        //var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                        //        //                                where p.Month == current
                        //        //                                && p.ProductID == product.ID
                        //        //                                && p.CompanyID == company.CompanyID
                        //        //                                select p).SingleOrDefault();
                        //        //if (report_SupplyCostMonthly != null)
                        //        //{
                        //        //    amountSupplyCost = report_SupplyCostMonthly.Amount;
                        //        //    quantitySupplyCost = report_SupplyCostMonthly.Quantity;
                        //        //}
                        //        break;
                        //    case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                        //        //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                        //        //                       where p.RentalMonth == current
                        //        //                       && p.PropertyLinked == company.Name
                        //        //                       select p).ToList();

                        //        //if (rentalDataDumps.Count > 0)
                        //        //{
                        //        //    amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                        //        //}

                        //        break;
                        //    case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                        //        var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                        //                                           where p.Posting_Date == current.Date
                        //                                           && p.G_L_Account_No == "6810"
                        //                                           select p).ToList();

                        //        if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                        //        {
                        //            amountSupplyCost = report_GeneralLedgerMonthly.Select(p => p.Amount).Sum();
                        //            quantitySupplyCost = report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum();
                        //        }


                        //        break;
                        //    case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                        //        var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                        //                                                where p.Posting_Date == current.Date
                        //                                                && p.G_L_Account_No == "7191"
                        //                                                select p).ToList();

                        //        if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                        //        {
                        //            amountSupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum();
                        //            quantitySupplyCost = report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum();
                        //        }

                        //        break;
                        //}

                        //if (amountSupplyCost.HasValue)
                        //    amountSupplyCost = amountSupplyCost.Value * -1.0m;
                        //if (quantitySupplyCost.HasValue)
                        //    quantitySupplyCost = quantitySupplyCost.Value * -1.0m;

                        //supplyCostItem.MonthlyValues.Add(current, amountSupplyCost);

                        //decimal? amountGrossAmount = null;
                        //if (amountProduct.HasValue || amountSupplyCost.HasValue)
                        //    amountGrossAmount = (amountProduct.HasValue ? amountProduct.Value : 0) + (amountSupplyCost.HasValue ? amountSupplyCost.Value : 0);

                        //decimal? quantityGrossAmount = null;
                        //if (quantityProduct.HasValue || quantitySupplyCost.HasValue)
                        //    quantityGrossAmount = (quantityProduct.HasValue ? quantityProduct.Value : 0) + (quantitySupplyCost.HasValue ? quantitySupplyCost.Value : 0);

                        //grossAmountItem.MonthlyValues.Add(current, amountGrossAmount);

                        //decimal? amountGrossPerc = null;
                        //decimal? quantityGrossPerc = null;

                        //if (amountGrossAmount.HasValue && amountProduct.HasValue && amountProduct.Value != 0)
                        //    amountGrossPerc = (amountGrossAmount.Value / amountProduct.Value) * 100.0m;

                        //if (amountGrossAmount.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                        //    quantityGrossPerc = (amountGrossAmount.Value / quantityProduct.Value) * 100.0m;

                        //grossPercItem.MonthlyValues.Add(current, amountGrossPerc);


                        current = current.AddDays(1);
                    }


                    if (model.HideNoData)
                    {
                        if (productItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                            /*&& supplyCostItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                            && grossAmountItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0
                            && grossPercItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0*/)
                        {
                            continue;
                        }
                    }
                    model.C01_ProductReport_DailyProductItems.Add(productItem);
                    //model.C01_ProductReport_DailySupplyCostItems.Add(supplyCostItem);
                    //model.C01_ProductReport_DailyGrossAmountItems.Add(grossAmountItem);
                    //model.C01_ProductReport_DailyGrossPercItems.Add(grossPercItem);
                }

            }

            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Details_Details")]
        public async Task<IActionResult> C01_ProductReport_Details_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Details_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Details_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Details_DetailsModel model = new C01_ProductReport_Details_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                C01_ProductReport_Details_DetailsItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (model.ToDate.AddMonths(-1) > model.FromDate)
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                List<string> tarrifNames = new List<string>();

                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                {
                    tarrifNames = (from p in tarrifs
                                   where p.Resource_No == Request.Query["Tariffs"]
                                   select p.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty).ToUpper()).Distinct().ToList();
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date <= model.ToDate.Date
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem item = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem()
                    {
                        C01_ProductReport_Details_DetailsSubItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem customerItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList();

                        foreach (var util in utils)
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 && p.Description == util.Description
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date == currentDate.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date == currentDate.Date
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountProduct));

                                    currentDate = currentDate.AddDays(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Details_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Details_DetailsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Details_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Details_DetailsItems = model.C01_ProductReport_Details_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Details_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Details_Details_Units")]
        public async Task<IActionResult> C01_ProductReport_Details_Details_Units()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Details_Details_Units, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Details_Details_Units}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Details_DetailsModel model = new C01_ProductReport_Details_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                C01_ProductReport_Details_DetailsItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (model.ToDate.AddMonths(-1) > model.FromDate)
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date <= model.ToDate.Date
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem item = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem()
                    {
                        C01_ProductReport_Details_DetailsSubItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem customerItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }
                                if (resourcesForProduct.Count == 0)
                                    continue;
                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date == currentDate.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date == currentDate.Date
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, quantityProduct));

                                    currentDate = currentDate.AddDays(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Details_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Details_Details_UnitsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Details_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Details_DetailsItems = model.C01_ProductReport_Details_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Details_Details_Units.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Details_Details_AVG")]
        public async Task<IActionResult> C01_ProductReport_Details_Details_AVG()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Details_Details_AVG, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Details_Details_AVG}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Details_DetailsModel model = new C01_ProductReport_Details_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                C01_ProductReport_Details_DetailsItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (model.ToDate.AddMonths(-1) > model.FromDate)
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date <= model.ToDate.Date
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem item = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem()
                    {
                        C01_ProductReport_Details_DetailsSubItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem customerItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Details_DetailsModel.C01_ProductReport_Details_DetailsItem.C01_ProductReport_Details_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date == currentDate.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date == currentDate.Date
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;

                                    decimal? avg = null;

                                    if (amountProduct.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                                    {
                                        avg = amountProduct.Value / quantityProduct.Value;
                                    }

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, avg));

                                    currentDate = currentDate.AddDays(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Details_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Details_DetailsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Details_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Details_DetailsItems = model.C01_ProductReport_Details_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Details_Details_AVG.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Monthly_Details")]
        public async Task<IActionResult> C01_ProductReport_Monthly_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Monthly_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Monthly_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Monthly_DetailsModel model = new C01_ProductReport_Monthly_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                C01_ProductReport_Monthly_DetailsItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));
            }

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem item = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem()
                    {
                        C01_ProductReport_Monthly_DetailsSubItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem customerItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountProduct));

                                    currentDate = currentDate.AddMonths(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Monthly_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Monthly_DetailsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Monthly_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Monthly_DetailsItems = model.C01_ProductReport_Monthly_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Monthly_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Monthly_Details_Units")]
        public async Task<IActionResult> C01_ProductReport_Monthly_Details_Units()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Monthly_Details_Units, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Monthly_Details_Units}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Monthly_DetailsModel model = new C01_ProductReport_Monthly_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                C01_ProductReport_Monthly_DetailsItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));
            }

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem item = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem()
                    {
                        C01_ProductReport_Monthly_DetailsSubItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem customerItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, quantityProduct));

                                    currentDate = currentDate.AddMonths(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Monthly_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Monthly_Details_UnitsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Monthly_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Monthly_DetailsItems = model.C01_ProductReport_Monthly_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Monthly_Details_Units.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_Monthly_Details_AVG")]
        public async Task<IActionResult> C01_ProductReport_Monthly_Details_AVG()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_Monthly_Details_AVG, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_Monthly_Details_AVG}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            C01_ProductReport_Monthly_DetailsModel model = new C01_ProductReport_Monthly_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                C01_ProductReport_Monthly_DetailsItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem>(),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Select Product]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                Tariffs = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Tariffs]", Selected = string.IsNullOrEmpty(Request.Query["Tariffs"]) }
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };
            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));
            }

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                        {
                            Value = t.Resource_No.ToString(),
                            Text = $"{t.Resource_No} - {t.Resource_Name}",
                            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                        });
                }

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem item = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem()
                    {
                        C01_ProductReport_Monthly_DetailsSubItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;

                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem customerItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in db.SkybillResourceLists
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new C01_ProductReport_Monthly_DetailsModel.C01_ProductReport_Monthly_DetailsItem.C01_ProductReport_Monthly_DetailsSubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                };

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
                                                                 select
                                                                 new
                                                                 {
                                                                     Amount = p.Total_Price,
                                                                     Quantity = p.Quantity
                                                                 }
                                                                 ).ToList();

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                            {
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                            //                       where p.RentalMonth == current
                                            //                       && p.PropertyLinked == company.Name
                                            //                       select p).ToList();

                                            //if (rentalDataDumps.Count > 0)
                                            //{
                                            //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            //}

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;

                                    decimal? avg = null;

                                    if (amountProduct.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                                    {
                                        avg = amountProduct.Value / quantityProduct.Value;
                                    }

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, avg));

                                    currentDate = currentDate.AddMonths(1);
                                }

                                if (model.HideNoData)
                                {
                                    if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                    {
                                        continue;
                                    }
                                }
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.C01_ProductReport_Monthly_DetailsSubItems.Add(customerItem);
                    }

                    //if (item.C01_ProductReport_Monthly_DetailsSubItems.Count == 0)
                    //    continue;

                    model.C01_ProductReport_Monthly_DetailsItems.Add(item);
                }


                model.C01_ProductReport_Monthly_DetailsItems = model.C01_ProductReport_Monthly_DetailsItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_Monthly_Details_AVG.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_AuditSyncReport_Summary")]
        public async Task<IActionResult> C01_ProductReport_AuditSyncReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_AuditSyncReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_AuditSyncReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C01_ProductReport_AuditSyncReport_SummaryModel model = new C01_ProductReport_AuditSyncReport_SummaryModel()
            {
                C01_ProductReport_AuditSyncReport_SummaryItems = new List<C01_ProductReport_AuditSyncReport_SummaryModel.C01_ProductReport_AuditSyncReport_SummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var companyNames = db.ManagementAccountsDataDumps.Select(p => p.CompanyID).Distinct().ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var f_SystemGeneratedReports_ManagementAccounts_Requests = db.F_SystemGeneratedReports_ManagementAccounts_Requests.ToList();
            var f_SystemGeneratedReports_GenLedgerSync_Requests = db.F_SystemGeneratedReports_GenLedgerSync_Requests.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (!companyNames.Contains(company.CompanyID))
                    continue;

                if (company.SyncManagementAccounts.HasValue && !company.SyncManagementAccounts.Value)
                    continue;

                C01_ProductReport_AuditSyncReport_SummaryModel.C01_ProductReport_AuditSyncReport_SummaryItem item = new C01_ProductReport_AuditSyncReport_SummaryModel.C01_ProductReport_AuditSyncReport_SummaryItem()
                {
                    CompanyID = company.CompanyID,
                    Name = company.Name,
                    BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = company.BalanceMustBeAbove,
                    ExistsInSkybill = company.ExistsInSkybill,
                    Registrable = company.Registrable,
                    ServiceKey = company.ServiceKey,
                };

                var latestRequest = (from p in f_SystemGeneratedReports_ManagementAccounts_Requests
                                     where p.CompanyID == uC.CompanyID
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                var latestSync = (from p in db.SystemGeneratedReports
                                  join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
                                  from c in sc.DefaultIfEmpty()
                                  where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_ManagementAccounts
                                  && c.CompanyID == uC.CompanyID
                                  orderby p.DateStarted descending
                                  select new { c, p }).FirstOrDefault();

                if (latestRequest != null && latestSync == null)
                {
                    var opProf = opProfs.Where(p => p.UserID == latestRequest.CreatedBy).SingleOrDefault();
                    if (opProf != null)
                        item.LatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                    item.LatestReportRequestedDate = latestRequest.CreatedDate;
                    item.LatestReportStartedDate = latestRequest.DateStarted;
                    item.LatestReportCompletedDate = latestRequest.DateEnded;
                    item.LatestReportProgress = latestRequest.Progress;

                }
                else if (latestRequest == null && latestSync != null)
                {
                    item.LatestReportCompletedDate = latestSync.c.DateCompleted;
                    var latestSyncRequest = f_SystemGeneratedReports_ManagementAccounts_Requests.Where(p => p.SystemReportID == latestSync.p.ID).FirstOrDefault();

                    if (latestSyncRequest != null)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestSyncRequest.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.LatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.LatestReportRequestedDate = latestSyncRequest.CreatedDate;
                        item.LatestReportStartedDate = latestSyncRequest.DateStarted;
                        item.LatestReportCompletedDate = latestSyncRequest.DateEnded;
                        item.LatestReportProgress = latestSyncRequest.Progress;
                    }
                }
                else if (latestRequest != null && latestSync != null)
                {
                    item.LatestReportCompletedDate = latestSync.c.DateCompleted;
                    var latestSyncRequest = f_SystemGeneratedReports_ManagementAccounts_Requests.Where(p => p.SystemReportID == latestSync.p.ID).FirstOrDefault();

                    if (latestSyncRequest != null)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestSyncRequest.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.LatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.LatestReportRequestedDate = latestSyncRequest.CreatedDate;
                        item.LatestReportStartedDate = latestSyncRequest.DateStarted;
                        item.LatestReportCompletedDate = latestSyncRequest.DateEnded;
                        item.LatestReportProgress = latestSyncRequest.Progress;
                    }
                    else if (latestSync.c.DateCompleted <= latestRequest.CreatedDate)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestRequest.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.LatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.LatestReportRequestedDate = latestRequest.CreatedDate;
                        item.LatestReportStartedDate = latestRequest.DateStarted;
                        item.LatestReportCompletedDate = latestRequest.DateEnded;
                        item.LatestReportProgress = latestRequest.Progress;
                    }
                }

                var latestRequestGen = (from p in f_SystemGeneratedReports_GenLedgerSync_Requests
                                        where p.CompanyID == uC.CompanyID
                                        orderby p.CreatedDate descending
                                        select p).FirstOrDefault();

                var latestSyncGen = (from p in db.SystemGeneratedReports
                                     join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
                                     from c in sc.DefaultIfEmpty()
                                     where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_GenLedgerSync
                                     && c.CompanyID == uC.CompanyID
                                     orderby c.DateCompleted descending
                                     select new { c, p }).FirstOrDefault();


                if (latestRequestGen != null && latestSyncGen == null)
                {
                    var opProf = opProfs.Where(p => p.UserID == latestRequestGen.CreatedBy).SingleOrDefault();
                    if (opProf != null)
                        item.GenLatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                    item.GenLatestReportRequestedDate = latestRequestGen.CreatedDate;
                    item.GenLatestReportStartedDate = latestRequestGen.DateStarted;
                    item.GenLatestReportCompletedDate = latestRequestGen.DateEnded;
                    item.GenLatestReportProgress = latestRequestGen.Progress;

                }
                else if (latestRequestGen == null && latestSyncGen != null)
                {
                    item.GenLatestReportCompletedDate = latestSyncGen.c.DateCompleted;
                    var latestSyncRequest = f_SystemGeneratedReports_ManagementAccounts_Requests.Where(p => p.SystemReportID == latestSyncGen.p.ID).FirstOrDefault();

                    if (latestSyncRequest != null)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestSyncRequest.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.GenLatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.GenLatestReportRequestedDate = latestSyncRequest.CreatedDate;
                        item.GenLatestReportStartedDate = latestSyncRequest.DateStarted;
                        item.GenLatestReportCompletedDate = latestSyncRequest.DateEnded;
                        item.GenLatestReportProgress = latestSyncRequest.Progress;
                    }
                }
                else if (latestRequestGen != null && latestSyncGen != null)
                {
                    item.GenLatestReportCompletedDate = latestSyncGen.c.DateCompleted;
                    var latestSyncRequest = f_SystemGeneratedReports_ManagementAccounts_Requests.Where(p => p.SystemReportID == latestSyncGen.p.ID).FirstOrDefault();

                    if (latestSyncRequest != null)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestSyncRequest.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.GenLatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.GenLatestReportRequestedDate = latestSyncRequest.CreatedDate;
                        item.GenLatestReportStartedDate = latestSyncRequest.DateStarted;
                        item.GenLatestReportCompletedDate = latestSyncRequest.DateEnded;
                        item.GenLatestReportProgress = latestSyncRequest.Progress;
                    }
                    else if (latestSyncGen.c.DateCompleted <= latestRequestGen.CreatedDate)
                    {
                        var opProf = opProfs.Where(p => p.UserID == latestRequestGen.CreatedBy).SingleOrDefault();
                        if (opProf != null)
                            item.GenLatestReportRequestedBy = $"{opProf.FirstName} {opProf.LastName}";
                        item.GenLatestReportRequestedDate = latestRequestGen.CreatedDate;
                        item.GenLatestReportStartedDate = latestRequestGen.DateStarted;
                        item.GenLatestReportCompletedDate = latestRequestGen.DateEnded;
                        item.GenLatestReportProgress = latestRequestGen.Progress;
                    }
                }

                model.C01_ProductReport_AuditSyncReport_SummaryItems.Add(item);
            }

            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_AuditSyncReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_AuditSyncReport_Details")]
        public async Task<IActionResult> C01_ProductReport_AuditSyncReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_AuditSyncReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_AuditSyncReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.Where(p => p.FinancialCategoryID.HasValue && p.FinancialCategoryID == (int)ManagementAccounts_ReportingCategory_FinancialCategoryEnum.Income).ToList();

            int forecastType = !string.IsNullOrEmpty(Request.Query["ForecastType"]) ? Convert.ToInt32(Request.Query["ForecastType"]) : 1;
            C01_ProductReport_AuditSyncReport_DetailsModel model = new C01_ProductReport_AuditSyncReport_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingDescriptions = new List<string>(),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
                AllReportingDescriptions = new List<C01_ProductReport_AuditSyncReport_DetailsModel.ReportingDescriptionitem>(),
                C01_ProductReport_AuditSyncReport_DetailsItems = new List<C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem>(),
                ReportingParentDescriptionItems_GrossProfit = new List<C01_ProductReport_AuditSyncReport_DetailsModel.ReportingParentDescriptionItem>(),
                Total_GrossProfit = new C01_ProductReport_AuditSyncReport_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                },
                ForecastType = new List<SelectListItem>()
                {
                    new SelectListItem("Forecast 1", "1", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "1"),
                    new SelectListItem("Forecast 2", "2", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "2"),
                    new SelectListItem("Forecast 3", "3", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "3"),
                    new SelectListItem("Forecast 4", "4", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "4"),
                    new SelectListItem("Forecast 5", "5", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "5"),
                },
                ForecastTypeID = forecastType,
            };

            if (_operationalProvider.CompanyID != 0)
            {
                foreach (var repDesc in reportingDescriptions)
                {
                    model.AllReportingDescriptions.Add(new C01_ProductReport_AuditSyncReport_DetailsModel.ReportingDescriptionitem()
                    {
                        ID = repDesc.ID,
                        DisplayName = repDesc.ReportingDescription,
                    });
                }

                if (!string.IsNullOrEmpty(Request.Query["from"]))
                    model.FromDate = Convert.ToDateTime(Request.Query["from"]);

                if (!string.IsNullOrEmpty(Request.Query["to"]))
                    model.ToDate = Convert.ToDateTime(Request.Query["to"]);

                if (!string.IsNullOrEmpty(Request.Query["productID"]))
                {
                    model.ReportingDescriptions = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                else
                {
                    model.ReportingDescriptions = model.AllReportingDescriptions.Select(p => p.ID.ToString()).ToList();
                }

                #region Big Select

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   join rc in db.ManagementAccounts_ReportingCategories on p.ReportingCategoryID equals rc.ID into src
                                                   from rc in src.DefaultIfEmpty()
                                                   join rd in db.ManagementAccounts_ReportingDescriptions on p.ReportingDescriptionID equals rd.ID into srd
                                                   from rd in srd.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && p.CompanyID == _operationalProvider.CompanyID
                                                   select new
                                                   {
                                                       p.ID,
                                                       p.CompanyID,
                                                       p.PropertyType,
                                                       p.Province,
                                                       p.LocalMunicipality,
                                                       p.LegalEntity,
                                                       p.Partner,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       p.AccountNo,
                                                       p.Reference,
                                                       p.Basis,
                                                       p.SourceName,
                                                       p.Date,
                                                       p.ActualRegisteredUnits,
                                                       p.ActualMeteringPoints,
                                                       p.ActualAmount,
                                                       p.ActualAmountPerRegisteredUnit,
                                                       p.ActualAmountPerMeteringPoint,
                                                       p.Forecast1RegisteredUnits,
                                                       p.Forecast1MeteringPoints,
                                                       p.Forecast1Amount,
                                                       p.Forecast1AmountPerRegisteredUnit,
                                                       p.Forecast1AmountPerMeteringPoint,
                                                       p.Forecast2RegisteredUnits,
                                                       p.Forecast2MeteringPoints,
                                                       p.Forecast2Amount,
                                                       p.Forecast2AmountPerRegisteredUnit,
                                                       p.Forecast2AmountPerMeteringPoint,
                                                       p.Forecast3RegisteredUnits,
                                                       p.Forecast3MeteringPoints,
                                                       p.Forecast3Amount,
                                                       p.Forecast3AmountPerRegisteredUnit,
                                                       p.Forecast3AmountPerMeteringPoint,
                                                       p.Forecast4RegisteredUnits,
                                                       p.Forecast4MeteringPoints,
                                                       p.Forecast4Amount,
                                                       p.Forecast4AmountPerRegisteredUnit,
                                                       p.Forecast4AmountPerMeteringPoint,
                                                       p.Forecast5RegisteredUnits,
                                                       p.Forecast5MeteringPoints,
                                                       p.Forecast5Amount,
                                                       p.Forecast5AmountPerRegisteredUnit,
                                                       p.Forecast5AmountPerMeteringPoint,
                                                       p.DateActualAmountsSynced,
                                                       p.ReviewedByActual,
                                                       p.ReviewedDateActual,
                                                       p.ApprovedByActual,
                                                       p.ApprovedDateActual,
                                                       p.AuditByActual,
                                                       p.AuditDateActual,
                                                       p.ReviewedByForecast1,
                                                       p.ReviewedDateForecast1,
                                                       p.ApprovedByForecast1,
                                                       p.ApprovedDateForecast1,
                                                       p.AuditByForecast1,
                                                       p.AuditDateForecast1,
                                                       p.ReviewedByForecast2,
                                                       p.ReviewedDateForecast2,
                                                       p.ApprovedByForecast2,
                                                       p.ApprovedDateForecast2,
                                                       p.AuditByForecast2,
                                                       p.AuditDateForecast2,
                                                       p.ReviewedByForecast3,
                                                       p.ReviewedDateForecast3,
                                                       p.ApprovedByForecast3,
                                                       p.ApprovedDateForecast3,
                                                       p.AuditByForecast3,
                                                       p.AuditDateForecast3,
                                                       p.ReviewedByForecast4,
                                                       p.ReviewedDateForecast4,
                                                       p.ApprovedByForecast4,
                                                       p.ApprovedDateForecast4,
                                                       p.AuditByForecast4,
                                                       p.AuditDateForecast4,
                                                       p.ReviewedByForecast5,
                                                       p.ReviewedDateForecast5,
                                                       p.ApprovedByForecast5,
                                                       p.ApprovedDateForecast5,
                                                       p.AuditByForecast5,
                                                       p.AuditDateForecast5,
                                                       p.DateForecast1AmountsSynced,
                                                       p.DateForecast2AmountsSynced,
                                                       p.DateForecast3AmountsSynced,
                                                       p.DateForecast4AmountsSynced,
                                                       p.DateForecast5AmountsSynced,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();

                #endregion

                var managementAccounts_ReportingParentDescriptions = db.ManagementAccounts_ReportingParentDescriptions.ToList();
                var managementAccounts_ReportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

                C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem C01_ProductReport_AuditSyncReport_DetailsItem_Summary = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem()
                {
                    Sales_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "Sales_Actual",
                    },
                    CostOfSales_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "CostOfSales_Actual",
                    },
                    GrossProfit_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Actual",
                    },
                    GrossProfit_Actual_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Actual_Perc",
                    },
                    Sales_Forecast1 = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "Sales_Forecast1",
                    },
                    GrossProfit_Forecast1 = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Forecast1",
                    },
                    GrossProfit_Forecast1_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Forecast1",
                    },
                    GrossProfit_Difference = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Difference",
                    },
                    GrossProfit_Difference_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                        ReportingCategory = "GrossProfit_Difference",
                    },
                    ReportingDescription = "Summary",
                    ReportingDescriptionCodeName = "0",
                };

                DateTime current = model.FromDate;

                foreach (var repDesc in model.ReportingDescriptions)
                {
                    var repDescItem = model.AllReportingDescriptions.Where(p => p.ID == Convert.ToInt32(repDesc)).SingleOrDefault();

                    var managementAccountsDataDumpsAllForDesc = (from p in managementAccountsDataDumps
                                                                 where p.ReportingDescriptionID == repDescItem.ID
                                                                 select p).ToList();

                    if (managementAccountsDataDumpsAllForDesc.Count == 0)
                        continue;

                    C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem C01_ProductReport_AuditSyncReport_DetailsItem = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem()
                    {
                        Sales_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "Sales_Actual",
                        },
                        CostOfSales_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "CostOfSales_Actual",
                        },
                        GrossProfit_Actual = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Actual",
                        },
                        GrossProfit_Actual_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Actual_Perc",
                        },
                        Sales_Forecast1 = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "Sales_Forecast1",
                        },
                        GrossProfit_Forecast1 = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Forecast1",
                        },
                        GrossProfit_Forecast1_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Forecast1",
                        },
                        GrossProfit_Difference = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Difference",
                        },
                        GrossProfit_Difference_Perc = new C01_ProductReport_AuditSyncReport_DetailsModel.C01_ProductReport_AuditSyncReport_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, Tuple<decimal?, string>>(),
                            ReportingCategory = "GrossProfit_Difference",
                        },
                        ReportingDescription = repDescItem.DisplayName,
                        ReportingDescriptionCodeName = repDesc,
                    };

                    current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        string cellClass = "table-success";

                        #region Sales_Actual

                        var sales_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                            where p.ReportingCategory == "Sales"
                                            && p.Date.Date == current.Date
                                            select p).ToList();

                        if (sales_Actual.Count != 0)
                        {
                            cellClass = "table-success";
                            if (!sales_Actual[0].ReviewedDateActual.HasValue)
                                cellClass = "table-danger";
                            else if (!sales_Actual[0].ApprovedDateActual.HasValue)
                                cellClass = "table-warning";
                            else if (!sales_Actual[0].AuditDateActual.HasValue)
                                cellClass = "table-info";
                            C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(sales_Actual.Select(p => p.ActualAmount).Sum(), cellClass));
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region CostOfSales_Actual

                        var CostOfSales_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                                  where p.ReportingCategory == "Cost of Sales"
                                                  && p.Date == current
                                                  select p).ToList();

                        if (CostOfSales_Actual.Count != 0)
                        {
                            cellClass = "table-success";
                            if (!CostOfSales_Actual[0].ReviewedDateActual.HasValue)
                                cellClass = "table-danger";
                            else if (!CostOfSales_Actual[0].ApprovedDateActual.HasValue)
                                cellClass = "table-warning";
                            else if (!CostOfSales_Actual[0].AuditDateActual.HasValue)
                                cellClass = "table-info";
                            C01_ProductReport_AuditSyncReport_DetailsItem.CostOfSales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(CostOfSales_Actual.Select(p => p.ActualAmount).Sum(), cellClass));
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.CostOfSales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region GrossProfit_Actual

                        var GrossProfit_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                                  where p.ReportingCategory == "Gross Profit"
                                                  && p.Date == current
                                                  select p).ToList();

                        if (GrossProfit_Actual.Count != 0)
                        {
                            cellClass = "table-success";
                            if (!GrossProfit_Actual[0].ReviewedDateForecast1.HasValue)
                                cellClass = "table-danger";
                            else if (!GrossProfit_Actual[0].ApprovedDateForecast1.HasValue)
                                cellClass = "table-warning";
                            else if (!GrossProfit_Actual[0].AuditDateForecast1.HasValue)
                                cellClass = "table-info";
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Actual.Select(p => p.ActualAmount).Sum(), cellClass));
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region GrossProfit_Actual_Perc

                        var GrossProfit_Actual_Perc = (from p in managementAccountsDataDumpsAllForDesc
                                                       where p.ReportingCategory == "Gross Profit %"
                                                       && p.Date == current
                                                       select p).ToList();

                        if (GrossProfit_Actual_Perc.Count != 0)
                        {
                            cellClass = "table-success";
                            if (!GrossProfit_Actual_Perc[0].ReviewedDateForecast1.HasValue)
                                cellClass = "table-danger";
                            else if (!GrossProfit_Actual_Perc[0].ApprovedDateForecast1.HasValue)
                                cellClass = "table-warning";
                            else if (!GrossProfit_Actual_Perc[0].AuditDateForecast1.HasValue)
                                cellClass = "table-info";
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Actual_Perc.Select(p => p.ActualAmount).Sum(), cellClass));
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region Sales_Forecast1

                        var Sales_Forecast1 = (from p in managementAccountsDataDumpsAllForDesc
                                               where p.ReportingCategory == "Sales"
                                               && p.Date == current
                                               select p).ToList();

                        if (Sales_Forecast1.Count != 0)
                        {
                            switch (forecastType)
                            {
                                case 1:
                                    cellClass = "table-success";
                                    if (!Sales_Forecast1[0].ReviewedDateForecast1.HasValue)
                                        cellClass = "table-danger";
                                    else if (!Sales_Forecast1[0].ApprovedDateForecast1.HasValue)
                                        cellClass = "table-warning";
                                    else if (!Sales_Forecast1[0].AuditDateForecast1.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1.Select(p => p.Forecast1Amount).Sum(), cellClass));
                                    break;
                                case 2:
                                    cellClass = "table-success";
                                    if (!Sales_Forecast1[0].ReviewedDateForecast2.HasValue)
                                        cellClass = "table-danger";
                                    else if (!Sales_Forecast1[0].ApprovedDateForecast2.HasValue)
                                        cellClass = "table-warning";
                                    else if (!Sales_Forecast1[0].AuditDateForecast2.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1.Select(p => p.Forecast2Amount).Sum(), cellClass));
                                    break;
                                case 3:
                                    cellClass = "table-success";
                                    if (!Sales_Forecast1[0].ReviewedDateForecast3.HasValue)
                                        cellClass = "table-danger";
                                    else if (!Sales_Forecast1[0].ApprovedDateForecast3.HasValue)
                                        cellClass = "table-warning";
                                    else if (!Sales_Forecast1[0].AuditDateForecast3.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1.Select(p => p.Forecast3Amount).Sum(), cellClass));
                                    break;
                                case 4:
                                    cellClass = "table-success";
                                    if (!Sales_Forecast1[0].ReviewedDateForecast4.HasValue)
                                        cellClass = "table-danger";
                                    else if (!Sales_Forecast1[0].ApprovedDateForecast4.HasValue)
                                        cellClass = "table-warning";
                                    else if (!Sales_Forecast1[0].AuditDateForecast4.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1.Select(p => p.Forecast4Amount).Sum(), cellClass));
                                    break;
                                case 5:
                                    cellClass = "table-success";
                                    if (!Sales_Forecast1[0].ReviewedDateForecast5.HasValue)
                                        cellClass = "table-danger";
                                    else if (!Sales_Forecast1[0].ApprovedDateForecast5.HasValue)
                                        cellClass = "table-warning";
                                    else if (!Sales_Forecast1[0].AuditDateForecast5.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1.Select(p => p.Forecast5Amount).Sum(), cellClass));
                                    break;
                            }
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region GrossProfit_Forecast1

                        var GrossProfit_Forecast1 = (from p in managementAccountsDataDumpsAllForDesc
                                                     where p.ReportingCategory == "Gross Profit"
                                                     && p.Date == current
                                                     select p).ToList();

                        if (GrossProfit_Forecast1.Count != 0)
                        {
                            switch (forecastType)
                            {
                                case 1:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1[0].ReviewedDateForecast1.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1[0].ApprovedDateForecast1.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1[0].AuditDateForecast1.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1.Select(p => p.Forecast1Amount).Sum(), cellClass));
                                    break;
                                case 2:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1[0].ReviewedDateForecast2.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1[0].ApprovedDateForecast2.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1[0].AuditDateForecast2.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1.Select(p => p.Forecast2Amount).Sum(), cellClass));
                                    break;
                                case 3:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1[0].ReviewedDateForecast3.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1[0].ApprovedDateForecast3.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1[0].AuditDateForecast3.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1.Select(p => p.Forecast3Amount).Sum(), cellClass));
                                    break;
                                case 4:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1[0].ReviewedDateForecast4.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1[0].ApprovedDateForecast4.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1[0].AuditDateForecast4.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1.Select(p => p.Forecast4Amount).Sum(), cellClass));
                                    break;
                                case 5:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1[0].ReviewedDateForecast5.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1[0].ApprovedDateForecast5.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1[0].AuditDateForecast5.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1.Select(p => p.Forecast5Amount).Sum(), cellClass));
                                    break;
                            }
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        #region GrossProfit_Forecast1_Perc

                        var GrossProfit_Forecast1_Perc = (from p in managementAccountsDataDumpsAllForDesc
                                                          where p.ReportingCategory == "Gross Profit %"
                                                          && p.Date == current
                                                          select p).ToList();

                        if (GrossProfit_Forecast1_Perc.Count != 0)
                        {
                            switch (forecastType)
                            {
                                case 1:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1_Perc[0].ReviewedDateForecast1.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1_Perc[0].ApprovedDateForecast1.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1_Perc[0].AuditDateForecast1.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc.Select(p => p.Forecast1Amount).Sum(), cellClass));
                                    break;
                                case 2:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1_Perc[0].ReviewedDateForecast2.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1_Perc[0].ApprovedDateForecast2.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1_Perc[0].AuditDateForecast2.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc.Select(p => p.Forecast2Amount).Sum(), cellClass));
                                    break;
                                case 3:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1_Perc[0].ReviewedDateForecast3.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1_Perc[0].ApprovedDateForecast3.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1_Perc[0].AuditDateForecast3.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc.Select(p => p.Forecast3Amount).Sum(), cellClass));
                                    break;
                                case 4:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1_Perc[0].ReviewedDateForecast4.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1_Perc[0].ApprovedDateForecast4.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1_Perc[0].AuditDateForecast4.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc.Select(p => p.Forecast4Amount).Sum(), cellClass));
                                    break;
                                case 5:
                                    cellClass = "table-success";
                                    if (!GrossProfit_Forecast1_Perc[0].ReviewedDateForecast5.HasValue)
                                        cellClass = "table-danger";
                                    else if (!GrossProfit_Forecast1_Perc[0].ApprovedDateForecast5.HasValue)
                                        cellClass = "table-warning";
                                    else if (!GrossProfit_Forecast1_Perc[0].AuditDateForecast5.HasValue)
                                        cellClass = "table-info";
                                    C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc.Select(p => p.Forecast5Amount).Sum(), cellClass));
                                    break;
                            }
                        }
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                        #endregion

                        current = current.AddMonths(1);
                    }

                    #region Totals

                    current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        var Sales_Actual = C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Actual.MonthlyValues[current].Item1;
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Item1.Value + Sales_Actual, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Actual, ""));

                        var CostOfSales_Actual = C01_ProductReport_AuditSyncReport_DetailsItem.CostOfSales_Actual.MonthlyValues[current].Item1;
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues[current].Item1.Value + CostOfSales_Actual, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(CostOfSales_Actual, ""));

                        var GrossProfit_Actual = C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual.MonthlyValues[current].Item1;
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current].Item1.Value + GrossProfit_Actual, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Actual, ""));

                        var Sales_Forecast1 = C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues[current].Item1;
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Item1.Value + Sales_Forecast1, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(Sales_Forecast1, ""));

                        var GrossProfit_Forecast1 = C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current].Item1;
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.Value + GrossProfit_Forecast1, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1, ""));


                        #region GrossProfit_Actual_Perc

                        //var GrossProfit_Actual_Perc = Sales_Actual != 0 ? (GrossProfit_Actual / Sales_Actual) * 100.0m : 0;
                        //C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Actual_Perc, ""));

                        #endregion

                        #region GrossProfit_Forecast1_Perc

                        //var GrossProfit_Forecast1_Perc = Sales_Forecast1 != 0 ? (GrossProfit_Forecast1 / Sales_Forecast1) * 100.0m : 0;
                        //C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Forecast1_Perc, ""));

                        #endregion

                        #region GrossProfit_Difference

                        var GrossProfit_Difference = GrossProfit_Actual - GrossProfit_Forecast1;
                        C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Difference.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Difference, ""));
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues.ContainsKey(current))
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current] = new Tuple<decimal?, string>(C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current].Item1.Value + GrossProfit_Difference, "");
                        else
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Difference, ""));


                        #endregion

                        #region GrossProfit_Difference_Perc

                        var GrossProfit_Difference_Perc = GrossProfit_Difference.HasValue && GrossProfit_Forecast1.HasValue && GrossProfit_Forecast1.Value != 0 ? (GrossProfit_Difference.Value / GrossProfit_Forecast1.Value) * 100.0m : 0;
                        C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Difference_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(GrossProfit_Difference_Perc, ""));

                        #endregion

                        current = current.AddMonths(1);
                    }


                    #endregion

                    if (
                        model.HideNoData
                        && C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Actual.MonthlyValues.Values.Where(p => p.Item1.HasValue && p.Item1.Value != 0).Count() == 0
                        && C01_ProductReport_AuditSyncReport_DetailsItem.CostOfSales_Actual.MonthlyValues.Values.Where(p => p.Item1.HasValue && p.Item1.Value != 0).Count() == 0
                        && C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Actual.MonthlyValues.Values.Where(p => p.Item1.HasValue && p.Item1.Value != 0).Count() == 0
                        && C01_ProductReport_AuditSyncReport_DetailsItem.Sales_Forecast1.MonthlyValues.Values.Where(p => p.Item1.HasValue && p.Item1.Value != 0).Count() == 0
                        && C01_ProductReport_AuditSyncReport_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Values.Where(p => p.Item1.HasValue && p.Item1.Value != 0).Count() == 0
                        )
                        continue;

                    model.C01_ProductReport_AuditSyncReport_DetailsItems.Add(C01_ProductReport_AuditSyncReport_DetailsItem);
                }

                current = model.FromDate;

                while (current <= model.ToDate)
                {
                    if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues.ContainsKey(current))
                    {
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current].Item1.HasValue && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Item1.HasValue && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Item1.Value != 0)
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual_Perc.MonthlyValues[current] = new Tuple<decimal?, string>((C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current].Item1.Value / C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Item1.Value) * 100.0m, "");
                    }
                    else
                        C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Actual_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                    if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.ContainsKey(current))
                    {
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.HasValue && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Item1.HasValue && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Item1.Value != 0)
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1_Perc.MonthlyValues[current] = new Tuple<decimal?, string>((C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.Value / C01_ProductReport_AuditSyncReport_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Item1.Value) * 100.0m, "");
                    }
                    else
                        C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                    if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.ContainsKey(current) && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues.ContainsKey(current))
                    {
                        if (C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.HasValue && C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.Value != 0)
                            C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference_Perc.MonthlyValues[current] = new Tuple<decimal?, string>((C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current].Item1.Value / C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Item1.Value) * 100.0m, "");
                    }
                    else
                        C01_ProductReport_AuditSyncReport_DetailsItem_Summary.GrossProfit_Difference_Perc.MonthlyValues.Add(current, new Tuple<decimal?, string>(0, ""));

                    current = current.AddMonths(1);
                }

                model.C01_ProductReport_AuditSyncReport_DetailsItems.Insert(0, C01_ProductReport_AuditSyncReport_DetailsItem_Summary);

                #region _GrossProfit

                List<int> parentReportingDescriptions_GrossProfit = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_GrossProfit = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_GrossProfit.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_GrossProfit = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_GrossProfit.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_GrossProfit = (from p in managementAccountsDataDumps
                                                               where reportingDescriptionsToCheck_GrossProfit.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                               && p.ReportingCategoryID == 4 // Gross Profit
                                                               select p).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck_GrossProfit)
                {
                    C01_ProductReport_AuditSyncReport_DetailsModel.ReportingParentDescriptionItem parentDescriptionItem = new C01_ProductReport_AuditSyncReport_DetailsModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C01_ProductReport_AuditSyncReport_DetailsModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C01_ProductReport_AuditSyncReport_DetailsModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_GrossProfit.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C01_ProductReport_AuditSyncReport_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C01_ProductReport_AuditSyncReport_DetailsModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_GrossProfit
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_GrossProfit.MonthlyValues.ContainsKey(current))
                                model.Total_GrossProfit.MonthlyValues[current] = model.Total_GrossProfit.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_GrossProfit.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }
                        if (reportingCategoryItem.MonthlyValues.Select(p => p.Value).Sum() != 0)
                            parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_GrossProfit.Add(parentDescriptionItem);
                }

                #endregion

            }
            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_AuditSyncReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C01_ProductReport_ProductAuditViewModel model = new C01_ProductReport_ProductAuditViewModel()
            {
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month)),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                ApprovalRequired = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Statuses", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) },
                    new SelectListItem() { Text = $"Approved Required", Value = $"1", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"1" },
                    new SelectListItem() { Text = $"Reviewed Required", Value = $"2", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"2" },
                    new SelectListItem() { Text = $"Confirmed Amount Required", Value = $"3", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"3" },
                },
                C01_ProductReport_ProductAuditViewItems = new List<C01_ProductReport_ProductAuditViewModel.C01_ProductReport_ProductAuditViewItem>(),
                ReportingCategoryID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Reporting Categories", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["ReportingCategoryID"]) },
                },
                ReportingDescriptionID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Reporting Descriptions", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["ReportingDescriptionID"]) },
                },
            };

            var db = new MyVoltageDbContext(_options);

            var managementAccounts_ReportingCategories = db.ManagementAccounts_ReportingCategories.ToList();
            var managementAccounts_ReportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                                            join c in db.ManagementAccounts_ReportingParentDescriptions on p.ParentReportingDescriptionID equals c.ID into sc
                                                            from c in sc.DefaultIfEmpty()
                                                            select new { p, c }).ToList();

            model.ReportingCategoryID.AddRange((from p in managementAccounts_ReportingCategories
                                                select new
                                                SelectListItem()
                                                {
                                                    Text = $"{p.ReportingCategory}",
                                                    Value = p.ID.ToString(),
                                                    Selected = !string.IsNullOrEmpty(Request.Query["ReportingCategoryID"]) && Request.Query["ReportingCategoryID"].ToString() == p.ID.ToString(),
                                                }).ToList());

            model.ReportingCategoryID = model.ReportingCategoryID.OrderBy(p => p.Text).ToList();

            model.ReportingDescriptionID.AddRange((from p in managementAccounts_ReportingDescriptions
                                                   select new
                                                   SelectListItem()
                                                   {
                                                       Text = p.c != null ? $"{p.c.ReportingParentDescription} - {p.p.ReportingDescription}" : $"{p.p.ReportingDescription}",
                                                       Value = p.p.ID.ToString(),
                                                       Selected = !string.IsNullOrEmpty(Request.Query["ReportingDescriptionID"]) && Request.Query["ReportingDescriptionID"].ToString() == p.p.ID.ToString(),
                                                   }).ToList());
            model.ReportingDescriptionID = model.ReportingDescriptionID.OrderBy(p => p.Text).ToList();

            if (_operationalProvider.CompanyID != 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var latestRequest = (from p in db.F_SystemGeneratedReports_ManagementAccounts_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     && p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new C01_ProductReport_ProductAuditViewModel.F_SystemGeneratedReports_ManagementAccounts_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latestRequest.CreatedBy,
                        CompanyID = latestRequest.CompanyID,
                        CreatedDate = latestRequest.CreatedDate,
                        DateEnded = latestRequest.DateEnded,
                        DateStarted = latestRequest.DateStarted,
                        FromDate = latestRequest.FromDate,
                        ID = latestRequest.ID,
                        Progress = latestRequest.Progress,
                        SystemReportID = latestRequest.SystemReportID,
                        ToDate = latestRequest.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();
                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && p.CompanyID == _operationalProvider.CompanyID
                                                   select new
                                                   {
                                                       CompanyName = c.Name,
                                                       p,
                                                   }).ToList();

                if (!string.IsNullOrEmpty(Request.Query["ApprovalRequired"]))
                {
                    if (Request.Query["ApprovalRequired"].ToString() == $"1") // Approved Required
                        managementAccountsDataDumps = managementAccountsDataDumps.Where(p =>
                        (!p.p.ApprovedDateActual.HasValue)
                        || (!p.p.ApprovedDateForecast1.HasValue)
                        || (!p.p.ApprovedDateForecast2.HasValue)
                        || (!p.p.ApprovedDateForecast3.HasValue)
                        || (!p.p.ApprovedDateForecast4.HasValue)
                        || (!p.p.ApprovedDateForecast5.HasValue)
                        ).ToList();
                    else if (Request.Query["ApprovalRequired"].ToString() == $"2") // Reviewed Required
                        managementAccountsDataDumps = managementAccountsDataDumps.Where(p =>
                        (!p.p.ReviewedDateActual.HasValue)
                        || (!p.p.ReviewedDateForecast1.HasValue)
                        || (!p.p.ReviewedDateForecast2.HasValue)
                        || (!p.p.ReviewedDateForecast3.HasValue)
                        || (!p.p.ReviewedDateForecast4.HasValue)
                        || (!p.p.ReviewedDateForecast5.HasValue)
                        ).ToList();
                    else if (Request.Query["ApprovalRequired"].ToString() == $"3") // Confirmed Amount Required
                        managementAccountsDataDumps = managementAccountsDataDumps.Where(p =>
                        (!p.p.AuditDateActual.HasValue)
                        || (!p.p.AuditDateForecast1.HasValue)
                        || (!p.p.AuditDateForecast2.HasValue)
                        || (!p.p.AuditDateForecast3.HasValue)
                        || (!p.p.AuditDateForecast4.HasValue)
                        || (!p.p.AuditDateForecast5.HasValue)
                        ).ToList();
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingCategoryID"]))
                    managementAccountsDataDumps = managementAccountsDataDumps.Where(p => Request.Query["ReportingCategoryID"].ToString() == p.p.ReportingCategoryID.ToString()).ToList();

                if (!string.IsNullOrEmpty(Request.Query["ReportingDescriptionID"]))
                    managementAccountsDataDumps = managementAccountsDataDumps.Where(p => Request.Query["ReportingDescriptionID"].ToString() == p.p.ReportingDescriptionID.ToString()).ToList();

                managementAccountsDataDumps = managementAccountsDataDumps.OrderBy(p => p.CompanyName).ThenBy(p => p.p.Date).ThenBy(p => p.p.ReportingCategoryID).ThenBy(p => p.p.ReportingDescriptionID).ToList();

                foreach (var ac in managementAccountsDataDumps)
                {
                    C01_ProductReport_ProductAuditViewModel.C01_ProductReport_ProductAuditViewItem item = new C01_ProductReport_ProductAuditViewModel.C01_ProductReport_ProductAuditViewItem()
                    {
                        CompanyName = ac.CompanyName,
                        AccountNo = ac.p.AccountNo,
                        ActualAmount = ac.p.ActualAmount,
                        ActualAmountPerMeteringPoint = ac.p.ActualAmountPerMeteringPoint,
                        ActualAmountPerRegisteredUnit = ac.p.ActualAmountPerRegisteredUnit,
                        ActualMeteringPoints = ac.p.ActualMeteringPoints,
                        ActualRegisteredUnits = ac.p.ActualRegisteredUnits,
                        ApprovedByActual = ac.p.ApprovedByActual,
                        ApprovedByForecast1 = ac.p.ApprovedByForecast1,
                        ApprovedByForecast2 = ac.p.ApprovedByForecast2,
                        ApprovedByForecast3 = ac.p.ApprovedByForecast3,
                        ApprovedByForecast4 = ac.p.ApprovedByForecast4,
                        ApprovedByForecast5 = ac.p.ApprovedByForecast5,
                        ApprovedByUsernameActual = "",
                        ApprovedByUsernameForecast1 = "",
                        ApprovedByUsernameForecast2 = "",
                        ApprovedByUsernameForecast3 = "",
                        ApprovedByUsernameForecast4 = "",
                        ApprovedByUsernameForecast5 = "",
                        ApprovedDateActual = ac.p.ApprovedDateActual,
                        ApprovedDateForecast1 = ac.p.ApprovedDateForecast1,
                        ApprovedDateForecast2 = ac.p.ApprovedDateForecast2,
                        ApprovedDateForecast3 = ac.p.ApprovedDateForecast3,
                        ApprovedDateForecast4 = ac.p.ApprovedDateForecast4,
                        ApprovedDateForecast5 = ac.p.ApprovedDateForecast5,
                        AuditByActual = ac.p.AuditByActual,
                        AuditByForecast1 = ac.p.AuditByForecast1,
                        AuditByForecast2 = ac.p.AuditByForecast2,
                        AuditByForecast3 = ac.p.AuditByForecast3,
                        AuditByForecast4 = ac.p.AuditByForecast4,
                        AuditByForecast5 = ac.p.AuditByForecast5,
                        AuditByUsernameActual = "",
                        AuditByUsernameForecast1 = "",
                        AuditByUsernameForecast2 = "",
                        AuditByUsernameForecast3 = "",
                        AuditByUsernameForecast4 = "",
                        AuditByUsernameForecast5 = "",
                        AuditDateActual = ac.p.AuditDateActual,
                        AuditDateForecast1 = ac.p.AuditDateForecast1,
                        AuditDateForecast2 = ac.p.AuditDateForecast2,
                        AuditDateForecast3 = ac.p.AuditDateForecast3,
                        AuditDateForecast4 = ac.p.AuditDateForecast4,
                        AuditDateForecast5 = ac.p.AuditDateForecast5,
                        Basis = ac.p.Basis,
                        CompanyID = ac.p.CompanyID,
                        Date = ac.p.Date,
                        DateActualAmountsSynced = ac.p.DateActualAmountsSynced,
                        Forecast1Amount = ac.p.Forecast1Amount,
                        Forecast1AmountPerMeteringPoint = ac.p.Forecast1AmountPerMeteringPoint,
                        Forecast1AmountPerRegisteredUnit = ac.p.Forecast1AmountPerRegisteredUnit,
                        Forecast1MeteringPoints = ac.p.Forecast1MeteringPoints,
                        Forecast1RegisteredUnits = ac.p.Forecast1RegisteredUnits,
                        Forecast2Amount = ac.p.Forecast2Amount,
                        Forecast2AmountPerMeteringPoint = ac.p.Forecast2AmountPerMeteringPoint,
                        Forecast2AmountPerRegisteredUnit = ac.p.Forecast2AmountPerRegisteredUnit,
                        Forecast2MeteringPoints = ac.p.Forecast2MeteringPoints,
                        Forecast2RegisteredUnits = ac.p.Forecast2RegisteredUnits,
                        Forecast3Amount = ac.p.Forecast3Amount,
                        Forecast3AmountPerMeteringPoint = ac.p.Forecast3AmountPerMeteringPoint,
                        Forecast3AmountPerRegisteredUnit = ac.p.Forecast3AmountPerRegisteredUnit,
                        Forecast3MeteringPoints = ac.p.Forecast3MeteringPoints,
                        Forecast3RegisteredUnits = ac.p.Forecast3RegisteredUnits,
                        Forecast4Amount = ac.p.Forecast4Amount,
                        Forecast4AmountPerMeteringPoint = ac.p.Forecast4AmountPerMeteringPoint,
                        Forecast4AmountPerRegisteredUnit = ac.p.Forecast4AmountPerRegisteredUnit,
                        Forecast4MeteringPoints = ac.p.Forecast4MeteringPoints,
                        Forecast4RegisteredUnits = ac.p.Forecast4RegisteredUnits,
                        Forecast5Amount = ac.p.Forecast5Amount,
                        Forecast5AmountPerMeteringPoint = ac.p.Forecast5AmountPerMeteringPoint,
                        Forecast5AmountPerRegisteredUnit = ac.p.Forecast5AmountPerRegisteredUnit,
                        Forecast5MeteringPoints = ac.p.Forecast5MeteringPoints,
                        Forecast5RegisteredUnits = ac.p.Forecast5RegisteredUnits,
                        ID = ac.p.ID,
                        LegalEntity = ac.p.LegalEntity,
                        LocalMunicipality = ac.p.LocalMunicipality,
                        Partner = ac.p.Partner,
                        PropertyType = ac.p.PropertyType,
                        Province = ac.p.Province,
                        Reference = ac.p.Reference,
                        ReportingCategoryID = ac.p.ReportingCategoryID,
                        ReportingDescriptionID = ac.p.ReportingDescriptionID,
                        ReviewedByActual = ac.p.ReviewedByActual,
                        ReviewedByForecast1 = ac.p.ReviewedByForecast1,
                        ReviewedByForecast2 = ac.p.ReviewedByForecast2,
                        ReviewedByForecast3 = ac.p.ReviewedByForecast3,
                        ReviewedByForecast4 = ac.p.ReviewedByForecast4,
                        ReviewedByForecast5 = ac.p.ReviewedByForecast5,
                        ReviewedByUsernameActual = "",
                        ReviewedByUsernameForecast1 = "",
                        ReviewedByUsernameForecast2 = "",
                        ReviewedByUsernameForecast3 = "",
                        ReviewedByUsernameForecast4 = "",
                        ReviewedByUsernameForecast5 = "",
                        ReviewedDateActual = ac.p.ReviewedDateActual,
                        ReviewedDateForecast1 = ac.p.ReviewedDateForecast1,
                        ReviewedDateForecast2 = ac.p.ReviewedDateForecast2,
                        ReviewedDateForecast3 = ac.p.ReviewedDateForecast3,
                        ReviewedDateForecast4 = ac.p.ReviewedDateForecast4,
                        ReviewedDateForecast5 = ac.p.ReviewedDateForecast5,
                        SourceName = ac.p.SourceName,
                        DateForecast1AmountsSynced = ac.p.DateForecast1AmountsSynced,
                        DateForecast2AmountsSynced = ac.p.DateForecast2AmountsSynced,
                        DateForecast3AmountsSynced = ac.p.DateForecast3AmountsSynced,
                        DateForecast4AmountsSynced = ac.p.DateForecast4AmountsSynced,
                        DateForecast5AmountsSynced = ac.p.DateForecast5AmountsSynced,
                        ActualRatePerUnit = ac.p.ActualRatePerUnit,
                        ActualUnits = ac.p.ActualUnits,
                        Forecast1RatePerUnit = ac.p.Forecast1RatePerUnit,
                        Forecast1Units = ac.p.Forecast1Units,
                        Forecast2RatePerUnit = ac.p.Forecast2RatePerUnit,
                        Forecast2Units = ac.p.Forecast2Units,
                        Forecast3RatePerUnit = ac.p.Forecast3RatePerUnit,
                        Forecast3Units = ac.p.Forecast3Units,
                        Forecast4RatePerUnit = ac.p.Forecast4RatePerUnit,
                        Forecast4Units = ac.p.Forecast4Units,
                        Forecast5RatePerUnit = ac.p.Forecast5RatePerUnit,
                        Forecast5Units = ac.p.Forecast5Units,
                    };

                    var reportingCategory = managementAccounts_ReportingCategories.Where(p => p.ID == ac.p.ReportingCategoryID).SingleOrDefault();
                    if (reportingCategory != null)
                    {
                        item.ReportingCategory = reportingCategory.ReportingCategory;
                    }

                    var reportingDescription = managementAccounts_ReportingDescriptions.Where(p => p.p.ID == ac.p.ReportingDescriptionID).SingleOrDefault();
                    if (reportingDescription != null)
                    {
                        item.ReportingDescription = reportingDescription.c != null ? $"{reportingDescription.c.ReportingParentDescription} - {reportingDescription.p.ReportingDescription}" : $"{reportingDescription.p.ReportingDescription}";
                    }

                    #region Actual

                    if (!string.IsNullOrEmpty(item.ReviewedByActual))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByActual.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameActual = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByActual))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByActual.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameActual = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByActual))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByActual.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameActual = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast1

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast1))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast1.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast1 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast1))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast1.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast1 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast1))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast1.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast1 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast2

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast2))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast2.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast2 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast2))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast2.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast2 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast2))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast2.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast2 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast3

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast3))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast3.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast3 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast3))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast3.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast3 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast3))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast3.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast3 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast4

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast4))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast4.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast4 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast4))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast4.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast4 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast4))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast4.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast4 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast5

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast5))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast5.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast5 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast5))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast5.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast5 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast5))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast5.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast5 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    model.C01_ProductReport_ProductAuditViewItems.Add(item);
                }

                model.C01_ProductReport_ProductAuditViewItems = model.C01_ProductReport_ProductAuditViewItems.OrderBy(p => p.CompanyName).ThenByDescending(p => p.Date).ThenBy(p => p.ReportingDescriptionID).ThenBy(p => p.ReportingCategoryID).ToList();
            }

            return View("~/Views/Operational/C01_ProductReport/C01_ProductReport_ProductAuditView.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_RequestRerun")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                var latestDateItem = (from p in db.ManagementAccountsDataDumps
                                      where p.CompanyID == _operationalProvider.CompanyID
                                      orderby p.Date descending
                                      select p).FirstOrDefault();

                var toDate = latestDateItem != null ? latestDateItem.Date : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month));

                Data.F_SystemGeneratedReports_ManagementAccounts_Request f_SystemGeneratedReports_AccountingChecklist_Request = new F_SystemGeneratedReports_ManagementAccounts_Request()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(2018, 01, 01),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = toDate
                };

                db.Add(f_SystemGeneratedReports_AccountingChecklist_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView");
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Actual/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Actual(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByActual = _userManager.GetUserId(User);
                item.ReviewedDateActual = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Actual/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Actual(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByActual = _userManager.GetUserId(User);
                item.ApprovedDateActual = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Actual/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Actual(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByActual = _userManager.GetUserId(User);
                item.AuditDateActual = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Forecast1/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Forecast1(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByForecast1 = _userManager.GetUserId(User);
                item.ReviewedDateForecast1 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Forecast1/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Forecast1(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByForecast1 = _userManager.GetUserId(User);
                item.ApprovedDateForecast1 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Forecast1/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Forecast1(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByForecast1 = _userManager.GetUserId(User);
                item.AuditDateForecast1 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Forecast2/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Forecast2(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByForecast2 = _userManager.GetUserId(User);
                item.ReviewedDateForecast2 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Forecast2/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Forecast2(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByForecast2 = _userManager.GetUserId(User);
                item.ApprovedDateForecast2 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Forecast2/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Forecast2(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByForecast2 = _userManager.GetUserId(User);
                item.AuditDateForecast2 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Forecast3/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Forecast3(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByForecast3 = _userManager.GetUserId(User);
                item.ReviewedDateForecast3 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Forecast3/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Forecast3(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByForecast3 = _userManager.GetUserId(User);
                item.ApprovedDateForecast3 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Forecast3/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Forecast3(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByForecast3 = _userManager.GetUserId(User);
                item.AuditDateForecast3 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Forecast4/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Forecast4(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByForecast4 = _userManager.GetUserId(User);
                item.ReviewedDateForecast4 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Forecast4/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Forecast4(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByForecast4 = _userManager.GetUserId(User);
                item.ApprovedDateForecast4 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Forecast4/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Forecast4(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByForecast4 = _userManager.GetUserId(User);
                item.AuditDateForecast4 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Review_Forecast5/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Review_Forecast5(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ReviewedByForecast5 = _userManager.GetUserId(User);
                item.ReviewedDateForecast5 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Approve_Forecast5/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Approve_Forecast5(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByForecast5 = _userManager.GetUserId(User);
                item.ApprovedDateForecast5 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C01_ProductReport/C01_ProductReport_ProductAuditView_Audit_Forecast5/{ID}")]
        public async Task<IActionResult> C01_ProductReport_ProductAuditView_Audit_Forecast5(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C01_ProductReport_ProductAuditView, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C01_ProductReport_ProductAuditView}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByForecast5 = _userManager.GetUserId(User);
                item.AuditDateForecast5 = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C01_ProductReport/C01_ProductReport_ProductAuditView";
            return Redirect(HttpUtility.UrlDecode(ret));
        }
    }
}
