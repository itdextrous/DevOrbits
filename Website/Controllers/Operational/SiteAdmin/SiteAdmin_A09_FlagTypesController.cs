using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_A09_FlagTypesModels;
using DocumentFormat.OpenXml.Office.CustomUI;
using System.IO;
using MyVoltageApi.Data;
using System.Text;
using Azure.Storage.Files.Shares;
using Azure;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class SiteAdmin_A09_FlagTypesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_A09_FlagTypesController(IMemoryCache cache,
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();
            var workflowGroups = db.WorkflowGroups.ToList();
            var businessPillars = db.BusinessPillars.ToList();
            var businessDepartments = db.BusinessDepartments.ToList();
            var secureAreas = db.SecureAreas.ToList();

            SiteAdmin_A09_FlagTypesModel model = new SiteAdmin_A09_FlagTypesModel()
            {
                SiteAdmin_A09_FlagTypesItems = new List<SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem>(),
            };

            foreach (var flags_Type in db.A09_Flags_Types.ToList())
            {
                SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem item = new SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem()
                {
                    Active = flags_Type.Active,
                    FlagTypeName = flags_Type.FlagTypeName,
                    ID = flags_Type.ID,
                    //A09_Flags_ResponsiblePeople = new List<SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem.A09_Flags_ResponsiblePersonItem>(),
                    DefaultAssignedToID = flags_Type.DefaultAssignedToID,
                    DefaultUsername = "",
                    SecureAreaID = flags_Type.SecureAreaID,
                    PriorityID = flags_Type.PriorityID,
                    DefaultStatusID = flags_Type.DefaultStatusID,
                    SiteAdmin_Status = "[NONE]",
                    SiteAdmin_StatusGroup = flags_Type.StatusGroupID.HasValue ? siteAdmin_StatusGroups.Where(p => p.ID == flags_Type.StatusGroupID.Value).SingleOrDefault() : null,
                    StatusGroupID = flags_Type.StatusGroupID,
                    ReportingToUserID = flags_Type.ReportingToUserID,
                    ReportingToUserUsername = "",
                    WorkflowGroupName = "",
                    HasComplianceCheck = flags_Type.HasComplianceCheck,
                    BusinessPillarName = "",
                    BusinessDepartmentName = "",
                    DefaultMeetingAgendaID = flags_Type.DefaultStatusID,
                    DefautlMinPlanned = flags_Type.DefautlMinPlanned,
                    Identifier = flags_Type.Identifier,
                    MeetingAgendaGroupID = flags_Type.MeetingAgendaGroupID,
                    PolicyDocumentURL = flags_Type.PolicyDocumentURL,
                    ReportingToUserRequiresCompletedState = flags_Type.ReportingToUserRequiresCompletedState,
                    SecureAreaGroupID = flags_Type.SecureAreaGroupID,
                };

                if (flags_Type.DefaultStatusID.HasValue)
                {
                    var status = siteAdmin_Statuses.Where(p => p.ID == flags_Type.DefaultStatusID.Value).SingleOrDefault();
                    item.SiteAdmin_Status = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}";
                }

                var opProf = opProfs.Where(p => p.UserID == flags_Type.DefaultAssignedToID).SingleOrDefault();
                if (opProf != null)
                    item.DefaultUsername = $"{opProf.FirstName} {opProf.LastName}";

                var opProfRepTo = opProfs.Where(p => p.UserID == flags_Type.ReportingToUserID).SingleOrDefault();
                if (opProfRepTo != null)
                    item.ReportingToUserUsername = $"{opProfRepTo.FirstName} {opProfRepTo.LastName}";

                if (flags_Type.PriorityID.HasValue)
                    item.SiteAdmin_Priority = priorities.Where(p => p.ID == flags_Type.PriorityID.Value).SingleOrDefault();

                int workflowID = 1;
                if (flags_Type.SecureAreaID.HasValue)
                {
                    var sc = secureAreas.Where(p => p.SecureAreaID == flags_Type.SecureAreaID.Value).SingleOrDefault();
                    if (sc != null && sc.GroupID.HasValue)
                        workflowID = sc.GroupID.Value;
                }
                if (flags_Type.SecureAreaGroupID.HasValue)
                    workflowID = flags_Type.SecureAreaGroupID.Value;

                var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
                if (wf != null)
                {
                    item.WorkflowGroupName = wf.WorkflowGroupName;
                    if (wf.BusinessDepartmentID.HasValue)
                    {
                        var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
                        item.BusinessDepartmentName = department.BusinessDepartmentName;
                        var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                        item.BusinessPillarName = pillar.BusinessPillarName;
                    }
                }

                //foreach (var flags_ResponsiblePerson in db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == flags_Type.ID).ToList())
                //{
                //    var user = _userManager.FindByIdAsync(flags_ResponsiblePerson.UserID).Result;

                //    if (user != null)
                //    {
                //        SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem.A09_Flags_ResponsiblePersonItem itemPerson = new SiteAdmin_A09_FlagTypesModel.SiteAdmin_A09_FlagTypesItem.A09_Flags_ResponsiblePersonItem()
                //        {
                //            FlagTypeID = flags_ResponsiblePerson.FlagTypeID,
                //            ID = flags_ResponsiblePerson.ID,
                //            UserID = flags_ResponsiblePerson.UserID,
                //            PersonUsername = user.UserName,
                //        };

                //        item.A09_Flags_ResponsiblePeople.Add(itemPerson);
                //    }

                //}

                model.SiteAdmin_A09_FlagTypesItems.Add(item);
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/SiteAdmin_A09_FlagTypes.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Add")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            SiteAdmin_A09_FlagTypesAddModel model = new SiteAdmin_A09_FlagTypesAddModel()
            {
                AvailableUsers = new List<SelectListItem>(),
                Priority = (from p in db.SiteAdmin_Priorities
                            where !p.IsDeleted
                            orderby p.PriorityName
                            select new SelectListItem()
                            {
                                Value = p.ID.ToString(),
                                Text = p.PriorityName,
                            }).ToList(),
                StatusGroup = new List<SelectListItem>(),
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_A09_FlagTypesAddModel.StatusItem>(),
                ReportingToUsers = new List<SelectListItem>(),
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
                SiteAdmin_A09_FlagTypesAddModel.StatusItem statusItem = new SiteAdmin_A09_FlagTypesAddModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var opProfs = db.OperationalProfiles.ToList();
            var userSecureAreaActions = db.UserSecureAreaActions.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (userSecureAreaActions.Where(p => p.UserID == user.Id && p.SecureAreaID == (int)Data.SecureAreaEnum.A09_Flags_CompanyReview && p.SecureAreaActionID == (int)Data.SecureAreaActionEnum.Edit).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.AvailableUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = model.DefaultAssignedToID == user.Id });
                model.ReportingToUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = model.DefaultAssignedToID == user.Id });
            }
            model.AvailableUsers = model.AvailableUsers.OrderBy(p => p.Text).ToList();
            model.ReportingToUsers = model.ReportingToUsers.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Add")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_Add(SiteAdmin_A09_FlagTypesAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            model.AvailableUsers = new List<SelectListItem>();
            model.ReportingToUsers = new List<SelectListItem>();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var opProfs = db.OperationalProfiles.ToList();
            var userSecureAreaActions = db.UserSecureAreaActions.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (userSecureAreaActions.Where(p => p.UserID == user.Id && p.SecureAreaID == (int)Data.SecureAreaEnum.A09_Flags_CompanyReview && p.SecureAreaActionID == (int)Data.SecureAreaActionEnum.Edit).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.AvailableUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["AvailableUsers"] == user.Id });
                model.ReportingToUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUsers"] == user.Id });
            }

            model.Priority = (from p in db.SiteAdmin_Priorities
                              where !p.IsDeleted
                              orderby p.PriorityName
                              select new SelectListItem()
                              {
                                  Value = p.ID.ToString(),
                                  Text = p.PriorityName,
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString(),
                              }).ToList();


            model.StatusGroup = new List<SelectListItem>();
            model.DefaultStatus = new List<SelectListItem>();
            model.StatusItems = new List<SiteAdmin_A09_FlagTypesAddModel.StatusItem>();

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
                SiteAdmin_A09_FlagTypesAddModel.StatusItem statusItem = new SiteAdmin_A09_FlagTypesAddModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            if (ModelState.IsValid)
            {
                var existingCount = db.A09_Flags_Types.Where(p => p.FlagTypeName == model.FlagTypeName).Count();

                if (existingCount > 0)
                {
                    model.IsSuccess = false;
                    ModelState.AddModelError("FlagTypeName", $"{model.FlagTypeName} - Already exist");
                    return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Add.cshtml", model);
                }

                int? linkedToSecureAreaID = null;

                if (!string.IsNullOrEmpty(Request.Form["addLinkedSecureAreaID"]))
                    linkedToSecureAreaID = Convert.ToInt32(Request.Form["addLinkedSecureAreaID"]);

                Data.A09_Flags.A09_Flags_Type flags_Type = new Data.A09_Flags.A09_Flags_Type()
                {
                    Active = true,
                    FlagTypeName = model.FlagTypeName,
                    DefaultAssignedToID = Request.Form["AvailableUsers"],
                    PriorityID = Convert.ToInt32(Request.Form["Priority"]),
                    SecureAreaID = linkedToSecureAreaID,
                    StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]),
                    DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]),
                    ReportingToUserID = Request.Form["ReportingToUsers"],
                };

                db.Add(flags_Type);
                db.SaveChanges();

                //#region FTPUpload

                //string un = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_UN"];
                //string pwd = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_Password"];

                //string fileName = flags_Type.ID.ToString() + System.IO.Path.GetExtension(model.PolicyDocument.FileName);
                //// Copy the contents of the file to the request stream.
                //Stream uploadFile = new MemoryStream();
                //model.PolicyDocument.CopyTo(uploadFile);
                //byte[] fileContents = new byte[uploadFile.Length];
                //uploadFile.Position = 0;
                //uploadFile.Read(fileContents, 0, fileContents.Length);

                //FTPProvider.UploadFile("", fileName, fileContents, un, pwd);

                //#endregion


                //flags_Type.PolicyDocumentURL = fileName;
                db.Update(flags_Type);
                db.SaveChanges();


                model.ResultFlagTypeID = flags_Type.ID.ToString();
                model.IsSuccess = true;
            }
            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var flagType = db.A09_Flags_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (flagType == null)
                return Redirect("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes");


            var workflowGroups = db.WorkflowGroups.ToList();
            SiteAdmin_A09_FlagTypesEditModel model = new SiteAdmin_A09_FlagTypesEditModel()
            {
                //A09_Flags_ResponsiblePeople = new List<SiteAdmin_A09_FlagTypesEditModel.A09_Flags_ResponsiblePersonItem>(),
                FlagTypeID = ID,
                AvailableUsers = new List<SelectListItem>(),
                FlagTypeName = flagType.FlagTypeName,
                PolicyDocumentURL = flagType.PolicyDocumentURL,
                DefaultAssignedToID = flagType.DefaultAssignedToID,
                LinkedToSecureAreaID = flagType.SecureAreaID.HasValue ? flagType.SecureAreaID.Value.ToString() : "",
                A09_Flags_Types_SerialsToExcludes = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == ID).ToList(),
                SkybillCustomers = db.SkybillCustomers.ToList(),
                Priority = (from p in db.SiteAdmin_Priorities
                            where !p.IsDeleted
                            orderby p.PriorityName
                            select new SelectListItem()
                            {
                                Value = p.ID.ToString(),
                                Text = p.PriorityName,
                                Selected = flagType.PriorityID.HasValue && flagType.PriorityID.Value == p.ID,
                            }).ToList(),
                StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !flagType.StatusGroupID.HasValue },
                },
                DefaultStatus = new List<SelectListItem>(),
                StatusItems = new List<SiteAdmin_A09_FlagTypesEditModel.StatusItem>(),
                DefaultStatusID = flagType.DefaultStatusID,
                MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = !flagType.MeetingAgendaGroupID.HasValue },
                },
                DefaultMeetingAgenda = new List<SelectListItem>(),
                MeetingAgendaItems = new List<SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem>(),
                DefaultMeetingAgendaID = flagType.DefaultMeetingAgendaID,
                ReportingToUsers = new List<SelectListItem>(),
                ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = flagType.ReportingToUserRequiresCompletedState.HasValue && flagType.ReportingToUserRequiresCompletedState.Value ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = !flagType.ReportingToUserRequiresCompletedState.HasValue || !flagType.ReportingToUserRequiresCompletedState.Value ? true : false },
                },
                DefautlMinPlanned = flagType.DefautlMinPlanned,
                WorkflowGroupItems = new List<SiteAdmin_A09_FlagTypesEditModel.WorkflowGroupItem>(),
                BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessDepartmentItems = new List<SiteAdmin_A09_FlagTypesEditModel.BusinessDepartmentItem>(),
                BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                },
                BusinessPillarItems = new List<SiteAdmin_A09_FlagTypesEditModel.BusinessPillarItem>(),
                WorkflowGroupID = new List<SelectListItem>(),
                A09_Flag_Type_LogItems = new List<SiteAdmin_A09_FlagTypesEditModel.A09_Flag_Type_LogItem>(),
                HasComplianceCheck = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes", Selected = flagType.HasComplianceCheck.HasValue && flagType.HasComplianceCheck.Value },
                    new SelectListItem() { Value = "false", Text = "No", Selected = !flagType.HasComplianceCheck.HasValue || !flagType.HasComplianceCheck.Value },
                },
                Identifier = flagType.Identifier,
            };

            #region Workflows

            int workflowID = 1;
            if (flagType.SecureAreaID.HasValue)
            {
                var sc = db.SecureAreas.Where(p => p.SecureAreaID == flagType.SecureAreaID.Value).SingleOrDefault();
                if (sc != null && sc.GroupID.HasValue)
                    workflowID = sc.GroupID.Value;
            }

            foreach (var wf in workflowGroups)
            {
                model.WorkflowGroupID.Add(new SelectListItem()
                {
                    Text = wf.WorkflowGroupName,
                    Value = wf.ID.ToString(),
                    Selected = flagType.SecureAreaGroupID.HasValue ? (flagType.SecureAreaGroupID.Value == wf.ID) : (workflowID == wf.ID),
                });
            }

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_A09_FlagTypesEditModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A09_FlagTypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new SiteAdmin_A09_FlagTypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            var siteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.Where(p => !p.IsDeleted).ToList();
            model.StatusGroup.AddRange((from p in siteAdmin_StatusGroups
                                        select new SelectListItem()
                                        {
                                            Text = p.StatusGroupName,
                                            Value = p.ID.ToString(),
                                            Selected = flagType.StatusGroupID.HasValue && flagType.StatusGroupID.Value == p.ID,
                                        }).ToList());
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                SiteAdmin_A09_FlagTypesEditModel.StatusItem statusItem = new SiteAdmin_A09_FlagTypesEditModel.StatusItem()
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
                                                   Selected = flagType.MeetingAgendaGroupID.HasValue && flagType.MeetingAgendaGroupID.Value == p.ID,
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            //var linkedPeople = db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == ID).ToList();

            //foreach (var person in linkedPeople)
            //{
            //    var user = _userManager.FindByIdAsync(person.UserID).Result;
            //    if (user != null)
            //    {
            //        model.A09_Flags_ResponsiblePeople.Add(new SiteAdmin_A09_FlagTypesEditModel.A09_Flags_ResponsiblePersonItem()
            //        {
            //            FlagTypeID = person.FlagTypeID,
            //            ID = person.ID,
            //            UserID = person.UserID,
            //            PersonUsername = user.UserName,
            //        });
            //    }
            //}

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var opProfs = db.OperationalProfiles.ToList();
            var userSecureAreaActions = db.UserSecureAreaActions.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (userSecureAreaActions.Where(p => p.UserID == user.Id && p.SecureAreaID == (int)Data.SecureAreaEnum.A09_Flags_CompanyReview && p.SecureAreaActionID == (int)Data.SecureAreaActionEnum.Edit).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.AvailableUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = model.DefaultAssignedToID == user.Id });
                model.ReportingToUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = flagType.ReportingToUserID == user.Id });
            }

            var a08_Flag_Type_Logs = db.A09_Flag_Type_Logs.Where(p => p.FlagTypeID == ID).ToList();
            foreach (var log in a08_Flag_Type_Logs.OrderByDescending(p => p.DateCreated))
            {
                var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                model.A09_Flag_Type_LogItems.Add(new SiteAdmin_A09_FlagTypesEditModel.A09_Flag_Type_LogItem()
                {
                    DateCreated = log.DateCreated,
                    UserID = log.UserID,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    FlagTypeID = log.FlagTypeID,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                });
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_Edit(int ID, SiteAdmin_A09_FlagTypesEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var flagType = db.A09_Flags_Types.Where(p => p.ID == ID).SingleOrDefault();

            if (flagType == null)
                return Redirect("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes");

            #region Workflows

            model.WorkflowGroupItems = new List<SiteAdmin_A09_FlagTypesEditModel.WorkflowGroupItem>();
            model.BusinessDepartment = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessDepartmentItems = new List<SiteAdmin_A09_FlagTypesEditModel.BusinessDepartmentItem>();
            model.BusinessPillar = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = $"00 - NONE" }
                };
            model.BusinessPillarItems = new List<SiteAdmin_A09_FlagTypesEditModel.BusinessPillarItem>();
            model.WorkflowGroupID = new List<SelectListItem>();

            var workflowGroups = db.WorkflowGroups.ToList();

            int workflowID = 1;
            if (flagType.SecureAreaID.HasValue)
            {
                var sc = db.SecureAreas.Where(p => p.SecureAreaID == flagType.SecureAreaID.Value).SingleOrDefault();
                if (sc != null && sc.GroupID.HasValue)
                    workflowID = sc.GroupID.Value;
            }

            foreach (var wf in workflowGroups)
            {
                model.WorkflowGroupID.Add(new SelectListItem()
                {
                    Text = wf.WorkflowGroupName,
                    Value = wf.ID.ToString(),
                    Selected = Request.Form["WorkflowGroupID"].ToString() == wf.ID.ToString(),
                });
            }

            var businessPillars = db.BusinessPillars.ToList();
            model.BusinessPillarItems = (from p in businessPillars
                                         select new SiteAdmin_A09_FlagTypesEditModel.BusinessPillarItem()
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
                                             select new SiteAdmin_A09_FlagTypesEditModel.BusinessDepartmentItem()
                                             {
                                                 BusinessPillarID = p.BusinessPillarID,
                                                 DisplayName = p.BusinessDepartmentName,
                                                 ID = p.ID,
                                             }).ToList();


            model.WorkflowGroupItems = (from p in workflowGroups
                                        select new SiteAdmin_A09_FlagTypesEditModel.WorkflowGroupItem()
                                        {
                                            BusinessDepartmentID = p.BusinessDepartmentID.HasValue ? p.BusinessDepartmentID.Value : 0,
                                            DisplayName = p.WorkflowGroupName,
                                            ID = p.ID,
                                        }).ToList();

            #endregion

            model.HasComplianceCheck = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes", Selected = Convert.ToBoolean(Request.Form["HasComplianceCheck"]) },
                    new SelectListItem() { Value = "false", Text = "No", Selected = !Convert.ToBoolean(Request.Form["HasComplianceCheck"]) },
                };

            var siteAdmin_Priorities = db.SiteAdmin_Priorities.ToList();
            model.Priority = (from p in siteAdmin_Priorities
                              where !p.IsDeleted
                              orderby p.PriorityName
                              select new SelectListItem()
                              {
                                  Value = p.ID.ToString(),
                                  Text = p.PriorityName,
                                  Selected = Request.Form["Priority"].ToString() == p.ID.ToString(),
                              }).ToList();
            model.AvailableUsers = new List<SelectListItem>();
            model.ReportingToUsers = new List<SelectListItem>();
            model.FlagTypeID = ID;
            model.PolicyDocumentURL = flagType.PolicyDocumentURL;
            model.A09_Flags_Types_SerialsToExcludes = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == ID).ToList();
            model.SkybillCustomers = db.SkybillCustomers.ToList();
            model.DefaultStatusID = flagType.DefaultStatusID;
            model.DefaultMeetingAgendaID = flagType.DefaultMeetingAgendaID;

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var opProfs = db.OperationalProfiles.ToList();
            var userSecureAreaActions = db.UserSecureAreaActions.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (userSecureAreaActions.Where(p => p.UserID == user.Id && p.SecureAreaID == (int)Data.SecureAreaEnum.A09_Flags_CompanyReview && p.SecureAreaActionID == (int)Data.SecureAreaActionEnum.Edit).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.AvailableUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["AvailableUsers"] == user.Id });
                model.ReportingToUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = Request.Form["ReportingToUsers"] == user.Id });
            }

            model.ReportingToUserRequiresCompletedState = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "true", Text = "Yes (Only reporting to user can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? true : false },
                    new SelectListItem() { Value = "false", Text = "No (Both users can close)", Selected = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]) ? false : true },
                };

            model.StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["StatusGroup"]) },
                };

            model.DefaultStatus = new List<SelectListItem>();
            model.StatusItems = new List<SiteAdmin_A09_FlagTypesEditModel.StatusItem>();
            model.MeetingAgendaGroup = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["MeetingAgendaGroup"]) },
                };

            model.DefaultMeetingAgenda = new List<SelectListItem>();
            model.MeetingAgendaItems = new List<SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem>();
            model.A09_Flag_Type_LogItems = new List<SiteAdmin_A09_FlagTypesEditModel.A09_Flag_Type_LogItem>();

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
                SiteAdmin_A09_FlagTypesEditModel.StatusItem statusItem = new SiteAdmin_A09_FlagTypesEditModel.StatusItem()
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
                                                   Selected = Request.Form["MeetingAgendaGroup"] == p.ID.ToString(),
                                               }).ToList());
            var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.Where(p => !p.IsDeleted).ToList();

            foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes)
            {
                SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem MeetingAgendaItem = new SiteAdmin_A09_FlagTypesEditModel.MeetingAgendaItem()
                {
                    DisplayName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == MeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == MeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}",
                    ID = MeetingAgenda.ID,
                    MeetingAgendaGroupID = MeetingAgenda.MeetingAgendaGroupID,
                };

                model.MeetingAgendaItems.Add(MeetingAgendaItem);
            }

            if (ModelState.IsValid)
            {
                StringBuilder sbSysLog = new StringBuilder();

                #region Azure Upload

                if (model.PolicyDocument != null)
                {
                    sbSysLog.AppendLine($"Policy Document uploaded.<br />");

                    // Name of the share, directory, and file we'll create
                    string shareName = "a09-flagtypes";
                    string dirName = $"{flagType.ID}";
                    string fileName = $"{flagType.ID}" + System.IO.Path.GetExtension(model.PolicyDocument.FileName);

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
                    model.PolicyDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);

                    flagType.PolicyDocumentURL = $"{fileName}";
                }

                #endregion


                if (!string.IsNullOrEmpty(Request.Form["addLinkedSecureAreaID"].ToString()) && flagType.SecureAreaID != Convert.ToInt32(Request.Form["addLinkedSecureAreaID"]))
                {
                    if (!flagType.SecureAreaID.HasValue)
                    {
                        sbSysLog.AppendLine($"addLinkedSecureAreaID added '{((MyVoltage.Data.SecureAreaEnum)Convert.ToInt32(Request.Form["addLinkedSecureAreaID"])).GetDescription()}'<br />");
                    }
                    else
                    {
                        sbSysLog.AppendLine($"addLinkedSecureAreaID from '{((MyVoltage.Data.SecureAreaEnum)flagType.SecureAreaID.Value).GetDescription()}' to '{((MyVoltage.Data.SecureAreaEnum)Convert.ToInt32(Request.Form["addLinkedSecureAreaID"])).GetDescription()}'<br />");
                    }
                    flagType.SecureAreaID = Convert.ToInt32(Request.Form["addLinkedSecureAreaID"]);
                }
                else if (flagType.SecureAreaID.HasValue && string.IsNullOrEmpty(Request.Form["addLinkedSecureAreaID"].ToString()))
                {
                    sbSysLog.AppendLine($"addLinkedSecureAreaID removed.<br />");
                    flagType.SecureAreaID = null;
                }

                if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupID"].ToString()) && flagType.SecureAreaGroupID != Convert.ToInt32(Request.Form["WorkflowGroupID"]))
                {
                    var beforeWF = flagType.SecureAreaGroupID.HasValue && workflowGroups.Where(p => p.ID == flagType.SecureAreaGroupID.Value).SingleOrDefault() != null ? workflowGroups.Where(p => p.ID == flagType.SecureAreaGroupID.Value).SingleOrDefault().WorkflowGroupName : "";
                    var afterWF = workflowGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["WorkflowGroupID"])).SingleOrDefault();

                    sbSysLog.AppendLine($"SecureAreaGroupID from '{beforeWF}' to '{afterWF.WorkflowGroupName}'<br />");
                    flagType.SecureAreaGroupID = Convert.ToInt32(Request.Form["WorkflowGroupID"]);
                }

                if (flagType.FlagTypeName != model.FlagTypeName)
                {
                    sbSysLog.AppendLine($"FlagTypeName from '{flagType.FlagTypeName}' to '{model.FlagTypeName}'<br />");
                    flagType.FlagTypeName = model.FlagTypeName;
                }

                if (flagType.Identifier != model.Identifier)
                {
                    sbSysLog.AppendLine($"Identifier from '{flagType.Identifier}' to '{model.Identifier}'<br />");
                    flagType.Identifier = model.Identifier;
                }

                if (!string.IsNullOrEmpty(Request.Form["StatusGroup"].ToString()) && flagType.StatusGroupID != Convert.ToInt32(Request.Form["StatusGroup"]))
                {
                    var statusGroupBefore = siteAdmin_StatusGroups.Where(p => p.ID == flagType.StatusGroupID).SingleOrDefault();
                    var statusGroupAfter = siteAdmin_StatusGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["StatusGroup"])).SingleOrDefault();
                    sbSysLog.AppendLine($"StatusGroup from '{statusGroupBefore.StatusGroupName}' to '{statusGroupAfter.StatusGroupName}'<br />");

                    flagType.StatusGroupID = Convert.ToInt32(Request.Form["StatusGroup"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["DefaultStatus"].ToString()) && flagType.DefaultStatusID != Convert.ToInt32(Request.Form["DefaultStatus"]))
                {
                    var defaultStatusBefore = siteAdmin_Statuses.Where(p => p.ID == flagType.DefaultStatusID).SingleOrDefault();
                    var defaultStatusAfter = siteAdmin_Statuses.Where(p => p.ID == Convert.ToInt32(Request.Form["DefaultStatus"])).SingleOrDefault();
                    sbSysLog.AppendLine($"DefaultStatus from '{siteAdmin_StatusActions.Where(p => p.ID == defaultStatusBefore.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == defaultStatusBefore.StatusReportingID).SingleOrDefault().StatusReportingName}' to '{siteAdmin_StatusActions.Where(p => p.ID == defaultStatusAfter.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == defaultStatusAfter.StatusReportingID).SingleOrDefault().StatusReportingName}'<br />");
                    flagType.DefaultStatusID = Convert.ToInt32(Request.Form["DefaultStatus"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["MeetingAgendaGroup"].ToString()) && flagType.MeetingAgendaGroupID != Convert.ToInt32(Request.Form["MeetingAgendaGroup"]))
                {
                    string MeetingAgendaGroupBeforeName = "";
                    if (flagType.MeetingAgendaGroupID.HasValue)
                    {
                        var MeetingAgendaGroupBefore = siteAdmin_MeetingAgendaGroups.Where(p => p.ID == flagType.MeetingAgendaGroupID).SingleOrDefault();
                        MeetingAgendaGroupBeforeName = MeetingAgendaGroupBefore != null ? MeetingAgendaGroupBefore.MeetingAgendaGroupName : "";
                    }
                    var MeetingAgendaGroupAfter = siteAdmin_MeetingAgendaGroups.Where(p => p.ID == Convert.ToInt32(Request.Form["MeetingAgendaGroup"])).SingleOrDefault();
                    sbSysLog.AppendLine($"MeetingAgendaGroup from '{MeetingAgendaGroupBeforeName}' to '{MeetingAgendaGroupAfter.MeetingAgendaGroupName}'<br />");

                    flagType.MeetingAgendaGroupID = Convert.ToInt32(Request.Form["MeetingAgendaGroup"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["DefaultMeetingAgenda"].ToString()) && flagType.DefaultMeetingAgendaID != Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]))
                {
                    string defaultMeetingAgendaBeforeName = "";
                    if (flagType.DefaultMeetingAgendaID.HasValue)
                    {
                        var defaultMeetingAgendaBefore = siteAdmin_MeetingAgendaes.Where(p => p.ID == flagType.DefaultMeetingAgendaID).SingleOrDefault();
                        defaultMeetingAgendaBeforeName = $"{siteAdmin_MeetingAgendaActions.Where(p => p.ID == defaultMeetingAgendaBefore.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == defaultMeetingAgendaBefore.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}";
                    }
                    var defaultMeetingAgendaAfter = siteAdmin_MeetingAgendaes.Where(p => p.ID == Convert.ToInt32(Request.Form["DefaultMeetingAgenda"])).SingleOrDefault();
                    sbSysLog.AppendLine($"DefaultMeetingAgenda from '{defaultMeetingAgendaBeforeName}' to '{siteAdmin_MeetingAgendaActions.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == defaultMeetingAgendaAfter.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}'<br />");
                    flagType.DefaultMeetingAgendaID = Convert.ToInt32(Request.Form["DefaultMeetingAgenda"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["ReportingToUsers"].ToString()) && flagType.ReportingToUserID != Request.Form["ReportingToUsers"].ToString())
                {
                    var opProfReportingToUserBefore = opProfs.Where(p => p.UserID == flagType.ReportingToUserID).SingleOrDefault();
                    var opProfReportingToUserAfter = opProfs.Where(p => p.UserID == Request.Form["ReportingToUsers"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ReportingToUser from '{opProfReportingToUserBefore.FirstName} {opProfReportingToUserBefore.LastName}' to '{opProfReportingToUserAfter.FirstName} {opProfReportingToUserAfter.LastName}'<br />");
                    flagType.ReportingToUserID = Request.Form["ReportingToUsers"];
                }

                if (!string.IsNullOrEmpty(Request.Form["AvailableUsers"].ToString()) && flagType.DefaultAssignedToID != Request.Form["AvailableUsers"].ToString())
                {
                    var opProfReportingToUserBefore = opProfs.Where(p => p.UserID == flagType.DefaultAssignedToID).SingleOrDefault();
                    var opProfReportingToUserAfter = opProfs.Where(p => p.UserID == Request.Form["AvailableUsers"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ReportingToUser from '{opProfReportingToUserBefore.FirstName} {opProfReportingToUserBefore.LastName}' to '{opProfReportingToUserAfter.FirstName} {opProfReportingToUserAfter.LastName}'<br />");
                    flagType.DefaultAssignedToID = Request.Form["AvailableUsers"];
                }

                if (!string.IsNullOrEmpty(Request.Form["Priority"].ToString()) && flagType.PriorityID != Convert.ToInt32(Request.Form["Priority"]))
                {
                    var priorityBefore = siteAdmin_Priorities.Where(p => p.ID == flagType.PriorityID).SingleOrDefault();
                    var priorityAfter = siteAdmin_Priorities.Where(p => p.ID == Convert.ToInt32(Request.Form["Priority"])).SingleOrDefault();
                    sbSysLog.AppendLine($"Priority from '{priorityBefore.PriorityName}' to '{priorityAfter.PriorityName}'<br />");
                    flagType.PriorityID = Convert.ToInt32(Request.Form["Priority"]);
                }

                if (flagType.ReportingToUserRequiresCompletedState != Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]))
                {
                    sbSysLog.AppendLine($"ReportingToUserRequiresCompletedState from '{flagType.ReportingToUserRequiresCompletedState}' to '{Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"])}'<br />");
                    flagType.ReportingToUserRequiresCompletedState = Convert.ToBoolean(Request.Form["ReportingToUserRequiresCompletedState"]);
                }

                if (flagType.HasComplianceCheck != Convert.ToBoolean(Request.Form["HasComplianceCheck"]))
                {
                    sbSysLog.AppendLine($"HasComplianceCheck from '{flagType.HasComplianceCheck}' to '{Convert.ToBoolean(Request.Form["HasComplianceCheck"])}'<br />");
                    flagType.HasComplianceCheck = Convert.ToBoolean(Request.Form["HasComplianceCheck"]);
                }

                if (model.DefautlMinPlanned.HasValue && flagType.DefautlMinPlanned != model.DefautlMinPlanned)
                {
                    sbSysLog.AppendLine($"DefautlMinPlanned from '{flagType.DefautlMinPlanned}' to '{model.DefautlMinPlanned}'<br />");
                    flagType.DefautlMinPlanned = model.DefautlMinPlanned;
                }

                if (!string.IsNullOrEmpty(model.FlagTypeName) && flagType.FlagTypeName != model.FlagTypeName)
                {
                    sbSysLog.AppendLine($"FlagTypeName from '{flagType.FlagTypeName}' to '{model.FlagTypeName}'<br />");
                    flagType.FlagTypeName = model.FlagTypeName;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    Data.A09_Flags.A09_Flag_Type_Log log = new Data.A09_Flags.A09_Flag_Type_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        FlagTypeID = flagType.ID,
                    };

                    db.Add(log);
                    db.SaveChanges();

                    db.Update(flagType);
                    db.SaveChanges();
                }
                _cache.Remove(MVCache.KEY_A09_Flags_Types);
                model.IsSuccess = true;
            }

            //A09_Flags_ResponsiblePeople = new List<SiteAdmin_A09_FlagTypesEditModel.A09_Flags_ResponsiblePersonItem>(),
            //var linkedPeople = db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == ID).ToList();

            //foreach (var person in linkedPeople)
            //{
            //    var user = _userManager.FindByIdAsync(person.UserID).Result;
            //    if (user != null)
            //    {
            //        model.A09_Flags_ResponsiblePeople.Add(new SiteAdmin_A09_FlagTypesEditModel.A09_Flags_ResponsiblePersonItem()
            //        {
            //            FlagTypeID = person.FlagTypeID,
            //            ID = person.ID,
            //            UserID = person.UserID,
            //            PersonUsername = user.UserName,
            //        });
            //    }
            //}


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes_SerialToExcludeAdd/{flagTypeID}")]
        public async Task<IActionResult> SiteAdmin_A09_FlagTypes_SerialToExcludeAdd(int flagTypeID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_code"])
                )
            {
                try
                {
                    var serialsToExcludeToEdit = (from p in db.A09_Flags_Types_SerialsToExcludes
                                                  where p.FlagTypeID == flagTypeID
                                                  && p.SerialToExclude == Request.Form["add_code"]
                                                  select p).FirstOrDefault();

                    if (serialsToExcludeToEdit != null)
                    {
                        serialsToExcludeToEdit.SerialToExclude = Request.Form["add_code"];
                        db.Update(serialsToExcludeToEdit);
                    }
                    else
                    {
                        serialsToExcludeToEdit = new Data.A09_Flags.A09_Flags_Types_SerialsToExclude()
                        {
                            CreatedByID = _userManager.GetUserId(User),
                            DateCreated = DateTime.Now,
                            FlagTypeID = flagTypeID,
                            SerialToExclude = Request.Form["add_code"],
                        };
                        db.Add(serialsToExcludeToEdit);
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
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes_SerialToExcludeDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_A09_FlagTypes_SerialToExcludeDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var a09_Flags_Types_SerialsToExclude = (from p in db.A09_Flags_Types_SerialsToExcludes
                                                        where p.ID == ID
                                                        select p).SingleOrDefault();

                if (a09_Flags_Types_SerialsToExclude != null)
                {
                    db.Remove(a09_Flags_Types_SerialsToExclude);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes_SerialToExclude/searchmeters")]
        public JsonResult SearchMeters(string Prefix)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<object> results = new List<object>();

            int nCount = 0;

            foreach (var serial in dbCache.SkybillCustomers.Where(p => p.Customer_Name.Contains(Prefix) || p.Serial_No.Contains(Prefix) || p.Customer_No.Contains(Prefix)).Select(p => p.Serial_No).Distinct())
            {

                var sbCustomer = (from p in dbCache.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();

                if (sbCustomer == null)
                    continue;

                nCount++;
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                string text = $"{serial} ({sbCustomer.Customer_No} - {sbCustomer.Customer_Name} - {company.Name})";

                results.Add(new
                {
                    Text = text,
                    Value = serial
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/AddResponsiblePerson/{flagTypeID}/{userID}")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_AddResponsiblePerson(int flagTypeID, string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var doesAlreadyExist = db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == flagTypeID && p.UserID == userID).Count() > 0 ? true : false;

            if (!doesAlreadyExist)
            {
                Data.A09_Flags.A09_Flags_ResponsiblePerson responsiblePerson = new Data.A09_Flags.A09_Flags_ResponsiblePerson()
                {
                    FlagTypeID = flagTypeID,
                    UserID = userID,
                };

                db.Add(responsiblePerson);
                db.SaveChanges();
            }

            _cache.Remove(MVCache.KEY_A09_Flags_ResponsiblePersons);
            return Redirect($"/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit/{flagTypeID}");

        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/DeleteResponsiblePerson/{flagTypeID}/{userID}")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_A09_FlagTypes_DeleteResponsiblePerson(int flagTypeID, string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_A09_FlagTypes, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_A09_FlagTypes}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var doesAlreadyExist = db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == flagTypeID && p.UserID == userID).Count() > 0 ? true : false;

            if (doesAlreadyExist)
            {
                db.Remove(db.A09_Flags_ResponsiblePersons.Where(p => p.FlagTypeID == flagTypeID && p.UserID == userID).FirstOrDefault());
                db.SaveChanges();
            }
            _cache.Remove(MVCache.KEY_A09_Flags_ResponsiblePersons);

            return Redirect($"/operational/SiteAdmin/SiteAdmin_A09_FlagTypes/Edit/{flagTypeID}");

        }

    }
}
