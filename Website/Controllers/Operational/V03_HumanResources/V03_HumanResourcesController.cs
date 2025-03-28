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
using MyVoltage.Models.OperationalModels.V03_HumanResources;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.V03_HumanResources
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class V03_HumanResourcesController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public V03_HumanResourcesController(
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
        [Route("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources")]
        public async Task<IActionResult> V02_Workflow_Allocations_AllTaskAllocations()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V02_Workflow_Allocations_AllTaskAllocations, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/V03_HumanResources/V03_HumanResources_PersonalDetails");

            #endregion


            V03_HumanResources_AllHumanResourcesModel model = new V03_HumanResources_AllHumanResourcesModel()
            {
                V03_HumanResources_AllHumanResourcesItems = new List<V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var v03_PersonalDetails = db.V03_PersonalDetails.ToList();
            var v03_JobDescriptions = db.V03_JobDescriptions.ToList();
            var v03_Correspondences = db.V03_Correspondences.ToList();

            foreach (var pD in v03_PersonalDetails)
            {
                var op = opProfs.Where(p => p.UserID == pD.OperationalUserID).SingleOrDefault();
                V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem item = new V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem()
                {
                    BankAccountName = pD.BankAccountName,
                    BankAccountNo = pD.BankAccountNo,
                    BankBranchName = pD.BankBranchName,
                    BankName = pD.BankName,
                    CompanyID = pD.CompanyID,
                    ContactNo = pD.ContactNo,
                    DateOfBirth = pD.DateOfBirth,
                    EmergencyFullName = pD.EmergencyFullName,
                    EmergencyPrimaryContactNo = pD.EmergencyPrimaryContactNo,
                    EmergencyRelationship = pD.EmergencyRelationship,
                    EmergencyResidentialAddress = pD.EmergencyResidentialAddress,
                    EmployeeEmail = pD.EmployeeEmail,
                    EmployeeNo = pD.EmployeeNo,
                    EndDate = pD.EndDate,
                    FullName = pD.FullName,
                    ID = pD.ID,
                    IDNumber = pD.IDNumber,
                    IncomeTaxNo = pD.IncomeTaxNo,
                    MaritialStatus = pD.MaritialStatus,
                    OperationalUserID = pD.OperationalUserID,
                    PersonalEmail = pD.PersonalEmail,
                    ReportingToUserID = pD.ReportingToUserID,
                    ResidentialAddress = pD.ResidentialAddress,
                    SpouseContactNo = pD.SpouseContactNo,
                    SpouseEmployer = pD.SpouseEmployer,
                    SpouseName = pD.SpouseName,
                    StartDate = pD.StartDate,
                    OperationalProfile = op,
                    OperationalUser = $"{op.FirstName} {op.LastName}",
                    CompanyName = "",
                    ReportingToUser = "",
                    ReportingToUserAccepted = pD.ReportingToUserAccepted,
                    ReportingToUserAcceptedDate = pD.ReportingToUserAcceptedDate,
                    ResponsibleUserAccepted = pD.ResponsibleUserAccepted,
                    ResponsibleUserAcceptedDate = pD.ResponsibleUserAcceptedDate,
                    V03_CorrespondencesCount = v03_Correspondences.Where(p => !p.IsDeleted && p.PersonalDetailsID == pD.ID).Count(),
                };


                var v03_JobDescription = (from p in v03_JobDescriptions
                                          where p.PersonalDetailsID == pD.ID
                                          select p).SingleOrDefault();

                item.V03_JobDescription = v03_JobDescription;

                var opRep = opProfs.Where(p => p.UserID == pD.ReportingToUserID).SingleOrDefault();
                item.ReportingToUser = $"{opRep.FirstName} {opRep.LastName}";


                model.V03_HumanResources_AllHumanResourcesItems.Add(item);
            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_AllHumanResources.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails_Add")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_PersonalDetails, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_PersonalDetails}/{(int)SecureAreaActionEnum.Add}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
            var alreadyUsedUsers = (from p in db.V03_PersonalDetails
                                    select p.OperationalUserID).Distinct().ToList();

            V03_HumanResources_PersonalDetailsModel model = new V03_HumanResources_PersonalDetailsModel()
            {
                CompanyID = (from p in companies
                             select new SelectListItem()
                             {
                                 Text = $"{p.Name}",
                                 Value = $"{p.CompanyID}",
                             }).ToList(),
                OperationalUserID = (from p in opProfs
                                     where !alreadyUsedUsers.Contains(p.UserID)
                                     select new SelectListItem()
                                     {
                                         Text = $"{p.FirstName} {p.LastName}",
                                         Value = $"{p.UserID}",
                                     }).ToList(),
                ReportingToUserID = (from p in opProfs
                                     select new SelectListItem()
                                     {
                                         Text = $"{p.FirstName} {p.LastName}",
                                         Value = $"{p.UserID}",
                                     }).ToList(),
            };
            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();
            model.OperationalUserID = model.OperationalUserID.OrderBy(p => p.Text).ToList();
            model.ReportingToUserID = model.ReportingToUserID.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_PersonalDetails_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails_Add")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails_Add(V03_HumanResources_PersonalDetailsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_PersonalDetails, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_PersonalDetails}/{(int)SecureAreaActionEnum.Add}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
            var alreadyUsedUsers = (from p in db.V03_PersonalDetails
                                    select p.OperationalUserID).Distinct().ToList();

            model.CompanyID = (from p in companies
                               select new SelectListItem()
                               {
                                   Text = $"{p.Name}",
                                   Value = $"{p.CompanyID}",
                                   Selected = Request.Form["CompanyID"].ToString() == p.CompanyID.ToString(),
                               }).ToList();

            model.OperationalUserID = (from p in opProfs
                                       where !alreadyUsedUsers.Contains(p.UserID)
                                       select new SelectListItem()
                                       {
                                           Text = $"{p.FirstName} {p.LastName}",
                                           Value = $"{p.UserID}",
                                           Selected = Request.Form["OperationalUserID"].ToString() == p.UserID,
                                       }).ToList();

            model.ReportingToUserID = (from p in opProfs
                                       select new SelectListItem()
                                       {
                                           Text = $"{p.FirstName} {p.LastName}",
                                           Value = $"{p.UserID}",
                                           Selected = Request.Form["ReportingToUserID"].ToString() == p.UserID,
                                       }).ToList();

            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();
            model.OperationalUserID = model.OperationalUserID.OrderBy(p => p.Text).ToList();
            model.ReportingToUserID = model.ReportingToUserID.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                Data.V03_PersonalDetail v03_PersonalDetail = new V03_PersonalDetail()
                {
                    BankAccountName = model.BankAccountName,
                    BankAccountNo = model.BankAccountNo,
                    BankBranchName = model.BankBranchName,
                    BankName = model.BankName,
                    CompanyID = Convert.ToInt32(Request.Form["CompanyID"]),
                    ContactNo = model.ContactNo,
                    DateOfBirth = model.DateOfBirth.Value,
                    EmergencyFullName = model.EmergencyFullName,
                    EmergencyPrimaryContactNo = model.EmergencyPrimaryContactNo,
                    EmergencyRelationship = model.EmergencyRelationship,
                    EmergencyResidentialAddress = model.EmergencyResidentialAddress,
                    EmployeeEmail = model.EmployeeEmail,
                    EmployeeNo = model.EmployeeNo,
                    EndDate = model.EndDate,
                    FullName = model.FullName,
                    IDNumber = model.IDNumber,
                    IncomeTaxNo = model.IncomeTaxNo,
                    MaritialStatus = model.MaritialStatus,
                    OperationalUserID = Request.Form["OperationalUserID"].ToString(),
                    PersonalEmail = model.PersonalEmail,
                    ReportingToUserID = Request.Form["ReportingToUserID"].ToString(),
                    ResidentialAddress = model.ResidentialAddress,
                    SpouseContactNo = model.SpouseContactNo,
                    SpouseEmployer = model.SpouseEmployer,
                    SpouseName = model.SpouseName,
                    StartDate = model.StartDate.Value,
                };

                db.Add(v03_PersonalDetail);
                db.SaveChanges();

                Data.V03_PersonalDetails_Log v03_PersonalDetails_Log = new V03_PersonalDetails_Log()
                {
                    DateCreated = DateTime.Now,
                    PersonalDetailsID = v03_PersonalDetail.ID,
                    SystemDescription = "Created",
                    UserID = _userManager.GetUserId(User),
                };

                db.Add(v03_PersonalDetails_Log);
                db.SaveChanges();

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_PersonalDetails_Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails()
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.OperationalUserID == _userManager.GetUserId(User)
                                      select p).SingleOrDefault();
            if (v03_PersonalDetail != null)
                return Redirect($"/operational/V03_HumanResources/V03_HumanResources_PersonalDetails/{v03_PersonalDetail.ID}");

            return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails/{ID}")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_PersonalDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_PersonalDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();

            V03_HumanResources_PersonalDetailsModel model = new V03_HumanResources_PersonalDetailsModel()
            {
                CompanyID = (from p in companies
                             select new SelectListItem()
                             {
                                 Text = $"{p.Name}",
                                 Value = $"{p.CompanyID}",
                                 Selected = v03_PersonalDetail.CompanyID.HasValue && v03_PersonalDetail.CompanyID.Value == p.CompanyID,
                             }).ToList(),
                OperationalUserID = (from p in opProfs
                                     select new SelectListItem()
                                     {
                                         Text = $"{p.FirstName} {p.LastName}",
                                         Value = $"{p.UserID}",
                                         Selected = v03_PersonalDetail.OperationalUserID == p.UserID,
                                     }).ToList(),
                ReportingToUserID = (from p in opProfs
                                     select new SelectListItem()
                                     {
                                         Text = $"{p.FirstName} {p.LastName}",
                                         Value = $"{p.UserID}",
                                         Selected = v03_PersonalDetail.ReportingToUserID == p.UserID,
                                     }).ToList(),
                BankAccountName = v03_PersonalDetail.BankAccountName,
                BankAccountNo = v03_PersonalDetail.BankAccountNo,
                BankBranchName = v03_PersonalDetail.BankBranchName,
                BankName = v03_PersonalDetail.BankName,
                ContactNo = v03_PersonalDetail.ContactNo,
                DateOfBirth = v03_PersonalDetail.DateOfBirth,
                EmergencyFullName = v03_PersonalDetail.EmergencyFullName,
                EmergencyPrimaryContactNo = v03_PersonalDetail.EmergencyPrimaryContactNo,
                EmergencyRelationship = v03_PersonalDetail.EmergencyRelationship,
                EmergencyResidentialAddress = v03_PersonalDetail.EmergencyResidentialAddress,
                EmployeeEmail = v03_PersonalDetail.EmployeeEmail,
                EmployeeNo = v03_PersonalDetail.EmployeeNo,
                EndDate = v03_PersonalDetail.EndDate,
                FullName = v03_PersonalDetail.FullName,
                ID = v03_PersonalDetail.ID,
                IDNumber = v03_PersonalDetail.IDNumber,
                IncomeTaxNo = v03_PersonalDetail.IncomeTaxNo,
                MaritialStatus = v03_PersonalDetail.MaritialStatus,
                PersonalEmail = v03_PersonalDetail.PersonalEmail,
                ResidentialAddress = v03_PersonalDetail.ResidentialAddress,
                SpouseContactNo = v03_PersonalDetail.SpouseContactNo,
                SpouseEmployer = v03_PersonalDetail.SpouseEmployer,
                SpouseName = v03_PersonalDetail.SpouseName,
                StartDate = v03_PersonalDetail.StartDate,
                ReportingToUserAccepted = v03_PersonalDetail.ReportingToUserAccepted,
                ReportingToUserAcceptedDate = v03_PersonalDetail.ReportingToUserAcceptedDate,
                ResponsibleUserAccepted = v03_PersonalDetail.ResponsibleUserAccepted,
                ResponsibleUserAcceptedDate = v03_PersonalDetail.ResponsibleUserAcceptedDate,
            };
            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();
            model.OperationalUserID = model.OperationalUserID.OrderBy(p => p.Text).ToList();
            model.ReportingToUserID = model.ReportingToUserID.OrderBy(p => p.Text).ToList();

            model.V03_PersonalDetails_LogItems = new List<V03_HumanResources_PersonalDetailsModel.V03_PersonalDetails_LogItem>();
            var v03_PersonalDetails_Logs = db.V03_PersonalDetails_Logs.Where(p => p.PersonalDetailsID == v03_PersonalDetail.ID).ToList();
            foreach (var log in v03_PersonalDetails_Logs.OrderByDescending(p => p.DateCreated))
            {
                var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                model.V03_PersonalDetails_LogItems.Add(new V03_HumanResources_PersonalDetailsModel.V03_PersonalDetails_LogItem()
                {
                    DateCreated = log.DateCreated,
                    UserID = log.UserID,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    PersonalDetailsID = log.PersonalDetailsID,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                });
            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_PersonalDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails/{ID}")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails(int ID, V03_HumanResources_PersonalDetailsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_PersonalDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_PersonalDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");


            model.CompanyID = (from p in companies
                               select new SelectListItem()
                               {
                                   Text = $"{p.Name}",
                                   Value = $"{p.CompanyID}",
                                   Selected = Request.Form["CompanyID"].ToString() == p.CompanyID.ToString(),
                               }).ToList();

            model.OperationalUserID = (from p in opProfs
                                       select new SelectListItem()
                                       {
                                           Text = $"{p.FirstName} {p.LastName}",
                                           Value = $"{p.UserID}",
                                           Selected = Request.Form["OperationalUserID"].ToString() == p.UserID,
                                       }).ToList();

            model.ReportingToUserID = (from p in opProfs
                                       select new SelectListItem()
                                       {
                                           Text = $"{p.FirstName} {p.LastName}",
                                           Value = $"{p.UserID}",
                                           Selected = Request.Form["ReportingToUserID"].ToString() == p.UserID,
                                       }).ToList();

            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();
            model.OperationalUserID = model.OperationalUserID.OrderBy(p => p.Text).ToList();
            model.ReportingToUserID = model.ReportingToUserID.OrderBy(p => p.Text).ToList();
            model.ReportingToUserAccepted = v03_PersonalDetail.ReportingToUserAccepted;
            model.ReportingToUserAcceptedDate = v03_PersonalDetail.ReportingToUserAcceptedDate;
            model.ResponsibleUserAccepted = v03_PersonalDetail.ResponsibleUserAccepted;
            model.ResponsibleUserAcceptedDate = v03_PersonalDetail.ResponsibleUserAcceptedDate;

            model.V03_PersonalDetails_LogItems = new List<V03_HumanResources_PersonalDetailsModel.V03_PersonalDetails_LogItem>();

            var v03_PersonalDetails_Logs = db.V03_PersonalDetails_Logs.Where(p => p.PersonalDetailsID == v03_PersonalDetail.ID).ToList();
            foreach (var log in v03_PersonalDetails_Logs.OrderByDescending(p => p.DateCreated))
            {
                var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                model.V03_PersonalDetails_LogItems.Add(new V03_HumanResources_PersonalDetailsModel.V03_PersonalDetails_LogItem()
                {
                    DateCreated = log.DateCreated,
                    UserID = log.UserID,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    PersonalDetailsID = log.PersonalDetailsID,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                });
            }

            if (ModelState.IsValid)
            {
                StringBuilder sbSysLog = new StringBuilder();
                if (v03_PersonalDetail.BankAccountName != model.BankAccountName)
                {
                    sbSysLog.AppendLine($"BankAccountName from '{v03_PersonalDetail.BankAccountName}' to '{model.BankAccountName}'<br />");
                    v03_PersonalDetail.BankAccountName = model.BankAccountName;
                }
                if (v03_PersonalDetail.BankAccountNo != model.BankAccountNo)
                {
                    sbSysLog.AppendLine($"BankAccountNo from '{v03_PersonalDetail.BankAccountNo}' to '{model.BankAccountNo}'<br />");
                    v03_PersonalDetail.BankAccountNo = model.BankAccountNo;
                }
                if (v03_PersonalDetail.BankBranchName != model.BankBranchName)
                {
                    sbSysLog.AppendLine($"BankBranchName from '{v03_PersonalDetail.BankBranchName}' to '{model.BankBranchName}'<br />");
                    v03_PersonalDetail.BankBranchName = model.BankBranchName;
                }
                if (v03_PersonalDetail.BankName != model.BankName)
                {
                    sbSysLog.AppendLine($"BankName from '{v03_PersonalDetail.BankName}' to '{model.BankName}'<br />");
                    v03_PersonalDetail.BankName = model.BankName;
                }
                if (v03_PersonalDetail.CompanyID != Convert.ToInt32(Request.Form["CompanyID"]))
                {
                    string oldCompanyName = v03_PersonalDetail.CompanyID.ToString();
                    if (v03_PersonalDetail.CompanyID.HasValue)
                        oldCompanyName = companies.Where(p => p.CompanyID == v03_PersonalDetail.CompanyID.Value).SingleOrDefault().Name;
                    string newCompanyName = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Form["CompanyID"])).SingleOrDefault().Name;

                    sbSysLog.AppendLine($"Company from '{oldCompanyName}' to '{newCompanyName}'<br />");
                    v03_PersonalDetail.CompanyID = Convert.ToInt32(Request.Form["CompanyID"]);
                }
                if (v03_PersonalDetail.ContactNo != model.ContactNo)
                {
                    sbSysLog.AppendLine($"ContactNo from '{v03_PersonalDetail.ContactNo}' to '{model.ContactNo}'<br />");
                    v03_PersonalDetail.ContactNo = model.ContactNo;
                }
                if (v03_PersonalDetail.DateOfBirth != model.DateOfBirth.Value)
                {
                    sbSysLog.AppendLine($"DateOfBirth from '{v03_PersonalDetail.DateOfBirth.ToDateShort()}' to '{model.DateOfBirth.ToDateShort()}'<br />");
                    v03_PersonalDetail.DateOfBirth = model.DateOfBirth.Value;
                }
                if (v03_PersonalDetail.EmergencyFullName != model.EmergencyFullName)
                {
                    sbSysLog.AppendLine($"EmergencyFullName from '{v03_PersonalDetail.EmergencyFullName}' to '{model.EmergencyFullName}'<br />");
                    v03_PersonalDetail.EmergencyFullName = model.EmergencyFullName;
                }
                if (v03_PersonalDetail.EmergencyPrimaryContactNo != model.EmergencyPrimaryContactNo)
                {
                    sbSysLog.AppendLine($"EmergencyPrimaryContactNo from '{v03_PersonalDetail.EmergencyPrimaryContactNo}' to '{model.EmergencyPrimaryContactNo}'<br />");
                    v03_PersonalDetail.EmergencyPrimaryContactNo = model.EmergencyPrimaryContactNo;
                }
                if (v03_PersonalDetail.EmergencyRelationship != model.EmergencyRelationship)
                {
                    sbSysLog.AppendLine($"EmergencyRelationship from '{v03_PersonalDetail.EmergencyRelationship}' to '{model.EmergencyRelationship}'<br />");
                    v03_PersonalDetail.EmergencyRelationship = model.EmergencyRelationship;
                }
                if (v03_PersonalDetail.EmergencyResidentialAddress != model.EmergencyResidentialAddress)
                {
                    sbSysLog.AppendLine($"EmergencyResidentialAddress from '{v03_PersonalDetail.EmergencyResidentialAddress}' to '{model.EmergencyResidentialAddress}'<br />");
                    v03_PersonalDetail.EmergencyResidentialAddress = model.EmergencyResidentialAddress;
                }
                if (v03_PersonalDetail.EmployeeEmail != model.EmployeeEmail)
                {
                    sbSysLog.AppendLine($"EmployeeEmail from '{v03_PersonalDetail.EmployeeEmail}' to '{model.EmployeeEmail}'<br />");
                    v03_PersonalDetail.EmployeeEmail = model.EmployeeEmail;
                }
                if (v03_PersonalDetail.EmployeeNo != model.EmployeeNo)
                {
                    sbSysLog.AppendLine($"EmployeeNo from '{v03_PersonalDetail.EmployeeNo}' to '{model.EmployeeNo}'<br />");
                    v03_PersonalDetail.EmployeeNo = model.EmployeeNo;
                }
                if (v03_PersonalDetail.EndDate != model.EndDate)
                {
                    sbSysLog.AppendLine($"EndDate from '{v03_PersonalDetail.EndDate.ToDateShort(true)}' to '{model.EndDate.ToDateShort(true)}'<br />");
                    v03_PersonalDetail.EndDate = model.EndDate;
                }
                if (v03_PersonalDetail.FullName != model.FullName)
                {
                    sbSysLog.AppendLine($"FullName from '{v03_PersonalDetail.FullName}' to '{model.FullName}'<br />");
                    v03_PersonalDetail.FullName = model.FullName;
                }
                if (v03_PersonalDetail.IDNumber != model.IDNumber)
                {
                    sbSysLog.AppendLine($"IDNumber from '{v03_PersonalDetail.IDNumber}' to '{model.IDNumber}'<br />");
                    v03_PersonalDetail.IDNumber = model.IDNumber;
                }
                if (v03_PersonalDetail.IncomeTaxNo != model.IncomeTaxNo)
                {
                    sbSysLog.AppendLine($"IncomeTaxNo from '{v03_PersonalDetail.IncomeTaxNo}' to '{model.IncomeTaxNo}'<br />");
                    v03_PersonalDetail.IncomeTaxNo = model.IncomeTaxNo;
                }
                if (v03_PersonalDetail.MaritialStatus != model.MaritialStatus)
                {
                    sbSysLog.AppendLine($"MaritialStatus from '{v03_PersonalDetail.MaritialStatus}' to '{model.MaritialStatus}'<br />");
                    v03_PersonalDetail.MaritialStatus = model.MaritialStatus;
                }
                if (v03_PersonalDetail.OperationalUserID != Request.Form["OperationalUserID"].ToString())
                {
                    var oldOp = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
                    string oldOpName = oldOp != null ? $"{oldOp.FirstName} {oldOp.LastName}" : "";
                    var newOp = opProfs.Where(p => p.UserID == Request.Form["OperationalUserID"].ToString()).SingleOrDefault();
                    string newOpName = newOp != null ? $"{newOp.FirstName} {newOp.LastName}" : "";
                    sbSysLog.AppendLine($"Operational User from '{oldOpName}' to '{newOpName}'<br />");
                    v03_PersonalDetail.OperationalUserID = Request.Form["OperationalUserID"].ToString();
                }
                if (v03_PersonalDetail.PersonalEmail != model.PersonalEmail)
                {
                    sbSysLog.AppendLine($"PersonalEmail from '{v03_PersonalDetail.PersonalEmail}' to '{model.PersonalEmail}'<br />");
                    v03_PersonalDetail.PersonalEmail = model.PersonalEmail;
                }
                if (v03_PersonalDetail.ReportingToUserID != Request.Form["ReportingToUserID"].ToString())
                {
                    var oldOp = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
                    string oldOpName = oldOp != null ? $"{oldOp.FirstName} {oldOp.LastName}" : "";
                    var newOp = opProfs.Where(p => p.UserID == Request.Form["ReportingToUserID"].ToString()).SingleOrDefault();
                    string newOpName = newOp != null ? $"{newOp.FirstName} {newOp.LastName}" : "";
                    sbSysLog.AppendLine($"Reporting To User User from '{oldOpName}' to '{newOpName}'<br />");
                    v03_PersonalDetail.ReportingToUserID = Request.Form["ReportingToUserID"].ToString();
                }
                if (v03_PersonalDetail.ResidentialAddress != model.ResidentialAddress)
                {
                    sbSysLog.AppendLine($"ResidentialAddress from '{v03_PersonalDetail.ResidentialAddress}' to '{model.ResidentialAddress}'<br />");
                    v03_PersonalDetail.ResidentialAddress = model.ResidentialAddress;
                }
                if (v03_PersonalDetail.SpouseContactNo != model.SpouseContactNo)
                {
                    sbSysLog.AppendLine($"SpouseContactNo from '{v03_PersonalDetail.SpouseContactNo}' to '{model.SpouseContactNo}'<br />");
                    v03_PersonalDetail.SpouseContactNo = model.SpouseContactNo;
                }
                if (v03_PersonalDetail.SpouseEmployer != model.SpouseEmployer)
                {
                    sbSysLog.AppendLine($"SpouseEmployer from '{v03_PersonalDetail.SpouseEmployer}' to '{model.SpouseEmployer}'<br />");
                    v03_PersonalDetail.SpouseEmployer = model.SpouseEmployer;
                }
                if (v03_PersonalDetail.SpouseName != model.SpouseName)
                {
                    sbSysLog.AppendLine($"SpouseName from '{v03_PersonalDetail.SpouseName}' to '{model.SpouseName}'<br />");
                    v03_PersonalDetail.SpouseName = model.SpouseName;
                }
                if (v03_PersonalDetail.StartDate != model.StartDate.Value)
                {
                    sbSysLog.AppendLine($"StartDate from '{v03_PersonalDetail.StartDate.ToDateShort()}' to '{model.StartDate.ToDateShort()}'<br />");
                    v03_PersonalDetail.StartDate = model.StartDate.Value;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    #region Reset Acceptance 

                    if (v03_PersonalDetail.ReportingToUserAccepted.HasValue)
                    {
                        sbSysLog.AppendLine($"Reporting To User Acceptance removed.<br />");
                        v03_PersonalDetail.ReportingToUserAccepted = null;
                        v03_PersonalDetail.ReportingToUserAcceptedDate = null;
                    }

                    if (v03_PersonalDetail.ResponsibleUserAccepted.HasValue)
                    {
                        sbSysLog.AppendLine($"Responsible User Acceptance removed.<br />");
                        v03_PersonalDetail.ResponsibleUserAccepted = null;
                        v03_PersonalDetail.ResponsibleUserAcceptedDate = null;
                    }

                    #endregion



                    db.Update(v03_PersonalDetail);
                    db.SaveChanges();

                    Data.V03_PersonalDetails_Log v03_PersonalDetails_Log = new V03_PersonalDetails_Log()
                    {
                        DateCreated = DateTime.Now,
                        PersonalDetailsID = v03_PersonalDetail.ID,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(v03_PersonalDetails_Log);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }

            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_PersonalDetails.cshtml", model);
        }



        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails_ResponsibleUserAccept/{ID}")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails_ResponsibleUserAccept(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");


            if (v03_PersonalDetail.OperationalUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                v03_PersonalDetail.ResponsibleUserAccepted = true;
                v03_PersonalDetail.ResponsibleUserAcceptedDate = DateTime.Now;
                db.Update(v03_PersonalDetail);
                db.SaveChanges();

                var opResponsibleUser = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
                Data.V03_PersonalDetails_Log log = new V03_PersonalDetails_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName} Responsible User Accept",
                    UserID = _userManager.GetUserId(User),
                    PersonalDetailsID = v03_PersonalDetail.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect($"/operational/V03_HumanResources/V03_HumanResources_PersonalDetails/{ID}");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_PersonalDetails_ReportingToUserAccept/{ID}")]
        public async Task<IActionResult> V03_HumanResources_PersonalDetails_ReportingToUserAccept(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");


            if (v03_PersonalDetail.ReportingToUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                v03_PersonalDetail.ReportingToUserAccepted = true;
                v03_PersonalDetail.ReportingToUserAcceptedDate = DateTime.Now;
                db.Update(v03_PersonalDetail);
                db.SaveChanges();

                var opReportingTo = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
                Data.V03_PersonalDetails_Log log = new V03_PersonalDetails_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opReportingTo.FirstName} {opReportingTo.LastName} Reporting To User Accept",
                    UserID = _userManager.GetUserId(User),
                    PersonalDetailsID = v03_PersonalDetail.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }


            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect($"/operational/V03_HumanResources/V03_HumanResources_PersonalDetails/{ID}");
        }


        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_JobDescriptions")]
        public async Task<IActionResult> V03_HumanResources_JobDescriptions()
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.OperationalUserID == _userManager.GetUserId(User)
                                      select p).SingleOrDefault();
            if (v03_PersonalDetail != null)
                return Redirect($"/operational/V03_HumanResources/V03_HumanResources_JobDescriptions/{v03_PersonalDetail.ID}");

            return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_JobDescriptions/{ID}")]
        public async Task<IActionResult> V03_HumanResources_JobDescriptions(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_JobDescriptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_JobDescriptions}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();
            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();

            var op = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
            V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem item = new V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem()
            {
                BankAccountName = v03_PersonalDetail.BankAccountName,
                BankAccountNo = v03_PersonalDetail.BankAccountNo,
                BankBranchName = v03_PersonalDetail.BankBranchName,
                BankName = v03_PersonalDetail.BankName,
                CompanyID = v03_PersonalDetail.CompanyID,
                ContactNo = v03_PersonalDetail.ContactNo,
                DateOfBirth = v03_PersonalDetail.DateOfBirth,
                EmergencyFullName = v03_PersonalDetail.EmergencyFullName,
                EmergencyPrimaryContactNo = v03_PersonalDetail.EmergencyPrimaryContactNo,
                EmergencyRelationship = v03_PersonalDetail.EmergencyRelationship,
                EmergencyResidentialAddress = v03_PersonalDetail.EmergencyResidentialAddress,
                EmployeeEmail = v03_PersonalDetail.EmployeeEmail,
                EmployeeNo = v03_PersonalDetail.EmployeeNo,
                EndDate = v03_PersonalDetail.EndDate,
                FullName = v03_PersonalDetail.FullName,
                ID = v03_PersonalDetail.ID,
                IDNumber = v03_PersonalDetail.IDNumber,
                IncomeTaxNo = v03_PersonalDetail.IncomeTaxNo,
                MaritialStatus = v03_PersonalDetail.MaritialStatus,
                OperationalUserID = v03_PersonalDetail.OperationalUserID,
                PersonalEmail = v03_PersonalDetail.PersonalEmail,
                ReportingToUserID = v03_PersonalDetail.ReportingToUserID,
                ResidentialAddress = v03_PersonalDetail.ResidentialAddress,
                SpouseContactNo = v03_PersonalDetail.SpouseContactNo,
                SpouseEmployer = v03_PersonalDetail.SpouseEmployer,
                SpouseName = v03_PersonalDetail.SpouseName,
                StartDate = v03_PersonalDetail.StartDate,
                OperationalProfile = op,
                OperationalUser = $"{op.FirstName} {op.LastName}",
            };

            if (v03_PersonalDetail.CompanyID.HasValue)
                item.CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == v03_PersonalDetail.CompanyID.Value).SingleOrDefault().Name;

            var opRep = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
            item.ReportingToUser = $"{opRep.FirstName} {opRep.LastName}";

            V03_HumanResources_JobDescriptionsModel model = new V03_HumanResources_JobDescriptionsModel()
            {
                V03_HumanResources_AllHumanResourcesItem = item,
                V02_Workflow_Allocations_AllTaskAllocationsItems = new List<Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem>(),
                V03_JobDescriptions_LogItems = new List<V03_HumanResources_JobDescriptionsModel.V03_JobDescriptions_LogItem>(),
            };

            var v03_JobDescription = (from p in db.V03_JobDescriptions
                                      where p.PersonalDetailsID == v03_PersonalDetail.ID
                                      select p).SingleOrDefault();

            if (v03_JobDescription != null)
            {
                model.DutiesAndResponsibilities_Overall = v03_JobDescription.DutiesAndResponsibilities_Overall;
                model.DutiesAndResponsibilities_Primary = v03_JobDescription.DutiesAndResponsibilities_Primary;
                model.DutiesAndResponsibilities_Secondary = v03_JobDescription.DutiesAndResponsibilities_Secondary;
                model.V03_JobDescriptions_LogItems = new List<V03_HumanResources_JobDescriptionsModel.V03_JobDescriptions_LogItem>();
                model.ReportingToUserAccepted = v03_JobDescription.ReportingToUserAccepted;
                model.ReportingToUserAcceptedDate = v03_JobDescription.ReportingToUserAcceptedDate;
                model.ResponsibleUserAccepted = v03_JobDescription.ResponsibleUserAccepted;
                model.ResponsibleUserAcceptedDate = v03_JobDescription.ResponsibleUserAcceptedDate;
                var v03_PersonalDetails_Logs = db.V03_JobDescriptions_Logs.Where(p => p.JobDescriptionID == v03_JobDescription.ID).ToList();
                foreach (var log in v03_PersonalDetails_Logs.OrderByDescending(p => p.DateCreated))
                {
                    var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                    model.V03_JobDescriptions_LogItems.Add(new V03_HumanResources_JobDescriptionsModel.V03_JobDescriptions_LogItem()
                    {
                        DateCreated = log.DateCreated,
                        UserID = log.UserID,
                        ID = log.ID,
                        SystemDescription = log.SystemDescription,
                        JobDescriptionID = log.JobDescriptionID,
                        Username = $"{opProf.FirstName} {opProf.LastName}",
                    });
                }

            }

            var a08_Task_Types = db.A08_Task_Types.Where(p => p.ResponsibleUserID == v03_PersonalDetail.OperationalUserID || p.ReportingToUserID == v03_PersonalDetail.OperationalUserID).ToList();

            foreach (var tType in a08_Task_Types)
            {
                Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem v02_Workflow_Allocations_AllTaskAllocationsItem = new Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem()
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
                    SecureAreaName = tType.ResponsibleUserID,
                    StatusGroupID = tType.StatusGroupID,
                    TemplateNo = tType.TemplateNo,
                };

                var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
                if (opReportingTo != null)
                    v02_Workflow_Allocations_AllTaskAllocationsItem.ReportingToUsername = $"{opReportingTo.FirstName} {opReportingTo.LastName}";

                var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
                if (opResponsibleUser != null)
                    v02_Workflow_Allocations_AllTaskAllocationsItem.ResponsibleUsername = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName}";


                if (tType.LinkedSecureAreaID.HasValue)
                {
                    foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
                    {
                        foreach (var sa in psa.Value)
                        {
                            if ((int)sa.Item1 == tType.LinkedSecureAreaID.Value)
                            {
                                v02_Workflow_Allocations_AllTaskAllocationsItem.SecureAreaName = $"{psa.Key.Item3} - {sa.Item3}";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    v02_Workflow_Allocations_AllTaskAllocationsItem.SecureAreaName = "00 - NONE";
                }

                model.V02_Workflow_Allocations_AllTaskAllocationsItems.Add(v02_Workflow_Allocations_AllTaskAllocationsItem);
            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_JobDescriptions.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V03_HumanResources/V03_HumanResources_JobDescriptions/{ID}")]
        public async Task<IActionResult> V03_HumanResources_JobDescriptions(int ID, V03_HumanResources_JobDescriptionsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_JobDescriptions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_JobDescriptions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var opProfs = (from p in db.OperationalProfiles
                           where !string.IsNullOrEmpty(p.FirstName)
                           && !string.IsNullOrEmpty(p.LastName)
                           select p).ToList();

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            var v03_JobDescription = (from p in db.V03_JobDescriptions
                                      where p.PersonalDetailsID == v03_PersonalDetail.ID
                                      select p).SingleOrDefault();


            var op = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
            V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem item = new V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem()
            {
                BankAccountName = v03_PersonalDetail.BankAccountName,
                BankAccountNo = v03_PersonalDetail.BankAccountNo,
                BankBranchName = v03_PersonalDetail.BankBranchName,
                BankName = v03_PersonalDetail.BankName,
                CompanyID = v03_PersonalDetail.CompanyID,
                ContactNo = v03_PersonalDetail.ContactNo,
                DateOfBirth = v03_PersonalDetail.DateOfBirth,
                EmergencyFullName = v03_PersonalDetail.EmergencyFullName,
                EmergencyPrimaryContactNo = v03_PersonalDetail.EmergencyPrimaryContactNo,
                EmergencyRelationship = v03_PersonalDetail.EmergencyRelationship,
                EmergencyResidentialAddress = v03_PersonalDetail.EmergencyResidentialAddress,
                EmployeeEmail = v03_PersonalDetail.EmployeeEmail,
                EmployeeNo = v03_PersonalDetail.EmployeeNo,
                EndDate = v03_PersonalDetail.EndDate,
                FullName = v03_PersonalDetail.FullName,
                ID = v03_PersonalDetail.ID,
                IDNumber = v03_PersonalDetail.IDNumber,
                IncomeTaxNo = v03_PersonalDetail.IncomeTaxNo,
                MaritialStatus = v03_PersonalDetail.MaritialStatus,
                OperationalUserID = v03_PersonalDetail.OperationalUserID,
                PersonalEmail = v03_PersonalDetail.PersonalEmail,
                ReportingToUserID = v03_PersonalDetail.ReportingToUserID,
                ResidentialAddress = v03_PersonalDetail.ResidentialAddress,
                SpouseContactNo = v03_PersonalDetail.SpouseContactNo,
                SpouseEmployer = v03_PersonalDetail.SpouseEmployer,
                SpouseName = v03_PersonalDetail.SpouseName,
                StartDate = v03_PersonalDetail.StartDate,
                OperationalProfile = op,
                OperationalUser = $"{op.FirstName} {op.LastName}",
            };

            if (v03_PersonalDetail.CompanyID.HasValue)
                item.CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == v03_PersonalDetail.CompanyID.Value).SingleOrDefault().Name;

            var opRep = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
            item.ReportingToUser = $"{opRep.FirstName} {opRep.LastName}";

            model.V03_HumanResources_AllHumanResourcesItem = item;

            model.V02_Workflow_Allocations_AllTaskAllocationsItems = new List<Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem>();

            var a08_Task_Types = db.A08_Task_Types.Where(p => p.ResponsibleUserID == v03_PersonalDetail.OperationalUserID || p.ReportingToUserID == v03_PersonalDetail.OperationalUserID).ToList();

            foreach (var tType in a08_Task_Types)
            {
                Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem v02_Workflow_Allocations_AllTaskAllocationsItem = new Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem()
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
                    SecureAreaName = tType.ResponsibleUserID,
                    StatusGroupID = tType.StatusGroupID,
                    TemplateNo = tType.TemplateNo,
                };

                var opReportingTo = opProfs.Where(p => p.UserID == tType.ReportingToUserID).SingleOrDefault();
                if (opReportingTo != null)
                    v02_Workflow_Allocations_AllTaskAllocationsItem.ReportingToUsername = $"{opReportingTo.FirstName} {opReportingTo.LastName}";

                var opResponsibleUser = opProfs.Where(p => p.UserID == tType.ResponsibleUserID).SingleOrDefault();
                if (opResponsibleUser != null)
                    v02_Workflow_Allocations_AllTaskAllocationsItem.ResponsibleUsername = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName}";


                if (tType.LinkedSecureAreaID.HasValue)
                {
                    foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
                    {
                        foreach (var sa in psa.Value)
                        {
                            if ((int)sa.Item1 == tType.LinkedSecureAreaID.Value)
                            {
                                v02_Workflow_Allocations_AllTaskAllocationsItem.SecureAreaName = $"{psa.Key.Item3} - {sa.Item3}";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    v02_Workflow_Allocations_AllTaskAllocationsItem.SecureAreaName = "00 - NONE";
                }

                model.V02_Workflow_Allocations_AllTaskAllocationsItems.Add(v02_Workflow_Allocations_AllTaskAllocationsItem);
            }

            model.V03_JobDescriptions_LogItems = new List<V03_HumanResources_JobDescriptionsModel.V03_JobDescriptions_LogItem>();
            if (v03_JobDescription != null)
            {
                model.ReportingToUserAccepted = v03_JobDescription.ReportingToUserAccepted;
                model.ReportingToUserAcceptedDate = v03_JobDescription.ReportingToUserAcceptedDate;
                model.ResponsibleUserAccepted = v03_JobDescription.ResponsibleUserAccepted;
                model.ResponsibleUserAcceptedDate = v03_JobDescription.ResponsibleUserAcceptedDate;
                var v03_PersonalDetails_Logs = db.V03_JobDescriptions_Logs.Where(p => p.JobDescriptionID == v03_JobDescription.ID).ToList();
                foreach (var log in v03_PersonalDetails_Logs.OrderByDescending(p => p.DateCreated))
                {
                    var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                    model.V03_JobDescriptions_LogItems.Add(new V03_HumanResources_JobDescriptionsModel.V03_JobDescriptions_LogItem()
                    {
                        DateCreated = log.DateCreated,
                        UserID = log.UserID,
                        ID = log.ID,
                        SystemDescription = log.SystemDescription,
                        JobDescriptionID = log.JobDescriptionID,
                        Username = $"{opProf.FirstName} {opProf.LastName}",
                    });
                }
            }

            if (ModelState.IsValid)
            {
                if (v03_JobDescription != null)
                {
                    StringBuilder sbSysLog = new StringBuilder();
                    if (v03_JobDescription.DutiesAndResponsibilities_Primary != model.DutiesAndResponsibilities_Primary)
                    {
                        sbSysLog.AppendLine($"DutiesAndResponsibilities_Primary from '{v03_JobDescription.DutiesAndResponsibilities_Primary}' to '{model.DutiesAndResponsibilities_Primary}'<br />");
                        v03_JobDescription.DutiesAndResponsibilities_Primary = model.DutiesAndResponsibilities_Primary;
                    }

                    if (v03_JobDescription.DutiesAndResponsibilities_Secondary != model.DutiesAndResponsibilities_Secondary)
                    {
                        sbSysLog.AppendLine($"DutiesAndResponsibilities_Secondary from '{v03_JobDescription.DutiesAndResponsibilities_Secondary}' to '{model.DutiesAndResponsibilities_Secondary}'<br />");
                        v03_JobDescription.DutiesAndResponsibilities_Secondary = model.DutiesAndResponsibilities_Secondary;
                    }

                    if (v03_JobDescription.DutiesAndResponsibilities_Overall != model.DutiesAndResponsibilities_Overall)
                    {
                        sbSysLog.AppendLine($"DutiesAndResponsibilities_Overall from '{v03_JobDescription.DutiesAndResponsibilities_Overall}' to '{model.DutiesAndResponsibilities_Overall}'<br />");
                        v03_JobDescription.DutiesAndResponsibilities_Overall = model.DutiesAndResponsibilities_Overall;
                    }

                    if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                    {
                        #region Reset Acceptance 

                        if (v03_JobDescription.ReportingToUserAccepted.HasValue)
                        {
                            sbSysLog.AppendLine($"Reporting To User Acceptance removed.<br />");
                            v03_JobDescription.ReportingToUserAccepted = null;
                            v03_JobDescription.ReportingToUserAcceptedDate = null;
                        }

                        if (v03_JobDescription.ResponsibleUserAccepted.HasValue)
                        {
                            sbSysLog.AppendLine($"Responsible User Acceptance removed.<br />");
                            v03_JobDescription.ResponsibleUserAccepted = null;
                            v03_JobDescription.ResponsibleUserAcceptedDate = null;
                        }

                        #endregion

                        db.Update(v03_JobDescription);
                        db.SaveChanges();

                        Data.V03_JobDescriptions_Log v03_PersonalDetails_Log = new V03_JobDescriptions_Log()
                        {
                            DateCreated = DateTime.Now,
                            JobDescriptionID = v03_JobDescription.ID,
                            SystemDescription = sbSysLog.ToString(),
                            UserID = _userManager.GetUserId(User),
                        };

                        db.Add(v03_PersonalDetails_Log);
                        db.SaveChanges();

                        model.IsSuccess = true;
                    }

                }
                else
                {
                    v03_JobDescription = new V03_JobDescription()
                    {
                        DutiesAndResponsibilities_Overall = model.DutiesAndResponsibilities_Overall,
                        DutiesAndResponsibilities_Secondary = model.DutiesAndResponsibilities_Secondary,
                        DutiesAndResponsibilities_Primary = model.DutiesAndResponsibilities_Primary,
                        PersonalDetailsID = v03_PersonalDetail.ID,
                    };

                    db.Add(v03_JobDescription);
                    db.SaveChanges();

                    Data.V03_JobDescriptions_Log v03_PersonalDetails_Log = new V03_JobDescriptions_Log()
                    {
                        DateCreated = DateTime.Now,
                        JobDescriptionID = v03_JobDescription.ID,
                        SystemDescription = "Created",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(v03_PersonalDetails_Log);
                    db.SaveChanges();

                    model.IsSuccess = true;

                }

            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_JobDescriptions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_JobDescriptions_ResponsibleUserAccept/{ID}")]
        public async Task<IActionResult> V03_HumanResources_JobDescriptions_ResponsibleUserAccept(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();
            var v03_JobDescription = (from p in db.V03_JobDescriptions
                                      where p.PersonalDetailsID == v03_PersonalDetail.ID
                                      select p).SingleOrDefault();



            if (v03_PersonalDetail == null
                || v03_JobDescription == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            if (v03_PersonalDetail.OperationalUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                v03_JobDescription.ResponsibleUserAccepted = true;
                v03_JobDescription.ResponsibleUserAcceptedDate = DateTime.Now;
                db.Update(v03_JobDescription);
                db.SaveChanges();

                var opResponsibleUser = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
                Data.V03_JobDescriptions_Log log = new V03_JobDescriptions_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opResponsibleUser.FirstName} {opResponsibleUser.LastName} Responsible User Accept",
                    UserID = _userManager.GetUserId(User),
                    JobDescriptionID = v03_JobDescription.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect($"/operational/V03_HumanResources/V03_HumanResources_JobDescriptions/{ID}");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_JobDescriptions_ReportingToUserAccept/{ID}")]
        public async Task<IActionResult> V03_HumanResources_JobDescriptions_ReportingToUserAccept(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();
            var v03_JobDescription = (from p in db.V03_JobDescriptions
                                      where p.PersonalDetailsID == v03_PersonalDetail.ID
                                      select p).SingleOrDefault();



            if (v03_PersonalDetail == null
                || v03_JobDescription == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            if (v03_PersonalDetail.ReportingToUserID == _userManager.GetUserId(User))
            {
                var opProfs = db.OperationalProfiles.ToList();
                v03_JobDescription.ReportingToUserAccepted = true;
                v03_JobDescription.ReportingToUserAcceptedDate = DateTime.Now;
                db.Update(v03_PersonalDetail);
                db.SaveChanges();

                var opReportingTo = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
                Data.V03_JobDescriptions_Log log = new V03_JobDescriptions_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"{opReportingTo.FirstName} {opReportingTo.LastName} Reporting To User Accept",
                    UserID = _userManager.GetUserId(User),
                    JobDescriptionID = v03_JobDescription.ID,
                };

                db.Add(log);
                db.SaveChanges();
            }


            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect($"/operational/V03_HumanResources/V03_HumanResources_JobDescriptions/{ID}");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList")]
        public async Task<IActionResult> V03_HumanResources_CorrespondenceList()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_CorrespondenceList, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_CorrespondenceList}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.OperationalUserID == _userManager.GetUserId(User)
                                      select p).SingleOrDefault();
            if (v03_PersonalDetail != null)
                return Redirect($"/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList/{v03_PersonalDetail.ID}");

            return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");
        }


        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList/{ID}")]
        public async Task<IActionResult> V03_HumanResources_CorrespondenceList(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_CorrespondenceList, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_CorrespondenceList}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            var opProfs = db.OperationalProfiles.ToList();

            var opPDItem = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
            V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem v03_HumanResources_AllHumanResourcesItem = new V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem()
            {
                BankAccountName = v03_PersonalDetail.BankAccountName,
                BankAccountNo = v03_PersonalDetail.BankAccountNo,
                BankBranchName = v03_PersonalDetail.BankBranchName,
                BankName = v03_PersonalDetail.BankName,
                CompanyID = v03_PersonalDetail.CompanyID,
                ContactNo = v03_PersonalDetail.ContactNo,
                DateOfBirth = v03_PersonalDetail.DateOfBirth,
                EmergencyFullName = v03_PersonalDetail.EmergencyFullName,
                EmergencyPrimaryContactNo = v03_PersonalDetail.EmergencyPrimaryContactNo,
                EmergencyRelationship = v03_PersonalDetail.EmergencyRelationship,
                EmergencyResidentialAddress = v03_PersonalDetail.EmergencyResidentialAddress,
                EmployeeEmail = v03_PersonalDetail.EmployeeEmail,
                EmployeeNo = v03_PersonalDetail.EmployeeNo,
                EndDate = v03_PersonalDetail.EndDate,
                FullName = v03_PersonalDetail.FullName,
                ID = v03_PersonalDetail.ID,
                IDNumber = v03_PersonalDetail.IDNumber,
                IncomeTaxNo = v03_PersonalDetail.IncomeTaxNo,
                MaritialStatus = v03_PersonalDetail.MaritialStatus,
                OperationalUserID = v03_PersonalDetail.OperationalUserID,
                PersonalEmail = v03_PersonalDetail.PersonalEmail,
                ReportingToUserID = v03_PersonalDetail.ReportingToUserID,
                ResidentialAddress = v03_PersonalDetail.ResidentialAddress,
                SpouseContactNo = v03_PersonalDetail.SpouseContactNo,
                SpouseEmployer = v03_PersonalDetail.SpouseEmployer,
                SpouseName = v03_PersonalDetail.SpouseName,
                StartDate = v03_PersonalDetail.StartDate,
                OperationalProfile = opPDItem,
                OperationalUser = $"{opPDItem.FirstName} {opPDItem.LastName}",
            };

            if (v03_PersonalDetail.CompanyID.HasValue)
                v03_HumanResources_AllHumanResourcesItem.CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == v03_PersonalDetail.CompanyID.Value).SingleOrDefault().Name;

            var opRep = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
            v03_HumanResources_AllHumanResourcesItem.ReportingToUser = $"{opRep.FirstName} {opRep.LastName}";

            V03_HumanResources_CorrespondenceListModel model = new V03_HumanResources_CorrespondenceListModel()
            {
                AttachmentType = (from p in ((V03_Correspondence.AttachmentTypeEnum[])Enum.GetValues(typeof(V03_Correspondence.AttachmentTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
                V03_CorrespondenceItems = new List<V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem>(),
                V03_HumanResources_AllHumanResourcesItem = v03_HumanResources_AllHumanResourcesItem,
            };

            var v03_Correspondences = (from p in db.V03_Correspondences
                                       where p.PersonalDetailsID == ID
                                       select p).ToList();

            foreach (var v03_Correspondence in v03_Correspondences)
            {
                var op = opProfs.Where(p => p.UserID == v03_Correspondence.UserID).SingleOrDefault();
                V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem item = new V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem()
                {
                    UserID = v03_Correspondence.UserID,
                    AttachmentTypeID = v03_Correspondence.AttachmentTypeID,
                    DateCreated = v03_Correspondence.DateCreated,
                    Description = v03_Correspondence.Description,
                    Filename = v03_Correspondence.Filename,
                    ID = v03_Correspondence.ID,
                    IsDeleted = v03_Correspondence.IsDeleted,
                    PersonalDetailsID = v03_Correspondence.PersonalDetailsID,
                    Username = $"{op.FirstName} {op.LastName}",
                };

                model.V03_CorrespondenceItems.Add(item);
            }

            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_CorrespondenceList.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList/{ID}")]
        public async Task<IActionResult> V03_HumanResources_CorrespondenceList(int ID, V03_HumanResources_CorrespondenceListModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V03_HumanResources_CorrespondenceList, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V03_HumanResources_CorrespondenceList}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var v03_PersonalDetail = (from p in db.V03_PersonalDetails
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_PersonalDetail == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_AllHumanResources");

            var opProfs = db.OperationalProfiles.ToList();

            var opPDItem = opProfs.Where(p => p.UserID == v03_PersonalDetail.OperationalUserID).SingleOrDefault();
            V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem v03_HumanResources_AllHumanResourcesItem = new V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem()
            {
                BankAccountName = v03_PersonalDetail.BankAccountName,
                BankAccountNo = v03_PersonalDetail.BankAccountNo,
                BankBranchName = v03_PersonalDetail.BankBranchName,
                BankName = v03_PersonalDetail.BankName,
                CompanyID = v03_PersonalDetail.CompanyID,
                ContactNo = v03_PersonalDetail.ContactNo,
                DateOfBirth = v03_PersonalDetail.DateOfBirth,
                EmergencyFullName = v03_PersonalDetail.EmergencyFullName,
                EmergencyPrimaryContactNo = v03_PersonalDetail.EmergencyPrimaryContactNo,
                EmergencyRelationship = v03_PersonalDetail.EmergencyRelationship,
                EmergencyResidentialAddress = v03_PersonalDetail.EmergencyResidentialAddress,
                EmployeeEmail = v03_PersonalDetail.EmployeeEmail,
                EmployeeNo = v03_PersonalDetail.EmployeeNo,
                EndDate = v03_PersonalDetail.EndDate,
                FullName = v03_PersonalDetail.FullName,
                ID = v03_PersonalDetail.ID,
                IDNumber = v03_PersonalDetail.IDNumber,
                IncomeTaxNo = v03_PersonalDetail.IncomeTaxNo,
                MaritialStatus = v03_PersonalDetail.MaritialStatus,
                OperationalUserID = v03_PersonalDetail.OperationalUserID,
                PersonalEmail = v03_PersonalDetail.PersonalEmail,
                ReportingToUserID = v03_PersonalDetail.ReportingToUserID,
                ResidentialAddress = v03_PersonalDetail.ResidentialAddress,
                SpouseContactNo = v03_PersonalDetail.SpouseContactNo,
                SpouseEmployer = v03_PersonalDetail.SpouseEmployer,
                SpouseName = v03_PersonalDetail.SpouseName,
                StartDate = v03_PersonalDetail.StartDate,
                OperationalProfile = opPDItem,
                OperationalUser = $"{opPDItem.FirstName} {opPDItem.LastName}",
            };

            if (v03_PersonalDetail.CompanyID.HasValue)
                v03_HumanResources_AllHumanResourcesItem.CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == v03_PersonalDetail.CompanyID.Value).SingleOrDefault().Name;

            var opRep = opProfs.Where(p => p.UserID == v03_PersonalDetail.ReportingToUserID).SingleOrDefault();
            v03_HumanResources_AllHumanResourcesItem.ReportingToUser = $"{opRep.FirstName} {opRep.LastName}";

            model.AttachmentType = (from p in ((V03_Correspondence.AttachmentTypeEnum[])Enum.GetValues(typeof(V03_Correspondence.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();

            model.V03_CorrespondenceItems = new List<V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem>();
            model.V03_HumanResources_AllHumanResourcesItem = v03_HumanResources_AllHumanResourcesItem;

            var v03_Correspondences = (from p in db.V03_Correspondences
                                       where p.PersonalDetailsID == ID
                                       select p).ToList();

            foreach (var v03_Correspondence in v03_Correspondences)
            {
                var op = opProfs.Where(p => p.UserID == v03_Correspondence.UserID).SingleOrDefault();
                V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem item = new V03_HumanResources_CorrespondenceListModel.V03_CorrespondenceItem()
                {
                    UserID = v03_Correspondence.UserID,
                    AttachmentTypeID = v03_Correspondence.AttachmentTypeID,
                    DateCreated = v03_Correspondence.DateCreated,
                    Description = v03_Correspondence.Description,
                    Filename = v03_Correspondence.Filename,
                    ID = v03_Correspondence.ID,
                    IsDeleted = v03_Correspondence.IsDeleted,
                    PersonalDetailsID = v03_Correspondence.PersonalDetailsID,
                    Username = $"{op.FirstName} {op.LastName}",
                };

                model.V03_CorrespondenceItems.Add(item);
            }


            if (ModelState.IsValid)
            {
                if (model.Attachment != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "v03-humanresources-correspondencelist";
                    string dirName = $"{ID}";
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

                    V03_Correspondence v03_Correspondence = new V03_Correspondence()
                    {
                        AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                        DateCreated = DateTime.Now,
                        Filename = fileName,
                        PersonalDetailsID = ID,
                        UserID = _userManager.GetUserId(User),
                        Description = model.Description,
                        IsDeleted = false,
                    };

                    db.Add(v03_Correspondence);
                    db.SaveChanges();

                    model.IsSuccess = true;

                }
            }


            return View("~/Views/Operational/V03_HumanResources/V03_HumanResources_CorrespondenceList.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_Correspondence_Download/{ID}")]
        public async Task<IActionResult> V03_HumanResources_Correspondence_Download(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var v03_Correspondence = (from p in db.V03_Correspondences
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_Correspondence == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList");


                    string shareName = "v03-humanresources-correspondencelist";
            string dirName = $"{v03_Correspondence.PersonalDetailsID}";
            string fileName = v03_Correspondence.Filename;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(v03_Correspondence.Filename));


            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/V03_HumanResources/V03_HumanResources_Correspondence_Delete/{ID}")]
        public async Task<IActionResult> V01_Policies_All_DeleteAttachment(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var v03_Correspondence = (from p in db.V03_Correspondences
                                      where p.ID == ID
                                      select p).SingleOrDefault();

            if (v03_Correspondence == null)
                return Redirect("/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList");

            v03_Correspondence.IsDeleted = true;
            db.Update(v03_Correspondence);
            db.SaveChanges();

            return Redirect($"/operational/V03_HumanResources/V03_HumanResources_CorrespondenceList/{v03_Correspondence.PersonalDetailsID}");
        }

    }
}
