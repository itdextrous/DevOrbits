using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.D02_SaleModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.D02_Sale
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class D02_SaleTasksController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public D02_SaleTasksController(
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
        [Route("/operational/D02_Sale/D02_SaleTasks_Company_Summary")]
        public async Task<IActionResult> D02_SaleTasks_Company_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Company_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Company_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Company_SummaryModel model = new D02_SaleTasks_Company_SummaryModel()
            {
                D02_SaleTasks_CompanySummaryItems = new List<D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                D02_SaleTasks_Company_SummaryStatusItems = new List<D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryStatusItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            List<Data.D02_SaleTask> a09_Tasks = (from p in db.D02_SaleTasks
                                                 where p.DueDate.HasValue
                                                 && p.DueDate.Value.Date >= model.FromDate
                                                 && p.DueDate.Value.Date <= model.ToDate
                                                 select p).ToList();

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryStatusItem item = new D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryStatusItem()
                {
                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                    ID = status.ID,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                };
                model.D02_SaleTasks_Company_SummaryStatusItems.Add(item);
            }

            #region No Company

            var noCompanyItems = a09_Tasks.Where(p => !p.CompanyID.HasValue).ToList();

            if (noCompanyItems.Count > 0)
            {
                D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem item = new D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem()
                {
                    CompanyID = 0,
                    CompanyName = "None",
                    D02_SaleTasks_Company_SummaryItemStatuses = new List<D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus>(),
                };

                foreach (var status in siteAdmin_Statuses)
                {
                    D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus itemStatus = new D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus()
                    {
                        Count = noCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                        StatusID = status.ID,
                    };

                    item.D02_SaleTasks_Company_SummaryItemStatuses.Add(itemStatus);
                }

                foreach (var task in noCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                {
                    TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                    if (task.DueDate.Value.Date == DateTime.Now.Date)
                        item.TodayCount++;
                    else if (openDuration.TotalDays <= 2)
                        item.OlderThan1DayCount++;
                    else if (openDuration.TotalDays <= 3)
                        item.OlderThan3DaysCount++;
                    else if (openDuration.TotalDays <= 7)
                        item.OlderThan7DaysCount++;
                    else if (openDuration.TotalDays <= 14)
                        item.OlderThan14DaysCount++;
                    else
                        item.OlderThan1MonthCount++;
                }

                var oldestNoCompanyFlag = noCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                if (oldestNoCompanyFlag != null)
                {
                    item.OldestUnresolvedTaskCreateDate = oldestNoCompanyFlag.DueDate.Value;
                    item.OldestUnresolvedTaskID = oldestNoCompanyFlag.ID;
                    item.OldestUnresolvedTaskTypeID = oldestNoCompanyFlag.TaskTypeID;
                }

                model.D02_SaleTasks_CompanySummaryItems.Add(item);
            }

            #endregion

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var thisCompanyItems = a09_Tasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

                if (thisCompanyItems.Count > 0)
                {
                    D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem item = new D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        D02_SaleTasks_Company_SummaryItemStatuses = new List<D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus itemStatus = new D02_SaleTasks_Company_SummaryModel.D02_SaleTasks_Company_SummaryItem.D02_SaleTasks_Company_SummaryItemStatus()
                        {
                            Count = thisCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                        };

                        item.D02_SaleTasks_Company_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var task in thisCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                        if (task.DueDate.Value.Date == DateTime.Now.Date)
                            item.TodayCount++;
                        else if (openDuration.TotalDays <= 2)
                            item.OlderThan1DayCount++;
                        else if (openDuration.TotalDays <= 3)
                            item.OlderThan3DaysCount++;
                        else if (openDuration.TotalDays <= 7)
                            item.OlderThan7DaysCount++;
                        else if (openDuration.TotalDays <= 14)
                            item.OlderThan14DaysCount++;
                        else
                            item.OlderThan1MonthCount++;
                    }


                    var oldestFlag = thisCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                    if (oldestFlag != null)
                    {
                        item.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedTaskID = oldestFlag.ID;
                        item.OldestUnresolvedTaskTypeID = oldestFlag.TaskTypeID;
                    }

                    model.D02_SaleTasks_CompanySummaryItems.Add(item);
                }

            }



            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Company_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Company_Details")]
        public async Task<IActionResult> D02_SaleTasks_Company_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Company_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Company_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Company_DetailsModel model = new D02_SaleTasks_Company_DetailsModel()
            {
                D02_SaleTasks_CompanyDetailsItems = new List<D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            //var responsiblePeople = dbCache.D02_SaleTasks_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();


            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            if (_operationalProvider.CompanyID == 0)
            {
                tasks = tasks1.Where(p => !p.CompanyID.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();
            }
            else
            {
                tasks = tasks1.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();
            }

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem item = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem.D02_SaleTasks_Company_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_CompanyDetailsItems.Add(item);
            }


            model.D02_SaleTasks_CompanyDetailsItems = model.D02_SaleTasks_CompanyDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();

            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Company_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Company_Results")]
        public async Task<IActionResult> D02_SaleTasks_Company_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Company_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Company_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Company_DetailsModel model = new D02_SaleTasks_Company_DetailsModel()
            {
                D02_SaleTasks_CompanyDetailsItems = new List<D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            //var responsiblePeople = dbCache.D02_SaleTasks_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID == 0)
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && !p.CompanyID.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }
            else
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem item = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_Company_DetailsModel.D02_SaleTasks_Company_DetailsItem.D02_SaleTasks_Company_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_CompanyDetailsItems.Add(item);
            }


            model.D02_SaleTasks_CompanyDetailsItems = model.D02_SaleTasks_CompanyDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();

            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Company_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_User_Summary")]
        public async Task<IActionResult> D02_SaleTasks_User_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_User_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_User_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_User_SummaryModel model = new D02_SaleTasks_User_SummaryModel()
            {
                D02_SaleTasks_UserSummaryItems = new List<D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem>(),
                D02_SaleTasks_User_SummaryStatusItems = new List<D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryStatusItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var opProfs = dbCache.OperationalProfiles;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryStatusItem item = new D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryStatusItem()
                {
                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                    ID = status.ID,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                };
                model.D02_SaleTasks_User_SummaryStatusItems.Add(item);
            }

            List<Data.D02_SaleTask> a09_Tasks = (from p in db.D02_SaleTasks
                                                 where p.DueDate.HasValue
                                                 && p.DueDate.Value.Date >= model.FromDate
                                                 && p.DueDate.Value.Date <= model.ToDate
                                                 select p).ToList();

            var uniqueUserIDs = (from p in a09_Tasks
                                 select p.ResponsibleUserID).Distinct().ToList();


            foreach (var userID in uniqueUserIDs)
            {
                string userName = "";
                var op = opProfs.Where(p => p.UserID == userID).SingleOrDefault();
                if (op != null && !string.IsNullOrEmpty(op.FirstName))
                {
                    userName = op.FirstName + " " + op.LastName;
                }
                else
                {
                    userName = db.Users.Where(p => p.Id == userID).SingleOrDefault().UserName;
                }

                var thisUserItems = a09_Tasks.Where(p => p.ResponsibleUserID == userID).ToList();

                if (thisUserItems.Count > 0)
                {
                    D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem item = new D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem()
                    {
                        UserID = userID,
                        UserName = userName,
                        D02_SaleTasks_User_SummaryItemStatuses = new List<D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem.D02_SaleTasks_User_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem.D02_SaleTasks_User_SummaryItemStatus itemStatus = new D02_SaleTasks_User_SummaryModel.D02_SaleTasks_User_SummaryItem.D02_SaleTasks_User_SummaryItemStatus()
                        {
                            Count = thisUserItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                        };

                        item.D02_SaleTasks_User_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var task in thisUserItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                        if (task.DueDate.Value.Date == DateTime.Now.Date)
                            item.TodayCount++;
                        else if (openDuration.TotalDays <= 2)
                            item.OlderThan1DayCount++;
                        else if (openDuration.TotalDays <= 3)
                            item.OlderThan3DaysCount++;
                        else if (openDuration.TotalDays <= 7)
                            item.OlderThan7DaysCount++;
                        else if (openDuration.TotalDays <= 14)
                            item.OlderThan14DaysCount++;
                        else
                            item.OlderThan1MonthCount++;
                    }


                    var oldestFlag = thisUserItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                    if (oldestFlag != null)
                    {
                        item.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedTaskID = oldestFlag.ID;
                        item.OldestUnresolvedTaskTypeID = oldestFlag.TaskTypeID;
                    }

                    model.D02_SaleTasks_UserSummaryItems.Add(item);
                }

            }



            model.D02_SaleTasks_UserSummaryItems = model.D02_SaleTasks_UserSummaryItems.OrderBy(p => p.UserName).ToList();
            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_User_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_User_Details")]
        public async Task<IActionResult> D02_SaleTasks_User_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_User_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_User_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_User_DetailsModel model = new D02_SaleTasks_User_DetailsModel()
            {
                D02_SaleTasks_UserDetailsItems = new List<D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            if (string.IsNullOrEmpty(_operationalProvider.TaskResponsibleUserID) && string.IsNullOrEmpty(_operationalProvider.TaskReportingToUserID))
                return Redirect("/operational/D02_Sale/D02_SaleTasks_User_Summary");

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            if (_operationalProvider.TaskResponsibleUser)
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.ResponsibleUserID == _operationalProvider.TaskResponsibleUserID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }
            else
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.ReportingToUserID == _operationalProvider.TaskReportingToUserID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }
            tasks = tasks.Where(p => p.DueDate.HasValue && p.ResponsibleUserID == _operationalProvider.TaskResponsibleUserID && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            model.TaskResponsibleUser = _operationalProvider.TaskResponsibleUser;

            string userName = "";
            var op = opProfs.Where(p => p.UserID == (_operationalProvider.TaskResponsibleUser ? _operationalProvider.TaskResponsibleUserID : _operationalProvider.TaskReportingToUserID)).SingleOrDefault();
            if (op != null && !string.IsNullOrEmpty(op.FirstName))
            {
                userName = op.FirstName + " " + op.LastName;
            }
            else
            {
                userName = _userManager.FindByIdAsync((_operationalProvider.TaskResponsibleUser ? _operationalProvider.TaskResponsibleUserID : _operationalProvider.TaskReportingToUserID)).Result.UserName;
            }
            model.UserName = userName;

            foreach (var task in tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem item = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem.D02_SaleTasks_User_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_UserDetailsItems.Add(item);
            }


            model.D02_SaleTasks_UserDetailsItems = model.D02_SaleTasks_UserDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();

            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_User_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_User_Results")]
        public async Task<IActionResult> D02_SaleTasks_User_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_User_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_User_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_User_DetailsModel model = new D02_SaleTasks_User_DetailsModel()
            {
                D02_SaleTasks_UserDetailsItems = new List<D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            if (string.IsNullOrEmpty(_operationalProvider.TaskResponsibleUserID) && string.IsNullOrEmpty(_operationalProvider.TaskReportingToUserID))
                return Redirect("/operational/D02_Sale/D02_SaleTasks_User_Summary");

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            if (_operationalProvider.TaskResponsibleUser)
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.ResponsibleUserID == _operationalProvider.TaskResponsibleUserID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }
            else
            {
                tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.ReportingToUserID == _operationalProvider.TaskReportingToUserID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            }
            model.TaskResponsibleUser = _operationalProvider.TaskResponsibleUser;

            string userName = "";
            var op = opProfs.Where(p => p.UserID == (_operationalProvider.TaskResponsibleUser ? _operationalProvider.TaskResponsibleUserID : _operationalProvider.TaskReportingToUserID)).SingleOrDefault();
            if (op != null && !string.IsNullOrEmpty(op.FirstName))
            {
                userName = op.FirstName + " " + op.LastName;
            }
            else
            {
                userName = _userManager.FindByIdAsync((_operationalProvider.TaskResponsibleUser ? _operationalProvider.TaskResponsibleUserID : _operationalProvider.TaskReportingToUserID)).Result.UserName;
            }
            model.UserName = userName;


            foreach (var task in tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem item = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_User_DetailsModel.D02_SaleTasks_User_DetailsItem.D02_SaleTasks_User_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_UserDetailsItems.Add(item);
            }


            model.D02_SaleTasks_UserDetailsItems = model.D02_SaleTasks_UserDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();
            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_User_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Type_Summary")]
        public async Task<IActionResult> D02_SaleTasks_Type_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Type_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Type_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Type_SummaryModel model = new D02_SaleTasks_Type_SummaryModel()
            {
                D02_SaleTasks_TypeSummaryItems = new List<D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                D02_SaleTasks_Type_SummaryStatusItems = new List<D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryStatusItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var opProfs = dbCache.OperationalProfiles;
            var D02_SaleTask_Types = db.D02_SaleTask_Types.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryStatusItem item = new D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryStatusItem()
                {
                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                    ID = status.ID,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                };
                model.D02_SaleTasks_Type_SummaryStatusItems.Add(item);
            }

            List<Data.D02_SaleTask> a09_Tasks = (from p in db.D02_SaleTasks
                                                 where p.DueDate.HasValue
                                                 && p.DateCreated.Date >= model.FromDate
                                                 && p.DateCreated.Date <= model.ToDate
                                                 select p).ToList();

            foreach (var type in D02_SaleTask_Types)
            {
                var thisTypeItems = a09_Tasks.Where(p => p.TaskTypeID == type.ID).ToList();

                if (thisTypeItems.Count > 0)
                {
                    D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem item = new D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem()
                    {
                        TypeID = type.ID,
                        TypeName = type.Heading,
                        D02_SaleTasks_Type_SummaryItemStatuses = new List<D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem.D02_SaleTasks_Type_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem.D02_SaleTasks_Type_SummaryItemStatus itemStatus = new D02_SaleTasks_Type_SummaryModel.D02_SaleTasks_Type_SummaryItem.D02_SaleTasks_Type_SummaryItemStatus()
                        {
                            Count = thisTypeItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                        };

                        item.D02_SaleTasks_Type_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var task in thisTypeItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                        if (task.DueDate.Value.Date == DateTime.Now.Date)
                            item.TodayCount++;
                        else if (openDuration.TotalDays <= 2)
                            item.OlderThan1DayCount++;
                        else if (openDuration.TotalDays <= 3)
                            item.OlderThan3DaysCount++;
                        else if (openDuration.TotalDays <= 7)
                            item.OlderThan7DaysCount++;
                        else if (openDuration.TotalDays <= 14)
                            item.OlderThan14DaysCount++;
                        else
                            item.OlderThan1MonthCount++;
                    }


                    var oldestFlag = thisTypeItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                    if (oldestFlag != null)
                    {
                        item.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedTaskID = oldestFlag.ID;
                        item.OldestUnresolvedTaskTypeID = oldestFlag.TaskTypeID;
                    }

                    model.D02_SaleTasks_TypeSummaryItems.Add(item);
                }

            }


            model.D02_SaleTasks_TypeSummaryItems = model.D02_SaleTasks_TypeSummaryItems.OrderBy(p => p.TypeName).ToList();
            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Type_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Type_Details")]
        public async Task<IActionResult> D02_SaleTasks_Type_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Type_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Type_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Type_DetailsModel model = new D02_SaleTasks_Type_DetailsModel()
            {
                D02_SaleTasks_TypeDetailsItems = new List<D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var type = taskTypes.Where(p => p.ID == _operationalProvider.D02SelectedTaskTypeID).SingleOrDefault();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.D02SelectedTaskTypeID == 0 || type == null)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            var tasks1 = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.TaskTypeID == _operationalProvider.D02SelectedTaskTypeID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && p.TaskTypeID == _operationalProvider.D02SelectedTaskTypeID && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            model.TypeName = type.Heading;

            foreach (var task in tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem item = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem.D02_SaleTasks_Type_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_TypeDetailsItems.Add(item);
            }


            model.D02_SaleTasks_TypeDetailsItems = model.D02_SaleTasks_TypeDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();
            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Type_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Type_Results")]
        public async Task<IActionResult> D02_SaleTasks_Type_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Type_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Type_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D02_SaleTasks_Type_DetailsModel model = new D02_SaleTasks_Type_DetailsModel()
            {
                D02_SaleTasks_TypeDetailsItems = new List<D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var type = taskTypes.Where(p => p.ID == _operationalProvider.D02SelectedTaskTypeID).SingleOrDefault();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.D02SelectedTaskTypeID == 0 || type == null)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            List<Data.D02_SaleTask> tasks = new List<Data.D02_SaleTask>();

            tasks = db.D02_SaleTasks.Where(p => p.DueDate.HasValue && p.TaskTypeID == _operationalProvider.D02SelectedTaskTypeID && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            model.TypeName = type.Heading;

            foreach (var task in tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem item = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    D02_SaleTasks_TypeItem = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new D02_SaleTasks_Type_DetailsModel.D02_SaleTasks_Type_DetailsItem.D02_SaleTasks_Type_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                model.D02_SaleTasks_TypeDetailsItems.Add(item);
            }


            model.D02_SaleTasks_TypeDetailsItems = model.D02_SaleTasks_TypeDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();
            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Type_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Review/{taskTypeID}/{taskID}")]
        public async Task<IActionResult> D02_SaleTask_Review(int taskTypeID, int taskID)
        {
            return Redirect($"/operational/Change_D02_TaskID/{taskTypeID}/{taskID}?R=/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Delete/{taskID}")]
        public async Task<IActionResult> D02_SaleTask_Delete(int taskID)
        {
            var db = new MyVoltageDbContext(_options);
            var D02_SaleTasks_Attachments = db.D02_SaleTasks_Attachments.Where(p => p.ID == taskID).ToList();
            db.RemoveRange(D02_SaleTasks_Attachments);
            db.SaveChanges();
            var D02_SaleTasks_ReassignLogs = db.D02_SaleTasks_ReassignLogs.Where(p => p.ID == taskID).ToList();
            db.RemoveRange(D02_SaleTasks_ReassignLogs);
            db.SaveChanges();
            var D02_SaleTask = db.D02_SaleTasks.Where(p => p.ID == taskID).SingleOrDefault();
            db.Remove(D02_SaleTask);
            db.SaveChanges();
            return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Review")]
        public async Task<IActionResult> D02_SaleTask_Review()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion


            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var priorities = db.SiteAdmin_Priorities.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var SiteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();
            siteAdmin_MeetingAgendaes = siteAdmin_MeetingAgendaes.OrderBy(p => p.MeetingAgendaGroupID).ThenBy(p => p.MeetingAgendaActionID).ToList();
            var vehicles = db.Vehicles.ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();

            D02_SaleTask_ReviewModel model = new D02_SaleTask_ReviewModel()
            {
                ReportingToUser = new List<SelectListItem>(),
                ResponsibleUser = new List<SelectListItem>(),
            };

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();
            var tType = db.D02_SaleTask_Types.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

            if (task != null)
            {
                if (!task.DueDate.HasValue)
                {
                    task.DueDate = task.DateCreated.AddWorkdays(tType.MinRequiredToClear);
                    db.Update(task);
                    db.SaveChanges();
                }

                foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
                {
                    var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                    model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = task.ResponsibleUserID == user.Id ? true : false });
                    model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = task.ReportingToUserID == user.Id ? true : false });
                }

                model.D02_SaleTask = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    ID = task.ID,
                    KmTravelRequired = task.KmTravelRequired,
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    StatusID = task.StatusID,
                    MeetingAgendaID = task.MeetingAgendaID,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    D02_SaleTasks_TypeItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                        DefaultStatusID = tType.DefaultStatusID,
                        Identifier = tType.Identifier,
                        IsDeleted = tType.IsDeleted,
                        StatusGroupID = tType.StatusGroupID,
                        TaskClassificationID = tType.TaskClassificationID,
                        WorkflowGroupID = tType.WorkflowGroupID,
                        D02_SaleTask_TypeStatuses = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus>(),
                        BusinessDepartmentID = tType.BusinessDepartmentID,
                        DefautlMinPlanned = tType.DefautlMinPlanned,
                        ReportingToUserAccepted = tType.ReportingToUserAccepted,
                        ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                        ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                        ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                        ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                        DefaultMeetingAgendaID = tType.DefaultMeetingAgendaID,
                        MeetingAgendaGroupID = tType.MeetingAgendaGroupID,
                        D02_SaleTask_TypeMeetingAgendaes = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda>(),
                    },
                    ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    D02_SaleTask_Review_ReassignLogItems = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem>(),
                    D02_SaleTask_Review_AttachmentItems = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_AttachmentItem>(),
                    D02_SaleTaskStatusItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskStatus(),
                    D02_SaleTaskMeetingAgendaItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskMeetingAgenda(),
                    DueDate = task.DueDate,
                    Level = task.Level,
                    Module_TimeOfWorkPlanneds = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned>(),
                    Module_TimeOfWorkAllocateds = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated>(),
                    Module_TravelAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation>(),
                    Module_StockAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation>(),
                    Module_InvoiceAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation>(),
                    BusinessDepartmentName = task.BusinessDepartmentName,
                    BusinessPillarName = task.BusinessPillarName,
                    WorkflowGroupName = task.WorkflowGroupName,
                    WrikeCustomStatus = task.WrikeCustomStatus,
                    WrikeID = task.WrikeID,
                    WrikeSyncDate = task.WrikeSyncDate,
                };


                int workflowID = 1;
                if (tType.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == tType.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (tType.WorkflowGroupID.HasValue)
                    workflowID = tType.WorkflowGroupID.Value;

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    if (string.IsNullOrEmpty(model.D02_SaleTask.WorkflowGroupName))
                        model.D02_SaleTask.WorkflowGroupName = wf.WorkflowGroupName;
                    if (tType.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == tType.BusinessDepartmentID.Value).SingleOrDefault();
                        if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessDepartmentName))
                            model.D02_SaleTask.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessPillarName))
                            model.D02_SaleTask.BusinessPillarName = pillar.BusinessPillarName;
                    }
                    else if (wf.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessDepartmentName))
                            model.D02_SaleTask.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessPillarName))
                            model.D02_SaleTask.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                model.DueDate = task.DueDate.Value;

                var taskStatus = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (taskStatus != null)
                {
                    model.D02_SaleTask.D02_SaleTaskStatusItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskStatus()
                    {
                        CreatedByID = taskStatus.CreatedByID,
                        StatusActionID = taskStatus.StatusActionID,
                        ID = taskStatus.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == taskStatus.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = taskStatus.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == taskStatus.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = taskStatus.IsDeleted,
                        IsResolvedStatus = taskStatus.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == taskStatus.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = taskStatus.StatusGroupID,
                        StatusReportingID = taskStatus.StatusReportingID,
                        UpdatedByID = taskStatus.UpdatedByID,
                        UpdatedDate = taskStatus.UpdatedDate,
                    };
                }

                var taskMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == task.MeetingAgendaID).SingleOrDefault();
                if (task.MeetingAgendaID.HasValue && taskMeetingAgenda != null)
                {
                    model.D02_SaleTask.D02_SaleTaskMeetingAgendaItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskMeetingAgenda()
                    {
                        CreatedByID = taskMeetingAgenda.CreatedByID,
                        MeetingAgendaActionID = taskMeetingAgenda.MeetingAgendaActionID,
                        ID = taskMeetingAgenda.ID,
                        ActionName = siteAdmin_MeetingAgendaActions.Where(p => p.ID == taskMeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName,
                        CreatedDate = taskMeetingAgenda.CreatedDate,
                        GroupName = SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == taskMeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName,
                        IsDeleted = taskMeetingAgenda.IsDeleted,
                        IsResolvedMeetingAgenda = taskMeetingAgenda.IsResolvedMeetingAgenda,
                        ReportingName = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == taskMeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName,
                        MeetingAgendaGroupID = taskMeetingAgenda.MeetingAgendaGroupID,
                        MeetingAgendaReportingID = taskMeetingAgenda.MeetingAgendaReportingID,
                        UpdatedByID = taskMeetingAgenda.UpdatedByID,
                        UpdatedDate = taskMeetingAgenda.UpdatedDate,
                    };
                }

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    model.D02_SaleTask.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    model.D02_SaleTask.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                if (tType.StatusGroupID.HasValue)
                {
                    foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
                    {
                        D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus D02_SaleTask_TypeStatus = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus()
                        {
                            CreatedByID = status.CreatedByID,
                            StatusActionID = status.StatusActionID,
                            ID = status.ID,
                            ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                            CreatedDate = status.CreatedDate,
                            GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                            IsDeleted = status.IsDeleted,
                            IsResolvedStatus = status.IsResolvedStatus,
                            ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                            StatusGroupID = status.StatusGroupID,
                            StatusReportingID = status.StatusReportingID,
                            UpdatedByID = status.UpdatedByID,
                            UpdatedDate = status.UpdatedDate,
                        };
                        model.D02_SaleTask.D02_SaleTasks_TypeItem.D02_SaleTask_TypeStatuses.Add(D02_SaleTask_TypeStatus);
                    }
                }

                if (tType.MeetingAgendaGroupID.HasValue)
                {
                    foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes.Where(p => p.MeetingAgendaGroupID == tType.MeetingAgendaGroupID.Value))
                    {
                        D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda D02_SaleTask_TypeMeetingAgenda = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda()
                        {
                            CreatedByID = MeetingAgenda.CreatedByID,
                            MeetingAgendaActionID = MeetingAgenda.MeetingAgendaActionID,
                            ID = MeetingAgenda.ID,
                            ActionName = siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName,
                            CreatedDate = MeetingAgenda.CreatedDate,
                            GroupName = SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == MeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName,
                            IsDeleted = MeetingAgenda.IsDeleted,
                            IsResolvedMeetingAgenda = MeetingAgenda.IsResolvedMeetingAgenda,
                            ReportingName = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName,
                            MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                            MeetingAgendaReportingID = MeetingAgenda.MeetingAgendaReportingID,
                            UpdatedByID = MeetingAgenda.UpdatedByID,
                            UpdatedDate = MeetingAgenda.UpdatedDate,
                        };
                        model.D02_SaleTask.D02_SaleTasks_TypeItem.D02_SaleTask_TypeMeetingAgendaes.Add(D02_SaleTask_TypeMeetingAgenda);
                    }
                }

                var reassignLogs = db.D02_SaleTasks_ReassignLogs.Where(p => p.TaskID == task.ID).ToList();
                foreach (var rLog in reassignLogs)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem reassignLogItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem()
                    {
                        DateCreated = rLog.DateCreated,
                        ID = rLog.ID,
                        SystemDescription = rLog.SystemDescription,
                        TaskID = rLog.TaskID,
                        UserDescription = rLog.UserDescription,
                        UserID = rLog.UserID,
                        Username = "",
                    };

                    var reassignUserUser = opProfs.Where(p => p.UserID == rLog.UserID).SingleOrDefault();
                    if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                        reassignLogItem.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                    model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems.Add(reassignLogItem);
                }
                model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems = model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems.OrderByDescending(p => p.DateCreated).ToList();

                var attachments = db.D02_SaleTasks_Attachments.Where(p => p.TaskID == task.ID).ToList();
                foreach (var rLog in attachments)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_AttachmentItem attachmentItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_AttachmentItem()
                    {
                        DateCreated = rLog.DateCreated,
                        ID = rLog.ID,
                        TaskID = rLog.TaskID,
                        UserID = rLog.UserID,
                        Username = "",
                        AttachmentTypeID = rLog.AttachmentTypeID,
                        Description = rLog.Description,
                        Filename = rLog.Filename,
                        IsDeleted = rLog.IsDeleted,
                    };

                    var reassignUserUser = opProfs.Where(p => p.UserID == rLog.UserID).SingleOrDefault();
                    if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                        attachmentItem.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                    model.D02_SaleTask.D02_SaleTask_Review_AttachmentItems.Add(attachmentItem);
                }
                model.D02_SaleTask.D02_SaleTask_Review_AttachmentItems = model.D02_SaleTask.D02_SaleTask_Review_AttachmentItems.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkPlanneds = db.Module_TimeOfWorkPlanneds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkPlanned in module_TimeOfWorkPlanneds)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned()
                    {
                        ActivityID = timeOfWorkPlanned.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = timeOfWorkPlanned.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = timeOfWorkPlanned.CreatedByUserID,
                        ActivityTypeID = timeOfWorkPlanned.ActivityTypeID,
                        DateCreated = timeOfWorkPlanned.DateCreated,
                        DateOfWorkPlanned = timeOfWorkPlanned.DateOfWorkPlanned,
                        DescriptionOfWorkPlanned = timeOfWorkPlanned.DescriptionOfWorkPlanned,
                        ExternalChargeOutRatePerHour = timeOfWorkPlanned.ExternalChargeOutRatePerHour,
                        ID = timeOfWorkPlanned.ID,
                        InternalChargeOutRatePerHour = timeOfWorkPlanned.InternalChargeOutRatePerHour,
                        MinOfWorkPlanned = timeOfWorkPlanned.MinOfWorkPlanned,
                        IsDeleted = timeOfWorkPlanned.IsDeleted,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == timeOfWorkPlanned.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TimeOfWorkPlanned.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTimeOfWorkPlanned = opProfs.Where(p => p.UserID == timeOfWorkPlanned.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTimeOfWorkPlanned != null && !string.IsNullOrEmpty(responsibleUserTimeOfWorkPlanned.FirstName))
                        module_TimeOfWorkPlanned.ResponsibleUsername = $"{responsibleUserTimeOfWorkPlanned.FirstName} {responsibleUserTimeOfWorkPlanned.LastName}";

                    model.D02_SaleTask.Module_TimeOfWorkPlanneds.Add(module_TimeOfWorkPlanned);
                }
                model.D02_SaleTask.Module_TimeOfWorkPlanneds = model.D02_SaleTask.Module_TimeOfWorkPlanneds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated()
                    {
                        ActivityID = timeOfWorkAllocated.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = timeOfWorkAllocated.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = timeOfWorkAllocated.CreatedByUserID,
                        ActivityTypeID = timeOfWorkAllocated.ActivityTypeID,
                        DateCreated = timeOfWorkAllocated.DateCreated,
                        DescriptionOfWorkAllocated = timeOfWorkAllocated.DescriptionOfWorkAllocated,
                        ExternalChargeOutRatePerHour = timeOfWorkAllocated.ExternalChargeOutRatePerHour,
                        ID = timeOfWorkAllocated.ID,
                        InternalChargeOutRatePerHour = timeOfWorkAllocated.InternalChargeOutRatePerHour,
                        IsDeleted = timeOfWorkAllocated.IsDeleted,
                        StartTime = timeOfWorkAllocated.StartTime,
                        EndTime = timeOfWorkAllocated.EndTime,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == timeOfWorkAllocated.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TimeOfWorkAllocated.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTimeOfWorkAllocated = opProfs.Where(p => p.UserID == timeOfWorkAllocated.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTimeOfWorkAllocated != null && !string.IsNullOrEmpty(responsibleUserTimeOfWorkAllocated.FirstName))
                        module_TimeOfWorkAllocated.ResponsibleUsername = $"{responsibleUserTimeOfWorkAllocated.FirstName} {responsibleUserTimeOfWorkAllocated.LastName}";

                    model.D02_SaleTask.Module_TimeOfWorkAllocateds.Add(module_TimeOfWorkAllocated);
                }
                model.D02_SaleTask.Module_TimeOfWorkAllocateds = model.D02_SaleTask.Module_TimeOfWorkAllocateds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TravelAllocations = db.Module_TravelAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var TravelAllocation in module_TravelAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation module_TravelAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation()
                    {
                        ActivityID = TravelAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = TravelAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = TravelAllocation.CreatedByUserID,
                        ActivityTypeID = TravelAllocation.ActivityTypeID,
                        DateCreated = TravelAllocation.DateCreated,
                        ID = TravelAllocation.ID,
                        IsDeleted = TravelAllocation.IsDeleted,
                        DateOfTravelAllocation = TravelAllocation.DateOfTravelAllocation,
                        PhotoURL = TravelAllocation.PhotoURL,
                        VehicleOdoEnd = TravelAllocation.VehicleOdoEnd,
                        VehicleOdoStart = TravelAllocation.VehicleOdoStart,
                        VehicleTypeID = TravelAllocation.VehicleTypeID,
                        VehicleID = TravelAllocation.VehicleID,
                        DescriptionOfTravelAllocation = TravelAllocation.DescriptionOfTravelAllocation,
                        RatePerKm = TravelAllocation.RatePerKm,
                        VehicleDistanceKm = TravelAllocation.VehicleDistanceKm,
                        Vehicle = vehicles.Where(p => p.ID == TravelAllocation.VehicleID).SingleOrDefault(),
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == TravelAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TravelAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTravelAllocation = opProfs.Where(p => p.UserID == TravelAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTravelAllocation != null && !string.IsNullOrEmpty(responsibleUserTravelAllocation.FirstName))
                        module_TravelAllocation.ResponsibleUsername = $"{responsibleUserTravelAllocation.FirstName} {responsibleUserTravelAllocation.LastName}";

                    model.D02_SaleTask.Module_TravelAllocations.Add(module_TravelAllocation);
                }
                model.D02_SaleTask.Module_TravelAllocations = model.D02_SaleTask.Module_TravelAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_StockAllocations = db.Module_StockAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var StockAllocation in module_StockAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation module_StockAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation()
                    {
                        ActivityID = StockAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = StockAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = StockAllocation.CreatedByUserID,
                        ActivityTypeID = StockAllocation.ActivityTypeID,
                        DateCreated = StockAllocation.DateCreated,
                        ID = StockAllocation.ID,
                        IsDeleted = StockAllocation.IsDeleted,
                        DateOfStockAllocation = StockAllocation.DateOfStockAllocation,
                        DescriptionOfStockAllocation = StockAllocation.DescriptionOfStockAllocation,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == StockAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_StockAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserStockAllocation = opProfs.Where(p => p.UserID == StockAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserStockAllocation != null && !string.IsNullOrEmpty(responsibleUserStockAllocation.FirstName))
                        module_StockAllocation.ResponsibleUsername = $"{responsibleUserStockAllocation.FirstName} {responsibleUserStockAllocation.LastName}";

                    model.D02_SaleTask.Module_StockAllocations.Add(module_StockAllocation);
                }
                model.D02_SaleTask.Module_StockAllocations = model.D02_SaleTask.Module_StockAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_InvoiceAllocations = db.Module_InvoiceAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var InvoiceAllocation in module_InvoiceAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation module_InvoiceAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation()
                    {
                        ActivityID = InvoiceAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = InvoiceAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = InvoiceAllocation.CreatedByUserID,
                        ActivityTypeID = InvoiceAllocation.ActivityTypeID,
                        DateCreated = InvoiceAllocation.DateCreated,
                        ID = InvoiceAllocation.ID,
                        IsDeleted = InvoiceAllocation.IsDeleted,
                        DateOfInvoiceAllocation = InvoiceAllocation.DateOfInvoiceAllocation,
                        DescriptionOfInvoiceAllocation = InvoiceAllocation.DescriptionOfInvoiceAllocation,
                        CustomerNo = InvoiceAllocation.CustomerNo,
                        InvoiceAmount = InvoiceAllocation.InvoiceAmount,
                        InvoiceNo = InvoiceAllocation.InvoiceNo,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == InvoiceAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_InvoiceAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserInvoiceAllocation = opProfs.Where(p => p.UserID == InvoiceAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserInvoiceAllocation != null && !string.IsNullOrEmpty(responsibleUserInvoiceAllocation.FirstName))
                        module_InvoiceAllocation.ResponsibleUsername = $"{responsibleUserInvoiceAllocation.FirstName} {responsibleUserInvoiceAllocation.LastName}";

                    model.D02_SaleTask.Module_InvoiceAllocations.Add(module_InvoiceAllocation);
                }
                model.D02_SaleTask.Module_InvoiceAllocations = model.D02_SaleTask.Module_InvoiceAllocations.OrderByDescending(p => p.DateCreated).ToList();

            }


            return View("~/Views/Operational/D02_Sale/D02_SaleTask_Review.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D02_Sale/D02_SaleTask_Review")]
        public async Task<IActionResult> D02_SaleTask_Review(D02_SaleTask_ReviewModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion


            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var priorities = db.SiteAdmin_Priorities.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.ToList();
            var SiteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();
            siteAdmin_MeetingAgendaes = siteAdmin_MeetingAgendaes.OrderBy(p => p.MeetingAgendaGroupID).ThenBy(p => p.MeetingAgendaActionID).ToList();
            var vehicles = db.Vehicles.ToList();

            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();
            //model.Status = new List<SelectListItem>();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = db.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"] == user.Id ? true : false });
            }

            //model.Status = (from p in ((D02_SaleTask.StatusEnum[])Enum.GetValues(typeof(D02_SaleTask.StatusEnum)))
            //                select new SelectListItem()
            //                {
            //                    Text = p.GetDescription(),
            //                    Value = ((int)p).ToString(),
            //                    Selected = Request.Form["Status"] == ((int)p).ToString() ? true : false,
            //                }).ToList();

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();
            var tType = db.D02_SaleTask_Types.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

            if (task != null)
            {
                #region Update Model

                model.D02_SaleTask = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    ID = task.ID,
                    KmTravelRequired = task.KmTravelRequired,
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    StatusID = task.StatusID,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    D02_SaleTasks_TypeItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                        D02_SaleTask_TypeStatuses = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus>(),
                        D02_SaleTask_TypeMeetingAgendaes = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda>(),
                        BusinessDepartmentID = tType.BusinessDepartmentID,
                        DefaultMeetingAgendaID = tType.DefaultMeetingAgendaID,
                        DefaultStatusID = tType.DefaultStatusID,
                        DefautlMinPlanned = tType.DefautlMinPlanned,
                        Identifier = tType.Identifier,
                        IsDeleted = tType.IsDeleted,
                        MeetingAgendaGroupID = tType.MeetingAgendaGroupID,
                        ReportingToUserAccepted = tType.ReportingToUserAccepted,
                        ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                        ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                        ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                        ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                        StatusGroupID = tType.StatusGroupID,
                        TaskClassificationID = tType.TaskClassificationID,
                        WorkflowGroupID = tType.WorkflowGroupID,
                    },
                    ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    D02_SaleTask_Review_ReassignLogItems = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem>(),
                    D02_SaleTaskStatusItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskStatus(),
                    DueDate = task.DueDate,
                    D02_SaleTask_Review_AttachmentItems = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_AttachmentItem>(),
                    Level = task.Level,
                    Module_TimeOfWorkAllocateds = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated>(),
                    Module_TimeOfWorkPlanneds = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned>(),
                    Module_TravelAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation>(),
                    Module_StockAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation>(),
                    Module_InvoiceAllocations = new List<D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation>(),
                    BusinessDepartmentName = task.BusinessDepartmentName,
                    BusinessPillarName = task.BusinessPillarName,
                    WorkflowGroupName = task.WorkflowGroupName,
                    WrikeCustomStatus = task.WrikeCustomStatus,
                    WrikeID = task.WrikeID,
                    WrikeSyncDate = task.WrikeSyncDate,
                    MeetingAgendaID = task.MeetingAgendaID,
                    D02_SaleTaskMeetingAgendaItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTaskMeetingAgenda(),
                };

                if (tType.BusinessDepartmentID.HasValue)
                {
                    var department = db.BusinessDepartments.Where(p => p.ID == tType.BusinessDepartmentID.Value).SingleOrDefault();
                    var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessDepartmentName))
                        model.D02_SaleTask.BusinessDepartmentName = department.BusinessDepartmentName;
                    if (string.IsNullOrEmpty(model.D02_SaleTask.BusinessPillarName))
                        model.D02_SaleTask.BusinessPillarName = pillar.BusinessPillarName;

                    var workflow = db.WorkflowGroups.Where(p => p.BusinessDepartmentID == department.ID).FirstOrDefault();
                    if (workflow != null)
                    {
                        if (string.IsNullOrEmpty(model.D02_SaleTask.WorkflowGroupName))
                            model.D02_SaleTask.WorkflowGroupName = workflow.WorkflowGroupName;
                    }
                }

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    model.D02_SaleTask.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    model.D02_SaleTask.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                if (tType.StatusGroupID.HasValue)
                {
                    foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
                    {
                        D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus D02_SaleTask_TypeStatus = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeStatus()
                        {
                            CreatedByID = status.CreatedByID,
                            StatusActionID = status.StatusActionID,
                            ID = status.ID,
                            ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                            CreatedDate = status.CreatedDate,
                            GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                            IsDeleted = status.IsDeleted,
                            IsResolvedStatus = status.IsResolvedStatus,
                            ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                            StatusGroupID = status.StatusGroupID,
                            StatusReportingID = status.StatusReportingID,
                            UpdatedByID = status.UpdatedByID,
                            UpdatedDate = status.UpdatedDate,
                        };
                        model.D02_SaleTask.D02_SaleTasks_TypeItem.D02_SaleTask_TypeStatuses.Add(D02_SaleTask_TypeStatus);
                    }
                }

                if (tType.MeetingAgendaGroupID.HasValue)
                {
                    foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes.Where(p => p.MeetingAgendaGroupID == tType.MeetingAgendaGroupID.Value))
                    {
                        D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda D02_SaleTask_TypeMeetingAgenda = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Type.D02_SaleTask_TypeMeetingAgenda()
                        {
                            CreatedByID = MeetingAgenda.CreatedByID,
                            MeetingAgendaActionID = MeetingAgenda.MeetingAgendaActionID,
                            ID = MeetingAgenda.ID,
                            ActionName = siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName,
                            CreatedDate = MeetingAgenda.CreatedDate,
                            GroupName = SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == MeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName,
                            IsDeleted = MeetingAgenda.IsDeleted,
                            IsResolvedMeetingAgenda = MeetingAgenda.IsResolvedMeetingAgenda,
                            ReportingName = siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName,
                            MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                            MeetingAgendaReportingID = MeetingAgenda.MeetingAgendaReportingID,
                            UpdatedByID = MeetingAgenda.UpdatedByID,
                            UpdatedDate = MeetingAgenda.UpdatedDate,
                        };
                        model.D02_SaleTask.D02_SaleTasks_TypeItem.D02_SaleTask_TypeMeetingAgendaes.Add(D02_SaleTask_TypeMeetingAgenda);
                    }
                }

                var reassignLogs = db.D02_SaleTasks_ReassignLogs.Where(p => p.TaskID == task.ID).ToList();
                foreach (var rLog in reassignLogs)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem reassignLogItem = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.D02_SaleTask_Review_ReassignLogItem()
                    {
                        DateCreated = rLog.DateCreated,
                        ID = rLog.ID,
                        SystemDescription = rLog.SystemDescription,
                        TaskID = rLog.TaskID,
                        UserDescription = rLog.UserDescription,
                        UserID = rLog.UserID,
                        Username = "",
                    };

                    var reassignUserUser = opProfs.Where(p => p.UserID == rLog.UserID).SingleOrDefault();
                    if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                        reassignLogItem.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                    model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems.Add(reassignLogItem);
                }
                model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems = model.D02_SaleTask.D02_SaleTask_Review_ReassignLogItems.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkPlanneds = db.Module_TimeOfWorkPlanneds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkPlanned in module_TimeOfWorkPlanneds)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkPlanned()
                    {
                        ActivityID = timeOfWorkPlanned.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = timeOfWorkPlanned.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = timeOfWorkPlanned.CreatedByUserID,
                        ActivityTypeID = timeOfWorkPlanned.ActivityTypeID,
                        DateCreated = timeOfWorkPlanned.DateCreated,
                        DateOfWorkPlanned = timeOfWorkPlanned.DateOfWorkPlanned,
                        DescriptionOfWorkPlanned = timeOfWorkPlanned.DescriptionOfWorkPlanned,
                        ExternalChargeOutRatePerHour = timeOfWorkPlanned.ExternalChargeOutRatePerHour,
                        ID = timeOfWorkPlanned.ID,
                        InternalChargeOutRatePerHour = timeOfWorkPlanned.InternalChargeOutRatePerHour,
                        MinOfWorkPlanned = timeOfWorkPlanned.MinOfWorkPlanned,
                        IsDeleted = timeOfWorkPlanned.IsDeleted,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == timeOfWorkPlanned.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TimeOfWorkPlanned.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTimeOfWorkPlanned = opProfs.Where(p => p.UserID == timeOfWorkPlanned.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTimeOfWorkPlanned != null && !string.IsNullOrEmpty(responsibleUserTimeOfWorkPlanned.FirstName))
                        module_TimeOfWorkPlanned.ResponsibleUsername = $"{responsibleUserTimeOfWorkPlanned.FirstName} {responsibleUserTimeOfWorkPlanned.LastName}";

                    model.D02_SaleTask.Module_TimeOfWorkPlanneds.Add(module_TimeOfWorkPlanned);
                }
                model.D02_SaleTask.Module_TimeOfWorkPlanneds = model.D02_SaleTask.Module_TimeOfWorkPlanneds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TimeOfWorkAllocated()
                    {
                        ActivityID = timeOfWorkAllocated.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = timeOfWorkAllocated.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = timeOfWorkAllocated.CreatedByUserID,
                        ActivityTypeID = timeOfWorkAllocated.ActivityTypeID,
                        DateCreated = timeOfWorkAllocated.DateCreated,
                        DescriptionOfWorkAllocated = timeOfWorkAllocated.DescriptionOfWorkAllocated,
                        ExternalChargeOutRatePerHour = timeOfWorkAllocated.ExternalChargeOutRatePerHour,
                        ID = timeOfWorkAllocated.ID,
                        InternalChargeOutRatePerHour = timeOfWorkAllocated.InternalChargeOutRatePerHour,
                        IsDeleted = timeOfWorkAllocated.IsDeleted,
                        StartTime = timeOfWorkAllocated.StartTime,
                        EndTime = timeOfWorkAllocated.EndTime,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == timeOfWorkAllocated.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TimeOfWorkAllocated.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTimeOfWorkAllocated = opProfs.Where(p => p.UserID == timeOfWorkAllocated.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTimeOfWorkAllocated != null && !string.IsNullOrEmpty(responsibleUserTimeOfWorkAllocated.FirstName))
                        module_TimeOfWorkAllocated.ResponsibleUsername = $"{responsibleUserTimeOfWorkAllocated.FirstName} {responsibleUserTimeOfWorkAllocated.LastName}";

                    model.D02_SaleTask.Module_TimeOfWorkAllocateds.Add(module_TimeOfWorkAllocated);
                }
                model.D02_SaleTask.Module_TimeOfWorkAllocateds = model.D02_SaleTask.Module_TimeOfWorkAllocateds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TravelAllocations = db.Module_TravelAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var TravelAllocation in module_TravelAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation module_TravelAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_TravelAllocation()
                    {
                        ActivityID = TravelAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = TravelAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = TravelAllocation.CreatedByUserID,
                        ActivityTypeID = TravelAllocation.ActivityTypeID,
                        DateCreated = TravelAllocation.DateCreated,
                        ID = TravelAllocation.ID,
                        IsDeleted = TravelAllocation.IsDeleted,
                        DateOfTravelAllocation = TravelAllocation.DateOfTravelAllocation,
                        PhotoURL = TravelAllocation.PhotoURL,
                        VehicleOdoEnd = TravelAllocation.VehicleOdoEnd,
                        VehicleOdoStart = TravelAllocation.VehicleOdoStart,
                        VehicleTypeID = TravelAllocation.VehicleTypeID,
                        VehicleID = TravelAllocation.VehicleID,
                        DescriptionOfTravelAllocation = TravelAllocation.DescriptionOfTravelAllocation,
                        RatePerKm = TravelAllocation.RatePerKm,
                        VehicleDistanceKm = TravelAllocation.VehicleDistanceKm,
                        Vehicle = vehicles.Where(p => p.ID == TravelAllocation.VehicleID).SingleOrDefault(),
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == TravelAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_TravelAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserTravelAllocation = opProfs.Where(p => p.UserID == TravelAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserTravelAllocation != null && !string.IsNullOrEmpty(responsibleUserTravelAllocation.FirstName))
                        module_TravelAllocation.ResponsibleUsername = $"{responsibleUserTravelAllocation.FirstName} {responsibleUserTravelAllocation.LastName}";

                    model.D02_SaleTask.Module_TravelAllocations.Add(module_TravelAllocation);
                }
                model.D02_SaleTask.Module_TravelAllocations = model.D02_SaleTask.Module_TravelAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_StockAllocations = db.Module_StockAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var StockAllocation in module_StockAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation module_StockAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_StockAllocation()
                    {
                        ActivityID = StockAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = StockAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = StockAllocation.CreatedByUserID,
                        ActivityTypeID = StockAllocation.ActivityTypeID,
                        DateCreated = StockAllocation.DateCreated,
                        ID = StockAllocation.ID,
                        IsDeleted = StockAllocation.IsDeleted,
                        DateOfStockAllocation = StockAllocation.DateOfStockAllocation,
                        DescriptionOfStockAllocation = StockAllocation.DescriptionOfStockAllocation,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == StockAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_StockAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserStockAllocation = opProfs.Where(p => p.UserID == StockAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserStockAllocation != null && !string.IsNullOrEmpty(responsibleUserStockAllocation.FirstName))
                        module_StockAllocation.ResponsibleUsername = $"{responsibleUserStockAllocation.FirstName} {responsibleUserStockAllocation.LastName}";

                    model.D02_SaleTask.Module_StockAllocations.Add(module_StockAllocation);
                }
                model.D02_SaleTask.Module_StockAllocations = model.D02_SaleTask.Module_StockAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_InvoiceAllocations = db.Module_InvoiceAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.D02_SaleTask && p.ActivityID == task.ID).ToList();
                foreach (var InvoiceAllocation in module_InvoiceAllocations)
                {
                    D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation module_InvoiceAllocation = new D02_SaleTask_ReviewModel.D02_SaleTask_ReviewItem.Module_InvoiceAllocation()
                    {
                        ActivityID = InvoiceAllocation.ActivityID,
                        ResponsibleUsername = "",
                        ResponsibleUserID = InvoiceAllocation.ResponsibleUserID,
                        CreatedByUsername = "",
                        CreatedByUserID = InvoiceAllocation.CreatedByUserID,
                        ActivityTypeID = InvoiceAllocation.ActivityTypeID,
                        DateCreated = InvoiceAllocation.DateCreated,
                        ID = InvoiceAllocation.ID,
                        IsDeleted = InvoiceAllocation.IsDeleted,
                        DateOfInvoiceAllocation = InvoiceAllocation.DateOfInvoiceAllocation,
                        DescriptionOfInvoiceAllocation = InvoiceAllocation.DescriptionOfInvoiceAllocation,
                        CustomerNo = InvoiceAllocation.CustomerNo,
                        InvoiceAmount = InvoiceAllocation.InvoiceAmount,
                        InvoiceNo = InvoiceAllocation.InvoiceNo,
                    };

                    var createdByUser = opProfs.Where(p => p.UserID == InvoiceAllocation.CreatedByUserID).SingleOrDefault();
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                        module_InvoiceAllocation.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                    var responsibleUserInvoiceAllocation = opProfs.Where(p => p.UserID == InvoiceAllocation.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUserInvoiceAllocation != null && !string.IsNullOrEmpty(responsibleUserInvoiceAllocation.FirstName))
                        module_InvoiceAllocation.ResponsibleUsername = $"{responsibleUserInvoiceAllocation.FirstName} {responsibleUserInvoiceAllocation.LastName}";

                    model.D02_SaleTask.Module_InvoiceAllocations.Add(module_InvoiceAllocation);
                }
                model.D02_SaleTask.Module_InvoiceAllocations = model.D02_SaleTask.Module_InvoiceAllocations.OrderByDescending(p => p.DateCreated).ToList();

                #endregion

                #region Update Task

                if (ModelState.IsValid)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.D02_SaleTasks.Where(p => p.ID == task.ID).SingleOrDefault();

                    if (!string.IsNullOrEmpty(Request.Form["ResponsibleUser"]) && Request.Form["ResponsibleUser"] != task.ResponsibleUserID)
                    {
                        var responsibleUserUpdate = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();
                        sbSysLog.AppendLine($"ResponsibleUser from {model.D02_SaleTask.ResponsibleUserUsername} to {responsibleUserUpdate.FirstName} {responsibleUserUpdate.LastName}<br />");
                        itemToUpdate.ResponsibleUserID = Request.Form["ResponsibleUser"];
                    }

                    //if (!string.IsNullOrEmpty(Request.Form["Status"]) && Convert.ToInt32(Request.Form["Status"]) != task.StatusID)
                    //{
                    //    sbSysLog.AppendLine($"Status from {model.D02_SaleTask.Status.GetDescription()} to {((D02_SaleTask.StatusEnum)Convert.ToInt32(Request.Form["Status"])).GetDescription()}<br />");
                    //    itemToUpdate.StatusID = Convert.ToInt32(Request.Form["Status"]);
                    //}

                    if (!string.IsNullOrEmpty(Request.Form["ReportingToUser"]) && Request.Form["ReportingToUser"] != task.ReportingToUserID)
                    {
                        var reportingToUserUserUpdate = opProfs.Where(p => p.UserID == Request.Form["ReportingToUser"]).SingleOrDefault();
                        sbSysLog.AppendLine($"ReportingToUser from {model.D02_SaleTask.ReportingToUserUsername} to {reportingToUserUserUpdate.FirstName} {reportingToUserUserUpdate.LastName}<br />");
                        itemToUpdate.ReportingToUserID = Request.Form["ReportingToUser"];
                    }

                    if (model.DueDate != null && model.DueDate != task.DueDate.Value)
                    {
                        sbSysLog.AppendLine($"DueDate from {task.DueDate} to {model.DueDate}<br />");
                        itemToUpdate.DueDate = model.DueDate;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.D02_SaleTasks_ReassignLog D02_SaleTasks_ReassignLog = new D02_SaleTasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = model.Comments,
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(D02_SaleTasks_ReassignLog);
                    db.SaveChanges();

                    return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
                }

                #endregion
            }


            return View("~/Views/Operational/D02_Sale/D02_SaleTask_Review.cshtml", model);
        }


        //[HttpGet]
        //[Route("/operational/D02_SaleTasks/D02_SaleTask_Create")]
        //public async Task<IActionResult> D02_SaleTask_Create()
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Create, SecureAreaActionEnum.View))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Create}/{(int)SecureAreaActionEnum.View}");

        //    #endregion

        //    var db = new MyVoltageDbContext(_options);
        //    var userSecureAreas = db.UserSecureAreaActions.ToList();
        //    var taskTypes = db.D02_SaleTask_Types.ToList();
        //    var priorities = db.SiteAdmin_Priorities.ToList();
        //    var opProfs = db.OperationalProfiles.ToList();
        //    var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
        //    var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
        //    var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
        //    var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
        //    var companies = (from p in db.Companies
        //                     select new { p.CompanyID, p.Name }).ToList();

        //    siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();


        //    D02_SaleTask_CreateModel model = new D02_SaleTask_CreateModel()
        //    {
        //        ReportingToUser = new List<SelectListItem>(),
        //        ResponsibleUser = new List<SelectListItem>(),
        //        TaskType = (from p in taskTypes
        //                    orderby p.Heading
        //                    select new SelectListItem()
        //                    {
        //                        Text = p.Heading,
        //                        Value = p.ID.ToString(),
        //                    }).ToList(),
        //        Status = new List<SelectListItem>()
        //        {
        //            new SelectListItem() { Value = "", Text = "[Select Task Type First]" },
        //        },
        //        StatusItems = new List<D02_SaleTask_CreateModel.StatusItem>(),
        //        Company = new List<SelectListItem>(),
        //        DueDate = DateTime.Now,
        //    };

        //    foreach (var uC in _operationalProvider.UserCompaniesWithNone)
        //    {
        //        model.Company.Add(new SelectListItem()
        //        {
        //            Value = uC.CompanyID.ToString(),
        //            Text = uC.CompanyID == 0 ? "[Not Linked]" : companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault().Name,
        //        });
        //    }

        //    foreach (var tType in taskTypes.Where(p => p.StatusGroupID.HasValue))
        //    {
        //        foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
        //        {
        //            D02_SaleTask_CreateModel.StatusItem statusItem = new D02_SaleTask_CreateModel.StatusItem()
        //            {
        //                DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
        //                ID = status.ID,
        //                TaskTypeID = tType.ID,
        //            };

        //            model.StatusItems.Add(statusItem);
        //        }
        //    }

        //    var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

        //    foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
        //    {
        //        if ((from p in userSecureAreas
        //             where p.UserID == user.Id
        //             && p.SecureAreaID == (int)SecureAreaEnum.D02_SaleTask_Review
        //             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
        //             select p).Count() == 0)
        //            continue;
        //        var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
        //        model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
        //        model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
        //    }
        //    model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
        //    model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

        //    return View("~/Views/Operational/D02_SaleTasks/D02_SaleTask_Create.cshtml", model);
        //}

        //[HttpPost]
        //[Route("/operational/D02_SaleTasks/D02_SaleTask_Create")]
        //public async Task<IActionResult> D02_SaleTask_Create(D02_SaleTask_CreateModel model)
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Create, SecureAreaActionEnum.View))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Create}/{(int)SecureAreaActionEnum.View}");

        //    #endregion

        //    var db = new MyVoltageDbContext(_options);
        //    var userSecureAreas = db.UserSecureAreaActions.ToList();
        //    var taskTypes = db.D02_SaleTask_Types.ToList();
        //    var priorities = db.SiteAdmin_Priorities.ToList();
        //    var opProfs = db.OperationalProfiles.ToList();
        //    var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
        //    var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
        //    var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
        //    var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
        //    var companies = (from p in db.Companies
        //                     select new { p.CompanyID, p.Name }).ToList();

        //    siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();


        //    model.ReportingToUser = new List<SelectListItem>();
        //    model.ResponsibleUser = new List<SelectListItem>();
        //    model.TaskType = (from p in taskTypes
        //                      orderby p.Heading
        //                      select new SelectListItem()
        //                      {
        //                          Text = p.Heading,
        //                          Value = p.ID.ToString(),
        //                          Selected = p.ID.ToString() == Request.Form["TaskType"]
        //                      }).ToList();
        //    model.Status = new List<SelectListItem>()
        //        {
        //            new SelectListItem() { Value = "", Text = "[Select Task Type First]" },
        //        };

        //    model.StatusItems = new List<D02_SaleTask_CreateModel.StatusItem>();
        //    model.Company = new List<SelectListItem>();

        //    foreach (var uC in _operationalProvider.UserCompaniesWithNone)
        //    {
        //        model.Company.Add(new SelectListItem()
        //        {
        //            Value = uC.CompanyID.ToString(),
        //            Text = uC.CompanyID == 0 ? "[Not Linked]" : companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault().Name,
        //            Selected = Request.Form["Company"] == uC.CompanyID.ToString(),
        //        });
        //    }

        //    foreach (var tType in taskTypes.Where(p => p.StatusGroupID.HasValue))
        //    {
        //        foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
        //        {
        //            D02_SaleTask_CreateModel.StatusItem statusItem = new D02_SaleTask_CreateModel.StatusItem()
        //            {
        //                DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
        //                ID = status.ID,
        //                TaskTypeID = tType.ID,
        //            };

        //            model.StatusItems.Add(statusItem);
        //        }
        //    }

        //    var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

        //    foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
        //    {
        //        if ((from p in userSecureAreas
        //             where p.UserID == user.Id
        //             && p.SecureAreaID == (int)SecureAreaEnum.D02_SaleTask_Review
        //             && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
        //             select p).Count() == 0)
        //            continue;
        //        var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
        //        model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id });
        //        model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"] == user.Id });
        //    }
        //    model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
        //    model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

        //    if (ModelState.IsValid)
        //    {
        //        Data.D02_SaleTask task = new D02_SaleTask()
        //        {
        //            CompanyID = null,
        //            DateCreated = DateTime.Now,
        //            DateEnded = null,
        //            DateStarted = null,
        //            DueDate = model.DueDate,
        //            KmTravelRequired = null,
        //            ReportingToUserID = Request.Form["ReportingToUser"],
        //            ResponsibleUserID = Request.Form["ResponsibleUser"],
        //            StatusID = Convert.ToInt32(Request.Form["Status"]),
        //            StockUsed = "",
        //            TaskTypeID = Convert.ToInt32(Request.Form["TaskType"]),
        //            Level = null,
        //        };

        //        if (!string.IsNullOrEmpty(Request.Form["Company"]))
        //            task.CompanyID = Convert.ToInt32(Request.Form["Company"]);

        //        db.Add(task);
        //        db.SaveChanges();

        //        Data.D02_SaleTasks_ReassignLog D02_SaleTasks_ReassignLog = new D02_SaleTasks_ReassignLog()
        //        {
        //            DateCreated = DateTime.Now,
        //            SystemDescription = "Task Created",
        //            TaskID = task.ID,
        //            UserDescription = model.Comments,
        //            UserID = _userManager.GetUserId(User),
        //        };

        //        db.Add(D02_SaleTasks_ReassignLog);
        //        db.SaveChanges();

        //        _cache.Remove(MVCache.KEY_D02_SaleTasks);
        //        _cache.Remove(MVCache.KEY_D02_SaleTasks_ReassignLog);
        //        _cache.Remove(MVCache.KEY_D02_SaleTasks_Attachments);

        //        return Redirect($"/operational/D02_SaleTasks/D02_SaleTask_Review/{task.TaskTypeID}/{task.ID}");

        //    }

        //    return View("~/Views/Operational/D02_SaleTasks/D02_SaleTask_Create.cshtml", model);
        //}

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_ReviewStatusChange/{statusID}")]
        public async Task<IActionResult> D02_SaleTask_ReviewStatusChange(int statusID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                #region Update Task

                if (statusID != task.StatusID)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.D02_SaleTasks.Where(p => p.ID == task.ID).SingleOrDefault();

                    var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
                    var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
                    var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
                    var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
                    var beforeStatus = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                    string beforeStatusString = beforeStatus != null ? $"{SiteAdmin_StatusGroups.Where(p => p.ID == beforeStatus.StatusGroupID).SingleOrDefault().StatusGroupName} - {siteAdmin_StatusActions.Where(p => p.ID == beforeStatus.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == beforeStatus.StatusReportingID).SingleOrDefault().StatusReportingName}" : "Unknown";

                    var afterStatus = siteAdmin_Statuses.Where(p => p.ID == statusID).SingleOrDefault();
                    string afterStatusString = afterStatus != null ? $"{SiteAdmin_StatusGroups.Where(p => p.ID == afterStatus.StatusGroupID).SingleOrDefault().StatusGroupName} - {siteAdmin_StatusActions.Where(p => p.ID == afterStatus.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == afterStatus.StatusReportingID).SingleOrDefault().StatusReportingName}" : "Unknown";

                    sbSysLog.AppendLine($"Status from '{beforeStatusString}' to '{afterStatusString}'");
                    itemToUpdate.StatusID = statusID;

                    if (afterStatus != null && afterStatus.IsResolvedStatus.HasValue && afterStatus.IsResolvedStatus.Value)
                    {
                        itemToUpdate.DateEnded = DateTime.Now;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.D02_SaleTasks_ReassignLog D02_SaleTasks_ReassignLog = new D02_SaleTasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(D02_SaleTasks_ReassignLog);
                    db.SaveChanges();
                }

                #endregion

                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
            }

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_ReviewMeetingAgendaChange/{MeetingAgendaID}")]
        public async Task<IActionResult> D02_SaleTask_ReviewMeetingAgendaChange(int MeetingAgendaID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                #region Update Task

                if (MeetingAgendaID != task.MeetingAgendaID)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.D02_SaleTasks.Where(p => p.ID == task.ID).SingleOrDefault();

                    var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.ToList();
                    var SiteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.ToList();
                    var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
                    var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();
                    var beforeMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == task.MeetingAgendaID).SingleOrDefault();
                    string beforeMeetingAgendaString = beforeMeetingAgenda != null ? $"{SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName} - {siteAdmin_MeetingAgendaActions.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}" : "Unknown";

                    var afterMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == MeetingAgendaID).SingleOrDefault();
                    string afterMeetingAgendaString = afterMeetingAgenda != null ? $"{SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName} - {siteAdmin_MeetingAgendaActions.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}" : "Unknown";

                    sbSysLog.AppendLine($"MeetingAgenda from '{beforeMeetingAgendaString}' to '{afterMeetingAgendaString}'");
                    itemToUpdate.MeetingAgendaID = MeetingAgendaID;

                    if (afterMeetingAgenda != null && afterMeetingAgenda.IsResolvedMeetingAgenda.HasValue && afterMeetingAgenda.IsResolvedMeetingAgenda.Value)
                    {
                        itemToUpdate.DateEnded = DateTime.Now;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.D02_SaleTasks_ReassignLog D02_SaleTasks_ReassignLog = new D02_SaleTasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(D02_SaleTasks_ReassignLog);
                    db.SaveChanges();
                }

                #endregion

                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
            }

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTaskTypeHowToDocument/{ID}")]
        public async Task<IActionResult> D02_SaleTaskTypeHowToDocument(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.D02_SaleTask_Types.Where(p => p.ID == ID).SingleOrDefault();
            string contentType = "text/plain";
            if (item != null)
            {
                // Get a reference to the file
                string shareName = "d02-saletasktypes";
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryTemp = share.GetDirectoryClient(item.ID.ToString());
                if (directoryTemp.Exists())
                {
                    ShareFileClient file = directoryTemp.GetFileClient(System.IO.Path.GetFileName(item.HowToURL).ToLower());

                    if (file.Exists())
                    {
                        // Download the file
                        ShareFileDownloadInfo download = file.Download();
                        Stream uploadFile = new MemoryStream();
                        download.Content.CopyTo(uploadFile);
                        uploadFile.Position = 0;
                        FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                        if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.HowToURL), out contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        if (uploadFile != null)
                            return File(uploadFile, contentType, System.IO.Path.GetFileName(item.HowToURL));
                    }
                }

            }
            return Content("The file you are looking for could not be found.");
        }


        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Review_AddAttachment")]
        public async Task<IActionResult> D02_SaleTask_Review_AddAttachment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            D02_SaleTask_Review_AddAttachmentModel model = new D02_SaleTask_Review_AddAttachmentModel()
            {

            };
            model.AttachmentType = (from p in ((D02_SaleTasks_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D02_SaleTasks_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                    }).ToList();

            return View("~/Views/Operational/D02_Sale/D02_SaleTask_Review_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D02_Sale/D02_SaleTask_Review_AddAttachment")]
        public async Task<IActionResult> D02_SaleTask_Review_AddAttachment(D02_SaleTask_Review_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.D02SelectedTaskTypeID == 0 || _operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            model.AttachmentType = (from p in ((D02_SaleTasks_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D02_SaleTasks_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();

            if (ModelState.IsValid)
            {
                if (model.Attachment != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "a08-tasks-attachments";
                    string dirName = $"{_operationalProvider.D02SelectedTaskID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

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

                    //  Azure allows for 4MB max uploads  (4 x 1024 x 1024 = 4194304)
                    const int uploadLimit = 4194304;
                    uploadFile.Seek(0, SeekOrigin.Begin);   // ensure stream is at the beginning
                    var fileClient = await directory.CreateFileAsync(fileName, uploadFile.Length);
                    // If stream is below the limit upload directly
                    if (uploadFile.Length <= uploadLimit)
                    {
                        await fileClient.Value.UploadRangeAsync(new HttpRange(0, uploadFile.Length), uploadFile);
                    }
                    else
                    {
                        int bytesRead;
                        long index = 0;
                        byte[] buffer = new byte[uploadLimit];

                        // Stream is larger than the limit so we need to upload in chunks
                        while ((bytesRead = uploadFile.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Create a memory stream for the buffer to upload
                            using MemoryStream ms = new MemoryStream(buffer, 0, bytesRead);
                            await fileClient.Value.UploadRangeAsync(ShareFileRangeWriteType.Update, new HttpRange(index, ms.Length), ms);
                            index += ms.Length; // increment the index to the account for bytes already written
                        }
                    }

                    //file.UploadRange(
                    //                    new HttpRange(0, uploadFile.Length),
                    //                    uploadFile);

                    Data.D02_SaleTasks_Attachment D02_SaleTasks_Attachment = new D02_SaleTasks_Attachment()
                    {
                        AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                        DateCreated = DateTime.Now,
                        Filename = fileName,
                        UserID = _userManager.GetUserId(User),
                        Description = model.Description,
                        TaskID = _operationalProvider.D02SelectedTaskID,
                    };

                    var db = new MyVoltageDbContext(_options);
                    db.Add(D02_SaleTasks_Attachment);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }



            return View("~/Views/Operational/D02_Sale/D02_SaleTask_Review_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Review_GetAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> D02_SaleTask_Review_GetAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var dbCache = new MyVoltageDbContext(_options);
            var D02_SaleTasks_Attachment = dbCache.D02_SaleTasks_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (D02_SaleTasks_Attachment == null)
                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");


            string shareName = "a08-tasks-attachments";
            string dirName = $"{_operationalProvider.D02SelectedTaskID}";
            string fileName = D02_SaleTasks_Attachment.Filename;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(D02_SaleTasks_Attachment.Filename));


            return Redirect("/operational/D02_SaleTasks/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_Review_DeleteAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> D02_SaleTask_Review_DeleteAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var D02_SaleTasks_Attachment = db.D02_SaleTasks_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (D02_SaleTasks_Attachment == null)
                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");

            D02_SaleTasks_Attachment.IsDeleted = true;
            db.Update(D02_SaleTasks_Attachment);
            db.SaveChanges();

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTask_AddTimeAllocated/{duration}")]
        public async Task<IActionResult> A08_Task_ReviewAddTimeAllocated(int duration)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ActivityID = _operationalProvider.D02SelectedTaskID,
                    ActivityTypeID = (int)ActivityTypeEnum.D02_SaleTask,
                    CreatedByUserID = op.UserID,
                    DateCreated = DateTime.Now,
                    DescriptionOfWorkAllocated = "None - Allocated via quick link",
                    ExternalChargeOutRatePerHour = op.ExternalChargeOutRatePerHour,
                    InternalChargeOutRatePerHour = op.InternalChargeOutRatePerHour,
                    IsDeleted = false,
                    ResponsibleUserID = op.UserID,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddMinutes(duration),
                };

                db.Add(module_TimeOfWorkAllocated);
                db.SaveChanges();

                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
            }

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_SaleTasks/D02_SaleTask_ReviewStartTimeAllocated")]
        public async Task<IActionResult> D02_SaleTask_ReviewStartTimeAllocated()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.D02SelectedTaskID == 0)
                return Redirect("/operational/D02_Sale/D02_SaleTasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var task = db.D02_SaleTasks.Where(p => p.ID == _operationalProvider.D02SelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ActivityID = _operationalProvider.D02SelectedTaskID,
                    ActivityTypeID = (int)ActivityTypeEnum.D02_SaleTask,
                    CreatedByUserID = op.UserID,
                    DateCreated = DateTime.Now,
                    DescriptionOfWorkAllocated = "None - Allocated via time tracker",
                    ExternalChargeOutRatePerHour = op.ExternalChargeOutRatePerHour,
                    InternalChargeOutRatePerHour = op.InternalChargeOutRatePerHour,
                    IsDeleted = false,
                    ResponsibleUserID = op.UserID,
                    StartTime = DateTime.Now,
                };

                db.Add(module_TimeOfWorkAllocated);
                db.SaveChanges();

                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
            }

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_SaleTasks/D02_SaleTask_ReviewStopTimeAllocated/{timeOfWorkAllocatedID}")]
        public async Task<IActionResult> D02_SaleTask_ReviewStopTimeAllocated(int timeOfWorkAllocatedID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTask_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTask_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var timeOfWorkAllocated = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == timeOfWorkAllocatedID).SingleOrDefault();

            if (timeOfWorkAllocated != null)
            {
                timeOfWorkAllocated.EndTime = DateTime.Now;
                db.Update(timeOfWorkAllocated);
                db.SaveChanges();

                return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
            }

            return Redirect("/operational/D02_Sale/D02_SaleTask_Review");
        }

        [HttpGet]
        [Route("/operational/D02_Sale/D02_SaleTasks_Company_Onboarding")]
        public async Task<IActionResult> D02_SaleTasks_Company_Onboarding()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D02_SaleTasks_Company_Onboarding, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D02_SaleTasks_Company_Onboarding}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.Where(p => p.BusinessPillarID == 8).ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            workflowGroups = workflowGroups.Where(p => p.BusinessDepartmentID.HasValue && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)).ToList();
            var workflowGroupParents = db.WorkflowGroupParents.ToList();
            var secureAreas = db.SecureAreas.ToList();

            D02_SaleTasks_Company_OnboardingModel model = new D02_SaleTasks_Company_OnboardingModel()
            {
                D02_SaleTasks_CompanyDetailsItems = new List<D02_SaleTasks_Company_OnboardingModel.D02_SaleTasks_Company_OnboardingItem>(),
                BusinessDepartmentItems = (from p in businessDepartments
                                           select new D02_SaleTasks_Company_OnboardingModel.BusinessDepartmentItem()
                                           {
                                               BusinessPillarID = p.BusinessPillarID,
                                               DisplayName = p.BusinessDepartmentName,
                                               ID = p.ID,
                                           }).ToList(),
                WorkflowGroupItems = (from p in workflowGroups
                                      select new D02_SaleTasks_Company_OnboardingModel.WorkflowGroupItem()
                                      {
                                          DisplayName = p.WorkflowGroupName,
                                          ID = p.ID,
                                          WorkflowGroupParentID = p.WorkflowGroupParentID,
                                      }).ToList(),
                WorkflowGroupParentItems = (from p in workflowGroupParents
                                            select new D02_SaleTasks_Company_OnboardingModel.WorkflowGroupParentItem()
                                            {
                                                DisplayName = p.WorkflowGroupParentName,
                                                ID = p.ID,
                                            }).ToList(),
                DisplayType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "All Details", Selected = string.IsNullOrEmpty(Request.Query["DisplayType"]) },
                    new SelectListItem() { Value = "1", Text = "Task Headings", Selected = !string.IsNullOrEmpty(Request.Query["DisplayType"]) && Request.Query["DisplayType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Overview", Selected = !string.IsNullOrEmpty(Request.Query["DisplayType"]) && Request.Query["DisplayType"].ToString() == "2" },
                },
                DisplayTypeFilter = Request.Query["DisplayType"].ToString(),
            };


            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.D02_SaleTask_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            //var responsiblePeople = dbCache.D02_SaleTasks_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();


            if (_operationalProvider.CompanyID != 0)
            {
                var tasks = db.D02_SaleTasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                var tasksReassigns = db.D02_SaleTasks_ReassignLogs.ToList();

                foreach (var task in tasks)
                {
                    var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                    var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                    if (status == null)
                        status = siteAdmin_Statuses.FirstOrDefault();
                    var workflowGroup = workflowGroups.Where(p => p.ID == tType.WorkflowGroupID).SingleOrDefault();
                    if (workflowGroup == null || tType == null)
                        continue;

                    D02_SaleTasks_Company_OnboardingModel.D02_SaleTasks_Company_OnboardingItem item = new D02_SaleTasks_Company_OnboardingModel.D02_SaleTasks_Company_OnboardingItem()
                    {
                        CompanyID = task.CompanyID,
                        DateCreated = task.DateCreated,
                        ID = task.ID,
                        StatusID = task.StatusID,
                        D02_SaleTasks_TypeItem = new D02_SaleTasks_Company_OnboardingModel.D02_SaleTasks_Company_OnboardingItem.D02_SaleTask_Type()
                        {
                            SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                            PriorityID = tType.PriorityID,
                            ID = tType.ID,
                            CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                            DashboardURL = tType.DashboardURL,
                            Description = $"{tType.Description}",
                            Heading = tType.Heading,
                            HowToURL = tType.HowToURL,
                            LinkedSecureAreaID = tType.LinkedSecureAreaID,
                            MinRequiredToClear = tType.MinRequiredToClear,
                            ReportingToUserID = tType.ReportingToUserID,
                            ResponsibleUserID = tType.ResponsibleUserID,
                            TemplateNo = tType.TemplateNo,
                            DefaultStatusID = tType.DefaultStatusID,
                            IsDeleted = tType.IsDeleted,
                            StatusGroupID = tType.StatusGroupID,
                            TaskClassificationID = tType.TaskClassificationID,
                            WorkflowGroupID = tType.WorkflowGroupID,
                            BusinessDepartmentID = tType.BusinessDepartmentID.HasValue ? tType.BusinessDepartmentID.Value : (workflowGroup.BusinessDepartmentID.HasValue ? workflowGroup.BusinessDepartmentID.Value : 0),
                            WorkflowGroupParentID = workflowGroup.WorkflowGroupParentID,
                            DefautlMinPlanned = tType.DefautlMinPlanned,
                            Identifier = tType.Identifier,
                            ReportingToUserAccepted = tType.ReportingToUserAccepted,
                            ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                            ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                            ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                            ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                        },
                        ReportingToUserID = task.ReportingToUserID,
                        ResponsibleUserID = task.ResponsibleUserID,
                        //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                        //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                        DateEnded = task.DateEnded,
                        DateStarted = task.DateStarted,
                        KmTravelRequired = task.KmTravelRequired,
                        StockUsed = task.StockUsed,
                        TaskTypeID = task.TaskTypeID,
                        Status = new D02_SaleTasks_Company_OnboardingModel.D02_SaleTasks_Company_OnboardingItem.D02_SaleTasks_Company_OnboardingItemStatus()
                        {
                            CreatedByID = status.CreatedByID,
                            StatusActionID = status.StatusActionID,
                            ID = status.ID,
                            ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                            CreatedDate = status.CreatedDate,
                            GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                            IsDeleted = status.IsDeleted,
                            IsResolvedStatus = status.IsResolvedStatus,
                            ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                            StatusGroupID = status.StatusGroupID,
                            StatusReportingID = status.StatusReportingID,
                            UpdatedByID = status.UpdatedByID,
                            UpdatedDate = status.UpdatedDate,
                            WIPType = status.WIPType,
                        },
                        DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                        Level = task.Level,
                        LatestComment = tasksReassigns.Where(p => p.TaskID == task.ID && !string.IsNullOrEmpty(p.UserDescription)).OrderByDescending(p => p.DateCreated).FirstOrDefault() != null ? tasksReassigns.Where(p => p.TaskID == task.ID && !string.IsNullOrEmpty(p.UserDescription)).OrderByDescending(p => p.DateCreated).FirstOrDefault().UserDescription : "",
                    };

                    var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                    if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                        item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                    var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                        item.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                    model.D02_SaleTasks_CompanyDetailsItems.Add(item);
                }

            }


            model.D02_SaleTasks_CompanyDetailsItems = model.D02_SaleTasks_CompanyDetailsItems.OrderBy(p => p.D02_SaleTasks_TypeItem.Heading).ThenBy(p => p.D02_SaleTasks_TypeItem.Description).ToList();

            return View("~/Views/Operational/D02_Sale/D02_SaleTasks_Company_Onboarding.cshtml", model);
        }
    }
}
