using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.F_SystemGeneratedReports;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.F_SystemGeneratedReports
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class F_SystemGeneratedReportsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public F_SystemGeneratedReportsController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }


        [HttpGet]
        [Route("/operational/F_SystemGeneratedReports/F_SystemGeneratedReports_AllReports")]
        public async Task<IActionResult> F_SystemGeneratedReports_AllReports()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.F_SystemGeneratedReports_AllReports, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.F_SystemGeneratedReports_AllReports}/{(int)SecureAreaActionEnum.View}");

            #endregion


            SecureAreaEnum secureArea = SecureAreaEnum.F_SystemGeneratedReports_AllReports;

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var systemGeneratedReportSecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                                                    where p.ToString().Contains("F_SystemGeneratedReports")
                                                    orderby p.GetDescription()
                                                    select p).ToList();

            F_SystemGeneratedReports_AllReportsModel model = new F_SystemGeneratedReports_AllReportsModel()
            {
                F_SystemGeneratedReports_AllReportsItems = new List<F_SystemGeneratedReports_AllReportsModel.F_SystemGeneratedReports_AllReportsItem>(),
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.AddDays(1).Date,
                SecureAreaCodeName = secureArea.ToString(),
                SecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                               where p.ToString().Contains("F_SystemGeneratedReports")
                               orderby p.GetDescription()
                               select new SelectListItem()
                               {
                                   Value = p.ToString(),
                                   Text = p.GetDescription(),
                                   Selected = secureArea == p
                               }).ToList()
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var systemGeneratedReports = db.SystemGeneratedReports.Where(p => p.DateStarted >= model.FromDate && p.DateStarted <= model.ToDate).ToList();

            foreach (var sec in systemGeneratedReportSecureAreas)
            {
                if (sec == SecureAreaEnum.F_SystemGeneratedReports_AllReports)
                    continue;

                F_SystemGeneratedReports_AllReportsModel.F_SystemGeneratedReports_AllReportsItem item = new F_SystemGeneratedReports_AllReportsModel.F_SystemGeneratedReports_AllReportsItem()
                {
                    ReportName = sec.GetDescription(),
                    SecureAreaName = sec.ToString(),
                    ReportLocation = SystemGeneratedReport.ReportLocationEnum.Unknown,
                };

                var latestReportStarted = (from p in systemGeneratedReports
                                           where p.SecureAreaID == (int)sec
                                           orderby p.DateStarted descending
                                           select p).FirstOrDefault();
                if (latestReportStarted != null)
                {
                    item.LatestStartDate = latestReportStarted.DateStarted;
                    item.LatestReportProgress = latestReportStarted.Progress;
                    if (latestReportStarted.DateEnded.HasValue)
                    {
                        item.LatestReportDuration = (latestReportStarted.DateEnded.Value - latestReportStarted.DateStarted);
                        if (!latestReportStarted.Progress.HasValue || latestReportStarted.Progress.Value != 100)
                            item.LatestReportFailed = true;
                    }
                    else
                        item.LatestReportInProgress = true;
                }

                var latestReportCompleted = (from p in systemGeneratedReports
                                             where p.SecureAreaID == (int)sec
                                             && p.DateEnded.HasValue
                                             orderby p.DateEnded descending
                                             select p).FirstOrDefault();

                if (latestReportCompleted != null)
                {
                    item.LatestEndDate = latestReportCompleted.DateEnded;
                    item.LatestReportURL = $"/operational/F_SystemGeneratedReportsDownload/{latestReportCompleted.ID}";
                    item.ReportLocation = latestReportCompleted.ReportLocation;
                    if (latestReportCompleted.DateEnded.HasValue)
                        item.LatestReportDuration = (latestReportCompleted.DateEnded.Value - latestReportCompleted.DateStarted);
                }

                item.ReportsCompleted = systemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateEnded.HasValue).Count();
                item.ReportsStarted = systemGeneratedReports.Where(p => p.SecureAreaID == (int)sec).Count();

                model.F_SystemGeneratedReports_AllReportsItems.Add(item);
            }

            return View("~/Views/Operational/F_SystemGeneratedReports/F_SystemGeneratedReports_AllReports.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/F_SystemGeneratedReports/F_SystemGeneratedReports_HistoricalSummary")]
        public async Task<IActionResult> F_SystemGeneratedReports_HistoricalSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            SecureAreaEnum secureArea = SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary;

            //Hangfire.GlobalConfiguration.Configuration.UseSqlServerStorage(_configuration.GetConnectionString("HangfireConnection"));
            //var scheduledJobs = Hangfire.JobStorage.Current.GetMonitoringApi().ScheduledJobs(0, Int32.MaxValue);

            var db = new MyVoltageDbContext(_options);
            var systemGeneratedReportSecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                                                    where p.ToString().Contains("F_SystemGeneratedReports")
                                                    orderby p.GetDescription()
                                                    select p).ToList();

            var f_SystemGeneratedReports_Details = db.F_SystemGeneratedReports_Details.ToList();

            F_SystemGeneratedReports_HistoricalSummaryModel model = new F_SystemGeneratedReports_HistoricalSummaryModel()
            {
                F_SystemGeneratedReports_HistoricalSummaryItems = new List<F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem>(),
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.AddDays(1).Date,
                SecureAreaCodeName = secureArea.ToString(),
                SecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                               where p.ToString().Contains("F_SystemGeneratedReports")
                               orderby p.GetDescription()
                               select new SelectListItem()
                               {
                                   Value = p.ToString(),
                                   Text = p.GetDescription(),
                                   Selected = secureArea == p
                               }).ToList()
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var systemGeneratedReports = db.SystemGeneratedReports.Where(p => p.DateStarted >= model.FromDate && p.DateStarted <= model.ToDate).ToList();

            foreach (var sec in systemGeneratedReportSecureAreas)
            {
                if (sec == SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary || sec == SecureAreaEnum.F_SystemGeneratedReports_AllReports)
                    continue;

                F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem item = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem()
                {
                    ReportName = sec.GetDescription(),
                    SecureAreaName = sec.ToString(),
                    ReportLocation = SystemGeneratedReport.ReportLocationEnum.Unknown,
                    SecureArea = sec,
                    LatestEndDate = null,
                    LatestReportDuration = null,
                    LatestReportFailed = false,
                    LatestReportInProgress = false,
                    LatestReportProgress = null,
                    LatestReportURL = null,
                    LatestStartDate = null,
                    ReportsCompleted = 0,
                    ReportsStarted = 0,
                    PastDay = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_HistoricalSummarySubItem(),
                    PastMonth = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_HistoricalSummarySubItem(),
                    PastThreeDays = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_HistoricalSummarySubItem(),
                    PastTwoWeeks = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_HistoricalSummarySubItem(),
                    PastWeek = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_HistoricalSummarySubItem(),
                };

                #region PastDay

                var systemGeneratedReportsPastDay = db.SystemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateStarted >= DateTime.Now.AddDays(-1)).ToList();
                item.PastDay.Started = systemGeneratedReportsPastDay.Count();
                item.PastDay.Completed = systemGeneratedReportsPastDay.Where(p => p.DateEnded.HasValue && p.Progress.HasValue && p.Progress.Value == 100).Count();

                #endregion

                #region PastThreeDays

                var systemGeneratedReportsPastThreeDays = db.SystemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateStarted >= DateTime.Now.AddDays(-3)).ToList();
                item.PastThreeDays.Started = systemGeneratedReportsPastThreeDays.Count();
                item.PastThreeDays.Completed = systemGeneratedReportsPastThreeDays.Where(p => p.DateEnded.HasValue && p.Progress.HasValue && p.Progress.Value == 100).Count();

                #endregion

                #region PastWeek

                var systemGeneratedReportsPastWeek = db.SystemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateStarted >= DateTime.Now.AddDays(-7)).ToList();
                item.PastWeek.Started = systemGeneratedReportsPastWeek.Count();
                item.PastWeek.Completed = systemGeneratedReportsPastWeek.Where(p => p.DateEnded.HasValue && p.Progress.HasValue && p.Progress.Value == 100).Count();

                #endregion

                #region PastTwoWeeks

                var systemGeneratedReportsPastTwoWeeks = db.SystemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateStarted >= DateTime.Now.AddDays(-14)).ToList();
                item.PastTwoWeeks.Started = systemGeneratedReportsPastTwoWeeks.Count();
                item.PastTwoWeeks.Completed = systemGeneratedReportsPastTwoWeeks.Where(p => p.DateEnded.HasValue && p.Progress.HasValue && p.Progress.Value == 100).Count();

                #endregion

                #region PastMonth

                var systemGeneratedReportsPastMonth = db.SystemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateStarted >= DateTime.Now.AddMonths(-1)).ToList();
                item.PastMonth.Started = systemGeneratedReportsPastMonth.Count();
                item.PastMonth.Completed = systemGeneratedReportsPastMonth.Where(p => p.DateEnded.HasValue && p.Progress.HasValue && p.Progress.Value == 100).Count();

                #endregion

                var latestReportStarted = (from p in systemGeneratedReports
                                           where p.SecureAreaID == (int)sec
                                           orderby p.DateStarted descending
                                           select p).FirstOrDefault();
                if (latestReportStarted != null)
                {
                    item.LatestStartDate = latestReportStarted.DateStarted;
                    item.LatestReportProgress = latestReportStarted.Progress;
                    if (latestReportStarted.DateEnded.HasValue)
                    {
                        item.LatestReportDuration = (latestReportStarted.DateEnded.Value - latestReportStarted.DateStarted);
                        if (!latestReportStarted.Progress.HasValue || latestReportStarted.Progress.Value != 100)
                            item.LatestReportFailed = true;
                    }
                    else
                        item.LatestReportInProgress = true;
                }

                var latestReportCompleted = (from p in systemGeneratedReports
                                             where p.SecureAreaID == (int)sec
                                             && p.DateEnded.HasValue
                                             orderby p.DateEnded descending
                                             select p).FirstOrDefault();

                if (latestReportCompleted != null)
                {
                    item.LatestEndDate = latestReportCompleted.DateEnded;
                    item.LatestReportURL = $"/operational/F_SystemGeneratedReportsDownload/{latestReportCompleted.ID}";
                    item.ReportLocation = latestReportCompleted.ReportLocation;
                    if (latestReportCompleted.DateEnded.HasValue)
                        item.LatestReportDuration = (latestReportCompleted.DateEnded.Value - latestReportCompleted.DateStarted);
                }

                item.ReportsCompleted = systemGeneratedReports.Where(p => p.SecureAreaID == (int)sec && p.DateEnded.HasValue).Count();
                item.ReportsStarted = systemGeneratedReports.Where(p => p.SecureAreaID == (int)sec).Count();

                var f_SystemGeneratedReports_Detail = f_SystemGeneratedReports_Details.Where(p => p.SecureAreaID == (int)sec).SingleOrDefault();
                if (f_SystemGeneratedReports_Detail != null)
                {
                    item.F_SystemGeneratedReports_DetailItem = new F_SystemGeneratedReports_HistoricalSummaryModel.F_SystemGeneratedReports_HistoricalSummaryItem.F_SystemGeneratedReports_Detail()
                    {
                        Comments = f_SystemGeneratedReports_Detail.Comments,
                        DateUpdated = f_SystemGeneratedReports_Detail.DateUpdated,
                        Frequency = f_SystemGeneratedReports_Detail.Frequency,
                        ID = f_SystemGeneratedReports_Detail.ID,
                        LastCodeUpdate = f_SystemGeneratedReports_Detail.LastCodeUpdate,
                        PriorityID = f_SystemGeneratedReports_Detail.PriorityID,
                        SecureAreaID = f_SystemGeneratedReports_Detail.SecureAreaID,
                        StandardDurationSec = f_SystemGeneratedReports_Detail.StandardDurationSec,
                        StartTimes = f_SystemGeneratedReports_Detail.StartTimes,
                        UpdatedBy = f_SystemGeneratedReports_Detail.UpdatedBy,
                        UserName = "",
                    };


                }

                model.F_SystemGeneratedReports_HistoricalSummaryItems.Add(item);
            }

            return View("~/Views/Operational/F_SystemGeneratedReports/F_SystemGeneratedReports_HistoricalSummary.cshtml", model);
        }


        [HttpPost]
        [Route("/operational/F_SystemGeneratedReports/F_SystemGeneratedReports_HistoricalSummaryUpdateDetails/{secureAreaID}")]
        public async Task<IActionResult> Y01_UserAdmin_UserEdit_AddSecureArea(int secureAreaID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary, SecureAreaActionEnum.ManagementApproval))
                return Content("false");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.F_SystemGeneratedReports_Details
                                where p.SecureAreaID == secureAreaID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    existing = new F_SystemGeneratedReports_Detail()
                    {
                        Comments = !string.IsNullOrEmpty(Request.Form["comments_"]) ? Request.Form["comments_"].ToString() : "",
                        DateUpdated = DateTime.Now,
                        Frequency = !string.IsNullOrEmpty(Request.Form["frequency_"]) ? Request.Form["frequency_"].ToString() : "",
                        LastCodeUpdate = null,
                        SecureAreaID = secureAreaID,
                        StartTimes = !string.IsNullOrEmpty(Request.Form["startTimes_"]) ? Request.Form["startTimes_"].ToString() : "",
                        UpdatedBy = _userManager.GetUserId(User),
                    };

                    if (!string.IsNullOrEmpty(Request.Form["priority_"]))
                        existing.PriorityID = Convert.ToInt32(Request.Form["priority_"]);

                    if (!string.IsNullOrEmpty(Request.Form["standardDurationSec_"]))
                        existing.StandardDurationSec = Convert.ToDecimal(Request.Form["standardDurationSec_"]);

                    if (!string.IsNullOrEmpty(Request.Form["lastCodeUpdate_"]))
                        existing.LastCodeUpdate = Convert.ToDateTime(Request.Form["lastCodeUpdate_"]);


                    db.Add(existing);
                }
                else
                {
                    if (!string.IsNullOrEmpty(Request.Form["priority_"]))
                        existing.PriorityID = Convert.ToInt32(Request.Form["priority_"]);

                    if (!string.IsNullOrEmpty(Request.Form["standardDurationSec_"]))
                        existing.StandardDurationSec = Convert.ToDecimal(Request.Form["standardDurationSec_"]);

                    if (!string.IsNullOrEmpty(Request.Form["comments_"]))
                        existing.Comments = Request.Form["comments_"];

                    if (!string.IsNullOrEmpty(Request.Form["frequency_"]))
                        existing.Frequency = Request.Form["frequency_"];

                    if (!string.IsNullOrEmpty(Request.Form["startTimes_"]))
                        existing.StartTimes = Request.Form["startTimes_"];

                    if (!string.IsNullOrEmpty(Request.Form["lastCodeUpdate_"]))
                        existing.LastCodeUpdate = Convert.ToDateTime(Request.Form["lastCodeUpdate_"]);

                    db.Update(existing);
                }

                db.SaveChanges();

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/F_SystemGeneratedReports/{*secureAreaName}")]
        public async Task<IActionResult> F_SystemGeneratedReportsList(string secureAreaName)
        {
            SecureAreaEnum secureArea = SecureAreaEnum.F_SystemGeneratedReports_AllReports;

            foreach (SecureAreaEnum secureAreatoCheck in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum)))
            {
                if (secureAreatoCheck.ToString() == secureAreaName)
                    secureArea = secureAreatoCheck;
            }

            var systemGeneratedReportSecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                                                    where p.ToString().Contains("F_SystemGeneratedReports")
                                                    orderby p.GetDescription()
                                                    select p).ToList();

            F_SystemGeneratedReportsListModel model = new F_SystemGeneratedReportsListModel()
            {
                F_SystemGeneratedReportsListItems = new List<F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem>(),
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.AddDays(1).Date,
                SecureAreaCodeName = secureAreaName,
                SecureAreas = (from p in (SecureAreaEnum[])Enum.GetValues(typeof(SecureAreaEnum))
                               where p.ToString().Contains("F_SystemGeneratedReports")
                               orderby p.GetDescription()
                               select new SelectListItem()
                               {
                                   Value = p.ToString(),
                                   Text = p.GetDescription(),
                                   Selected = secureArea == p
                               }).ToList()
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            ViewData["Title"] = secureArea.GetDescription();

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));
            var db = new MyVoltageDbContext(_options);
            var companies = (from p in db.Companies
                             select new
                             {
                                 p.Name,
                                 p.CompanyID
                             }).ToList();
            var lastReports = (from p in db.SystemGeneratedReports
                               where p.SecureAreaID == (int)secureArea
                               && p.DateStarted.Date >= model.FromDate.Date
                               && p.DateStarted.Date <= model.ToDate.Date
                               orderby p.DateStarted descending
                               select p).ToList();

            var reportsCompaniess = (from p in db.SystemGeneratedReports_Companies
                                     where lastReports.Select(c => c.ID).Contains(p.SystemGeneratedReportID)
                                     select p).ToList();
            var reportsCompanies = (from p in reportsCompaniess
                                    where lastReports.Select(c => c.ID).Contains(p.SystemGeneratedReportID)
                                    select p).ToList();

            var f_SystemGeneratedReports_AccountingChecklist_Requests = db.F_SystemGeneratedReports_AccountingChecklist_Requests.ToList();
            var f_SystemGeneratedReports_ManagementAccounts_Requests = db.F_SystemGeneratedReports_ManagementAccounts_Requests.ToList();

            foreach (var report in lastReports)
            {
                F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem item = new F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem()
                {
                    DateEnded = report.DateEnded,
                    DateStarted = report.DateStarted,
                    ID = report.ID,
                    ReportURL = report.ReportURL,
                    SecureAreaID = report.SecureAreaID,
                    Progress = report.Progress,
                    NotificationSent = report.NotificationSent,
                    NotificationSentTo = report.NotificationSentTo,
                    ReportLocationID = report.ReportLocationID,
                    RetryCount = report.RetryCount,
                    F_SystemGeneratedReportsListItemCompanies = new List<F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem.F_SystemGeneratedReportsListItemCompany>(),
                    ReportParam = "",
                };

                var accountingChecklist_Request = f_SystemGeneratedReports_AccountingChecklist_Requests.Where(p => p.SystemReportID.HasValue && p.SystemReportID.Value == report.ID).SingleOrDefault();
                if (accountingChecklist_Request != null)
                    item.ReportParam = $"Company: {(accountingChecklist_Request.CompanyID.HasValue ? companies.Where(p => p.CompanyID == accountingChecklist_Request.CompanyID.Value).SingleOrDefault().Name : "All")}<br />From: {accountingChecklist_Request.FromDate.ToDateShort()}<br />To: {accountingChecklist_Request.ToDate.ToDateShort()}";

                var managementAccounts_Request = f_SystemGeneratedReports_ManagementAccounts_Requests.Where(p => p.SystemReportID.HasValue && p.SystemReportID.Value == report.ID).SingleOrDefault();
                if (managementAccounts_Request != null)
                    item.ReportParam = $"Company: {(managementAccounts_Request.CompanyID.HasValue ? companies.Where(p => p.CompanyID == managementAccounts_Request.CompanyID.Value).SingleOrDefault().Name : "All")}<br />From: {managementAccounts_Request.FromDate.ToDateShort()}<br />To: {managementAccounts_Request.ToDate.ToDateShort()}";

                var repCompanies = reportsCompanies.Where(p => p.SystemGeneratedReportID == report.ID).ToList();
                foreach (var repC in repCompanies)
                {
                    F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem.F_SystemGeneratedReportsListItemCompany itemCompany = new F_SystemGeneratedReportsListModel.F_SystemGeneratedReportsListItem.F_SystemGeneratedReportsListItemCompany()
                    {
                        CompanyID = repC.CompanyID,
                        CompanyName = companies.Where(p => p.CompanyID == repC.CompanyID).SingleOrDefault().Name,
                        DateCompleted = repC.DateCompleted,
                        ID = repC.ID,
                        SystemGeneratedReportID = repC.SystemGeneratedReportID,
                    };

                    item.F_SystemGeneratedReportsListItemCompanies.Add(itemCompany);
                }
                item.F_SystemGeneratedReportsListItemCompanies = item.F_SystemGeneratedReportsListItemCompanies.OrderBy(p => p.CompanyName).ToList();
                model.F_SystemGeneratedReportsListItems.Add(item);
            }


            return View("~/Views/Operational/F_SystemGeneratedReports/F_SystemGeneratedReportsList.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/F_SystemGeneratedReportsDownload/{reportID}")]
        public async Task<IActionResult> F_SystemGeneratedReportsDownload(int reportID)
        {


            var db = new MyVoltageDbContext(_options);

            var item = db.SystemGeneratedReports.Where(p => p.ID == reportID).SingleOrDefault();

            if (item != null)
            {
                string shareName = "f-systemgeneratedreports";
                string dirName = $"{item.SecureAreaID}";
                string dirNameDate = $"{item.DateStarted:yyyy_MM_dd}";
                string fileName = $"{System.IO.Path.GetFileName(item.ReportURL)}";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                ShareDirectoryClient directoryDate = directory.GetSubdirectoryClient(dirNameDate);
                ShareFileClient file = directoryDate.GetFileClient(fileName);

                // Download the file
                ShareFileDownloadInfo download = file.Download();
                Stream uploadFile = new MemoryStream();
                download.Content.CopyTo(uploadFile);
                uploadFile.Position = 0;
                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(item.ReportURL, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (file != null)
                    return File(uploadFile, contentType, System.IO.Path.GetFileName(item.ReportURL));
            }

            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/F_SystemGeneratedReports_Connector_Download/{reportID}")]
        public async Task<IActionResult> F_SystemGeneratedReports_Connector_Download(int reportID)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.ConnectionRuns.Where(p => p.ID == reportID).SingleOrDefault();

            if (item != null)
            {
                ClosedXML.Excel.XLWorkbook xLWorkbook = new ClosedXML.Excel.XLWorkbook();
                var xLWorksheet = xLWorkbook.AddWorksheet("Connector");
                var table = xLWorksheet.Cell(1, 1).InsertTable(db.ConnectionRun_Customers.Where(p => p.ConnectionRunID == item.ID).ToList());

                Stream stream = new MemoryStream();
                xLWorkbook.SaveAs(stream);
                stream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType("Connector.xlsx", out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (stream != null)
                    return File(stream, contentType, System.IO.Path.GetFileName("Connector.xlsx"));
            }

            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/F_SystemGeneratedReports_SOC_Download/{reportID}")]
        public async Task<IActionResult> F_SystemGeneratedReports_SOC_Download(int reportID)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.SystemGeneratedReports.Where(p => p.ID == reportID).SingleOrDefault();

            if (item != null)
            {
                ClosedXML.Excel.XLWorkbook xLWorkbook = new ClosedXML.Excel.XLWorkbook();
                var xLWorksheet = xLWorkbook.AddWorksheet("SOC");

                var SOC = (from s in db.SOC_Snapshots
                           join i in db.SOC_SnapshotItems on s.ID equals i.SnapshotID into si
                           from i in si.DefaultIfEmpty()
                           join c in db.Companies on s.CompanyID equals c.CompanyID into sc
                           from c in sc.DefaultIfEmpty()
                           where s.SnapshotDate.Date == item.DateStarted.Date
                           orderby c.Name, i.ItemCode
                           select new
                           {
                               CompanyName = c.Name,
                               s.SnapshotDate,
                               s.CustomerCount,
                               s.DevicesCount,
                               s.GatewaysCount,
                               s.AvgTurnover,
                               s.AvgGrossProfit,
                               i.ItemCode,
                               i.DidPass,
                               i.SecureAreaID,
                               i.DateUpdated,
                               i.ProblemChild,
                           }).ToList();

                var table = xLWorksheet.Cell(1, 1).InsertTable(SOC);
                xLWorksheet.Columns("A", "ZZ").AdjustToContents();

                Stream stream = new MemoryStream();
                xLWorkbook.SaveAs(stream);
                stream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType("SOC.xlsx", out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (stream != null)
                    return File(stream, contentType, System.IO.Path.GetFileName($"SOC_{item.DateStarted:yyyy_MM_dd}.xlsx"));
            }

            return Content("The file you are looking for could not be found.");
        }




    }
}
