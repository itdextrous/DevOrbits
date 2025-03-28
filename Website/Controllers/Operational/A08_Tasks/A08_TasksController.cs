using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ClosedXML.Report.Utils;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
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
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A08_Tasks.A08_TasksModels;
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
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.A08_Tasks
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A08_TasksController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpService _httpService;

        public A08_TasksController(
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpService httpService
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _httpService = httpService;
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Company_Summary")]
        public async Task<IActionResult> A08_Tasks_Company_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Tasks_Company_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Tasks_Company_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A08_Tasks_Company_SummaryModel model = new A08_Tasks_Company_SummaryModel()
            {
                A08_Tasks_CompanySummaryItems = new List<A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem>(),
                FromDate = DateTime.Now.AddMonths(-1).Date,
                ToDate = DateTime.Now.Date,
                A08_Tasks_Company_SummaryStatusItems = new List<A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem>(),
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"].ToString()) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"].ToString()) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"].ToString()) }
                },
            };

            var db = new MyVoltageDbContext(_options);
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var opProfs = db.OperationalProfiles.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"].ToString());
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"].ToString());
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

            List<Data.A08_Task> a09_Tasks = (from p in db.A08_Tasks
                                             where p.DueDate.HasValue
                                             && p.DueDate.Value.Date >= model.FromDate.Value.Date
                                             && p.DueDate.Value.Date <= model.ToDate.Value.Date
                                             select p).ToList();

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem item = new A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem()
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
                model.A08_Tasks_Company_SummaryStatusItems.Add(item);
            }

            #region No Company

            var noCompanyItems = a09_Tasks.Where(p => !p.CompanyID.HasValue).ToList();

            if (noCompanyItems.Count > 0)
            {
                A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem item = new A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem()
                {
                    CompanyID = 0,
                    CompanyName = "None",
                    A08_Tasks_Company_SummaryItemStatuses = new List<A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus>(),
                };

                foreach (var status in siteAdmin_Statuses)
                {
                    if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                        continue;
                    A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus itemStatus = new A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus()
                    {
                        Count = noCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                        StatusID = status.ID,
                        StatusGroupID = status.StatusGroupID,
                    };

                    item.A08_Tasks_Company_SummaryItemStatuses.Add(itemStatus);
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

                model.A08_Tasks_CompanySummaryItems.Add(item);
            }

            #endregion

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var thisCompanyItems = a09_Tasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

                if (thisCompanyItems.Count > 0)
                {
                    A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem item = new A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        A08_Tasks_Company_SummaryItemStatuses = new List<A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                            continue;
                        A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus itemStatus = new A08_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus()
                        {
                            Count = thisCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                            StatusGroupID = status.StatusGroupID,
                        };

                        item.A08_Tasks_Company_SummaryItemStatuses.Add(itemStatus);
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

                    model.A08_Tasks_CompanySummaryItems.Add(item);
                }

            }



            return View("~/Views/Operational/A08_Tasks/A08_Tasks_Company_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Company_Details")]
        public async Task<IActionResult> A08_Tasks_Company_Details()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_Company_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Company_Results")]
        public async Task<IActionResult> A08_Tasks_Company_Results()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_Company_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_User_Summary")]
        public async Task<IActionResult> A08_Tasks_User_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Tasks_User_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Tasks_User_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A08_Tasks_User_SummaryModel model = new A08_Tasks_User_SummaryModel()
            {
                A08_Tasks_UserSummaryItems = new List<A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem>(),
                A08_Tasks_User_SummaryStatusItems = new List<A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"].ToString()) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"].ToString()) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"].ToString()) }
                },
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var opProfs = dbCache.OperationalProfiles;
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
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


            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
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
                A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem item = new A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem()
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
                model.A08_Tasks_User_SummaryStatusItems.Add(item);
            }

            List<Data.A08_Task> a09_Tasks = (from p in dbCache.A08_Tasks
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
                    var userTemp = db.Users.Where(p => p.Id == userID).SingleOrDefault();
                    userName = userTemp != null ? userTemp.UserName : null;
                }

                var thisUserItems = a09_Tasks.Where(p => p.ResponsibleUserID == userID).ToList();

                if (thisUserItems.Count > 0)
                {
                    A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem item = new A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem()
                    {
                        UserID = userID,
                        UserName = userName,
                        A08_Tasks_User_SummaryItemStatuses = new List<A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                            continue;
                        A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus itemStatus = new A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus()
                        {
                            Count = thisUserItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                            StatusGroupID = status.StatusGroupID,
                        };

                        item.A08_Tasks_User_SummaryItemStatuses.Add(itemStatus);
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

                    model.A08_Tasks_UserSummaryItems.Add(item);
                }

            }



            model.A08_Tasks_UserSummaryItems = model.A08_Tasks_UserSummaryItems.OrderBy(p => p.UserName).ToList();
            model.A08_Tasks_UserSummaryItems = model.A08_Tasks_UserSummaryItems.Where(p => p.UserName != null).ToList();
            return View("~/Views/Operational/A08_Tasks/A08_Tasks_User_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_User_Details")]
        public async Task<IActionResult> A08_Tasks_User_Details()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_User_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_User_Results")]
        public async Task<IActionResult> A08_Tasks_User_Results()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_User_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Type_Summary")]
        public async Task<IActionResult> A08_Tasks_Type_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Tasks_Type_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Tasks_Type_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A08_Tasks_Type_SummaryModel model = new A08_Tasks_Type_SummaryModel()
            {
                A08_Tasks_TypeSummaryItems = new List<A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                A08_Tasks_Type_SummaryStatusItems = new List<A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryStatusItem>(),
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"].ToString()) }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Companies]", Selected = string.IsNullOrEmpty(Request.Query["Company"].ToString()) }
                },
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Responsible Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()) }
                },
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Reporting To Users]", Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()) }
                },
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Priorities]", Selected = string.IsNullOrEmpty(Request.Query["Priority"].ToString()) }
                },
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var opProfs = dbCache.OperationalProfiles;
            var a08_Task_Types = dbCache.A08_Tasks_Types;

            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
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


            foreach (var status in siteAdmin_Statuses)
            {
                A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryStatusItem item = new A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryStatusItem()
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
                model.A08_Tasks_Type_SummaryStatusItems.Add(item);
            }

            List<Data.A08_Task> a09_Tasks = (from p in dbCache.A08_Tasks
                                             where p.DueDate.HasValue
                                             && p.DueDate.Value.Date >= model.FromDate.Value.Date
                                             && p.DueDate.Value.Date <= model.ToDate.Value.Date
                                             select p).ToList();

            foreach (var type in a08_Task_Types)
            {
                var thisTypeItems = a09_Tasks.Where(p => p.TaskTypeID == type.ID).ToList();

                if (thisTypeItems.Count > 0)
                {
                    A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem item = new A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem()
                    {
                        TypeID = type.ID,
                        TypeName = !string.IsNullOrEmpty(type.Identifier) ? $"{type.Identifier} - {type.Heading}" : type.Heading,
                        A08_Tasks_Type_SummaryItemStatuses = new List<A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem.A08_Tasks_Type_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                            continue;
                        A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem.A08_Tasks_Type_SummaryItemStatus itemStatus = new A08_Tasks_Type_SummaryModel.A08_Tasks_Type_SummaryItem.A08_Tasks_Type_SummaryItemStatus()
                        {
                            Count = thisTypeItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                            StatusGroupID = status.StatusGroupID,
                        };

                        item.A08_Tasks_Type_SummaryItemStatuses.Add(itemStatus);
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

                    model.A08_Tasks_TypeSummaryItems.Add(item);
                }

            }


            model.A08_Tasks_TypeSummaryItems = model.A08_Tasks_TypeSummaryItems.OrderBy(p => p.TypeName).ToList();
            return View("~/Views/Operational/A08_Tasks/A08_Tasks_Type_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Type_Details")]
        public async Task<IActionResult> A08_Tasks_Type_Details()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Type_Results")]
        public async Task<IActionResult> A08_Tasks_Type_Results()
        {
            return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Review/{taskTypeID}/{taskID}")]
        public async Task<IActionResult> A08_Task_Review(int taskTypeID, int taskID)
        {
            return Redirect($"/operational/changeTaskID/{taskTypeID}/{taskID}?R=/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Review")]
        public async Task<IActionResult> A08_Task_Review()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

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

            A08_Task_ReviewModel model = new A08_Task_ReviewModel()
            {
                ReportingToUser = new List<SelectListItem>(),
                ResponsibleUser = new List<SelectListItem>(),
            };

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.A08_Tasks.Where(p => p.ID == _operationalProvider.TaskSelectedTaskID).SingleOrDefault();
            var tType = db.A08_Task_Types.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

            if (task == null)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            #region Populate Task

            model.NotificationsActive = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = task.NotificationsActive.HasValue && task.NotificationsActive.Value },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = !task.NotificationsActive.HasValue || !task.NotificationsActive.Value },
            };

            model.CustomerNo = task.CustomerNo;
            model.MeterSerialNumber = task.MeterSerialNumber;
            model.Identifier = task.Identifier;

            if (!task.DueDate.HasValue)
            {
                task.DueDate = task.DateCreated.AddWorkdays(tType.MinRequiredToClear);
                db.Update(task);
                db.SaveChanges();
            }

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper())).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = task.ResponsibleUserID == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = task.ReportingToUserID == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            var responsibleUserName = _userManager.FindByIdAsync(task.ResponsibleUserID).Result;
            var reportingToUserName = _userManager.FindByIdAsync(task.ReportingToUserID).Result;
            model.A08_Task = new A08_Task_ReviewModel.A08_Task_ReviewItem()
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
                A08_Tasks_TypeItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type()
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
                    A08_Task_TypeStatuses = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus>(),
                    ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                    A08_Task_TypeMeetingAgendaes = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda>(),
                    A08_Task_TypeStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus(),
                    StatusGroupName = SiteAdmin_StatusGroups.Where(p => p.ID == tType.StatusGroupID.Value).SingleOrDefault().StatusGroupName,
                    MeetingAgendaGroupName = SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == tType.MeetingAgendaGroupID.Value).SingleOrDefault().MeetingAgendaGroupName,
                    A08_Task_TypeMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda(),
                    FrequenciesCount = db.A08_Task_Type_Frequencies.Where(p => p.TaskTypeID == tType.ID).Count(),
                    BusinessDepartmentID = tType.BusinessDepartmentID,
                    DefaultMeetingAgendaID = tType.DefaultMeetingAgendaID,
                    DefaultStatusID = tType.DefaultStatusID,
                    DefautlMinPlanned = tType.DefautlMinPlanned,
                    Identifier = tType.Identifier,
                    BusinessDepartmentName = "",
                    BusinessPillarName = "",
                    IsDeleted = tType.IsDeleted,
                    MeetingAgendaGroupID = tType.MeetingAgendaGroupID,
                    ReportingToUserAccepted = tType.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                    ReportingToUserUsername = "",
                    ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                    ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                    ResponsibleUserUsername = "",
                    SecureAreaGroupID = tType.SecureAreaGroupID,
                    StatusGroupID = tType.StatusGroupID,
                    TaskClassificationID = tType.TaskClassificationID,
                    UpdateExistingTask = tType.UpdateExistingTask,
                    WorkflowGroupName = "",
                    HasComplianceCheck = tType.HasComplianceCheck,
                },
                ReportingToUserUsername = reportingToUserName != null ? reportingToUserName.UserName : "",
                ResponsibleUserUsername = responsibleUserName != null ? responsibleUserName.UserName : "",
                A08_Task_Review_ReassignLogItems = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem>(),
                A08_Task_Review_AttachmentItems = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_AttachmentItem>(),
                A08_TaskStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskStatus(),
                A08_TaskMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskMeetingAgenda(),
                DueDate = task.DueDate,
                Level = task.Level,
                Module_TimeOfWorkPlanneds = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned>(),
                Module_TimeOfWorkAllocateds = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated>(),
                Module_TravelAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation>(),
                Module_StockAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation>(),
                Module_InvoiceAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation>(),
                CustomerNo = task.CustomerNo,
                MeterSerialNumber = task.MeterSerialNumber,
                Description = string.IsNullOrEmpty(task.Description) ? tType.Description : task.Description,
                WrikeCustomStatus = task.WrikeCustomStatus,
                WrikeID = task.WrikeID,
                WrikeSyncDate = task.WrikeSyncDate,
                BusinessDepartmentName = "",
                BusinessPillarName = "",
                WorkflowGroupName = "",
                Module_NonCompliances = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_NonCompliance>(),
                Identifier = task.Identifier,
                BusinessDepartmentID = task.BusinessDepartmentID,
                NotificationsActive = task.NotificationsActive,
                PriorityID = task.PriorityID,
                SecureAreaGroupID = task.SecureAreaGroupID,
            };

            if (task.SecureAreaGroupID.HasValue)
            {
                var workflow = db.WorkflowGroups.Where(p => p.ID == task.SecureAreaGroupID.Value).SingleOrDefault();
                if (workflow != null)
                {
                    model.A08_Task.WorkflowGroupName = workflow.WorkflowGroupName;
                    if (task.BusinessDepartmentID.HasValue)
                    {
                        var department = db.BusinessDepartments.Where(p => p.ID == task.BusinessDepartmentID.Value).SingleOrDefault();
                        var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                        model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                    }
                    else if (workflow.BusinessDepartmentID.HasValue)
                    {
                        var department = db.BusinessDepartments.Where(p => p.ID == workflow.BusinessDepartmentID.Value).SingleOrDefault();
                        var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                        model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }
            }

            if (tType.SecureAreaGroupID.HasValue)
            {
                var workflow = db.WorkflowGroups.Where(p => p.ID == tType.SecureAreaGroupID.Value).SingleOrDefault();
                if (workflow != null)
                {
                    model.A08_Task.A08_Tasks_TypeItem.WorkflowGroupName = workflow.WorkflowGroupName;
                    if (string.IsNullOrEmpty(model.A08_Task.WorkflowGroupName))
                        model.A08_Task.WorkflowGroupName = workflow.WorkflowGroupName;
                    if (workflow.BusinessDepartmentID.HasValue)
                    {
                        var department = db.BusinessDepartments.Where(p => p.ID == workflow.BusinessDepartmentID.Value).SingleOrDefault();
                        var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();

                        model.A08_Task.A08_Tasks_TypeItem.BusinessDepartmentName = department.BusinessDepartmentName;
                        model.A08_Task.A08_Tasks_TypeItem.BusinessPillarName = pillar.BusinessPillarName;

                        if (string.IsNullOrEmpty(model.A08_Task.BusinessDepartmentName))
                            model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                        if (string.IsNullOrEmpty(model.A08_Task.BusinessPillarName))
                            model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }
            }

            #region Workflows

            var workflowGroups = db.WorkflowGroups.ToList();
            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = task.SecureAreaGroupID.HasValue && task.SecureAreaGroupID == p.ID,
                                     }).ToList();

            model.WorkflowGroupItems = new List<A08_Task_ReviewModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<A08_Task_ReviewModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<A08_Task_ReviewModel.BusinessPillarItem>();

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new A08_Task_ReviewModel.BusinessPillarItem()
                                         {
                                             DisplayName = p.BusinessPillarName,
                                             ID = p.ID,
                                         }).ToList();

            model.BusinessPillar.AddRange((from p in businessPillars
                                           select new SelectListItem()
                                           {
                                               Text = p.BusinessPillarName,
                                               Value = p.ID.ToString(),
                                           }).ToList());

            var businessDepartments = db.BusinessDepartments.ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new A08_Task_ReviewModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new A08_Task_ReviewModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            model.Description = string.IsNullOrEmpty(task.Description) ? tType.Description : task.Description;
            model.DueDate = task.DueDate.Value;

            var taskStatus = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
            if (taskStatus != null)
            {
                model.A08_Task.A08_TaskStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskStatus()
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

            var taskTypeStatus = siteAdmin_Statuses.Where(p => p.ID == model.A08_Task.A08_Tasks_TypeItem.DefaultStatusID.Value).SingleOrDefault();
            if (taskTypeStatus != null)
            {
                model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus()
                {
                    CreatedByID = taskTypeStatus.CreatedByID,
                    StatusActionID = taskTypeStatus.StatusActionID,
                    ID = taskTypeStatus.ID,
                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == taskTypeStatus.StatusActionID).SingleOrDefault().StatusActionName,
                    CreatedDate = taskTypeStatus.CreatedDate,
                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == taskTypeStatus.StatusGroupID).SingleOrDefault().StatusGroupName,
                    IsDeleted = taskTypeStatus.IsDeleted,
                    IsResolvedStatus = taskTypeStatus.IsResolvedStatus,
                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == taskTypeStatus.StatusReportingID).SingleOrDefault().StatusReportingName,
                    StatusGroupID = taskTypeStatus.StatusGroupID,
                    StatusReportingID = taskTypeStatus.StatusReportingID,
                    UpdatedByID = taskTypeStatus.UpdatedByID,
                    UpdatedDate = taskTypeStatus.UpdatedDate,
                };
            }

            if (task.MeetingAgendaID.HasValue)
            {
                var taskMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == task.MeetingAgendaID).SingleOrDefault();
                if (taskMeetingAgenda != null)
                {
                    model.A08_Task.A08_TaskMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskMeetingAgenda()
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
            }

            if (tType.DefaultMeetingAgendaID.HasValue)
            {
                var taskMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == tType.DefaultMeetingAgendaID.Value).SingleOrDefault();
                if (taskMeetingAgenda != null)
                {
                    model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda()
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
            }

            var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
            if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                model.A08_Task.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

            var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
            if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                model.A08_Task.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

            var reportingToUserUserTaskType = opProfs.Where(p => p.UserID == model.A08_Task.A08_Tasks_TypeItem.ReportingToUserID).SingleOrDefault();
            if (reportingToUserUserTaskType != null && !string.IsNullOrEmpty(reportingToUserUserTaskType.FirstName))
                model.A08_Task.A08_Tasks_TypeItem.ReportingToUserUsername = $"{reportingToUserUserTaskType.FirstName} {reportingToUserUserTaskType.LastName}";

            var responsibleUserTaskType = opProfs.Where(p => p.UserID == model.A08_Task.A08_Tasks_TypeItem.ResponsibleUserID).SingleOrDefault();
            if (responsibleUserTaskType != null && !string.IsNullOrEmpty(responsibleUserTaskType.FirstName))
                model.A08_Task.A08_Tasks_TypeItem.ResponsibleUserUsername = $"{responsibleUserTaskType.FirstName} {responsibleUserTaskType.LastName}";

            if (tType.StatusGroupID.HasValue)
            {
                foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus a08_Task_TypeStatus = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus()
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
                    model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeStatuses.Add(a08_Task_TypeStatus);
                }
            }

            if (tType.MeetingAgendaGroupID.HasValue)
            {
                foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes.Where(p => p.MeetingAgendaGroupID == tType.MeetingAgendaGroupID.Value))
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda a08_Task_TypeMeetingAgenda = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda()
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
                    model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeMeetingAgendaes.Add(a08_Task_TypeMeetingAgenda);
                }
            }

            #region Modules

            var reassignLogs = db.A08_Tasks_ReassignLogs.Where(p => p.TaskID == task.ID).ToList();
            foreach (var rLog in reassignLogs)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem reassignLogItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem()
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

                model.A08_Task.A08_Task_Review_ReassignLogItems.Add(reassignLogItem);
            }
            model.A08_Task.A08_Task_Review_ReassignLogItems = model.A08_Task.A08_Task_Review_ReassignLogItems.OrderByDescending(p => p.DateCreated).ToList();

            var attachments = db.A08_Tasks_Attachments.Where(p => p.TaskID == task.ID).ToList();
            foreach (var rLog in attachments)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_AttachmentItem attachmentItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_AttachmentItem()
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

                model.A08_Task.A08_Task_Review_AttachmentItems.Add(attachmentItem);
            }
            model.A08_Task.A08_Task_Review_AttachmentItems = model.A08_Task.A08_Task_Review_AttachmentItems.OrderByDescending(p => p.DateCreated).ToList();

            var module_TimeOfWorkPlanneds = db.Module_TimeOfWorkPlanneds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var timeOfWorkPlanned in module_TimeOfWorkPlanneds)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned()
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

                model.A08_Task.Module_TimeOfWorkPlanneds.Add(module_TimeOfWorkPlanned);
            }
            model.A08_Task.Module_TimeOfWorkPlanneds = model.A08_Task.Module_TimeOfWorkPlanneds.OrderByDescending(p => p.DateCreated).ToList();

            var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated()
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

                model.A08_Task.Module_TimeOfWorkAllocateds.Add(module_TimeOfWorkAllocated);
            }
            model.A08_Task.Module_TimeOfWorkAllocateds = model.A08_Task.Module_TimeOfWorkAllocateds.OrderByDescending(p => p.DateCreated).ToList();

            var module_TravelAllocations = db.Module_TravelAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var TravelAllocation in module_TravelAllocations)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation module_TravelAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation()
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

                model.A08_Task.Module_TravelAllocations.Add(module_TravelAllocation);
            }
            model.A08_Task.Module_TravelAllocations = model.A08_Task.Module_TravelAllocations.OrderByDescending(p => p.DateCreated).ToList();

            var module_StockAllocations = db.Module_StockAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var StockAllocation in module_StockAllocations)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation module_StockAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation()
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

                model.A08_Task.Module_StockAllocations.Add(module_StockAllocation);
            }
            model.A08_Task.Module_StockAllocations = model.A08_Task.Module_StockAllocations.OrderByDescending(p => p.DateCreated).ToList();

            var module_InvoiceAllocations = db.Module_InvoiceAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var InvoiceAllocation in module_InvoiceAllocations)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation module_InvoiceAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation()
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

                model.A08_Task.Module_InvoiceAllocations.Add(module_InvoiceAllocation);
            }
            model.A08_Task.Module_InvoiceAllocations = model.A08_Task.Module_InvoiceAllocations.OrderByDescending(p => p.DateCreated).ToList();

            var module_NonCompliances = db.Module_NonCompliances.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
            foreach (var NonCompliance in module_NonCompliances)
            {
                A08_Task_ReviewModel.A08_Task_ReviewItem.Module_NonCompliance module_NonCompliance = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_NonCompliance()
                {
                    ActivityID = NonCompliance.ActivityID,
                    ResponsibleUsername = "",
                    ResponsibleUserID = NonCompliance.ResponsibleUserID,
                    ActivityTypeID = NonCompliance.ActivityTypeID,
                    DateCreated = NonCompliance.DateCreated,
                    ID = NonCompliance.ID,
                    EnforcerComment = NonCompliance.EnforcerComment,
                    EnforcerCommentDate = NonCompliance.EnforcerCommentDate,
                    EnforcerUserID = NonCompliance.EnforcerUserID,
                    EnforcerCommentUsername = "",
                    HasBeenResolved = NonCompliance.HasBeenResolved,
                    UserComment = NonCompliance.UserComment,
                    UserCommentDate = NonCompliance.UserCommentDate,
                    UserCommentID = NonCompliance.UserComment,
                    UserCommentUsername = "",
                };

                var responsibleUserNonCompliance = opProfs.Where(p => p.UserID == NonCompliance.ResponsibleUserID).SingleOrDefault();
                if (responsibleUserNonCompliance != null && !string.IsNullOrEmpty(responsibleUserNonCompliance.FirstName))
                    module_NonCompliance.ResponsibleUsername = $"{responsibleUserNonCompliance.FirstName} {responsibleUserNonCompliance.LastName}";

                if (!string.IsNullOrEmpty(NonCompliance.EnforcerUserID))
                {
                    var EnforcerCommentNonCompliance = opProfs.Where(p => p.UserID == NonCompliance.EnforcerUserID).SingleOrDefault();
                    if (EnforcerCommentNonCompliance != null && !string.IsNullOrEmpty(EnforcerCommentNonCompliance.FirstName))
                        module_NonCompliance.EnforcerCommentUsername = $"{EnforcerCommentNonCompliance.FirstName} {EnforcerCommentNonCompliance.LastName}";
                }
                if (!string.IsNullOrEmpty(NonCompliance.UserCommentID))
                {
                    var UserCommentNonCompliance = opProfs.Where(p => p.UserID == NonCompliance.UserCommentID).SingleOrDefault();
                    if (UserCommentNonCompliance != null && !string.IsNullOrEmpty(UserCommentNonCompliance.FirstName))
                        module_NonCompliance.UserCommentUsername = $"{UserCommentNonCompliance.FirstName} {UserCommentNonCompliance.LastName}";
                }

                model.A08_Task.Module_NonCompliances.Add(module_NonCompliance);
            }
            model.A08_Task.Module_NonCompliances = model.A08_Task.Module_NonCompliances.OrderByDescending(p => p.DateCreated).ToList();

            #endregion

            #endregion

            return View("~/Views/Operational/A08_Tasks/A08_Task_Review.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A08_Tasks/A08_Task_Review")]
        public async Task<IActionResult> A08_Task_Review(A08_Task_ReviewModel model) //update method
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

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
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper())).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"] == user.Id ? true : false });
            }

            model.NotificationsActive = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["NotificationsActive"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = Convert.ToBoolean(Request.Form["NotificationsActive"]) },
            };

            //model.Status = (from p in ((A08_Task.StatusEnum[])Enum.GetValues(typeof(A08_Task.StatusEnum)))
            //                select new SelectListItem()
            //                {
            //                    Text = p.GetDescription(),
            //                    Value = ((int)p).ToString(),
            //                    Selected = Request.Form["Status"] == ((int)p).ToString() ? true : false,
            //                }).ToList();

            var task = db.A08_Tasks.Where(p => p.ID == _operationalProvider.TaskSelectedTaskID).SingleOrDefault();
            var tType = db.A08_Task_Types.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

            var responsibles = model.ResponsibleUser.Where(x => x.Value == Request.Form["ResponsibleUser"]).FirstOrDefault();
            var reportingToUser = model.ReportingToUser.Where(x => x.Value == Request.Form["ReportingToUser"]).FirstOrDefault();

            var company = db.Companies.Where(x => x.CompanyID == task.CompanyID).FirstOrDefault();
            SiteAdmin_Priority priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault();

            var reportingToUserName = _userManager.FindByIdAsync(task.ReportingToUserID).Result;
            var responsibleUserName = _userManager.FindByIdAsync(task.ResponsibleUserID).Result;

            if (task != null)
            {
                #region Update Model

                model.A08_Task = new A08_Task_ReviewModel.A08_Task_ReviewItem()
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
                    A08_Tasks_TypeItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type()
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
                        TemplateNo = tType.ID,
                        A08_Task_TypeStatuses = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus>(),
                        A08_Task_TypeMeetingAgendaes = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda>(),
                        ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                        A08_Task_TypeMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda(),
                        A08_Task_TypeStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus(),
                    },
                    ReportingToUserUsername = reportingToUserName != null ? reportingToUserName.UserName : "",
                    ResponsibleUserUsername = responsibleUserName != null ? responsibleUserName.UserName : "",
                    A08_Task_Review_ReassignLogItems = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem>(),
                    A08_TaskStatusItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskStatus(),
                    A08_TaskMeetingAgendaItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_TaskMeetingAgenda(),
                    DueDate = task.DueDate,
                    A08_Task_Review_AttachmentItems = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_AttachmentItem>(),
                    Level = task.Level,
                    Module_TimeOfWorkAllocateds = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated>(),
                    Module_TimeOfWorkPlanneds = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned>(),
                    Module_TravelAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation>(),
                    Module_StockAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation>(),
                    Module_InvoiceAllocations = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation>(),
                    WrikeCustomStatus = task.WrikeCustomStatus,
                    WrikeID = task.WrikeID,
                    WrikeSyncDate = task.WrikeSyncDate,
                    BusinessDepartmentName = "",
                    BusinessPillarName = "",
                    WorkflowGroupName = "",
                    Module_NonCompliances = new List<A08_Task_ReviewModel.A08_Task_ReviewItem.Module_NonCompliance>(),
                };

                if (task.SecureAreaGroupID.HasValue)
                {
                    var workflow = db.WorkflowGroups.Where(p => p.ID == task.SecureAreaGroupID.Value).SingleOrDefault();
                    if (workflow != null)
                    {
                        model.A08_Task.WorkflowGroupName = workflow.WorkflowGroupName;
                        if (task.BusinessDepartmentID.HasValue)
                        {
                            var department = db.BusinessDepartments.Where(p => p.ID == task.BusinessDepartmentID.Value).SingleOrDefault();
                            var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                            model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                            model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                        }
                        else if (workflow.BusinessDepartmentID.HasValue)
                        {
                            var department = db.BusinessDepartments.Where(p => p.ID == workflow.BusinessDepartmentID.Value).SingleOrDefault();
                            var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                            model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                            model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                        }
                    }
                }

                if (tType.SecureAreaGroupID.HasValue)
                {
                    var workflow = db.WorkflowGroups.Where(p => p.ID == tType.SecureAreaGroupID.Value).SingleOrDefault();
                    if (workflow != null)
                    {
                        if (string.IsNullOrEmpty(model.A08_Task.WorkflowGroupName))
                            model.A08_Task.WorkflowGroupName = workflow.WorkflowGroupName;
                        if (workflow.BusinessDepartmentID.HasValue)
                        {
                            var department = db.BusinessDepartments.Where(p => p.ID == workflow.BusinessDepartmentID.Value).SingleOrDefault();
                            var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                            if (string.IsNullOrEmpty(model.A08_Task.BusinessDepartmentName))
                                model.A08_Task.BusinessDepartmentName = department.BusinessDepartmentName;
                            if (string.IsNullOrEmpty(model.A08_Task.BusinessPillarName))
                                model.A08_Task.BusinessPillarName = pillar.BusinessPillarName;
                        }
                    }
                }

                #region Workflows

                var workflowGroups = db.WorkflowGroups.ToList();
                model.WorkflowGroupID = (from p in workflowGroups
                                         orderby p.WorkflowGroupName
                                         select new SelectListItem()
                                         {
                                             Text = p.WorkflowGroupName,
                                             Value = p.ID.ToString(),
                                             Selected = Request.Form["WorkflowGroupID"].ToString() == p.ID.ToString() ? true : false,
                                         }).ToList();

                model.WorkflowGroupItems = new List<A08_Task_ReviewModel.WorkflowGroupItem>();
                model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
                model.BusinessDepartmentItems = new List<A08_Task_ReviewModel.BusinessDepartmentItem>();
                model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
                model.BusinessPillarItems = new List<A08_Task_ReviewModel.BusinessPillarItem>();

                var businessPillars = db.BusinessPillars.ToList();
                model.BusinessPillarItems = (from p in businessPillars
                                             select new A08_Task_ReviewModel.BusinessPillarItem()
                                             {
                                                 DisplayName = p.BusinessPillarName,
                                                 ID = p.ID,
                                             }).ToList();

                model.BusinessPillar.AddRange((from p in businessPillars
                                               select new SelectListItem()
                                               {
                                                   Text = p.BusinessPillarName,
                                                   Value = p.ID.ToString(),
                                               }).ToList());

                var businessDepartments = db.BusinessDepartments.ToList();
                model.BusinessDepartmentItems = (from p in businessDepartments
                                                 select new A08_Task_ReviewModel.BusinessDepartmentItem()
                                                 {
                                                     BusinessPillarID = p.BusinessPillarID,
                                                     DisplayName = p.BusinessDepartmentName,
                                                     ID = p.ID,
                                                 }).ToList();


                model.WorkflowGroupItems = (from p in workflowGroups
                                            select new A08_Task_ReviewModel.WorkflowGroupItem()
                                            {
                                                BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                                DisplayName = p.WorkflowGroupName,
                                                ID = p.ID,
                                            }).ToList();

                #endregion

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    model.A08_Task.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    model.A08_Task.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                if (tType.StatusGroupID.HasValue)
                {
                    foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
                    {
                        A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus a08_Task_TypeStatus = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeStatus()
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
                        model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeStatuses.Add(a08_Task_TypeStatus);
                    }
                }

                if (tType.MeetingAgendaGroupID.HasValue)
                {
                    foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes.Where(p => p.MeetingAgendaGroupID == tType.MeetingAgendaGroupID.Value))
                    {
                        A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda a08_Task_TypeMeetingAgenda = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Type.A08_Task_TypeMeetingAgenda()
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
                        model.A08_Task.A08_Tasks_TypeItem.A08_Task_TypeMeetingAgendaes.Add(a08_Task_TypeMeetingAgenda);
                    }
                }

                var reassignLogs = db.A08_Tasks_ReassignLogs.Where(p => p.TaskID == task.ID).ToList();
                foreach (var rLog in reassignLogs)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem reassignLogItem = new A08_Task_ReviewModel.A08_Task_ReviewItem.A08_Task_Review_ReassignLogItem()
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

                    model.A08_Task.A08_Task_Review_ReassignLogItems.Add(reassignLogItem);
                }
                model.A08_Task.A08_Task_Review_ReassignLogItems = model.A08_Task.A08_Task_Review_ReassignLogItems.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkPlanneds = db.Module_TimeOfWorkPlanneds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkPlanned in module_TimeOfWorkPlanneds)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkPlanned()
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

                    model.A08_Task.Module_TimeOfWorkPlanneds.Add(module_TimeOfWorkPlanned);
                }
                model.A08_Task.Module_TimeOfWorkPlanneds = model.A08_Task.Module_TimeOfWorkPlanneds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
                foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TimeOfWorkAllocated()
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

                    model.A08_Task.Module_TimeOfWorkAllocateds.Add(module_TimeOfWorkAllocated);
                }
                model.A08_Task.Module_TimeOfWorkAllocateds = model.A08_Task.Module_TimeOfWorkAllocateds.OrderByDescending(p => p.DateCreated).ToList();

                var module_TravelAllocations = db.Module_TravelAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
                foreach (var TravelAllocation in module_TravelAllocations)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation module_TravelAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_TravelAllocation()
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

                    model.A08_Task.Module_TravelAllocations.Add(module_TravelAllocation);
                }
                model.A08_Task.Module_TravelAllocations = model.A08_Task.Module_TravelAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_StockAllocations = db.Module_StockAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
                foreach (var StockAllocation in module_StockAllocations)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation module_StockAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_StockAllocation()
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

                    model.A08_Task.Module_StockAllocations.Add(module_StockAllocation);
                }
                model.A08_Task.Module_StockAllocations = model.A08_Task.Module_StockAllocations.OrderByDescending(p => p.DateCreated).ToList();

                var module_InvoiceAllocations = db.Module_InvoiceAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A08_Task && p.ActivityID == task.ID).ToList();
                foreach (var InvoiceAllocation in module_InvoiceAllocations)
                {
                    A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation module_InvoiceAllocation = new A08_Task_ReviewModel.A08_Task_ReviewItem.Module_InvoiceAllocation()
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

                    model.A08_Task.Module_InvoiceAllocations.Add(module_InvoiceAllocation);
                }
                model.A08_Task.Module_InvoiceAllocations = model.A08_Task.Module_InvoiceAllocations.OrderByDescending(p => p.DateCreated).ToList();

                #endregion

                #region Update Task
                string wrikeId;

                if (!string.IsNullOrEmpty(task.WrikeID))
                {
                    WrikeUpdateViewModel wrikeUpdateViewModel = new WrikeUpdateViewModel()
                    {
                        WrikeID = task.WrikeID,
                        Description = model.Description,
                        Status = model.WrikeCustomStatus,
                        Responsibles = responsibles.Text,
                        ResponsibleIds = responsibles.Value,
                        ReportingToUser = reportingToUser.Text,
                        DueDate = model.DueDate,
                        DateStarted = task.DateStarted != null ? task.DateStarted : task.DateCreated,
                        //Company = task.CompanyID,
                        Priority = priority.PriorityName,
                        CustomStatus = task.StatusID.ToString(),
                        Comments = model.Comments,
                        CustomerNo = model.CustomerNo,
                        MeterSerielNo = model.MeterSerialNumber,
                        NotificationActive = model.NotificationData,
                        WrikeSyncDate = DateTime.Now,
                        DefaultMinPlanned = tType.DefautlMinPlanned,
                        WorkflowGroupName = model.A08_Task.WorkflowGroupName,
                        BusinessDepartmentName = model.A08_Task.BusinessDepartmentName,
                        BusinessPillarName = model.A08_Task.BusinessPillarName,
                    };
                    var workflow = db.WorkflowGroups.Where(p => p.ID == tType.SecureAreaGroupID.Value).SingleOrDefault();
                    wrikeId = await _httpService.UpdateAsync(wrikeUpdateViewModel);
                }
                else
                {
                    var wrikeViewModel = new WrikeViewModel
                    {
                        ID = task.ID,
                        Title = tType.Heading,
                        Description = model.Description,
                        Status = task.WrikeCustomStatus != null ? task.WrikeCustomStatus : null,
                        Responsibles = responsibles.Text,
                        ResponsibleIds = responsibles.Value,
                        ReportingToUser = reportingToUser.Text,
                        DateStarted = task.DateStarted != null ? task.DateStarted : null,
                        DueDate = task.DueDate != null ? task.DueDate.Value : task.DateCreated.AddDays(2),
                        DateCreated = task.DateCreated,
                        Priority = priority != null ? priority.PriorityName : null,
                        Company = company != null ? company.Name : null,
                        WrikeSyncDate = DateTime.Now,
                        CustomerNo = model.CustomerNo,
                        MeterSerielNo = model.MeterSerialNumber,
                        NotificationActive = model.NotificationData,
                        WorkflowGroupName = model.A08_Task.WorkflowGroupName != null ? model.A08_Task.WorkflowGroupName : "[Not Linked]",
                        BusinessDepartmentName = model.A08_Task.BusinessDepartmentName != null ? model.A08_Task.BusinessDepartmentName : null,
                        BusinessPillarName = model.A08_Task.BusinessPillarName != null ? model.A08_Task.BusinessPillarName : null,
                    };
                    wrikeId = await _httpService.PostAsync(wrikeViewModel);
                }

                if (ModelState.IsValid)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.A08_Tasks.Where(p => p.ID == task.ID).SingleOrDefault();

                    itemToUpdate.WrikeID = wrikeId;
                    itemToUpdate.WrikeSyncDate = DateTime.Now;

                    if (!string.IsNullOrEmpty(Request.Form["ResponsibleUser"]) && Request.Form["ResponsibleUser"].ToString().ToUpper().Trim() != task.ResponsibleUserID.ToUpper().Trim())
                    {
                        var responsibleUserUpdate = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();
                        sbSysLog.AppendLine($"ResponsibleUser from {model.A08_Task.ResponsibleUserUsername} to {responsibleUserUpdate.FirstName} {responsibleUserUpdate.LastName}<br />");
                        itemToUpdate.ResponsibleUserID = Request.Form["ResponsibleUser"];
                    }

                    if (!string.IsNullOrEmpty(Request.Form["ReportingToUser"]) && Request.Form["ReportingToUser"].ToString().ToUpper().Trim() != task.ReportingToUserID.ToUpper().Trim())
                    {
                        var reportingToUserUserUpdate = opProfs.Where(p => p.UserID == Request.Form["ReportingToUser"]).SingleOrDefault();
                        sbSysLog.AppendLine($"ReportingToUser from {model.A08_Task.ReportingToUserUsername} to {reportingToUserUserUpdate.FirstName} {reportingToUserUserUpdate.LastName}<br />");
                        itemToUpdate.ReportingToUserID = Request.Form["ReportingToUser"];
                    }

                    if (model.DueDate != null && model.DueDate != task.DueDate.Value)
                    {
                        sbSysLog.AppendLine($"DueDate from {task.DueDate} to {model.DueDate}<br />");
                        itemToUpdate.DueDate = model.DueDate;
                    }

                    if (!string.IsNullOrEmpty(model.CustomerNo) && model.CustomerNo != task.CustomerNo)
                    {
                        sbSysLog.AppendLine($"CustomerNo from {task.CustomerNo} to {model.CustomerNo}<br />");
                        itemToUpdate.CustomerNo = model.CustomerNo;
                    }

                    if (!string.IsNullOrEmpty(model.MeterSerialNumber) && model.MeterSerialNumber != task.MeterSerialNumber)
                    {
                        sbSysLog.AppendLine($"MeterSerialNumber from {task.MeterSerialNumber} to {model.MeterSerialNumber}<br />");
                        itemToUpdate.MeterSerialNumber = model.MeterSerialNumber;
                    }

                    if (!string.IsNullOrEmpty(Request.Form["NotificationsActive"]) && Convert.ToBoolean(Request.Form["NotificationsActive"]) != task.NotificationsActive)
                    {
                        // Convert.ToBoolean(Request.Form["NotificationsActive"])
                        sbSysLog.AppendLine($"NotificationsActive from {itemToUpdate.NotificationsActive.ToBoolean()} to {Convert.ToBoolean(Request.Form["NotificationsActive"]).ToBoolean()}<br />");
                        itemToUpdate.NotificationsActive = Convert.ToBoolean(Request.Form["NotificationsActive"]);
                    }

                    if (!string.IsNullOrEmpty(model.Description) && (model.Description != task.Description))
                    {
                        sbSysLog.AppendLine($"Description from '{(string.IsNullOrEmpty(task.Description) ? tType.Description : task.Description)}' to '{model.Description}'<br />");
                        itemToUpdate.Description = model.Description;
                    }

                    if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupID"].ToString()) && task.SecureAreaGroupID != Convert.ToInt32(Request.Form["WorkflowGroupID"]))
                    {
                        var beforeWF = task.SecureAreaGroupID.HasValue && workflowGroups.Where(p => p.ID == task.SecureAreaGroupID.Value).SingleOrDefault() != null ? workflowGroups.Where(p => p.ID == task.SecureAreaGroupID.Value).SingleOrDefault().WorkflowGroupName : "";
                        var afterWF = workflowGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["WorkflowGroupID"])).SingleOrDefault();

                        sbSysLog.AppendLine($"SecureAreaGroupID from '{beforeWF}' to '{afterWF.WorkflowGroupName}'<br />");
                        task.SecureAreaGroupID = Convert.ToInt32(Request.Form["WorkflowGroupID"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["BusinessDepartment"].ToString()) && task.BusinessDepartmentID != Convert.ToInt32(Request.Form["BusinessDepartment"]))
                    {
                        var beforeWF = task.BusinessDepartmentID.HasValue ? businessDepartments.Where(p => p.ID == task.BusinessDepartmentID.Value).SingleOrDefault().BusinessDepartmentName : "";
                        var afterWF = businessDepartments.Where(p => p.ID == Convert.ToInt32(Request.Form["BusinessDepartment"])).SingleOrDefault();

                        sbSysLog.AppendLine($"BusinessDepartmentID from '{beforeWF}' to '{afterWF.BusinessDepartmentName}'<br />");
                        task.BusinessDepartmentID = Convert.ToInt32(Request.Form["BusinessDepartment"]);
                    }

                    if (!string.IsNullOrEmpty(model.Identifier) && model.Identifier != task.Identifier)
                    {
                        sbSysLog.AppendLine($"Identifier from {task.Identifier} to {model.Identifier}<br />");
                        itemToUpdate.Identifier = model.Identifier;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.A08_Tasks_ReassignLog a08_Tasks_ReassignLog = new A08_Tasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = !string.IsNullOrEmpty(model.Comments) ? model.Comments : "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(a08_Tasks_ReassignLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_A08_Tasks);
                    _cache.Remove(MVCache.KEY_A08_Tasks_ReassignLog);
                    _cache.Remove(MVCache.KEY_A08_Tasks_Attachments);


                    return Redirect("/operational/A08_Tasks/A08_Task_Review");
                }

                #endregion
            }


            return View("~/Views/Operational/A08_Tasks/A08_Task_Review.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Create")]
        public async Task<IActionResult> A08_Task_Create()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var taskTypes = db.A08_Task_Types.OrderBy(p => p.Heading).ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            var companies = (from p in db.Companies
                             select new { p.CompanyID, p.Name }).ToList();

            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();


            var workflowGroups = db.WorkflowGroups.ToList();
            A08_Task_CreateModel model = new A08_Task_CreateModel()
            {
                ReportingToUser = new List<SelectListItem>(),
                ResponsibleUser = new List<SelectListItem>(),
                TaskType = (from p in taskTypes
                            orderby p.Heading
                            select new SelectListItem()
                            {
                                Text = $"{p.Heading} - ({p.TaskClassification.GetDescription()})",
                                Value = p.ID.ToString(),
                            }).ToList(),
                Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[Select Task Type First]" },
                },
                StatusItems = new List<A08_Task_CreateModel.StatusItem>(),
                Company = new List<SelectListItem>(),
                DueDate = DateTime.Now,
                A08_Task_Types = taskTypes,
                NotificationsActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString() },
                    new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString() },
                },
                WorkflowGroupID = (from p in workflowGroups
                                   orderby p.WorkflowGroupName
                                   select new SelectListItem()
                                   {
                                       Text = p.WorkflowGroupName,
                                       Value = p.ID.ToString(),
                                   }).ToList(),
                WorkflowGroupItems = new List<A08_Task_CreateModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<A08_Task_CreateModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<A08_Task_CreateModel.BusinessPillarItem>(),
            };


            #region Workflows

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new A08_Task_CreateModel.BusinessPillarItem()
                                         {
                                             DisplayName = p.BusinessPillarName,
                                             ID = p.ID,
                                         }).ToList();

            model.BusinessPillar.AddRange((from p in businessPillars
                                           select new SelectListItem()
                                           {
                                               Text = p.BusinessPillarName,
                                               Value = p.ID.ToString(),
                                           }).ToList());

            var businessDepartments = db.BusinessDepartments.ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new A08_Task_CreateModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();

            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new A08_Task_CreateModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            foreach (var uC in _operationalProvider.UserCompaniesWithNone)
            {
                if (uC.CompanyID == 0)
                    continue;
                model.Company.Add(new SelectListItem()
                {
                    Value = uC.CompanyID.ToString(),
                    Text = uC.CompanyID == 0 ? "[Not Linked]" : companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault().Name,
                });
            }

            foreach (var tType in taskTypes.Where(p => p.StatusGroupID.HasValue))
            {
                foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == tType.StatusGroupID.Value))
                {
                    A08_Task_CreateModel.StatusItem statusItem = new A08_Task_CreateModel.StatusItem()
                    {
                        DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                        ID = status.ID,
                        TaskTypeID = tType.ID,
                    };

                    model.StatusItems.Add(statusItem);
                }
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if ((from p in userSecureAreas
                     where p.UserID == user.Id
                     && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                     && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                     select p).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/A08_Tasks/A08_Task_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A08_Tasks/A08_Task_Create")]
        public async Task<IActionResult> A08_Task_Create(A08_Task_CreateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var userSecureAreas = db.UserSecureAreaActions.ToList();
            var taskTypes = db.A08_Task_Types.OrderBy(p => p.Heading).ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            var companies = (from p in db.Companies
                             select new { p.CompanyID, p.Name }).ToList();
            var secureAreas = db.SecureAreas.ToList();
            var tasks = db.A08_Tasks.OrderBy(x => x.ID);
            var TaskId = tasks.LastOrDefault().ID + 1;

            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            #region Workflows
            var businessDepartments = db.BusinessDepartments.ToList();
            var workflowGroups = db.WorkflowGroups.ToList();

            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = p.ID.ToString() == Request.Form["WorkflowGroupID"].ToString()
                                     }).ToList();
            model.WorkflowGroupItems = new List<A08_Task_CreateModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<A08_Task_CreateModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<A08_Task_CreateModel.BusinessPillarItem>();

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new A08_Task_CreateModel.BusinessPillarItem()
                                         {
                                             DisplayName = p.BusinessPillarName,
                                             ID = p.ID,
                                         }).ToList();

            model.BusinessPillar.AddRange((from p in businessPillars
                                           select new SelectListItem()
                                           {
                                               Text = p.BusinessPillarName,
                                               Value = p.ID.ToString(),
                                           }).ToList());


            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new A08_Task_CreateModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();

            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new A08_Task_CreateModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();
            model.TaskType = (from p in taskTypes
                              orderby p.Heading
                              select new SelectListItem()
                              {
                                  Text = p.Heading,
                                  Value = p.ID.ToString(),
                                  Selected = p.ID.ToString() == Request.Form["TaskType"].ToString()
                              }).ToList();
            model.A08_Task_Types = taskTypes;
            model.Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[Select Task Type First]" },
                };

            model.StatusItems = new List<A08_Task_CreateModel.StatusItem>();
            model.Company = new List<SelectListItem>();
            model.NotificationsActive = new List<SelectListItem>()
            {
                new SelectListItem() { Text = true.ToBoolean(), Value = true.ToString(), Selected = Convert.ToBoolean(Request.Form["NotificationsActive"]) },
                new SelectListItem() { Text = false.ToBoolean(), Value = false.ToString(), Selected = Convert.ToBoolean(Request.Form["NotificationsActive"]) },
            };


            foreach (var uC in _operationalProvider.UserCompaniesWithNone)
            {
                if (uC.CompanyID == 0)
                    continue;
                model.Company.Add(new SelectListItem()
                {
                    Value = uC.CompanyID.ToString(),
                    Text = uC.CompanyID == 0 ? "[Not Linked]" : companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault().Name,
                    Selected = Request.Form["Company"].ToString() == uC.CompanyID.ToString(),
                });
            }

            foreach (var taskType in taskTypes.Where(p => p.StatusGroupID.HasValue))
            {
                foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == taskType.StatusGroupID.Value))
                {
                    A08_Task_CreateModel.StatusItem statusItem = new A08_Task_CreateModel.StatusItem()
                    {
                        DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                        ID = status.ID,
                        TaskTypeID = taskType.ID,
                    };

                    model.StatusItems.Add(statusItem);
                }
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if ((from p in userSecureAreas
                     where p.UserID == user.Id
                     && p.SecureAreaID == (int)SecureAreaEnum.A08_Task_Review
                     && p.SecureAreaActionID == (int)SecureAreaActionEnum.Edit
                     select p).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"].ToString() == user.Id });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"].ToString() == user.Id });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            var tType = db.A08_Task_Types.Where(p => p.ID == Convert.ToInt32(Request.Form["TaskType"])).SingleOrDefault();

            if (tType.TaskClassification == TaskClassificationEnum.Client || tType.TaskClassification == TaskClassificationEnum.NotLinked)
            {
                if (string.IsNullOrEmpty(Request.Form["Company"]))
                    ModelState.AddModelError("Company", "Required");
                if (string.IsNullOrEmpty(Request.Form["Status"]))
                    ModelState.AddModelError("Status", "Required");
                if (string.IsNullOrEmpty(model.CustomerNo))
                    ModelState.AddModelError("CustomerNo", "Required");
                if (string.IsNullOrEmpty(model.MeterSerialNumber))
                    ModelState.AddModelError("MeterSerialNumber", "Required");
            }

            var taskTypeHeading = taskTypes.Where(x => x.ID == Convert.ToInt32(Request.Form["TaskType"])).FirstOrDefault();



            var responsibleUser = model.ResponsibleUser.Where(x => x.Value == Request.Form["ResponsibleUser"]).FirstOrDefault();
            var company = model.Company.Where(x => x.Value == Request.Form["Company"]).FirstOrDefault();
            var reportingToUser = model.ReportingToUser.Where(x => x.Value == Request.Form["ReportingToUser"]).FirstOrDefault();
            var taskStatus = model.StatusItems.Where(x => x.ID == Convert.ToInt32(Request.Form["Status"])).FirstOrDefault();
            //tType = db.A08_Task_Types.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
            SiteAdmin_Priority priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault();

            var workflowGroup = model.WorkflowGroupID.Where(x => x.Value == Request.Form["WorkflowGroupID"]).FirstOrDefault();
            var businessPillar = businessPillars.Where(x => x.ID == Request.Form["BusinessPillar"]).FirstOrDefault();
            var businessDepartmentId = workflowGroups.Where(y => y.ID == Int32.Parse(workflowGroup.Value)).Select(x => x.BusinessDepartmentID).FirstOrDefault();
            var businessPillarId = businessDepartments.Where(y => y.ID == businessDepartmentId).Select(x => x.BusinessPillarID).FirstOrDefault();
            var businessDepartmentName = businessDepartments.Where(y => y.ID == businessDepartmentId).Select(x => x.BusinessDepartmentName).FirstOrDefault();
            var businessPillarName = businessPillars.Where(y => y.ID == businessPillarId).Select(x => x.BusinessPillarName).FirstOrDefault();

            int? defaultMinPlanned = 0;
            if (!String.IsNullOrEmpty(Request.Form["TaskType"]))
            {
                defaultMinPlanned = db.A08_Task_Types.Where(p => p.ID == Convert.ToInt32(Request.Form["TaskType"])).SingleOrDefault().DefautlMinPlanned;
            }

            WrikeViewModel wrikeViewModel = new WrikeViewModel()
            {
                ID = TaskId,
                Title = taskTypeHeading.Heading,
                Description = tType.Description,
                Status = taskStatus.DisplayName,
                Responsibles = responsibleUser.Text,
                ResponsibleIds = responsibleUser.Value,
                DateCreated = DateTime.Now,
                DateStarted = null,
                DueDate = model.DueDate,
                Priority = priority.PriorityName,
                ReportingToUser = reportingToUser.Text,
                Company = company.Text,
                WrikeSyncDate = DateTime.Now,
                DefaultMinPlanned = defaultMinPlanned,
                WorkflowGroupName = workflowGroup.Text,
                BusinessDepartmentName = businessDepartmentName,
                BusinessPillarName = businessPillarName,
            };

            var wrikeId = await _httpService.PostAsync(wrikeViewModel);

            if (ModelState.IsValid)
            {
                Data.A08_Task task = new A08_Task()
                {
                    CompanyID = null,
                    DateCreated = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    DueDate = model.DueDate,
                    KmTravelRequired = null,
                    ReportingToUserID = Request.Form["ReportingToUser"].ToString(),
                    ResponsibleUserID = Request.Form["ResponsibleUser"].ToString(),
                    StatusID = Convert.ToInt32(Request.Form["Status"]),
                    StockUsed = "",
                    TaskTypeID = Convert.ToInt32(Request.Form["TaskType"]),
                    Level = null,
                    CustomerNo = model.CustomerNo,
                    MeterSerialNumber = model.MeterSerialNumber,
                    NotificationsActive = Convert.ToBoolean(Request.Form["NotificationsActive"]),
                    Description = tType.Description,
                    PriorityID = priority.ID,
                    WrikeID = wrikeId,
                    WrikeCustomStatus = taskStatus.DisplayName,
                    WrikeSyncDate = DateTime.Now,
                };

                int workflowID = 1;
                if (tType.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == tType.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (tType.SecureAreaGroupID.HasValue)
                    workflowID = tType.SecureAreaGroupID.Value;

                if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupID"]))
                    workflowID = Convert.ToInt32(Request.Form["WorkflowGroupID"]);

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    task.SecureAreaGroupID = workflowID;
                }

                if (!string.IsNullOrEmpty(Request.Form["Company"]))
                    task.CompanyID = Convert.ToInt32(Request.Form["Company"]);

                db.Add(task);
                db.SaveChanges();

                Data.A08_Tasks_ReassignLog a08_Tasks_ReassignLog = new A08_Tasks_ReassignLog()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = "Task Created",
                    TaskID = task.ID,
                    UserDescription = model.Comments,
                    UserID = _userManager.GetUserId(User),
                };

                db.Add(a08_Tasks_ReassignLog);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_A08_Tasks);
                _cache.Remove(MVCache.KEY_A08_Tasks_ReassignLog);
                _cache.Remove(MVCache.KEY_A08_Tasks_Attachments);

                return Redirect($"/operational/A08_Tasks/A08_Task_Review/{task.TaskTypeID}/{task.ID}");

            }

            return View("~/Views/Operational/A08_Tasks/A08_Task_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A08_Tasks/A08_Task_Create_CustomerNo_Search")]
        public JsonResult A08_Task_Create_CustomerNo_Search(string serialOrName)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var sbCustomers = (from p in db.SkybillCustomers
                               where p.Serial_No.Contains(serialOrName)
                               || p.Customer_Name.Contains(serialOrName)
                               || p.Customer_No.Contains(serialOrName)
                               select p).ToList();

            List<string> serialsChecked = new List<string>();

            foreach (var sb in sbCustomers)
            {
                if (results.Count == 20)
                    break;
                if (serialsChecked.Contains(sb.Customer_No))
                    continue;

                serialsChecked.Add(sb.Customer_No);

                string text = $"{sb.Customer_No} ({sb.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = sb.Customer_No,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/A08_Tasks/A08_Task_Create_MeterSerialNumber_Search")]
        public JsonResult A08_Task_Create_MeterSerialNumber_Search(string serialOrName)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var sbCustomers = (from p in db.SkybillCustomers
                               where p.Serial_No.Contains(serialOrName)
                               || p.Customer_No.Contains(serialOrName)
                               || p.Customer_Name.Contains(serialOrName)
                               select p).ToList();
            List<string> serialsChecked = new List<string>();
            foreach (var sb in sbCustomers)
            {
                if (results.Count == 20)
                    break;
                if (serialsChecked.Contains(sb.Serial_No))
                    continue;

                serialsChecked.Add(sb.Serial_No);

                string text = $"{sb.Serial_No} - {sb.Customer_No} ({sb.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = sb.Serial_No,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_ReviewStatusChange/{statusID}")]
        public async Task<IActionResult> A08_Task_ReviewStatusChange(int statusID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.A08_Tasks.Where(p => p.ID == _operationalProvider.TaskSelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                #region Update Task

                if (statusID != task.StatusID)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.A08_Tasks.Where(p => p.ID == task.ID).SingleOrDefault();

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

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.A08_Tasks_ReassignLog a08_Tasks_ReassignLog = new A08_Tasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(a08_Tasks_ReassignLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_A08_Tasks);
                    _cache.Remove(MVCache.KEY_A08_Tasks_ReassignLog);

                }

                #endregion

                return Redirect("/operational/A08_Tasks/A08_Task_Review");
            }

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_ReviewMeetingAgendaChange/{MeetingAgendaID}")]
        public async Task<IActionResult> A08_Task_ReviewMeetingAgendaChange(int MeetingAgendaID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.A08_Tasks.Where(p => p.ID == _operationalProvider.TaskSelectedTaskID).SingleOrDefault();

            if (task != null)
            {
                #region Update Task

                if (MeetingAgendaID != task.MeetingAgendaID)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.A08_Tasks.Where(p => p.ID == task.ID).SingleOrDefault();

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

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.A08_Tasks_ReassignLog a08_Tasks_ReassignLog = new A08_Tasks_ReassignLog()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        TaskID = task.ID,
                        UserDescription = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(a08_Tasks_ReassignLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_A08_Tasks);
                    _cache.Remove(MVCache.KEY_A08_Tasks_ReassignLog);

                }

                #endregion

                return Redirect("/operational/A08_Tasks/A08_Task_Review");
            }

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_ReviewAddTimeAllocated/{duration}")]
        public async Task<IActionResult> A08_Task_ReviewAddTimeAllocated(int duration)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var task = db.A08_Tasks.Where(p => p.ID == _operationalProvider.TaskSelectedTaskID).SingleOrDefault();

            if (task != null)
            {

                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                // Find if open time allocated and close
                var latestOpen = (from p in db.Module_TimeOfWorkAllocateds
                                  where p.ActivityTypeID == (int)ActivityTypeEnum.A08_Task
                                  && p.ActivityID == task.ID
                                  && p.ResponsibleUserID == op.UserID
                                  && !p.EndTime.HasValue
                                  && !p.IsDeleted
                                  select p).FirstOrDefault();

                if (latestOpen != null)
                {
                    latestOpen.EndTime = DateTime.Now;
                    db.Update(latestOpen);
                    db.SaveChanges();
                }

                Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ActivityID = _operationalProvider.TaskSelectedTaskID,
                    ActivityTypeID = (int)ActivityTypeEnum.A08_Task,
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

                return Redirect("/operational/A08_Tasks/A08_Task_Review");
            }

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_ReviewStartTimeAllocated/{taskID}")]
        public async Task<IActionResult> A08_Task_ReviewStartTimeAllocated(int taskID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var task = db.A08_Tasks.Where(p => p.ID == taskID).SingleOrDefault();

            if (task != null)
            {
                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ActivityID = taskID,
                    ActivityTypeID = (int)ActivityTypeEnum.A08_Task,
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

                return Redirect("/operational/A08_Tasks/A08_Task_Review");
            }

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_ReviewStopTimeAllocated/{timeOfWorkAllocatedID}")]
        public async Task<IActionResult> A08_Task_ReviewStopTimeAllocated(int timeOfWorkAllocatedID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var timeOfWorkAllocated = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == timeOfWorkAllocatedID).SingleOrDefault();

            if (timeOfWorkAllocated != null)
            {
                timeOfWorkAllocated.EndTime = DateTime.Now;
                db.Update(timeOfWorkAllocated);
                db.SaveChanges();

                return Redirect("/operational/A08_Tasks/A08_Task_Review");
            }

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_TaskTypeHowToDocument/{ID}")]
        public async Task<IActionResult> A08_TaskTypeHowToDocument(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.A08_Task_Types.Where(p => p.ID == ID).SingleOrDefault();
            string contentType = "text/plain";
            if (item != null && !string.IsNullOrEmpty(item.HowToURL))
            {
                string shareName = "a08-tasktypes";
                string dirName = $"{item.ID}";
                string fileName = $"{item.ID}" + System.IO.Path.GetExtension(item.HowToURL);

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

                if (!provider.TryGetContentType(fileName, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (uploadFile != null)
                    return File(uploadFile, contentType, System.IO.Path.GetFileName(fileName));

            }
            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Review_AddAttachment")]
        public async Task<IActionResult> A08_Task_Review_AddAttachment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            A08_Task_Review_AddAttachmentModel model = new A08_Task_Review_AddAttachmentModel()
            {
                Description = "Photo",
            };
            model.AttachmentType = (from p in ((A08_Tasks_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(A08_Tasks_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = p == A08_Tasks_Attachment.AttachmentTypeEnum.Photo
                                    }).ToList();

            return View("~/Views/Operational/A08_Tasks/A08_Task_Review_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A08_Tasks/A08_Task_Review_AddAttachment")]
        public async Task<IActionResult> A08_Task_Review_AddAttachment(A08_Task_Review_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || _operationalProvider.TaskSelectedTaskID == 0)
                return Redirect("/operational/A08_Tasks/A08_Tasks_Type_Summary");

            model.AttachmentType = (from p in ((A08_Tasks_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(A08_Tasks_Attachment.AttachmentTypeEnum)))
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
                    string dirName = $"{_operationalProvider.TaskSelectedTaskID}";
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

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);

                    Data.A08_Tasks_Attachment a08_Tasks_Attachment = new A08_Tasks_Attachment()
                    {
                        AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                        DateCreated = DateTime.Now,
                        Filename = fileName,
                        UserID = _userManager.GetUserId(User),
                        Description = model.Description,
                        TaskID = _operationalProvider.TaskSelectedTaskID,
                    };

                    var db = new MyVoltageDbContext(_options);
                    db.Add(a08_Tasks_Attachment);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_A08_Tasks_Attachments);
                    model.IsSuccess = true;

                }
            }



            return View("~/Views/Operational/A08_Tasks/A08_Task_Review_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Review_GetAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> A08_Task_Review_GetAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var a08_Tasks_Attachment = dbCache.A08_Tasks_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (a08_Tasks_Attachment == null)
                return Redirect("/operational/A08_Tasks/A08_Task_Review");


            string shareName = "a08-tasks-attachments";
            string dirName = $"{_operationalProvider.TaskSelectedTaskID}";
            string fileName = a08_Tasks_Attachment.Filename;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(a08_Tasks_Attachment.Filename));


            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Task_Review_DeleteAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> A08_Task_Review_DeleteAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a08_Tasks_Attachment = db.A08_Tasks_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (a08_Tasks_Attachment == null)
                return Redirect("/operational/A08_Tasks/A08_Task_Review");

            a08_Tasks_Attachment.IsDeleted = true;
            db.Update(a08_Tasks_Attachment);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_A08_Tasks_Attachments);

            return Redirect("/operational/A08_Tasks/A08_Task_Review");
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Search")]
        public async Task<IActionResult> A08_Tasks_Search()
        {

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            A08_Tasks_SearchModel model = new A08_Tasks_SearchModel()
            {
                A08_Tasks_TypeDetailsItems = new List<A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem>(),
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]),
                    }
                },
                TaskID = !string.IsNullOrEmpty(Request.Query["TaskID"]) ? Request.Query["TaskID"].ToString() : "",
                Comments = !string.IsNullOrEmpty(Request.Query["Comments"]) ? Request.Query["Comments"].ToString() : "",
                Description = !string.IsNullOrEmpty(Request.Query["Description"]) ? Request.Query["Description"].ToString() : "",
                Heading = !string.IsNullOrEmpty(Request.Query["Heading"]) ? Request.Query["Heading"].ToString() : "",
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]),
                    }
                },
                TaskType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Task Types--]",
                        Selected = string.IsNullOrEmpty(Request.Query["TaskType"]),
                    }
                },
                DefaultStatus = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Statuses--]",
                        Selected = string.IsNullOrEmpty(Request.Query["TaskType"]),
                    }
                },
                StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Status Groups--]",
                        Selected = string.IsNullOrEmpty(Request.Query["StatusGroup"]),
                    }
                },
                StatusItems = new List<A08_Tasks_SearchModel.StatusItem>(),
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Task Priorities--]",
                        Selected = string.IsNullOrEmpty(Request.Query["Priority"]),
                    }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Task Companies--]",
                        Selected = string.IsNullOrEmpty(Request.Query["Company"]),
                    }
                },
                ResolvedStatusType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Resolved Status Types--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]),
                    },
                    new SelectListItem()
                    {
                        Value = true.ToString(),
                        Text = "Resolved Only",
                        Selected = !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) && Convert.ToBoolean(Request.Query["ResolvedStatusType"]),
                    },
                    new SelectListItem()
                    {
                        Value = false.ToString(),
                        Text = "Unresolved Only",
                        Selected = !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) && !Convert.ToBoolean(Request.Query["ResolvedStatusType"]),
                    },
                },
            };

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());

            model.Priority.AddRange((from p in priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"] == p.ID.ToString() ? true : false,
                                     }).ToList());

            model.TaskType.AddRange((from p in db.A08_Task_Types
                                     orderby p.Heading
                                     select new SelectListItem()
                                     {
                                         Selected = !string.IsNullOrEmpty(Request.Query["TaskType"]) && Request.Query["TaskType"].ToString() == p.ID.ToString(),
                                         Text = $"{p.Identifier} - {p.Heading}",
                                         Value = p.ID.ToString(),
                                     }).ToList());
            model.TaskType = model.TaskType.OrderBy(p => p.Text).ToList();

            var responsibleUsers = (from p in db.A08_Tasks
                                    select p.ResponsibleUserID).Distinct().ToList();

            model.ResponsibleUser.AddRange((from p in opProfs
                                            where responsibleUsers.Contains(p.UserID)
                                            select new SelectListItem()
                                            {
                                                Selected = !string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) && Request.Query["ResponsibleUser"].ToString() == p.UserID,
                                                Text = $"{p.FirstName} {p.LastName}",
                                                Value = p.UserID,
                                            }).ToList());
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var reportingToUsers = (from p in db.A08_Tasks
                                    select p.ReportingToUserID).Distinct().ToList();

            model.ReportingToUser.AddRange((from p in opProfs
                                            where reportingToUsers.Contains(p.UserID)
                                            select new SelectListItem()
                                            {
                                                Selected = !string.IsNullOrEmpty(Request.Query["ReportingToUser"]) && Request.Query["ReportingToUser"].ToString() == p.UserID,
                                                Text = $"{p.FirstName} {p.LastName}",
                                                Value = p.UserID,
                                            }).ToList());
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = Request.Query["StatusGroup"] == p.ID.ToString(),
                                        }).ToList());

            foreach (var status in siteAdmin_Statuses)
            {
                A08_Tasks_SearchModel.StatusItem statusItem = new A08_Tasks_SearchModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            List<A08_Task> tasks = new List<A08_Task>();
            List<int> taskIDs = new List<int>();

            SqlCommand sqlCommandLatestComment = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandLatestComment).Fill(tblsp_GetA08_TasksLatestComment);

            if (!string.IsNullOrEmpty(Request.Query["DueDateFrom"]))
            {
                model.DueDateFrom = Convert.ToDateTime(Request.Query["DueDateFrom"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DueDateTo"]))
            {
                model.DueDateTo = Convert.ToDateTime(Request.Query["DueDateTo"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DefaultStatus"]))
            {
                model.DefaultStatusID = Convert.ToInt32(Request.Query["DefaultStatus"]);
            }
            if (Request.Query.Count > 0)
            {
                SqlConnection connSearch = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandSearch = new SqlCommand("sp_A08_Tasks_Search", connSearch);
                sqlCommandSearch.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCommandSearch.Parameters.AddWithValue("@TaskType", !string.IsNullOrEmpty(Request.Query["TaskType"]) ? Request.Query["TaskType"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Heading", !string.IsNullOrEmpty(Request.Query["Heading"]) ? Request.Query["Heading"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Description", !string.IsNullOrEmpty(Request.Query["Description"]) ? Request.Query["Description"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Comments", !string.IsNullOrEmpty(Request.Query["Comments"]) ? Request.Query["Comments"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ResponsibleUser", !string.IsNullOrEmpty(Request.Query["ResponsibleUser"]) ? Request.Query["ResponsibleUser"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ReportingToUser", !string.IsNullOrEmpty(Request.Query["ReportingToUser"]) ? Request.Query["ReportingToUser"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateFrom", !string.IsNullOrEmpty(Request.Query["DueDateFrom"]) ? Request.Query["DueDateFrom"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateTo", !string.IsNullOrEmpty(Request.Query["DueDateTo"]) ? Request.Query["DueDateTo"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DefaultStatusID", !string.IsNullOrEmpty(Request.Query["DefaultStatus"]) ? Request.Query["DefaultStatus"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@PriorityID", !string.IsNullOrEmpty(Request.Query["Priority"]) ? Request.Query["Priority"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@TaskID", !string.IsNullOrEmpty(Request.Query["TaskID"]) ? Request.Query["TaskID"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@CompanyID", !string.IsNullOrEmpty(Request.Query["Company"]) ? Request.Query["Company"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@IsResolvedOnly", !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) ? (Convert.ToBoolean(Request.Query["ResolvedStatusType"]) ? "1" : "0") : "");

                System.Data.DataTable dataTableSearch = new System.Data.DataTable();

                connSearch.Open();
                new SqlDataAdapter(sqlCommandSearch).Fill(dataTableSearch);
                connSearch.Close();

                foreach (DataRow dr in dataTableSearch.Rows)
                {
                    taskIDs.Add(Convert.ToInt32(dr[0]));
                }
            }

            if (taskIDs.Count > 0)
                tasks = (from p in db.A08_Tasks
                         where taskIDs.Contains(p.ID)
                         select p).ToList();

            foreach (var task in tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem item = new A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Task_Type()
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
                        StatusGroupID = tType.StatusGroupID,
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                        ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                        HasComplianceCheck = tType.HasComplianceCheck,
                        DefautlMinPlanned = tType.DefautlMinPlanned,
                        DefaultStatusID = tType.DefaultStatusID,
                        DefaultMeetingAgendaID = tType.DefaultMeetingAgendaID,
                        BusinessDepartmentID = tType.BusinessDepartmentID,
                        Identifier = tType.Identifier,
                        IsDeleted = tType.IsDeleted,
                        MeetingAgendaGroupID = tType.MeetingAgendaGroupID,
                        ReportingToUserAccepted = tType.ReportingToUserAccepted,
                        ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                        ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                        ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                        TaskClassificationID = tType.TaskClassificationID,
                        UpdateExistingTask = tType.UpdateExistingTask,
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
                    Status = new A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Tasks_Type_DetailsItemStatus()
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
                    BusinessDepartmentID = task.BusinessDepartmentID,
                    CustomerNo = task.CustomerNo,
                    Description = task.Description,
                    Identifier = task.Identifier,
                    LatestComment = task.Identifier,
                    MeetingAgendaID = task.MeetingAgendaID,
                    MeterSerialNumber = task.MeterSerialNumber,
                    NotificationsActive = task.NotificationsActive,
                    PriorityID = task.PriorityID,
                    ReportingToUserUsername = "",
                    ResponsibleUserUsername = "",
                    SecureAreaGroupID = task.SecureAreaGroupID,
                    WrikeCustomStatus = task.WrikeCustomStatus,
                    WrikeID = task.WrikeID,
                    WrikeSyncDate = task.WrikeSyncDate,
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
                model.A08_Tasks_TypeDetailsItems.Add(item);
            }

            return View("~/Views/Operational/A08_Tasks/A08_Tasks_Search.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Overview")]
        public async Task<IActionResult> A08_Tasks_Overview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Tasks_Overview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Tasks_Overview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            workflowGroups = workflowGroups.Where(p => p.BusinessDepartmentID.HasValue).ToList();
            var workflowGroupParents = db.WorkflowGroupParents.ToList();
            var WorkflowGroupGrandParents = db.WorkflowGroupGrandParents.ToList();
            var secureAreas = db.SecureAreas.ToList();
            int WorkflowGroupGrandParentID = string.IsNullOrEmpty(Request.Query["WorkflowGroupGrandParentID"]) ? WorkflowGroupGrandParents.FirstOrDefault().ID : Convert.ToInt32(Request.Query["WorkflowGroupGrandParentID"]);
            var opProfs = db.OperationalProfiles.ToList();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            //var responsiblePeople = dbCache.A08_Tasks_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var workflowGroupParent = workflowGroupParents.Where(p => p.ID == WorkflowGroupGrandParentID).SingleOrDefault();
            var workflowParentsAllowed = workflowGroupParents.Where(p => p.WorkflowGroupGrandParentID == WorkflowGroupGrandParentID).ToList();
            var workflowGroupsAllowed = workflowGroups.Where(p => p.WorkflowGroupParentID.HasValue && workflowParentsAllowed.Select(c => c.ID).Contains(p.WorkflowGroupParentID.Value)).ToList();
            var businessDepartmentsAllowed = businessDepartments.Where(p => workflowGroupsAllowed.Where(c => c.BusinessDepartmentID.HasValue).Select(c => c.BusinessDepartmentID.Value).Contains(p.ID)).ToList();

            A08_Tasks_OverviewModel model = new A08_Tasks_OverviewModel()
            {
                A08_Tasks_CompanyDetailsItems = new List<A08_Tasks_OverviewModel.A08_Tasks_OverviewItem>(),
                BusinessDepartmentItems = (from p in businessDepartmentsAllowed
                                           select new A08_Tasks_OverviewModel.BusinessDepartmentItem()
                                           {
                                               BusinessPillarID = p.BusinessPillarID,
                                               DisplayName = p.BusinessDepartmentName,
                                               ID = p.ID,
                                           }).ToList(),
                WorkflowGroupItems = (from p in workflowGroupsAllowed
                                      select new A08_Tasks_OverviewModel.WorkflowGroupItem()
                                      {
                                          DisplayName = p.WorkflowGroupName,
                                          ID = p.ID,
                                          WorkflowGroupParentID = p.WorkflowGroupParentID,
                                      }).ToList(),
                WorkflowGroupParentItems = (from p in workflowParentsAllowed
                                            select new A08_Tasks_OverviewModel.WorkflowGroupParentItem()
                                            {
                                                DisplayName = p.WorkflowGroupParentName,
                                                ID = p.ID,
                                            }).ToList(),
                WorkflowGroupGrandParentItems = (from p in WorkflowGroupGrandParents
                                                 select new A08_Tasks_OverviewModel.WorkflowGroupGrandParentItem()
                                                 {
                                                     DisplayName = p.WorkflowGroupGrandParentName,
                                                     ID = p.ID,
                                                 }).ToList(),
                WorkflowGroupGrandParentID = (from p in WorkflowGroupGrandParents
                                              select new SelectListItem()
                                              {
                                                  Text = p.WorkflowGroupGrandParentName,
                                                  Value = p.ID.ToString(),
                                                  Selected = WorkflowGroupGrandParentID == p.ID,
                                              }).ToList(),
                DisplayType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "All Details", Selected = string.IsNullOrEmpty(Request.Query["DisplayType"]) },
                    new SelectListItem() { Value = "1", Text = "Task Headings", Selected = !string.IsNullOrEmpty(Request.Query["DisplayType"]) && Request.Query["DisplayType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Overview", Selected = !string.IsNullOrEmpty(Request.Query["DisplayType"]) && Request.Query["DisplayType"].ToString() == "2" },
                },
                DisplayTypeFilter = Request.Query["DisplayType"].ToString(),
            };



            if (_operationalProvider.CompanyID != 0)
            {
                var tasks = db.A08_Tasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                var tasksReassigns = db.A08_Tasks_ReassignLogs.ToList();

                foreach (var task in tasks)
                {
                    var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                    if (tType == null || !tType.SecureAreaGroupID.HasValue)
                        continue;


                    if (tType.BusinessDepartmentID.HasValue && !businessDepartmentsAllowed.Select(c => c.ID).Contains(tType.BusinessDepartmentID.Value))
                        continue;
                    else if (tType.SecureAreaGroupID.HasValue && !workflowGroupsAllowed.Select(c => c.ID).Contains(tType.SecureAreaGroupID.Value))
                        continue;

                    var workflowGroup = workflowGroups.Where(p => p.ID == tType.SecureAreaGroupID.Value).SingleOrDefault();

                    var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                    if (status == null)
                        status = siteAdmin_Statuses.FirstOrDefault();

                    A08_Tasks_OverviewModel.A08_Tasks_OverviewItem item = new A08_Tasks_OverviewModel.A08_Tasks_OverviewItem()
                    {
                        CompanyID = task.CompanyID,
                        DateCreated = task.DateCreated,
                        ID = task.ID,
                        StatusID = task.StatusID,
                        A08_Tasks_TypeItem = new A08_Tasks_OverviewModel.A08_Tasks_OverviewItem.A08_Task_Type()
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
                            SecureAreaGroupID = tType.SecureAreaGroupID,
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
                        Status = new A08_Tasks_OverviewModel.A08_Tasks_OverviewItem.A08_Tasks_OverviewItemStatus()
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

                    model.A08_Tasks_CompanyDetailsItems.Add(item);
                }

            }


            model.A08_Tasks_CompanyDetailsItems = model.A08_Tasks_CompanyDetailsItems.OrderBy(p => p.A08_Tasks_TypeItem.Heading).ThenBy(p => p.A08_Tasks_TypeItem.Description).ToList();

            return View("~/Views/Operational/A08_Tasks/A08_Tasks_Overview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A08_Tasks/A08_Tasks_Heatmap")]
        public async Task<IActionResult> A08_Tasks_Heatmap()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Tasks_Heatmap, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Tasks_Heatmap}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var WorkflowGroupGrandParents = db.WorkflowGroupGrandParents.OrderBy(p => p.WorkflowGroupGrandParentName).ToList();
            var secureAreas = db.SecureAreas.ToList();
            int WorkflowGroupGrandParentID = string.IsNullOrEmpty(Request.Query["WorkflowGroupGrandParentID"]) ? WorkflowGroupGrandParents.FirstOrDefault().ID : Convert.ToInt32(Request.Query["WorkflowGroupGrandParentID"]);
            var opProfs = db.OperationalProfiles.ToList();

            var workflowGroupParents = db.WorkflowGroupParents.OrderBy(p => p.WorkflowGroupParentName).ToList();
            var workflowGroups = db.WorkflowGroups.OrderBy(p => p.WorkflowGroupName).ToList();
            workflowGroups = workflowGroups.Where(p => p.BusinessDepartmentID.HasValue).ToList();

            var priorities = db.SiteAdmin_Priorities.ToList();
            //var responsiblePeople = dbCache.A08_Tasks_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            var workflowGroupParent = workflowGroupParents.Where(p => p.ID == WorkflowGroupGrandParentID).SingleOrDefault();
            var workflowParentsAllowed = workflowGroupParents.Where(p => p.WorkflowGroupGrandParentID == WorkflowGroupGrandParentID).ToList();
            var workflowGroupsAllowed = workflowGroups.Where(p => p.WorkflowGroupParentID.HasValue && workflowParentsAllowed.Select(c => c.ID).Contains(p.WorkflowGroupParentID.Value)).ToList();
            var businessDepartmentsAllowed = businessDepartments.Where(p => workflowGroupsAllowed.Where(c => c.BusinessDepartmentID.HasValue).Select(c => c.BusinessDepartmentID.Value).Contains(p.ID)).ToList();
            var taskTypes = db.A08_Task_Types.OrderBy(p => p.Identifier).ToList();
            taskTypes = taskTypes.Where(p => p.SecureAreaGroupID.HasValue && workflowGroupsAllowed.Select(c => c.ID).Contains(p.SecureAreaGroupID.Value)).ToList();

            A08_Tasks_HeatmapModel model = new A08_Tasks_HeatmapModel()
            {
                A08_Tasks_CompanyDetailsItems = new List<A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem>(),
                WorkflowGroupGrandParentItems = (from p in WorkflowGroupGrandParents
                                                 select new A08_Tasks_HeatmapModel.WorkflowGroupGrandParentItem()
                                                 {
                                                     DisplayName = p.WorkflowGroupGrandParentName,
                                                     ID = p.ID,
                                                 }).ToList(),
                WorkflowGroupGrandParentID = (from p in WorkflowGroupGrandParents
                                              select new SelectListItem()
                                              {
                                                  Text = p.WorkflowGroupGrandParentName,
                                                  Value = p.ID.ToString(),
                                                  Selected = WorkflowGroupGrandParentID == p.ID,
                                              }).ToList(),
                A08_Task_Types = taskTypes,
                DueDateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DueDateTo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            };

            if (!string.IsNullOrEmpty(Request.Query["fromDate"]))
            {
                model.DueDateFrom = Convert.ToDateTime(Request.Query["fromDate"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["toDate"]))
            {
                model.DueDateTo = Convert.ToDateTime(Request.Query["toDate"]);
            }

            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            List<A08_Task_Type> a08_Task_TypesUsed = new List<A08_Task_Type>();


            var tasks = db.A08_Tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom).ToList();

            var tasksNoCompany = tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom &&
            !p.CompanyID.HasValue).ToList();
            if (tasksNoCompany.Count != 0)
            {
                A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem a08_Tasks_HeatmapItem = new A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem()
                {
                    CompanyID = 0,
                    CompanyName = "All Companies / Not Linked",
                    A08_Tasks_HeatmapSubItems = new List<A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem>(),
                };

                foreach (var tType in taskTypes)
                {
                    var tasksForType = tasksNoCompany.Where(p => p.TaskTypeID == tType.ID).ToList();

                    if (tasksForType.Count != 0)
                    {
                        if (!a08_Task_TypesUsed.Contains(tType))
                            a08_Task_TypesUsed.Add(tType);

                        A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem a08_Tasks_HeatmapSubItem = new A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem()
                        {
                            TaskTypeID = tType.ID,
                            TaskTypeShortName = tType.Identifier.Contains(" ") ? tType.Identifier.Split(" ")[0] : tType.Identifier,
                            UnresolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                            ResolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => p.IsResolvedStatus.HasValue && p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                        };

                        a08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItems.Add(a08_Tasks_HeatmapSubItem);
                    }
                }

                if (a08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItems.Count != 0)
                    model.A08_Tasks_CompanyDetailsItems.Add(a08_Tasks_HeatmapItem);
            }

            foreach (var c in companies)
            {
                var tasksForCompany = tasks.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom && p.CompanyID.HasValue && p.CompanyID.Value == c.CompanyID).ToList();
                if (tasksForCompany.Count != 0)
                {
                    A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem a08_Tasks_HeatmapItem = new A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem()
                    {
                        CompanyID = c.CompanyID,
                        CompanyName = c.Name,
                        A08_Tasks_HeatmapSubItems = new List<A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem>(),
                    };

                    foreach (var tType in taskTypes)
                    {
                        var tasksForType = tasksForCompany.Where(p => p.TaskTypeID == tType.ID).ToList();

                        if (tasksForType.Count != 0)
                        {
                            if (!a08_Task_TypesUsed.Contains(tType))
                                a08_Task_TypesUsed.Add(tType);

                            A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem a08_Tasks_HeatmapSubItem = new A08_Tasks_HeatmapModel.A08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItem()
                            {
                                TaskTypeID = tType.ID,
                                TaskTypeShortName = tType.Identifier.Contains(" ") ? tType.Identifier.Split(" ")[0] : tType.Identifier,
                                UnresolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                                ResolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => p.IsResolvedStatus.HasValue && p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                            };

                            a08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItems.Add(a08_Tasks_HeatmapSubItem);
                        }
                    }

                    if (a08_Tasks_HeatmapItem.A08_Tasks_HeatmapSubItems.Count != 0)
                        model.A08_Tasks_CompanyDetailsItems.Add(a08_Tasks_HeatmapItem);
                }
            }

            model.A08_Task_Types = a08_Task_TypesUsed.OrderBy(p => p.Identifier).ToList();

            return View("~/Views/Operational/A08_Tasks/A08_Tasks_Heatmap.cshtml", model);
        }
    }
}
