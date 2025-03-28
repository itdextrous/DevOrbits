using Azure;
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
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_A08TaskTypesModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class SiteAdmin_A08TaskTypesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_A08TaskTypesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var a08_Tasks = db.A08_Tasks.ToList();
            var a08_Task_Types = db.A08_Task_Types.ToList();
            var a08_Task_Type_Companies = db.A08_Task_Type_Companies.ToList();
            var a08_Task_Type_Frequencies = db.A08_Task_Type_Frequencies.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();


            SiteAdmin_A08_Task_TypesModel model = new SiteAdmin_A08_Task_TypesModel()
            {
                SiteAdmin_A08TaskTypesItems = new List<SiteAdmin_A08_Task_TypesModel.SiteAdmin_A08TaskTypesItem>(),
                TaskClassification = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Task Classifications]", Selected = string.IsNullOrEmpty(Request.Query["TaskClassification"].ToString()) }
                },
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

            model.TaskClassification.AddRange((from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                               orderby p.GetDescription()
                                               select new SelectListItem()
                                               {
                                                   Text = p.GetDescription(),
                                                   Value = ((int)p).ToString(),
                                                   Selected = Request.Query["TaskClassification"].ToString() == ((int)p).ToString() ? true : false,
                                               }).ToList());
            model.TaskClassification = model.TaskClassification.OrderBy(p => p.Text).ToList();


            model.SecureAreaGroupID.AddRange((from p in workflowGroups
                                              orderby p.WorkflowGroupName
                                              select new SelectListItem()
                                              {
                                                  Text = p.WorkflowGroupName,
                                                  Value = p.ID.ToString(),
                                                  Selected = Request.Query["SecureAreaGroupID"].ToString() == p.ID.ToString() ? true : false,
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

            foreach (var task_Type in a08_Task_Types)
            {
                //if (!string.IsNullOrEmpty(Request.Query["Department"].ToString()))
                //{
                //    if (Convert.ToInt32(Request.Query["Department"]) != task_Type.DepartmentID)
                //        continue;
                //}

                if (!string.IsNullOrEmpty(Request.Query["TaskClassification"].ToString()))
                {
                    if (Convert.ToInt32(Request.Query["TaskClassification"]) != (task_Type.TaskClassificationID.HasValue ? task_Type.TaskClassificationID.Value : 0))
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"].ToString()))
                {
                    if (!task_Type.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != task_Type.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != task_Type.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != task_Type.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != task_Type.PriorityID)
                        continue;
                }

                var opProfResponsible = opProfs.Where(p => p.UserID == task_Type.ResponsibleUserID).SingleOrDefault();
                var opProfReportingToU = opProfs.Where(p => p.UserID == task_Type.ReportingToUserID).SingleOrDefault();
                SiteAdmin_A08_Task_TypesModel.SiteAdmin_A08TaskTypesItem item = new SiteAdmin_A08_Task_TypesModel.SiteAdmin_A08TaskTypesItem()
                {
                    A08_Task_Type_Companies = a08_Task_Type_Companies.Where(p => p.TaskTypeID == task_Type.ID).ToList(),
                    A08_Task_Type_Frequencies = a08_Task_Type_Frequencies.Where(p => p.TaskTypeID == task_Type.ID).ToList(),
                    ID = task_Type.ID,
                    DashboardURL = task_Type.DashboardURL,
                    Description = task_Type.Description,
                    Heading = task_Type.Heading,
                    HowToURL = task_Type.HowToURL,
                    PriorityID = task_Type.PriorityID,
                    ReportingToUserID = task_Type.ReportingToUserID,
                    ResponsibleUserID = task_Type.ResponsibleUserID,
                    TemplateNo = task_Type.ID,
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
                    SecureAreaGroupID = task_Type.SecureAreaGroupID,
                    ReportingToUserAccepted = task_Type.ReportingToUserAccepted,
                    ResponsibleUserAcceptedDate = task_Type.ResponsibleUserAcceptedDate,
                    ResponsibleUserAccepted = task_Type.ResponsibleUserAccepted,
                    ReportingToUserRequiresCompletedState = task_Type.ReportingToUserRequiresCompletedState,
                    ReportingToUserAcceptedDate = task_Type.ReportingToUserAcceptedDate,
                    PreviousRunDate = null,
                    IsDeleted = task_Type.IsDeleted,
                    DefautlMinPlanned = task_Type.DefautlMinPlanned,
                    TaskClassificationID = task_Type.TaskClassificationID,
                    Identifier = task_Type.Identifier,
                    DefaultMeetingAgendaID = task_Type.DefaultMeetingAgendaID,
                    SiteAdmin_MeetingAgenda = "[NONE]",
                    SiteAdmin_MeetingAgendaGroup = task_Type.MeetingAgendaGroupID.HasValue ? siteAdmin_MeetingAgendaGroups.Where(p => p.ID == task_Type.MeetingAgendaGroupID.Value).SingleOrDefault() : null,
                    MeetingAgendaGroupID = task_Type.MeetingAgendaGroupID,
                    BusinessDepartmentID = task_Type.BusinessDepartmentID,
                    BusinessDepartmentName = "",
                    BusinessPillarName = "",
                    HasComplianceCheck = task_Type.HasComplianceCheck,
                    UpdateExistingTask = task_Type.UpdateExistingTask,
                    WorkflowGroupName = "",
                };

                if (!string.IsNullOrEmpty(Request.Query["Company"].ToString()))
                {
                    if (!item.A08_Task_Type_Companies.Select(p => p.CompanyID).Contains(Convert.ToInt32(Request.Query["Company"])))
                        continue;
                }

                if (task_Type.DefaultStatusID.HasValue)
                {
                    var status = siteAdmin_Statuses.Where(p => p.ID == task_Type.DefaultStatusID.Value).SingleOrDefault();
                    item.SiteAdmin_Status = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}";
                }

                if (a08_Tasks.Where(p => p.TaskTypeID == task_Type.ID).Count() > 0)
                    item.PreviousRunDate = a08_Tasks.Where(p => p.TaskTypeID == task_Type.ID).Select(p => p.DateCreated).Max();

                int workflowID = 1;
                if (task_Type.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == task_Type.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (task_Type.SecureAreaGroupID.HasValue)
                    workflowID = task_Type.SecureAreaGroupID.Value;

                if (task_Type.BusinessDepartmentID.HasValue)
                {
                    var department = businessDepartments.Where(p => p.ID == task_Type.BusinessDepartmentID.Value).SingleOrDefault();
                    item.BusinessDepartmentName = department.BusinessDepartmentName;
                    var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    item.BusinessPillarName = pillar.BusinessPillarName;
                }

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    item.WorkflowGroupName = wf.WorkflowGroupName;
                    if (wf.BusinessDepartmentID.HasValue && !task_Type.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        item.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        item.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                model.SiteAdmin_A08TaskTypesItems.Add(item);
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/SiteAdmin_A08TaskTypes.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Add")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var workflowGroups = db.WorkflowGroups.ToList();
            SiteAdmin_A08_Task_TypesAddModel model = new SiteAdmin_A08_Task_TypesAddModel()
            {
                TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                      where p != TaskClassificationEnum.NotLinked
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
                CreateIndividualFlagsForCompaniesLinked = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Task per company)" },
                    new SelectListItem() { Value = "false", Text = "No (One task for all companies)" },
                },
                StatusGroup = new List<SelectListItem>(),
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_A08_Task_TypesAddModel.StatusItem>(),
                MeetingAgendaGroup = new List<SelectListItem>(),
                DefaultMeetingAgenda = new List<SelectListItem>(),
                MeetingAgendaItems = new List<SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem>(),
                WorkflowGroupID = (from p in workflowGroups
                                   orderby p.WorkflowGroupName
                                   select new SelectListItem()
                                   {
                                       Text = p.WorkflowGroupName,
                                       Value = p.ID.ToString(),
                                   }).ToList(),
                WorkflowGroupItems = new List<SiteAdmin_A08_Task_TypesAddModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<SiteAdmin_A08_Task_TypesAddModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<SiteAdmin_A08_Task_TypesAddModel.BusinessPillarItem>(),
                UpdateExistingTask = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "Create New" },
                    new SelectListItem() { Value = "true", Text = "Update Existing Task" },
                },
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
                SiteAdmin_A08_Task_TypesAddModel.StatusItem statusItem = new SiteAdmin_A08_Task_TypesAddModel.StatusItem()
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
                SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper())).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName });
                if (!user.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                    model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

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
                                         select new SiteAdmin_A08_Task_TypesAddModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A08_Task_TypesAddModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_A08_Task_TypesAddModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Add")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_Add(SiteAdmin_A08_Task_TypesAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                        where p != TaskClassificationEnum.NotLinked
                                        select new SelectListItem()
                                        {
                                            Text = p.GetDescription(),
                                            Value = ((int)p).ToString(),
                                            Selected = Request.Form["TaskClassification"].ToString() == ((int)p).ToString() ? true : false,
                                        }).ToList();

            model.UpdateExistingTask = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Update Existing Task", Selected = Convert.ToBoolean(Request.Form["UpdateExistingTask"]) },
                    new SelectListItem() { Value = "false", Text = "Create New", Selected = !Convert.ToBoolean(Request.Form["UpdateExistingTask"]) },
                };

            var workflowGroups = db.WorkflowGroups.ToList();
            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Form["WorkflowGroupID"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList();

            model.WorkflowGroupItems = new List<SiteAdmin_A08_Task_TypesAddModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<SiteAdmin_A08_Task_TypesAddModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<SiteAdmin_A08_Task_TypesAddModel.BusinessPillarItem>();
            model.Priority = (from p in db.SiteAdmin_Priorities
                              select new SelectListItem()
                              {
                                  Text = p.PriorityName,
                                  Value = p.ID.ToString(),
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString() ? true : false,
                              }).ToList();
            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();
            model.CreateIndividualFlagsForCompaniesLinked = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Task per company)", Selected = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]) ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (One task for all companies)", Selected = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]) ? false : true },
                };

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper())).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = Request.Form["ResponsibleUser"].ToString() == user.Id ? true : false });
                if (!user.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                    model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = Request.Form["ReportingToUser"].ToString() == user.Id ? true : false });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();
            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"].ToString()) ? true : false });
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
            model.StatusItems = new List<SiteAdmin_A08_Task_TypesAddModel.StatusItem>();

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
                SiteAdmin_A08_Task_TypesAddModel.StatusItem statusItem = new SiteAdmin_A08_Task_TypesAddModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            model.MeetingAgendaGroup = new List<SelectListItem>();
            model.DefaultMeetingAgenda = new List<SelectListItem>();
            model.MeetingAgendaItems = new List<SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem>();

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
                SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A08_Task_TypesAddModel.MeetingAgendaItem()
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
                                         select new SiteAdmin_A08_Task_TypesAddModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A08_Task_TypesAddModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        where p.BusinessDepartmentID.HasValue
                                        && businessDepartments.Select(c => c.ID).Contains(p.BusinessDepartmentID.Value)
                                        select new SiteAdmin_A08_Task_TypesAddModel.WorkflowGroupItem()
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
                //    return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Add.cshtml", model);
                //}

                Data.A08_Task_Type task_Type = new A08_Task_Type()
                {
                    DashboardURL = "",
                    TaskClassificationID = Convert.ToInt32(Request.Form["TaskClassification"]),
                    StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]),
                    DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]),
                    Description = model.Description,
                    Heading = model.Heading,
                    HowToURL = "",
                    PriorityID = Convert.ToInt32(Request.Form["Priority"]),
                    ReportingToUserID = Request.Form["ReportingToUser"].ToString(),
                    ResponsibleUserID = Request.Form["ResponsibleUser"].ToString(),
                    TemplateNo = model.TemplateNo,
                    CreateIndividualFlagsForCompaniesLinked = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]),
                    UpdateExistingTask = Convert.ToBoolean(Request.Form["UpdateExistingTask"]),
                    MinRequiredToClear = model.MinRequiredToClear,
                    Identifier = model.Identifier,
                    MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroup"]),
                    DefaultMeetingAgendaID = Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]),
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

                if (_userManager.FindByIdAsync(task_Type.ResponsibleUserID).Result.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                {
                    task_Type.ResponsibleUserAccepted = true;
                    task_Type.ResponsibleUserAcceptedDate = DateTime.Now;
                }

                db.Add(task_Type);
                db.SaveChanges();

                task_Type.TemplateNo = task_Type.ID;
                db.Update(task_Type);
                db.SaveChanges();

                //if (model.HowToDocument != null)
                //{
                //    #region FTPUpload

                //    string un = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_UN"];
                //    string pwd = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_Password"];

                //    string fileName = task_Type.ID.ToString() + System.IO.Path.GetExtension(model.HowToDocument.FileName);
                //    // Copy the contents of the file to the request stream.
                //    Stream uploadFile = new MemoryStream();
                //    model.HowToDocument.CopyTo(uploadFile);
                //    byte[] fileContents = new byte[uploadFile.Length];
                //    uploadFile.Position = 0;
                //    uploadFile.Read(fileContents, 0, fileContents.Length);

                //    FTPProvider.UploadFile("A08TaskTypes", fileName, fileContents, un, pwd);

                //    #endregion


                //    task_Type.HowToURL = $"A08TaskTypes/{fileName}";
                //    db.Update(task_Type);
                //    db.SaveChanges();
                //}

                model.ResultFlagTypeID = task_Type.ID.ToString();
                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.A08_Task_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes");

            var workflowGroups = db.WorkflowGroups.ToList();
            SiteAdmin_A08_Task_TypesEditModel model = new SiteAdmin_A08_Task_TypesEditModel()
            {
                TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                      where p != TaskClassificationEnum.NotLinked
                                      select new SelectListItem()
                                      {
                                          Text = p.GetDescription(),
                                          Value = ((int)p).ToString(),
                                          Selected = item.TaskClassification == (p) ? true : false,
                                      }).ToList(),
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
                TemplateNo = item.ID,
                CreateIndividualFlagsForCompaniesLinked = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Task per company)", Selected = item.CreateIndividualFlagsForCompaniesLinked },
                    new SelectListItem() { Value = "false", Text = "No (One task for all companies)", Selected = item.CreateIndividualFlagsForCompaniesLinked ? false : true },
                },
                UpdateExistingTask = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Update Existing Task", Selected = item.UpdateExistingTask.HasValue && item.UpdateExistingTask.Value },
                    new SelectListItem() { Value = "false", Text = "Create New", Selected = !item.UpdateExistingTask.HasValue || !item.UpdateExistingTask.Value },
                },
                HasComplianceCheck = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes", Selected = item.HasComplianceCheck.HasValue && item.HasComplianceCheck.Value },
                    new SelectListItem() { Value = "false", Text = "No", Selected = !item.HasComplianceCheck.HasValue || !item.HasComplianceCheck.Value },
                },
                A08_Task_Type = item,
                A08_Task_Type_CompanyItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem>(),
                A08_Task_Type_FrequencyItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem>(),
                MinRequiredToClear = item.MinRequiredToClear,
                StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !item.StatusGroupID.HasValue },
                },
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_A08_Task_TypesEditModel.StatusItem>(),
                A08_Task_Type_LogItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_LogItem>(),
                ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = item.ReportingToUserRequiresCompletedState.HasValue && item.ReportingToUserRequiresCompletedState.Value ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = !item.ReportingToUserRequiresCompletedState.HasValue || !item.ReportingToUserRequiresCompletedState.Value ? true : false },
                },
                DefautlMinPlanned = item.DefautlMinPlanned,
                WorkflowGroupID = (from p in workflowGroups
                                   orderby p.WorkflowGroupName
                                   select new SelectListItem()
                                   {
                                       Text = p.WorkflowGroupName,
                                       Value = p.ID.ToString(),
                                       Selected = item.SecureAreaGroupID.HasValue && item.SecureAreaGroupID.Value == p.ID,
                                   }).ToList(),
                WorkflowGroupItems = new List<SiteAdmin_A08_Task_TypesEditModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<SiteAdmin_A08_Task_TypesEditModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<SiteAdmin_A08_Task_TypesEditModel.BusinessPillarItem>(),
                Identifier = item.Identifier,
                MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !item.MeetingAgendaGroupID.HasValue },
                },
                DefaultMeetingAgenda = new List<SelectListItem>(),
                MeetingAgendaItems = new List<SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem>(),
                LinkedTasks = db.A08_Tasks.Where(p => p.TaskTypeID == item.ID).Count(),
            };

            var a08_Task_Type_Companies = db.A08_Task_Type_Companies.Where(p => p.TaskTypeID == item.ID).ToList();
            foreach (var a08_Task_Type_Company in a08_Task_Type_Companies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == a08_Task_Type_Company.CompanyID).SingleOrDefault();
                SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem a08_Task_Type_CompanyItem = new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem()
                {
                    CompanyID = a08_Task_Type_Company.CompanyID,
                    CompanyName = company.Name,
                    ID = a08_Task_Type_Company.ID,
                    TaskTypeID = a08_Task_Type_Company.TaskTypeID,
                };
                model.A08_Task_Type_CompanyItems.Add(a08_Task_Type_CompanyItem);
            }

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
                SiteAdmin_A08_Task_TypesEditModel.StatusItem statusItem = new SiteAdmin_A08_Task_TypesEditModel.StatusItem()
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
                SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted && p.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper())).ToList())
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = item.ResponsibleUserID == user.Id ? true : false });
                if (!user.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                    model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName} ({user.Email})" : user.UserName, Selected = item.ReportingToUserID == user.Id ? true : false });
            }

            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

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
                                         select new SiteAdmin_A08_Task_TypesEditModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A08_Task_TypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new SiteAdmin_A08_Task_TypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            foreach (var freq in db.A08_Task_Type_Frequencies.Where(p => p.TaskTypeID == item.ID).ToList())
            {
                SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem freqItem = new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem()
                {
                    FrequencyID = freq.FrequencyID,
                    ID = freq.ID,
                    OneTimeExecuteDate = freq.OneTimeExecuteDate,
                    TaskTypeID = freq.TaskTypeID,
                    NextRunDate = freq.NextRunDate,
                };

                model.A08_Task_Type_FrequencyItems.Add(freqItem);
            }

            var a08_Task_Type_Logs = db.A08_Task_Type_Logs.Where(p => p.TaskTypeID == ID).ToList();
            foreach (var log in a08_Task_Type_Logs.OrderByDescending(p => p.DateCreated))
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == log.UserID).SingleOrDefault();
                model.A08_Task_Type_LogItems.Add(new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_LogItem()
                {
                    DateCreated = log.DateCreated,
                    UserID = log.UserID,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    TaskTypeID = log.TaskTypeID,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                });
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_Edit(int ID, SiteAdmin_A08_Task_TypesEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.A08_Task_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes");

            model.LinkedTasks = db.A08_Tasks.Where(p => p.TaskTypeID == item.ID).Count();
            var workflowGroups = db.WorkflowGroups.ToList();
            model.TaskClassification = (from p in ((TaskClassificationEnum[])Enum.GetValues(typeof(TaskClassificationEnum)))
                                        where p != TaskClassificationEnum.NotLinked
                                        select new SelectListItem()
                                        {
                                            Text = p.GetDescription(),
                                            Value = ((int)p).ToString(),
                                            Selected = Request.Form["TaskClassification"].ToString() == ((int)p).ToString() ? true : false,
                                        }).ToList();

            model.WorkflowGroupID = (from p in workflowGroups
                                     orderby p.WorkflowGroupName
                                     select new SelectListItem()
                                     {
                                         Text = p.WorkflowGroupName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Form["WorkflowGroupID"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList();

            model.WorkflowGroupItems = new List<SiteAdmin_A08_Task_TypesEditModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<SiteAdmin_A08_Task_TypesEditModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<SiteAdmin_A08_Task_TypesEditModel.BusinessPillarItem>();

            var siteAdmin_Priorities = db.SiteAdmin_Priorities.ToList();
            model.Priority = (from p in siteAdmin_Priorities
                              select new SelectListItem()
                              {
                                  Text = p.PriorityName,
                                  Value = p.ID.ToString(),
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString() ? true : false,
                              }).ToList();
            model.ReportingToUser = new List<SelectListItem>();
            model.ResponsibleUser = new List<SelectListItem>();
            model.CreateIndividualFlagsForCompaniesLinked = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Task per company)", Selected = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]) ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (One task for all companies)", Selected = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]) ? false : true },
                };
            model.UpdateExistingTask = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Update Existing Task", Selected = item.UpdateExistingTask.HasValue && item.UpdateExistingTask.Value },
                    new SelectListItem() { Value = "false", Text = "Create New", Selected = !item.UpdateExistingTask.HasValue || !item.UpdateExistingTask.Value },
                };

            model.HasComplianceCheck = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes", Selected = Convert.ToBoolean(Request.Form["HasComplianceCheck"]) },
                    new SelectListItem() { Value = "false", Text = "No", Selected = !Convert.ToBoolean(Request.Form["HasComplianceCheck"]) },
                };

            model.ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? false : true },
                };

            model.A08_Task_Type = item;
            model.A08_Task_Type_CompanyItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem>();

            var a08_Task_Type_Companies = db.A08_Task_Type_Companies.Where(p => p.TaskTypeID == item.ID).ToList();
            foreach (var a08_Task_Type_Company in a08_Task_Type_Companies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == a08_Task_Type_Company.CompanyID).SingleOrDefault();
                SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem a08_Task_Type_CompanyItem = new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_CompanyItem()
                {
                    CompanyID = a08_Task_Type_Company.CompanyID,
                    CompanyName = company.Name,
                    ID = a08_Task_Type_Company.ID,
                    TaskTypeID = a08_Task_Type_Company.TaskTypeID,
                };
                model.A08_Task_Type_CompanyItems.Add(a08_Task_Type_CompanyItem);
            }

            model.A08_Task_Type_FrequencyItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem>();
            foreach (var freq in db.A08_Task_Type_Frequencies.Where(p => p.TaskTypeID == item.ID).ToList())
            {
                SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem freqItem = new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_FrequencyItem()
                {
                    FrequencyID = freq.FrequencyID,
                    ID = freq.ID,
                    OneTimeExecuteDate = freq.OneTimeExecuteDate,
                    TaskTypeID = freq.TaskTypeID,
                    NextRunDate = freq.NextRunDate,
                };

                var latestCreatedTask = (from p in db.A08_Tasks
                                         where p.TaskTypeID == item.ID
                                         orderby p.DateCreated descending
                                         select p).FirstOrDefault();

                if (latestCreatedTask != null)
                    freqItem.PreviousRunDate = latestCreatedTask.DateCreated;

                model.A08_Task_Type_FrequencyItems.Add(freqItem);
            }
            //model.MinRequiredToClear = item.MinRequiredToClear;

            model.StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["StatusGroup"].ToString()) },
                };

            model.DefaultStatus = new List<SelectListItem>();
            model.StatusItems = new List<SiteAdmin_A08_Task_TypesEditModel.StatusItem>();

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = Request.Form["StatusGroup"] == p.ID.ToString(),
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_A08_Task_TypesEditModel.StatusItem statusItem = new SiteAdmin_A08_Task_TypesEditModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            model.MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["MeetingAgendaGroup"].ToString()) },
                };

            model.DefaultMeetingAgenda = new List<SelectListItem>();
            model.MeetingAgendaItems = new List<SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem>();

            var siteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.Where(p => !p.IsDeleted).ToList();
            model.MeetingAgendaGroup.AddRange((from p in siteAdmin_MeetingAgendaGroups
                                               select new SelectListItem()
                                               {
                                                   Text = p.MeetingAgendaGroupName,
                                                   Value = p.ID.ToString(),
                                                   Selected = Request.Form["MeetingAgendaGroup"] == p.ID.ToString(),
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A08_Task_TypesEditModel.MeetingAgendaItem()
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
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ResponsibleUser"].ToString() == user.Id ? true : false });
                if (!user.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                    model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUser"].ToString() == user.Id ? true : false });
            }

            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.ReportingToUser = model.ReportingToUser.OrderBy(p => p.Text).ToList();

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
                                         select new SiteAdmin_A08_Task_TypesEditModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A08_Task_TypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new SiteAdmin_A08_Task_TypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"].ToString()) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureArea"].ToString() == ((int)sa.Item1).ToString() ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            model.A08_Task_Type_LogItems = new List<SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_LogItem>();
            var a08_Task_Type_Logs = db.A08_Task_Type_Logs.Where(p => p.TaskTypeID == ID).ToList();
            foreach (var log in a08_Task_Type_Logs.OrderByDescending(p => p.DateCreated))
            {
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == log.UserID).SingleOrDefault();
                model.A08_Task_Type_LogItems.Add(new SiteAdmin_A08_Task_TypesEditModel.A08_Task_Type_LogItem()
                {
                    DateCreated = log.DateCreated,
                    UserID = log.UserID,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    TaskTypeID = log.TaskTypeID,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                });
            }

            if (ModelState.IsValid)
            {

                //var db = new MyVoltageDbContext(_options);

                //var existingCount = db.A09_Flags_Types.Where(p => p.FlagTypeName == model.FlagTypeName).Count();

                //if (existingCount > 0)
                //{
                //    model.IsSuccess = false;
                //    ModelState.EditModelError("FlagTypeName", $"{model.FlagTypeName} - Already exist");
                //    return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit.cshtml", model);
                //}

                StringBuilder sbSysLog = new StringBuilder();


                if (!string.IsNullOrEmpty(Request.Form["TaskClassification"].ToString()) && item.TaskClassificationID != Convert.ToInt32(Request.Form["TaskClassification"]))
                {
                    sbSysLog.AppendLine($"TaskClassification from '{(item.TaskClassificationID.HasValue ? ((TaskClassificationEnum)item.TaskClassificationID).GetDescription() : "None")}' to '{((TaskClassificationEnum)Convert.ToInt32(Request.Form["TaskClassification"])).GetDescription()}'<br />");
                    item.TaskClassificationID = Convert.ToInt32(Request.Form["TaskClassification"]);
                }

                if (!string.IsNullOrEmpty(model.Description) && item.Description != model.Description)
                {
                    sbSysLog.AppendLine($"Description from '{item.Description}' to '{model.Description}'<br />");
                    item.Description = model.Description;
                }

                if (!string.IsNullOrEmpty(model.Identifier) && item.Identifier != model.Identifier)
                {
                    sbSysLog.AppendLine($"Identifier from '{item.Identifier}' to '{model.Identifier}'<br />");
                    item.Identifier = model.Identifier;
                }

                if (!string.IsNullOrEmpty(model.Heading) && item.Heading != model.Heading)
                {
                    sbSysLog.AppendLine($"Heading from '{item.Heading}' to '{model.Heading}'<br />");
                    item.Heading = model.Heading;
                }

                if (!string.IsNullOrEmpty(Request.Form["Priority"].ToString()) && item.PriorityID != Convert.ToInt32(Request.Form["Priority"]))
                {
                    var priorityBefore = siteAdmin_Priorities.Where(p => p.ID == item.PriorityID).SingleOrDefault();
                    var priorityAfter = siteAdmin_Priorities.Where(p => p.ID == Convert.ToInt32(Request.Form["Priority"])).SingleOrDefault();
                    sbSysLog.AppendLine($"Priority from '{priorityBefore.PriorityName}' to '{priorityAfter.PriorityName}'<br />");
                    item.PriorityID = Convert.ToInt32(Request.Form["Priority"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["ReportingToUser"].ToString()) && item.ReportingToUserID != Request.Form["ReportingToUser"].ToString())
                {
                    var opProfReportingToUserBefore = dbCache.OperationalProfiles.Where(p => p.UserID == item.ReportingToUserID).SingleOrDefault();
                    var opProfReportingToUserAfter = dbCache.OperationalProfiles.Where(p => p.UserID == Request.Form["ReportingToUser"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ReportingToUser from '{opProfReportingToUserBefore.FirstName} {opProfReportingToUserBefore.LastName}' to '{opProfReportingToUserAfter.FirstName} {opProfReportingToUserAfter.LastName}'<br />");
                    item.ReportingToUserID = Request.Form["ReportingToUser"];
                }

                if (!string.IsNullOrEmpty(Request.Form["ResponsibleUser"].ToString()) && item.ResponsibleUserID != Request.Form["ResponsibleUser"].ToString())
                {
                    var opProfResponsibleUserBefore = dbCache.OperationalProfiles.Where(p => p.UserID == item.ResponsibleUserID).SingleOrDefault();
                    var opProfResponsibleUserAfter = dbCache.OperationalProfiles.Where(p => p.UserID == Request.Form["ResponsibleUser"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ResponsibleUser from '{opProfResponsibleUserBefore.FirstName} {opProfResponsibleUserBefore.LastName}' to '{opProfResponsibleUserAfter.FirstName} {opProfResponsibleUserAfter.LastName}'<br />");
                    item.ResponsibleUserID = Request.Form["ResponsibleUser"];

                }

                if (item.TemplateNo != model.TemplateNo)
                {
                    sbSysLog.AppendLine($"TemplateNo from '{item.TemplateNo}' to '{model.TemplateNo}'<br />");
                    item.TemplateNo = model.TemplateNo;
                }

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"].ToString()) && item.LinkedSecureAreaID != Convert.ToInt32(Request.Form["LinkedSecureArea"]))
                {
                    if (!item.LinkedSecureAreaID.HasValue)
                    {
                        sbSysLog.AppendLine($"LinkedSecureArea added '{((MyVoltage.Data.SecureAreaEnum)Convert.ToInt32(Request.Form["LinkedSecureArea"])).GetDescription()}'<br />");
                    }
                    else
                    {
                        sbSysLog.AppendLine($"LinkedSecureArea from '{((MyVoltage.Data.SecureAreaEnum)item.LinkedSecureAreaID.Value).GetDescription()}' to '{((MyVoltage.Data.SecureAreaEnum)Convert.ToInt32(Request.Form["LinkedSecureArea"])).GetDescription()}'<br />");
                    }
                    item.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);
                }
                else if (item.LinkedSecureAreaID.HasValue && string.IsNullOrEmpty(Request.Form["LinkedSecureArea"].ToString()))
                {
                    sbSysLog.AppendLine($"LinkedSecureArea removed.<br />");
                    item.LinkedSecureAreaID = null;
                }

                if (!string.IsNullOrEmpty(Request.Form["CreateIndividualFlagsForCompaniesLinked"].ToString()) && item.CreateIndividualFlagsForCompaniesLinked != Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]))
                {
                    sbSysLog.AppendLine($"CreateIndividualFlagsForCompaniesLinked from '{item.CreateIndividualFlagsForCompaniesLinked}' to '{Request.Form["CreateIndividualFlagsForCompaniesLinked"].ToString()}'<br />");
                    item.CreateIndividualFlagsForCompaniesLinked = Convert.ToBoolean(Request.Form["CreateIndividualFlagsForCompaniesLinked"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["UpdateExistingTask"].ToString()) && item.UpdateExistingTask != Convert.ToBoolean(Request.Form["UpdateExistingTask"]))
                {
                    sbSysLog.AppendLine($"UpdateExistingTask from '{item.UpdateExistingTask}' to '{Request.Form["UpdateExistingTask"].ToString()}'<br />");
                    item.UpdateExistingTask = Convert.ToBoolean(Request.Form["UpdateExistingTask"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["HasComplianceCheck"].ToString()) && item.HasComplianceCheck != Convert.ToBoolean(Request.Form["HasComplianceCheck"]))
                {
                    sbSysLog.AppendLine($"HasComplianceCheck from '{item.HasComplianceCheck}' to '{Request.Form["HasComplianceCheck"].ToString()}'<br />");
                    item.HasComplianceCheck = Convert.ToBoolean(Request.Form["HasComplianceCheck"]);
                }

                if (item.MinRequiredToClear != model.MinRequiredToClear)
                {
                    sbSysLog.AppendLine($"MinRequiredToClear from '{item.MinRequiredToClear}' to '{model.MinRequiredToClear}'<br />");
                    item.MinRequiredToClear = model.MinRequiredToClear;
                }

                if (!string.IsNullOrEmpty(Request.Form["StatusGroup"].ToString()) && item.StatusGroupID != Convert.ToInt32(Request.Form["StatusGroup"]))
                {
                    var statusGroupBefore = siteAdmin_StatusGroups.Where(p => p.ID == item.StatusGroupID).SingleOrDefault();
                    var statusGroupAfter = siteAdmin_StatusGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["StatusGroup"])).SingleOrDefault();
                    sbSysLog.AppendLine($"StatusGroup from '{statusGroupBefore.StatusGroupName}' to '{statusGroupAfter.StatusGroupName}'<br />");

                    item.StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["DefaultStatus"].ToString()) && item.DefaultStatusID != Convert.ToInt32(Request.Form["DefaultStatus"]))
                {
                    var defaultStatusBefore = siteAdmin_Statuses.Where(p => p.ID == item.DefaultStatusID).SingleOrDefault();
                    var defaultStatusAfter = siteAdmin_Statuses.Where(p => p.ID == Convert.ToInt32(Request.Form["DefaultStatus"])).SingleOrDefault();
                    sbSysLog.AppendLine($"DefaultStatus from '{siteAdmin_StatusActions.Where(p => p.ID == defaultStatusBefore.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == defaultStatusBefore.StatusReportingID).SingleOrDefault().StatusReportingName}' to '{siteAdmin_StatusActions.Where(p => p.ID == defaultStatusAfter.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == defaultStatusAfter.StatusReportingID).SingleOrDefault().StatusReportingName}'<br />");
                    item.DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["MeetingAgendaGroup"].ToString()) && item.MeetingAgendaGroupID != Convert.ToInt32(Request.Form["MeetingAgendaGroup"]))
                {
                    var MeetingAgendaGroupBefore = siteAdmin_MeetingAgendaGroups.Where(p => p.ID == item.MeetingAgendaGroupID).SingleOrDefault();
                    var MeetingAgendaGroupAfter = siteAdmin_MeetingAgendaGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["MeetingAgendaGroup"])).SingleOrDefault();
                    sbSysLog.AppendLine($"MeetingAgendaGroup from '{(MeetingAgendaGroupBefore != null ? MeetingAgendaGroupBefore.MeetingAgendaGroupName : "")}' to '{MeetingAgendaGroupAfter.MeetingAgendaGroupName}'<br />");

                    item.MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroup"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["DefaultMeetingAgenda"].ToString()) && item.DefaultMeetingAgendaID != Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]))
                {
                    var defaultMeetingAgendaBefore = siteAdmin_MeetingAgendaes.Where(p => p.ID == item.DefaultMeetingAgendaID).SingleOrDefault();
                    var defaultMeetingAgendaAfter = siteAdmin_MeetingAgendaes.Where(p => p.ID == Convert.ToInt32(Request.Form["DefaultMeetingAgenda"])).SingleOrDefault();
                    if (defaultMeetingAgendaBefore == null)
                        sbSysLog.AppendLine($"DefaultMeetingAgenda from '' to '{siteAdmin_MeetingAgendaActions.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}'<br />");
                    else
                        sbSysLog.AppendLine($"DefaultMeetingAgenda from '{siteAdmin_MeetingAgendaActions.Where(p => p.ID == defaultMeetingAgendaBefore.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == defaultMeetingAgendaBefore.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}' to '{siteAdmin_MeetingAgendaActions.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}'<br />");
                    item.DefaultMeetingAgendaID = Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["ReportingToUserRequiresCompletedState"].ToString()) && item.ReportingToUserRequiresCompletedState != Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]))
                {
                    sbSysLog.AppendLine($"ReportingToUserRequiresCompletedState from '{item.ReportingToUserRequiresCompletedState}' to '{Request.Form["ReportingToUserRequiresCompletedState"]}'<br />");
                    item.ReportingToUserRequiresCompletedState = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]);
                }


                if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupID"].ToString()) && item.SecureAreaGroupID != Convert.ToInt32(Request.Form["WorkflowGroupID"]))
                {
                    var beforeWF = item.SecureAreaGroupID.HasValue && workflowGroups.Where(p => p.ID == item.SecureAreaGroupID.Value).SingleOrDefault() != null ? workflowGroups.Where(p => p.ID == item.SecureAreaGroupID.Value).SingleOrDefault().WorkflowGroupName : "";
                    var afterWF = workflowGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["WorkflowGroupID"])).SingleOrDefault();

                    sbSysLog.AppendLine($"SecureAreaGroupID from '{beforeWF}' to '{afterWF.WorkflowGroupName}'<br />");
                    item.SecureAreaGroupID = Convert.ToInt32(Request.Form["WorkflowGroupID"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["BusinessDepartment"].ToString()) && item.BusinessDepartmentID != Convert.ToInt32(Request.Form["BusinessDepartment"]))
                {
                    var beforeWF = item.BusinessDepartmentID.HasValue ? businessDepartments.Where(p => p.ID == item.BusinessDepartmentID.Value).SingleOrDefault().BusinessDepartmentName : "";
                    var afterWF = businessDepartments.Where(p => p.ID == Convert.ToInt32(Request.Form["BusinessDepartment"])).SingleOrDefault();

                    sbSysLog.AppendLine($"BusinessDepartmentID from '{beforeWF}' to '{afterWF.BusinessDepartmentName}'<br />");
                    item.BusinessDepartmentID = Convert.ToInt32(Request.Form["BusinessDepartment"]);
                }

                if (item.DefautlMinPlanned != model.DefautlMinPlanned)
                {
                    sbSysLog.AppendLine($"DefaultMinPlanned from '{item.DefautlMinPlanned}' to '{model.DefautlMinPlanned}'<br />");
                    item.DefautlMinPlanned = model.DefautlMinPlanned;
                }

                if (model.HowToDocument != null)
                {
                    sbSysLog.AppendLine($"How to document uploaded.<br />");

                    // Name of the share, directory, and file we'll create
                    string shareName = "a08-tasktypes";
                    string dirName = $"{item.ID}";
                    string fileName = $"{item.ID}" + System.IO.Path.GetExtension(model.HowToDocument.FileName);

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);
                    if (file.Exists())
                        file.Delete();

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.HowToDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);

                    item.HowToURL = $"{fileName}";
                }

                model.A08_Task_Type = item;
                model.ResultFlagTypeID = item.ID.ToString();

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    #region Reset Acceptance 

                    if (item.ReportingToUserAccepted.HasValue)
                    {
                        sbSysLog.AppendLine($"Reporting To User Acceptance removed.<br />");
                        item.ReportingToUserAccepted = null;
                        item.ReportingToUserAcceptedDate = null;
                    }

                    if (item.ResponsibleUserAccepted.HasValue)
                    {
                        sbSysLog.AppendLine($"Responsible User Acceptance removed.<br />");
                        item.ResponsibleUserAccepted = null;
                        item.ResponsibleUserAcceptedDate = null;
                    }

                    #endregion

                    if (_userManager.FindByIdAsync(item.ResponsibleUserID).Result.Email.ToUpper().Contains("EVERYONE@MYVOLTAGE.CO.ZA"))
                    {
                        item.ResponsibleUserAccepted = true;
                        item.ResponsibleUserAcceptedDate = DateTime.Now;

                        var opResponsibleUser = dbCache.OperationalProfiles.Where(p => p.UserID == item.ResponsibleUserID).SingleOrDefault();
                        sbSysLog.AppendLine($"{opResponsibleUser.FirstName} {opResponsibleUser.LastName} Responsible User Auto Accept");
                    }

                    db.Update(item);
                    db.SaveChanges();

                    Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        TaskTypeID = item.ID,
                    };

                    db.Add(log);
                    db.SaveChanges();
                }

                model.IsSuccess = true;

                _cache.Remove(MVCache.KEY_A08_Tasks_Types);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/AddCompany/{taskTypeID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_AddCompany(int taskTypeID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskTypeCompany = db.A08_Task_Type_Companies.Where(p => p.TaskTypeID == taskTypeID && p.CompanyID == companyID).SingleOrDefault();

            if (taskTypeCompany == null)
            {
                //var item = db.A08_Task_Types.Where(p => p.ID == taskTypeID).SingleOrDefault();

                taskTypeCompany = new A08_Task_Type_Company()
                {
                    TaskTypeID = taskTypeID,
                    CompanyID = companyID,
                };
                db.Add(taskTypeCompany);
                db.SaveChanges();

                #region Reset Acceptance 

                //if (item.ReportingToUserAccepted.HasValue)
                //{
                //    sbSysLog.AppendLine($"Reporting To User Acceptance removed.<br />");
                //    item.ReportingToUserAccepted = null;
                //    item.ReportingToUserAcceptedDate = null;
                //}

                //if (item.ResponsibleUserAccepted.HasValue)
                //{
                //    sbSysLog.AppendLine($"Responsible User Acceptance removed.<br />");
                //    item.ResponsibleUserAccepted = null;
                //    item.ResponsibleUserAcceptedDate = null;
                //}

                #endregion


                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{company.Name} added",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = taskTypeID,
                };

                db.Add(log);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit/{taskTypeID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/DeleteCompany/{taskTypeID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_DeleteCompany(int taskTypeID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskTypeCompanys = db.A08_Task_Type_Companies.Where(p => p.TaskTypeID == taskTypeID && p.CompanyID == companyID).ToList();

            foreach (var taskTypeCompany in taskTypeCompanys)
            {
                db.Remove(taskTypeCompany);
                db.SaveChanges();

                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{company.Name} removed",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = taskTypeID,
                };

                db.Add(log);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit/{taskTypeID}");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/AddFrequency/{taskTypeID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_AddFrequency(int taskTypeID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (
                !string.IsNullOrEmpty(Request.Form["addFrequencyDropDown"].ToString())
                )
            {
                try
                {
                    int addFrequencyDropDown = Convert.ToInt32(Request.Form["addFrequencyDropDown"]);
                    if (addFrequencyDropDown == (int)MyVoltage.Data.A08_Task_Type_Frequency.FrequencyEnum.OnceOff)
                    {
                        if (!string.IsNullOrEmpty(Request.Form["addFrequencyDate"].ToString()))
                        {
                            DateTime addFrequencyDate = Convert.ToDateTime(Request.Form["addFrequencyDate"]);
                            Data.A08_Task_Type_Frequency a08_Task_Type_Frequency = new A08_Task_Type_Frequency()
                            {
                                FrequencyID = addFrequencyDropDown,
                                OneTimeExecuteDate = addFrequencyDate,
                                TaskTypeID = taskTypeID,
                            };
                            db.Add(a08_Task_Type_Frequency);
                            db.SaveChanges();

                            Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                            {
                                DateCreated = DateTime.Now,
                                SystemDescription = $"Frequency added - {MyVoltage.Data.A08_Task_Type_Frequency.FrequencyEnum.OnceOff.GetDescription()} @ {addFrequencyDate.ToDateAndTimeShort()}",
                                UserID = _userManager.GetUserId(User),
                                TaskTypeID = taskTypeID,
                            };

                            db.Add(log);
                            db.SaveChanges();

                            return Content("true");
                        }
                    }
                    else
                    {
                        Data.A08_Task_Type_Frequency a08_Task_Type_Frequency = new A08_Task_Type_Frequency()
                        {
                            FrequencyID = addFrequencyDropDown,
                            OneTimeExecuteDate = null,
                            TaskTypeID = taskTypeID,
                        };
                        db.Add(a08_Task_Type_Frequency);
                        db.SaveChanges();

                        Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                        {
                            DateCreated = DateTime.Now,
                            SystemDescription = $"Frequency added - {((MyVoltage.Data.A08_Task_Type_Frequency.FrequencyEnum)addFrequencyDropDown).GetDescription()}",
                            UserID = _userManager.GetUserId(User),
                            TaskTypeID = taskTypeID,
                        };

                        db.Add(log);
                        db.SaveChanges();

                        return Content("true");
                    }

                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/DeleteFrequency/{taskTypeID}/{taskTypeFrequencyID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_DeleteFrequency(int taskTypeID, int taskTypeFrequencyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.UserAdmin, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.UserAdmin}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var taskTypeFrequency = db.A08_Task_Type_Frequencies.Where(p => p.TaskTypeID == taskTypeID && p.ID == taskTypeFrequencyID).SingleOrDefault();

            if (taskTypeFrequency != null)
            {
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"Frequency removed - {taskTypeFrequency.Frequency.GetDescription()}",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = taskTypeID,
                };

                db.Add(log);
                db.SaveChanges();

                db.Remove(taskTypeFrequency);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Edit/{taskTypeID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A08TaskTypes/Delete/{taskTypeID}")]
        public async Task<IActionResult> SiteAdmin_A08TaskTypes_Delete(int taskTypeID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A08TaskTypes, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A08TaskTypes}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var task_Type = db.A08_Task_Types.Where(p => p.ID == taskTypeID).SingleOrDefault();

            if (task_Type != null)
            {
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"Task Deleted",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = taskTypeID,
                };

                db.Add(log);
                db.SaveChanges();

                task_Type.IsDeleted = true;
                db.Update(task_Type);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_A08TaskTypes");
        }


    }
}
