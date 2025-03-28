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
using MyVoltage.Models.OperationalModels.W01_ActivityLogsModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.W01_ActivityLogs
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class W01_ActivityLogsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public W01_ActivityLogsController(
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
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TimePlanner_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_TimePlanner_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TimePlanner_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TimePlanner_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            W01_ActivityLogs_TimePlanner_SummaryModel model = new W01_ActivityLogs_TimePlanner_SummaryModel()
            {
                FromDate = DateTime.Now.Date,
                ToDate = DateTime.Now.AddDays(14).Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Time", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Internal Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "External Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "3" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TimePlanner_SummaryItems = new List<W01_ActivityLogs_TimePlanner_SummaryModel.W01_ActivityLogs_TimePlanner_SummaryItem>(),
                ShowOnlyOpen = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "All", Selected = string.IsNullOrEmpty(Request.Query["ShowOnlyOpen"]) || Request.Query["ShowOnlyOpen"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Open Only", Selected = !string.IsNullOrEmpty(Request.Query["ShowOnlyOpen"]) && Request.Query["ShowOnlyOpen"].ToString() == "2" },
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_TimeOfWorkAllocateds = new SqlCommand($"exec [sp_Module_TimeOfWorkPlanneds] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_TimeOfWorkAllocateds = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_TimeOfWorkAllocateds).Fill(tbl_sp_Module_TimeOfWorkAllocateds);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_TimePlanner_SummaryModel.W01_ActivityLogs_TimePlanner_SummaryItem item = new W01_ActivityLogs_TimePlanner_SummaryModel.W01_ActivityLogs_TimePlanner_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkPlanned] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkPlanned] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        if (!string.IsNullOrEmpty(Request.Query["ShowOnlyOpen"]) && Request.Query["ShowOnlyOpen"].ToString() == "2")
                        {
                            var status = siteAdmin_Statuses.Where(p => p.ID == Convert.ToInt32(row["StatusID"])).SingleOrDefault();
                            if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                                continue;
                        }
                        if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                        {
                            if (row["MinOfWorkPlanned"] != DBNull.Value)
                            {
                                minSpent += Convert.ToDecimal(row["MinOfWorkPlanned"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "2")
                        {
                            if (row["InternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkPlanned"]) / 60) * Convert.ToDecimal(row["InternalChargeOutRatePerHour"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "3")
                        {
                            if (row["ExternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkPlanned"]) / 60) * Convert.ToDecimal(row["ExternalChargeOutRatePerHour"]);
                            }
                        }
                    }

                    item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(current, minSpent));

                    current = current.AddDays(1);
                }

                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_TimePlanner_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_TimePlanner_SummaryItems = model.W01_ActivityLogs_TimePlanner_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TimePlanner_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TimePlanner_Details")]
        public async Task<IActionResult> W01_ActivityLogs_TimePlanner_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TimePlanner_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TimePlanner_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_TimePlanner_DetailsModel model = new W01_ActivityLogs_TimePlanner_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.AddDays(14).Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Time", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Internal Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "External Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "3" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TimePlanner_DetailsItems = new PaginatedList<W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem>(new List<W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0
            };

            var W01_ActivityLogs_TimePlanner_DetailsItems = new List<W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem>();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.Module_TimeOfWorkPlanneds
                                             where p.DateOfWorkPlanned.Date >= model.FromDate.Value
                                             && p.DateOfWorkPlanned.Date <= model.ToDate.Value
                                             select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ResponsibleUserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActivityTypeID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }

            var tTypes = db.A08_Task_Types.ToList();
            var fTypes = db.A09_Flags_Types.ToList();
            var oTypes = db.E01_BuildingOnboardingTask_Types.ToList();

            SqlCommand cmd_sp_W01_ActivityLogs_TimePlanner_Details_Tasks = new SqlCommand($"sp_W01_ActivityLogs_TimePlanner_Details_Tasks", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Tasks.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Tasks.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Tasks.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimePlanner_Details_Tasks = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimePlanner_Details_Tasks).Fill(tbl_sp_W01_ActivityLogs_TimePlanner_Details_Tasks);

            SqlCommand cmd_sp_W01_ActivityLogs_TimePlanner_Details_Flags = new SqlCommand($"sp_W01_ActivityLogs_TimePlanner_Details_Flags", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Flags.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Flags.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_Flags.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimePlanner_Details_Flags = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimePlanner_Details_Flags).Fill(tbl_sp_W01_ActivityLogs_TimePlanner_Details_Flags);

            SqlCommand cmd_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks = new SqlCommand($"sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks).Fill(tbl_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks);

            SqlCommand cmd_sp_GetA08_TasksLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA08_TasksLatestComment).Fill(tbl_sp_GetA08_TasksLatestComment);

            SqlCommand cmd_sp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA09_FlagsLatestComment).Fill(tbl_sp_GetA09_FlagsLatestComment);


            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var opProf = opProfs.Where(p => p.UserID == module.ResponsibleUserID).SingleOrDefault();

                W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem item = new W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem()
                {
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ResponsibleUserID = module.ResponsibleUserID,
                    ActivityTypeID = module.ActivityTypeID,
                    DateOfWorkPlanned = module.DateOfWorkPlanned,
                    ActivityID = module.ActivityID,
                    CreatedByUserID = module.CreatedByUserID,
                    DateCreated = module.DateCreated,
                    DescriptionOfWorkPlanned = module.DescriptionOfWorkPlanned,
                    ExternalChargeOutRatePerHour = module.ExternalChargeOutRatePerHour,
                    ID = module.ID,
                    InternalChargeOutRatePerHour = module.InternalChargeOutRatePerHour,
                    IsDeleted = module.IsDeleted,
                    MinOfWorkPlanned = module.MinOfWorkPlanned,
                };

                switch (module.ActivityTypeID)
                {
                    case (int)ActivityTypeEnum.A09_Flag:
                        System.Data.DataRow[] linkedFlag = tbl_sp_W01_ActivityLogs_TimePlanner_Details_Flags.Select($"ID = '{module.ActivityID}'");
                        if (linkedFlag.Length > 0)
                        {
                            item.ActivityURL = $"/operational/A08_Flags/A08_Flag_Review/{linkedFlag[0]["FlagTypeID"]}/{linkedFlag[0]["ID"]}";
                            item.Company = linkedFlag[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedFlag[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedFlag[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = fTypes.Where(p => p.ID == Convert.ToInt32(linkedFlag[0]["FlagTypeID"])).SingleOrDefault().FlagTypeName;

                            var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag[0]["ID"]}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                            }

                        }
                        //var linkedFlag = flags.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        //if (linkedFlag != null)
                        //{
                        //    item.ActivityURL = $"/operational/A09_Flags/A09_Flags_CompanyReview/{linkedFlag.FlagTypeID}/{linkedFlag.ID}";
                        //    item.Company = linkedFlag.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault().Name : $"";
                        //    item.ActivityHeading = fTypes.Where(p => p.ID == linkedFlag.FlagTypeID).SingleOrDefault().FlagTypeName;
                        //    var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag.ID}'");
                        //    if (latestCommentResults.Length > 0)
                        //    {
                        //        string latestCommentUserUsername = "";
                        //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                        //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                        //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                        //    }
                        //}
                        break;
                    case (int)ActivityTypeEnum.A08_Task:

                        System.Data.DataRow[] linkedTask = tbl_sp_W01_ActivityLogs_TimePlanner_Details_Tasks.Select($"ID = '{module.ActivityID}'");
                        if (linkedTask.Length > 0)
                        {
                            item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask[0]["TaskTypeID"]}/{linkedTask[0]["ID"]}";
                            item.Company = linkedTask[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedTask[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedTask[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = tTypes.Where(p => p.ID == Convert.ToInt32(linkedTask[0]["TaskTypeID"])).SingleOrDefault().Heading;

                            var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask[0]["ID"]}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                            }

                        }


                        //var linkedTask = tasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        //if (linkedTask != null)
                        //{
                        //    item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask.TaskTypeID}/{linkedTask.ID}";
                        //    item.Company = linkedTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault().Name : $"";
                        //    item.ActivityHeading = tTypes.Where(p => p.ID == linkedTask.TaskTypeID).SingleOrDefault().Heading;

                        //    var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask.ID}'");
                        //    if (latestCommentResults.Length > 0)
                        //    {
                        //        string latestCommentUserUsername = "";
                        //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                        //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                        //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                        //    }

                        //}
                        break;
                    case (int)ActivityTypeEnum.E01_BuildingOnboardingTask:
                        System.Data.DataRow[] linkedoTask = tbl_sp_W01_ActivityLogs_TimePlanner_Details_BuildingOnboardingTasks.Select($"ID = '{module.ActivityID}'");
                        if (linkedoTask.Length > 0)
                        {
                            item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask[0]["TaskTypeID"]}/{linkedoTask[0]["ID"]}";
                            item.Company = linkedoTask[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedoTask[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedoTask[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = oTypes.Where(p => p.ID == Convert.ToInt32(linkedoTask[0]["TaskTypeID"])).SingleOrDefault().Heading;
                        }
                        break;
                }

                decimal minSpent = 0;
                if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                {
                    minSpent += item.MinOfWorkPlanned;
                }
                else if (Request.Query["DisplayAs"].ToString() == "2")
                {
                    if (item.InternalChargeOutRatePerHour.HasValue)
                    {
                        minSpent += (Convert.ToDecimal(item.MinOfWorkPlanned) / 60) * Convert.ToDecimal(item.InternalChargeOutRatePerHour);
                    }
                }
                else if (Request.Query["DisplayAs"].ToString() == "3")
                {
                    if (item.ExternalChargeOutRatePerHour.HasValue)
                    {
                        minSpent += (Convert.ToDecimal(item.MinOfWorkPlanned) / 60) * Convert.ToDecimal(item.ExternalChargeOutRatePerHour);
                    }
                }
                item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(item.DateOfWorkPlanned.Date, minSpent));


                if (item.TimeSpent.Count > 0)
                    W01_ActivityLogs_TimePlanner_DetailsItems.Add(item);
            }


            W01_ActivityLogs_TimePlanner_DetailsItems = W01_ActivityLogs_TimePlanner_DetailsItems.OrderBy(p => p.UserName).ToList();

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = W01_ActivityLogs_TimePlanner_DetailsItems.Count;
            model.W01_ActivityLogs_TimePlanner_DetailsItems = await PaginatedList<W01_ActivityLogs_TimePlanner_DetailsModel.W01_ActivityLogs_TimePlanner_DetailsItem>.CreateAsync(W01_ActivityLogs_TimePlanner_DetailsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TimePlanner_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TimeAllocated_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_TimeAllocated_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_TimeAllocated_SummaryModel model = new W01_ActivityLogs_TimeAllocated_SummaryModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Time", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Internal Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "External Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "3" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TimeAllocated_SummaryItems = new List<W01_ActivityLogs_TimeAllocated_SummaryModel.W01_ActivityLogs_TimeAllocated_SummaryItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_TimeOfWorkAllocateds = new SqlCommand($"exec [sp_Module_TimeOfWorkAllocateds] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_TimeOfWorkAllocateds = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_TimeOfWorkAllocateds).Fill(tbl_sp_Module_TimeOfWorkAllocateds);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_TimeAllocated_SummaryModel.W01_ActivityLogs_TimeAllocated_SummaryItem item = new W01_ActivityLogs_TimeAllocated_SummaryModel.W01_ActivityLogs_TimeAllocated_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkAllocated] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkAllocated] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                        {
                            if (row["MinOfWorkAllocated"] != DBNull.Value)
                            {
                                minSpent += Convert.ToDecimal(row["MinOfWorkAllocated"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "2")
                        {
                            if (row["InternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkAllocated"]) / 60) * Convert.ToDecimal(row["InternalChargeOutRatePerHour"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "3")
                        {
                            if (row["ExternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkAllocated"]) / 60) * Convert.ToDecimal(row["ExternalChargeOutRatePerHour"]);
                            }
                        }
                    }

                    item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(current, minSpent));

                    current = current.AddDays(1);
                }

                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_TimeAllocated_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_TimeAllocated_SummaryItems = model.W01_ActivityLogs_TimeAllocated_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TimeAllocated_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TimeAllocated_Details")]
        public async Task<IActionResult> W01_ActivityLogs_TimeAllocated_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_TimeAllocated_DetailsModel model = new W01_ActivityLogs_TimeAllocated_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Time", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Internal Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "External Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "3" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TimeAllocated_DetailsItems = new List<W01_ActivityLogs_TimeAllocated_DetailsModel.W01_ActivityLogs_TimeAllocated_DetailsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.Module_TimeOfWorkAllocateds
                                             select p).ToList();

            module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                         where p.DateOfWorkAllocated.Date >= model.FromDate.Value
                                         && p.DateOfWorkAllocated.Date <= model.ToDate.Value
                                         select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ResponsibleUserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActivityTypeID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }

            var tTypes = db.A08_Task_Types.ToList();
            var fTypes = db.A09_Flags_Types.ToList();
            var oTypes = db.E01_BuildingOnboardingTask_Types.ToList();

            SqlCommand cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks = new SqlCommand($"sp_W01_ActivityLogs_TimeAllocated_Details_Tasks", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks).Fill(tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks);

            SqlCommand cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Flags = new SqlCommand($"sp_W01_ActivityLogs_TimeAllocated_Details_Flags", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Flags.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Flags.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Flags.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Flags = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimeAllocated_Details_Flags).Fill(tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Flags);

            SqlCommand cmd_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks = new SqlCommand($"sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks.CommandType = System.Data.CommandType.StoredProcedure;
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks.Parameters.AddWithValue("@FromDate", model.FromDate.Value.ToString("yyyy-MM-dd"));
            cmd_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks.Parameters.AddWithValue("@ToDate", model.ToDate.Value.ToString("yyyy-MM-dd"));
            System.Data.DataTable tbl_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks).Fill(tbl_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks);

            SqlCommand cmd_sp_GetA08_TasksLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA08_TasksLatestComment).Fill(tbl_sp_GetA08_TasksLatestComment);

            SqlCommand cmd_sp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA09_FlagsLatestComment).Fill(tbl_sp_GetA09_FlagsLatestComment);

            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var opProf = opProfs.Where(p => p.UserID == module.ResponsibleUserID).SingleOrDefault();

                W01_ActivityLogs_TimeAllocated_DetailsModel.W01_ActivityLogs_TimeAllocated_DetailsItem item = new W01_ActivityLogs_TimeAllocated_DetailsModel.W01_ActivityLogs_TimeAllocated_DetailsItem()
                {
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ResponsibleUserID = module.ResponsibleUserID,
                    ActivityTypeID = module.ActivityTypeID,
                    ActivityID = module.ActivityID,
                    CreatedByUserID = module.CreatedByUserID,
                    DateCreated = module.DateCreated,
                    DescriptionOfWorkAllocated = module.DescriptionOfWorkAllocated,
                    ExternalChargeOutRatePerHour = module.ExternalChargeOutRatePerHour,
                    ID = module.ID,
                    InternalChargeOutRatePerHour = module.InternalChargeOutRatePerHour,
                    IsDeleted = module.IsDeleted,
                    EndTime = module.EndTime,
                    StartTime = module.StartTime,
                };

                switch (module.ActivityTypeID)
                {
                    case (int)ActivityTypeEnum.A09_Flag:
                        System.Data.DataRow[] linkedFlag = tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Flags.Select($"ID = '{module.ActivityID}'");
                        if (linkedFlag.Length > 0)
                        {
                            item.ActivityURL = $"/operational/A08_Flags/A08_Flag_Review/{linkedFlag[0]["FlagTypeID"]}/{linkedFlag[0]["ID"]}";
                            item.Company = linkedFlag[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedFlag[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedFlag[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = fTypes.Where(p => p.ID == Convert.ToInt32(linkedFlag[0]["FlagTypeID"])).SingleOrDefault().FlagTypeName;

                            var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag[0]["ID"]}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                            }

                        }
                        //var linkedFlag = flags.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        //if (linkedFlag != null)
                        //{
                        //    item.ActivityURL = $"/operational/A09_Flags/A09_Flags_CompanyReview/{linkedFlag.FlagTypeID}/{linkedFlag.ID}";
                        //    item.Company = linkedFlag.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault().Name : $"";
                        //    item.ActivityHeading = fTypes.Where(p => p.ID == linkedFlag.FlagTypeID).SingleOrDefault().FlagTypeName;
                        //    var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag.ID}'");
                        //    if (latestCommentResults.Length > 0)
                        //    {
                        //        string latestCommentUserUsername = "";
                        //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                        //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                        //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                        //    }
                        //}
                        break;
                    case (int)ActivityTypeEnum.A08_Task:

                        System.Data.DataRow[] linkedTask = tbl_sp_W01_ActivityLogs_TimeAllocated_Details_Tasks.Select($"ID = '{module.ActivityID}'");
                        if (linkedTask.Length > 0)
                        {
                            item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask[0]["TaskTypeID"]}/{linkedTask[0]["ID"]}";
                            item.Company = linkedTask[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedTask[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedTask[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = tTypes.Where(p => p.ID == Convert.ToInt32(linkedTask[0]["TaskTypeID"])).SingleOrDefault().Heading;

                            var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask[0]["ID"]}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                            }

                        }

                        //var linkedTask = tasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        //if (linkedTask != null)
                        //{
                        //    item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask.TaskTypeID}/{linkedTask.ID}";
                        //    item.Company = linkedTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault().Name : $"";
                        //    item.ActivityHeading = tTypes.Where(p => p.ID == linkedTask.TaskTypeID).SingleOrDefault().Heading;
                        //    var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask.ID}'");
                        //    if (latestCommentResults.Length > 0)
                        //    {
                        //        string latestCommentUserUsername = "";
                        //        var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                        //        if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        //            latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                        //        item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                        //    }
                        //}
                        break;
                    case (int)ActivityTypeEnum.E01_BuildingOnboardingTask:
                        System.Data.DataRow[] linkedoTask = tbl_sp_W01_ActivityLogs_TimeAllocated_Details_BuildingOnboardingTasks.Select($"ID = '{module.ActivityID}'");
                        if (linkedoTask.Length > 0)
                        {
                            item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask[0]["TaskTypeID"]}/{linkedoTask[0]["ID"]}";
                            item.Company = linkedoTask[0]["CompanyID"] != DBNull.Value && companies.Where(p => p.CompanyID == Convert.ToInt32(linkedoTask[0]["CompanyID"])).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == Convert.ToInt32(linkedoTask[0]["CompanyID"])).SingleOrDefault().Name : $"";
                            item.ActivityHeading = oTypes.Where(p => p.ID == Convert.ToInt32(linkedoTask[0]["TaskTypeID"])).SingleOrDefault().Heading;
                        }

                        //var linkedoTask = onboardingTasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        //if (linkedoTask != null)
                        //{
                        //    item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask.TaskTypeID}/{linkedoTask.ID}";
                        //    item.Company = linkedoTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault().Name : $"";
                        //    item.ActivityHeading = oTypes.Where(p => p.ID == linkedoTask.TaskTypeID).SingleOrDefault().Heading;
                        //}
                        break;
                }

                decimal minSpent = 0;
                if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                {
                    minSpent += item.MinOfWorkAllocated;
                }
                else if (Request.Query["DisplayAs"].ToString() == "2")
                {
                    if (item.InternalChargeOutRatePerHour.HasValue)
                    {
                        minSpent += (Convert.ToDecimal(item.MinOfWorkAllocated) / 60) * Convert.ToDecimal(item.InternalChargeOutRatePerHour);
                    }
                }
                else if (Request.Query["DisplayAs"].ToString() == "3")
                {
                    if (item.ExternalChargeOutRatePerHour.HasValue)
                    {
                        minSpent += (Convert.ToDecimal(item.MinOfWorkAllocated) / 60) * Convert.ToDecimal(item.ExternalChargeOutRatePerHour);
                    }
                }
                item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(item.DateOfWorkAllocated.Date, minSpent));


                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_TimeAllocated_DetailsItems.Add(item);
            }


            model.W01_ActivityLogs_TimeAllocated_DetailsItems = model.W01_ActivityLogs_TimeAllocated_DetailsItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TimeAllocated_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_MissingTimeAllocated_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_MissingTimeAllocated_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_MissingTimeAllocated_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_MissingTimeAllocated_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_MissingTimeAllocated_SummaryModel model = new W01_ActivityLogs_MissingTimeAllocated_SummaryModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Time", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Internal Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "External Cost", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "3" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_MissingTimeAllocated_SummaryItems = new List<W01_ActivityLogs_MissingTimeAllocated_SummaryModel.W01_ActivityLogs_MissingTimeAllocated_SummaryItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_TimeOfWorkAllocateds = new SqlCommand($"exec [sp_Module_TimeOfWorkAllocateds] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_TimeOfWorkAllocateds = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_TimeOfWorkAllocateds).Fill(tbl_sp_Module_TimeOfWorkAllocateds);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_MissingTimeAllocated_SummaryModel.W01_ActivityLogs_MissingTimeAllocated_SummaryItem item = new W01_ActivityLogs_MissingTimeAllocated_SummaryModel.W01_ActivityLogs_MissingTimeAllocated_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkAllocated] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_TimeOfWorkAllocateds.Select($"[DateOfWorkAllocated] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                        {
                            if (row["MinOfWorkAllocated"] != DBNull.Value)
                            {
                                minSpent += Convert.ToDecimal(row["MinOfWorkAllocated"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "2")
                        {
                            if (row["InternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkAllocated"]) / 60) * Convert.ToDecimal(row["InternalChargeOutRatePerHour"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "3")
                        {
                            if (row["ExternalChargeOutRatePerHour"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["MinOfWorkAllocated"]) / 60) * Convert.ToDecimal(row["ExternalChargeOutRatePerHour"]);
                            }
                        }
                    }

                    decimal missingMin = (60.0m * 8.0m) - minSpent;

                    item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(current, missingMin));

                    current = current.AddDays(1);
                }

                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_MissingTimeAllocated_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_MissingTimeAllocated_SummaryItems = model.W01_ActivityLogs_MissingTimeAllocated_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_MissingTimeAllocated_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TravelAllocation_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_TravelAllocation_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_TravelAllocation_SummaryModel model = new W01_ActivityLogs_TravelAllocation_SummaryModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TravelAllocation_SummaryItems = new List<W01_ActivityLogs_TravelAllocation_SummaryModel.W01_ActivityLogs_TravelAllocation_SummaryItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_TravelAllocations = new SqlCommand($"exec [sp_Module_TravelAllocations] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_TravelAllocations = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_TravelAllocations).Fill(tbl_sp_Module_TravelAllocations);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_TravelAllocations.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_TravelAllocations.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_TravelAllocation_SummaryModel.W01_ActivityLogs_TravelAllocation_SummaryItem item = new W01_ActivityLogs_TravelAllocation_SummaryModel.W01_ActivityLogs_TravelAllocation_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    TravelSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_TravelAllocations.Select($"[DateOfTravelAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_TravelAllocations.Select($"[DateOfTravelAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                        {
                            if (row["VehicleDistanceKm"] != DBNull.Value && Convert.ToDecimal(row["VehicleDistanceKm"]) != 0)
                            {
                                minSpent += Convert.ToDecimal(row["VehicleDistanceKm"]);
                            }
                        }
                        else if (Request.Query["DisplayAs"].ToString() == "2")
                        {
                            if (row["RatePerKm"] != DBNull.Value)
                            {
                                minSpent += (Convert.ToDecimal(row["VehicleDistanceKm"])) * Convert.ToDecimal(row["RatePerKm"]);
                            }
                        }
                    }

                    item.TravelSpent.Add(new KeyValuePair<DateTime, decimal>(current, minSpent));

                    current = current.AddDays(1);
                }

                if (item.TravelSpent.Count > 0)
                    model.W01_ActivityLogs_TravelAllocation_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_TravelAllocation_SummaryItems = model.W01_ActivityLogs_TravelAllocation_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TravelAllocation_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_TravelAllocation_Details")]
        public async Task<IActionResult> W01_ActivityLogs_TravelAllocation_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_TravelAllocation_DetailsModel model = new W01_ActivityLogs_TravelAllocation_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_TravelAllocation_DetailsItems = new List<W01_ActivityLogs_TravelAllocation_DetailsModel.W01_ActivityLogs_TravelAllocation_DetailsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.Module_TravelAllocations
                                             where p.DateOfTravelAllocation.Date >= model.FromDate.Value
                                             && p.DateOfTravelAllocation.Date <= model.ToDate.Value
                                             select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ResponsibleUserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActivityTypeID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }

            var tTypes = db.A08_Task_Types.ToList();
            var tasks = (from p in db.A08_Tasks
                         select p).ToList();
            tasks = (from p in tasks
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A08_Task).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var fTypes = db.A09_Flags_Types.ToList();
            var flags = (from p in db.A09_Flags
                         select p).ToList();
            flags = (from p in flags
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A09_Flag).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var oTypes = db.E01_BuildingOnboardingTask_Types.ToList();
            var onboardingTasks = (from p in db.E01_BuildingOnboardingTasks
                                   select p).ToList();
            onboardingTasks = (from p in onboardingTasks
                               where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.E01_BuildingOnboardingTask).Select(c => c.ActivityID).Contains(p.ID)
                               select p).ToList();

            SqlCommand cmd_sp_GetA08_TasksLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA08_TasksLatestComment).Fill(tbl_sp_GetA08_TasksLatestComment);

            SqlCommand cmd_sp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA09_FlagsLatestComment).Fill(tbl_sp_GetA09_FlagsLatestComment);

            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var opProf = opProfs.Where(p => p.UserID == module.ResponsibleUserID).SingleOrDefault();

                W01_ActivityLogs_TravelAllocation_DetailsModel.W01_ActivityLogs_TravelAllocation_DetailsItem item = new W01_ActivityLogs_TravelAllocation_DetailsModel.W01_ActivityLogs_TravelAllocation_DetailsItem()
                {
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ResponsibleUserID = module.ResponsibleUserID,
                    ActivityTypeID = module.ActivityTypeID,
                    DateOfTravelAllocation = module.DateOfTravelAllocation,
                    ActivityID = module.ActivityID,
                    CreatedByUserID = module.CreatedByUserID,
                    DateCreated = module.DateCreated,
                    DescriptionOfTravelAllocation = module.DescriptionOfTravelAllocation,
                    ID = module.ID,
                    IsDeleted = module.IsDeleted,
                    ActivityHeading = "",
                    ActivityURL = "",
                    LatestComment = "",
                    PhotoURL = module.PhotoURL,
                    RatePerKm = module.RatePerKm,
                    VehicleDistanceKm = module.VehicleDistanceKm,
                    VehicleID = module.VehicleID,
                    VehicleOdoEnd = module.VehicleOdoEnd,
                    VehicleOdoStart = module.VehicleOdoStart,
                    VehicleTypeID = module.VehicleTypeID,
                };

                switch (module.ActivityTypeID)
                {
                    case (int)ActivityTypeEnum.A09_Flag:
                        var linkedFlag = flags.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedFlag != null)
                        {
                            item.ActivityURL = $"/operational/A09_Flags/A09_Flags_CompanyReview/{linkedFlag.FlagTypeID}/{linkedFlag.ID}";
                            item.Company = linkedFlag.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = fTypes.Where(p => p.ID == linkedFlag.FlagTypeID).SingleOrDefault().FlagTypeName;
                            var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.A08_Task:
                        var linkedTask = tasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedTask != null)
                        {
                            item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask.TaskTypeID}/{linkedTask.ID}";
                            item.Company = linkedTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = tTypes.Where(p => p.ID == linkedTask.TaskTypeID).SingleOrDefault().Heading;
                            var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.E01_BuildingOnboardingTask:
                        var linkedoTask = onboardingTasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedoTask != null)
                        {
                            item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask.TaskTypeID}/{linkedoTask.ID}";
                            item.Company = linkedoTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = oTypes.Where(p => p.ID == linkedoTask.TaskTypeID).SingleOrDefault().Heading;
                        }
                        break;
                }

                decimal minSpent = 0;
                if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                {
                    minSpent += item.VehicleDistanceKm;
                }
                else if (Request.Query["DisplayAs"].ToString() == "2")
                {
                    if (item.RatePerKm.HasValue)
                    {
                        minSpent += Convert.ToDecimal(item.VehicleDistanceKm) * Convert.ToDecimal(item.RatePerKm);
                    }
                }
                item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(item.DateOfTravelAllocation.Date, minSpent));


                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_TravelAllocation_DetailsItems.Add(item);
            }


            model.W01_ActivityLogs_TravelAllocation_DetailsItems = model.W01_ActivityLogs_TravelAllocation_DetailsItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_TravelAllocation_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_StockAllocation_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_StockAllocation_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_StockAllocation_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_StockAllocation_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_StockAllocation_SummaryModel model = new W01_ActivityLogs_StockAllocation_SummaryModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_StockAllocation_SummaryItems = new List<W01_ActivityLogs_StockAllocation_SummaryModel.W01_ActivityLogs_StockAllocation_SummaryItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_StockAllocations = new SqlCommand($"exec [sp_Module_StockAllocations] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_StockAllocations = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_StockAllocations).Fill(tbl_sp_Module_StockAllocations);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_StockAllocations.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_StockAllocations.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_StockAllocation_SummaryModel.W01_ActivityLogs_StockAllocation_SummaryItem item = new W01_ActivityLogs_StockAllocation_SummaryModel.W01_ActivityLogs_StockAllocation_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    StockSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_StockAllocations.Select($"[DateOfStockAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_StockAllocations.Select($"[DateOfStockAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        minSpent++;
                    }

                    item.StockSpent.Add(new KeyValuePair<DateTime, decimal>(current, minSpent));

                    current = current.AddDays(1);
                }

                if (item.StockSpent.Count > 0)
                    model.W01_ActivityLogs_StockAllocation_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_StockAllocation_SummaryItems = model.W01_ActivityLogs_StockAllocation_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_StockAllocation_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_StockAllocation_Details")]
        public async Task<IActionResult> W01_ActivityLogs_StockAllocation_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_StockAllocation_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_StockAllocation_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_StockAllocation_DetailsModel model = new W01_ActivityLogs_StockAllocation_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_StockAllocation_DetailsItems = new List<W01_ActivityLogs_StockAllocation_DetailsModel.W01_ActivityLogs_StockAllocation_DetailsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.Module_StockAllocations
                                             where p.DateOfStockAllocation.Date >= model.FromDate.Value
                                             && p.DateOfStockAllocation.Date <= model.ToDate.Value
                                             select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ResponsibleUserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActivityTypeID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }

            var tTypes = db.A08_Task_Types.ToList();
            var tasks = (from p in db.A08_Tasks
                         select p).ToList();
            tasks = (from p in tasks
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A08_Task).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var fTypes = db.A09_Flags_Types.ToList();
            var flags = (from p in db.A09_Flags
                         select p).ToList();
            flags = (from p in flags
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A09_Flag).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var oTypes = db.E01_BuildingOnboardingTask_Types.ToList();
            var onboardingTasks = (from p in db.E01_BuildingOnboardingTasks
                                   select p).ToList();
            onboardingTasks = (from p in onboardingTasks
                               where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.E01_BuildingOnboardingTask).Select(c => c.ActivityID).Contains(p.ID)
                               select p).ToList();

            SqlCommand cmd_sp_GetA08_TasksLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA08_TasksLatestComment).Fill(tbl_sp_GetA08_TasksLatestComment);

            SqlCommand cmd_sp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA09_FlagsLatestComment).Fill(tbl_sp_GetA09_FlagsLatestComment);

            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var opProf = opProfs.Where(p => p.UserID == module.ResponsibleUserID).SingleOrDefault();

                W01_ActivityLogs_StockAllocation_DetailsModel.W01_ActivityLogs_StockAllocation_DetailsItem item = new W01_ActivityLogs_StockAllocation_DetailsModel.W01_ActivityLogs_StockAllocation_DetailsItem()
                {
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ResponsibleUserID = module.ResponsibleUserID,
                    ActivityTypeID = module.ActivityTypeID,
                    DateOfStockAllocation = module.DateOfStockAllocation,
                    ActivityID = module.ActivityID,
                    CreatedByUserID = module.CreatedByUserID,
                    DateCreated = module.DateCreated,
                    DescriptionOfStockAllocation = module.DescriptionOfStockAllocation,
                    ID = module.ID,
                    IsDeleted = module.IsDeleted,
                    ActivityHeading = "",
                    ActivityURL = "",
                    LatestComment = "",
                };

                switch (module.ActivityTypeID)
                {
                    case (int)ActivityTypeEnum.A09_Flag:
                        var linkedFlag = flags.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedFlag != null)
                        {
                            item.ActivityURL = $"/operational/A09_Flags/A09_Flags_CompanyReview/{linkedFlag.FlagTypeID}/{linkedFlag.ID}";
                            item.Company = linkedFlag.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = fTypes.Where(p => p.ID == linkedFlag.FlagTypeID).SingleOrDefault().FlagTypeName;
                            var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.A08_Task:
                        var linkedTask = tasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedTask != null)
                        {
                            item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask.TaskTypeID}/{linkedTask.ID}";
                            item.Company = linkedTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = tTypes.Where(p => p.ID == linkedTask.TaskTypeID).SingleOrDefault().Heading;
                            var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.E01_BuildingOnboardingTask:
                        var linkedoTask = onboardingTasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedoTask != null)
                        {
                            item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask.TaskTypeID}/{linkedoTask.ID}";
                            item.Company = linkedoTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = oTypes.Where(p => p.ID == linkedoTask.TaskTypeID).SingleOrDefault().Heading;
                        }
                        break;
                }

                //decimal minSpent = 0;
                //if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                //{
                //    minSpent += item.VehicleDistanceKm;
                //}
                //else if (Request.Query["DisplayAs"].ToString() == "2")
                //{
                //    if (item.RatePerKm.HasValue)
                //    {
                //        minSpent += Convert.ToDecimal(item.VehicleDistanceKm) * Convert.ToDecimal(item.RatePerKm);
                //    }
                //}
                item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(item.DateOfStockAllocation.Date, 1));


                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_StockAllocation_DetailsItems.Add(item);
            }


            model.W01_ActivityLogs_StockAllocation_DetailsItems = model.W01_ActivityLogs_StockAllocation_DetailsItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_StockAllocation_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_InvoiceAllocation_Summary")]
        public async Task<IActionResult> W01_ActivityLogs_InvoiceAllocation_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_InvoiceAllocation_SummaryModel model = new W01_ActivityLogs_InvoiceAllocation_SummaryModel()
            {
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_InvoiceAllocation_SummaryItems = new List<W01_ActivityLogs_InvoiceAllocation_SummaryModel.W01_ActivityLogs_InvoiceAllocation_SummaryItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            SqlCommand cmd_sp_Module_InvoiceAllocations = new SqlCommand($"exec [sp_Module_InvoiceAllocations] '{model.FromDate.Value.ToDateShort()}', '{model.ToDate.Value.ToDateShort()}'", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tbl_sp_Module_InvoiceAllocations = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_Module_InvoiceAllocations).Fill(tbl_sp_Module_InvoiceAllocations);

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() != user.Id)
                {
                    continue;
                }
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                var userRows = tbl_sp_Module_InvoiceAllocations.Select($"[ResponsibleUserID] = '{user.Id}'");
                if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                {
                    userRows = tbl_sp_Module_InvoiceAllocations.Select($"[ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                }
                if (userRows.Length == 0)
                    continue;
                W01_ActivityLogs_InvoiceAllocation_SummaryModel.W01_ActivityLogs_InvoiceAllocation_SummaryItem item = new W01_ActivityLogs_InvoiceAllocation_SummaryModel.W01_ActivityLogs_InvoiceAllocation_SummaryItem()
                {
                    ActivityType = string.IsNullOrEmpty(Request.Query["ActivityType"]) ? "All Activity Types" : ((ActivityTypeEnum)Convert.ToInt32(Request.Query["ActivityType"])).GetDescription(),
                    Company = "All Companies",
                    InvoiceSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ActivityTypeID = Request.Query["ActivityType"],
                    CompanyID = Request.Query["Company"],
                    UserID = user.Id,
                };
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    item.Company = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault().Name;
                }

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var dateRows = tbl_sp_Module_InvoiceAllocations.Select($"[DateOfInvoiceAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}'");
                    if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
                    {
                        dateRows = tbl_sp_Module_InvoiceAllocations.Select($"[DateOfInvoiceAllocation] = '{current.ToString("yyyy-MM-dd")}' And [ResponsibleUserID] = '{user.Id}' And [ActivityTypeID] = '{Request.Query["ActivityType"]}'");
                    }
                    decimal minSpent = 0;

                    foreach (var row in dateRows)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Company"]))
                        {
                            if (row["CompanyID"] == DBNull.Value || row["CompanyID"] != Request.Query["Company"])
                                continue;
                        }
                        minSpent++;
                    }

                    item.InvoiceSpent.Add(new KeyValuePair<DateTime, decimal>(current, minSpent));

                    current = current.AddDays(1);
                }

                if (item.InvoiceSpent.Count > 0)
                    model.W01_ActivityLogs_InvoiceAllocation_SummaryItems.Add(item);
            }


            model.W01_ActivityLogs_InvoiceAllocation_SummaryItems = model.W01_ActivityLogs_InvoiceAllocation_SummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_InvoiceAllocation_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_InvoiceAllocation_Details")]
        public async Task<IActionResult> W01_ActivityLogs_InvoiceAllocation_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_InvoiceAllocation_DetailsModel model = new W01_ActivityLogs_InvoiceAllocation_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                DisplayAs = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "KM", Selected = string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Rate", Selected = !string.IsNullOrEmpty(Request.Query["DisplayAs"]) && Request.Query["DisplayAs"].ToString() == "2" },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_InvoiceAllocation_DetailsItems = new List<W01_ActivityLogs_InvoiceAllocation_DetailsModel.W01_ActivityLogs_InvoiceAllocation_DetailsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((ActivityTypeEnum[])Enum.GetValues(typeof(ActivityTypeEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.Module_InvoiceAllocations
                                             where p.DateOfInvoiceAllocation.Date >= model.FromDate.Value
                                             && p.DateOfInvoiceAllocation.Date <= model.ToDate.Value
                                             select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ResponsibleUserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActivityTypeID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }

            var tTypes = db.A08_Task_Types.ToList();
            var tasks = (from p in db.A08_Tasks
                         select p).ToList();
            tasks = (from p in tasks
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A08_Task).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var fTypes = db.A09_Flags_Types.ToList();
            var flags = (from p in db.A09_Flags
                         select p).ToList();
            flags = (from p in flags
                     where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.A09_Flag).Select(c => c.ActivityID).Contains(p.ID)
                     select p).ToList();
            var oTypes = db.E01_BuildingOnboardingTask_Types.ToList();
            var onboardingTasks = (from p in db.E01_BuildingOnboardingTasks
                                   select p).ToList();
            onboardingTasks = (from p in onboardingTasks
                               where module_TimeOfWorkPlanneds.Where(c => c.ActivityTypeID == (int)ActivityTypeEnum.E01_BuildingOnboardingTask).Select(c => c.ActivityID).Contains(p.ID)
                               select p).ToList();

            SqlCommand cmd_sp_GetA08_TasksLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA08_TasksLatestComment).Fill(tbl_sp_GetA08_TasksLatestComment);

            SqlCommand cmd_sp_GetA09_FlagsLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tbl_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(cmd_sp_GetA09_FlagsLatestComment).Fill(tbl_sp_GetA09_FlagsLatestComment);

            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var opProf = opProfs.Where(p => p.UserID == module.ResponsibleUserID).SingleOrDefault();

                W01_ActivityLogs_InvoiceAllocation_DetailsModel.W01_ActivityLogs_InvoiceAllocation_DetailsItem item = new W01_ActivityLogs_InvoiceAllocation_DetailsModel.W01_ActivityLogs_InvoiceAllocation_DetailsItem()
                {
                    Company = "All Companies",
                    TimeSpent = new List<KeyValuePair<DateTime, decimal>>(),
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    ResponsibleUserID = module.ResponsibleUserID,
                    ActivityTypeID = module.ActivityTypeID,
                    DateOfInvoiceAllocation = module.DateOfInvoiceAllocation,
                    ActivityID = module.ActivityID,
                    CreatedByUserID = module.CreatedByUserID,
                    DateCreated = module.DateCreated,
                    DescriptionOfInvoiceAllocation = module.DescriptionOfInvoiceAllocation,
                    ID = module.ID,
                    IsDeleted = module.IsDeleted,
                    ActivityHeading = "",
                    ActivityURL = "",
                    LatestComment = "",
                };

                switch (module.ActivityTypeID)
                {
                    case (int)ActivityTypeEnum.A09_Flag:
                        var linkedFlag = flags.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedFlag != null)
                        {
                            item.ActivityURL = $"/operational/A09_Flags/A09_Flags_CompanyReview/{linkedFlag.FlagTypeID}/{linkedFlag.ID}";
                            item.Company = linkedFlag.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedFlag.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = fTypes.Where(p => p.ID == linkedFlag.FlagTypeID).SingleOrDefault().FlagTypeName;
                            var latestCommentResults = tbl_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{linkedFlag.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.A08_Task:
                        var linkedTask = tasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedTask != null)
                        {
                            item.ActivityURL = $"/operational/A08_Tasks/A08_Task_Review/{linkedTask.TaskTypeID}/{linkedTask.ID}";
                            item.Company = linkedTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = tTypes.Where(p => p.ID == linkedTask.TaskTypeID).SingleOrDefault().Heading;
                            var latestCommentResults = tbl_sp_GetA08_TasksLatestComment.Select($"[TaskID] = '{linkedTask.ID}'");
                            if (latestCommentResults.Length > 0)
                            {
                                string latestCommentUserUsername = "";
                                var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                                if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                                    latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                                item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                            }
                        }
                        break;
                    case (int)ActivityTypeEnum.E01_BuildingOnboardingTask:
                        var linkedoTask = onboardingTasks.Where(p => p.ID == module.ActivityID).SingleOrDefault();
                        if (linkedoTask != null)
                        {
                            item.ActivityURL = $"/operational/E01_BuildingOnboarding/E01_BuildingOnboardingTask_Review/{linkedoTask.TaskTypeID}/{linkedoTask.ID}";
                            item.Company = linkedoTask.CompanyID.HasValue && companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault() != null ? companies.Where(p => p.CompanyID == linkedoTask.CompanyID.Value).SingleOrDefault().Name : $"";
                            item.ActivityHeading = oTypes.Where(p => p.ID == linkedoTask.TaskTypeID).SingleOrDefault().Heading;
                        }
                        break;
                }

                //decimal minSpent = 0;
                //if (string.IsNullOrEmpty(Request.Query["DisplayAs"]) || Request.Query["DisplayAs"].ToString() == "1")
                //{
                //    minSpent += item.VehicleDistanceKm;
                //}
                //else if (Request.Query["DisplayAs"].ToString() == "2")
                //{
                //    if (item.RatePerKm.HasValue)
                //    {
                //        minSpent += Convert.ToDecimal(item.VehicleDistanceKm) * Convert.ToDecimal(item.RatePerKm);
                //    }
                //}
                item.TimeSpent.Add(new KeyValuePair<DateTime, decimal>(item.DateOfInvoiceAllocation.Date, 1));


                if (item.TimeSpent.Count > 0)
                    model.W01_ActivityLogs_InvoiceAllocation_DetailsItems.Add(item);
            }


            model.W01_ActivityLogs_InvoiceAllocation_DetailsItems = model.W01_ActivityLogs_InvoiceAllocation_DetailsItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_InvoiceAllocation_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_UserActivity_Details")]
        public async Task<IActionResult> W01_ActivityLogs_UserActivity_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.W01_ActivityLogs_UserActivity_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.W01_ActivityLogs_UserActivity_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            W01_ActivityLogs_UserActivity_DetailsModel model = new W01_ActivityLogs_UserActivity_DetailsModel()
            {
                FromDate = DateTime.Now.AddDays(-7).Date,
                ToDate = DateTime.Now.Date,
                ActivityType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Activity Types]", Selected = string.IsNullOrEmpty(Request.Query["ActivityType"]) },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"]) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) }
                },
                W01_ActivityLogs_UserActivity_DetailsItems = new List<W01_ActivityLogs_UserActivity_DetailsModel.W01_ActivityLogs_UserActivity_DetailsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());
            model.Company = model.Company.OrderBy(p => p.Text).ToList();


            model.ActivityType.AddRange((from p in ((LogActionEnum[])Enum.GetValues(typeof(LogActionEnum)))
                                         orderby p.GetDescription()
                                         select new SelectListItem()
                                         {
                                             Text = p.GetDescription(),
                                             Value = ((int)p).ToString(),
                                             Selected = Request.Query["ActivityType"] == ((int)p).ToString() ? true : false,
                                         }).ToList());
            model.ActivityType = model.ActivityType.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = (from p in db.ActivityLogs
                                             where p.DateStarted.Date >= model.FromDate.Value
                                             && p.DateStarted.Date <= model.ToDate.Value
                                             select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.UserID == Request.Query["ResponsibleUser"].ToString()
                                             select p).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ActivityType"]))
            {
                module_TimeOfWorkPlanneds = (from p in module_TimeOfWorkPlanneds
                                             where p.ActionID == Convert.ToInt32(Request.Query["ActivityType"])
                                             select p).ToList();
            }
            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select p).ToList();

            var users = (from p in db.Users
                         where !p.IsDeleted
                         select p).ToList();

            foreach (var module in module_TimeOfWorkPlanneds)
            {
                var customer = customers.Where(p => p.UserID == module.UserID).SingleOrDefault();
                var user = users.Where(p => p.Id == module.UserID).SingleOrDefault();

                W01_ActivityLogs_UserActivity_DetailsModel.W01_ActivityLogs_UserActivity_DetailsItem item = new W01_ActivityLogs_UserActivity_DetailsModel.W01_ActivityLogs_UserActivity_DetailsItem()
                {
                    Company = customer != null ? companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault().Name : "-",
                    UserName = $"{(customer != null ? customer.FullName : (user != null ? user.UserName : "-"))}",
                    ID = module.ID,
                    UserID = module.UserID,
                    DateStarted = module.DateStarted,
                    DateEnded = module.DateEnded,
                    Request = module.Request,
                    Response = module.Response,
                    ActionID = module.ActionID,
                    SourceID = module.SourceID,
                    SourceIP = module.SourceIP,
                    URL = module.URL,
                };


                model.W01_ActivityLogs_UserActivity_DetailsItems.Add(item);
            }

            Dictionary<string, string> usersUsed = new Dictionary<string, string>();
            foreach (var item in model.W01_ActivityLogs_UserActivity_DetailsItems)
            {
                if (usersUsed.ContainsKey(item.UserID))
                    continue;

                usersUsed.Add(item.UserID, item.UserName);
            }

            foreach (var user in usersUsed.OrderBy(p => p.Value))
            {
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Key, Text = user.Value, Selected = Request.Query["ResponsibleUser"] == user.Key ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            model.W01_ActivityLogs_UserActivity_DetailsItems = model.W01_ActivityLogs_UserActivity_DetailsItems.OrderBy(p => p.DateStarted).ToList();

            return View("~/Views/Operational/W01_ActivityLogs/W01_ActivityLogs_UserActivity_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/W01_ActivityLogs/W01_ActivityLogs_UserActivity_SourceIP/{ID}")]
        public async Task<IActionResult> W01_ActivityLogs_UserActivity_SourceIP(int ID)
        {
            //var db = new MyVoltageDbContext(_options);

            //var item = db.ActivityLogs.Where(p => p.ID == ID).SingleOrDefault();

            //if (item != null)
            //{
            //}

            return Content("");
        }

    }
}
