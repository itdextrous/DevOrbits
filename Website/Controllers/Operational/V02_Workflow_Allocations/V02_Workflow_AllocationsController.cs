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
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.V02_Workflow_Allocations
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class V02_Workflow_AllocationsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public V02_Workflow_AllocationsController(
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
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
            _client = new MyVoltage.Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_AllTaskAllocations")]
        public async Task<IActionResult> V02_Workflow_Allocations_AllTaskAllocations()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_AllTaskAllocations, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V02_Workflow_Allocations_AllTaskAllocations}/{(int)SecureAreaActionEnum.View}");

            #endregion


            V02_Workflow_Allocations_AllTaskAllocationsModel model = new V02_Workflow_Allocations_AllTaskAllocationsModel()
            {
                V02_Workflow_Allocations_AllTaskAllocationsItems = new List<V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem>(),
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
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
            };
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var a08_Task_Types = db.A08_Task_Types.Where(p => !p.IsDeleted.HasValue || !p.IsDeleted.Value).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var SiteAdmin_Priorities = db.SiteAdmin_Priorities.ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();
            model.Priority.AddRange((from p in SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"] == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"] == user.Id ? true : false });
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

            foreach (var tType in a08_Task_Types)
            {
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

                V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem item = new V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem()
                {
                    ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                    CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                    DashboardURL = tType.DashboardURL,
                    DefaultStatusID = tType.DefaultStatusID,
                    Description = tType.Description,
                    Heading = tType.Heading,
                    HowToURL = tType.HowToURL,
                    ID = tType.ID,
                    LinkedSecureAreaID = tType.LinkedSecureAreaID,
                    MinRequiredToClear = tType.MinRequiredToClear,
                    PriorityID = tType.PriorityID,
                    ReportingToUserAccepted = tType.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                    ReportingToUserID = tType.ReportingToUserID,
                    ReportingToUsername = "",
                    ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                    ResponsibleUserID = tType.ResponsibleUserID,
                    ResponsibleUsername = "",
                    SecureAreaName = tType.LinkedSecureArea.HasValue ? tType.LinkedSecureArea.Value.GetDescription() : "",
                    StatusGroupID = tType.StatusGroupID,
                    TemplateNo = tType.TemplateNo,
                    SecureAreaGroupID = tType.SecureAreaGroupID,
                    DefautlMinPlanned = tType.DefautlMinPlanned,
                    ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                    BusinessDepartmentID = tType.BusinessDepartmentID,
                    Identifier = tType.Identifier,
                    IsDeleted = tType.IsDeleted,
                    TaskClassificationID = tType.TaskClassificationID,
                };

                var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
                if (opReportingTo != null)
                    item.ReportingToUsername = $"{opReportingTo.FirstName} {opReportingTo.LastName}";

                var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
                if (opResponsibleUser != null)
                    item.ResponsibleUsername = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName}";


                #region Workflows

                int workflowID = 1;
                if (tType.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == tType.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (tType.SecureAreaGroupID.HasValue)
                    workflowID = tType.SecureAreaGroupID.Value;

                if (tType.BusinessDepartmentID.HasValue)
                {
                    var department = businessDepartments.Where(p => p.ID == tType.BusinessDepartmentID.Value).SingleOrDefault();
                    item.BusinessDepartmentName = department.BusinessDepartmentName;
                    var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    item.BusinessPillarName = pillar.BusinessPillarName;
                }

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    item.WorkflowGroupName = wf.WorkflowGroupName;
                    if (wf.BusinessDepartmentID.HasValue && !tType.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        item.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        item.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                #endregion

                model.V02_Workflow_Allocations_AllTaskAllocationsItems.Add(item);
            }

            model.V02_Workflow_Allocations_AllTaskAllocationsItems = model.V02_Workflow_Allocations_AllTaskAllocationsItems.OrderBy(p => p.Identifier).ThenBy(p => p.Heading).ThenBy(p => p.SecureAreaGroup.GetDescription()).ToList();
            return View("~/Views/Operational/V02_Workflow_Allocations/V02_Workflow_Allocations_AllTaskAllocations.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_MyTaskAllocations")]
        public async Task<IActionResult> V02_Workflow_Allocations_MyTaskAllocations()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_MyTaskAllocations, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V02_Workflow_Allocations_MyTaskAllocations}/{(int)SecureAreaActionEnum.View}");

            #endregion


            V02_Workflow_Allocations_AllTaskAllocationsModel model = new V02_Workflow_Allocations_AllTaskAllocationsModel()
            {
                V02_Workflow_Allocations_AllTaskAllocationsItems = new List<V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem>(),
                SecureAreaGroupID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Workflow Groups]", Selected = string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]) }
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
            };
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var a08_Task_Types = db.A08_Task_Types.Where(p => (p.ResponsibleUserID == _userManager.GetUserId(User) || p.ReportingToUserID == _userManager.GetUserId(User)) && (!p.IsDeleted.HasValue || !p.IsDeleted.Value)).ToList();

            var SiteAdmin_Priorities = db.SiteAdmin_Priorities.ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();
            model.Priority.AddRange((from p in SiteAdmin_Priorities
                                     select new SelectListItem()
                                     {
                                         Text = p.PriorityName,
                                         Value = p.ID.ToString(),
                                         Selected = Request.Query["Priority"] == p.ID.ToString() ? true : false,
                                     }).ToList());
            model.Priority = model.Priority.OrderBy(p => p.Text).ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ResponsibleUser"] == user.Id ? true : false });
                model.ReportingToUser.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Query["ReportingToUser"] == user.Id ? true : false });
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

            foreach (var tType in a08_Task_Types)
            {
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
                V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem item = new V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem()
                {
                    ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                    CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                    DashboardURL = tType.DashboardURL,
                    DefaultStatusID = tType.DefaultStatusID,
                    Description = tType.Description,
                    Heading = tType.Heading,
                    HowToURL = tType.HowToURL,
                    ID = tType.ID,
                    LinkedSecureAreaID = tType.LinkedSecureAreaID,
                    MinRequiredToClear = tType.MinRequiredToClear,
                    PriorityID = tType.PriorityID,
                    ReportingToUserAccepted = tType.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                    ReportingToUserID = tType.ReportingToUserID,
                    ReportingToUsername = "",
                    ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                    ResponsibleUserID = tType.ResponsibleUserID,
                    ResponsibleUsername = "",
                    SecureAreaName = tType.LinkedSecureArea.HasValue ? tType.LinkedSecureArea.Value.GetDescription() : "",
                    StatusGroupID = tType.StatusGroupID,
                    TemplateNo = tType.TemplateNo,
                    SecureAreaGroupID = tType.SecureAreaGroupID,
                    DefautlMinPlanned = tType.DefautlMinPlanned,
                    ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                    BusinessDepartmentID = tType.BusinessDepartmentID,
                    Identifier = tType.Identifier,
                    IsDeleted = tType.IsDeleted,
                    TaskClassificationID = tType.TaskClassificationID,
                };

                var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
                if (opReportingTo != null)
                    item.ReportingToUsername = $"{opReportingTo.FirstName} {opReportingTo.LastName}";

                var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
                if (opResponsibleUser != null)
                    item.ResponsibleUsername = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName}";


                if (tType.LinkedSecureAreaID.HasValue)
                {
                    foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
                    {
                        foreach (var sa in psa.Value)
                        {
                            if ((int)sa.Item1 == tType.LinkedSecureAreaID.Value)
                            {
                                item.SecureAreaName = $"{psa.Key.Item3} - {sa.Item3}";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    item.SecureAreaName = "00 - NONE";
                }

                #region Workflows

                int workflowID = 1;
                if (tType.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == tType.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (tType.SecureAreaGroupID.HasValue)
                    workflowID = tType.SecureAreaGroupID.Value;

                if (tType.BusinessDepartmentID.HasValue)
                {
                    var department = businessDepartments.Where(p => p.ID == tType.BusinessDepartmentID.Value).SingleOrDefault();
                    item.BusinessDepartmentName = department.BusinessDepartmentName;
                    var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    item.BusinessPillarName = pillar.BusinessPillarName;
                }

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    item.WorkflowGroupName = wf.WorkflowGroupName;
                    if (wf.BusinessDepartmentID.HasValue && !tType.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        item.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        item.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                #endregion

                model.V02_Workflow_Allocations_AllTaskAllocationsItems.Add(item);
            }

            model.V02_Workflow_Allocations_AllTaskAllocationsItems = model.V02_Workflow_Allocations_AllTaskAllocationsItems.OrderBy(p => p.Identifier).ThenBy(p => p.Heading).ThenBy(p => p.SecureAreaGroup.GetDescription()).ToList();
            return View("~/Views/Operational/V02_Workflow_Allocations/V02_Workflow_Allocations_MyTaskAllocations.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations")]
        public async Task<IActionResult> V02_Workflow_Allocations_ViewTaskAllocations()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var tType = db.A08_Task_Types.Where(p => p.ID == _operationalProvider.TaskSelectedTaskTypeID).SingleOrDefault();
            var opProfs = db.OperationalProfiles.ToList();

            if (_operationalProvider.TaskSelectedTaskTypeID == 0 || tType == null)
                return Redirect("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_MyTaskAllocations");
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();

            V02_Workflow_Allocations_ViewTaskAllocationsModel model = new V02_Workflow_Allocations_ViewTaskAllocationsModel()
            {
                A08_Task_Type = new V02_Workflow_Allocations_ViewTaskAllocationsModel.V02_Workflow_AllocationsItem()
                {
                    ResponsibleUserAccepted = tType.ResponsibleUserAccepted,
                    CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                    DashboardURL = tType.DashboardURL,
                    DefaultStatusID = tType.DefaultStatusID,
                    Description = tType.Description,
                    Heading = tType.Heading,
                    HowToURL = tType.HowToURL,
                    ID = tType.ID,
                    LinkedSecureAreaID = tType.LinkedSecureAreaID,
                    MinRequiredToClear = tType.MinRequiredToClear,
                    PriorityID = tType.PriorityID,
                    ReportingToUserAccepted = tType.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = tType.ReportingToUserAcceptedDate,
                    ReportingToUserID = tType.ReportingToUserID,
                    ReportingToUsername = "",
                    ResponsibleUserAcceptedDate = tType.ResponsibleUserAcceptedDate,
                    ResponsibleUserID = tType.ResponsibleUserID,
                    ResponsibleUsername = "",
                    SecureAreaName = tType.LinkedSecureArea.HasValue ? tType.LinkedSecureArea.Value.GetDescription() : "",
                    StatusGroupID = tType.StatusGroupID,
                    TemplateNo = tType.TemplateNo,
                    DefautlMinPlanned = tType.DefautlMinPlanned,
                    ReportingToUserRequiresCompletedState = tType.ReportingToUserRequiresCompletedState,
                    BusinessDepartmentID = tType.BusinessDepartmentID,
                    Identifier = tType.Identifier,
                    IsDeleted = tType.IsDeleted,
                    TaskClassificationID = tType.TaskClassificationID,
                    BusinessDepartmentName = "",
                    BusinessPillarName = "",
                    SecureAreaGroupID = tType.SecureAreaGroupID,
                    WorkflowGroupName = "",
                },
            };


            var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
            if (opReportingTo != null)
                model.A08_Task_Type.ReportingToUsername = $"{opReportingTo.FirstName} {opReportingTo.LastName}";

            var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
            if (opResponsibleUser != null)
                model.A08_Task_Type.ResponsibleUsername = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName}";


                #region Workflows

                int workflowID = 1;
                if (tType.LinkedSecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == tType.LinkedSecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (tType.SecureAreaGroupID.HasValue)
                    workflowID = tType.SecureAreaGroupID.Value;

                if (tType.BusinessDepartmentID.HasValue)
                {
                    var department = businessDepartments.Where(p => p.ID == tType.BusinessDepartmentID.Value).SingleOrDefault();
                    model.A08_Task_Type.BusinessDepartmentName = department.BusinessDepartmentName;
                    var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    model.A08_Task_Type.BusinessPillarName = pillar.BusinessPillarName;
                }

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    model.A08_Task_Type.WorkflowGroupName = wf.WorkflowGroupName;
                    if (wf.BusinessDepartmentID.HasValue && !tType.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        model.A08_Task_Type.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        model.A08_Task_Type.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                #endregion


            return View("~/Views/Operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ResponsibleUserAccept")]
        public async Task<IActionResult> V02_Workflow_Allocations_ResponsibleUserAccept()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var tType = db.A08_Task_Types.Where(p => p.ID == _operationalProvider.TaskSelectedTaskTypeID).SingleOrDefault();

            if (tType == null)
                return Redirect("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations");


            if (tType.ResponsibleUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                tType.ResponsibleUserAccepted = true;
                tType.ResponsibleUserAcceptedDate = DateTime.Now;
                db.Update(tType);
                db.SaveChanges();

                var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName} Responsible User Accept",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = tType.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations");
        }

        [HttpGet]
        [Route("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ReportingToUserAccept")]
        public async Task<IActionResult> V02_Workflow_Allocations_ReportingToUserAccept()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var tType = db.A08_Task_Types.Where(p => p.ID == _operationalProvider.TaskSelectedTaskTypeID).SingleOrDefault();

            if (tType == null)
                return Redirect("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations");


            if (tType.ReportingToUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                tType.ReportingToUserAccepted = true;
                tType.ReportingToUserAcceptedDate = DateTime.Now;
                db.Update(tType);
                db.SaveChanges();

                var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
                Data.A08_Task_Type_Log log = new A08_Task_Type_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opReportingTo.FirstName} {opReportingTo.LastName} Reporting To User Accept",
                    UserID = _userManager.GetUserId(User),
                    TaskTypeID = tType.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }


            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/V02_Workflow_Allocations/V02_Workflow_Allocations_ViewTaskAllocations");
        }
    }
}
