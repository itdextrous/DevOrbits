using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.B03_SupplyCouncilStatementsModels;
using MyVoltage.Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels;
using MyVoltage.Models.OperationalModels.C03_ReportModels;
using MyVoltage.Models.OperationalModels.C09_CashflowForecast;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.C09_CashflowForecast
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C09_CashflowForecastController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C09_CashflowForecastController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_Summary")]
        public async Task<IActionResult> C09_CashflowForecast_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C09_CashflowForecast_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C09_CashflowForecast_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();

            var reportingCategories = db.ManagementAccounts_ReportingCategories.OrderBy(p => p.ReportingCategory).ToList();
            var managementAccounts_ReportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                                            join c in db.ManagementAccounts_ReportingParentDescriptions on p.ParentReportingDescriptionID equals c.ID into sc
                                                            from c in sc.DefaultIfEmpty()
                                                            select new { p, c }).ToList();

            C09_CashflowForecast_SummaryModel model = new C09_CashflowForecast_SummaryModel()
            {
                FromDate = DateTime.Now.AddDays(-14).Date,
                C09_CashflowForecast_SummaryPivotItemsSP = new List<C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem>(),
                C09_CashflowForecast_SummaryPivotItemsBanksBalances = new List<C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem>(),
                ToDate = DateTime.Now.AddDays(14).Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "0", Selected = _operationalProvider.CompanyID == 0 },
                },
                CompanyItems = new List<C09_CashflowForecast_SummaryModel.CompanyItem>(),
                NetcashDate = !string.IsNullOrEmpty(Request.Query["NetcashDate"]) ? Convert.ToDateTime(Request.Query["NetcashDate"]) : DateTime.Now.AddDays(-1),
                ManagementAccounts_ReportingCategories = reportingCategories.Where(p => p.ID == 6 || p.ID == 3).Select(p => new SelectListItem() { Text = p.ReportingCategory, Value = p.ID.ToString() }).ToList(),
                ManagementAccounts_ReportingDescriptions = managementAccounts_ReportingDescriptions.OrderBy(p => $"{p.c.ReportingParentDescription} - {p.p.ReportingDescription}").Select(p => new SelectListItem() { Text = $"{p.c.ReportingParentDescription} - {p.p.ReportingDescription}", Value = p.p.ID.ToString() }).ToList(),
                Companies = db.Companies.OrderBy(p => p.Name).Select(p => new SelectListItem() { Text = p.Name, Value = p.CompanyID.ToString() }).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            ManagementAccounts_AmountTypeEnum managementAccounts_AmountTypeEnum = ManagementAccounts_AmountTypeEnum.Actual;
            if (!string.IsNullOrEmpty(Request.Query["AmountType"]))
                managementAccounts_AmountTypeEnum = (ManagementAccounts_AmountTypeEnum)Convert.ToInt32(Request.Query["AmountType"]);

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            foreach (var c in companies.Where(p => model.Partner.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                C09_CashflowForecast_SummaryModel.CompanyItem companyItem = new C09_CashflowForecast_SummaryModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            var managementAccountsDataDumps_Forecasts1 = (from p in db.ManagementAccountsDataDumps_Forecasts
                                                          where p.Date >= model.FromDate
                                                          && p.Date <= model.ToDate
                                                          select p).ToList();

            var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
            var buildingCouncilDetails_Invoices = db.BuildingCouncilDetails_Invoices.ToList();
            var buildingCouncilDetails_InvoiceItems = db.BuildingCouncilDetails_InvoiceItems.ToList();
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();
            var netcashStatements = (from p in db.NetcashStatements
                                     where p.Date >= model.FromDate
                                     && p.Date <= model.ToDate
                                     && p.TransactionCode == "CBL"
                                     select p).ToList();

            var companies_OperationalBalances = (from p in db.Companies_OperationalBalances
                                                 where p.Date >= model.FromDate
                                                 && p.Date <= model.ToDate
                                                 select p).ToList();


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

            C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem c09_CashflowForecast_SummaryPivotItem_C9_012 = new C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem()
            {
                ReportingDescription = "Operational Payments",
                MonthlyValues = new Dictionary<DateTime, decimal>(),
            };

            C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem c09_CashflowForecast_SummaryPivotItem_B5_022 = new C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem()
            {
                ReportingDescription = "Supply Payments",
                MonthlyValues = new Dictionary<DateTime, decimal>(),
            };

            C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem c09_CashflowForecast_SummaryPivotItem_C3_032 = new C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem()
            {
                ReportingDescription = "Projected Receipts",
                MonthlyValues = new Dictionary<DateTime, decimal>(),
            };

            C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem c09_CashflowForecast_SummaryPivotItem_Netcash = new C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem()
            {
                ReportingDescription = "Netcash",
                MonthlyValues = new Dictionary<DateTime, decimal>(),
            };

            C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem c09_CashflowForecast_SummaryPivotItem_Operational = new C09_CashflowForecast_SummaryModel.C09_CashflowForecast_SummaryPivotItem()
            {
                ReportingDescription = "Operational Bank",
                MonthlyValues = new Dictionary<DateTime, decimal>(),
            };

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

            #endregion

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                if (_operationalProvider.CompanyID != 0 && (co.CompanyID != _operationalProvider.CompanyID))
                    continue;

                #region C9.012 CASHFLOW FORECAST DAILY

                var managementAccountsDataDumps_Forecasts = (from p in managementAccountsDataDumps_Forecasts1
                                                             where p.CompanyID == uC.CompanyID
                                                             select new C09_CashflowForecast_DailyModel.C09_CashflowForecast_DailyPivotItem()
                                                             {
                                                                 AccountNo = p.AccountNo,
                                                                 ActualAmount = p.ActualAmount,
                                                                 Company = co.Name,
                                                                 CompanyID = p.CompanyID,
                                                                 Date = p.Date,
                                                                 ID = p.ID,
                                                                 Reference = p.Reference,
                                                                 ReportingCategoryID = p.ReportingCategoryID,
                                                                 ReportingDescriptionID = p.ReportingDescriptionID,
                                                                 ReportingDescription = managementAccounts_ReportingDescriptions.Where(c => c.p.ID == p.ReportingDescriptionID).Select(c => $"{c.c.ReportingParentDescription} - {c.p.ReportingDescription}").SingleOrDefault(),
                                                             }).ToList();

                foreach (var c09_CashflowForecast_DailyPivotItem in managementAccountsDataDumps_Forecasts)
                {
                    if (!c09_CashflowForecast_SummaryPivotItem_C9_012.MonthlyValues.ContainsKey(c09_CashflowForecast_DailyPivotItem.Date))
                        c09_CashflowForecast_SummaryPivotItem_C9_012.MonthlyValues.Add(c09_CashflowForecast_DailyPivotItem.Date, c09_CashflowForecast_DailyPivotItem.ActualAmount);
                    else
                        c09_CashflowForecast_SummaryPivotItem_C9_012.MonthlyValues[c09_CashflowForecast_DailyPivotItem.Date] = c09_CashflowForecast_SummaryPivotItem_C9_012.MonthlyValues[c09_CashflowForecast_DailyPivotItem.Date] + c09_CashflowForecast_DailyPivotItem.ActualAmount;
                }

                #endregion

                #region B5.022 CASHFLOW FORECAST

                List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem> items = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>();

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    foreach (var bCD in bCDs)
                    {
                        var invoices = (from p in buildingCouncilDetails_Invoices
                                        where p.BuildingCouncilDetailID == bCD.ID
                                        && !p.IsDeleted
                                        orderby p.TAXInvoiceDate
                                        select p).ToList();

                        bool isFirst = true;

                        B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem previousItem = null;

                        foreach (var inv in invoices)
                        {
                            decimal openingBalance = 0;
                            decimal totalAmount = 0;

                            decimal openingBalanceC = 0;
                            decimal totalAmountC = 0;

                            decimal openingBalanceSP = 0;
                            decimal totalAmountSP = 0;

                            var invoiceItems = (from p in buildingCouncilDetails_InvoiceItems
                                                where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                select p).ToList();

                            B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem item = new B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem()
                            {
                                AccountNo = $"{bCD.CouncilElecAccNo}",
                                PropertyLinked = co.Name,
                                ReferencedDocument = "",
                                Resourcees = new Dictionary<string, decimal>(),
                                TAXInvoiceDate = inv.TAXInvoiceDate,
                                TAXInvoiceNo = inv.TAXInvoiceNo,
                                PayableByServiceProvider = 0,
                                InvoiceID = inv.ID,
                                Status = inv.Status,
                                PayableByClient = 0,
                                FinalDateForPayment = inv.FinalDateForPayment,
                                ClosingBalance = 0,
                                ClosingBalanceClient = 0,
                                ClosingBalanceServiceProvider = 0,
                                OpeningBalance = 0,
                                OpeningBalanceClient = 0,
                                OpeningBalanceServiceProvider = 0,
                            };

                            if (isFirst)
                            {
                                isFirst = false;
                                openingBalance = bCD.OpeningBalance + bCD.OpeningBalanceClient;
                                openingBalanceSP = bCD.OpeningBalance;
                                openingBalanceC = bCD.OpeningBalanceClient;
                                previousItem = null;
                            }
                            else if (previousItem != null)
                            {
                                openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                                openingBalanceSP = previousItem.ClosingBalanceServiceProvider;
                                openingBalanceC = previousItem.ClosingBalanceClient;
                            }

                            foreach (var iItem in invoiceItems)
                            {
                                var res = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                                if (item.Resourcees.ContainsKey(res.ResourceTypeName))
                                    item.Resourcees[res.ResourceTypeName] = item.Resourcees[res.ResourceTypeName] + iItem.AmountInclVAT;
                                else
                                    item.Resourcees.Add(res.ResourceTypeName, iItem.AmountInclVAT);

                                if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                                {
                                    item.PaidByClient += iItem.PayableByClient;
                                    item.PaidByServiceProvider += iItem.PayableByServiceProvider;
                                }
                                else
                                {
                                    item.PayableByClient += iItem.PayableByClient;
                                    item.PayableByServiceProvider += iItem.PayableByServiceProvider;
                                }
                            }

                            item.ClosingBalance = openingBalance + item.TotalCharges;
                            item.OpeningBalance = openingBalance;

                            item.ClosingBalanceServiceProvider = openingBalanceSP + item.PayableByServiceProvider + item.PaidByServiceProvider;
                            item.OpeningBalanceServiceProvider = openingBalanceSP;

                            item.ClosingBalanceClient = openingBalanceC + item.PayableByClient + item.PaidByClient;
                            item.OpeningBalanceClient = openingBalanceC;


                            items.Add(item);
                            previousItem = item;
                        }


                    }


                    var accountNos = (from p in items
                                      select p.AccountNo).Distinct().ToList();

                    if (items.Count != 0)
                    {
                        //var netcashStatement = (from p in db.NetcashStatements
                        //                        where p.CompanyID == co.CompanyID
                        //                        && p.Date.Date == model.NetcashDate.Date
                        //                        && p.TransactionCode == "CBL"
                        //                        select p).SingleOrDefault();

                        foreach (var accNo in accountNos)
                        {
                            DateTime current = model.FromDate.Date;
                            DateTime toDate = model.ToDate.Date;

                            var taxInvoiceNos = items.Where(p => p.AccountNo == accNo && p.FinalDateForPayment.Date >= current.Date && p.FinalDateForPayment.Date <= toDate.Date).Select(p => p.TAXInvoiceNo).Distinct().ToList();

                            foreach (var invoiceNo in taxInvoiceNos)
                            {
                                B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem B05_SupplyPayments_CashflowForecastPivotItemSP = new B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem()
                                {
                                    AccountNo = accNo,
                                    ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                                    Company = co.Name,
                                    CompanyID = co.CompanyID.ToString(),
                                    NetcashClosingBalance = 0,
                                    //NetcashClosingBalance = netcashStatement != null ? netcashStatement.RealAmount.Value : 0,
                                    TaxInvoiceNo = invoiceNo,
                                    TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                                    TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                                    FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                                };

                                B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem B05_SupplyPayments_CashflowForecastPivotItemC = new B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem()
                                {
                                    AccountNo = accNo,
                                    ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                                    Company = co.Name,
                                    CompanyID = co.CompanyID.ToString(),
                                    NetcashClosingBalance = 0,
                                    //NetcashClosingBalance = netcashStatement != null ? netcashStatement.RealAmount.Value : 0,
                                    TaxInvoiceNo = invoiceNo,
                                    TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                                    TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                                    FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                                };

                                while (current <= toDate)
                                {
                                    var B05_SupplyPayments_CashflowForecastItems = (from p in items
                                                                                    where p.AccountNo == accNo
                                                                                    && p.FinalDateForPayment.Date == current.Date
                                                                                    && p.TAXInvoiceNo == invoiceNo
                                                                                    select new
                                                                                    {
                                                                                        ClosingBalanceClient = p.ClosingBalanceClient,
                                                                                        ClosingBalanceServiceProvider = p.ClosingBalanceServiceProvider,
                                                                                    }).ToList();

                                    if (B05_SupplyPayments_CashflowForecastItems.Count != 0)
                                    {
                                        if (B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum() != 0)
                                        {
                                            if (!c09_CashflowForecast_SummaryPivotItem_B5_022.MonthlyValues.ContainsKey(current))
                                                c09_CashflowForecast_SummaryPivotItem_B5_022.MonthlyValues.Add(current, B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum() * -1.0m);
                                            else
                                                c09_CashflowForecast_SummaryPivotItem_B5_022.MonthlyValues[current] = c09_CashflowForecast_SummaryPivotItem_B5_022.MonthlyValues[current] + (B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum() * -1.0m);
                                        }
                                    }

                                    current = current.AddDays(1);
                                }
                            }
                        }
                    }
                }

                #endregion

                #region C3.032 RECEIPT PER PROPERTY DETAILS

                #region Totals

                DateTime currentC3 = model.FromDate;
                while (currentC3 <= model.ToDate)
                {
                    #region Item

                    decimal amount = 0;

                    var unipin = (from p in tuplesUniPins
                                  where p.Item2.Date == currentC3.Date
                                  && p.Item1.HasValue
                                  && p.Item1.Value == co.CompanyID
                                  select p).FirstOrDefault();
                    if (unipin != null)
                        amount += unipin.Item3;

                    var Payment = (from p in tuplesPayments
                                   where p.Item2.Date == currentC3.Date
                                   && p.Item1.HasValue
                                   && p.Item1.Value == co.CompanyID
                                   select p).FirstOrDefault();
                    if (Payment != null)
                        amount += Payment.Item3;

                    var NetcashManualPayment = (from p in tuplesNetcashManualPayments
                                                where p.Item2.Date == currentC3.Date
                                                && p.Item1.HasValue
                                                && p.Item1.Value == co.CompanyID
                                                select p).FirstOrDefault();
                    if (NetcashManualPayment != null)
                        amount += NetcashManualPayment.Item3;

                    if (!c09_CashflowForecast_SummaryPivotItem_C3_032.MonthlyValues.ContainsKey(currentC3))
                        c09_CashflowForecast_SummaryPivotItem_C3_032.MonthlyValues.Add(currentC3, amount);
                    else
                        c09_CashflowForecast_SummaryPivotItem_C3_032.MonthlyValues[currentC3] = c09_CashflowForecast_SummaryPivotItem_C3_032.MonthlyValues[currentC3] + amount;

                    #endregion
                    currentC3 = currentC3.AddDays(1);
                }

                #endregion


                #endregion

                #region Bank Balances

                #region Netcash

                DateTime currentNetcash = model.FromDate;
                while (currentNetcash <= model.ToDate)
                {
                    #region Item

                    var netcashStatement = (from p in netcashStatements
                                            where p.CompanyID == co.CompanyID
                                            && p.Date.Date <= currentNetcash
                                            && p.TransactionCode == "CBL"
                                            orderby p.Date descending
                                            select p).FirstOrDefault();

                    decimal amount = 0;

                    if (netcashStatement != null && netcashStatement.RealAmount.HasValue)
                        amount = netcashStatement.RealAmount.Value;


                    if (!c09_CashflowForecast_SummaryPivotItem_Netcash.MonthlyValues.ContainsKey(currentNetcash))
                        c09_CashflowForecast_SummaryPivotItem_Netcash.MonthlyValues.Add(currentNetcash, amount);
                    else
                        c09_CashflowForecast_SummaryPivotItem_Netcash.MonthlyValues[currentNetcash] = c09_CashflowForecast_SummaryPivotItem_Netcash.MonthlyValues[currentNetcash] + amount;

                    #endregion
                    currentNetcash = currentNetcash.AddDays(1);
                }


                #endregion

                #region Operational

                DateTime currentOperational = model.FromDate;
                while (currentOperational <= model.ToDate)
                {
                    #region Item

                    var companies_OperationalBalance = (from p in companies_OperationalBalances
                                                        where p.CompanyID == co.CompanyID
                                                        && p.Date.Date <= currentOperational
                                                        orderby p.Date descending
                                                        select p).FirstOrDefault();

                    decimal amount = 0;

                    if (companies_OperationalBalance != null)
                        amount = companies_OperationalBalance.Amount;


                    if (!c09_CashflowForecast_SummaryPivotItem_Operational.MonthlyValues.ContainsKey(currentOperational))
                        c09_CashflowForecast_SummaryPivotItem_Operational.MonthlyValues.Add(currentOperational, amount);
                    else
                        c09_CashflowForecast_SummaryPivotItem_Operational.MonthlyValues[currentOperational] = c09_CashflowForecast_SummaryPivotItem_Operational.MonthlyValues[currentOperational] + amount;

                    #endregion
                    currentOperational = currentOperational.AddDays(1);
                }


                #endregion

                #endregion

            }

            model.C09_CashflowForecast_SummaryPivotItemsSP.Add(c09_CashflowForecast_SummaryPivotItem_C9_012);
            model.C09_CashflowForecast_SummaryPivotItemsSP.Add(c09_CashflowForecast_SummaryPivotItem_B5_022);
            model.C09_CashflowForecast_SummaryPivotItemsSP.Add(c09_CashflowForecast_SummaryPivotItem_C3_032);

            model.C09_CashflowForecast_SummaryPivotItemsBanksBalances.Add(c09_CashflowForecast_SummaryPivotItem_Netcash);
            model.C09_CashflowForecast_SummaryPivotItemsBanksBalances.Add(c09_CashflowForecast_SummaryPivotItem_Operational);



            return View("~/Views/Operational/C09_CashflowForecast/C09_CashflowForecast_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_UpdateBankBalances")]
        public async Task<IActionResult> C09_CashflowForecast_UpdateBankBalances()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C09_CashflowForecast_UpdateBankBalances, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C09_CashflowForecast_UpdateBankBalances}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            C09_CashflowForecast_UpdateBankBalancesModel model = new C09_CashflowForecast_UpdateBankBalancesModel()
            {
                C09_CashflowForecast_CostSettings_Templates = new List<C09_CashflowForecast_UpdateBankBalancesModel.C09_CashflowForecast_UpdateBankBalances>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                Companies = (from p in db.Companies
                             orderby p.Name
                             select new SelectListItem()
                             {
                                 Value = p.CompanyID.ToString(),
                                 Text = p.Name,
                                 Selected = _operationalProvider.CompanyID == p.CompanyID,
                             }).ToList(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var companies_OperationalBalances = (from p in db.Companies_OperationalBalances
                                                 join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                 from c in sc.DefaultIfEmpty()
                                                 where p.Date >= model.FromDate.Date
                                                 && p.Date <= model.ToDate.Date
                                                 select new
                                                 {
                                                     c,
                                                     p,
                                                 }).ToList();

            if (_operationalProvider.CompanyID > 0)
            {
                companies_OperationalBalances = (from p in db.Companies_OperationalBalances
                                                 join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                 from c in sc.DefaultIfEmpty()
                                                 where p.Date >= model.FromDate.Date
                                                 && p.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     c,
                                                     p,
                                                 }).ToList();

                model.Companies = (from p in db.Companies
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   orderby p.Name
                                   select new SelectListItem()
                                   {
                                       Value = p.CompanyID.ToString(),
                                       Text = p.Name,
                                       Selected = _operationalProvider.CompanyID == p.CompanyID,
                                   }).ToList();
            }
            var opProfs = db.OperationalProfiles.ToList();

            companies_OperationalBalances = companies_OperationalBalances.OrderBy(p => p.c.Name).ThenBy(p => p.p.Date).ToList();

            foreach (var ac in companies_OperationalBalances)
            {
                C09_CashflowForecast_UpdateBankBalancesModel.C09_CashflowForecast_UpdateBankBalances item = new C09_CashflowForecast_UpdateBankBalancesModel.C09_CashflowForecast_UpdateBankBalances()
                {
                    Date = ac.p.Date,
                    ID = ac.p.ID,
                    CompanyName = ac.c.Name,
                    Amount = ac.p.Amount,
                    CompanyID = ac.p.CompanyID,
                };

                model.C09_CashflowForecast_CostSettings_Templates.Add(item);
            }

            model.C09_CashflowForecast_CostSettings_Templates = model.C09_CashflowForecast_CostSettings_Templates.OrderBy(p => p.CompanyName).ThenByDescending(p => p.Date).ToList();




            return View("~/Views/Operational/C09_CashflowForecast/C09_CashflowForecast_UpdateBankBalances.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_UpdateBankBalances_ItemAdd")]
        public async Task<IActionResult> C09_CashflowForecast_UpdateBankBalances_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["reportingDescriptionID"])
                && !string.IsNullOrEmpty(Request.Form["date"])
                && !string.IsNullOrEmpty(Request.Form["amount"])
                )
            {
                try
                {
                    int reportingDescriptionID = Convert.ToInt32(Request.Form["reportingDescriptionID"]);
                    DateTime date = Convert.ToDateTime(Request.Form["date"]);
                    decimal amount = Convert.ToDecimal(Request.Form["amount"]);

                    var managementAccountsDataDumps_Forecast = (from p in db.Companies_OperationalBalances
                                                                where p.CompanyID == reportingDescriptionID
                                                                && p.Date == date
                                                                select p).FirstOrDefault();

                    if (managementAccountsDataDumps_Forecast == null)
                    {
                        managementAccountsDataDumps_Forecast = new Companies_OperationalBalance()
                        {
                            CompanyID = reportingDescriptionID,
                            Date = date,
                            Amount = amount,
                        };

                        db.Add(managementAccountsDataDumps_Forecast);
                    }

                    db.SaveChanges();

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
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_UpdateBankBalances_ItemUpdate")]
        public async Task<IActionResult> C09_CashflowForecast_UpdateBankBalances_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["reportingDescriptionID"])
                && !string.IsNullOrEmpty(Request.Form["date"])
                && !string.IsNullOrEmpty(Request.Form["amount"])
                && !string.IsNullOrEmpty(Request.Form["itemid"])
                )
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.Companies_OperationalBalances.Where(p => p.ID == itemid).SingleOrDefault();

                    int reportingDescriptionID = Convert.ToInt32(Request.Form["reportingDescriptionID"]);
                    DateTime date = Convert.ToDateTime(Request.Form["date"]);
                    decimal amount = Convert.ToDecimal(Request.Form["amount"]);

                    item.CompanyID = reportingDescriptionID;
                    item.Date = date;
                    item.Amount = amount;

                    db.Update(item);
                    db.SaveChanges();

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
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_UpdateBankBalances_ItemDelete")]
        public async Task<IActionResult> C09_CashflowForecast_UpdateBankBalances_ItemDelete()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemid"]))
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.Companies_OperationalBalances.Where(p => p.ID == itemid).SingleOrDefault();

                    db.Remove(item);
                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_Daily")]
        public async Task<IActionResult> C09_CashflowForecast_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C09_CashflowForecast_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C09_CashflowForecast_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();

            var reportingCategories = db.ManagementAccounts_ReportingCategories.OrderBy(p => p.ReportingCategory).ToList();
            var managementAccounts_ReportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                                            join c in db.ManagementAccounts_ReportingParentDescriptions on p.ParentReportingDescriptionID equals c.ID into sc
                                                            from c in sc.DefaultIfEmpty()
                                                            select new { p, c }).ToList();

            C09_CashflowForecast_DailyModel model = new C09_CashflowForecast_DailyModel()
            {
                FromDate = DateTime.Now.AddDays(-14).Date,
                C09_CashflowForecast_DailyPivotItemsSP = new List<C09_CashflowForecast_DailyModel.C09_CashflowForecast_DailyPivotItem>(),
                ToDate = DateTime.Now.AddDays(14).Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Companies]", Value = "0", Selected = _operationalProvider.CompanyID == 0 },
                },
                CompanyItems = new List<C09_CashflowForecast_DailyModel.CompanyItem>(),
                NetcashDate = !string.IsNullOrEmpty(Request.Query["NetcashDate"]) ? Convert.ToDateTime(Request.Query["NetcashDate"]) : DateTime.Now.AddDays(-1),
                ManagementAccounts_ReportingCategories = reportingCategories.Where(p => p.ID == 6 || p.ID == 3).Select(p => new SelectListItem() { Text = p.ReportingCategory, Value = p.ID.ToString() }).ToList(),
                ManagementAccounts_ReportingDescriptions = managementAccounts_ReportingDescriptions.OrderBy(p => $"{p.c.ReportingParentDescription} - {p.p.ReportingDescription}").Select(p => new SelectListItem() { Text = $"{p.c.ReportingParentDescription} - {p.p.ReportingDescription}", Value = p.p.ID.ToString() }).ToList(),
                Companies = db.Companies.OrderBy(p => p.Name).Select(p => new SelectListItem() { Text = p.Name, Value = p.CompanyID.ToString() }).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            foreach (var c in companies.Where(p => model.Partner.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                C09_CashflowForecast_DailyModel.CompanyItem companyItem = new C09_CashflowForecast_DailyModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            var items1 = (from p in db.ManagementAccountsDataDumps_Forecasts
                          where p.Date >= model.FromDate
                          && p.Date <= model.ToDate
                          select p).ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                if (_operationalProvider.CompanyID != 0 && (co.CompanyID != _operationalProvider.CompanyID))
                    continue;

                var items = (from p in items1
                             where p.CompanyID == uC.CompanyID
                             select new C09_CashflowForecast_DailyModel.C09_CashflowForecast_DailyPivotItem()
                             {
                                 AccountNo = p.AccountNo,
                                 ActualAmount = p.ActualAmount,
                                 Company = co.Name,
                                 CompanyID = p.CompanyID,
                                 Date = p.Date,
                                 ID = p.ID,
                                 Reference = p.Reference,
                                 ReportingCategoryID = p.ReportingCategoryID,
                                 ReportingDescriptionID = p.ReportingDescriptionID,
                                 ReportingDescription = managementAccounts_ReportingDescriptions.Where(c => c.p.ID == p.ReportingDescriptionID).Select(c => $"{c.c.ReportingParentDescription} - {c.p.ReportingDescription}").SingleOrDefault(),
                             }).ToList();

                model.C09_CashflowForecast_DailyPivotItemsSP.AddRange(items);

            }




            return View("~/Views/Operational/C09_CashflowForecast/C09_CashflowForecast_Daily.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_Daily_ItemAdd")]
        public async Task<IActionResult> C09_CashflowForecast_Daily_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["reportingDescriptionID"])
                && !string.IsNullOrEmpty(Request.Form["company"])
                && !string.IsNullOrEmpty(Request.Form["accountNo"])
                && !string.IsNullOrEmpty(Request.Form["reference"])
                && !string.IsNullOrEmpty(Request.Form["date"])
                && !string.IsNullOrEmpty(Request.Form["amount"])
                )
            {
                try
                {
                    int reportingDescriptionID = Convert.ToInt32(Request.Form["reportingDescriptionID"]);
                    int company = Convert.ToInt32(Request.Form["company"]);
                    string accountNo = Request.Form["accountNo"];
                    string reference = Request.Form["reference"];
                    DateTime date = Convert.ToDateTime(Request.Form["date"]);
                    decimal amount = Convert.ToDecimal(Request.Form["amount"]);

                    var managementAccountsDataDumps_Forecast = (from p in db.ManagementAccountsDataDumps_Forecasts
                                                                where p.CompanyID == company
                                                                && p.ReportingDescriptionID == reportingDescriptionID
                                                                && p.Date == date
                                                                select p).FirstOrDefault();

                    if (managementAccountsDataDumps_Forecast == null)
                    {
                        managementAccountsDataDumps_Forecast = new ManagementAccountsDataDumps_Forecast()
                        {
                            CompanyID = company,
                            Date = date,
                            ReportingDescriptionID = reportingDescriptionID,
                            AccountNo = accountNo,
                            ActualAmount = amount,
                            Reference = reference,
                            ReportingCategoryID = 6,
                        };

                        db.Add(managementAccountsDataDumps_Forecast);
                    }

                    db.SaveChanges();

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
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_Daily_ItemUpdate")]
        public async Task<IActionResult> C09_CashflowForecast_Daily_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["reportingDescriptionID"])
                && !string.IsNullOrEmpty(Request.Form["company"])
                && !string.IsNullOrEmpty(Request.Form["accountNo"])
                && !string.IsNullOrEmpty(Request.Form["reference"])
                && !string.IsNullOrEmpty(Request.Form["date"])
                && !string.IsNullOrEmpty(Request.Form["amount"])
                && !string.IsNullOrEmpty(Request.Form["itemid"])
                )
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.ManagementAccountsDataDumps_Forecasts.Where(p => p.ID == itemid).SingleOrDefault();

                    int reportingDescriptionID = Convert.ToInt32(Request.Form["reportingDescriptionID"]);
                    int company = Convert.ToInt32(Request.Form["company"]);
                    string accountNo = Request.Form["accountNo"];
                    string reference = Request.Form["reference"];
                    DateTime date = Convert.ToDateTime(Request.Form["date"]);
                    decimal amount = Convert.ToDecimal(Request.Form["amount"]);

                    item.CompanyID = company;
                    item.ReportingDescriptionID = reportingDescriptionID;
                    item.AccountNo = accountNo;
                    item.Reference = reference;
                    item.Date = date;
                    item.ActualAmount = amount;

                    db.Update(item);
                    db.SaveChanges();

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
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_Daily_ItemDelete")]
        public async Task<IActionResult> C09_CashflowForecast_Daily_ItemDelete()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemid"]))
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.ManagementAccountsDataDumps_Forecasts.Where(p => p.ID == itemid).SingleOrDefault();

                    db.Remove(item);
                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_ConsolidatedNetcashBalances")]
        public async Task<IActionResult> C09_CashflowForecast_ConsolidatedNetcashBalances()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C09_CashflowForecast_ConsolidatedNetcashBalances, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C09_CashflowForecast_ConsolidatedNetcashBalances}/{(int)SecureAreaActionEnum.View}");

            #endregion



            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? true : Convert.ToBoolean(Request.Query["MovementReport"]);
            C09_CashflowForecast_ConsolidatedNetcashBalancesModel model = new C09_CashflowForecast_ConsolidatedNetcashBalancesModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemsSP = new List<C09_CashflowForecast_ConsolidatedNetcashBalancesModel.C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItem>(),
                ToDate = DateTime.Now.Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
                IsMovementReport = movementReport,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            List<Data.NetcashStatement> netcashStatements = new List<NetcashStatement>();

            if (movementReport)
            {
                netcashStatements = (from p in db.NetcashStatements
                                     where p.Date >= model.FromDate
                                     && p.Date <= model.ToDate.Value
                                     && p.TransactionCode != "OBL"
                                     && p.TransactionCode != "CBL"
                                     select p).ToList();
            }
            else
            {
                netcashStatements = (from p in db.NetcashStatements
                                     where p.Date >= model.FromDate
                                     && p.Date <= model.ToDate.Value
                                     && p.TransactionCode == "CBL"
                                     select p).ToList();
            }

            foreach (var uC in _operationalProvider.Companies)
            {
                if (uC.CompanyID == 0)
                    continue;
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                C09_CashflowForecast_ConsolidatedNetcashBalancesModel.C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItem C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemC = new C09_CashflowForecast_ConsolidatedNetcashBalancesModel.C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItem()
                {
                    AccountNo = co.NetcashBankAccountNo,
                    ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                    Company = co.Name,
                    CompanyID = co.CompanyID.ToString(),
                };

                DateTime current = model.FromDate.Value.Date;
                DateTime toDate = model.ToDate.HasValue ? model.ToDate.Value.Date : DateTime.Now.Date;

                while (current <= toDate)
                {
                    decimal? amount = null;

                    if (movementReport)
                        amount = (from p in netcashStatements
                                  where p.CompanyID == co.CompanyID
                                  && p.Date.Date == current.Date.Date
                                  && p.RealAmount.HasValue
                                  select p.RealAmount.Value).Sum();
                    else
                    {
                        var cbl = (from p in netcashStatements
                                   where p.CompanyID == co.CompanyID
                                   && p.Date.Date == current.Date.Date
                                   && p.RealAmount.HasValue
                                   select p).FirstOrDefault();

                        if (cbl != null)
                            amount = cbl.RealAmount.Value;
                    }

                    if (amount.HasValue)
                        C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemC.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, amount.Value));

                    current = current.AddDays(1);
                }
                if (C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemC.ClosingBalances.Count > 0)
                    model.C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemsSP.Add(C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemC);
            }




            return View("~/Views/Operational/C09_CashflowForecast/C09_CashflowForecast_ConsolidatedNetcashBalances.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C09_CashflowForecast/C09_CashflowForecast_ConsolidatedBankBalances")]
        public async Task<IActionResult> C09_CashflowForecast_ConsolidatedBankBalances()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C09_CashflowForecast_ConsolidatedBankBalances, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C09_CashflowForecast_ConsolidatedBankBalances}/{(int)SecureAreaActionEnum.View}");

            #endregion



            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? false : Convert.ToBoolean(Request.Query["MovementReport"]);
            C09_CashflowForecast_ConsolidatedBankBalancesModel model = new C09_CashflowForecast_ConsolidatedBankBalancesModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                C09_CashflowForecast_ConsolidatedBankBalancesPivotItemsSP = new List<C09_CashflowForecast_ConsolidatedBankBalancesModel.C09_CashflowForecast_ConsolidatedBankBalancesPivotItem>(),
                ToDate = DateTime.Now.Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    //new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
                IsMovementReport = movementReport,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            List<Companies_OperationalBalance> companies_OperationalBalance = new List<Companies_OperationalBalance>();

            if (movementReport)
            {
                companies_OperationalBalance = (from p in db.Companies_OperationalBalances
                                                where p.Date >= model.FromDate
                                                && p.Date <= model.ToDate.Value
                                                select p).ToList();
            }
            else
            {
                companies_OperationalBalance = (from p in db.Companies_OperationalBalances
                                                where p.Date >= model.FromDate
                                                && p.Date <= model.ToDate.Value
                                                select p).ToList();
            }

            foreach (var uC in _operationalProvider.Companies)
            {
                if (uC.CompanyID == 0)
                    continue;
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                C09_CashflowForecast_ConsolidatedBankBalancesModel.C09_CashflowForecast_ConsolidatedBankBalancesPivotItem C09_CashflowForecast_ConsolidatedBankBalancesPivotItemC = new C09_CashflowForecast_ConsolidatedBankBalancesModel.C09_CashflowForecast_ConsolidatedBankBalancesPivotItem()
                {
                    ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                    Company = co.Name,
                    CompanyID = co.CompanyID.ToString(),
                    OperationalBankAccountNo = co.OperationalBankAccountNo,
                    OperationalBankAccountType = co.OperationalBankAccountType,
                    OperationalBankBranchCode = co.OperationalBankBranchCode,
                    OperationalBankName = co.OperationalBankName,
                };

                DateTime current = model.FromDate.Value.Date;
                DateTime toDate = model.ToDate.HasValue ? model.ToDate.Value.Date : DateTime.Now.Date;

                while (current <= toDate)
                {
                    decimal? amount = null;

                    if (movementReport)
                        amount = 0;
                    else
                    {
                        var cbl = (from p in companies_OperationalBalance
                                   where p.CompanyID == co.CompanyID
                                   && p.Date.Date <= current.Date.Date
                                   orderby p.Date descending
                                   select p).FirstOrDefault();

                        if (cbl != null)
                            amount = cbl.Amount;
                    }

                    if (amount.HasValue)
                        C09_CashflowForecast_ConsolidatedBankBalancesPivotItemC.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, amount.Value));

                    current = current.AddDays(1);
                }
                if (C09_CashflowForecast_ConsolidatedBankBalancesPivotItemC.ClosingBalances.Count > 0)
                    model.C09_CashflowForecast_ConsolidatedBankBalancesPivotItemsSP.Add(C09_CashflowForecast_ConsolidatedBankBalancesPivotItemC);
            }




            return View("~/Views/Operational/C09_CashflowForecast/C09_CashflowForecast_ConsolidatedBankBalances.cshtml", model);
        }
    }
}
