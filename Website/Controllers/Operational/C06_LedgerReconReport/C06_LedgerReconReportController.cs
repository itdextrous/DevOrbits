using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C06_LedgerReconReportModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.C06_LedgerReconReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C06_LedgerReconReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C06_LedgerReconReportController(
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
        [Route("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Summary")]
        public async Task<IActionResult> C06_LedgerReconReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_LedgerReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_LedgerReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C06_LedgerReconReport_SummaryModel model = new C06_LedgerReconReport_SummaryModel()
            {
                C06_LedgerReconReport_SummaryItems = new List<C06_LedgerReconReport_SummaryModel.C06_LedgerReconReport_SummaryItem>(),
            };

            //var products = (from p in db.SiteAdmin_Products
            //                where p.IncludeInC602x
            //                select p).ToList();

            //foreach (var uC in _operationalProvider.UserCompanies)
            //{
            //    var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

            //    C06_LedgerReconReport_SummaryModel.C06_LedgerReconReport_SummaryItem item = new C06_LedgerReconReport_SummaryModel.C06_LedgerReconReport_SummaryItem()
            //    {
            //        CompanyID = company.CompanyID,
            //        CompanyName = company.Name,
            //        LedgerTotal = 0,
            //        ProductsTotal = 0,
            //        CoaTotal = 0,
            //    };

            //    if (products.Count > 0)
            //    {
            //        var productLedgers = (from p in db.Report_ProductsResourceLedgerMonthlies
            //                              where products.Select(c => c.ID).Contains(p.ProductID)
            //                              && p.CompanyID == company.CompanyID
            //                              select p).ToList();

            //        if (productLedgers.Count > 0)
            //        {
            //            item.ProductsTotal = productLedgers.Select(p => p.Amount).Sum();
            //        }
            //    }

            //    var ledgers = (from p in db.Report_GeneralLedgerMonthlies
            //                   where p.CompanyID == company.CompanyID
            //                   && (p.GenLedgerNo == 6410 || p.GenLedgerNo == 5930)
            //                   select p).ToList();

            //    if (ledgers.Count > 0)
            //    {
            //        item.LedgerTotal = ledgers.Select(p => p.Amount).Sum();
            //    }

            //    var latestSync = (from p in db.SystemGeneratedReports
            //                      join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
            //                      from c in sc.DefaultIfEmpty()
            //                      where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync
            //                      && c.CompanyID == uC.CompanyID
            //                      orderby c.DateCompleted descending
            //                      select c).FirstOrDefault();

            //    if (latestSync != null)
            //        item.LatestSyncDate = latestSync.DateCompleted;

            //    var latestSyncMonthly = (from p in db.SystemGeneratedReports
            //                             join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
            //                             from c in sc.DefaultIfEmpty()
            //                             where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerMonthliesSync
            //                             && c.CompanyID == uC.CompanyID
            //                             orderby c.DateCompleted descending
            //                             select c).FirstOrDefault();

            //    if (latestSyncMonthly != null)
            //        item.LatestSyncDate_Monthly = latestSyncMonthly.DateCompleted;

            //    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
            //    var chartOfAccounts = skyBillApiClient.GetChartOfAccounts();
            //    var coa = chartOfAccounts.Where(p => p.No == "6410").SingleOrDefault();
            //    if (coa != null)
            //        item.CoaTotal = Convert.ToDecimal(coa.Balance_at_Date);

            //    model.C06_LedgerReconReport_SummaryItems.Add(item);
            //}

            //model.C06_LedgerReconReport_SummaryItems = model.C06_LedgerReconReport_SummaryItems.OrderBy(p => p.CompanyName).ToList();
            return View("~/Views/Operational/C06_LedgerReconReport/C06_LedgerReconReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_LedgerReconReport_SummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> C06_LedgerReconReport_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_TBGLReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_TBGLReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C06_LedgerReconReport_SummaryModel.C06_LedgerReconReport_SummaryItem model = new C06_LedgerReconReport_SummaryModel.C06_LedgerReconReport_SummaryItem()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name,
                TableRowID = $"LedgerReconReport_SummaryItem_{companyID}",
                LedgerTotal = 0,
                ProductsTotal = 0,
                CoaTotal = 0,
            };

            var db = new MyVoltageDbContext(_options);

            if (companyID > 0)
            {
                var products = (from p in db.SiteAdmin_Products
                                where p.IncludeInC602x
                                select p).ToList();

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                if (products.Count > 0)
                {
                    var productLedgers = (from p in db.Report_ProductsResourceLedgerMonthlies
                                          where products.Select(c => c.ID).Contains(p.ProductID)
                                          && p.CompanyID == company.CompanyID
                                          select p).ToList();

                    if (productLedgers.Count > 0)
                    {
                        model.ProductsTotal = productLedgers.Select(p => p.Amount).Sum();
                    }
                }

                var ledgers = (from p in db.Report_GeneralLedgerMonthlies
                               where p.CompanyID == company.CompanyID
                               && (p.GenLedgerNo == 6410 || p.GenLedgerNo == 5930)
                               select p).ToList();

                if (ledgers.Count > 0)
                {
                    model.LedgerTotal = ledgers.Select(p => p.Amount).Sum();
                }

                var latestSync = (from p in db.SystemGeneratedReports
                                  join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
                                  from c in sc.DefaultIfEmpty()
                                  where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync
                                  && c.CompanyID == companyID
                                  orderby c.DateCompleted descending
                                  select c).FirstOrDefault();

                if (latestSync != null)
                    model.LatestSyncDate = latestSync.DateCompleted;

                var latestSyncMonthly = (from p in db.SystemGeneratedReports
                                         join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
                                         from c in sc.DefaultIfEmpty()
                                         where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerMonthliesSync
                                         && c.CompanyID == companyID
                                         orderby c.DateCompleted descending
                                         select c).FirstOrDefault();

                if (latestSyncMonthly != null)
                    model.LatestSyncDate_Monthly = latestSyncMonthly.DateCompleted;

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var chartOfAccounts = skyBillApiClient.GetChartOfAccounts();
                var coa = chartOfAccounts.Where(p => p.No == "6410").SingleOrDefault();
                if (coa != null)
                    model.CoaTotal = Convert.ToDecimal(coa.Balance_at_Date);

            }

            return PartialView("~/Views/Operational/C06_LedgerReconReport/C06_LedgerReconReport_SummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Details")]
        public async Task<IActionResult> C06_LedgerReconReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_LedgerReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_LedgerReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C06_LedgerReconReport_DetailsModel model = new C06_LedgerReconReport_DetailsModel()
            {
                C06_LedgerReconReport_DetailsProductItems = new List<C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsProductItem>(),
                C06_LedgerReconReport_DetailsSupplyCostItems = new List<C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsSupplyCostItem>(),
                C06_LedgerReconReport_DetailsGrossAmountItems = new List<C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsGrossAmountItem>(),
                C06_LedgerReconReport_DetailsGrossPercItems = new List<C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsGrossPercItem>(),
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.Where(p => p.IncludeInC602x).ToList(),
                ProductIDs = new List<string>(),
            };

            if (string.IsNullOrEmpty(Request.Query["hideNoData"]) || Convert.ToBoolean(Request.Query["hideNoData"]))
                model.HideNoData = true;
            else
                model.HideNoData = false;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var latestRequest = (from p in db.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new C06_LedgerReconReport_DetailsModel.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request()
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
            }

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

                    C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsProductItem productItem = new C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsProductItem()
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

                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        productItem.MonthlyValues.Add(current, amountProduct);

                        current = current.AddMonths(1);
                    }


                    if (model.HideNoData)
                    {
                        if (productItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0)
                        {
                            continue;
                        }
                    }
                    model.C06_LedgerReconReport_DetailsProductItems.Add(productItem);
                }


                C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsSupplyCostItem supplyCostItem = new C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsSupplyCostItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    ProductID = 0,
                    ProductName = "6410 - Sales, Resources - Dom.",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsGrossAmountItem grossAmountItem = new C06_LedgerReconReport_DetailsModel.C06_LedgerReconReport_DetailsGrossAmountItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    ProductID = 0,
                    ProductName = "Difference",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime currentCOS = model.FromDate;
                while (currentCOS <= model.ToDate)
                {
                    decimal? amountCOS = null;
                    decimal? quantityCOS = null;

                    var report_GenLedgerMonthy = (from p in report_GeneralLedgerMonthlies
                                                  where p.Month == currentCOS
                                                  && p.GenLedgerNo == 6410
                                                  && p.CompanyID == company.CompanyID
                                                  select p).SingleOrDefault();

                    if (report_GenLedgerMonthy != null)
                    {
                        amountCOS = report_GenLedgerMonthy.Amount;
                        quantityCOS = report_GenLedgerMonthy.Quantity;
                    }

                    if (amountCOS.HasValue)
                        amountCOS = amountCOS.Value * -1.0m;
                    if (quantityCOS.HasValue)
                        quantityCOS = quantityCOS.Value * -1.0m;

                    supplyCostItem.MonthlyValues.Add(currentCOS, amountCOS);

                    decimal productTotal = 0;
                    foreach (var productItem in model.C06_LedgerReconReport_DetailsProductItems)
                    {
                        productTotal += productItem.MonthlyValues[currentCOS].HasValue ? productItem.MonthlyValues[currentCOS].Value : 0;
                    }
                    decimal amountGross = productTotal - (amountCOS.HasValue ? amountCOS.Value : 0);
                    grossAmountItem.MonthlyValues.Add(currentCOS, amountGross);


                    currentCOS = currentCOS.AddMonths(1);
                }



                model.C06_LedgerReconReport_DetailsSupplyCostItems.Add(supplyCostItem);
                model.C06_LedgerReconReport_DetailsGrossAmountItems.Add(grossAmountItem);

            }

            return View("~/Views/Operational/C06_LedgerReconReport/C06_LedgerReconReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Daily")]
        public async Task<IActionResult> C06_LedgerReconReport_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_LedgerReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_LedgerReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C06_LedgerReconReport_DailyModel model = new C06_LedgerReconReport_DailyModel()
            {
                C06_LedgerReconReport_DailyProductItems = new List<C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyProductItem>(),
                C06_LedgerReconReport_DailySupplyCostItems = new List<C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailySupplyCostItem>(),
                C06_LedgerReconReport_DailyGrossAmountItems = new List<C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyGrossAmountItem>(),
                C06_LedgerReconReport_DailyGrossPercItems = new List<C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyGrossPercItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SiteAdmin_Products = db.SiteAdmin_Products.Where(p => p.IncludeInC602x).ToList(),
                ProductIDs = new List<string>(),
            };

            if (string.IsNullOrEmpty(Request.Query["hideNoData"]) || Convert.ToBoolean(Request.Query["hideNoData"]))
                model.HideNoData = true;
            else
                model.HideNoData = false;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (model.ToDate.Date >= model.FromDate.AddMonths(1).Date)
            {
                model.ToDate = model.FromDate.AddMonths(1).Date;
            }

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            //var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            //var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            //var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID == 0 || _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                foreach (var product in model.SiteAdmin_Products)
                {
                    if (model.ProductIDs.Count == 0 || model.ProductIDs.Contains(product.ID.ToString()))
                    { }
                    else
                        continue;

                    C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyProductItem productItem = new C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyProductItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    };

                    var resourcesForProduct = (from p in db.SkybillResourceLists
                                               where p.ProductID.HasValue
                                               && p.ProductID.Value == product.ID
                                               && p.CompanyID == company.CompanyID
                                               select p.No).ToList();

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


                        if (resourceLedgerEntries.Count > 0)
                        {
                            amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                            quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                        }

                        if (amountProduct.HasValue)
                            amountProduct = amountProduct.Value * -1.0m;
                        if (quantityProduct.HasValue)
                            quantityProduct = quantityProduct.Value * -1.0m;

                        productItem.MonthlyValues.Add(current, amountProduct);

                        current = current.AddDays(1);
                    }


                    if (model.HideNoData)
                    {
                        if (productItem.MonthlyValues.Where(p => p.Value.HasValue).Count() == 0)
                        {
                            continue;
                        }
                    }
                    model.C06_LedgerReconReport_DailyProductItems.Add(productItem);
                }


                C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailySupplyCostItem supplyCostItem = new C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailySupplyCostItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    ProductID = 0,
                    ProductName = "6410 - Sales, Resources - Dom.",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyGrossAmountItem grossAmountItem = new C06_LedgerReconReport_DailyModel.C06_LedgerReconReport_DailyGrossAmountItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    ProductID = 0,
                    ProductName = "Difference",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime currentCOS = model.FromDate;
                while (currentCOS <= model.ToDate)
                {
                    decimal? amountCOS = null;
                    decimal? quantityCOS = null;

                    var report_GenLedgerMonthy = (from p in db.GeneralLedgerEntries
                                                  where p.Posting_Date.Date == currentCOS.Date
                                                  && p.G_L_Account_No == "6410"
                                                  && p.CompanyID == company.CompanyID
                                                  select p).ToList();

                    if (report_GenLedgerMonthy.Count > 0)
                    {
                        amountCOS = report_GenLedgerMonthy.Select(p => p.Amount).Sum();
                        quantityCOS = report_GenLedgerMonthy.Select(p => p.Quantity).Sum();
                    }

                    if (amountCOS.HasValue)
                        amountCOS = amountCOS.Value * -1.0m;
                    if (quantityCOS.HasValue)
                        quantityCOS = quantityCOS.Value * -1.0m;

                    supplyCostItem.MonthlyValues.Add(currentCOS, amountCOS);

                    decimal productTotal = 0;
                    foreach (var productItem in model.C06_LedgerReconReport_DailyProductItems)
                    {
                        productTotal += productItem.MonthlyValues[currentCOS].HasValue ? productItem.MonthlyValues[currentCOS].Value : 0;
                    }
                    decimal amountGross = productTotal - (amountCOS.HasValue ? amountCOS.Value : 0);
                    grossAmountItem.MonthlyValues.Add(currentCOS, amountGross);


                    currentCOS = currentCOS.AddDays(1);
                }



                model.C06_LedgerReconReport_DailySupplyCostItems.Add(supplyCostItem);
                model.C06_LedgerReconReport_DailyGrossAmountItems.Add(grossAmountItem);

            }

            return View("~/Views/Operational/C06_LedgerReconReport/C06_LedgerReconReport_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Summary")]
        public async Task<IActionResult> C06_TBGLReconReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_TBGLReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_TBGLReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C06_TBGLReconReport_SummaryModel model = new C06_TBGLReconReport_SummaryModel()
            {
                C06_TBGLReconReport_SummaryItems = new List<C06_TBGLReconReport_SummaryModel.C06_TBGLReconReport_SummaryItem>(),
                ReportingDate = !string.IsNullOrEmpty(Request.Query["ReportingDate"]) ? Convert.ToDateTime(Request.Query["ReportingDate"]) : DateTime.Now.AddDays(-1).Date,
            };

            model.C06_TBGLReconReport_SummaryItems = model.C06_TBGLReconReport_SummaryItems.OrderBy(p => p.CompanyName).ToList();
            return View("~/Views/Operational/C06_LedgerReconReport/C06_TBGLReconReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_TBGLReconReport_SummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> C06_TBGLReconReport_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_TBGLReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_TBGLReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C06_TBGLReconReport_SummaryModel.C06_TBGLReconReport_SummaryItem model = new C06_TBGLReconReport_SummaryModel.C06_TBGLReconReport_SummaryItem()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name,
                TableRowID = $"CalibrationSummaryItem_{companyID}",
                ReportingDate = !string.IsNullOrEmpty(Request.Query["ReportingDate"]) ? Convert.ToDateTime(Request.Query["ReportingDate"]) : DateTime.Now.AddDays(-1).Date,
                ChartOfAccountsTotal = 0,
                LedgerTotal = 0,
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            List<int> cOAsToExclude = new List<int>()
            {
                1000    , // -	BALANCE SHEET
                1002    , // -	ASSETS
                1003    , // -	Fixed Assets
                1005    , // -	Tangible Fixed Assets
                1100    , // -	Land and Buildings
                1190    , // -	Land and Buildings, Total
                1200    , // -	Operating Equipment
                1290    , // -	Operating Equipment, Total
                1300    , // -	Vehicles
                1390    , // -	Vehicles, Total
                1395    , // -	Tangible Fixed Assets, Total
                1999    , // -	Fixed Assets, Total
                2000    , // -	Current Assets
                2100    , // -	Inventory
                2190    , // -	Inventory, Total
                2200    , // -	Job WIP
                2210    , // -	WIP Sales
                2220    , // -	WIP Sales, Total
                2230    , // -	WIP Costs
                2240    , // -	WIP Costs, Total
                2290    , // -	Job WIP, Total
                2300    , // -	Accounts Receivable
                2390    , // -	Accounts Receivable, Total
                2400    , // -	Purchase Prepayments
                2440    , // -	Purchase Prepayments, Total
                2800    , // -	Securities
                2890    , // -	Securities, Total
                2900    , // -	Liquid Assets
                2990    , // -	Liquid Assets, Total
                2995    , // -	Current Assets, Total
                2999    , // -	TOTAL ASSETS
                3000    , // -	LIABILITIES AND EQUITY
                3100    , // -	Stockholder's Equity
                3195    , // -	Net Income for the Year
                3199    , // -	Total Stockholder's Equity
                4000    , // -	Allowances
                4999    , // -	Allowances, Total
                5000    , // -	Liabilities
                5100    , // -	Long-term Liabilities
                5290    , // -	Long-term Liabilities, Total
                5300    , // -	Short-term Liabilities
                5350    , // -	Sales Prepayments
                5390    , // -	Sales Prepayments, Total
                5400    , // -	Accounts Payable
                5490    , // -	Accounts Payable, Total
                5500    , // -	Inv. Adjmt. (Interim)
                5590    , // -	Inv. Adjmt. (Interim), Total
                5600    , // -	GST
                5790    , // -	GST, Total
                5795    , // -	Prepaid Service Contracts
                5799    , // -	Total Prepaid Service Contract
                5800    , // -	Personnel-related Items
                5890    , // -	Total Personnel-related Items
                5900    , // -	Other Liabilities
                5990    , // -	Other Liabilities, Total
                5995    , // -	Short-term Liabilities, Total
                5997    , // -	Total Liabilities
                5999    , // -	TOTAL LIABILITIES AND EQUITY
                6000    , // -	INCOME STATEMENT
                6100    , // -	Revenue
                6105    , // -	Sales of Retail
                6195    , // -	Total Sales of Retail
                6205    , // -	Sales of Raw Materials
                6295    , // -	Total Sales of Raw Materials
                6405    , // -	Sales of Resources
                6495    , // -	Total Sales of Resources
                6605    , // -	Sales of Jobs
                6695    , // -	Total Sales of Jobs
                6950    , // -	Sales of Service Contracts
                6959    , // -	Total Sale of Serv. Contracts
                6995    , // -	Total Revenue
                7100    , // -	Cost
                7105    , // -	Cost of Retail
                7195    , // -	Total Cost of Retail
                7205    , // -	Cost of Raw Materials
                7295    , // -	Total Cost of Raw Materials
                7405    , // -	Cost of Resources
                7495    , // -	Total Cost of Resources
                7705    , // -	Cost of Capacities
                7795    , // -	Total Cost of Capacities
                7805    , // -	Variance
                7895    , // -	Total Variance
                7995    , // -	Total Cost
                8000    , // -	Operating Expenses
                8100    , // -	Building Maintenance Expenses
                8190    , // -	Total Bldg. Maint. Expenses
                8200    , // -	Administrative Expenses
                8290    , // -	Total Administrative Expenses
                8300    , // -	Computer Expenses
                8390    , // -	Total Computer Expenses
                8400    , // -	Selling Expenses
                8490    , // -	Total Selling Expenses
                8500    , // -	Vehicle Expenses
                8590    , // -	Total Vehicle Expenses
                8600    , // -	Other Operating Expenses
                8690    , // -	Other Operating Exp., Total
                8695    , // -	Total Operating Expenses
                8700    , // -	Personnel Expenses
                8790    , // -	Total Personnel Expenses
                8800    , // -	Depreciation of Fixed Assets
                8890    , // -	Total Fixed Asset Depreciation
                8995    , // -	Net Operating Income
                9100    , // -	Interest Income
                9190    , // -	Total Interest Income
                9200    , // -	Interest Expenses
                9290    , // -	Total Interest Expenses
                9395    , // -	NI BEF. EXTR. ITEMS & US TAXES
                9495    , // -	NET INCOME BEFORE US TAXES
                9999    , // -	NET INCOME

            };

            if (companyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                //StringBuilder sqlQuery = new StringBuilder();
                //sqlQuery.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByDayForCompany] '{model.ReportingDate.ToString("yyyy-MM-dd")}', '{model.ReportingDate.ToString("yyyy-MM-dd")}', '{companyID}', '0'");

                //SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                //sqlCommand.CommandTimeout = 600;

                //System.Data.DataTable dataTable = new System.Data.DataTable();
                //new SqlDataAdapter(sqlCommand).Fill(dataTable);

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var chartOfAccounts = skyBillApiClient.GetChartOfAccounts(model.ReportingDate);
                chartOfAccounts = chartOfAccounts.Where(p => !cOAsToExclude.Contains(Convert.ToInt32(p.No))).ToList();
                var companyLedgers = db.Report_GeneralLedgerMonthlies.Where(p => p.CompanyID == company.CompanyID).ToList();
                if (chartOfAccounts.Count > 0)
                {
                    var ledgers = (from p in db.GeneralLedgerEntries
                                   where p.CompanyID == companyID
                                   && p.Posting_Date <= model.ReportingDate.Date
                                   //&& p.Balance.HasValue
                                   select new
                                   {
                                       G_L_Account_No = Convert.ToInt32(p.G_L_Account_No),
                                       p.Amount,
                                   }).ToList();
                    foreach (var cOA in chartOfAccounts)
                    {
                        decimal thisLedgerTotal = 0;

                        //var tableResults = dataTable.Select($"[Posting_Date] = '{model.ReportingDate.ToString("yyyy-MM-dd")}' And G_L_Account_No = '{cOA.No}'");

                        var latestLedger = (from p in ledgers
                                                //&& p.Balance.HasValue
                                            where p.G_L_Account_No == Convert.ToInt32(cOA.No.ToString())
                                            select p.Amount).Sum();

                        if (latestLedger != null)
                            thisLedgerTotal = latestLedger;

                        //foreach (var dr in tableResults)
                        //{
                        //    thisLedgerTotal = thisLedgerTotal + Convert.ToDecimal(dr["Amount"]);
                        //}

                        model.LedgerTotal += Convert.ToDecimal(Convert.ToInt32(thisLedgerTotal));
                        model.ChartOfAccountsTotal += Convert.ToDecimal(Convert.ToInt32(cOA.Balance_at_Date));

                        if (Convert.ToDecimal(cOA.Balance_at_Date) != thisLedgerTotal)
                        {
                            decimal diff = (Convert.ToDecimal(cOA.Balance_at_Date) - thisLedgerTotal);
                            model.Diff += diff;

                            decimal diffABS = Math.Abs((Convert.ToDecimal(cOA.Balance_at_Date) - thisLedgerTotal));
                            if (diffABS < 0)
                                diffABS = diffABS * -1.0m;

                            model.DiffABS += diffABS;
                        }
                    }

                }

                var latestSync = (from p in db.SystemGeneratedReports
                                  join c in db.SystemGeneratedReports_Companies on p.ID equals c.SystemGeneratedReportID into sc
                                  from c in sc.DefaultIfEmpty()
                                  where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_GenLedgerSync
                                  && c.CompanyID == companyID
                                  orderby c.DateCompleted descending
                                  select c).FirstOrDefault();

                if (latestSync != null)
                    model.LatestSyncDate = latestSync.DateCompleted;
            }

            return PartialView("~/Views/Operational/C06_LedgerReconReport/C06_TBGLReconReport_SummaryItem.cshtml", model);

        }


        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Details")]
        public async Task<IActionResult> C06_TBGLReconReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C06_TBGLReconReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C06_TBGLReconReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            C06_TBGLReconReport_DetailsModel model = new C06_TBGLReconReport_DetailsModel()
            {
                C06_TBGLReconReport_DetailsItems = new List<C06_TBGLReconReport_DetailsModel.C06_TBGLReconReport_DetailsItem>(),
                Date = string.IsNullOrEmpty(Request.Query["Date"]) ? DateTime.Now.AddDays(-1).Date : Convert.ToDateTime(Request.Query["Date"]),
            };

            List<int> cOAsToExclude = new List<int>()
            {
                1000    , // -	BALANCE SHEET
                1002    , // -	ASSETS
                1003    , // -	Fixed Assets
                1005    , // -	Tangible Fixed Assets
                1100    , // -	Land and Buildings
                1190    , // -	Land and Buildings, Total
                1200    , // -	Operating Equipment
                1290    , // -	Operating Equipment, Total
                1300    , // -	Vehicles
                1390    , // -	Vehicles, Total
                1395    , // -	Tangible Fixed Assets, Total
                1999    , // -	Fixed Assets, Total
                2000    , // -	Current Assets
                2100    , // -	Inventory
                2190    , // -	Inventory, Total
                2200    , // -	Job WIP
                2210    , // -	WIP Sales
                2220    , // -	WIP Sales, Total
                2230    , // -	WIP Costs
                2240    , // -	WIP Costs, Total
                2290    , // -	Job WIP, Total
                2300    , // -	Accounts Receivable
                2390    , // -	Accounts Receivable, Total
                2400    , // -	Purchase Prepayments
                2440    , // -	Purchase Prepayments, Total
                2800    , // -	Securities
                2890    , // -	Securities, Total
                2900    , // -	Liquid Assets
                2990    , // -	Liquid Assets, Total
                2995    , // -	Current Assets, Total
                2999    , // -	TOTAL ASSETS
                3000    , // -	LIABILITIES AND EQUITY
                3100    , // -	Stockholder's Equity
                3195    , // -	Net Income for the Year
                3199    , // -	Total Stockholder's Equity
                4000    , // -	Allowances
                4999    , // -	Allowances, Total
                5000    , // -	Liabilities
                5100    , // -	Long-term Liabilities
                5290    , // -	Long-term Liabilities, Total
                5300    , // -	Short-term Liabilities
                5350    , // -	Sales Prepayments
                5390    , // -	Sales Prepayments, Total
                5400    , // -	Accounts Payable
                5490    , // -	Accounts Payable, Total
                5500    , // -	Inv. Adjmt. (Interim)
                5590    , // -	Inv. Adjmt. (Interim), Total
                5600    , // -	GST
                5790    , // -	GST, Total
                5795    , // -	Prepaid Service Contracts
                5799    , // -	Total Prepaid Service Contract
                5800    , // -	Personnel-related Items
                5890    , // -	Total Personnel-related Items
                5900    , // -	Other Liabilities
                5990    , // -	Other Liabilities, Total
                5995    , // -	Short-term Liabilities, Total
                5997    , // -	Total Liabilities
                5999    , // -	TOTAL LIABILITIES AND EQUITY
                6000    , // -	INCOME STATEMENT
                6100    , // -	Revenue
                6105    , // -	Sales of Retail
                6195    , // -	Total Sales of Retail
                6205    , // -	Sales of Raw Materials
                6295    , // -	Total Sales of Raw Materials
                6405    , // -	Sales of Resources
                6495    , // -	Total Sales of Resources
                6605    , // -	Sales of Jobs
                6695    , // -	Total Sales of Jobs
                6950    , // -	Sales of Service Contracts
                6959    , // -	Total Sale of Serv. Contracts
                6995    , // -	Total Revenue
                7100    , // -	Cost
                7105    , // -	Cost of Retail
                7195    , // -	Total Cost of Retail
                7205    , // -	Cost of Raw Materials
                7295    , // -	Total Cost of Raw Materials
                7405    , // -	Cost of Resources
                7495    , // -	Total Cost of Resources
                7705    , // -	Cost of Capacities
                7795    , // -	Total Cost of Capacities
                7805    , // -	Variance
                7895    , // -	Total Variance
                7995    , // -	Total Cost
                8000    , // -	Operating Expenses
                8100    , // -	Building Maintenance Expenses
                8190    , // -	Total Bldg. Maint. Expenses
                8200    , // -	Administrative Expenses
                8290    , // -	Total Administrative Expenses
                8300    , // -	Computer Expenses
                8390    , // -	Total Computer Expenses
                8400    , // -	Selling Expenses
                8490    , // -	Total Selling Expenses
                8500    , // -	Vehicle Expenses
                8590    , // -	Total Vehicle Expenses
                8600    , // -	Other Operating Expenses
                8690    , // -	Other Operating Exp., Total
                8695    , // -	Total Operating Expenses
                8700    , // -	Personnel Expenses
                8790    , // -	Total Personnel Expenses
                8800    , // -	Depreciation of Fixed Assets
                8890    , // -	Total Fixed Asset Depreciation
                8995    , // -	Net Operating Income
                9100    , // -	Interest Income
                9190    , // -	Total Interest Income
                9200    , // -	Interest Expenses
                9290    , // -	Total Interest Expenses
                9395    , // -	NI BEF. EXTR. ITEMS & US TAXES
                9495    , // -	NET INCOME BEFORE US TAXES
                9999    , // -	NET INCOME

            };

            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var latestRequest = (from p in db.F_SystemGeneratedReports_GenLedgerSync_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new C06_TBGLReconReport_DetailsModel.F_SystemGeneratedReports_GenLedgerSync_Request()
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

                var latestRequestFULL = (from p in db.F_SystemGeneratedReports_GenLedgerSyncFULL_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequestFULL != null)
                {

                    model.LatestRequestFULL = new C06_TBGLReconReport_DetailsModel.F_SystemGeneratedReports_GenLedgerSyncFULL_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latestRequestFULL.CreatedBy,
                        CompanyID = latestRequestFULL.CompanyID,
                        CreatedDate = latestRequestFULL.CreatedDate,
                        DateEnded = latestRequestFULL.DateEnded,
                        DateStarted = latestRequestFULL.DateStarted,
                        FromDate = latestRequestFULL.FromDate,
                        ID = latestRequestFULL.ID,
                        Progress = latestRequestFULL.Progress,
                        SystemReportID = latestRequestFULL.SystemReportID,
                        ToDate = latestRequestFULL.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latestRequestFULL.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latestRequestFULL.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.LatestRequestFULL.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var chartOfAccountsSB = skyBillApiClient.GetChartOfAccounts(model.Date);
                chartOfAccountsSB = chartOfAccountsSB.Where(p => !cOAsToExclude.Contains(Convert.ToInt32(p.No))).ToList();

                var ledgers = (from p in db.GeneralLedgerEntries
                               where p.CompanyID == _operationalProvider.CompanyID
                               && p.Posting_Date <= model.Date.Date
                               //&& p.Balance.HasValue
                               select new
                               {
                                   G_L_Account_No = Convert.ToInt32(p.G_L_Account_No),
                                   p.Amount,
                               }).ToList();

                foreach (var cOASB in chartOfAccountsSB)
                {
                    var cOA = new ChartOfAccountsSnapshot()
                    {
                        Amount = Convert.ToDecimal(cOASB.Balance_at_Date),
                        CompanyID = _operationalProvider.CompanyID,
                        Date = model.Date,
                        GLAccountNo = Convert.ToInt32(cOASB.No),
                        ID = 0,
                    };

                    C06_TBGLReconReport_DetailsModel.C06_TBGLReconReport_DetailsItem item = new C06_TBGLReconReport_DetailsModel.C06_TBGLReconReport_DetailsItem()
                    {
                        Amount = cOA.Amount,
                        Date = cOA.Date,
                        CompanyID = cOA.CompanyID,
                        GLAccountNo = cOA.GLAccountNo,
                        ID = cOA.ID,
                        LedgerTotal = 0,
                        ChartOfAccount = cOASB,
                    };

                    decimal thisLedgerTotal = 0;

                    //var latestLedger = (from p in db.GeneralLedgerEntries
                    //                    where p.CompanyID == _operationalProvider.CompanyID
                    //                    && p.Posting_Date <= model.Date.Date
                    //                    && p.Balance.HasValue
                    //                    && p.G_L_Account_No == cOA.GLAccountNo.ToString()
                    //                    orderby p.Entry_No descending
                    //                    select new
                    //                    {
                    //                        Balance = p.Balance.Value
                    //                    }).FirstOrDefault();

                    //if (latestLedger != null)
                    //    item.LedgerTotal = latestLedger.Balance;

                    var latestLedger = (from p in ledgers
                                        where p.G_L_Account_No == cOA.GLAccountNo
                                        select p.Amount).Sum();

                    if (latestLedger != null)
                        item.LedgerTotal = latestLedger;

                    if (item.Amount == 0
                        && item.LedgerTotal == 0)
                        continue;

                    model.C06_TBGLReconReport_DetailsItems.Add(item);
                }



            }


            model.C06_TBGLReconReport_DetailsItems = model.C06_TBGLReconReport_DetailsItems.OrderBy(p => p.GLAccountNo).ToList();
            return View("~/Views/Operational/C06_LedgerReconReport/C06_TBGLReconReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Details_RequestRerun")]
        public async Task<IActionResult> C06_TBGLReconReport_Details_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_GenLedgerSync_Request F_SystemGeneratedReports_GenLedgerSync_Request = new F_SystemGeneratedReports_GenLedgerSync_Request()
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

                db.Add(F_SystemGeneratedReports_GenLedgerSync_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Details");
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Details_RequestRerunFULL")]
        public async Task<IActionResult> C06_TBGLReconReport_Details_RequestRerunFULL()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_GenLedgerSyncFULL_Request F_SystemGeneratedReports_GenLedgerSyncFULL_Request = new F_SystemGeneratedReports_GenLedgerSyncFULL_Request()
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

                db.Add(F_SystemGeneratedReports_GenLedgerSyncFULL_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C06_LedgerReconReport/C06_TBGLReconReport_Details");
        }

        [HttpGet]
        [Route("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Details_RequestRerun")]
        public async Task<IActionResult> C06_LedgerReconReport_Details_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request = new F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request()
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

                db.Add(F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C06_LedgerReconReport/C06_LedgerReconReport_Details");
        }

    }
}
