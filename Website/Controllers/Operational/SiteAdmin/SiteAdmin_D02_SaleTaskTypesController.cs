using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_D02_SaleTaskTypesModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class SiteAdmin_D02_SaleTypesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_D02_SaleTypesController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var D02_SaleTasks = db.D02_SaleTasks.ToList();
            var D02_SaleTask_Types = db.D02_SaleTask_Types.ToList();
            var D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();


            SiteAdmin_D02_SaleTask_TypesModel model = new SiteAdmin_D02_SaleTask_TypesModel()
            {
                SiteAdmin_D02_SaleTaskTypesItems = new List<SiteAdmin_D02_SaleTask_TypesModel.SiteAdmin_D02_SaleTaskTypesItem>(),
                ShowDeleted = !string.IsNullOrEmpty(Request.Query["SD"]),
            };

            if (!model.ShowDeleted)
                D02_SaleTask_Types = D02_SaleTask_Types.Where(p => !p.IsDeleted.HasValue || !p.IsDeleted.Value).ToList();

            foreach (var task_Type in D02_SaleTask_Types)
            {
                var opProfResponsible = opProfs.Where(p => p.UserID == task_Type.ResponsibleUserID).SingleOrDefault();
                var opProfReportingToU = opProfs.Where(p => p.UserID == task_Type.ReportingToUserID).SingleOrDefault();
                SiteAdmin_D02_SaleTask_TypesModel.SiteAdmin_D02_SaleTaskTypesItem item = new SiteAdmin_D02_SaleTask_TypesModel.SiteAdmin_D02_SaleTaskTypesItem()
                {
                    ID = task_Type.ID,
                    DashboardURL = task_Type.DashboardURL,
                    Description = task_Type.Description,
                    Heading = task_Type.Heading,
                    HowToURL = task_Type.HowToURL,
                    PriorityID = task_Type.PriorityID,
                    ReportingToUserID = task_Type.ReportingToUserID,
                    ResponsibleUserID = task_Type.ResponsibleUserID,
                    TemplateNo = task_Type.TemplateNo,
                    ResponsibleUsername = opProfResponsible != null ? $"{opProfResponsible.FirstName} {opProfResponsible.LastName}" : _userManager.FindByIdAsync(task_Type.ResponsibleUserID).Result.UserName,
                    ReportingToUserUsername = opProfResponsible != null ? $"{opProfReportingToU.FirstName} {opProfReportingToU.LastName}" : _userManager.FindByIdAsync(task_Type.ReportingToUserID).Result.UserName,
                    LinkedSecureAreaID = task_Type.LinkedSecureAreaID,
                    CreateIndividualFlagsForCompaniesLinked = task_Type.CreateIndividualFlagsForCompaniesLinked,
                    MinRequiredToClear = task_Type.MinRequiredToClear,
                    SiteAdmin_Priority = priorities.Where(p => p.ID == task_Type.PriorityID).SingleOrDefault(),
                    DefaultStatusID = task_Type.DefaultStatusID,
                    SiteAdmin_Status = "[NONE]",
                    SiteAdmin_StatusGroup = task_Type.StatusGroupID.HasValue ? siteAdmin_StatusGroups.Where(p => p.ID == task_Type.StatusGroupID.Value).SingleOrDefault() : null,
                    StatusGroupID = task_Type.StatusGroupID,
                    DefaultMeetingAgendaID = task_Type.DefaultMeetingAgendaID,
                    SiteAdmin_MeetingAgenda = "[NONE]",
                    SiteAdmin_MeetingAgendaGroup = task_Type.MeetingAgendaGroupID.HasValue ? siteAdmin_MeetingAgendaGroups.Where(p => p.ID == task_Type.MeetingAgendaGroupID.Value).SingleOrDefault() : null,
                    MeetingAgendaGroupID = task_Type.MeetingAgendaGroupID,
                    IsLinkedToSelectedCompany = D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == task_Type.ID && p.CompanyID == _operationalProvider.CompanyID).Count() != 0,
                    IsDeleted = task_Type.IsDeleted,
                    Identifier = task_Type.Identifier,
                    WorkflowGroupName = "",
                    BusinessPillarName = "",
                    BusinessDepartmentName = "",
                    BusinessDepartmentID = task_Type.BusinessDepartmentID,
                    TaskClassificationID = task_Type.TaskClassificationID,
                    WorkflowGroupID = task_Type.WorkflowGroupID,
                    PreviousRunDate = null,
                    ReportingToUserAccepted = task_Type.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = task_Type.ReportingToUserAcceptedDate,
                    ReportingToUserRequiresCompletedState = task_Type.ReportingToUserRequiresCompletedState,
                    ResponsibleUserAccepted = task_Type.ResponsibleUserAccepted,
                    ResponsibleUserAcceptedDate = task_Type.ResponsibleUserAcceptedDate,
                };

                if (task_Type.DefaultStatusID.HasValue)
                {
                    var status = siteAdmin_Statuses.Where(p => p.ID == task_Type.DefaultStatusID.Value).SingleOrDefault();
                    item.SiteAdmin_Status = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}";
                }

                int workflowID = 1;
                if (task_Type.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == task_Type.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (task_Type.WorkflowGroupID.HasValue)
                    workflowID = task_Type.WorkflowGroupID.Value;

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    item.WorkflowGroupName = wf.WorkflowGroupName;
                    if (task_Type.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == task_Type.BusinessDepartmentID.Value).SingleOrDefault();
                        if (department != null)
                        {
                            item.BusinessDepartmentName = department.BusinessDepartmentName;
                            var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                            if (pillar != null)
                                item.BusinessPillarName = pillar.BusinessPillarName;
                        }
                    }
                    else if (wf.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        item.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        item.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                if (D02_SaleTasks.Where(p => p.TaskTypeID == task_Type.ID).Count() > 0)
                    item.PreviousRunDate = D02_SaleTasks.Where(p => p.TaskTypeID == task_Type.ID).Select(p => p.DateCreated).Max();

                model.SiteAdmin_D02_SaleTaskTypesItems.Add(item);
            }

            model.SiteAdmin_D02_SaleTaskTypesItems = model.SiteAdmin_D02_SaleTaskTypesItems.OrderBy(p => p.Identifier).ThenBy(p => p.Heading).ToList();
            return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/SiteAdmin_D02_SaleTaskTypes.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Add")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var workflowGroups = db.WorkflowGroups.ToList();
            SiteAdmin_D02_SaleTask_TypesAddModel model = new SiteAdmin_D02_SaleTask_TypesAddModel()
            {
                Department = (from p in ((D02_SaleTask_Type.DepartmentEnum[])Enum.GetValues(typeof(D02_SaleTask_Type.DepartmentEnum)))
                              select new SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = ((int)p).ToString(),
                              }).ToList(),
                Priority = (from p in db.SiteAdmin_Priorities
                            select new SelectListItem()
                            {
                                Text = p.PriorityName,
                                Value = p.ID.ToString(),
                            }).ToList(),
                ReportingToUser = new List<SelectListItem>(),
                ResponsibleUser = new List<SelectListItem>(),
                LinkedSecureArea = new List<SelectListItem>(),
                StatusGroup = new List<SelectListItem>(),
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem>(),
                MeetingAgendaGroup = new List<SelectListItem>(),
                DefaultMeetingAgenda = new List<SelectListItem>(),
                MeetingAgendaItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem>(),
                TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                      select new SelectListItem()
                                      {
                                          Text = p.GetDescription(),
                                          Value = ((int)p).ToString(),
                                      }).ToList(),
                WorkflowGroupID = (from p in workflowGroups
                                   orderby p.WorkflowGroupName
                                   select new SelectListItem()
                                   {
                                       Text = p.WorkflowGroupName,
                                       Value = p.ID.ToString(),
                                   }).ToList(),
                WorkflowGroupItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.BusinessPillarItem>(),
            };

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem statusItem = new SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            model.MeetingAgendaGroup.AddRange((from p in siteAdmin_MeetingAgendaGroups
                                               select new SelectListItem()
                                               {
                                                   Text = p.MeetingAgendaGroupName,
                                                   Value = p.ID.ToString(),
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName });
            }

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE" });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}" });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            #region Workflows

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_D02_SaleTask_TypesAddModel.BusinessPillarItem()
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

            var businessDepartments = db.BusinessDepartments.Where(p => p.BusinessPillarID == 8).ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new SiteAdmin_D02_SaleTask_TypesAddModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_D02_SaleTask_TypesAddModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Add")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Add(SiteAdmin_D02_SaleTask_TypesAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.Department = (from p in ((D02_SaleTask_Type.DepartmentEnum[])Enum.GetValues(typeof(D02_SaleTask_Type.DepartmentEnum)))
                                select new SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString(),
                                    Selected = Request.Form["Department"] == ((int)p).ToString() ? true : false,
                                }).ToList();
            model.TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                        select new SelectListItem()
                                        {
                                            Text = p.GetDescription(),
                                            Value = ((int)p).ToString(),
                                            Selected = Request.Form["TaskClassification"].ToString() == ((int)p).ToString() ? true : false,
                                        }).ToList();

            var workflowGroups = db.WorkflowGroups.ToList();
            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Form["WorkflowGroupID"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList();

            model.WorkflowGroupItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.BusinessPillarItem>();
            model.Priority = (from p in db.SiteAdmin_Priorities
                              select new SelectListItem()
                              {
                                  Text = p.PriorityName,
                                  Value = p.ID.ToString(),
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString() ? true : false,
                              }).ToList();
            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"].ToString() == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"].ToString() == user.Id ? true : false });
            }

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureArea"].ToString() == ((int)sa.Item1).ToString() ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            model.StatusGroup = new List<SelectListItem>();
            model.DefaultStatus = new List<SelectListItem>();
            model.StatusItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem>();

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = Request.Form["StatusGroup"].ToString() == p.ID.ToString(),
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem statusItem = new SiteAdmin_D02_SaleTask_TypesAddModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            model.MeetingAgendaGroup = new List<SelectListItem>();
            model.DefaultMeetingAgenda = new List<SelectListItem>();
            model.MeetingAgendaItems = new List<SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem>();

            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            model.MeetingAgendaGroup.AddRange((from p in siteAdmin_MeetingAgendaGroups
                                               select new SelectListItem()
                                               {
                                                   Text = p.MeetingAgendaGroupName,
                                                   Value = p.ID.ToString(),
                                                   Selected = Request.Form["MeetingAgendaGroup"].ToString() == p.ID.ToString(),
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_D02_SaleTask_TypesAddModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            #region Workflows

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_D02_SaleTask_TypesAddModel.BusinessPillarItem()
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

            var businessDepartments = db.BusinessDepartments.Where(p => p.BusinessPillarID == 8).ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new SiteAdmin_D02_SaleTask_TypesAddModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_D02_SaleTask_TypesAddModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            if (ModelState.IsValid)
            {

                //var db = new MyVoltageDbContext(_options);

                //var existingCount = db.A09_Flags_Types.Where(p => p.FlagTypeName == model.FlagTypeName).Count();

                //if (existingCount > 0)
                //{
                //    model.IsSuccess = false;
                //    ModelState.AddModelError("FlagTypeName", $"{model.FlagTypeName} - Already exist");
                //    return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Add.cshtml", model);
                //}

                Data.D02_SaleTask_Type task_Type = new D02_SaleTask_Type()
                {
                    DashboardURL = "",
                    StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]),
                    DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]),
                    MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroup"]),
                    DefaultMeetingAgendaID = Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]),
                    Description = model.Description,
                    Heading = model.Heading,
                    HowToURL = "",
                    PriorityID = Convert.ToInt32(Request.Form["Priority"]),
                    ReportingToUserID = Request.Form["ReportingToUser"],
                    ResponsibleUserID = Request.Form["ResponsibleUser"],
                    TemplateNo = model.TemplateNo,
                    CreateIndividualFlagsForCompaniesLinked = false,
                    MinRequiredToClear = model.MinRequiredToClear,
                    Identifier = model.Identifier,
                    TaskClassificationID = Convert.ToInt32(Request.Form["TaskClassification"]),
                    WorkflowGroupID = Convert.ToInt32(Request.Form["WorkflowGroupID"]),
                };
                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]))
                    task_Type.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);


                if (!string.IsNullOrEmpty(Request.Form["BusinessDepartment"]))
                {
                    var wf = db.WorkflowGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["WorkflowGroupID"])).SingleOrDefault();
                    if (!wf.BusinessDepartmentID.HasValue || wf.BusinessDepartmentID.Value != Convert.ToInt32(Request.Form["BusinessDepartment"]))
                        task_Type.BusinessDepartmentID = Convert.ToInt32(Request.Form["BusinessDepartment"]);
                    else if (wf.BusinessDepartmentID.HasValue && wf.BusinessDepartmentID.Value == Convert.ToInt32(Request.Form["BusinessDepartment"]))
                        task_Type.BusinessDepartmentID = null;
                }

                db.Add(task_Type);
                db.SaveChanges();

                if (model.HowToDocument != null)
                {
                    #region Azure Upload

                    string shareName = "d02-saletasktypes";
                    string dirName = $"{task_Type.ID}".ToLower();
                    string fileName = task_Type.ID.ToString() + System.IO.Path.GetExtension(model.HowToDocument.FileName);
                    fileName = fileName.ToLower();

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryTemp = share.GetDirectoryClient(dirName);
                    var directoryTempResult = directoryTemp.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directoryTemp.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.HowToDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    #endregion

                    task_Type.HowToURL = $"{dirName}/{fileName}";
                    db.Update(task_Type);
                    db.SaveChanges();
                }

                model.ResultFlagTypeID = task_Type.ID.ToString();
                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.D02_SaleTask_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");

            var workflowGroups = db.WorkflowGroups.ToList();
            SiteAdmin_D02_SaleTask_TypesEditModel model = new SiteAdmin_D02_SaleTask_TypesEditModel()
            {
                Priority = (from p in db.SiteAdmin_Priorities
                            select new SelectListItem()
                            {
                                Text = p.PriorityName,
                                Value = p.ID.ToString(),
                                Selected = item.PriorityID == p.ID ? true : false,
                            }).ToList(),
                ReportingToUser = new List<SelectListItem>(),
                ResponsibleUser = new List<SelectListItem>(),
                LinkedSecureArea = new List<SelectListItem>(),
                Description = item.Description,
                Heading = item.Heading,
                TemplateNo = item.TemplateNo,
                D02_SaleTask_Type = item,
                D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == item.ID).ToList(),
                MinRequiredToClear = item.MinRequiredToClear,
                DefautlMinPlanned = item.DefautlMinPlanned,
                StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !item.StatusGroupID.HasValue },
                },
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem>(),
                MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !item.MeetingAgendaGroupID.HasValue },
                },
                DefaultMeetingAgenda = new List<SelectListItem>(),
                MeetingAgendaItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem>(),
                TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                      select new SelectListItem()
                                      {
                                          Text = p.GetDescription(),
                                          Value = ((int)p).ToString(),
                                          Selected = item.TaskClassification == (p) ? true : false,
                                      }).ToList(),
                WorkflowGroupID = (from p in workflowGroups
                                   orderby p.WorkflowGroupName
                                   select new SelectListItem()
                                   {
                                       Text = p.WorkflowGroupName,
                                       Value = p.ID.ToString(),
                                       Selected = item.WorkflowGroupID.HasValue && item.WorkflowGroupID.Value == p.ID,
                                   }).ToList(),
                WorkflowGroupItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.BusinessPillarItem>(),
                Identifier = item.Identifier,
                ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = item.ReportingToUserRequiresCompletedState.HasValue && item.ReportingToUserRequiresCompletedState.Value ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = !item.ReportingToUserRequiresCompletedState.HasValue || !item.ReportingToUserRequiresCompletedState.Value ? true : false },
                },
            };

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = item.StatusGroupID.HasValue && item.StatusGroupID.Value == p.ID,
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem statusItem = new SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            model.MeetingAgendaGroup.AddRange((from p in siteAdmin_MeetingAgendaGroups
                                               select new SelectListItem()
                                               {
                                                   Text = p.MeetingAgendaGroupName,
                                                   Value = p.ID.ToString(),
                                                   Selected = item.MeetingAgendaGroupID.HasValue && item.MeetingAgendaGroupID.Value == p.ID,
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.ResponsibleUserID == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = item.ReportingToUserID == user.Id ? true : false });
            }

            #region Workflows

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = !item.LinkedSecureAreaID.HasValue ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = item.LinkedSecureAreaID.HasValue && item.LinkedSecureAreaID.Value == ((int)sa.Item1) ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_D02_SaleTask_TypesEditModel.BusinessPillarItem()
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

            var businessDepartments = db.BusinessDepartments.Where(p => p.BusinessPillarID == 8).ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new SiteAdmin_D02_SaleTask_TypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_D02_SaleTask_TypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Edit(int ID, SiteAdmin_D02_SaleTask_TypesEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.D02_SaleTask_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");

            model.TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                        select new SelectListItem()
                                        {
                                            Text = p.GetDescription(),
                                            Value = ((int)p).ToString(),
                                            Selected = Request.Form["TaskClassification"].ToString() == ((int)p).ToString() ? true : false,
                                        }).ToList();

            var workflowGroups = db.WorkflowGroups.ToList();
            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Form["WorkflowGroupID"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList();

            model.WorkflowGroupItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.BusinessPillarItem>();
            model.Priority = (from p in db.SiteAdmin_Priorities
                              select new SelectListItem()
                              {
                                  Text = p.PriorityName,
                                  Value = p.ID.ToString(),
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString() ? true : false,
                              }).ToList();
            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();

            model.D02_SaleTask_Type = item;
            model.D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == item.ID).ToList();

            model.StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["StatusGroup"]) },
                };

            model.DefaultStatus = new List<SelectListItem>();
            model.StatusItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem>();

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = Request.Form["StatusGroup"].ToString() == p.ID.ToString(),
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem statusItem = new SiteAdmin_D02_SaleTask_TypesEditModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            model.MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["MeetingAgendaGroup"]) },
                };

            model.DefaultMeetingAgenda = new List<SelectListItem>();
            model.MeetingAgendaItems = new List<SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem>();

            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            model.MeetingAgendaGroup.AddRange((from p in siteAdmin_MeetingAgendaGroups
                                               select new SelectListItem()
                                               {
                                                   Text = p.MeetingAgendaGroupName,
                                                   Value = p.ID.ToString(),
                                                   Selected = Request.Form["MeetingAgendaGroup"].ToString() == p.ID.ToString(),
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_D02_SaleTask_TypesEditModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"] == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"] == user.Id ? true : false });
            }

            model.ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? false : true },
                };

            #region Workflows

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = !item.LinkedSecureAreaID.HasValue ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = item.LinkedSecureAreaID.HasValue && item.LinkedSecureAreaID.Value == ((int)sa.Item1) ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_D02_SaleTask_TypesEditModel.BusinessPillarItem()
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

            var businessDepartments = db.BusinessDepartments.Where(p => p.BusinessPillarID == 8).ToList();
            model.BusinessDepartmentItems = (from p in businessDepartments
                                             select new SiteAdmin_D02_SaleTask_TypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_D02_SaleTask_TypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            if (ModelState.IsValid)
            {

                //var db = new MyVoltageDbContext(_options);

                //var existingCount = db.A09_Flags_Types.Where(p => p.FlagTypeName == model.FlagTypeName).Count();

                //if (existingCount > 0)
                //{
                //    model.IsSuccess = false;
                //    ModelState.EditModelError("FlagTypeName", $"{model.FlagTypeName} - Already exist");
                //    return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit.cshtml", model);
                //}

                if (!string.IsNullOrEmpty(Request.Form["ReportingToUserRequiresCompletedState"].ToString()) && item.ReportingToUserRequiresCompletedState != Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]))
                {
                    item.ReportingToUserRequiresCompletedState = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]);
                }

                item.Description = model.Description;
                item.Heading = model.Heading;
                item.PriorityID = Convert.ToInt32(Request.Form["Priority"]);
                item.ReportingToUserID = Request.Form["ReportingToUser"];
                item.ResponsibleUserID = Request.Form["ResponsibleUser"];
                item.TemplateNo = model.TemplateNo;
                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]))
                    item.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);
                else
                    item.LinkedSecureAreaID = null;
                item.MinRequiredToClear = model.MinRequiredToClear;
                item.DefautlMinPlanned = model.DefautlMinPlanned;

                item.StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]);
                item.DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]);
                item.MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroup"]);
                item.DefaultMeetingAgendaID = Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]);
                item.TaskClassificationID = Convert.ToInt32(Request.Form["TaskClassification"]);
                item.WorkflowGroupID = Convert.ToInt32(Request.Form["WorkflowGroupID"]);
                item.Identifier = model.Identifier;

                if (!string.IsNullOrEmpty(Request.Form["BusinessDepartment"]))
                {
                    var wf = db.WorkflowGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["WorkflowGroupID"])).SingleOrDefault();
                    if (!wf.BusinessDepartmentID.HasValue || wf.BusinessDepartmentID.Value != Convert.ToInt32(Request.Form["BusinessDepartment"]))
                        item.BusinessDepartmentID = Convert.ToInt32(Request.Form["BusinessDepartment"]);
                    else if (wf.BusinessDepartmentID.HasValue && wf.BusinessDepartmentID.Value == Convert.ToInt32(Request.Form["BusinessDepartment"]))
                        item.BusinessDepartmentID = null;
                }

                db.Update(item);
                db.SaveChanges();

                if (model.HowToDocument != null)
                {
                    #region Azure Upload

                    string shareName = "d02-saletasktypes";
                    string dirName = $"{item.ID}".ToLower();
                    string fileName = item.ID.ToString() + System.IO.Path.GetExtension(model.HowToDocument.FileName);
                    fileName = fileName.ToLower();

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryTemp = share.GetDirectoryClient(dirName);
                    var directoryTempResult = directoryTemp.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directoryTemp.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.HowToDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    #endregion

                    item.HowToURL = $"{dirName}/{fileName}";
                    db.Update(item);
                    db.SaveChanges();
                }

                model.D02_SaleTask_Type = item;
                model.ResultFlagTypeID = item.ID.ToString();
                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes_Delete/{taskTypeID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Delete(int taskTypeID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskType = db.D02_SaleTask_Types.Where(p => p.ID == taskTypeID).SingleOrDefault();

            if (taskType != null)
            {
                taskType.IsDeleted = true;
                db.Update(taskType);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes_Restore/{taskTypeID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_Restore(int taskTypeID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskType = db.D02_SaleTask_Types.Where(p => p.ID == taskTypeID).SingleOrDefault();

            if (taskType != null)
            {
                taskType.IsDeleted = false;
                db.Update(taskType);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/AddCompany/{taskTypeID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_AddCompany(int taskTypeID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskTypeCompany = db.D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == taskTypeID && p.CompanyID == companyID).SingleOrDefault();

            if (taskTypeCompany == null)
            {
                taskTypeCompany = new D02_SaleTask_Type_Company()
                {
                    TaskTypeID = taskTypeID,
                    CompanyID = companyID,
                };
                db.Add(taskTypeCompany);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit/{taskTypeID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/DeleteCompany/{taskTypeID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_DeleteCompany(int taskTypeID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskTypeCompany = db.D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == taskTypeID && p.CompanyID == companyID).SingleOrDefault();

            if (taskTypeCompany != null)
            {
                db.Remove(taskTypeCompany);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/Edit/{taskTypeID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/CreateTasksForCompany/{companyID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_CreateTasksForCompany(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var D02_SaleTask_Types = db.D02_SaleTask_Types.ToList();
            var D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();

            foreach (var D02_SaleTask_Type in D02_SaleTask_Types)
            {
                if (D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == D02_SaleTask_Type.ID && p.CompanyID == companyID).Count() == 0)
                    continue;
                if (D02_SaleTask_Type.IsDeleted.HasValue && D02_SaleTask_Type.IsDeleted.Value)
                    continue;

                var existing = (from p in db.D02_SaleTasks
                                where p.TaskTypeID == D02_SaleTask_Type.ID
                                && p.CompanyID == companyID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    existing = new D02_SaleTask()
                    {
                        CompanyID = companyID,
                        DateCreated = DateTime.Now,
                        DateEnded = null,
                        DateStarted = null,
                        DueDate = DateTime.Now,
                        KmTravelRequired = null,
                        Level = null,
                        ReportingToUserID = D02_SaleTask_Type.ReportingToUserID,
                        ResponsibleUserID = D02_SaleTask_Type.ResponsibleUserID,
                        StatusID = D02_SaleTask_Type.DefaultStatusID.HasValue ? D02_SaleTask_Type.DefaultStatusID.Value : 1,
                        StockUsed = "",
                        TaskTypeID = D02_SaleTask_Type.ID,
                    };

                    db.Add(existing);
                    db.SaveChanges();
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/LinkTasksForCompany/{companyID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_LinkTasksForCompany(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var D02_SaleTask_Types = db.D02_SaleTask_Types.ToList();
            var D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();

            foreach (var D02_SaleTask_Type in D02_SaleTask_Types)
            {
                if (D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == D02_SaleTask_Type.ID && p.CompanyID == companyID).Count() != 0)
                    continue;
                if (D02_SaleTask_Type.IsDeleted.HasValue && D02_SaleTask_Type.IsDeleted.Value)
                    continue;

                var existing = (from p in db.D02_SaleTask_Type_Companies
                                where p.TaskTypeID == D02_SaleTask_Type.ID
                                && p.CompanyID == companyID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    existing = new D02_SaleTask_Type_Company()
                    {
                        CompanyID = companyID,
                        TaskTypeID = D02_SaleTask_Type.ID,
                    };

                    db.Add(existing);
                    db.SaveChanges();
                }
                D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes/UnLinkTasksForCompany/{companyID}")]
        public async Task<IActionResult> SiteAdmin_D02_SaleTaskTypes_UnLinkTasksForCompany(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var D02_SaleTask_Types = db.D02_SaleTask_Types.ToList();
            var D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();

            foreach (var D02_SaleTask_Type in D02_SaleTask_Types)
            {
                if (D02_SaleTask_Type_Companies.Where(p => p.TaskTypeID == D02_SaleTask_Type.ID && p.CompanyID == companyID).Count() == 0)
                    continue;
                if (D02_SaleTask_Type.IsDeleted.HasValue && D02_SaleTask_Type.IsDeleted.Value)
                    continue;

                var existing = (from p in db.D02_SaleTask_Type_Companies
                                where p.TaskTypeID == D02_SaleTask_Type.ID
                                && p.CompanyID == companyID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
                    db.SaveChanges();
                }
                D02_SaleTask_Type_Companies = db.D02_SaleTask_Type_Companies.ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/SiteAdmin/SiteAdmin_D02_SaleTaskTypes");
        }

    }
}
