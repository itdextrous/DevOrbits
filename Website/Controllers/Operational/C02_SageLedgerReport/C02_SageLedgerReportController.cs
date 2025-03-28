using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C02_SageLedgerReportModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C02_SageLedgerReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C02_SageLedgerReportController(
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
        [Route("/operational/C02_SageLedgerReport/C02_SageLedgerReport_Summary")]
        public async Task<IActionResult> C02_SageLedgerReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_SageLedgerReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_SageLedgerReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? true : Convert.ToBoolean(Request.Query["MovementReport"]);
            C02_SageLedgerReport_SummaryModel model = new C02_SageLedgerReport_SummaryModel()
            {
                C02_SageLedgerReport_SummaryItems = new List<C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
                Companies = db.Companies.Where(p => p.SageAccountingCompanyID.HasValue).ToList(),
            };

            var report_SageLedgerMonthlies = db.Report_SageLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            return View("~/Views/Operational/C02_SageLedgerReport/C02_SageLedgerReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_SageLedgerReport/C02_SageLedgerReport_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> C02_SageLedgerReport_Summary(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_SageLedgerReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_SageLedgerReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? true : Convert.ToBoolean(Request.Query["MovementReport"]);

            C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem model = new C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                C02_SageLedgerReport_SummarySubItems = new List<C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem.C02_SageLedgerReport_SummarySubItem>(),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            //var report_SageLedgerMonthlies = db.Report_SageLedgerMonthlies.ToList();
            //var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            //var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            //var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyName = company.Name;
                model.TableRowID = trid;

                model.CompanyID = company.CompanyID;
                model.Name = company.Name;
                model.BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo;
                model.BalanceMustBeAbove = company.BalanceMustBeAbove;
                model.ExistsInSkybill = company.ExistsInSkybill;
                model.Registrable = company.Registrable;
                model.ServiceKey = company.ServiceKey;
                model.C02_SageLedgerReport_SummarySubItems = new List<C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem.C02_SageLedgerReport_SummarySubItem>();

                var accountingChecklists = (from p in db.AccountingChecklists
                                            join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                            from c in sc.DefaultIfEmpty()
                                            where p.Date >= new DateTime(model.FromDate.Date.Year, model.FromDate.Date.Month, 1)
                                            && p.Date <= new DateTime(model.ToDate.Date.Year, model.ToDate.Date.Month, DateTime.DaysInMonth(model.ToDate.Date.Year, model.ToDate.Date.Month))
                                            && p.CompanyID == uC.CompanyID
                                            select new
                                            {
                                                CompanyName = c.Name,
                                                p.ApprovedBy,
                                                p.ApprovedDate,
                                                p.BalanceAmount,
                                                p.BalanceAmountConfirmed,
                                                p.BalanceSyncDate,
                                                p.Comments,
                                                p.CompanyID,
                                                p.Date,
                                                p.FlagID,
                                                p.ID,
                                                p.LedgerName,
                                                p.LedgerNo,
                                                p.MovementAmount,
                                                p.MovementAmountConfirmed,
                                                p.MovementSyncDate,
                                                p.PendingIssue,
                                                p.ReviewedBy,
                                                p.ReviewedDate,
                                                p.TaskID,
                                                p.ReviewedByBalance,
                                                p.ReviewedDateBalance,
                                                p.ApprovedByBalance,
                                                p.ApprovedDateBalance,
                                                p.AttachmentFileName,
                                                p.LastCheckedDate,
                                            }).ToList();

                if (movementReport)
                {
                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        string KEY_balance_sheet = $"BalanceSheetTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                        decimal? balanceSheet = null;
                        int balanceSheetCount = 0;
                        int balanceSheetCompleted = 0;
                        DateTime? balanceSheetLatestCompleted = null;

                        if (!_cache.TryGetValue(KEY_balance_sheet, out balanceSheet))
                        {
                            if (movementReport)
                            {
                                balanceSheet = (from p in db.SageAccounting_DetailedLedgerTransactions
                                                where p.Date.Value.Year == current.Year
                                                && p.Date.Value.Month == current.Month
                                                && p.CompanyId == company.SageAccountingCompanyID
                                                select p.Debit.HasValue && p.Debit.Value == 0 ? p.Credit.Value : p.Debit.Value).Sum();

                                var balanceSheetEntries = (from p in accountingChecklists
                                                           where p.Date.Year == current.Year
                                                           && p.Date.Month == current.Month
                                                           //&& p.LedgerNo <= 5999
                                                           select p).ToList();


                                balanceSheetCount = balanceSheetEntries.Count;
                                balanceSheetCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count();
                                if (balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    balanceSheetLatestCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
                            }
                            else
                            {
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions();

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                            _cache.Set(KEY_balance_sheet, balanceSheet, cacheEntryOptions);
                        }

                        string KEY_incomeStatement = $"IncomeStatementTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                        decimal? incomeStatement = null;
                        int incomeStatementCount = 0;
                        int incomeStatementCompleted = 0;
                        DateTime? incomeStatementLatestCompleted = null;

                        if (!_cache.TryGetValue(KEY_incomeStatement, out incomeStatement))
                        {
                            if (movementReport)
                            {
                                incomeStatement = (from p in db.SageAccounting_DetailedLedgerTransactions
                                                   where p.Date.Value.Year == current.Year
                                                   && p.Date.Value.Month == current.Month
                                                   && p.CompanyId == company.SageAccountingCompanyID
                                                   select p.Debit.HasValue && p.Debit.Value == 0 ? p.Credit.Value : p.Debit.Value).Sum();

                                var incomeStatementEntries = (from p in accountingChecklists
                                                              where p.Date.Year == current.Year
                                                              && p.Date.Month == current.Month
                                                              //&& p.LedgerNo > 5999
                                                              select p).ToList();


                                incomeStatementCount = incomeStatementEntries.Count;
                                incomeStatementCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count();
                                if (incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    incomeStatementLatestCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
                            }
                            else
                            {
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions();

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                            _cache.Set(KEY_incomeStatement, incomeStatement, cacheEntryOptions);
                        }

                        model.C02_SageLedgerReport_SummarySubItems.Add(new C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem.C02_SageLedgerReport_SummarySubItem()
                        {
                            Month = current,
                            BalanceSheet = balanceSheet.HasValue ? balanceSheet.Value : 0,
                            IncomeStatement = incomeStatement.HasValue ? incomeStatement.Value : 0,
                            Total = (balanceSheet.HasValue ? balanceSheet.Value : 0) + (incomeStatement.HasValue ? incomeStatement.Value : 0),
                            BalanceSheetCompleted = balanceSheetCompleted,
                            BalanceSheetCount = balanceSheetCount,
                            IncomeStatementCompleted = incomeStatementCompleted,
                            IncomeStatementCount = incomeStatementCount,
                            BalanceLatestCompleted = balanceSheetLatestCompleted,
                            IncomeStatementLatestCompleted = incomeStatementLatestCompleted,
                        });

                        current = current.AddMonths(1);
                    }
                }
                else
                {
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

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                    Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();

                    DateTime current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        if (current.Date <= DateTime.Now.Date)
                            cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                        current = current.AddMonths(1);
                    }

                    current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        string KEY_balance_sheet = $"BalanceSheetTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                        decimal? balanceSheet = null;
                        int balanceSheetCount = 0;
                        int balanceSheetCompleted = 0;
                        DateTime? balanceSheetLatestCompleted = null;
                        var balanceSheetEntries = (from p in accountingChecklists
                                                   where p.Date.Year == current.Year
                                                   && p.Date.Month == current.Month
                                                   && p.LedgerNo <= 5999
                                                   select p).ToList();


                        balanceSheetCount = balanceSheetEntries.Count;
                        balanceSheetCompleted = balanceSheetEntries.Where(p => p.ApprovedDateBalance.HasValue).Count();
                        if (balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                            balanceSheetLatestCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();

                        if (!_cache.TryGetValue(KEY_balance_sheet, out balanceSheet))
                        {
                            if (cOAs.ContainsKey(current))
                            {
                                var cOAforDate = cOAs[current];

                                balanceSheet = Convert.ToDecimal(cOAforDate.Where(p => cOAsToExclude.Contains(Convert.ToInt32(p.No)) && Convert.ToInt32(p.No) <= 5999).Select(p => p.Balance_at_Date).Sum());
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions();

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                            _cache.Set(KEY_balance_sheet, balanceSheet, cacheEntryOptions);
                        }

                        string KEY_incomeStatement = $"IncomeStatementTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                        decimal? incomeStatement = null;
                        int incomeStatementCount = 0;
                        int incomeStatementCompleted = 0;
                        DateTime? incomeStatementLatestCompleted = null;
                        var incomeStatementEntries = (from p in accountingChecklists
                                                      where p.Date.Year == current.Year
                                                      && p.Date.Month == current.Month
                                                      && p.LedgerNo > 5999
                                                      select p).ToList();


                        incomeStatementCount = incomeStatementEntries.Count;
                        incomeStatementCompleted = incomeStatementEntries.Where(p => p.ApprovedDateBalance.HasValue).Count();
                        if (incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                            incomeStatementLatestCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();

                        if (!_cache.TryGetValue(KEY_incomeStatement, out incomeStatement))
                        {
                            if (cOAs.ContainsKey(current))
                            {
                                var cOAforDate = cOAs[current];

                                incomeStatement = Convert.ToDecimal(cOAforDate.Where(p => cOAsToExclude.Contains(Convert.ToInt32(p.No)) && Convert.ToInt32(p.No) > 5999).Select(p => p.Balance_at_Date).Sum());
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions();

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                            _cache.Set(KEY_incomeStatement, incomeStatement, cacheEntryOptions);
                        }

                        model.C02_SageLedgerReport_SummarySubItems.Add(new C02_SageLedgerReport_SummaryModel.C02_SageLedgerReport_SummaryItem.C02_SageLedgerReport_SummarySubItem()
                        {
                            Month = current,
                            BalanceSheet = balanceSheet.HasValue ? balanceSheet.Value : 0,
                            IncomeStatement = incomeStatement.HasValue ? incomeStatement.Value : 0,
                            Total = (balanceSheet.HasValue ? balanceSheet.Value : 0) - (incomeStatement.HasValue ? incomeStatement.Value : 0),
                            BalanceSheetCompleted = balanceSheetCompleted,
                            BalanceSheetCount = balanceSheetCount,
                            IncomeStatementCompleted = incomeStatementCompleted,
                            IncomeStatementCount = incomeStatementCount,
                            BalanceLatestCompleted = balanceSheetLatestCompleted,
                            IncomeStatementLatestCompleted = incomeStatementLatestCompleted,
                        });

                        current = current.AddMonths(1);
                    }
                }
            }

            return PartialView("~/Views/Operational/C02_SageLedgerReport/C02_SageLedgerReport_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_SageLedgerReport/C02_SageLedgerReport_Monthly")]
        public async Task<IActionResult> C02_SageLedgerReport_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_SageLedgerReport_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_SageLedgerReport_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var sageManagementAccounts_ReportingParentDescriptions = db.SageManagementAccounts_ReportingParentDescriptions.OrderBy(p => p.ReportingParentDescription).ToList();

            C02_SageLedgerReport_MonthlyModel model = new C02_SageLedgerReport_MonthlyModel()
            {
                C02_SageLedgerReport_MonthlyItems_BalanceSheet = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem>(),
                C02_SageLedgerReport_MonthlyItems_IncomeStatement = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "1", Text = "Movement Report", Selected = string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "2", Text = "Balance Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "2" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "3", Text = "Accountant Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "3" },
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
                ParentReportingCategory = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[-- All --]", Selected = string.IsNullOrEmpty(Request.Query["ParentReportingCategory"]) },
                },
            };

            model.ParentReportingCategory.AddRange((from p in sageManagementAccounts_ReportingParentDescriptions
                                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                                    {
                                                        Value = p.ID.ToString(),
                                                        Text = p.ReportingParentDescription,
                                                        Selected = !string.IsNullOrEmpty(Request.Query["ParentReportingCategory"]) && Request.Query["ParentReportingCategory"] == p.ID.ToString()
                                                    }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            StringBuilder sqlQuery = new StringBuilder();
            var cOA = db.SageAccounting_Accounts.ToList();
            if (_operationalProvider.CompanyID != 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                cOA = (from p in db.SageAccounting_Accounts
                       where p.CompanyId.Value == company.SageAccountingCompanyID
                       select p).ToList();
                sqlQuery.AppendLine($"exec [sp_SageLedgerEntriesGroupedByMonthPerCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{company.SageAccountingCompanyID}', '1'");
            }
            else
                sqlQuery.AppendLine($"exec [sp_SageLedgerEntriesGroupedByMonthPerCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '0', '1'");

            if (!string.IsNullOrEmpty(Request.Query["ParentReportingCategory"]))
                cOA = cOA.Where(p => p.ReportingParentDescriptionID.HasValue && p.ReportingParentDescriptionID.Value == Convert.ToInt32(Request.Query["ParentReportingCategory"])).ToList();

            var sageAccounting_AccountCategories = db.SageAccounting_AccountCategories.ToList();
            var sageAccounting_Companies = db.SageAccounting_Companies.ToList();
            var accountingChecklists = db.AccountingChecklists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Date >= model.FromDate.Date && p.Date <= model.ToDate.Date).ToList();
            DateTime current = model.FromDate;

            if (string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" || Request.Query["MovementReport"].ToString().ToLower() == "true")
            {
                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand.CommandTimeout = 600;
                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                foreach (var c in cOA)
                {
                    var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                    if (category == null)
                        continue;
                    string reportingDescription = "";
                    string reportingCategory = "";
                    if (c.ReportingParentDescriptionID.HasValue)
                    {
                        var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                        if (managementAccounts_ReportingDescription != null)
                        {
                            reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                            reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                        }
                    }
                    var sageAccounting_Company = sageAccounting_Companies.Where(p => p.SageID == c.CompanyId).SingleOrDefault();

                    if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                    {
                        // BALANCE SHEET ( Acc No 1 - 5999)

                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem balanceSheetItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal? amount = null;

                            var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}' And CompanyId = '{c.CompanyId}'");

                            foreach (var dr in tableResults)
                            {
                                if (amount.HasValue)
                                    amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                else
                                    amount = Convert.ToDecimal(dr["Amount"]);
                            }

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }
                        if (balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                        }
                    }
                    else
                    {
                        // INCOME STATEMENT (Acc No 6000 - 9999)
                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem incomeStatementItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal? amount = null;
                            var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}' And CompanyId = '{c.CompanyId}'");

                            foreach (var dr in tableResults)
                            {
                                if (amount.HasValue)
                                    amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                else
                                    amount = Convert.ToDecimal(dr["Amount"]);
                            }

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }

                        if (incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                        }
                    }
                }
            }
            else if (Request.Query["MovementReport"].ToString() == "2" || Request.Query["MovementReport"].ToString().ToLower() == "false")
            {
                current = model.FromDate;

                foreach (var c in cOA)
                {
                    var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                    if (category == null)
                        continue;
                    var sageAccounting_Company = sageAccounting_Companies.Where(p => p.SageID == c.CompanyId).SingleOrDefault();

                    string reportingDescription = "";
                    string reportingCategory = "";
                    if (c.ReportingParentDescriptionID.HasValue)
                    {
                        var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                        if (managementAccounts_ReportingDescription != null)
                        {
                            reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                            reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                        }
                    }

                    var categoryLedgers = (from p in db.SageAccounting_DetailedLedgerTransactions
                                           where p.AccountId == c.SageID
                                           select new
                                           {
                                               p.Date,
                                               p.Debit,
                                               p.Credit,
                                           }).ToList();

                    if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                    {
                        // BALANCE SHEET ( Acc No 1 - 5999)

                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem balanceSheetItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal amount = (from p in categoryLedgers
                                              where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                              select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }
                        if (balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                        }
                    }
                    else
                    {
                        // INCOME STATEMENT (Acc No 6000 - 9999)
                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem incomeStatementItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal amount = (from p in categoryLedgers
                                              where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                              select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }

                        if (incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                        }
                    }
                }

            }
            else if (Request.Query["MovementReport"].ToString() == "3")
            {
                #region Movement Report - Add only INCOME STATEMENT items to C02_SageLedgerReport_MonthlyItems_IncomeStatement

                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand.CommandTimeout = 600;
                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                foreach (var c in cOA)
                {
                    var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                    if (category == null)
                        continue;
                    var sageAccounting_Company = sageAccounting_Companies.Where(p => p.SageID == c.CompanyId).SingleOrDefault();
                    string reportingDescription = "";
                    string reportingCategory = "";
                    if (c.ReportingParentDescriptionID.HasValue)
                    {
                        var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                        if (managementAccounts_ReportingDescription != null)
                        {
                            reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                            reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                        }
                    }

                    if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                    {
                    }
                    else
                    {
                        // INCOME STATEMENT (Acc No 6000 - 9999)
                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem incomeStatementItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal? amount = null;
                            var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}' And CompanyId = '{c.CompanyId}'");

                            foreach (var dr in tableResults)
                            {
                                if (amount.HasValue)
                                    amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                else
                                    amount = Convert.ToDecimal(dr["Amount"]);
                            }

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }

                        if (incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                        }
                    }
                }

                #endregion

                #region Balance Report - Add BALANCE SHEET to C02_SageLedgerReport_MonthlyItems_BalanceSheet; Add INCOME STATEMENT to balanceSheetItems (Memory)
                List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem> balanceSheetItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem>();

                current = model.FromDate;

                foreach (var c in cOA)
                {
                    var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                    if (category == null)
                        continue;
                    var sageAccounting_Company = sageAccounting_Companies.Where(p => p.SageID == c.CompanyId).SingleOrDefault();
                    string reportingDescription = "";
                    string reportingCategory = "";
                    if (c.ReportingParentDescriptionID.HasValue)
                    {
                        var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                        if (managementAccounts_ReportingDescription != null)
                        {
                            reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                            reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                        }
                    }

                    var categoryLedgers = (from p in db.SageAccounting_DetailedLedgerTransactions
                                           where p.AccountId == c.SageID
                                           select new
                                           {
                                               p.Date,
                                               p.Debit,
                                               p.Credit,
                                           }).ToList();

                    if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                    {
                        // BALANCE SHEET ( Acc No 1 - 5999)

                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem balanceSheetItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal amount = (from p in categoryLedgers
                                              where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                              select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }
                        if (balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            model.C02_SageLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                        }
                    }
                    else
                    {
                        // INCOME STATEMENT (Acc No 6000 - 9999)
                        C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem incomeStatementItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                        {
                            C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                            FromDate = model.FromDate,
                            JournalName = $"{c.Name}",
                            JournalNo = Convert.ToInt32(c.SageID),
                            ToDate = model.ToDate,
                            Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                            CategoryName = category.Description,
                            ReportingDescription = reportingDescription,
                            ReportingCategory = reportingCategory,
                            ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        current = model.FromDate;

                        while (current <= model.ToDate)
                        {
                            decimal amount = (from p in categoryLedgers
                                              where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                              select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                            var c02_SageLedgerReport_MonthlySubItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = amount,
                                Month = current,
                                CellStyle = "",
                            };

                            var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                            if (ac != null)
                            {
                                if (!ac.ReviewedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                else if (!ac.ApprovedDate.HasValue)
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                else
                                    c02_SageLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                c02_SageLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                            }

                            incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Add(c02_SageLedgerReport_MonthlySubItem);


                            current = current.AddMonths(1);
                        }

                        if (incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                        {
                            if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                continue;
                            balanceSheetItems.Add(incomeStatementItem);
                        }
                    }
                }
                #endregion

                #region Retained earnings

                C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem retainedEarningsItem = new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem()
                {
                    C02_SageLedgerReport_MonthlySubItems = new List<C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem>(),
                    FromDate = model.FromDate,
                    JournalName = "Retained earnings",
                    JournalNo = 0000,
                    ToDate = model.ToDate,
                    Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                };

                foreach (var item in balanceSheetItems)
                {
                    foreach (var subItem in item.C02_SageLedgerReport_MonthlySubItems)
                    {
                        var existingMonthly = (from p in retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems
                                               where p.Month == subItem.Month
                                               select p).SingleOrDefault();

                        if (existingMonthly == null)
                        {
                            retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems.Add(new C02_SageLedgerReport_MonthlyModel.C02_SageLedgerReport_MonthlyItem.C02_SageLedgerReport_MonthlySubItem()
                            {
                                Amount = subItem.Amount,
                                CellStyle = "",
                                Month = subItem.Month,
                            });
                        }
                        else
                        {
                            retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems[retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems.IndexOf(existingMonthly)].Amount = retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems[retainedEarningsItem.C02_SageLedgerReport_MonthlySubItems.IndexOf(existingMonthly)].Amount + subItem.Amount;
                        }

                    }
                }

                model.C02_SageLedgerReport_MonthlyItems_BalanceSheet.Add(retainedEarningsItem);

                #endregion
            }

            return View("~/Views/Operational/C02_SageLedgerReport/C02_SageLedgerReport_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_SageLedgerReport/C02_SageLedgerReport_MonthlySummary")]
        public async Task<IActionResult> C02_SageLedgerReport_MonthlySummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_SageLedgerReport_MonthlySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_SageLedgerReport_MonthlySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_SageLedgerReport_MonthlySummaryModel model = new C02_SageLedgerReport_MonthlySummaryModel()
            {
                C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem>(),
                C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "1", Text = "Movement Report", Selected = string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "2", Text = "Balance Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "2" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "3", Text = "Accountant Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "3" },
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var cOA = (from p in db.SageAccounting_Accounts
                           where p.CompanyId.Value == company.SageAccountingCompanyID
                           select p
                           ).ToList();
                var sageManagementAccounts_ReportingParentDescriptions = db.SageManagementAccounts_ReportingParentDescriptions.ToList();
                var sageAccounting_AccountCategories = db.SageAccounting_AccountCategories.ToList();
                var accountingChecklists = db.AccountingChecklists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Date >= model.FromDate.Date && p.Date <= model.ToDate.Date).ToList();
                DateTime current = model.FromDate;

                if (string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" || Request.Query["MovementReport"].ToString().ToLower() == "true")
                {
                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine($"exec [sp_SageLedgerEntriesGroupedByMonthForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{company.SageAccountingCompanyID}', '1'");

                    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommand.CommandTimeout = 600;
                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);

                    foreach (var managementAccounts_ReportingDescription in sageManagementAccounts_ReportingParentDescriptions)
                    {
                        var category = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault();
                        if (category == null)
                            continue;
                        string reportingDescription = "";
                        string reportingCategory = "";
                        if (managementAccounts_ReportingDescription != null)
                        {
                            reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                            reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                        }
                        var thisCOAs = cOA.Where(p => p.ReportingParentDescriptionID.HasValue && p.ReportingParentDescriptionID.Value == managementAccounts_ReportingDescription.ID).ToList();

                        if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem balanceSheetItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{managementAccounts_ReportingDescription.ReportingParentDescription}",
                                JournalNo = managementAccounts_ReportingDescription.ID,
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = managementAccounts_ReportingDescription.ID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                List<DataRow> tableResults = new List<DataRow>();
                                foreach (var c in thisCOAs)
                                    tableResults.AddRange(dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}'"));

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);

                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet.Add(balanceSheetItem);
                            }
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem incomeStatementItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{managementAccounts_ReportingDescription.ReportingParentDescription}",
                                JournalNo = managementAccounts_ReportingDescription.ID,
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = managementAccounts_ReportingDescription.ID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                List<DataRow> tableResults = new List<DataRow>();
                                foreach (var c in thisCOAs)
                                    tableResults.AddRange(dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}'"));

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                }
                else if (Request.Query["MovementReport"].ToString() == "2" || Request.Query["MovementReport"].ToString().ToLower() == "false")
                {
                    current = model.FromDate;

                    foreach (var c in cOA)
                    {
                        var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                        if (category == null)
                            continue;

                        string reportingDescription = "";
                        string reportingCategory = "";
                        if (c.ReportingParentDescriptionID.HasValue)
                        {
                            var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                            if (managementAccounts_ReportingDescription != null)
                            {
                                reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                                reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                            }
                        }

                        var categoryLedgers = (from p in db.SageAccounting_DetailedLedgerTransactions
                                               where p.AccountId == c.SageID
                                               select new
                                               {
                                                   p.Date,
                                                   p.Debit,
                                                   p.Credit,
                                               }).ToList();

                        if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem balanceSheetItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{c.Name}",
                                JournalNo = Convert.ToInt32(c.SageID),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal amount = (from p in categoryLedgers
                                                  where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                                  select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-warning";
                                    else
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-success";
                                    C02_SageLedgerReport_MonthlySummarySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet.Add(balanceSheetItem);
                            }
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem incomeStatementItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{c.Name}",
                                JournalNo = Convert.ToInt32(c.SageID),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal amount = (from p in categoryLedgers
                                                  where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                                  select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-warning";
                                    else
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-success";
                                    C02_SageLedgerReport_MonthlySummarySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }

                }
                #region Old
                else if (Request.Query["MovementReport"].ToString() == "3")
                {
                    #region Movement Report - Add only INCOME STATEMENT items to C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement

                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine($"exec [sp_SageLedgerEntriesGroupedByMonthForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{company.SageAccountingCompanyID}', '1'");

                    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommand.CommandTimeout = 600;
                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);

                    foreach (var c in cOA)
                    {
                        var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                        if (category == null)
                            continue;
                        string reportingDescription = "";
                        string reportingCategory = "";
                        if (c.ReportingParentDescriptionID.HasValue)
                        {
                            var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                            if (managementAccounts_ReportingDescription != null)
                            {
                                reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                                reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                            }
                        }

                        if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                        {
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem incomeStatementItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{c.Name}",
                                JournalNo = Convert.ToInt32(c.SageID),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.SageID}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-warning";
                                    else
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-success";
                                    C02_SageLedgerReport_MonthlySummarySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }

                    #endregion

                    #region Balance Report - Add BALANCE SHEET to C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet; Add INCOME STATEMENT to balanceSheetItems (Memory)
                    List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem> balanceSheetItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem>();

                    current = model.FromDate;

                    foreach (var c in cOA)
                    {
                        var category = sageAccounting_AccountCategories.Where(p => p.SageID == c.Category.Value).FirstOrDefault();
                        if (category == null)
                            continue;
                        string reportingDescription = "";
                        string reportingCategory = "";
                        if (c.ReportingParentDescriptionID.HasValue)
                        {
                            var managementAccounts_ReportingDescription = sageManagementAccounts_ReportingParentDescriptions.Where(p => p.ID == c.ReportingParentDescriptionID.Value).SingleOrDefault();
                            if (managementAccounts_ReportingDescription != null)
                            {
                                reportingDescription = $"{managementAccounts_ReportingDescription.ReportingParentDescription}";
                                reportingCategory = sageAccounting_AccountCategories.Where(p => p.SageID == managementAccounts_ReportingDescription.ReportingCategoryID).FirstOrDefault().Description;
                            }
                        }


                        var categoryLedgers = (from p in db.SageAccounting_DetailedLedgerTransactions
                                               where p.AccountId == c.SageID
                                               select new
                                               {
                                                   p.Date,
                                                   p.Debit,
                                                   p.Credit,
                                               }).ToList();

                        if (category.IsBalanceSheet.HasValue && category.IsBalanceSheet.Value)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem balanceSheetItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{c.Name}",
                                JournalNo = Convert.ToInt32(c.SageID),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal amount = (from p in categoryLedgers
                                                  where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                                  select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-warning";
                                    else
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-success";
                                    C02_SageLedgerReport_MonthlySummarySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet.Add(balanceSheetItem);
                            }
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem incomeStatementItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                            {
                                C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = $"{c.Name}",
                                JournalNo = Convert.ToInt32(c.SageID),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement,
                                CategoryName = category.Description,
                                ReportingDescription = reportingDescription,
                                ReportingCategory = reportingCategory,
                                ParentReportingCategoryID = c.ReportingParentDescriptionID.ToString(),
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal amount = (from p in categoryLedgers
                                                  where p.Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date
                                                  select ((p.Debit.HasValue ? p.Debit.Value : 0) - (p.Credit.HasValue ? p.Credit.Value : 0))).Sum();

                                var C02_SageLedgerReport_MonthlySummarySubItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.SageID)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-warning";
                                    else
                                        C02_SageLedgerReport_MonthlySummarySubItem.CellStyle = "table-success";
                                    C02_SageLedgerReport_MonthlySummarySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(C02_SageLedgerReport_MonthlySummarySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_SageLedgerReport_MonthlySummarySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                balanceSheetItems.Add(incomeStatementItem);
                            }
                        }
                    }
                    #endregion

                    #region Retained earnings

                    C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem retainedEarningsItem = new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem()
                    {
                        C02_SageLedgerReport_MonthlySummarySubItems = new List<C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem>(),
                        FromDate = model.FromDate,
                        JournalName = "Retained earnings",
                        JournalNo = 0000,
                        ToDate = model.ToDate,
                        Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                    };

                    foreach (var item in balanceSheetItems)
                    {
                        foreach (var subItem in item.C02_SageLedgerReport_MonthlySummarySubItems)
                        {
                            var existingMonthly = (from p in retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems
                                                   where p.Month == subItem.Month
                                                   select p).SingleOrDefault();

                            if (existingMonthly == null)
                            {
                                retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems.Add(new C02_SageLedgerReport_MonthlySummaryModel.C02_SageLedgerReport_MonthlySummaryItem.C02_SageLedgerReport_MonthlySummarySubItem()
                                {
                                    Amount = subItem.Amount,
                                    CellStyle = "",
                                    Month = subItem.Month,
                                });
                            }
                            else
                            {
                                retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems[retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems.IndexOf(existingMonthly)].Amount = retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems[retainedEarningsItem.C02_SageLedgerReport_MonthlySummarySubItems.IndexOf(existingMonthly)].Amount + subItem.Amount;
                            }

                        }
                    }

                    model.C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet.Add(retainedEarningsItem);

                    #endregion
                }
                #endregion
            }

            return View("~/Views/Operational/C02_SageLedgerReport/C02_SageLedgerReport_MonthlySummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_SageLedgerReport/C02_SageLedgerReport_Details")]
        public async Task<IActionResult> C02_SageLedgerReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_SageLedgerReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_SageLedgerReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_SageLedgerReport_DetailsModel model = new C02_SageLedgerReport_DetailsModel()
            {
                C02_SageLedgerReport_DetailsItems = new List<C02_SageLedgerReport_DetailsModel.C02_SageLedgerReport_DetailsItem>(),
                JournalName = Request.Query["JNA"],
                JournalNo = Request.Query["JNO"],
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                TransactionType = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All transactions", Value = "", Selected = string.IsNullOrEmpty(Request.Query["TransactionType"]) },
                    new SelectListItem() { Text = "Only active entries", Value = "1", Selected = !string.IsNullOrEmpty(Request.Query["TransactionType"]) },
                },
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (_operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(model.JournalNo) && !string.IsNullOrEmpty(model.JournalName))
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                if (company != null && company.SageAccountingCompanyID.HasValue)
                {
                    var sageAccounting_Company = db.SageAccounting_Companies.Where(p => p.SageID == company.SageAccountingCompanyID.Value).SingleOrDefault();
                    var ledgers = (from p in db.SageAccounting_DetailedLedgerTransactions
                                   where p.CompanyId == company.SageAccountingCompanyID
                                   && p.Date >= model.FromDate.Date
                                   && p.Date <= model.ToDate.Date
                                   && p.AccountId == Convert.ToInt32(model.JournalNo)
                                   select p).ToList();

                    foreach (var l in ledgers)
                    {
                        C02_SageLedgerReport_DetailsModel.C02_SageLedgerReport_DetailsItem item = new C02_SageLedgerReport_DetailsModel.C02_SageLedgerReport_DetailsItem()
                        {
                            Description = l.Description,
                            ID = l.ID,
                            CompanyId = l.CompanyId,
                            SageID = l.SageID,
                            AccountCategoryDescription = l.AccountCategoryDescription,
                            AccountCategoryId = l.AccountCategoryId,
                            AccountDescription = l.AccountDescription,
                            AccountId = l.AccountId,
                            AnalysisCategoryId1 = l.AnalysisCategoryId1,
                            AnalysisCategoryId2 = l.AnalysisCategoryId2,
                            AnalysisCategoryId3 = l.AnalysisCategoryId3,
                            ContraAccountDescription = l.ContraAccountDescription,
                            ContraAccountId = l.ContraAccountId,
                            Credit = l.Credit,
                            Date = l.Date,
                            Debit = l.Debit,
                            Modified = l.Modified,
                            Reference = l.Reference,
                            TaxTypeId = l.TaxTypeId,
                            TaxTypeName = l.TaxTypeName,
                            TransactionTypeDescription = l.TransactionTypeDescription,
                            TransactionTypeId = l.TransactionTypeId,
                            CompanyName = sageAccounting_Company != null ? sageAccounting_Company.Name : "",
                        };

                        model.C02_SageLedgerReport_DetailsItems.Add(item);
                    }
                }
            }

            model.C02_SageLedgerReport_DetailsItems = model.C02_SageLedgerReport_DetailsItems.OrderByDescending(p => p.Date).ToList();

            return View("~/Views/Operational/C02_SageLedgerReport/C02_SageLedgerReport_Details.cshtml", model);
        }

    }
}
