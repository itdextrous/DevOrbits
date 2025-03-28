using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
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
using MyVoltage.Data.A09_Flags;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A09_Flags.A09_FlagsModels;
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

namespace MyVoltage.Controllers.Operational.A09_Flags
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A09_FlagsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public A09_FlagsController(
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
        [Route("/operational/A09_Flags/GetFlagTypePolicyDocument/{ID}")]
        public async Task<IActionResult> GetFlagTypePolicyDocument(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.A09_Flags_Types.Where(p => p.ID == ID).SingleOrDefault();
            string contentType = "text/plain";
            if (item != null)
            {
                string shareName = "a09-flagtypes";
                string dirName = $"{item.ID}";
                string fileName = $"{item.ID}" + System.IO.Path.GetExtension(item.PolicyDocumentURL);

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
            return File(System.Text.Encoding.UTF8.GetBytes("The file you are looking for could not be found."), contentType, "NotFound.txt");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanySummary")]
        public async Task<IActionResult> A09_Flags_CompanySummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A09_Flags_Company_SummaryModel model = new A09_Flags_Company_SummaryModel()
            {
                A09_Flags_CompanySummaryItems = new List<A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                A09_Flags_Company_SummaryStatusItems = new List<A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryStatusItem>(),
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

            List<Data.A09_Flags.A09_Flag> a09_Flags = (from p in db.A09_Flags
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
                A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryStatusItem item = new A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryStatusItem()
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
                model.A09_Flags_Company_SummaryStatusItems.Add(item);
            }

            #region No Company

            var noCompanyItems = a09_Flags.Where(p => !p.CompanyID.HasValue).ToList();

            if (noCompanyItems.Count > 0)
            {
                A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem item = new A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem()
                {
                    CompanyID = 0,
                    CompanyName = "None",
                    A09_Flags_Company_SummaryItemStatuses = new List<A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus>(),
                };

                foreach (var status in siteAdmin_Statuses)
                {
                    if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                        continue;
                    A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus itemStatus = new A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus()
                    {
                        Count = noCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                        StatusID = status.ID,
                        StatusGroupID = status.StatusGroupID,
                    };

                    item.A09_Flags_Company_SummaryItemStatuses.Add(itemStatus);
                }

                foreach (var flag in noCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                {
                    TimeSpan openDuration = DateTime.Now - flag.DueDate.Value;

                    if (flag.DueDate.Value.Date == DateTime.Now.Date)
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

                var oldestNoCompanyFlag = noCompanyItems.OrderBy(p => p.DueDate.Value).FirstOrDefault();
                if (oldestNoCompanyFlag != null)
                {
                    item.OldestUnresolvedFlagCreateDate = oldestNoCompanyFlag.DueDate.Value;
                    item.OldestUnresolvedFlagID = oldestNoCompanyFlag.ID;
                    item.OldestUnresolvedFlagTypeID = oldestNoCompanyFlag.FlagTypeID;
                }

                model.A09_Flags_CompanySummaryItems.Add(item);
            }

            #endregion

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var thisCompanyItems = a09_Flags.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

                if (thisCompanyItems.Count > 0)
                {
                    A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem item = new A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        A09_Flags_Company_SummaryItemStatuses = new List<A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                            continue;
                        A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus itemStatus = new A09_Flags_Company_SummaryModel.A09_Flags_Company_SummaryItem.A09_Flags_Company_SummaryItemStatus()
                        {
                            Count = thisCompanyItems.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                            StatusGroupID = status.StatusGroupID,
                        };

                        item.A09_Flags_Company_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var flag in thisCompanyItems.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - flag.DueDate.Value;

                        if (flag.DueDate.Value.Date == DateTime.Now.Date)
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
                        item.OldestUnresolvedFlagCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedFlagID = oldestFlag.ID;
                        item.OldestUnresolvedFlagTypeID = oldestFlag.FlagTypeID;
                    }

                    model.A09_Flags_CompanySummaryItems.Add(item);
                }

            }



            return View("~/Views/Operational/A09_Flags/A09_Flags_CompanySummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyDetails")]
        public async Task<IActionResult> A09_Flags_CompanyDetails()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyResults")]
        public async Task<IActionResult> A09_Flags_CompanyResults()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview/{flagTypeID?}/{flagID?}")]
        public async Task<IActionResult> A09_Flags_CompanyReview(int? flagTypeID, int? flagID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return Redirect($"/operational/changeFlagID/{flagTypeID}/{flagID}?R=/operational/A09_Flags/A09_Flags_CompanyReview");

        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview")]
        public async Task<IActionResult> A09_Flags_CompanyReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
            {
                if (_operationalProvider.CompanyID > 0)
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanyResults");
                else
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");
            }

            var db = new MyVoltageDbContext(_options);
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

            //var responsiblePeople = dbCache.A09_Flags_ResponsiblePersons;

            var flag = db.A09_Flags.Where(p => p.ID == _operationalProvider.FlagSelectedFlagID).SingleOrDefault();

            if (flag.LinkedObjectDBTableName == "SkybillCustomers" && _operationalProvider.CustomerNumber != flag.LinkedObjectUniqueID)
            {
                var sc = db.SkybillCustomers.Where(p => p.Customer_No == flag.LinkedObjectUniqueID).FirstOrDefault();
                if (sc != null && sc.Serial_No != _operationalProvider.CustomerMeterSerial)
                    return Redirect($"/operational/changeActiveMeter/{sc.Serial_No}?R=/operational/A09_Flags/A09_Flags_CompanyReview");
            }
            else if (flag.LinkedObjectDBTableName == "Devices" && _operationalProvider.CustomerMeterSerial != flag.LinkedObjectUniqueID)
            {
                var sc = db.SkybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID).FirstOrDefault();
                if (sc != null)
                    return Redirect($"/operational/changeActiveMeter/{sc.Serial_No}?R=/operational/A09_Flags/A09_Flags_CompanyReview");
            }

            if (!flag.GPSLong.HasValue && !flag.GPSLat.HasValue && !string.IsNullOrEmpty(flag.CustomerNo))
            {
                decimal? gpsLat = null;
                decimal? gpsLong = null;

                var skybillCustomer = (from p in db.SkybillCustomers
                                       where p.Customer_No == flag.CustomerNo
                                       select p).FirstOrDefault();

                if (skybillCustomer != null)
                {
                    if (!string.IsNullOrEmpty(skybillCustomer.GPS_Coordinates))
                    {
                        string[] gps = skybillCustomer.GPS_Coordinates.Split(',');
                        try { gpsLat = Convert.ToDecimal(gps[0]); }
                        catch { }
                        try { gpsLong = Convert.ToDecimal(gps[1]); }
                        catch { }
                    }
                    var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();

                    if (flagToUpdate.GPSLong != gpsLong)
                        flagToUpdate.GPSLong = gpsLong;
                    if (flagToUpdate.GPSLat != gpsLat)
                        flagToUpdate.GPSLat = gpsLat;

                    db.Update(flagToUpdate);
                    db.SaveChanges();

                    flag = flagToUpdate;
                }
            }

            var userID = _userManager.GetUserId(User);
            if (flag.AssignedToID == userID)
            {
                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
                flagToUpdate.DateLastViewdBy = userID;
                flagToUpdate.DateLastViewed = DateTime.Now;
                db.Update(flagToUpdate);
                db.SaveChanges();

                _cache.Remove($"{MVCache.KEY_A09_Flags}");
            }
            var fType = db.A09_Flags_Types.Where(p => p.ID == _operationalProvider.FlagSelectedFlagTypeID).SingleOrDefault();
            A09_Flags_CompanyReviewModel model = new A09_Flags_CompanyReviewModel()
            {
                FlagType = new A09_Flags_CompanyReviewModel.A09_Flags_TypeItem()
                {
                    A09_Flag_TypeStatus = new A09_Flags_CompanyReviewModel.A09_Flag_TypeStatus(),
                    Active = fType.Active,
                    AssignedToUsername = "",
                    BusinessDepartmentName = "",
                    BusinessPillarName = "",
                    DefaultAssignedToID = fType.DefaultAssignedToID,
                    DefaultStatusID = fType.DefaultStatusID,
                    DefautlMinPlanned = fType.DefautlMinPlanned,
                    FlagPriority = db.SiteAdmin_Priorities.Where(p => p.ID == fType.PriorityID).SingleOrDefault(),
                    FlagTypeName = fType.FlagTypeName,
                    HasComplianceCheck = fType.HasComplianceCheck,
                    ID = fType.ID,
                    PolicyDocumentURL = fType.PolicyDocumentURL,
                    PriorityID = fType.PriorityID,
                    ReportingToUserID = fType.ReportingToUserID,
                    ReportingToUserRequiresCompletedState = fType.ReportingToUserRequiresCompletedState,
                    ReportingToUserUsername = "",
                    SecureAreaGroupID = fType.SecureAreaGroupID,
                    SecureAreaID = fType.SecureAreaID,
                    StatusGroupID = fType.StatusGroupID,
                    WorkflowGroupName = "",
                    StatusGroup = SiteAdmin_StatusGroups.Where(p => p.ID == fType.StatusGroupID.Value).SingleOrDefault(),
                    DefaultMeetingAgendaID = fType.DefaultMeetingAgendaID,
                    Identifier = fType.Identifier,
                    MeetingAgendaGroupID = fType.MeetingAgendaGroupID,
                },
                Flag = flag,
                //A09_Flags_ResponsiblePeople = new List<A09_Flags_CompanyReviewModel.A09_Flags_ResponsiblePersonItem>(),
                AvailableUsers = new List<SelectListItem>(),
                A09_Flags_ReassignLogItems = new List<A09_Flags_CompanyReviewModel.A09_Flags_ReassignLogItem>(),
                FlagPriority = db.SiteAdmin_Priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                A09_FlagStatusItem = new A09_Flags_CompanyReviewModel.A09_FlagStatus(),
                A09_Flag_TypeStatuses = new List<A09_Flags_CompanyReviewModel.A09_Flag_TypeStatus>(),
                A09_Flag_TypeMeetingAgendaes = new List<A09_Flags_CompanyReviewModel.A09_Flag_TypeMeetingAgenda>(),
                ReportingToUsers = new List<SelectListItem>(),
                A09_Flag_Review_AttachmentItems = new List<A09_Flags_CompanyReviewModel.A09_Flag_Review_AttachmentItem>(),
                Module_TimeOfWorkPlanneds = new List<A09_Flags_CompanyReviewModel.Module_TimeOfWorkPlanned>(),
                Module_TimeOfWorkAllocateds = new List<A09_Flags_CompanyReviewModel.Module_TimeOfWorkAllocated>(),
                Module_TravelAllocations = new List<A09_Flags_CompanyReviewModel.Module_TravelAllocation>(),
                Module_StockAllocations = new List<A09_Flags_CompanyReviewModel.Module_StockAllocation>(),
                Module_InvoiceAllocations = new List<A09_Flags_CompanyReviewModel.Module_InvoiceAllocation>(),
                Module_NonCompliances = new List<A09_Flags_CompanyReviewModel.Module_NonCompliance>(),
                A09_FlagMeetingAgendaItem = new A09_Flags_CompanyReviewModel.A09_FlagMeetingAgenda(),
            };

            int workflowID = 1;
            if (model.FlagType.SecureAreaID.HasValue)
            {
                var sc = db.SecureAreas.Where(p => p.SecureAreaID == model.FlagType.SecureAreaID.Value).SingleOrDefault();
                if (sc != null && sc.GroupID.HasValue)
                    workflowID = sc.GroupID.Value;
            }
            if (model.FlagType.SecureAreaGroupID.HasValue)
            {
                workflowID = model.FlagType.SecureAreaGroupID.Value;
            }

            var workflow = db.WorkflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
            if (workflow != null)
            {
                model.FlagType.WorkflowGroupName = workflow.WorkflowGroupName;
                if (string.IsNullOrEmpty(model.Flag.WorkflowGroupName))
                    model.Flag.WorkflowGroupName = workflow.WorkflowGroupName;
                if (workflow.BusinessDepartmentID.HasValue)
                {
                    var department = db.BusinessDepartments.Where(p => p.ID == workflow.BusinessDepartmentID.Value).SingleOrDefault();
                    var pillar = db.BusinessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
                    if (string.IsNullOrEmpty(model.Flag.BusinessDepartmentName))
                        model.Flag.BusinessDepartmentName = department.BusinessDepartmentName;
                    if (string.IsNullOrEmpty(model.Flag.BusinessPillarName))
                        model.Flag.BusinessPillarName = pillar.BusinessPillarName;

                    model.FlagType.BusinessPillarName = pillar.BusinessPillarName;
                    model.FlagType.BusinessDepartmentName = department.BusinessDepartmentName;
                }
            }

            var taskStatus = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
            if (taskStatus != null)
            {
                model.A09_FlagStatusItem = new A09_Flags_CompanyReviewModel.A09_FlagStatus()
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

            if (flag.MeetingAgendaID.HasValue)
            {
                var taskMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == flag.MeetingAgendaID).SingleOrDefault();
                if (taskMeetingAgenda != null)
                {
                    model.A09_FlagMeetingAgendaItem = new A09_Flags_CompanyReviewModel.A09_FlagMeetingAgenda()
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

            var taskTypeStatus = siteAdmin_Statuses.Where(p => p.ID == fType.DefaultStatusID.Value).SingleOrDefault();
            if (taskTypeStatus != null)
            {
                model.FlagType.A09_Flag_TypeStatus = new A09_Flags_CompanyReviewModel.A09_Flag_TypeStatus()
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

            if (model.FlagType.StatusGroupID.HasValue)
            {
                foreach (var status in siteAdmin_Statuses.Where(p => p.StatusGroupID == model.FlagType.StatusGroupID.Value && !p.IsDeleted))
                {
                    A09_Flags_CompanyReviewModel.A09_Flag_TypeStatus A09_Flag_TypeStatus = new A09_Flags_CompanyReviewModel.A09_Flag_TypeStatus()
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
                    model.A09_Flag_TypeStatuses.Add(A09_Flag_TypeStatus);
                }
            }

            if (model.FlagType.MeetingAgendaGroupID.HasValue)
            {
                foreach (var MeetingAgenda in siteAdmin_MeetingAgendaes.Where(p => p.MeetingAgendaGroupID == model.FlagType.MeetingAgendaGroupID.Value && !p.IsDeleted))
                {
                    A09_Flags_CompanyReviewModel.A09_Flag_TypeMeetingAgenda A09_Flag_TypeMeetingAgenda = new A09_Flags_CompanyReviewModel.A09_Flag_TypeMeetingAgenda()
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
                    model.A09_Flag_TypeMeetingAgendaes.Add(A09_Flag_TypeMeetingAgenda);
                }
            }


            var opProfAssignedToFT = opProfs.Where(p => p.UserID == model.Flag.AssignedToID).SingleOrDefault();
            if (opProfAssignedToFT != null)
                model.FlagType.AssignedToUsername = $"{opProfAssignedToFT.FirstName} {opProfAssignedToFT.LastName}";
            var opProfReportingToFT = opProfs.Where(p => p.UserID == model.Flag.AssignedToID).SingleOrDefault();
            if (opProfReportingToFT != null)
                model.FlagType.ReportingToUserUsername = $"{opProfReportingToFT.FirstName} {opProfReportingToFT.LastName}";

            var opProfAssignedTo = opProfs.Where(p => p.UserID == model.Flag.AssignedToID).SingleOrDefault();
            if (opProfAssignedTo != null)
                model.AssignedToUsername = $"{opProfAssignedTo.FirstName} {opProfAssignedTo.LastName}";
            else
                model.AssignedToUsername = _userManager.FindByIdAsync(model.Flag.AssignedToID).Result.UserName;

            var opProfReportingTo = opProfs.Where(p => p.UserID == model.Flag.AssignedToID).SingleOrDefault();
            if (opProfReportingTo != null)
                model.ReportingToUserUsername = $"{opProfReportingTo.FirstName} {opProfReportingTo.LastName}";
            else
                model.ReportingToUserUsername = _userManager.FindByIdAsync(model.Flag.ReportingToUserID).Result.UserName;

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var userSecureAreaActions = db.UserSecureAreaActions.ToList();

            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (userSecureAreaActions.Where(p => p.UserID == user.Id && p.SecureAreaID == (int)Data.SecureAreaEnum.A09_Flags_CompanyReview && p.SecureAreaActionID == (int)Data.SecureAreaActionEnum.Edit).Count() == 0)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.AvailableUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = flag.AssignedToID == user.Id });
                model.ReportingToUsers.Add(new SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName, Selected = flag.ReportingToUserID == user.Id });
            }

            model.AvailableUsers = model.AvailableUsers.OrderBy(p => p.Text).ToList();
            model.ReportingToUsers = model.ReportingToUsers.OrderBy(p => p.Text).ToList();

            var module_TimeOfWorkPlanneds = db.Module_TimeOfWorkPlanneds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var timeOfWorkPlanned in module_TimeOfWorkPlanneds)
            {
                A09_Flags_CompanyReviewModel.Module_TimeOfWorkPlanned module_TimeOfWorkPlanned = new A09_Flags_CompanyReviewModel.Module_TimeOfWorkPlanned()
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

                model.Module_TimeOfWorkPlanneds.Add(module_TimeOfWorkPlanned);
            }
            model.Module_TimeOfWorkPlanneds = model.Module_TimeOfWorkPlanneds.OrderByDescending(p => p.DateCreated).ToList();

            var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
            {
                A09_Flags_CompanyReviewModel.Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new A09_Flags_CompanyReviewModel.Module_TimeOfWorkAllocated()
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

                model.Module_TimeOfWorkAllocateds.Add(module_TimeOfWorkAllocated);
            }
            model.Module_TimeOfWorkAllocateds = model.Module_TimeOfWorkAllocateds.OrderByDescending(p => p.DateCreated).ToList();

            var module_TravelAllocations = db.Module_TravelAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var TravelAllocation in module_TravelAllocations)
            {
                A09_Flags_CompanyReviewModel.Module_TravelAllocation module_TravelAllocation = new A09_Flags_CompanyReviewModel.Module_TravelAllocation()
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

                model.Module_TravelAllocations.Add(module_TravelAllocation);
            }
            model.Module_TravelAllocations = model.Module_TravelAllocations.OrderByDescending(p => p.DateCreated).ToList();

            var module_StockAllocations = db.Module_StockAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var StockAllocation in module_StockAllocations)
            {
                A09_Flags_CompanyReviewModel.Module_StockAllocation module_StockAllocation = new A09_Flags_CompanyReviewModel.Module_StockAllocation()
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

                model.Module_StockAllocations.Add(module_StockAllocation);
            }
            model.Module_StockAllocations = model.Module_StockAllocations.OrderByDescending(p => p.DateCreated).ToList();

            var module_InvoiceAllocations = db.Module_InvoiceAllocations.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var InvoiceAllocation in module_InvoiceAllocations)
            {
                A09_Flags_CompanyReviewModel.Module_InvoiceAllocation module_InvoiceAllocation = new A09_Flags_CompanyReviewModel.Module_InvoiceAllocation()
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

                model.Module_InvoiceAllocations.Add(module_InvoiceAllocation);
            }
            model.Module_InvoiceAllocations = model.Module_InvoiceAllocations.OrderByDescending(p => p.DateCreated).ToList();

            //foreach (var person in responsiblePeople.Where(p => p.FlagTypeID == flagTypeID).ToList())
            //{
            //    var user = _userManager.FindByIdAsync(person.UserID).Result;
            //    if (user != null)
            //    {
            //        model.A09_Flags_ResponsiblePeople.Add(new A09_Flags_CompanyReviewModel.A09_Flags_ResponsiblePersonItem()
            //        {
            //            FlagTypeID = person.FlagTypeID,
            //            ID = person.ID,
            //            UserID = person.UserID,
            //            PersonUsername = user.UserName,
            //        });
            //    }
            //}

            var a09_Flags_ReassignLog = db.A09_Flags_ReassignLogs.Where(p => p.FlagID == _operationalProvider.FlagSelectedFlagID).ToList();
            foreach (var reassign in a09_Flags_ReassignLog)
            {
                string reassignedByUsername = "";
                if (!string.IsNullOrEmpty(reassign.ReassignedByID))
                {
                    var op = opProfs.Where(p => p.UserID == reassign.ReassignedByID).SingleOrDefault();
                    if (op != null && !string.IsNullOrEmpty(op.FirstName))
                    {
                        reassignedByUsername = op.FirstName + " " + op.LastName;
                    }
                    else
                    {
                        reassignedByUsername = operationalUsers.Where(p => p.Id == reassign.ReassignedByID).FirstOrDefault().UserName;
                    }
                }
                else
                    reassignedByUsername = "[System]";

                string reassignedToUsername = "";
                if (!string.IsNullOrEmpty(reassign.ReassignedToID))
                {
                    var op = opProfs.Where(p => p.UserID == reassign.ReassignedToID).SingleOrDefault();
                    if (op != null && !string.IsNullOrEmpty(op.FirstName))
                    {
                        reassignedToUsername = op.FirstName + " " + op.LastName;
                    }
                    else
                    {
                        reassignedToUsername = operationalUsers.Where(p => p.Id == reassign.ReassignedToID).FirstOrDefault().UserName;
                    }
                }
                else
                    reassignedToUsername = "[System]";


                A09_Flags_CompanyReviewModel.A09_Flags_ReassignLogItem item = new A09_Flags_CompanyReviewModel.A09_Flags_ReassignLogItem()
                {
                    Comments = reassign.Comments,
                    Created = reassign.Created,
                    FlagID = reassign.FlagID,
                    ID = reassign.ID,
                    ReassignedByID = reassign.ReassignedByID,
                    ReassignedByUsername = reassignedByUsername,
                    ReassignedToID = reassign.ReassignedToID,
                    ReassignedToUsername = reassignedToUsername,
                    ActionID = reassign.ActionID,
                    Status = new A09_Flags_CompanyReviewModel.A09_FlagStatus(),
                    SystemDescription = reassign.SystemDescription,
                };

                var reassignStatus = siteAdmin_Statuses.Where(p => p.ID == reassign.ActionID).SingleOrDefault();
                if (reassignStatus != null)
                {
                    item.Status = new A09_Flags_CompanyReviewModel.A09_FlagStatus()
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


                model.A09_Flags_ReassignLogItems.Add(item);
            }

            var attachments = db.A09_Flags_Attachments.Where(p => p.FlagID == _operationalProvider.FlagSelectedFlagID).ToList();
            foreach (var rLog in attachments)
            {
                A09_Flags_CompanyReviewModel.A09_Flag_Review_AttachmentItem attachmentItem = new A09_Flags_CompanyReviewModel.A09_Flag_Review_AttachmentItem()
                {
                    DateCreated = rLog.DateCreated,
                    ID = rLog.ID,
                    FlagID = rLog.FlagID,
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

                model.A09_Flag_Review_AttachmentItems.Add(attachmentItem);
            }
            model.A09_Flag_Review_AttachmentItems = model.A09_Flag_Review_AttachmentItems.OrderByDescending(p => p.DateCreated).ToList();

            var module_NonCompliances = db.Module_NonCompliances.Where(p => p.ActivityTypeID == (int)Data.ActivityTypeEnum.A09_Flag && p.ActivityID == flag.ID).ToList();
            foreach (var NonCompliance in module_NonCompliances)
            {
                A09_Flags_CompanyReviewModel.Module_NonCompliance module_NonCompliance = new A09_Flags_CompanyReviewModel.Module_NonCompliance()
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

                model.Module_NonCompliances.Add(module_NonCompliance);
            }
            model.Module_NonCompliances = model.Module_NonCompliances.OrderByDescending(p => p.DateCreated).ToList();


            if (model.FlagType != null)
            {
                switch ((Data.A09_Flags.A09_Flags_TypeEnum)model.FlagType.ID)
                {
                    case Data.A09_Flags.A09_Flags_TypeEnum.A1_GWandDeviceMonitoring_DeviceOffline:
                        var m2mDev = _client.GetDeviceByMeterNumber(model.Flag.LinkedObjectUniqueID);
                        if (m2mDev != null)
                            model.CustomName = m2mDev.name;
                        break;
                    case Data.A09_Flags.A09_Flags_TypeEnum.A1_GWandDeviceMonitoring_GatewayOffline:
                        var m2mGW = _client.GetGateway(model.Flag.LinkedObjectUniqueID);
                        if (m2mGW != null)
                            model.CustomName = m2mGW.name;
                        break;
                }
            }


            return View("~/Views/Operational/A09_Flags/A09_Flags_CompanyReview.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview_Action/{actionID}/{flagID}")]
        public async Task<IActionResult> A09_Flags_CompanyReview_Action(int actionID, int flagID)
        {
            if (!string.IsNullOrEmpty(Request.Form["userID"].ToString())
                && !string.IsNullOrEmpty(Request.Form["ReportingToUsers"].ToString())
                && !string.IsNullOrEmpty(Request.Form["comments"].ToString())
                && !string.IsNullOrEmpty(Request.Form["dueDate"].ToString())
                )
            {
                DateTime? DueDate = null;
                try
                {
                    DueDate = Convert.ToDateTime(Request.Form["dueDate"]);
                }
                catch { }

                var db = new MyVoltageDbContext(_options);
                var opProfs = db.OperationalProfiles.ToList();

                var status = db.SiteAdmin_Statuses.Where(p => p.ID == actionID).SingleOrDefault();
                var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
                var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
                var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
                var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();

                if (status == null)
                    return Content("false");

                StringBuilder sbSysLog = new StringBuilder();

                var flag = db.A09_Flags.Where(p => p.ID == flagID).SingleOrDefault();

                if (flag.AssignedToID != Request.Form["userID"].ToString())
                {
                    var oldUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                    var newUser = opProfs.Where(p => p.UserID == Request.Form["userID"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ResponsibleUser from {oldUser.FirstName} {oldUser.LastName} to {newUser.FirstName} {newUser.LastName}<br />");
                    flag.AssignedToID = Request.Form["userID"].ToString();
                }

                if (flag.ReportingToUserID != Request.Form["ReportingToUsers"].ToString())
                {
                    var oldUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                    var newUser = opProfs.Where(p => p.UserID == Request.Form["ReportingToUsers"].ToString()).SingleOrDefault();
                    sbSysLog.AppendLine($"ReportingToUsers from {oldUser.FirstName} {oldUser.LastName} to {newUser.FirstName} {newUser.LastName}<br />");
                    flag.ReportingToUserID = Request.Form["ReportingToUsers"].ToString();
                }

                if (flag.DueDate != DueDate)
                {
                    sbSysLog.AppendLine($"DueDate from {flag.DueDate.ToDateShort()} to {DueDate.ToDateShort()}<br />");
                    flag.DueDate = DueDate;
                }

                if (flag.StatusID != (int)status.ID)
                {
                    sbSysLog.AppendLine($"StatusID to {siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}<br />");
                    flag.StatusID = (int)status.ID;
                }

                if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                    flag.ClosedDate = DateTime.Now;

                db.Update(flag);
                db.SaveChanges();

                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
                {
                    Comments = $"{Request.Form["comments"].ToString()}",
                    Created = DateTime.Now,
                    FlagID = flagID,
                    ReassignedByID = _userManager.GetUserId(User),
                    ReassignedToID = Request.Form["userID"].ToString(),
                    ActionID = actionID,
                    ReportingToUserID = Request.Form["ReportingToUsers"].ToString(),
                    SystemDescription = !string.IsNullOrEmpty(sbSysLog.ToString()) ? sbSysLog.ToString() : "Comment Added",
                };

                db.Add(a09_Flags_ReassignLog);
                db.SaveChanges();

                _cache.Remove($"{MVCache.KEY_A09_Flags_ReassignLogs}_{flagID}");
                _cache.Remove($"{MVCache.KEY_A09_Flags}");

                return Content("true");
            }

            return Content("false");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flag_ReviewMeetingAgendaChange/{MeetingAgendaID}")]
        public async Task<IActionResult> A09_Flag_ReviewMeetingAgendaChange(int MeetingAgendaID)
        {
            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
                return Redirect("/operational/A09_Flags/A09_Flags_Type_Summary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var task = db.A09_Flags.Where(p => p.ID == _operationalProvider.FlagSelectedFlagID).SingleOrDefault();

            if (task != null)
            {
                #region Update Task

                if (MeetingAgendaID != task.MeetingAgendaID)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    var itemToUpdate = db.A09_Flags.Where(p => p.ID == task.ID).SingleOrDefault();

                    var siteAdmin_MeetingAgendaes = db.SiteAdmin_MeetingAgendas.ToList();
                    var SiteAdmin_MeetingAgendaGroups = db.SiteAdmin_MeetingAgendaGroups.ToList();
                    var siteAdmin_MeetingAgendaActions = db.SiteAdmin_MeetingAgendaActions.ToList();
                    var siteAdmin_MeetingAgendaReportings = db.SiteAdmin_MeetingAgendaReportings.ToList();
                    string beforeMeetingAgendaString = "";
                    if (task.MeetingAgendaID.HasValue)
                    {
                        var beforeMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == task.MeetingAgendaID).SingleOrDefault();
                        beforeMeetingAgendaString = beforeMeetingAgenda != null ? $"{SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName} - {siteAdmin_MeetingAgendaActions.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == beforeMeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}" : "Unknown";
                    }
                    var afterMeetingAgenda = siteAdmin_MeetingAgendaes.Where(p => p.ID == MeetingAgendaID).SingleOrDefault();
                    string afterMeetingAgendaString = afterMeetingAgenda != null ? $"{SiteAdmin_MeetingAgendaGroups.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaGroupID).SingleOrDefault().MeetingAgendaGroupName} - {siteAdmin_MeetingAgendaActions.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaActionID).SingleOrDefault().MeetingAgendaActionName} - {siteAdmin_MeetingAgendaReportings.Where(p => p.ID == afterMeetingAgenda.MeetingAgendaReportingID).SingleOrDefault().MeetingAgendaReportingName}" : "Unknown";

                    sbSysLog.AppendLine($"MeetingAgenda from '{beforeMeetingAgendaString}' to '{afterMeetingAgendaString}'");
                    itemToUpdate.MeetingAgendaID = MeetingAgendaID;

                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    Data.A09_Flags.A09_Flags_ReassignLog A09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
                    {
                        Created = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        FlagID = task.ID,
                        Comments = "",
                        ReassignedByID = _userManager.GetUserId(User),
                        ReassignedToID = _userManager.GetUserId(User),
                    };

                    db.Add(A09_Flags_ReassignLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_A09_Flags);
                    _cache.Remove(MVCache.KEY_A09_Flags_ReassignLogs);

                }

                #endregion

            }

            return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReviewAddTimeAllocated/{duration}")]
        public async Task<IActionResult> A09_Flags_CompanyReviewAddTimeAllocated(int duration)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
                return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var Flag = db.A09_Flags.Where(p => p.ID == _operationalProvider.FlagSelectedFlagID).SingleOrDefault();

            if (Flag != null)
            {
                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                // Find if open time allocated and close
                var latestOpen = (from p in db.Module_TimeOfWorkAllocateds
                                  where p.ActivityTypeID == (int)ActivityTypeEnum.A09_Flag
                                  && p.ActivityID == Flag.ID
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
                    ActivityID = _operationalProvider.FlagSelectedFlagID,
                    ActivityTypeID = (int)ActivityTypeEnum.A09_Flag,
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

                return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
            }

            return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReviewStartTimeAllocated")]
        public async Task<IActionResult> A09_Flags_CompanyReviewStartTimeAllocated()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
                return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var Flag = db.A09_Flags.Where(p => p.ID == _operationalProvider.FlagSelectedFlagID).SingleOrDefault();

            if (Flag != null)
            {
                var op = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                Module_TimeOfWorkAllocated module_TimeOfWorkAllocated = new Module_TimeOfWorkAllocated()
                {
                    ActivityID = Flag.ID,
                    ActivityTypeID = (int)ActivityTypeEnum.A09_Flag,
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

                return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
            }

            return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReviewStopTimeAllocated/{timeOfWorkAllocatedID}")]
        public async Task<IActionResult> A09_Flags_CompanyReviewStopTimeAllocated(int timeOfWorkAllocatedID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
                return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var timeOfWorkAllocated = db.Module_TimeOfWorkAllocateds.Where(p => p.ID == timeOfWorkAllocatedID).SingleOrDefault();

            if (timeOfWorkAllocated != null)
            {
                timeOfWorkAllocated.EndTime = DateTime.Now;
                db.Update(timeOfWorkAllocated);
                db.SaveChanges();

                return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
            }

            return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_UserSummary")]
        public async Task<IActionResult> A09_Flags_UserSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_UserSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_UserSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A09_Flags_User_SummaryModel model = new A09_Flags_User_SummaryModel()
            {
                A09_Flags_UserSummaryItems = new List<A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                A09_Flags_User_SummaryStatusItems = new List<A09_Flags_User_SummaryModel.A09_Flags_User_SummaryStatusItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var a09_Flags_Types = db.A09_Flags_Types.ToList();

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
                A09_Flags_User_SummaryModel.A09_Flags_User_SummaryStatusItem item = new A09_Flags_User_SummaryModel.A09_Flags_User_SummaryStatusItem()
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
                model.A09_Flags_User_SummaryStatusItems.Add(item);
            }

            var a09_Flags = (from p in db.A09_Flags
                             where p.DueDate.HasValue
                             && p.DueDate.Value.Date >= model.FromDate.Value.Date
                             && p.DueDate.Value.Date <= model.ToDate.Value.Date
                             select p).ToList();

            var uniqueUserIDs = (from p in a09_Flags
                                 select p.AssignedToID).Distinct().ToList();

            var opProfs = db.OperationalProfiles.ToList();
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

                var thisUserFlags = a09_Flags.Where(p => p.AssignedToID == userID).ToList();

                A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem item = new A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem()
                {
                    UserID = userID,
                    UserName = userName,
                    LastTouchedFlag = a09_Flags.Where(p => p.DateLastViewdBy == userID).OrderByDescending(p => p.DateLastViewed).FirstOrDefault(),
                    A09_Flags_User_SummaryItemStatuses = new List<A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem.A09_Flags_User_SummaryItemStatus>(),
                };

                foreach (var status in siteAdmin_Statuses)
                {
                    if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                        continue;
                    A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem.A09_Flags_User_SummaryItemStatus itemStatus = new A09_Flags_User_SummaryModel.A09_Flags_User_SummaryItem.A09_Flags_User_SummaryItemStatus()
                    {
                        Count = thisUserFlags.Where(p => p.StatusID == status.ID).Count(),
                        StatusID = status.ID,
                        StatusGroupID = status.StatusGroupID,
                    };

                    item.A09_Flags_User_SummaryItemStatuses.Add(itemStatus);
                }

                foreach (var flag in thisUserFlags.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                {
                    TimeSpan openDuration = DateTime.Now - flag.DueDate.Value;

                    if (flag.DueDate.Value.Date == DateTime.Now.Date)
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

                var oldestNoCompanyFlag = thisUserFlags.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                if (oldestNoCompanyFlag != null)
                {
                    item.OldestUnresolvedFlagCreateDate = oldestNoCompanyFlag.DueDate.Value;
                    item.OldestUnresolvedFlagID = oldestNoCompanyFlag.ID;
                    item.OldestUnresolvedFlagTypeID = oldestNoCompanyFlag.FlagTypeID;
                }

                model.A09_Flags_UserSummaryItems.Add(item);

            }

            model.A09_Flags_UserSummaryItems = model.A09_Flags_UserSummaryItems.OrderBy(p => p.UserName).ToList();

            return View("~/Views/Operational/A09_Flags/A09_Flags_UserSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_UserDetails")]
        public async Task<IActionResult> A09_Flags_UserDetails()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_UserSummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_UserResults")]
        public async Task<IActionResult> A09_Flags_UserResults()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_UserSummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_TypeSummary")]
        public async Task<IActionResult> A09_Flags_TypeSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_TypeSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_TypeSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A09_Flags_Type_SummaryModel model = new A09_Flags_Type_SummaryModel()
            {
                A09_Flags_TypeSummaryItems = new List<A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-2).Date,
                ToDate = DateTime.Now.Date,
                A09_Flags_Type_SummaryStatusItems = new List<A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryStatusItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var a09_Flags_Types = db.A09_Flags_Types.OrderBy(p => p.FlagTypeName).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"].ToString()))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var a09_Flags = (from p in db.A09_Flags
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
                A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryStatusItem item = new A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryStatusItem()
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
                model.A09_Flags_Type_SummaryStatusItems.Add(item);
            }


            foreach (var fType in a09_Flags_Types)
            {
                var typsTypeFlags = a09_Flags.Where(p => p.FlagTypeID == fType.ID).ToList();

                A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem item = new A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem()
                {
                    TypeID = fType.ID,
                    TypeName = fType.FlagTypeName,
                    LastTouchedFlag = a09_Flags.Where(p => p.FlagTypeID == fType.ID).OrderByDescending(p => p.DateLastViewed).FirstOrDefault(),
                    A09_Flags_Type_SummaryItemStatuses = new List<A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem.A09_Flags_Type_SummaryItemStatus>(),
                };

                foreach (var status in siteAdmin_Statuses)
                {
                    if (status.IsResolvedStatus.HasValue && status.IsResolvedStatus.Value)
                        continue;
                    A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem.A09_Flags_Type_SummaryItemStatus itemStatus = new A09_Flags_Type_SummaryModel.A09_Flags_Type_SummaryItem.A09_Flags_Type_SummaryItemStatus()
                    {
                        Count = typsTypeFlags.Where(p => p.StatusID == status.ID).Count(),
                        StatusID = status.ID,
                        StatusGroupID = status.StatusGroupID,
                    };

                    item.A09_Flags_Type_SummaryItemStatuses.Add(itemStatus);
                }

                foreach (var flag in typsTypeFlags.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                {
                    TimeSpan openDuration = DateTime.Now - flag.DueDate.Value;

                    if (flag.DueDate.Value.Date == DateTime.Now.Date)
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

                var oldestNoCompanyFlag = typsTypeFlags.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                if (oldestNoCompanyFlag != null)
                {
                    item.OldestUnresolvedFlagCreateDate = oldestNoCompanyFlag.DueDate.Value;
                    item.OldestUnresolvedFlagID = oldestNoCompanyFlag.ID;
                    item.OldestUnresolvedFlagTypeID = oldestNoCompanyFlag.FlagTypeID;
                }


                model.A09_Flags_TypeSummaryItems.Add(item);

            }


            return View("~/Views/Operational/A09_Flags/A09_Flags_TypeSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_TypeDetails")]
        public async Task<IActionResult> A09_Flags_TypeDetails()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_TypeSummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_TypeResults")]
        public async Task<IActionResult> A09_Flags_TypeResults()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_TypeSummary");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_Create")]
        public async Task<IActionResult> A09_Flags_Create()
        {
            return Redirect("/operational/A09_Flags/A09_Flags_TypeSummary");
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A09_Flags_CreateModel model = new A09_Flags_CreateModel()
            {
                Company = new List<SelectListItem>(),
                FlagType = new List<SelectListItem>(),
                AssignTo = new List<SelectListItem>(),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            model.Company.Add(new SelectListItem() { Selected = true, Text = "<--Select-->", Value = "" });

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.Company.Add(new SelectListItem() { Text = company.Name, Value = uC.CompanyID.ToString() });
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            foreach (var fType in db.A09_Flags_Types.Where(p => p.Active).OrderBy(p => p.FlagTypeName).ToList())
            {
                model.FlagType.Add(new SelectListItem() { Text = $"{fType.FlagTypeName}", Value = fType.ID.ToString() });
            }

            foreach (var user in operationalUsers)
            {
                model.AssignTo.Add(new SelectListItem() { Text = $"{user.UserName}", Value = user.Id.ToString() });
            }

            return View("~/Views/Operational/A09_Flags/A09_Flags_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A09_Flags/A09_Flags_Create")]
        public async Task<IActionResult> A09_Flags_Create(A09_Flags_CreateModel model)
        {
            return Redirect("/operational/A09_Flags/A09_Flags_TypeSummary");
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion


            model.Company = new List<SelectListItem>();
            model.FlagType = new List<SelectListItem>();
            model.AssignTo = new List<SelectListItem>();

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            model.Company.Add(new SelectListItem() { Selected = string.IsNullOrEmpty(Request.Form["Company"].ToString()) ? true : false, Text = "<--Select-->", Value = "" });

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.Company.Add(new SelectListItem() { Selected = Request.Form["Company"].ToString() == uC.CompanyID.ToString() ? true : false, Text = company.Name, Value = uC.CompanyID.ToString() });
            }

            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            foreach (var fType in db.A09_Flags_Types.Where(p => p.Active).OrderBy(p => p.FlagTypeName).ToList())
            {
                model.FlagType.Add(new SelectListItem() { Selected = Request.Form["FlagType"].ToString() == fType.ID.ToString() ? true : false, Text = $"{fType.FlagTypeName}", Value = fType.ID.ToString() });
            }

            foreach (var user in operationalUsers)
            {
                model.AssignTo.Add(new SelectListItem() { Selected = Request.Form["AssignTo"].ToString() == user.Id.ToString() ? true : false, Text = $"{user.UserName}", Value = user.Id.ToString() });
            }

            if (ModelState.IsValid)
            {
                string linkedObjectDBTableName = "";
                string linkedObjectUniqueID = "";
                var fType = db.A09_Flags_Types.Where(p => p.ID == Convert.ToInt32(Request.Form["FlagType"])).SingleOrDefault();
                int? companyID = null;
                decimal? gpsLat = null;
                decimal? gpsLong = null;

                var skybillCustomer = (from p in db.SkybillCustomers
                                       where p.Customer_No == Request.Form["customerNo"].ToString()
                                       select p).FirstOrDefault();

                if (skybillCustomer != null)
                {
                    if (!string.IsNullOrEmpty(skybillCustomer.GPS_Coordinates))
                    {
                        string[] gps = skybillCustomer.GPS_Coordinates.Split(',');
                        try { gpsLat = Convert.ToDecimal(gps[0]); }
                        catch { }
                        try { gpsLong = Convert.ToDecimal(gps[1]); }
                        catch { }
                    }
                }


                if (!string.IsNullOrEmpty(Request.Form["Company"].ToString()))
                {
                    companyID = Convert.ToInt32(Request.Form["Company"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["gatewayID"].ToString()))
                {
                    linkedObjectDBTableName = "Gateways";
                    linkedObjectUniqueID = Request.Form["gatewayID"].ToString();
                }
                else if (!string.IsNullOrEmpty(Request.Form["DeviceSearch_Serial"].ToString()))
                {
                    linkedObjectDBTableName = "Devices";
                    linkedObjectUniqueID = Request.Form["DeviceSearch_Serial"].ToString();
                }

                Data.A09_Flags.A09_Flag a09_Flag = new Data.A09_Flags.A09_Flag()
                {
                    FlagTypeID = Convert.ToInt32(Request.Form["FlagType"]),
                    LinkedObjectDBTableName = linkedObjectDBTableName,
                    LinkedObjectUniqueID = linkedObjectUniqueID,
                    AssignedToID = Request.Form["AssignTo"].ToString(),
                    AmountInvoiced = null,
                    ClosedDate = null,
                    CompanyID = companyID,
                    Created = DateTime.Now,
                    CustomerNo = Request.Form["customerNo"].ToString(),
                    DateLastViewdBy = "",
                    DateLastViewed = null,
                    DueDate = null,
                    GPSLat = gpsLat,
                    GPSLong = gpsLong,
                    MinRequiredToClear = null,
                    OnceOffFlag = null,
                    PriorityID = fType.PriorityID.HasValue ? fType.PriorityID.Value : 1,
                    ReasonForFlag = model.ReasonForFlag,
                    ReasonForTicket = "",
                    StatusID = fType.DefaultStatusID.HasValue ? fType.DefaultStatusID.Value : 1,
                    StockUsed = "",
                    TravelKmRequired = null,
                    CreatedBy = _userManager.GetUserId(User),
                    Level = null,
                };

                db.Add(a09_Flag);
                db.SaveChanges();

                _cache.Remove($"{MVCache.KEY_A09_Flags}");

                return Redirect($"/operational/A09_Flags/A09_Flags_CompanyReview/{a09_Flag.FlagTypeID}/{a09_Flag.ID}");
            }

            return View("~/Views/Operational/A09_Flags/A09_Flags_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A09_Flags/A09_Flags_Create_Search")]
        public JsonResult A09_Flags_Create_Search(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    orderby p.Customer_No
                                    select p).Take(10).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {
                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/A09_Flags/A09_Flags_Create_Search_Customer_No")]
        public JsonResult A09_Flags_Create_Search_Customer_No(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    orderby p.Customer_No
                                    select p).Take(10).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {
                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Customer_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_Create_Search_Details/{MeterSerial}")]
        public JsonResult A09_Flags_Create_Search_Details(string MeterSerial)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomer = (from p in db.SkybillCustomers
                                   where p.Serial_No == MeterSerial
                                   || p.Customer_No == MeterSerial
                                   orderby p.Customer_No
                                   select p).FirstOrDefault();

            object result = new
            {
                result = false,
                companyID = "",
                customerNumber = "",
                meterSerial = "",
            };

            if (skybillCustomer != null)
            {
                result = new
                {
                    result = true,
                    companyID = skybillCustomer.CompanyID,
                    customerNumber = skybillCustomer.Customer_No,
                    meterSerial = skybillCustomer.Serial_No,
                };
            }

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_Create_Search_Details_GW/{gatewayID}")]
        public JsonResult A09_Flags_Create_Search_Details(int gatewayID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var localGW = (from p in db.Gateways
                           where p.GatewayID == gatewayID
                           select p).FirstOrDefault();

            object result = new
            {
                result = false,
                companyID = "",
            };

            if (localGW != null && localGW.CompanyID.HasValue)
            {
                result = new
                {
                    result = true,
                    companyID = localGW.CompanyID.Value,
                };
            }

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview_AddAttachment")]
        public async Task<IActionResult> A09_Flags_CompanyReview_AddAttachment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
            {
                if (_operationalProvider.CompanyID > 0)
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanyResults");
                else
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");
            }


            A09_Flag_Review_AddAttachmentModel model = new A09_Flag_Review_AddAttachmentModel()
            {

            };

            model.AttachmentType = (from p in ((D01_Leads_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Leads_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                    }).ToList();

            return View("~/Views/Operational/A09_Flags/A09_Flags_CompanyReview_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview_AddAttachment")]
        public async Task<IActionResult> A09_Flags_CompanyReview_AddAttachment(A09_Flag_Review_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.FlagSelectedFlagTypeID == 0 || _operationalProvider.FlagSelectedFlagID == 0)
            {
                if (_operationalProvider.CompanyID > 0)
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanyResults");
                else
                    return Redirect("/operational/A09_Flags/A09_Flags_CompanySummary");
            }

            model.AttachmentType = (from p in ((A08_Tasks_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(A08_Tasks_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"].ToString() == ((int)p).ToString() ? true : false,
                                    }).ToList();

            if (ModelState.IsValid)
            {
                if (model.Attachment != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "a09-flags-attachments";
                    string dirName = $"{_operationalProvider.FlagSelectedFlagID}";
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

                    Data.A09_Flags.A09_Flags_Attachment A09_Flags_Attachment = new Data.A09_Flags.A09_Flags_Attachment()
                    {
                        AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                        DateCreated = DateTime.Now,
                        Filename = fileName,
                        UserID = _userManager.GetUserId(User),
                        Description = model.Description,
                        FlagID = _operationalProvider.FlagSelectedFlagID,
                    };

                    var db = new MyVoltageDbContext(_options);
                    db.Add(A09_Flags_Attachment);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }



            return View("~/Views/Operational/A09_Flags/A09_Flags_CompanyReview_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview_GetAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> A09_Flags_CompanyReview_GetAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var A09_Flags_Attachment = db.A09_Flags_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (A09_Flags_Attachment == null)
                return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");


            string shareName = "a09-flags-attachments";
            string dirName = $"{A09_Flags_Attachment.FlagID}";
            string fileName = A09_Flags_Attachment.Filename;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(A09_Flags_Attachment.Filename));


            return Redirect("/operational/A09_Flags/A09_Flag_Review");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_CompanyReview_DeleteAttachment/{taskAttachmentID}")]
        public async Task<IActionResult> A09_Flags_CompanyReview_DeleteAttachment(int taskAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_CompanyReview, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_CompanyReview}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var A09_Flags_Attachment = db.A09_Flags_Attachments.Where(p => p.ID == taskAttachmentID).SingleOrDefault();

            if (A09_Flags_Attachment == null)
                return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");

            A09_Flags_Attachment.IsDeleted = true;
            db.Update(A09_Flags_Attachment);
            db.SaveChanges();

            return Redirect("/operational/A09_Flags/A09_Flags_CompanyReview");
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_Search")]
        public async Task<IActionResult> A09_Flags_Search()
        {

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var flagTypes = db.A09_Flags_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();

            A09_Flags_SearchModel model = new A09_Flags_SearchModel()
            {
                A09_Flags_TypeDetailsItems = new List<A09_Flags_SearchModel.A09_Flags_Type_DetailsItem>(),
                ReportingToUser = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()),
                    }
                },
                FlagID = !string.IsNullOrEmpty(Request.Query["FlagID"].ToString()) ? Request.Query["FlagID"].ToString() : "",
                Comments = !string.IsNullOrEmpty(Request.Query["Comments"].ToString()) ? Request.Query["Comments"].ToString() : "",
                Description = !string.IsNullOrEmpty(Request.Query["Description"].ToString()) ? Request.Query["Description"].ToString() : "",
                Heading = !string.IsNullOrEmpty(Request.Query["Heading"].ToString()) ? Request.Query["Heading"].ToString() : "",
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()),
                    }
                },
                FlagType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Flag Types--]",
                        Selected = string.IsNullOrEmpty(Request.Query["FlagType"].ToString()),
                    }
                },
                DefaultStatus = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Statuses--]",
                        Selected = string.IsNullOrEmpty(Request.Query["DefaultStatus"].ToString()),
                    }
                },
                StatusGroup = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Status Groups--]",
                        Selected = string.IsNullOrEmpty(Request.Query["StatusGroup"].ToString()),
                    }
                },
                StatusItems = new List<A09_Flags_SearchModel.StatusItem>(),
                Priority = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Task Priorities--]",
                        Selected = string.IsNullOrEmpty(Request.Query["Priority"].ToString()),
                    }
                },
                LinkedTo = !string.IsNullOrEmpty(Request.Query["LinkedTo"].ToString()) ? Request.Query["LinkedTo"].ToString() : "",
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
                                         Selected = Request.Query["Priority"].ToString() == p.ID.ToString() ? true : false,
                                     }).ToList());

            model.FlagType.AddRange((from p in db.A09_Flags_Types
                                     orderby p.FlagTypeName
                                     select new SelectListItem()
                                     {
                                         Selected = !string.IsNullOrEmpty(Request.Query["FlagType"].ToString()) && Request.Query["FlagType"].ToString() == p.ID.ToString(),
                                         Text = $"{p.FlagTypeName}",
                                         Value = p.ID.ToString(),
                                     }).ToList());

            var responsibleUsers = (from p in db.A09_Flags
                                    select p.AssignedToID).Distinct().ToList();

            model.ResponsibleUser.AddRange((from p in opProfs
                                            where responsibleUsers.Contains(p.UserID)
                                            select new SelectListItem()
                                            {
                                                Selected = !string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()) && Request.Query["ResponsibleUser"].ToString() == p.UserID,
                                                Text = $"{p.FirstName} {p.LastName}",
                                                Value = p.UserID,
                                            }).ToList());
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var reportingToUsers = (from p in db.A09_Flags
                                    select p.ReportingToUserID).Distinct().ToList();

            model.ReportingToUser.AddRange((from p in opProfs
                                            where reportingToUsers.Contains(p.UserID)
                                            select new SelectListItem()
                                            {
                                                Selected = !string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()) && Request.Query["ReportingToUser"].ToString() == p.UserID,
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
                                            Selected = Request.Query["StatusGroup"].ToString() == p.ID.ToString(),
                                        }).ToList());

            foreach (var status in siteAdmin_Statuses)
            {
                A09_Flags_SearchModel.StatusItem statusItem = new A09_Flags_SearchModel.StatusItem()
                {
                    DisplayName = $"{siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName} - {siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName}",
                    ID = status.ID,
                    StatusGroupID = status.StatusGroupID,
                };

                model.StatusItems.Add(statusItem);
            }

            List<Data.A09_Flags.A09_Flag> flags = new List<Data.A09_Flags.A09_Flag>();
            List<int> taskIDs = new List<int>();

            SqlCommand sqlCommandLatestComment = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandLatestComment).Fill(tblsp_GetA09_FlagsLatestComment);

            if (!string.IsNullOrEmpty(Request.Query["DueDateFrom"].ToString()))
            {
                model.DueDateFrom = Convert.ToDateTime(Request.Query["DueDateFrom"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DueDateTo"].ToString()))
            {
                model.DueDateTo = Convert.ToDateTime(Request.Query["DueDateTo"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DefaultStatus"].ToString()))
            {
                model.DefaultStatusID = Convert.ToInt32(Request.Query["DefaultStatus"]);
            }
            if (Request.Query.Count > 0)
            {
                SqlConnection connSearch = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandSearch = new SqlCommand("sp_A09_Flags_Search", connSearch);
                sqlCommandSearch.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCommandSearch.Parameters.AddWithValue("@FlagType", !string.IsNullOrEmpty(Request.Query["FlagType"].ToString()) ? Request.Query["FlagType"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Heading", !string.IsNullOrEmpty(Request.Query["Heading"].ToString()) ? Request.Query["Heading"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Description", !string.IsNullOrEmpty(Request.Query["Description"].ToString()) ? Request.Query["Description"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@Comments", !string.IsNullOrEmpty(Request.Query["Comments"].ToString()) ? Request.Query["Comments"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ResponsibleUser", !string.IsNullOrEmpty(Request.Query["ResponsibleUser"].ToString()) ? Request.Query["ResponsibleUser"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ReportingToUser", !string.IsNullOrEmpty(Request.Query["ReportingToUser"].ToString()) ? Request.Query["ReportingToUser"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateFrom", !string.IsNullOrEmpty(Request.Query["DueDateFrom"].ToString()) ? Request.Query["DueDateFrom"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateTo", !string.IsNullOrEmpty(Request.Query["DueDateTo"].ToString()) ? Request.Query["DueDateTo"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DefaultStatusID", !string.IsNullOrEmpty(Request.Query["DefaultStatus"].ToString()) ? Request.Query["DefaultStatus"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@PriorityID", !string.IsNullOrEmpty(Request.Query["Priority"].ToString()) ? Request.Query["Priority"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@FlagID", !string.IsNullOrEmpty(Request.Query["FlagID"].ToString()) ? Request.Query["FlagID"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@LinkedTo", !string.IsNullOrEmpty(Request.Query["LinkedTo"].ToString()) ? Request.Query["LinkedTo"].ToString() : "");
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
                flags = (from p in db.A09_Flags
                         where taskIDs.Contains(p.ID)
                         select p).ToList();

            foreach (var flag in flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var opProf = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                var opProfLastViewedBy = opProfs.Where(p => p.UserID == flag.DateLastViewdBy).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                A09_Flags_SearchModel.A09_Flags_Type_DetailsItem item = new A09_Flags_SearchModel.A09_Flags_Type_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
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
                    A09_Flags_TypeItem = new A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                        ReportingToUserID = fType.ReportingToUserID,
                        MeetingAgendaGroupID = fType.MeetingAgendaGroupID,
                        Identifier = fType.Identifier,
                        DefaultMeetingAgendaID = fType.DefaultMeetingAgendaID,
                        DefaultStatusID = fType.DefaultStatusID,
                        DefautlMinPlanned = fType.DefautlMinPlanned,
                        HasComplianceCheck = fType.HasComplianceCheck,
                        ReportingToUserRequiresCompletedState = fType.ReportingToUserRequiresCompletedState,
                        SecureAreaGroupID = fType.SecureAreaGroupID,
                        StatusGroupID = fType.StatusGroupID,
                    },
                    AssignedToID = flag.AssignedToID,
                    AssignedToUsername = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : "",
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    OnceOffFlag = flag.OnceOffFlag,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    GPSLong = flag.GPSLong,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type_DetailsItemStatus()
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
                    LastViewedBy = opProfLastViewedBy != null ? $"{opProfLastViewedBy.FirstName} {opProfLastViewedBy.LastName}" : "",
                    BusinessDepartmentName = flag.BusinessDepartmentName,
                    BusinessPillarName = flag.BusinessPillarName,
                    CompanyName = flag.CompanyID.HasValue ? companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name : "",
                    Identifier = flag.Identifier,
                    LatestComment = "",
                    ReportingToUserID = flag.ReportingToUserID,
                    ReportingToUserUsername = "",
                    WorkflowGroupName = flag.WorkflowGroupName,
                    WrikeCustomStatus = flag.WrikeCustomStatus,
                    WrikeID = flag.WrikeID,
                    WrikeSyncDate = flag.WrikeSyncDate,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"{latestCommentUserUsername} @ {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}: {latestCommentResults[0]["Comments"]}";
                }

                model.A09_Flags_TypeDetailsItems.Add(item);
            }

            return View("~/Views/Operational/A09_Flags/A09_Flags_Search.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A09_Flags/A09_Flags_Heatmap")]
        public async Task<IActionResult> A09_Flags_Heatmap()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A09_Flags_Heatmap, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A09_Flags_Heatmap}/{(int)SecureAreaActionEnum.View}");

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
            //var responsiblePeople = dbCache.A09_Flags_ResponsiblePersons;
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            var workflowGroupParent = workflowGroupParents.Where(p => p.ID == WorkflowGroupGrandParentID).SingleOrDefault();
            var workflowParentsAllowed = workflowGroupParents.Where(p => p.WorkflowGroupGrandParentID == WorkflowGroupGrandParentID).ToList();
            var workflowGroupsAllowed = workflowGroups.Where(p => p.WorkflowGroupParentID.HasValue && workflowParentsAllowed.Select(c => c.ID).Contains(p.WorkflowGroupParentID.Value)).ToList();
            var businessDepartmentsAllowed = businessDepartments.Where(p => workflowGroupsAllowed.Where(c => c.BusinessDepartmentID.HasValue).Select(c => c.BusinessDepartmentID.Value).Contains(p.ID)).ToList();
            var taskTypes = db.A09_Flags_Types.OrderBy(p => p.Identifier).ToList();
            taskTypes = taskTypes.Where(p => p.SecureAreaGroupID.HasValue && workflowGroupsAllowed.Select(c => c.ID).Contains(p.SecureAreaGroupID.Value)).ToList();

            A09_Flags_HeatmapModel model = new A09_Flags_HeatmapModel()
            {
                A09_Flags_CompanyDetailsItems = new List<A09_Flags_HeatmapModel.A09_Flags_HeatmapItem>(),
                WorkflowGroupGrandParentItems = (from p in WorkflowGroupGrandParents
                                                 select new A09_Flags_HeatmapModel.WorkflowGroupGrandParentItem()
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
                A09_Flags_Types = taskTypes,
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
            List<A09_Flags_Type> A09_Flag_TypesUsed = new List<A09_Flags_Type>();

            var flags = db.A09_Flags.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom).ToList();

            var flagsNoCompany = flags.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom &&
            !p.CompanyID.HasValue).ToList();
            if (flagsNoCompany.Count != 0)
            {
                A09_Flags_HeatmapModel.A09_Flags_HeatmapItem A09_Flags_HeatmapItem = new A09_Flags_HeatmapModel.A09_Flags_HeatmapItem()
                {
                    CompanyID = 0,
                    CompanyName = "All Companies / Not Linked",
                    A09_Flags_HeatmapSubItems = new List<A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem>(),
                };

                foreach (var tType in taskTypes)
                {
                    var tasksForType = flagsNoCompany.Where(p => p.FlagTypeID == tType.ID).ToList();

                    if (tasksForType.Count != 0)
                    {
                        if (!A09_Flag_TypesUsed.Contains(tType))
                            A09_Flag_TypesUsed.Add(tType);

                        A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem A09_Flags_HeatmapSubItem = new A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem()
                        {
                            TaskTypeID = tType.ID,
                            TaskTypeShortName = tType.Identifier.Contains(" ") ? tType.Identifier.Split(" ")[0] : tType.Identifier,
                            UnresolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                            ResolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => p.IsResolvedStatus.HasValue && p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                        };

                        A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItems.Add(A09_Flags_HeatmapSubItem);
                    }
                }

                if (A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItems.Count != 0)
                    model.A09_Flags_CompanyDetailsItems.Add(A09_Flags_HeatmapItem);
            }

            foreach (var c in companies)
            {
                var flagsForCompany = flags.Where(p => p.DueDate.HasValue && p.DueDate.Value <= model.DueDateTo && p.DueDate.Value >= model.DueDateFrom && p.CompanyID.HasValue && p.CompanyID.Value == c.CompanyID).ToList();
                if (flagsForCompany.Count != 0)
                {
                    A09_Flags_HeatmapModel.A09_Flags_HeatmapItem A09_Flags_HeatmapItem = new A09_Flags_HeatmapModel.A09_Flags_HeatmapItem()
                    {
                        CompanyID = c.CompanyID,
                        CompanyName = c.Name,
                        A09_Flags_HeatmapSubItems = new List<A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem>(),
                    };

                    foreach (var tType in taskTypes)
                    {
                        var tasksForType = flagsForCompany.Where(p => p.FlagTypeID == tType.ID).ToList();

                        if (tasksForType.Count != 0)
                        {
                            if (!A09_Flag_TypesUsed.Contains(tType))
                                A09_Flag_TypesUsed.Add(tType);

                            A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem A09_Flags_HeatmapSubItem = new A09_Flags_HeatmapModel.A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItem()
                            {
                                TaskTypeID = tType.ID,
                                TaskTypeShortName = tType.Identifier.Contains(" ") ? tType.Identifier.Split(" ")[0] : tType.Identifier,
                                UnresolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                                ResolvedCount = tasksForType.Where(p => siteAdmin_Statuses.Where(p => p.IsResolvedStatus.HasValue && p.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).Count(),
                            };

                            A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItems.Add(A09_Flags_HeatmapSubItem);
                        }
                    }

                    if (A09_Flags_HeatmapItem.A09_Flags_HeatmapSubItems.Count != 0)
                        model.A09_Flags_CompanyDetailsItems.Add(A09_Flags_HeatmapItem);
                }
            }

            model.A09_Flags_Types = A09_Flag_TypesUsed.OrderBy(p => p.Identifier).ToList();

            return View("~/Views/Operational/A09_Flags/A09_Flags_Heatmap.cshtml", model);
        }
    }
}
