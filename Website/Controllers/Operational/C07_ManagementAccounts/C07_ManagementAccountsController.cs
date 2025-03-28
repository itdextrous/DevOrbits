using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C07_ManagementAccounts;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.C07_ManagementAccounts
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C07_ManagementAccountsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C07_ManagementAccountsController(
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
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_Summary")]
        public async Task<IActionResult> C07_ManagementAccounts_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C07_ManagementAccounts_SummaryModel model = new C07_ManagementAccounts_SummaryModel()
            {
                C07_ManagementAccounts_SummaryItems = new List<C07_ManagementAccounts_SummaryModel.C07_ManagementAccounts_SummaryItem>(),
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

                C07_ManagementAccounts_SummaryModel.C07_ManagementAccounts_SummaryItem item = new C07_ManagementAccounts_SummaryModel.C07_ManagementAccounts_SummaryItem()
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

                model.C07_ManagementAccounts_SummaryItems.Add(item);
            }

            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_RequestRerun/{companyID}")]
        public async Task<IActionResult> C07_ManagementAccounts_RequestRerun(int companyID)
        {
            var db = new MyVoltageDbContext(_options);
            var latestDateItem = (from p in db.ManagementAccountsDataDumps
                                  where p.CompanyID == companyID
                                  orderby p.Date descending
                                  select p).FirstOrDefault();

            var toDate = latestDateItem != null ? latestDateItem.Date : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month));

            Data.F_SystemGeneratedReports_ManagementAccounts_Request f_SystemGeneratedReports_ManagementAccounts_Request = new F_SystemGeneratedReports_ManagementAccounts_Request()
            {
                CompanyID = companyID,
                CreatedBy = _userManager.GetUserId(User),
                CreatedDate = DateTime.Now,
                DateEnded = null,
                DateStarted = null,
                FromDate = new DateTime(2018, 01, 01),
                Progress = null,
                SystemReportID = null,
                ToDate = toDate,
            };

            db.Add(f_SystemGeneratedReports_ManagementAccounts_Request);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView");
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_RequestRerunAll")]
        public async Task<IActionResult> C07_ManagementAccounts_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);

            foreach (var c in db.Companies.OrderBy(p => p.Name).ToList())
            {
                if (c.SyncManagementAccounts.HasValue && !c.SyncManagementAccounts.Value)
                    continue;

                var latestDateItem = (from p in db.ManagementAccountsDataDumps
                                      where p.CompanyID == c.CompanyID
                                      orderby p.Date descending
                                      select p).FirstOrDefault();

                var toDate = latestDateItem != null ? latestDateItem.Date : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month));

                Data.F_SystemGeneratedReports_ManagementAccounts_Request f_SystemGeneratedReports_ManagementAccounts_Request = new F_SystemGeneratedReports_ManagementAccounts_Request()
                {
                    CompanyID = c.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(2018, 01, 01),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = toDate
                };

                db.Add(f_SystemGeneratedReports_ManagementAccounts_Request);
            }
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/C02_GeneralLedgerReport/C02_GeneralLedgerReport_GLAuditView");
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_GrandFinale_Monthly")]
        public async Task<IActionResult> C07_ManagementAccounts_GrandFinale_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Monthly.GetDescription();

            C07_ManagementAccounts_GrandFinale_MonthlyModel model = new C07_ManagementAccounts_GrandFinale_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems_Sales = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem>(),
                Total_Sales = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Sales",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 1",
                    ChartColor = "#A6CE39",
                },
                ReportingParentDescriptionItems_CostOfSales = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem>(),
                Total_CostOfSales = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Cost Of Sales",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 2",
                    ChartColor = "#35BEAD",
                },
                ReportingParentDescriptionItems_GrossProfit = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem>(),
                Total_GrossProfit = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Gross Profit",
                    ReportingDescriptionID = 0,
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 2",
                    ChartColor = "#F7931D",
                },
                ReportingParentDescriptionItems_OperatingExpenses = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem>(),
                Total_OperatingExpenses = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Operating Expenses",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartColor = "#ED1A3B",
                },
                ReportingParentDescriptionItems_OtherExpenses = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem>(),
                Total_OtherExpenses = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Other Expenses",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartColor = "#FECA0A",
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
                ReportingParentDescriptionItems_GrossProfit_Companies = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                Total_GrossProfit_Companies = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "All Companies",
                    ReportingDescriptionID = 0,
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartStack = "Stack 2",
                    ChartColor = "#F7931D",
                },
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

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   join rc in db.ManagementAccounts_ReportingCategories on p.ReportingCategoryID equals rc.ID into src
                                                   from rc in src.DefaultIfEmpty()
                                                   join rd in db.ManagementAccounts_ReportingDescriptions on p.ReportingDescriptionID equals rd.ID into srd
                                                   from rd in srd.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && companyIDs.Contains(p.CompanyID)
                                                   select new
                                                   {
                                                       p.AccountNo,
                                                       p.ActualAmount,
                                                       p.CompanyID,
                                                       p.Date,
                                                       p.Forecast1Amount,
                                                       p.Forecast2Amount,
                                                       p.Forecast3Amount,
                                                       p.Forecast4Amount,
                                                       p.Forecast5Amount,
                                                       p.ID,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();

                var managementAccounts_ReportingParentDescriptions = db.ManagementAccounts_ReportingParentDescriptions.ToList();
                var managementAccounts_ReportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();


                #region _Sales

                List<int> parentReportingDescriptions_Sales = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_Sales = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_Sales.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_Sales = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_Sales.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_Sales = (from p in managementAccountsDataDumps
                                                         where reportingDescriptionsToCheck_Sales.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                         && p.ReportingCategoryID == 6 // Sales
                                                         select p).ToList();

                foreach (var parent in parentReportingDescriptionsToCheck_Sales)
                {
                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_Sales.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_Sales
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() - 1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum());
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_Sales.MonthlyValues.ContainsKey(current))
                                model.Total_Sales.MonthlyValues[current] = model.Total_Sales.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_Sales.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_Sales.Add(parentDescriptionItem);
                }

                #endregion

                #region _CostOfSales

                List<int> parentReportingDescriptions_CostOfSales = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_CostOfSales = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_CostOfSales.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_CostOfSales = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_CostOfSales.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_CostOfSales = (from p in managementAccountsDataDumps
                                                               where reportingDescriptionsToCheck_CostOfSales.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                               && p.ReportingCategoryID == 3 // Cost of Sales
                                                               select p).ToList();

                foreach (var parent in parentReportingDescriptionsToCheck_CostOfSales)
                {
                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_CostOfSales.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_CostOfSales
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_CostOfSales.MonthlyValues.ContainsKey(current))
                                model.Total_CostOfSales.MonthlyValues[current] = model.Total_CostOfSales.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_CostOfSales.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_CostOfSales.Add(parentDescriptionItem);
                }

                #endregion

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
                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
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
                        C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_GrossProfit
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum());
                                        break;
                                }
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

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_GrossProfit.Add(parentDescriptionItem);
                }

                #endregion

                #region _OperatingExpenses

                List<int> parentReportingDescriptions_OperatingExpenses = new List<int>()
                {
                    1,//	Employee costs
                    2,//	IT Costs
                    3,//	Overhead Costs
                };
                var parentReportingDescriptionsToCheck_OperatingExpenses = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_OperatingExpenses.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_OperatingExpenses = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_OperatingExpenses.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_OperatingExpenses = (from p in managementAccountsDataDumps
                                                                     where reportingDescriptionsToCheck_OperatingExpenses.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                                     select p).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck_OperatingExpenses)
                {
                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_OperatingExpenses.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_OperatingExpenses
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_OperatingExpenses.MonthlyValues.ContainsKey(current))
                                model.Total_OperatingExpenses.MonthlyValues[current] = model.Total_OperatingExpenses.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_OperatingExpenses.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_OperatingExpenses.Add(parentDescriptionItem);
                }

                #endregion

                #region _OtherExpenses

                List<int> parentReportingDescriptions_OtherExpenses = new List<int>()
                {
                    4,//	Dividends Paid
                    5,//	Finance Costs
                    6,//	Management Fees
                };
                var parentReportingDescriptionsToCheck_OtherExpenses = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_OtherExpenses.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_OtherExpenses = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_OtherExpenses.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_OtherExpenses = (from p in managementAccountsDataDumps
                                                                 where reportingDescriptionsToCheck_OtherExpenses.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                                 select p).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck_OtherExpenses)
                {
                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_OtherExpenses.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_OtherExpenses
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_OtherExpenses.MonthlyValues.ContainsKey(current))
                                model.Total_OtherExpenses.MonthlyValues[current] = model.Total_OtherExpenses.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_OtherExpenses.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_OtherExpenses.Add(parentDescriptionItem);
                }

                #endregion

                #region _GrossProfit_Companies_Companies

                List<int> parentReportingDescriptions_GrossProfit_Companies = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_GrossProfit_Companies = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_GrossProfit_Companies.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_GrossProfit_Companies = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_GrossProfit_Companies.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_GrossProfit_Companies = (from p in managementAccountsDataDumps
                                                                         where reportingDescriptionsToCheck_GrossProfit_Companies.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                                         && p.ReportingCategoryID == 4 // Gross Profit
                                                                         select p).ToList();

                Random rnd = new Random();
                foreach (var company in companies)
                {
                    if ((from p in managementAccountsDataDumps_GrossProfit_Companies
                         where p.CompanyID == company.CompanyID
                         select p).Count() == 0)
                        continue;


                    C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_MonthlyModel.ReportingCategoryItem()
                    {
                        ChartColor = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                        ChartType = "bar",
                        MonthlyValues = new Dictionary<DateTime, decimal>(),
                        ReportingDescription = company.Name,
                        ReportingDescriptionID = company.CompanyID,
                    };

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        #region Item

                        var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_GrossProfit_Companies
                                                   where p.CompanyID == company.CompanyID
                                                   && p.Date.Date == current.Date
                                                   select p).ToList();

                        if (EmployeeCosts_Admin.Count != 0)
                            switch (managementAccounts_AmountTypeEnum)
                            {
                                case ManagementAccounts_AmountTypeEnum.Actual:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast1:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum());
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast2:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum());
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast3:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum());
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast4:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum());
                                    break;
                                case ManagementAccounts_AmountTypeEnum.Forecast5:
                                    reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum());
                                    break;
                            }
                        else
                            reportingCategoryItem.MonthlyValues.Add(current, 0);


                        if (model.Total_GrossProfit_Companies.MonthlyValues.ContainsKey(current))
                            model.Total_GrossProfit_Companies.MonthlyValues[current] = model.Total_GrossProfit_Companies.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                        else
                            model.Total_GrossProfit_Companies.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                        #endregion

                        current = current.AddMonths(1);
                    }

                    model.ReportingParentDescriptionItems_GrossProfit_Companies.Add(reportingCategoryItem);
                }

                #endregion

            }

            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_GrandFinale_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_GrandFinale_Yearly")]
        public async Task<IActionResult> C07_ManagementAccounts_GrandFinale_Yearly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Yearly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Yearly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Yearly.GetDescription();

            C07_ManagementAccounts_GrandFinale_YearlyModel model = new C07_ManagementAccounts_GrandFinale_YearlyModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-3).Year, 1, 1),
                ToDate = new DateTime(DateTime.Now.AddYears(2).Year, 12, 31),
                ReportingParentDescriptionItems_Sales = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem>(),
                Total_Sales = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Sales",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                },
                ReportingParentDescriptionItems_CostOfSales = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem>(),
                Total_CostOfSales = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Cost Of Sales",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                },
                ReportingParentDescriptionItems_GrossProfit = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem>(),
                Total_GrossProfit = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Gross Profit",
                    ReportingDescriptionID = 0,
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                },
                ReportingParentDescriptionItems_OperatingExpenses = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem>(),
                Total_OperatingExpenses = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Operating Expenses",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
                },
                ReportingParentDescriptionItems_OtherExpenses = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem>(),
                Total_OtherExpenses = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Other Expenses",
                    ReportingDescriptionID = 0,
                    IsTotal = false,
                    IsPercentage = false,
                    ShowOnChart = true,
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

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   join rc in db.ManagementAccounts_ReportingCategories on p.ReportingCategoryID equals rc.ID into src
                                                   from rc in src.DefaultIfEmpty()
                                                   join rd in db.ManagementAccounts_ReportingDescriptions on p.ReportingDescriptionID equals rd.ID into srd
                                                   from rd in srd.DefaultIfEmpty()
                                                   where p.Date.Year >= model.FromDate.Date.Year
                                                   && p.Date.Year <= model.ToDate.Date.Year
                                                   && companyIDs.Contains(p.CompanyID)
                                                   select new
                                                   {
                                                       p.AccountNo,
                                                       p.ActualAmount,
                                                       p.CompanyID,
                                                       p.Date,
                                                       p.Forecast1Amount,
                                                       p.Forecast2Amount,
                                                       p.Forecast3Amount,
                                                       p.Forecast4Amount,
                                                       p.Forecast5Amount,
                                                       p.ID,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();

                var managementAccounts_ReportingParentDescriptions = db.ManagementAccounts_ReportingParentDescriptions.ToList();
                var managementAccounts_ReportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();


                #region _Sales

                List<int> parentReportingDescriptions_Sales = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_Sales = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_Sales.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_Sales = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_Sales.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_Sales = (from p in managementAccountsDataDumps
                                                         where reportingDescriptionsToCheck_Sales.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                         && p.ReportingCategoryID == 6 // Sales
                                                         select p).ToList();

                foreach (var parent in parentReportingDescriptionsToCheck_Sales)
                {
                    C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_Sales.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_Sales
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Year == current.Year
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() - 1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum());
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_Sales.MonthlyValues.ContainsKey(current))
                                model.Total_Sales.MonthlyValues[current] = model.Total_Sales.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_Sales.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddYears(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_Sales.Add(parentDescriptionItem);
                }

                #endregion

                #region _CostOfSales

                List<int> parentReportingDescriptions_CostOfSales = new List<int>()
                {
                    7,//	Income
                };
                var parentReportingDescriptionsToCheck_CostOfSales = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_CostOfSales.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_CostOfSales = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_CostOfSales.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_CostOfSales = (from p in managementAccountsDataDumps
                                                               where reportingDescriptionsToCheck_CostOfSales.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                               && p.ReportingCategoryID == 3 // Cost of Sales
                                                               select p).ToList();

                foreach (var parent in parentReportingDescriptionsToCheck_CostOfSales)
                {
                    C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_CostOfSales.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_CostOfSales
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Year == current.Year
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_CostOfSales.MonthlyValues.ContainsKey(current))
                                model.Total_CostOfSales.MonthlyValues[current] = model.Total_CostOfSales.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_CostOfSales.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddYears(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_CostOfSales.Add(parentDescriptionItem);
                }

                #endregion

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
                    C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
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
                        C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_GrossProfit
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Year == current.Year
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum());
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum());
                                        break;
                                }
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


                            current = current.AddYears(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_GrossProfit.Add(parentDescriptionItem);
                }

                #endregion

                #region _OperatingExpenses

                List<int> parentReportingDescriptions_OperatingExpenses = new List<int>()
                {
                    1,//	Employee costs
                    2,//	IT Costs
                    3,//	Overhead Costs
                };
                var parentReportingDescriptionsToCheck_OperatingExpenses = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_OperatingExpenses.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_OperatingExpenses = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_OperatingExpenses.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_OperatingExpenses = (from p in managementAccountsDataDumps
                                                                     where reportingDescriptionsToCheck_OperatingExpenses.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                                     select p).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck_OperatingExpenses)
                {
                    C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_OperatingExpenses.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_OperatingExpenses
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Year == current.Year
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_OperatingExpenses.MonthlyValues.ContainsKey(current))
                                model.Total_OperatingExpenses.MonthlyValues[current] = model.Total_OperatingExpenses.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_OperatingExpenses.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddYears(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_OperatingExpenses.Add(parentDescriptionItem);
                }

                #endregion

                #region _OtherExpenses

                List<int> parentReportingDescriptions_OtherExpenses = new List<int>()
                {
                    4,//	Dividends Paid
                    5,//	Finance Costs
                    6,//	Management Fees
                };
                var parentReportingDescriptionsToCheck_OtherExpenses = managementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions_OtherExpenses.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck_OtherExpenses = managementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions_OtherExpenses.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps_OtherExpenses = (from p in managementAccountsDataDumps
                                                                 where reportingDescriptionsToCheck_OtherExpenses.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                                 select p).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck_OtherExpenses)
                {
                    C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck_OtherExpenses.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_GrandFinale_YearlyModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps_OtherExpenses
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Year == current.Year
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total_OtherExpenses.MonthlyValues.ContainsKey(current))
                                model.Total_OtherExpenses.MonthlyValues[current] = model.Total_OtherExpenses.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total_OtherExpenses.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddYears(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }

                    model.ReportingParentDescriptionItems_OtherExpenses.Add(parentDescriptionItem);
                }

                #endregion
            }

            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_GrandFinale_Yearly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_Details")]
        public async Task<IActionResult> C07_ManagementAccounts_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.Where(p => p.FinancialCategoryID.HasValue && p.FinancialCategoryID == (int)ManagementAccounts_ReportingCategory_FinancialCategoryEnum.Income).ToList();

            C07_ManagementAccounts_DetailsModel model = new C07_ManagementAccounts_DetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingDescriptions = new List<string>(),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
                AllReportingDescriptions = new List<C07_ManagementAccounts_DetailsModel.ReportingDescriptionitem>(),
                C07_ManagementAccounts_DetailsItems = new List<C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem>(),
                ReportingParentDescriptionItems_GrossProfit = new List<C07_ManagementAccounts_DetailsModel.ReportingParentDescriptionItem>(),
                Total_GrossProfit = new C07_ManagementAccounts_DetailsModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                },
            };
            if (_operationalProvider.CompanyID != 0)
            {
                foreach (var repDesc in reportingDescriptions)
                {
                    model.AllReportingDescriptions.Add(new C07_ManagementAccounts_DetailsModel.ReportingDescriptionitem()
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
                                                       p.AccountNo,
                                                       p.ActualAmount,
                                                       p.ActualAmountPerMeteringPoint,
                                                       p.ActualAmountPerRegisteredUnit,
                                                       p.Basis,
                                                       p.CompanyID,
                                                       p.Date,
                                                       p.Forecast1Amount,
                                                       p.Forecast1AmountPerMeteringPoint,
                                                       p.Forecast1AmountPerRegisteredUnit,
                                                       p.Forecast2Amount,
                                                       p.Forecast2AmountPerMeteringPoint,
                                                       p.Forecast2AmountPerRegisteredUnit,
                                                       p.Forecast3Amount,
                                                       p.Forecast3AmountPerMeteringPoint,
                                                       p.Forecast3AmountPerRegisteredUnit,
                                                       p.ID,
                                                       p.LegalEntity,
                                                       p.ActualMeteringPoints,
                                                       p.Partner,
                                                       p.PropertyType,
                                                       p.Reference,
                                                       p.ActualRegisteredUnits,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       p.SourceName,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();

                var managementAccounts_ReportingParentDescriptions = db.ManagementAccounts_ReportingParentDescriptions.ToList();
                var managementAccounts_ReportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

                C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem c07_ManagementAccounts_DetailsItem_Summary = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem()
                {
                    Sales_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "Sales_Actual",
                    },
                    CostOfSales_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "CostOfSales_Actual",
                    },
                    GrossProfit_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "GrossProfit_Actual",
                    },
                    GrossProfit_Actual_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "GrossProfit_Actual_Perc",
                    },
                    Sales_Forecast1 = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "Sales_Forecast1",
                    },
                    GrossProfit_Forecast1 = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "GrossProfit_Forecast1",
                    },
                    GrossProfit_Forecast1_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "GrossProfit_Forecast1",
                    },
                    GrossProfit_Difference = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ReportingCategory = "GrossProfit_Difference",
                    },
                    GrossProfit_Difference_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
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

                    C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem c07_ManagementAccounts_DetailsItem = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem()
                    {
                        Sales_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "Sales_Actual",
                        },
                        CostOfSales_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "CostOfSales_Actual",
                        },
                        GrossProfit_Actual = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Actual",
                        },
                        GrossProfit_Actual_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Actual_Perc",
                        },
                        Sales_Forecast1 = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "Sales_Forecast1",
                        },
                        GrossProfit_Forecast1 = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Forecast1",
                        },
                        GrossProfit_Forecast1_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Forecast1",
                        },
                        GrossProfit_Difference = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Difference",
                        },
                        GrossProfit_Difference_Perc = new C07_ManagementAccounts_DetailsModel.C07_ManagementAccounts_DetailsItem.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                            ReportingCategory = "GrossProfit_Difference",
                        },
                        ReportingDescription = repDescItem.DisplayName,
                        ReportingDescriptionCodeName = repDesc,
                    };

                    current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        #region Sales_Actual

                        var sales_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                            where p.ReportingCategory == "Sales"
                                            && p.Date.Date == current.Date
                                            select p).ToList();

                        if (sales_Actual.Count != 0)
                            c07_ManagementAccounts_DetailsItem.Sales_Actual.MonthlyValues.Add(current, sales_Actual.Select(p => p.ActualAmount).Sum());
                        else
                            c07_ManagementAccounts_DetailsItem.Sales_Actual.MonthlyValues.Add(current, 0);

                        if (c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.Sales_Actual.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.Sales_Actual.MonthlyValues[current]);

                        #endregion

                        #region CostOfSales_Actual

                        var CostOfSales_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                                  where p.ReportingCategory == "Cost of Sales"
                                                  && p.Date == current
                                                  select p).ToList();

                        if (CostOfSales_Actual.Count != 0)
                            c07_ManagementAccounts_DetailsItem.CostOfSales_Actual.MonthlyValues.Add(current, CostOfSales_Actual.Select(p => p.ActualAmount).Sum());
                        else
                            c07_ManagementAccounts_DetailsItem.CostOfSales_Actual.MonthlyValues.Add(current, 0);

                        if (c07_ManagementAccounts_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.CostOfSales_Actual.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.CostOfSales_Actual.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.CostOfSales_Actual.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Actual

                        var GrossProfit_Actual = (from p in managementAccountsDataDumpsAllForDesc
                                                  where p.ReportingCategory == "Gross Profit"
                                                  && p.Date == current
                                                  select p).ToList();

                        if (GrossProfit_Actual.Count != 0)
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues.Add(current, GrossProfit_Actual.Select(p => p.ActualAmount).Sum());
                        else
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues.Add(current, 0);

                        if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Actual_Perc

                        if (GrossProfit_Actual.Count != 0 && sales_Actual.Select(p => p.ActualAmount).Sum() != 0)
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues.Add(current, (GrossProfit_Actual.Select(p => p.ActualAmount).Sum() / sales_Actual.Select(p => p.ActualAmount).Sum()) * 100.0m);
                        else
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues.Add(current, 0);

                        if (!c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual_Perc.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual_Perc.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Actual_Perc.MonthlyValues[current]);

                        #endregion

                        #region Sales_Forecast1

                        var Sales_Forecast1 = (from p in managementAccountsDataDumpsAllForDesc
                                               where p.ReportingCategory == "Sales"
                                               && p.Date == current
                                               select p).ToList();

                        if (Sales_Forecast1.Count != 0)
                            c07_ManagementAccounts_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, Sales_Forecast1.Select(p => p.Forecast1Amount).Sum());
                        else
                            c07_ManagementAccounts_DetailsItem.Sales_Forecast1.MonthlyValues.Add(current, 0);

                        if (c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.Sales_Forecast1.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.Sales_Forecast1.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Forecast1

                        var GrossProfit_Forecast1 = (from p in managementAccountsDataDumpsAllForDesc
                                                     where p.ReportingCategory == "Gross Profit"
                                                     && p.Date == current
                                                     select p).ToList();

                        if (GrossProfit_Forecast1.Count != 0)
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, GrossProfit_Forecast1.Select(p => p.Forecast1Amount).Sum());
                        else
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Add(current, 0);

                        if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Forecast1_Perc

                        if (Sales_Forecast1.Count != 0 && GrossProfit_Forecast1.Count != 0 && Sales_Forecast1.Select(p => p.Forecast1Amount).Sum() != 0)
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, (GrossProfit_Forecast1.Select(p => p.Forecast1Amount).Sum() / Sales_Forecast1.Select(p => p.Forecast1Amount).Sum()) * 100.0m);
                        else
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, 0);

                        if (!c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1_Perc.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1_Perc.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1_Perc.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Difference

                        var GrossProfit_Difference = c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues[current] - c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current];

                        c07_ManagementAccounts_DetailsItem.GrossProfit_Difference.MonthlyValues.Add(current, GrossProfit_Difference);

                        if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current] = c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current] + c07_ManagementAccounts_DetailsItem.GrossProfit_Difference.MonthlyValues[current];
                        else
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Difference.MonthlyValues[current]);

                        #endregion

                        #region GrossProfit_Difference_Perc

                        if (c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current] != 0)
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Difference_Perc.MonthlyValues.Add(current, (GrossProfit_Difference.Value / c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues[current].Value) * 100.0m);
                        else
                            c07_ManagementAccounts_DetailsItem.GrossProfit_Difference_Perc.MonthlyValues.Add(current, 0);

                        if (!c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference_Perc.MonthlyValues.ContainsKey(current))
                            c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference_Perc.MonthlyValues.Add(current, c07_ManagementAccounts_DetailsItem.GrossProfit_Difference_Perc.MonthlyValues[current]);

                        #endregion

                        current = current.AddMonths(1);
                    }

                    if (
                        model.HideNoData
                        && c07_ManagementAccounts_DetailsItem.Sales_Actual.MonthlyValues.Values.Where(p => p.HasValue && p.Value != 0).Count() == 0
                        && c07_ManagementAccounts_DetailsItem.CostOfSales_Actual.MonthlyValues.Values.Where(p => p.HasValue && p.Value != 0).Count() == 0
                        && c07_ManagementAccounts_DetailsItem.GrossProfit_Actual.MonthlyValues.Values.Where(p => p.HasValue && p.Value != 0).Count() == 0
                        && c07_ManagementAccounts_DetailsItem.Sales_Forecast1.MonthlyValues.Values.Where(p => p.HasValue && p.Value != 0).Count() == 0
                        && c07_ManagementAccounts_DetailsItem.GrossProfit_Forecast1.MonthlyValues.Values.Where(p => p.HasValue && p.Value != 0).Count() == 0
                        )
                        continue;

                    model.C07_ManagementAccounts_DetailsItems.Add(c07_ManagementAccounts_DetailsItem);
                }

                current = model.FromDate;

                while (current <= model.ToDate)
                {
                    if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current].HasValue && c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].HasValue && c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Value != 0)
                        c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual_Perc.MonthlyValues[current] = (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Actual.MonthlyValues[current].Value / c07_ManagementAccounts_DetailsItem_Summary.Sales_Actual.MonthlyValues[current].Value) * 100.0m;

                    if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].HasValue && c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].HasValue && c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Value != 0)
                        c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1_Perc.MonthlyValues[current] = (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Value / c07_ManagementAccounts_DetailsItem_Summary.Sales_Forecast1.MonthlyValues[current].Value) * 100.0m;

                    if (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].HasValue && c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Value != 0)
                        c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference_Perc.MonthlyValues[current] = (c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Difference.MonthlyValues[current].Value / c07_ManagementAccounts_DetailsItem_Summary.GrossProfit_Forecast1.MonthlyValues[current].Value) * 100.0m;
                    current = current.AddMonths(1);
                }

                model.C07_ManagementAccounts_DetailsItems.Insert(0, c07_ManagementAccounts_DetailsItem_Summary);

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
                    C07_ManagementAccounts_DetailsModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_DetailsModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_DetailsModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_DetailsModel.ReportingCategoryItem()
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
                        C07_ManagementAccounts_DetailsModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_DetailsModel.ReportingCategoryItem()
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
            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_OperatingExpenses")]
        public async Task<IActionResult> C07_ManagementAccounts_OperatingExpenses()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_OperatingExpenses, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_OperatingExpenses}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C07_ManagementAccounts_OperatingExpenses.GetDescription();

            C07_ManagementAccounts_OperatingExpensesModel model = new C07_ManagementAccounts_OperatingExpensesModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems = new List<C07_ManagementAccounts_OperatingExpensesModel.ReportingParentDescriptionItem>(),
                Total = new C07_ManagementAccounts_OperatingExpensesModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Total",
                    ReportingDescriptionID = 0,
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


                List<int> parentReportingDescriptions = new List<int>()
                {
                    1,//	Employee costs
                    2,//	IT Costs
                    3,//	Overhead Costs
                };
                var parentReportingDescriptionsToCheck = db.ManagementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck = db.ManagementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   join rc in db.ManagementAccounts_ReportingCategories on p.ReportingCategoryID equals rc.ID into src
                                                   from rc in src.DefaultIfEmpty()
                                                   join rd in db.ManagementAccounts_ReportingDescriptions on p.ReportingDescriptionID equals rd.ID into srd
                                                   from rd in srd.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && companyIDs.Contains(p.CompanyID)
                                                   && reportingDescriptionsToCheck.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                   select new
                                                   {
                                                       p.AccountNo,
                                                       p.ActualAmount,
                                                       p.CompanyID,
                                                       p.Date,
                                                       p.Forecast1Amount,
                                                       p.Forecast2Amount,
                                                       p.Forecast3Amount,
                                                       p.Forecast4Amount,
                                                       p.Forecast5Amount,
                                                       p.ID,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();

                foreach (var parent in parentReportingDescriptionsToCheck)
                {
                    C07_ManagementAccounts_OperatingExpensesModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_OperatingExpensesModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_OperatingExpensesModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_OperatingExpensesModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_OperatingExpensesModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_OperatingExpensesModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total.MonthlyValues.ContainsKey(current))
                                model.Total.MonthlyValues[current] = model.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }
                    parentDescriptionItem.ReportingCategoryItems = parentDescriptionItem.ReportingCategoryItems.OrderBy(p => p.ReportingDescription).ToList();
                    model.ReportingParentDescriptionItems.Add(parentDescriptionItem);
                }
                model.ReportingParentDescriptionItems = model.ReportingParentDescriptionItems.OrderBy(p => p.ReportingParentDescription).ToList();
            }

            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_OperatingExpenses.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C07_ManagementAccounts/C07_ManagementAccounts_OtherExpenses")]
        public async Task<IActionResult> C07_ManagementAccounts_OtherExpenses()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C07_ManagementAccounts_OtherExpenses, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C07_ManagementAccounts_OtherExpenses}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            ViewData["Title"] = SecureAreaEnum.C07_ManagementAccounts_OtherExpenses.GetDescription();

            C07_ManagementAccounts_OtherExpensesModel model = new C07_ManagementAccounts_OtherExpensesModel()
            {
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingParentDescriptionItems = new List<C07_ManagementAccounts_OtherExpensesModel.ReportingParentDescriptionItem>(),
                Total = new C07_ManagementAccounts_OtherExpensesModel.ReportingCategoryItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    ReportingDescription = "Total",
                    ReportingDescriptionID = 0,
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


                List<int> parentReportingDescriptions = new List<int>()
                {
                    4,//	Dividends Paid
                    5,//	Finance Costs
                    6,//	Management Fees
                };
                var parentReportingDescriptionsToCheck = db.ManagementAccounts_ReportingParentDescriptions.Where(p => parentReportingDescriptions.Contains(p.ID)).ToList();
                var reportingDescriptionsToCheck = db.ManagementAccounts_ReportingDescriptions.Where(p => p.ParentReportingDescriptionID.HasValue && parentReportingDescriptions.Contains(p.ParentReportingDescriptionID.Value)).ToList();

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   join rc in db.ManagementAccounts_ReportingCategories on p.ReportingCategoryID equals rc.ID into src
                                                   from rc in src.DefaultIfEmpty()
                                                   join rd in db.ManagementAccounts_ReportingDescriptions on p.ReportingDescriptionID equals rd.ID into srd
                                                   from rd in srd.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && companyIDs.Contains(p.CompanyID)
                                                   && reportingDescriptionsToCheck.Select(d => d.ID).Contains(p.ReportingDescriptionID)
                                                   select new
                                                   {
                                                       p.AccountNo,
                                                       p.ActualAmount,
                                                       p.CompanyID,
                                                       p.Date,
                                                       p.Forecast1Amount,
                                                       p.Forecast2Amount,
                                                       p.Forecast3Amount,
                                                       p.Forecast4Amount,
                                                       p.Forecast5Amount,
                                                       p.ID,
                                                       p.ReportingCategoryID,
                                                       p.ReportingDescriptionID,
                                                       rc.ReportingCategory,
                                                       rd.ReportingDescription,
                                                   }).ToList();
                foreach (var parent in parentReportingDescriptionsToCheck)
                {
                    C07_ManagementAccounts_OtherExpensesModel.ReportingParentDescriptionItem parentDescriptionItem = new C07_ManagementAccounts_OtherExpensesModel.ReportingParentDescriptionItem()
                    {
                        ID = parent.ID,
                        ReportingCategoryItems = new List<C07_ManagementAccounts_OtherExpensesModel.ReportingCategoryItem>(),
                        ReportingParentDescription = parent.ReportingParentDescription,
                        ChartColor = parent.ChartColor,
                        ChartType = parent.ChartType,
                        Total = new C07_ManagementAccounts_OtherExpensesModel.ReportingCategoryItem()
                        {
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = "Total",
                            ReportingDescriptionID = 0,
                            ChartColor = parent.ChartColor,
                            ChartType = parent.ChartType,
                        },
                    };

                    foreach (var reportingDescription in reportingDescriptionsToCheck.Where(p => p.ParentReportingDescriptionID.HasValue && p.ParentReportingDescriptionID.Value == parent.ID))
                    {
                        C07_ManagementAccounts_OtherExpensesModel.ReportingCategoryItem reportingCategoryItem = new C07_ManagementAccounts_OtherExpensesModel.ReportingCategoryItem()
                        {
                            ChartColor = reportingDescription.ChartColor,
                            ChartType = reportingDescription.ChartType,
                            MonthlyValues = new Dictionary<DateTime, decimal>(),
                            ReportingDescription = reportingDescription.ReportingDescription,
                            ReportingDescriptionID = reportingDescription.ID,
                        };

                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            #region Item

                            var EmployeeCosts_Admin = (from p in managementAccountsDataDumps
                                                       where p.ReportingDescriptionID == reportingDescription.ID
                                                       && p.Date.Date == current.Date
                                                       select p).ToList();

                            if (EmployeeCosts_Admin.Count != 0)
                                switch (managementAccounts_AmountTypeEnum)
                                {
                                    case ManagementAccounts_AmountTypeEnum.Actual:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.ActualAmount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast1:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast1Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast2:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast2Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast3:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast3Amount).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast4:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast4Amount.HasValue ? p.Forecast4Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                    case ManagementAccounts_AmountTypeEnum.Forecast5:
                                        reportingCategoryItem.MonthlyValues.Add(current, EmployeeCosts_Admin.Select(p => p.Forecast5Amount.HasValue ? p.Forecast5Amount.Value : 0).Sum() * -1.0m);
                                        break;
                                }
                            else
                                reportingCategoryItem.MonthlyValues.Add(current, 0);

                            if (parentDescriptionItem.Total.MonthlyValues.ContainsKey(current))
                                parentDescriptionItem.Total.MonthlyValues[current] = parentDescriptionItem.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                parentDescriptionItem.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);

                            #endregion

                            if (model.Total.MonthlyValues.ContainsKey(current))
                                model.Total.MonthlyValues[current] = model.Total.MonthlyValues[current] + reportingCategoryItem.MonthlyValues[current];
                            else
                                model.Total.MonthlyValues.Add(current, reportingCategoryItem.MonthlyValues[current]);


                            current = current.AddMonths(1);
                        }

                        parentDescriptionItem.ReportingCategoryItems.Add(reportingCategoryItem);
                    }
                    parentDescriptionItem.ReportingCategoryItems = parentDescriptionItem.ReportingCategoryItems.OrderBy(p => p.ReportingDescription).ToList();
                    model.ReportingParentDescriptionItems.Add(parentDescriptionItem);
                }
                model.ReportingParentDescriptionItems = model.ReportingParentDescriptionItems.OrderBy(p => p.ReportingParentDescription).ToList();
            }

            return View("~/Views/Operational/C07_ManagementAccounts/C07_ManagementAccounts_OtherExpenses.cshtml", model);
        }

    }
}
