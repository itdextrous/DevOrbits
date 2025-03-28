using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.V04_InternalMeetingsModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.V04_InternalMeetings
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class V04_InternalMeetingsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public V04_InternalMeetingsController(
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
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings")]
        public async Task<IActionResult> V04_InternalMeetings_ExcoMeetings()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_ExcoMeetings, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_ExcoMeetings}/{(int)SecureAreaActionEnum.View}");

            #endregion

            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_ExcoMeetings.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();


            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 14 /*Exco Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 8)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }

            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 14 /*Exco Meeting*/).ToList();

            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }


            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_MancoMeetings")]
        public async Task<IActionResult> V04_InternalMeetings_MancoMeetings()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_MancoMeetings, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_MancoMeetings}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_MancoMeetings.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"] == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 17/*Manco Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 2)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 17/*Manco Meeting*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_RocksMeetings")]
        public async Task<IActionResult> V04_InternalMeetings_RocksMeetings()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_RocksMeetings, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_RocksMeetings}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_RocksMeetings.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 13 /*Rocks Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 3)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 13 /*Rocks Meeting*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_StandupMeetings")]
        public async Task<IActionResult> V04_InternalMeetings_StandupMeetings()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_StandupMeetings, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_StandupMeetings}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_StandupMeetings.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 18 /*Stand-up Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 4)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 18 /*Stand-up Meeting*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_CustomerCareMeeting")]
        public async Task<IActionResult> V04_InternalMeetings_CustomerCareMeeting()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_CustomerCareMeeting, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_CustomerCareMeeting}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_CustomerCareMeeting.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 19 /*Customer Care Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 5)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 19 /*Customer Care Meeting*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_TechnicianDispatch")]
        public async Task<IActionResult> V04_InternalMeetings_TechnicianDispatch()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_ExcoMeetings, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_ExcoMeetings}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_TechnicianDispatch.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 20 /*Technician Dispatch*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 6)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 20 /*Technician Dispatch*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_ITSystemMeeting")]
        public async Task<IActionResult> V04_InternalMeetings_ITSystemMeeting()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_ITSystemMeeting, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_ITSystemMeeting}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_ITSystemMeeting.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => p.StatusID == 21 /*IT System Meeting*/ || (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 7)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var flagTypes = db.A09_Flags_Types.ToList();
            var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            flags = flags.Where(p => p.StatusID == 21 /*IT System Meeting*/).ToList();
            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
                        continue;
                }


                V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
                    A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    GPSLong = flag.GPSLong,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    OnceOffFlag = flag.OnceOffFlag,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
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
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_CompanyDetailsItems.Add(item);
            }

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_SalesMeeting")]
        public async Task<IActionResult> V04_InternalMeetings_SalesMeeting()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_SalesMeeting, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_SalesMeeting}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_SalesMeeting.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 9)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            //SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            //System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            //new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            //var flagTypes = db.A09_Flags_Types.ToList();
            //var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            //var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            //flags = flags.Where(p => p.StatusID == 21 /*IT System Meeting*/).ToList();
            //foreach (var flag in flags)
            //{
            //    var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
            //    var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
            //    if (status == null)
            //        status = siteAdmin_Statuses.FirstOrDefault();

            //    if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            //    {
            //        if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
            //    {
            //        if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["Priority"]))
            //    {
            //        if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["Company"]))
            //    {
            //        if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
            //            continue;
            //    }


            //    V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
            //    {
            //        CompanyID = flag.CompanyID,
            //        CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
            //        Created = flag.Created,
            //        CustomerNo = flag.CustomerNo,
            //        FlagTypeID = flag.FlagTypeID,
            //        ID = flag.ID,
            //        LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
            //        LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
            //        PriorityID = flag.PriorityID,
            //        ReasonForFlag = flag.ReasonForFlag,
            //        ReasonForTicket = flag.ReasonForTicket,
            //        StatusID = flag.StatusID,
            //        //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
            //        A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
            //        {
            //            Active = fType.Active,
            //            DefaultAssignedToID = fType.DefaultAssignedToID,
            //            ID = fType.ID,
            //            FlagTypeName = fType.FlagTypeName,
            //            PolicyDocumentURL = fType.PolicyDocumentURL,
            //            PriorityID = fType.PriorityID,
            //            SecureAreaID = fType.SecureAreaID,
            //            SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
            //        },
            //        AssignedToID = flag.AssignedToID,
            //        AmountInvoiced = flag.AmountInvoiced,
            //        ClosedDate = flag.ClosedDate,
            //        CreatedBy = flag.CreatedBy,
            //        DateLastViewdBy = flag.DateLastViewdBy,
            //        DateLastViewed = flag.DateLastViewed,
            //        DueDate = flag.DueDate,
            //        GPSLat = flag.GPSLat,
            //        GPSLong = flag.GPSLong,
            //        MinRequiredToClear = flag.MinRequiredToClear,
            //        OnceOffFlag = flag.OnceOffFlag,
            //        SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
            //        StockUsed = flag.StockUsed,
            //        TravelKmRequired = flag.TravelKmRequired,
            //        Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
            //        {
            //            CreatedByID = status.CreatedByID,
            //            StatusActionID = status.StatusActionID,
            //            ID = status.ID,
            //            ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
            //            CreatedDate = status.CreatedDate,
            //            GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
            //            IsDeleted = status.IsDeleted,
            //            IsResolvedStatus = status.IsResolvedStatus,
            //            ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
            //            StatusGroupID = status.StatusGroupID,
            //            StatusReportingID = status.StatusReportingID,
            //            UpdatedByID = status.UpdatedByID,
            //            UpdatedDate = status.UpdatedDate,
            //        },
            //        Level = flag.Level,
            //    };

            //    var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
            //    if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
            //        item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

            //    var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
            //    if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
            //        item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

            //    var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
            //    if (latestCommentResults.Length > 0)
            //    {
            //        string latestCommentUserUsername = "";
            //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
            //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
            //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

            //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
            //    }

            //    model.A09_Flags_CompanyDetailsItems.Add(item);
            //}

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V04_InternalMeetings/V04_InternalMeetings_OnboardingMeeting")]
        public async Task<IActionResult> V04_InternalMeetings_OnboardingMeeting()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V04_InternalMeetings_OnboardingMeeting, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V04_InternalMeetings_OnboardingMeeting}/{(int)SecureAreaActionEnum.View}");

            #endregion


            ViewData["Title"] = MyVoltage.Data.SecureAreaEnum.V04_InternalMeetings_OnboardingMeeting.GetDescription();
            V04_InternalMeetings_ExcoMeetingsModel model = new V04_InternalMeetings_ExcoMeetingsModel()
            {
                V04_InternalMeetings_ExcoMeetingsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem>(),
                FromDate = DateTime.Now.AddYears(-1).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"]) }
                },
                A09_Flags_CompanyDetailsItems = new List<V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Priority.AddRange((from p in db.SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            model.SecureAreaGroupID.AddRange((from p in ((SecureArea.GroupEnum[])Enum.GetValues(typeof(SecureArea.GroupEnum)))
                                              orderby p.GetDescription()
                                              select new SelectListItem()
                                              {
                                                  Text = p.GetDescription(),
                                                  Value = ((int)p).ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == ((int)p).ToString() ? true : false,
                                              }).ToList());
            model.SecureAreaGroupID = model.SecureAreaGroupID.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"].ToString() == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            List<Data.A08_Task> tasks = new List<Data.A08_Task>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var tasks1 = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            tasks = tasks1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            tasks = tasks.Where(p => (p.MeetingAgendaID.HasValue && p.MeetingAgendaID.Value == 10)).ToList();

            foreach (var task in tasks)
            {
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem item = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.A08_Task_Type()
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
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new V04_InternalMeetings_ExcoMeetingsModel.V04_InternalMeetings_ExcoMeetingsItem.ItemStatus()
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

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                model.V04_InternalMeetings_ExcoMeetingsItems.Add(item);
            }


            //SqlCommand sqlCommandsp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            //System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            //new SqlDataAdapter(sqlCommandsp_GetA09_FlagsLatestComment).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            //var flagTypes = db.A09_Flags_Types.ToList();
            //var flags1 = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();
            //var flags = flags1.Where(p => p.DueDate.HasValue && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.Value.Date >= model.FromDate.Value.Date && p.DueDate.Value.Date <= model.ToDate.Value.Date).ToList();

            //flags = flags.Where(p => p.StatusID == 21 /*IT System Meeting*/).ToList();
            //foreach (var flag in flags)
            //{
            //    var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
            //    var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
            //    if (status == null)
            //        status = siteAdmin_Statuses.FirstOrDefault();

            //    if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            //    {
            //        if (Request.Query["ResponsibleUser"].ToString() != flag.AssignedToID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
            //    {
            //        if (Request.Query["ReportingToUser"].ToString() != flag.ReportingToUserID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["Priority"]))
            //    {
            //        if (Convert.ToInt32(Request.Query["Priority"]) != fType.PriorityID)
            //            continue;
            //    }

            //    if (!string.IsNullOrEmpty(Request.Query["Company"]))
            //    {
            //        if (Convert.ToInt32(Request.Query["Company"]) != flag.CompanyID)
            //            continue;
            //    }


            //    V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem item = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem()
            //    {
            //        CompanyID = flag.CompanyID,
            //        CompanyName = flag.CompanyID.HasValue && companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "---",
            //        Created = flag.Created,
            //        CustomerNo = flag.CustomerNo,
            //        FlagTypeID = flag.FlagTypeID,
            //        ID = flag.ID,
            //        LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
            //        LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
            //        PriorityID = flag.PriorityID,
            //        ReasonForFlag = flag.ReasonForFlag,
            //        ReasonForTicket = flag.ReasonForTicket,
            //        StatusID = flag.StatusID,
            //        //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyDetailsModel.A09_Flags_CompanyDetailsItem.A09_Flags_ResponsiblePersonItem>(),
            //        A09_Flags_TypeItem = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Type()
            //        {
            //            Active = fType.Active,
            //            DefaultAssignedToID = fType.DefaultAssignedToID,
            //            ID = fType.ID,
            //            FlagTypeName = fType.FlagTypeName,
            //            PolicyDocumentURL = fType.PolicyDocumentURL,
            //            PriorityID = fType.PriorityID,
            //            SecureAreaID = fType.SecureAreaID,
            //            SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
            //        },
            //        AssignedToID = flag.AssignedToID,
            //        AmountInvoiced = flag.AmountInvoiced,
            //        ClosedDate = flag.ClosedDate,
            //        CreatedBy = flag.CreatedBy,
            //        DateLastViewdBy = flag.DateLastViewdBy,
            //        DateLastViewed = flag.DateLastViewed,
            //        DueDate = flag.DueDate,
            //        GPSLat = flag.GPSLat,
            //        GPSLong = flag.GPSLong,
            //        MinRequiredToClear = flag.MinRequiredToClear,
            //        OnceOffFlag = flag.OnceOffFlag,
            //        SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
            //        StockUsed = flag.StockUsed,
            //        TravelKmRequired = flag.TravelKmRequired,
            //        Status = new V04_InternalMeetings_ExcoMeetingsModel.A09_Flags_Company_DetailsItem.A09_Flags_Company_DetailsItemStatus()
            //        {
            //            CreatedByID = status.CreatedByID,
            //            StatusActionID = status.StatusActionID,
            //            ID = status.ID,
            //            ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
            //            CreatedDate = status.CreatedDate,
            //            GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
            //            IsDeleted = status.IsDeleted,
            //            IsResolvedStatus = status.IsResolvedStatus,
            //            ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
            //            StatusGroupID = status.StatusGroupID,
            //            StatusReportingID = status.StatusReportingID,
            //            UpdatedByID = status.UpdatedByID,
            //            UpdatedDate = status.UpdatedDate,
            //        },
            //        Level = flag.Level,
            //    };

            //    var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
            //    if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
            //        item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

            //    var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
            //    if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
            //        item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

            //    var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
            //    if (latestCommentResults.Length > 0)
            //    {
            //        string latestCommentUserUsername = "";
            //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
            //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
            //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

            //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
            //    }

            //    model.A09_Flags_CompanyDetailsItems.Add(item);
            //}

            model.V04_InternalMeetings_ExcoMeetingsItems = model.V04_InternalMeetings_ExcoMeetingsItems.OrderByDescending(p => p.Level).ThenByDescending(p => p.DueDate.Value).ToList();
            return View("~/Views/Operational/V04_InternalMeetings/V04_InternalMeetings_ExcoMeetings.cshtml", model);
        }

    }
}
