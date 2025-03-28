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
using MyVoltage.Models.OperationalModels.C02_GeneralLedgerReportModels;
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

namespace MyVoltage.Controllers.Operational.C01_ProductReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C02_GeneralLedgerReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C02_GeneralLedgerReportController(
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
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Summary")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? true : Convert.ToBoolean(Request.Query["MovementReport"]);
            C02_GeneralLedgerReport_SummaryModel model = new C02_GeneralLedgerReport_SummaryModel()
            {
                C02_GeneralLedgerReport_SummaryItems = new List<C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
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

            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Summary(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? true : Convert.ToBoolean(Request.Query["MovementReport"]);

            C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem model = new C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                C02_GeneralLedgerReport_SummarySubItems = new List<C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem.C02_GeneralLedgerReport_SummarySubItem>(),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
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
                model.C02_GeneralLedgerReport_SummarySubItems = new List<C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem.C02_GeneralLedgerReport_SummarySubItem>();

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
                                balanceSheet = (from p in db.GeneralLedgerEntries
                                                where !string.IsNullOrEmpty(p.G_L_Account_No)
                                                && Convert.ToInt32(p.G_L_Account_No) <= 5999
                                                && p.Posting_Date.Year == current.Year
                                                && p.Posting_Date.Month == current.Month
                                                && p.CompanyID == uC.CompanyID
                                                select p.Amount).Sum();

                                var balanceSheetEntries = (from p in accountingChecklists
                                                           where p.Date.Year == current.Year
                                                           && p.Date.Month == current.Month
                                                           && p.LedgerNo <= 5999
                                                           select p).ToList();


                                balanceSheetCount = balanceSheetEntries.Count;
                                balanceSheetCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count();
                                if (balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    balanceSheetLatestCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
                            }
                            else
                            {
                                var latestEntry = (from p in db.GeneralLedgerEntries
                                                   where !string.IsNullOrEmpty(p.G_L_Account_No)
                                                   && Convert.ToInt32(p.G_L_Account_No) <= 5999
                                                   && p.Posting_Date.Year == current.Year
                                                   && p.Posting_Date.Month == current.Month
                                                   && p.CompanyID == uC.CompanyID
                                                   orderby p.Entry_No descending
                                                   select p).FirstOrDefault();

                                if (latestEntry != null)
                                    balanceSheet = latestEntry.Balance;

                                var balanceSheetEntries = (from p in accountingChecklists
                                                           where p.Date.Year == current.Year
                                                           && p.Date.Month == current.Month
                                                           && p.LedgerNo <= 5999
                                                           select p).ToList();


                                balanceSheetCount = balanceSheetEntries.Count;
                                balanceSheetCompleted = balanceSheetEntries.Where(p => p.ApprovedDateBalance.HasValue).Count();
                                if (balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    balanceSheetLatestCompleted = balanceSheetEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
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
                                incomeStatement = (from p in db.GeneralLedgerEntries
                                                   where !string.IsNullOrEmpty(p.G_L_Account_No)
                                                   && Convert.ToInt32(p.G_L_Account_No) > 5999
                                                   && p.Posting_Date.Year == current.Year
                                                   && p.Posting_Date.Month == current.Month
                                                   && p.CompanyID == uC.CompanyID
                                                   select p.Amount).Sum();

                                var incomeStatementEntries = (from p in accountingChecklists
                                                              where p.Date.Year == current.Year
                                                              && p.Date.Month == current.Month
                                                              && p.LedgerNo > 5999
                                                              select p).ToList();


                                incomeStatementCount = incomeStatementEntries.Count;
                                incomeStatementCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count();
                                if (incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    incomeStatementLatestCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
                            }
                            else
                            {
                                var latestEntry = (from p in db.GeneralLedgerEntries
                                                   where !string.IsNullOrEmpty(p.G_L_Account_No)
                                                   && Convert.ToInt32(p.G_L_Account_No) > 5999
                                                   && p.Posting_Date.Year == current.Year
                                                   && p.Posting_Date.Month == current.Month
                                                   && p.CompanyID == uC.CompanyID
                                                   orderby p.Entry_No descending
                                                   select p).FirstOrDefault();

                                if (latestEntry != null)
                                    balanceSheet = latestEntry.Balance;

                                var incomeStatementEntries = (from p in accountingChecklists
                                                              where p.Date.Year == current.Year
                                                              && p.Date.Month == current.Month
                                                              && p.LedgerNo > 5999
                                                              select p).ToList();


                                incomeStatementCount = incomeStatementEntries.Count;
                                incomeStatementCompleted = incomeStatementEntries.Where(p => p.ApprovedDateBalance.HasValue).Count();
                                if (incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Count() > 0)
                                    incomeStatementLatestCompleted = incomeStatementEntries.Where(p => p.ApprovedDate.HasValue).Select(p => p.ApprovedDate.Value).Max();
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions();

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                            _cache.Set(KEY_incomeStatement, incomeStatement, cacheEntryOptions);
                        }

                        model.C02_GeneralLedgerReport_SummarySubItems.Add(new C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem.C02_GeneralLedgerReport_SummarySubItem()
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

                        model.C02_GeneralLedgerReport_SummarySubItems.Add(new C02_GeneralLedgerReport_SummaryModel.C02_GeneralLedgerReport_SummaryItem.C02_GeneralLedgerReport_SummarySubItem()
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

            return PartialView("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Monthly")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_GeneralLedgerReport_MonthlyModel model = new C02_GeneralLedgerReport_MonthlyModel()
            {
                C02_GeneralLedgerReport_MonthlyItems_BalanceSheet = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem>(),
                C02_GeneralLedgerReport_MonthlyItems_IncomeStatement = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem>(),
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
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();
                var accountingChecklists = db.AccountingChecklists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Date >= model.FromDate.Date && p.Date <= model.ToDate.Date).ToList();
                DateTime current = model.FromDate;

                if (string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" || Request.Query["MovementReport"].ToString().ToLower() == "true")
                {
                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '1'");

                    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommand.CommandTimeout = 600;
                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);

                    foreach (var c in cOA)
                    {
                        if (Convert.ToInt32(c.No) == 5920)
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem balanceSheetItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                            }
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem incomeStatementItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                }
                else if (Request.Query["MovementReport"].ToString() == "2" || Request.Query["MovementReport"].ToString().ToLower() == "false")
                {
                    List<int> cOAsToExclude = new List<int>()
            {
                1000    , // -	BALANCE SHEET
                1002    , // -	ASSETS
                1003    , // -	Fixed Assets
                1005    , // -	Tangible Fixed Assets
                1100    , // -	Land and Buildings
                1190    , // -	Land and Buildings, Total
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
                5920    , // -	Takeon Balance Adjustment
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

                    Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();

                    current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        if (current.Date <= DateTime.Now.Date)
                            cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                        current = current.AddMonths(1);
                    }

                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem balanceSheetItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem incomeStatementItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }

                }
                else if (Request.Query["MovementReport"].ToString() == "3")
                {
                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '1'");

                    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommand.CommandTimeout = 600;
                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);
                    // Movement Report
                    foreach (var c in cOA)
                    {
                        if (Convert.ToInt32(c.No) == 5920)
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)
                            continue;

                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem balanceSheetItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                            }
                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem incomeStatementItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                var tableResults = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                    CellStyle = "",
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDate.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }

                    List<int> cOAsToExclude = new List<int>()
            {
                1000    , // -	BALANCE SHEET
                1002    , // -	ASSETS
                1003    , // -	Fixed Assets
                1005    , // -	Tangible Fixed Assets
                1100    , // -	Land and Buildings
                1190    , // -	Land and Buildings, Total
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
                5920    , // -	Takeon Balance Adjustment
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

                    Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();

                    current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        if (current.Date <= DateTime.Now.Date)
                            cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                        current = current.AddMonths(1);
                    }
                    List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem> balanceSheetItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem>();
                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            //continue;
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem balanceSheetItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_MonthlyItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem incomeStatementItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                            {
                                C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;
                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                var c02_GeneralLedgerReport_MonthlySubItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                };

                                var ac = accountingChecklists.Where(p => p.Date == new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)) && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_MonthlySubItem.CellStyle = "table-success";
                                    c02_GeneralLedgerReport_MonthlySubItem.ToolTip = $"{ac.Comments}";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Add(c02_GeneralLedgerReport_MonthlySubItem);


                                current = current.AddMonths(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_MonthlySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                balanceSheetItems.Add(incomeStatementItem);
                            }
                        }
                    }


                    C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem retainedEarningsItem = new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem()
                    {
                        C02_GeneralLedgerReport_MonthlySubItems = new List<C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem>(),
                        FromDate = model.FromDate,
                        JournalName = "Retained earnings",
                        JournalNo = 0000,
                        ToDate = model.ToDate,
                        Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                    };

                    foreach (var item in balanceSheetItems)
                    {
                        foreach (var subItem in item.C02_GeneralLedgerReport_MonthlySubItems)
                        {
                            var existingMonthly = (from p in retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems
                                                   where p.Month == subItem.Month
                                                   select p).SingleOrDefault();

                            if (existingMonthly == null)
                            {
                                retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems.Add(new C02_GeneralLedgerReport_MonthlyModel.C02_GeneralLedgerReport_MonthlyItem.C02_GeneralLedgerReport_MonthlySubItem()
                                {
                                    Amount = subItem.Amount,
                                    CellStyle = "",
                                    Month = subItem.Month,
                                });
                            }
                            else
                            {
                                retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems[retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems.IndexOf(existingMonthly)].Amount = retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems[retainedEarningsItem.C02_GeneralLedgerReport_MonthlySubItems.IndexOf(existingMonthly)].Amount + subItem.Amount;
                            }

                        }
                    }

                    model.C02_GeneralLedgerReport_MonthlyItems_BalanceSheet.Add(retainedEarningsItem);

                }
            }

            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Monthly_Caseware")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Monthly_Caseware()
        {
            var db = new MyVoltageDbContext(_options);

            if (_operationalProvider.CompanyID > 0)
            {
                DateTime toDate = new DateTime(DateTime.Now.Year, 02, 01);
                if (toDate.Date <= DateTime.Now.Date)
                    toDate = new DateTime(DateTime.Now.AddYears(1).Year, 02, 01);

                DateTime fromDate = new DateTime(toDate.AddYears(-5).Year, 03, 01);

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();
                var accountingChecklists = db.AccountingChecklists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Date >= fromDate.Date && p.Date <= toDate.Date).ToList();
                Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();
                System.Data.DataTable tblResults = new DataTable("Import");

                tblResults.Columns.Add("Company Name", typeof(string));
                tblResults.Columns.Add("Account No", typeof(string));
                tblResults.Columns.Add("Account Description", typeof(string));


                DateTime current = toDate;
                bool isFirst = true;
                while (current >= fromDate)
                {
                    if (isFirst)
                        isFirst = false;
                    else if (current.Month == 2)
                    {
                        cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                        tblResults.Columns.Add($"B{current:yyyy MMM}", typeof(decimal));
                    }

                    tblResults.Columns.Add($"M{current:yyyy MMM}", typeof(decimal));

                    current = current.AddMonths(-1);
                }
                cOAs.Add(new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month)), skyBillApiClient.GetChartOfAccounts(new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month))));
                tblResults.Columns.Add($"B{new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month)):yyyy MMM}", typeof(decimal));


                #region Get Figures

                StringBuilder sqlQueryMovement = new StringBuilder();
                sqlQueryMovement.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthForCompany] '{fromDate.ToString("yyyy-MM-dd")}', '{toDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '1'");

                SqlCommand sqlCommandMovement = new SqlCommand(sqlQueryMovement.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandMovement.CommandTimeout = 600;
                System.Data.DataTable dataTableMovement = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandMovement).Fill(dataTableMovement);

                List<int> cOAsToExclude = new List<int>()
            {
                1000    , // -	BALANCE SHEET
                1002    , // -	ASSETS
                1003    , // -	Fixed Assets
                1005    , // -	Tangible Fixed Assets
                1100    , // -	Land and Buildings
                1190    , // -	Land and Buildings, Total
                //1200    , // -	Operating Equipment
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
                5920    , // -	Takeon Balance Adjustment
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

                foreach (var c in cOA)
                {
                    if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                        continue;
                    if (Convert.ToInt32(c.No) == 5920)
                        continue;

                    DataRow drNew = tblResults.NewRow();
                    drNew["Company Name"] = $"{_operationalProvider.CompanyName.Substring(0, 7)}";
                    drNew["Account No"] = $"{c.No}";
                    drNew["Account Description"] = c.Name;

                    current = toDate;
                    isFirst = true;
                    bool showRow = false;

                    while (current >= fromDate)
                    {
                        decimal amount = 0;

                        var tableResults = dataTableMovement.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");

                        foreach (var dr in tableResults)
                        {
                            amount += Convert.ToDecimal(dr["Amount"]);
                        }
                        if (amount != 0)
                            showRow = true;
                        drNew[$"M{current:yyyy MMM}"] = amount;

                        if (isFirst)
                            isFirst = false;
                        else if (current.Month == 2)
                        {
                            if (Convert.ToInt32(c.No) >= 5910)
                            {
                                drNew[$"B{current:yyyy MMM}"] = 0;
                            }
                            else
                            {
                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        drNew[$"B{current:yyyy MMM}"] = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }
                            }
                        }

                        current = current.AddMonths(-1);
                    }

                    if (Convert.ToInt32(c.No) >= 5910)
                    {
                        drNew[$"B{new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month)):yyyy MMM}"] = 0;
                    }
                    else
                    {
                        var cOAforClosingDate = cOAs[new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month))];

                        var cOAforCosingGL = cOAforClosingDate.Where(p => p.No == c.No).SingleOrDefault();

                        if (cOAforCosingGL != null)
                            drNew[$"B{new DateTime(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month, DateTime.DaysInMonth(fromDate.AddMonths(-1).Year, fromDate.AddMonths(-1).Month)):yyyy MMM}"] = Convert.ToDecimal(cOAforCosingGL.Balance_at_Date);
                    }

                    if (showRow)
                    {
                        tblResults.Rows.Add(drNew);
                        tblResults.AcceptChanges();
                    }
                }

                #endregion

                #region System rounding differences

                DataRow drDiff = tblResults.NewRow();

                drDiff["Company Name"] = $"{_operationalProvider.CompanyName.Substring(0, 7)}";
                drDiff["Account No"] = $"5999";
                drDiff["Account Description"] = "System rounding differences";

                foreach (DataColumn col in tblResults.Columns)
                {
                    if (col.ColumnName == "Company Name"
                        || col.ColumnName == "Account No"
                        || col.ColumnName == "Account Description")
                        continue;
                    decimal value1 = 0;
                    decimal value2 = 0;
                    foreach (DataRow dr in tblResults.Rows)
                    {
                        if (Convert.ToInt32(dr["Account No"]) < 6000)
                            value1 += Convert.ToDecimal(dr[col.ColumnName]);
                        else
                            value2 += Convert.ToDecimal(dr[col.ColumnName]);
                    }
                    drDiff[col.ColumnName] = (value1 + value2) * -1.0m;
                }
                int insertIndex = 0;
                foreach (DataRow dr in tblResults.Rows)
                {
                    if (Convert.ToInt32(dr["Account No"]) < 6000)
                        insertIndex++;
                }

                tblResults.Rows.InsertAt(drDiff, insertIndex);
                tblResults.AcceptChanges();

                #endregion

                #region VAT Account Sum

                DataRow drVAT = tblResults.NewRow();

                drVAT["Company Name"] = $"{_operationalProvider.CompanyName.Substring(0, 7)}";
                drVAT["Account No"] = $"5600";
                drVAT["Account Description"] = "Value Added Taxation";

                List<DataRow> rowsToRemove = new List<DataRow>();
                foreach (DataRow dr in tblResults.Rows)
                {
                    if (Convert.ToInt32(dr["Account No"]) == 5611)
                        insertIndex = tblResults.Rows.IndexOf(dr);
                    if (Convert.ToInt32(dr["Account No"]) == 5611
                        || Convert.ToInt32(dr["Account No"]) == 5621)
                    {
                        foreach (DataColumn col in tblResults.Columns)
                        {
                            if (col.ColumnName == "Company Name"
                                || col.ColumnName == "Account No"
                                || col.ColumnName == "Account Description")
                                continue;
                            if (drVAT[col.ColumnName] != DBNull.Value)
                                drVAT[col.ColumnName] = Convert.ToDecimal(drVAT[col.ColumnName]) + Convert.ToDecimal(dr[col.ColumnName]);
                            else
                                drVAT[col.ColumnName] = Convert.ToDecimal(dr[col.ColumnName]);
                        }
                        rowsToRemove.Add(dr);
                    }
                }


                tblResults.Rows.InsertAt(drVAT, insertIndex);
                tblResults.AcceptChanges();
                foreach (var dr in rowsToRemove)
                    tblResults.Rows.Remove(dr);
                tblResults.AcceptChanges();

                #endregion

                #region Excel Export

                Stream excelFile = new MemoryStream();
                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var scheduledWorksheet = workbook.Worksheets.Add(tblResults.TableName);
                    var scheduledTable = scheduledWorksheet.Cell(1, 1).InsertTable(tblResults, tblResults.TableName, true);
                    scheduledWorksheet.Columns("A", "ZZ").AdjustToContents();
                    if (workbook.Worksheets.Count > 0)
                        workbook.SaveAs(excelFile);
                }

                if (excelFile != null && excelFile.Length > 0)
                {
                    excelFile.Position = 0;
                    return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{_operationalProvider.CompanyName.Substring(0, 7)}_CWImport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}" + ".xlsx");
                }


                #endregion

            }

            return Redirect("/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Monthly");
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Daily")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = false;
            if (string.IsNullOrEmpty(Request.Query["MovementReport"]))
            {
                movementReport = true;
            }
            else
            {
                try
                {
                    movementReport = Convert.ToBoolean(Request.Query["MovementReport"]);
                }
                catch
                {
                    if (Request.Query["MovementReport"].ToString() == "1")
                        movementReport = true;
                    else
                        movementReport = false;
                }
            }
            C02_GeneralLedgerReport_DailyModel model = new C02_GeneralLedgerReport_DailyModel()
            {
                C02_GeneralLedgerReport_DailyItems_BalanceSheet = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem>(),
                C02_GeneralLedgerReport_DailyItems_IncomeStatement = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.FromDate.Year, model.FromDate.Month, DateTime.DaysInMonth(model.FromDate.Year, model.FromDate.Month));

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                if (movementReport)
                {
                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByDayForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '{(movementReport ? "1" : "0")}'");

                    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommand.CommandTimeout = 600;

                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);

                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    foreach (var c in cOA)
                    {
                        if (Convert.ToInt32(c.No) == 5920)
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem balanceSheetItem = new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem()
                            {
                                C02_GeneralLedgerReport_DailySubItems = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            DateTime current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                var tableResults = dataTable.Select($"[Posting_Date] = '{current.ToString("yyyy-MM-dd")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_DailySubItems.Add(new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                });


                                current = current.AddDays(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                                model.C02_GeneralLedgerReport_DailyItems_BalanceSheet.Add(balanceSheetItem);

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem incomeStatementItem = new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem()
                            {
                                C02_GeneralLedgerReport_DailySubItems = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            DateTime current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                var tableResults = dataTable.Select($"[Posting_Date] = '{current.ToString("yyyy-MM-dd")}' And G_L_Account_No = '{c.No}'");

                                foreach (var dr in tableResults)
                                {
                                    if (amount.HasValue)
                                        amount = amount.Value + Convert.ToDecimal(dr["Amount"]);
                                    else
                                        amount = Convert.ToDecimal(dr["Amount"]);
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_DailySubItems.Add(new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                });


                                current = current.AddDays(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                                model.C02_GeneralLedgerReport_DailyItems_IncomeStatement.Add(incomeStatementItem);
                        }
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
                5920    , // -	Takeon Balance Adjustment
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

                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();


                    DateTime current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        if (current.Date <= DateTime.Now.Date)
                            cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(current.Date));
                        current = current.AddDays(1);
                    }

                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;

                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem balanceSheetItem = new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem()
                            {
                                C02_GeneralLedgerReport_DailySubItems = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_DailySubItems.Add(new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                });


                                current = current.AddDays(1);
                            }
                            if (balanceSheetItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_DailyItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem incomeStatementItem = new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem()
                            {
                                C02_GeneralLedgerReport_DailySubItems = new List<C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem>(),
                                FromDate = model.FromDate,
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                ToDate = model.ToDate,
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.IncomeStatement
                            };

                            current = model.FromDate;

                            while (current <= model.ToDate)
                            {
                                decimal? amount = null;

                                if (cOAs.ContainsKey(current))
                                {
                                    var cOAforDate = cOAs[current];

                                    var cOAforGL = cOAforDate.Where(p => p.No == c.No).SingleOrDefault();

                                    if (cOAforGL != null)
                                        amount = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_DailySubItems.Add(new C02_GeneralLedgerReport_DailyModel.C02_GeneralLedgerReport_DailyItem.C02_GeneralLedgerReport_DailySubItem()
                                {
                                    Amount = amount,
                                    Month = current,
                                });


                                current = current.AddDays(1);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_DailySubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_DailyItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                }

            }

            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Details")]
        public async Task<IActionResult> C02_GeneralLedgerReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_GeneralLedgerReport_DetailsModel model = new C02_GeneralLedgerReport_DetailsModel()
            {
                C02_GeneralLedgerReport_DetailsItems = new List<C02_GeneralLedgerReport_DetailsModel.C02_GeneralLedgerReport_DetailsItem>(),
                JournalName = Request.Query["JNA"],
                JournalNo = Request.Query["JNO"],
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                TransactionType = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All transactions", Value = "", Selected = string.IsNullOrEmpty(Request.Query["TransactionType"]) },
                    new SelectListItem() { Text = "Only active entries", Value = "1", Selected = !string.IsNullOrEmpty(Request.Query["TransactionType"]) },
                }
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            //if (model.ToDate >= model.FromDate.AddMonths(1))
            //    model.ToDate = new DateTime(model.FromDate.AddMonths(1).Year, model.FromDate.AddMonths(1).Month, model.FromDate.AddMonths(1).Day);

            if (_operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(model.JournalNo) && !string.IsNullOrEmpty(model.JournalName))
            {
                var ledgers = (from p in db.GeneralLedgerEntries
                               where p.CompanyID == _operationalProvider.CompanyID
                               && p.Posting_Date.Date >= model.FromDate.Date
                               && p.Posting_Date <= model.ToDate.Date
                               && p.G_L_Account_No == model.JournalNo
                               select p).ToList();

                foreach (var l in ledgers)
                {
                    if (!string.IsNullOrEmpty(Request.Query["TransactionType"]))
                    {
                        var reversedEntry = (from p in ledgers
                                             where p.Document_No == l.Document_No
                                             && p.Reversed
                                             select p).FirstOrDefault();

                        if (reversedEntry != null)
                            continue;
                    }

                    C02_GeneralLedgerReport_DetailsModel.C02_GeneralLedgerReport_DetailsItem item = new C02_GeneralLedgerReport_DetailsModel.C02_GeneralLedgerReport_DetailsItem()
                    {
                        Additional_Currency_Amount = l.Additional_Currency_Amount,
                        Amount = l.Amount,
                        Bal_Account_No = l.Bal_Account_No,
                        Bal_Account_Type = l.Bal_Account_Type,
                        CompanyID = l.CompanyID,
                        CreateDate = l.CreateDate,
                        Description = l.Description,
                        Document_No = l.Document_No,
                        Document_Type = l.Document_Type,
                        Entry_No = l.Entry_No,
                        ETag = l.ETag,
                        FA_Entry_No = l.FA_Entry_No,
                        FA_Entry_Type = l.FA_Entry_Type,
                        FL_Contract_No = l.FL_Contract_No,
                        FromDate = model.FromDate,
                        Gen_Bus_Posting_Group = l.Gen_Bus_Posting_Group,
                        Gen_Posting_Type = l.Gen_Posting_Type,
                        Gen_Prod_Posting_Group = l.Gen_Prod_Posting_Group,
                        Global_Dimension_1_Code = l.Global_Dimension_1_Code,
                        Global_Dimension_2_Code = l.Global_Dimension_2_Code,
                        G_L_Account_Name = l.G_L_Account_Name,
                        G_L_Account_No = l.G_L_Account_No,
                        IC_Partner_Code = l.IC_Partner_Code,
                        ID = l.ID,
                        Job_No = l.Job_No,
                        Posting_Date = l.Posting_Date,
                        Quantity = l.Quantity,
                        Reason_Code = l.Reason_Code,
                        Reversed = l.Reversed,
                        Reversed_Entry_No = l.Reversed_Entry_No,
                        Source_Code = l.Source_Code,
                        ToDate = model.ToDate,
                        User_ID = l.User_ID,
                        VAT_Amount = l.VAT_Amount,
                        Balance = l.Balance,
                    };

                    model.C02_GeneralLedgerReport_DetailsItems.Add(item);
                }
            }

            model.C02_GeneralLedgerReport_DetailsItems = model.C02_GeneralLedgerReport_DetailsItems.OrderByDescending(p => p.Posting_Date).ToList();

            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_ConsolidatedTB")]
        public async Task<IActionResult> C02_GeneralLedgerReport_ConsolidatedTB()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_ConsolidatedTB, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_ConsolidatedTB}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_GeneralLedgerReport_ConsolidatedTBModel model = new C02_GeneralLedgerReport_ConsolidatedTBModel()
            {
                C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem>(),
                C02_GeneralLedgerReport_ConsolidatedTBItems_IncomeStatement = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-12).Year, DateTime.Now.AddMonths(-12).Month, 1),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
                Companies = new Dictionary<int, string>(),
                PartnerID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Select Partner]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "1", Text = "Movement Report", Selected = string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "2", Text = "Balance Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "2" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "3", Text = "Accountant Report", Selected = !string.IsNullOrEmpty(Request.Query["MovementReport"]) && Request.Query["MovementReport"].ToString() == "3" },
                },
            };

            if (string.IsNullOrEmpty(Request.Query["MovementReport"]))
                return Redirect($"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_ConsolidatedTB?MovementReport=3");

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            var db = new MyVoltageDbContext(_options);
            model.PartnerID.AddRange((from p in db.SiteAdmin_Partners
                                      orderby p.PartnerName
                                      select new SelectListItem()
                                      {
                                          Value = p.ID.ToString(),
                                          Text = p.PartnerName,
                                          Selected = _operationalProvider.PartnerID == p.ID
                                      }).ToList());


            var companies = db.Companies.ToList();

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
                5920    , // -	Takeon Balance Adjustment
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


            if (Request.QueryString.HasValue)
            {
                var accountingChecklists = db.AccountingChecklists.Where(p => p.Date.Year == model.FromDate.Date.Year && p.Date.Month == model.FromDate.Date.Month).ToList();
                if (string.IsNullOrEmpty(Request.Query["MovementReport"]) || Request.Query["MovementReport"].ToString() == "1" || Request.Query["MovementReport"].ToString().ToLower() == "true")
                {
                    StringBuilder sqlQueryMovement = new StringBuilder();

                    sqlQueryMovement.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthPerCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '0', '1'");

                    SqlCommand sqlCommandMovement = new SqlCommand(sqlQueryMovement.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommandMovement.CommandTimeout = 600;

                    System.Data.DataTable dataTableMovement = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommandMovement).Fill(dataTableMovement);

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(companies.OrderBy(p => p.Name).Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).FirstOrDefault().Name, _cache);

                    if (_operationalProvider.CompanyID != 0)
                        skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem balanceSheetItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableMovement.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem incomeStatementItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableMovement.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                }
                else if (Request.Query["MovementReport"].ToString() == "2" || Request.Query["MovementReport"].ToString().ToLower() == "false")
                {
                    StringBuilder sqlQueryBalance = new StringBuilder();

                    if (_operationalProvider.PartnerID == 0)
                        sqlQueryBalance.AppendLine($"exec [sp_GetGeneralNoLedgersTotals] '', '{model.ToDate.ToString("yyyy-MM-dd")}'");
                    else
                        sqlQueryBalance.AppendLine($"exec [sp_GetGeneralNoLedgersTotals] '{_operationalProvider.PartnerID}', '{model.ToDate.ToString("yyyy-MM-dd")}'");

                    SqlCommand sqlCommandBalance = new SqlCommand(sqlQueryBalance.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommandBalance.CommandTimeout = 600;

                    System.Data.DataTable dataTableBalance = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommandBalance).Fill(dataTableBalance);

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(companies.OrderBy(p => p.Name).Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).FirstOrDefault().Name, _cache);

                    if (_operationalProvider.CompanyID != 0)
                        skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem balanceSheetItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableBalance.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem incomeStatementItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableBalance.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                }
                else if (Request.Query["MovementReport"].ToString() == "3")
                {
                    StringBuilder sqlQueryMovement = new StringBuilder();

                    sqlQueryMovement.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthPerCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '0', '1'");

                    SqlCommand sqlCommandMovement = new SqlCommand(sqlQueryMovement.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommandMovement.CommandTimeout = 600;

                    System.Data.DataTable dataTableMovement = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommandMovement).Fill(dataTableMovement);

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(companies.OrderBy(p => p.Name).Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).FirstOrDefault().Name, _cache);

                    if (_operationalProvider.CompanyID != 0)
                        skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) == 5920)
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            continue;
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem balanceSheetItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableMovement.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem incomeStatementItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableMovement.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_IncomeStatement.Add(incomeStatementItem);
                            }
                        }
                    }
                    StringBuilder sqlQueryBalance = new StringBuilder();

                    if (_operationalProvider.PartnerID == 0)
                        sqlQueryBalance.AppendLine($"exec [sp_GetGeneralNoLedgersTotals] '', '{model.ToDate.ToString("yyyy-MM-dd")}'");
                    else
                        sqlQueryBalance.AppendLine($"exec [sp_GetGeneralNoLedgersTotals] '{_operationalProvider.PartnerID}', '{model.ToDate.ToString("yyyy-MM-dd")}'");

                    SqlCommand sqlCommandBalance = new SqlCommand(sqlQueryBalance.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                    sqlCommandBalance.CommandTimeout = 600;

                    System.Data.DataTable dataTableBalance = new System.Data.DataTable();
                    new SqlDataAdapter(sqlCommandBalance).Fill(dataTableBalance);

                    List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem> balanceSheetItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem>();
                    foreach (var c in cOA)
                    {
                        if (cOAsToExclude.Contains(Convert.ToInt32(c.No)))
                            continue;
                        if (Convert.ToInt32(c.No) < 6000)
                        {
                            // BALANCE SHEET ( Acc No 1 - 5999)

                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem balanceSheetItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableBalance.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && balanceSheetItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                model.C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet.Add(balanceSheetItem);
                            }

                        }
                        else
                        {
                            // INCOME STATEMENT (Acc No 6000 - 9999)
                            C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem incomeStatementItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                            {
                                JournalName = c.Name,
                                JournalNo = Convert.ToInt32(c.No),
                                Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                                C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                            };

                            foreach (var co in companies)
                            {
                                if (_operationalProvider.PartnerID != 0 && _operationalProvider.PartnerID.ToString() != co.PartnerID.ToString())
                                    continue;

                                decimal amount = 0;
                                foreach (System.Data.DataRow dr in dataTableBalance.Select($"[G_L_Account_No] = '{c.No}' And [CompanyID] = '{co.CompanyID}'"))
                                {
                                    amount += Convert.ToDecimal(dr["Amount"]);
                                }

                                C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem c02_GeneralLedgerReport_ConsolidatedTBSubItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = amount,
                                    CompanyID = co.CompanyID,
                                    CompanyName = co.Name,
                                };

                                var ac = accountingChecklists.Where(p => p.CompanyID == co.CompanyID && p.LedgerNo == Convert.ToInt32(c.No)).SingleOrDefault();
                                if (ac != null)
                                {
                                    if (!ac.ReviewedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-danger";
                                    else if (!ac.ApprovedDateBalance.HasValue)
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-warning";
                                    else
                                        c02_GeneralLedgerReport_ConsolidatedTBSubItem.CellStyle = "table-success";
                                }

                                incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(c02_GeneralLedgerReport_ConsolidatedTBSubItem);

                                if (!model.Companies.ContainsKey(co.CompanyID))
                                    model.Companies.Add(co.CompanyID, co.Name);
                            }

                            if (incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Count() > 0)
                            {
                                if (model.HideNoData && incomeStatementItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Where(p => p.Amount.HasValue).Select(p => p.Amount.Value).Sum() == 0)
                                    continue;
                                balanceSheetItems.Add(incomeStatementItem);
                            }
                        }
                    }

                    C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem retainedEarningsItem = new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem()
                    {
                        C02_GeneralLedgerReport_ConsolidatedTBSubItems = new List<C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem>(),
                        JournalName = "Retained earnings",
                        JournalNo = 0000,
                        Type = GeneralJournal.GeneralJournalLedgerType.TypeEnum.BalanceSheet,
                    };

                    foreach (var item in balanceSheetItems)
                    {
                        foreach (var subItem in item.C02_GeneralLedgerReport_ConsolidatedTBSubItems)
                        {
                            var existingMonthly = (from p in retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems
                                                   where p.CompanyID == subItem.CompanyID
                                                   select p).SingleOrDefault();

                            if (existingMonthly == null)
                            {
                                retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.Add(new C02_GeneralLedgerReport_ConsolidatedTBModel.C02_GeneralLedgerReport_ConsolidatedTBItem.C02_GeneralLedgerReport_ConsolidatedTBSubItem()
                                {
                                    Amount = subItem.Amount,
                                    CellStyle = "",
                                    CompanyID = subItem.CompanyID,
                                    CompanyName = subItem.CompanyName,
                                });
                            }
                            else
                            {
                                retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems[retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.IndexOf(existingMonthly)].Amount = retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems[retainedEarningsItem.C02_GeneralLedgerReport_ConsolidatedTBSubItems.IndexOf(existingMonthly)].Amount + subItem.Amount;
                            }

                        }
                    }

                    model.C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet.Add(retainedEarningsItem);
                }
            }
            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_ConsolidatedTB.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditView()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditView, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditView}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C02_GeneralLedgerReport_GLAuditViewModel model = new C02_GeneralLedgerReport_GLAuditViewModel()
            {
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month)),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month)),
                ApprovalRequired = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Statuses", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) },
                    new SelectListItem() { Text = $"Approved Required", Value = $"1", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"1" },
                    new SelectListItem() { Text = $"Reviewed Required", Value = $"2", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"2" },
                    new SelectListItem() { Text = $"Confirmed Amount Required", Value = $"3", Selected = !string.IsNullOrEmpty(Request.Query["ApprovalRequired"]) && Request.Query["ApprovalRequired"].ToString() == $"3" },
                },
                C02_GeneralLedgerReport_GLAuditViewItems = new List<C02_GeneralLedgerReport_GLAuditViewModel.C02_GeneralLedgerReport_GLAuditViewItem>(),
                LedgerNo = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Ledgers", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["LedgerNo"]) },
                },
                PendingIssue = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"Pending and Non Pending Issues", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["PendingIssue"]) },
                    new SelectListItem() { Text = $"Pending Issues Only", Value = $"1", Selected = !string.IsNullOrEmpty(Request.Query["PendingIssue"]) && Request.Query["PendingIssue"].ToString() == $"1" },
                    new SelectListItem() { Text = $"Non Pending Issues Only", Value = $"2", Selected = !string.IsNullOrEmpty(Request.Query["PendingIssue"]) && Request.Query["PendingIssue"].ToString() == $"2" },
                },
                Classification = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All Classifications", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["Classification"]) },
                    new SelectListItem() { Text = $"Balance Sheet", Value = $"1", Selected = !string.IsNullOrEmpty(Request.Query["Classification"]) && Request.Query["Classification"].ToString() == $"1" },
                    new SelectListItem() { Text = $"Income Statement", Value = $"2", Selected = !string.IsNullOrEmpty(Request.Query["Classification"]) && Request.Query["Classification"].ToString() == $"2" },
                },
                AmountClassification = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = $"All", Value = $"", Selected = string.IsNullOrEmpty(Request.Query["AmountClassification"]) },
                    new SelectListItem() { Text = $"Same only", Value = $"1", Selected = !string.IsNullOrEmpty(Request.Query["AmountClassification"]) && Request.Query["AmountClassification"].ToString() == $"1" },
                    new SelectListItem() { Text = $"Different only", Value = $"2", Selected = !string.IsNullOrEmpty(Request.Query["AmountClassification"]) && Request.Query["AmountClassification"].ToString() == $"2" },
                },
            };

            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var sageAccounting_Accounts = db.SageAccounting_Accounts.ToList();
                var sageAccounting_Companies = db.SageAccounting_Companies.ToList();
                var sageAccounting_JournalLogs = db.SageAccounting_JournalLogs.ToList();
                var latest_AccountingChecklist_Request = (from p in db.F_SystemGeneratedReports_AccountingChecklist_Requests
                                                          where p.CompanyID.HasValue
                                                          && p.CompanyID == _operationalProvider.CompanyID
                                                          && p.FromDate.Date == new DateTime(2018, 01, 01)
                                                          orderby p.CreatedDate descending
                                                          select p).FirstOrDefault();

                if (latest_AccountingChecklist_Request != null)
                {

                    model.Latest_AccountingChecklist_Request = new C02_GeneralLedgerReport_GLAuditViewModel.F_SystemGeneratedReports_AccountingChecklist_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latest_AccountingChecklist_Request.CreatedBy,
                        CompanyID = latest_AccountingChecklist_Request.CompanyID,
                        CreatedDate = latest_AccountingChecklist_Request.CreatedDate,
                        DateEnded = latest_AccountingChecklist_Request.DateEnded,
                        DateStarted = latest_AccountingChecklist_Request.DateStarted,
                        FromDate = latest_AccountingChecklist_Request.FromDate,
                        ID = latest_AccountingChecklist_Request.ID,
                        Progress = latest_AccountingChecklist_Request.Progress,
                        SystemReportID = latest_AccountingChecklist_Request.SystemReportID,
                        ToDate = latest_AccountingChecklist_Request.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latest_AccountingChecklist_Request.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latest_AccountingChecklist_Request.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.Latest_AccountingChecklist_Request.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                var latest_Journal_Request = (from p in db.F_SystemGeneratedReports_SageAccounting_JournalRequests
                                              where p.CompanyID.HasValue
                                              && p.CompanyID == _operationalProvider.CompanyID
                                              orderby p.CreatedDate descending
                                              select p).FirstOrDefault();

                if (latest_Journal_Request != null)
                {

                    model.Latest_Journal_Request = new C02_GeneralLedgerReport_GLAuditViewModel.SageAccounting_JournalRequests_AccountingChecklistID_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latest_Journal_Request.CreatedBy,
                        CompanyID = latest_Journal_Request.CompanyID,
                        CreatedDate = latest_Journal_Request.CreatedDate,
                        DateEnded = latest_Journal_Request.DateEnded,
                        DateStarted = latest_Journal_Request.DateStarted,
                        FromDate = latest_Journal_Request.FromDate,
                        ID = latest_Journal_Request.ID,
                        Progress = latest_Journal_Request.Progress,
                        SystemReportID = latest_Journal_Request.SystemReportID,
                        ToDate = latest_Journal_Request.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latest_Journal_Request.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latest_Journal_Request.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.Latest_Journal_Request.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();
                model.LedgerNo.AddRange((from p in cOA
                                         select new
                                         SelectListItem()
                                         {
                                             Text = $"{p.No} - {p.Name}",
                                             Value = p.No,
                                             Selected = !string.IsNullOrEmpty(Request.Query["LedgerNo"]) && Request.Query["LedgerNo"].ToString() == p.No,
                                         }).ToList());

                var accountingChecklists = (from p in db.AccountingChecklists
                                            join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                            from c in sc.DefaultIfEmpty()
                                            where p.Date >= model.FromDate.Date
                                            && p.Date <= model.ToDate.Date
                                            && p.CompanyID == _operationalProvider.CompanyID
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
                                                p.AuditBy,
                                                p.AuditByBalance,
                                                p.AuditComments,
                                                p.AuditDate,
                                                p.AuditDateBalance,
                                                p.SageCompanyID,
                                                p.SageJournalID,
                                                c.SageAccountingLegalEntityCompanyID,
                                            }).ToList();

                if (!string.IsNullOrEmpty(Request.Query["PendingIssue"]))
                {
                    if (Request.Query["PendingIssue"].ToString() == $"1") // Pending Issues Only
                        accountingChecklists = accountingChecklists.Where(p => !string.IsNullOrEmpty(p.PendingIssue)).ToList();
                    else if (Request.Query["PendingIssue"].ToString() == $"2") // Non Pending Issues Only
                        accountingChecklists = accountingChecklists.Where(p => string.IsNullOrEmpty(p.PendingIssue)).ToList();
                }

                if (!string.IsNullOrEmpty(Request.Query["ApprovalRequired"]))
                {
                    if (Request.Query["ApprovalRequired"].ToString() == $"1") // Approved Required
                        accountingChecklists = accountingChecklists.Where(p => (p.MovementAmountConfirmed.HasValue && !p.ApprovedDate.HasValue) || (p.BalanceAmountConfirmed.HasValue && !p.ApprovedDateBalance.HasValue)).ToList();
                    else if (Request.Query["ApprovalRequired"].ToString() == $"2") // Reviewed Required
                        accountingChecklists = accountingChecklists.Where(p => (p.MovementAmountConfirmed.HasValue && !p.ReviewedDate.HasValue) || (p.BalanceAmountConfirmed.HasValue && !p.ReviewedDateBalance.HasValue)).ToList();
                    else if (Request.Query["ApprovalRequired"].ToString() == $"3") // Confirmed Amount Required
                        accountingChecklists = accountingChecklists.Where(p => (!p.MovementAmountConfirmed.HasValue) || (!p.BalanceAmountConfirmed.HasValue)).ToList();
                }

                if (!string.IsNullOrEmpty(Request.Query["Classification"]))
                {
                    if (Request.Query["Classification"].ToString() == $"1") // BALANCE SHEET ( Acc No 1 - 5999)
                        accountingChecklists = accountingChecklists.Where(p => p.LedgerNo <= 5999).ToList();
                    else if (Request.Query["Classification"].ToString() == $"2") // INCOME STATEMENT (Acc No 6000 - 9999) 
                        accountingChecklists = accountingChecklists.Where(p => p.LedgerNo > 5999).ToList();
                }

                if (!string.IsNullOrEmpty(Request.Query["LedgerNo"]))
                    accountingChecklists = accountingChecklists.Where(p => Request.Query["LedgerNo"].ToString() == p.LedgerNo.ToString()).ToList();

                accountingChecklists = accountingChecklists.OrderBy(p => p.CompanyName).ThenBy(p => p.Date).ThenBy(p => p.LedgerNo).ToList();

                foreach (var ac in accountingChecklists)
                {
                    C02_GeneralLedgerReport_GLAuditViewModel.C02_GeneralLedgerReport_GLAuditViewItem item = new C02_GeneralLedgerReport_GLAuditViewModel.C02_GeneralLedgerReport_GLAuditViewItem()
                    {
                        ApprovedBy = ac.ApprovedBy,
                        ApprovedByUsername = "",
                        ApprovedDate = ac.ApprovedDate,
                        BalanceAmount = ac.BalanceAmount,
                        BalanceAmountConfirmed = ac.BalanceAmountConfirmed,
                        BalanceSyncDate = ac.BalanceSyncDate,
                        Comments = ac.Comments,
                        CompanyID = ac.CompanyID,
                        CompanyName = ac.CompanyName,
                        Date = ac.Date,
                        FlagID = ac.FlagID,
                        ID = ac.ID,
                        LedgerName = ac.LedgerName,
                        LedgerNo = ac.LedgerNo,
                        MovementAmount = ac.MovementAmount,
                        MovementAmountConfirmed = ac.MovementAmountConfirmed,
                        MovementSyncDate = ac.MovementSyncDate,
                        PendingIssue = ac.PendingIssue,
                        ReviewedBy = ac.ReviewedBy,
                        ReviewedByUsername = "",
                        ReviewedDate = ac.ReviewedDate,
                        TaskID = ac.TaskID,
                        ApprovedByBalance = ac.ApprovedByBalance,
                        ApprovedDateBalance = ac.ApprovedDateBalance,
                        ReviewedDateBalance = ac.ReviewedDateBalance,
                        ReviewedByBalance = ac.ReviewedByBalance,
                        AttachmentFileName = ac.AttachmentFileName,
                        LastCheckedDate = ac.LastCheckedDate,
                        AuditBy = ac.AuditBy,
                        AuditByBalance = ac.AuditByBalance,
                        AuditComments = ac.AuditComments,
                        AuditDate = ac.AuditDate,
                        AuditDateBalance = ac.AuditDateBalance,
                        ApprovedByUsernameBalance = "",
                        AuditByUsername = "",
                        AuditByUsernameBalance = "",
                        ReviewedByUsernameBalance = "",
                        SageCompanyID = ac.SageCompanyID,
                        SageJournalID = ac.SageJournalID,
                        SageCompanyName = "",
                        SageAmount = null,
                        ShowJournalLink = false,
                    };

                    if (!string.IsNullOrEmpty(item.ReviewedBy))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedBy.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsername = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }

                    if (!string.IsNullOrEmpty(item.ReviewedByBalance))
                    {
                        var opReviewedByBalance = opProfs.Where(p => p.UserID == item.ReviewedByBalance.Trim()).SingleOrDefault();
                        if (opReviewedByBalance != null)
                            item.ReviewedByUsernameBalance = $"{opReviewedByBalance.FirstName} {opReviewedByBalance.LastName}";
                    }

                    if (!string.IsNullOrEmpty(item.ApprovedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                    if (!string.IsNullOrEmpty(item.ApprovedByBalance))
                    {
                        var opApprovedByBalance = opProfs.Where(p => p.UserID == item.ApprovedByBalance.Trim()).SingleOrDefault();
                        if (opApprovedByBalance != null)
                            item.ApprovedByUsernameBalance = $"{opApprovedByBalance.FirstName} {opApprovedByBalance.LastName}";
                    }

                    if (!string.IsNullOrEmpty(item.AuditBy))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditBy.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsername = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    if (!string.IsNullOrEmpty(item.AuditByBalance))
                    {
                        var opAuditByBalance = opProfs.Where(p => p.UserID == item.AuditByBalance.Trim()).SingleOrDefault();
                        if (opAuditByBalance != null)
                            item.AuditByUsernameBalance = $"{opAuditByBalance.FirstName} {opAuditByBalance.LastName}";
                    }

                    if (ac.SageCompanyID.HasValue)
                    {
                        var sageAccounting_Company = sageAccounting_Companies.Where(p => p.SageID == ac.SageCompanyID).SingleOrDefault();
                        if (sageAccounting_Company != null)
                            item.SageCompanyName = $"{sageAccounting_Company.Name}";
                    }

                    if (!ac.SageJournalID.HasValue && ac.SageAccountingLegalEntityCompanyID.HasValue)
                    {
                        string accountSearchTerm = $"{_operationalProvider.CompanyName.Substring(0, 7)}.{ac.LedgerNo}.{ac.LedgerName}";
                        var sageAccounting_Account = sageAccounting_Accounts.Where(p => p.Name == accountSearchTerm && p.CompanyId == ac.SageAccountingLegalEntityCompanyID).FirstOrDefault();
                        string contraAccountSearchTerm = $"{_operationalProvider.CompanyName.Substring(0, 7)}.5999.System rounding differences";
                        var sageAccounting_ContraAccount = db.SageAccounting_Accounts.Where(p => p.Name == contraAccountSearchTerm && p.CompanyId == ac.SageAccountingLegalEntityCompanyID).FirstOrDefault();
                        if (sageAccounting_Account != null && sageAccounting_ContraAccount != null && ac.ApprovedDate.HasValue && ac.ApprovedDateBalance.HasValue)
                        {
                            item.ShowJournalLink = true;
                        }
                    }

                    if (ac.SageJournalID.HasValue)
                    {
                        var journal = sageAccounting_JournalLogs.Where(p => p.SageID == ac.SageJournalID.Value).FirstOrDefault();
                        if (journal != null)
                        {
                            item.SageAmount = journal.Amount;
                        }
                    }


                    model.C02_GeneralLedgerReport_GLAuditViewItems.Add(item);
                }

                if (!string.IsNullOrEmpty(Request.Query["AmountClassification"]))
                {
                    if (Request.Query["AmountClassification"].ToString() == $"1")
                        model.C02_GeneralLedgerReport_GLAuditViewItems = model.C02_GeneralLedgerReport_GLAuditViewItems.Where(p => p.SageAmount.HasValue && p.SageAmount.Value == p.MovementAmount).ToList();
                    else if (Request.Query["AmountClassification"].ToString() == $"2")
                        model.C02_GeneralLedgerReport_GLAuditViewItems = model.C02_GeneralLedgerReport_GLAuditViewItems.Where(p => p.SageAmount.HasValue && p.SageAmount.Value != p.MovementAmount).ToList();
                }

                model.C02_GeneralLedgerReport_GLAuditViewItems = model.C02_GeneralLedgerReport_GLAuditViewItems.OrderBy(p => p.CompanyName).ThenBy(p => p.Date).ThenBy(p => p.LedgerNo).ToList();
            }
            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView_RequestRerun")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditView_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_AccountingChecklist_Request f_SystemGeneratedReports_AccountingChecklist_Request = new F_SystemGeneratedReports_AccountingChecklist_Request()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(2018, 01, 01),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month))
                };

                db.Add(f_SystemGeneratedReports_AccountingChecklist_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView");
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView_JournalPost/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditView_JournalPost(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            var accountingChecklist = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();
            if (accountingChecklist != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == accountingChecklist.CompanyID).SingleOrDefault();
                string accountSearchTerm = $"{company.Name.Substring(0, 7)}.{accountingChecklist.LedgerNo}.{accountingChecklist.LedgerName}";
                var sageAccounting_Account = db.SageAccounting_Accounts.Where(p => p.Name == accountSearchTerm && p.CompanyId == company.SageAccountingLegalEntityCompanyID).FirstOrDefault();

                string contraAccountSearchTerm = $"{company.Name.Substring(0, 7)}.5999.System rounding differences";
                var sageAccounting_ContraAccount = db.SageAccounting_Accounts.Where(p => p.Name == contraAccountSearchTerm && p.CompanyId == company.SageAccountingLegalEntityCompanyID).FirstOrDefault();
                if (sageAccounting_Account != null && sageAccounting_ContraAccount != null)
                {
                    var sageAccounting_AccountTaxType = db.SageAccounting_AccountTaxTypes.Where(p => p.SageID.HasValue && p.SageID == sageAccounting_Account.DefaultTaxTypeId && p.CompanyId == sageAccounting_Account.CompanyId).SingleOrDefault();
                    MyVoltage.Api.SageAccounting.SageAccountingAPI sageAccountingAPI = new MyVoltage.Api.SageAccounting.SageAccountingAPI(_cache, _options, _APIoptions);

                    string description = $"{accountingChecklist.Date.ToMonth()} Electra nett movement";

                    var saveJournalEntryResponse = sageAccountingAPI.SaveJournalEntry(sageAccounting_Account.CompanyId.Value, sageAccounting_Account.SageID.Value, sageAccounting_Account.DefaultTaxTypeId.Value, sageAccounting_AccountTaxType != null && sageAccounting_AccountTaxType.Percentage.HasValue ? sageAccounting_AccountTaxType.Percentage.Value : 0, sageAccounting_ContraAccount.SageID.Value, accountingChecklist.MovementAmount, accountingChecklist.Date, description, description, ID);

                    if (saveJournalEntryResponse != null)
                    {
                        accountingChecklist.SageJournalID = saveJournalEntryResponse.ID;
                        accountingChecklist.SageCompanyID = sageAccounting_Account.CompanyId;
                        db.Update(accountingChecklist);
                        db.SaveChanges();

                    }
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView");
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView_JournalRePost/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditView_JournalRePost(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            var accountingChecklist = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();
            if (accountingChecklist != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == accountingChecklist.CompanyID).SingleOrDefault();
                string accountSearchTerm = $"{company.Name.Substring(0, 7)}.{accountingChecklist.LedgerNo}.{accountingChecklist.LedgerName}";
                var sageAccounting_Account = db.SageAccounting_Accounts.Where(p => p.Name == accountSearchTerm && p.CompanyId == company.SageAccountingLegalEntityCompanyID).FirstOrDefault();

                string contraAccountSearchTerm = $"{company.Name.Substring(0, 7)}.5999.System rounding differences";
                var sageAccounting_ContraAccount = db.SageAccounting_Accounts.Where(p => p.Name == contraAccountSearchTerm && p.CompanyId == company.SageAccountingLegalEntityCompanyID).FirstOrDefault();
                if (sageAccounting_Account != null && sageAccounting_ContraAccount != null)
                {
                    var sageAccounting_AccountTaxType = db.SageAccounting_AccountTaxTypes.Where(p => p.SageID.HasValue && p.SageID == sageAccounting_Account.DefaultTaxTypeId && p.CompanyId == sageAccounting_Account.CompanyId).SingleOrDefault();
                    MyVoltage.Api.SageAccounting.SageAccountingAPI sageAccountingAPI = new MyVoltage.Api.SageAccounting.SageAccountingAPI(_cache, _options, _APIoptions);

                    if (accountingChecklist.SageJournalID.HasValue)
                    {
                        sageAccountingAPI.DeleteJournal(accountingChecklist.SageJournalID.Value, sageAccounting_Account.CompanyId.Value);
                    }

                    string description = $"{accountingChecklist.Date.ToMonth()} Electra nett movement";

                    var saveJournalEntryResponse = sageAccountingAPI.SaveJournalEntry(sageAccounting_Account.CompanyId.Value, sageAccounting_Account.SageID.Value, sageAccounting_Account.DefaultTaxTypeId.Value, sageAccounting_AccountTaxType != null && sageAccounting_AccountTaxType.Percentage.HasValue ? sageAccounting_AccountTaxType.Percentage.Value : 0, sageAccounting_ContraAccount.SageID.Value, accountingChecklist.MovementAmount, accountingChecklist.Date, description, description, ID);

                    if (saveJournalEntryResponse != null)
                    {
                        accountingChecklist.SageJournalID = saveJournalEntryResponse.ID;
                        accountingChecklist.SageCompanyID = sageAccounting_Account.CompanyId;
                        db.Update(accountingChecklist);
                        db.SaveChanges();

                    }
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView");
        }

        [HttpPost]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView_JournalBulkPost")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditView_JournalBulkPost()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var accountingChecklistIDsRaw = Request.Form["accountingChecklistIDs"].ToString().Split(',');
                if (accountingChecklistIDsRaw.Length != 0)
                {
                    F_SystemGeneratedReports_SageAccounting_JournalRequest sageAccounting_JournalRequest = new F_SystemGeneratedReports_SageAccounting_JournalRequest()
                    {
                        CreatedDate = DateTime.Now,
                        CreatedBy = _userManager.GetUserId(User),
                        CompanyID = _operationalProvider.CompanyID,
                        FromDate = DateTime.Now,
                        ToDate = DateTime.Now,
                    };
                    db.Add(sageAccounting_JournalRequest);
                    db.SaveChanges();

                    foreach (var accountingChecklistID in accountingChecklistIDsRaw)
                    {
                        SageAccounting_JournalRequests_AccountingChecklistID sageAccounting_JournalRequests_AccountingChecklistID = new SageAccounting_JournalRequests_AccountingChecklistID()
                        {
                            AccountChecklistID = Convert.ToInt32(accountingChecklistID),
                            DateCreated = DateTime.Now,
                            SageAccounting_JournalRequestsID = sageAccounting_JournalRequest.ID,
                        };
                        db.Add(sageAccounting_JournalRequests_AccountingChecklistID);
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

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
            {
                string ret = "/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView";
                _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
                if (string.IsNullOrEmpty(ret))
                    ret = "/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView";
                return Redirect(HttpUtility.UrlDecode(ret));
            }

            var opProfs = db.OperationalProfiles.ToList();

            C02_GeneralLedgerReport_GLAuditUpdateModel model = new C02_GeneralLedgerReport_GLAuditUpdateModel()
            {
                C02_GeneralLedgerReport_GLAuditUpdateItem = new C02_GeneralLedgerReport_GLAuditUpdateModel.C02_GeneralLedgerReport_GLAuditUpdate()
                {
                    ID = item.ID,
                    ApprovedBy = item.ApprovedBy,
                    ApprovedByUsername = "",
                    ApprovedDate = item.ApprovedDate,
                    BalanceAmount = item.BalanceAmount,
                    BalanceAmountConfirmed = item.BalanceAmountConfirmed,
                    BalanceSyncDate = item.BalanceSyncDate,
                    Comments = item.Comments,
                    CompanyID = item.CompanyID,
                    CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault().Name,
                    Date = item.Date,
                    FlagID = item.FlagID,
                    LedgerName = item.LedgerName,
                    LedgerNo = item.LedgerNo,
                    MovementAmount = item.MovementAmount,
                    MovementAmountConfirmed = item.MovementAmountConfirmed,
                    MovementSyncDate = item.MovementSyncDate,
                    PendingIssue = item.PendingIssue,
                    ReviewedBy = item.ReviewedBy,
                    ReviewedByUsername = "",
                    ReviewedDate = item.ReviewedDate,
                    TaskID = item.TaskID,
                    ReviewedByUsernameBalance = "",
                    ApprovedByUsernameBalance = "",
                    ApprovedByBalance = item.ApprovedByBalance,
                    ApprovedDateBalance = item.ApprovedDateBalance,
                    ReviewedByBalance = item.ReviewedByBalance,
                    ReviewedDateBalance = item.ReviewedDateBalance,
                    AttachmentFileName = item.AttachmentFileName,
                    LastCheckedDate = item.LastCheckedDate,
                    AuditBy = item.AuditBy,
                    AuditByBalance = item.AuditByBalance,
                    AuditComments = item.AuditComments,
                    AuditDate = item.AuditDate,
                    AuditDateBalance = item.AuditDateBalance,
                    AuditByUsername = "",
                    AuditByUsernameBalance = "",
                },
                Comments = item.Comments,
                TaskID = item.TaskID,
                ConfirmedBalanceAmount = item.BalanceAmountConfirmed.HasValue ? item.BalanceAmountConfirmed.Value : item.BalanceAmount,
                ConfirmedMovementAmount = item.MovementAmountConfirmed.HasValue ? item.MovementAmountConfirmed.Value : item.MovementAmount,
                FlagID = item.FlagID,
                PendingIssue = item.PendingIssue,
                AuditComments = item.AuditComments,
                JobTitle = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault().JobTitle,
            };

            if (!string.IsNullOrEmpty(item.ApprovedBy))
            {
                var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedBy.Trim()).SingleOrDefault();
                if (opApprovedBy != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ApprovedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ReviewedBy))
            {
                var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedBy.Trim()).SingleOrDefault();
                if (opReviewedBy != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ReviewedByUsername = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ApprovedByBalance))
            {
                var opApprovedByBalance = opProfs.Where(p => p.UserID == item.ApprovedByBalance.Trim()).SingleOrDefault();
                if (opApprovedByBalance != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ApprovedByUsernameBalance = $"{opApprovedByBalance.FirstName} {opApprovedByBalance.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ReviewedByBalance))
            {
                var opReviewedByBalance = opProfs.Where(p => p.UserID == item.ReviewedByBalance.Trim()).SingleOrDefault();
                if (opReviewedByBalance != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ReviewedByUsernameBalance = $"{opReviewedByBalance.FirstName} {opReviewedByBalance.LastName}";
            }

            if (!string.IsNullOrEmpty(item.AuditBy))
            {
                var opAuditBy = opProfs.Where(p => p.UserID == item.AuditBy.Trim()).SingleOrDefault();
                if (opAuditBy != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.AuditByUsername = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
            }

            if (!string.IsNullOrEmpty(item.AuditByBalance))
            {
                var opAuditByBalance = opProfs.Where(p => p.UserID == item.AuditByBalance.Trim()).SingleOrDefault();
                if (opAuditByBalance != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.AuditByUsernameBalance = $"{opAuditByBalance.FirstName} {opAuditByBalance.LastName}";
            }

            return View("~/Views/Operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate(int ID, C02_GeneralLedgerReport_GLAuditUpdateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            string ret = "/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = "/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView";

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
            {
                return Redirect(HttpUtility.UrlDecode(ret));
            }

            model.C02_GeneralLedgerReport_GLAuditUpdateItem = new C02_GeneralLedgerReport_GLAuditUpdateModel.C02_GeneralLedgerReport_GLAuditUpdate()
            {
                ID = item.ID,
                ApprovedBy = item.ApprovedBy,
                ApprovedByUsername = "",
                ApprovedDate = item.ApprovedDate,
                BalanceAmount = item.BalanceAmount,
                BalanceAmountConfirmed = item.BalanceAmountConfirmed,
                BalanceSyncDate = item.BalanceSyncDate,
                Comments = item.Comments,
                CompanyID = item.CompanyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault().Name,
                Date = item.Date,
                FlagID = item.FlagID,
                LedgerName = item.LedgerName,
                LedgerNo = item.LedgerNo,
                MovementAmount = item.MovementAmount,
                MovementAmountConfirmed = item.MovementAmountConfirmed,
                MovementSyncDate = item.MovementSyncDate,
                PendingIssue = item.PendingIssue,
                ReviewedBy = item.ReviewedBy,
                ReviewedByUsername = "",
                ReviewedDate = item.ReviewedDate,
                TaskID = item.TaskID,
                ReviewedByUsernameBalance = "",
                ApprovedByUsernameBalance = "",
                ApprovedByBalance = item.ApprovedByBalance,
                ApprovedDateBalance = item.ApprovedDateBalance,
                ReviewedByBalance = item.ReviewedByBalance,
                ReviewedDateBalance = item.ReviewedDateBalance,
                AttachmentFileName = item.AttachmentFileName,
                LastCheckedDate = item.LastCheckedDate,
                AuditBy = item.AuditBy,
                AuditByBalance = item.AuditByBalance,
                AuditComments = item.AuditComments,
                AuditDate = item.AuditDate,
                AuditDateBalance = item.AuditDateBalance,
                AuditByUsername = "",
                AuditByUsernameBalance = "",
            };

            var opProfs = db.OperationalProfiles.ToList();

            if (!string.IsNullOrEmpty(item.ApprovedBy))
            {
                var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedBy.Trim()).SingleOrDefault();
                if (opApprovedBy != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ApprovedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ReviewedBy))
            {
                var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedBy.Trim()).SingleOrDefault();
                if (opReviewedBy != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ReviewedByUsername = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ApprovedByBalance))
            {
                var opApprovedByBalance = opProfs.Where(p => p.UserID == item.ApprovedByBalance.Trim()).SingleOrDefault();
                if (opApprovedByBalance != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ApprovedByUsernameBalance = $"{opApprovedByBalance.FirstName} {opApprovedByBalance.LastName}";
            }

            if (!string.IsNullOrEmpty(item.ReviewedByBalance))
            {
                var opReviewedByBalance = opProfs.Where(p => p.UserID == item.ReviewedByBalance.Trim()).SingleOrDefault();
                if (opReviewedByBalance != null)
                    model.C02_GeneralLedgerReport_GLAuditUpdateItem.ReviewedByUsernameBalance = $"{opReviewedByBalance.FirstName} {opReviewedByBalance.LastName}";
            }


            if (item.BalanceAmountConfirmed != model.ConfirmedBalanceAmount)
            {
                item.BalanceAmountConfirmed = model.ConfirmedBalanceAmount;
                if (item.BalanceAmountConfirmed.HasValue && item.BalanceAmount == item.BalanceAmountConfirmed.Value)
                {
                    item.ReviewedByBalance = _userManager.GetUserId(User);
                    item.ReviewedDateBalance = DateTime.Now;
                }
                else
                {
                    item.ReviewedByBalance = "";
                    item.ReviewedDateBalance = null;
                }
                item.ApprovedByBalance = "";
                item.ApprovedDateBalance = null;
            }

            if (item.MovementAmountConfirmed != model.ConfirmedMovementAmount)
            {
                item.MovementAmountConfirmed = model.ConfirmedMovementAmount;
                if (item.MovementAmountConfirmed.HasValue && item.MovementAmount == item.MovementAmountConfirmed.Value)
                {
                    item.ReviewedBy = _userManager.GetUserId(User);
                    item.ReviewedDate = DateTime.Now;
                }
                else
                {
                    item.ReviewedBy = "";
                    item.ReviewedDate = null;
                }

                item.ApprovedBy = "";
                item.ApprovedDate = null;
            }

            if (model.Attachment != null)
            {
                // Name of the share, directory, and file we'll create
                string shareName = "accountingchecklists";
                string dirName = $"{item.ID}";
                string fileName = System.IO.Path.GetFileName(model.Attachment.FileName);

                // Get a reference to a share and then create it
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                share.CreateIfNotExists();

                // Get a reference to a directory and create it
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                directory.CreateIfNotExists();

                // Get a reference to a file and upload it
                ShareFileClient file = directory.GetFileClient(fileName);

                // Copy the contents of the file to the request stream.
                Stream uploadFile = new MemoryStream();
                model.Attachment.CopyTo(uploadFile);
                //byte[] fileContents = new byte[uploadFile.Length];
                uploadFile.Position = 0;
                //uploadFile.Read(fileContents, 0, fileContents.Length);

                file.Create(uploadFile.Length);
                file.UploadRange(
                    new HttpRange(0, uploadFile.Length),
                    uploadFile);

                item.AttachmentFileName = fileName;
            }


            item.PendingIssue = model.PendingIssue;
            item.Comments = model.Comments;
            item.AuditComments = model.AuditComments;
            item.FlagID = model.FlagID;
            item.TaskID = model.TaskID;

            db.Update(item);
            db.SaveChanges();

            return Redirect(HttpUtility.UrlDecode(ret));
        }


        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAudit_GetAttachment/{ID}")]
        public async Task<IActionResult> A09_Flags_CompanyReview_GetAttachment(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Content("Not Found");


            string shareName = "accountingchecklists";
            string dirName = $"{item.ID}";
            string fileName = item.AttachmentFileName;

            // Get a reference to the file
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
            ShareFileClient file = directory.GetFileClient(fileName);

            // Download the file
            ShareFileDownloadInfo download = file.Download();
            Stream uploadFile = new MemoryStream();
            download.Content.CopyTo(uploadFile);
            uploadFile.Position = 0;
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(item.AttachmentFileName));

            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate_Approve/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate_Approve(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedBy = _userManager.GetUserId(User);
                item.ApprovedDate = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate_ApproveBalance/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate_ApproveBalance(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.Approval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.Approval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.ApprovedByBalance = _userManager.GetUserId(User);
                item.ApprovedDateBalance = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }

            string ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate_Audit/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate_Audit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditBy = _userManager.GetUserId(User);
                item.AuditDate = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            string ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

        [HttpGet]
        [Route("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate_AuditBalance/{ID}")]
        public async Task<IActionResult> C02_GeneralLedgerReport_GLAuditUpdate_AuditBalance(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            var db = new MyVoltageDbContext(_options);

            var item = db.AccountingChecklists.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                item.AuditByBalance = _userManager.GetUserId(User);
                item.AuditDateBalance = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }

            string ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (string.IsNullOrEmpty(ret))
                ret = $"/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditUpdate/{ID}";
            return Redirect(HttpUtility.UrlDecode(ret));
        }

    }
}
