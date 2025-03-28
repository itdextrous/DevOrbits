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
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Z_BugsModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Z_Bugs
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Z_BugsController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public Z_BugsController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IConfiguration configuration
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_ReportABug")]
        public async Task<IActionResult> Z_Bugs_ReportABug()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_ReportABug, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_ReportABug}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            Z_Bugs_ReportABugModel model = new Z_Bugs_ReportABugModel()
            {
                PriorityID = (from p in ((Data.ReportedBug.PriorityEnum[])Enum.GetValues(typeof(Data.ReportedBug.PriorityEnum)))
                              select new SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = ((int)p).ToString(),
                              }).ToList(),
                LinkedSecureAreaID = new List<SelectListItem>(),
                IsSuccess = false,
                CompanyID = new List<SelectListItem>(),
                CustomerNo = _operationalProvider.CustomerNumber,
                MeterSerial = _operationalProvider.CustomerMeterSerial,
            };

            model.LinkedSecureAreaID.Add(new SelectListItem() { Value = "", Text = $"00 - NONE" });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    if (_operationalProvider.UserSecureAreaActions.Where(p => p.SecureAreaID == (int)sa.Item1).Count() > 0)
                        model.LinkedSecureAreaID.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}" });
                }
            }
            model.LinkedSecureAreaID = model.LinkedSecureAreaID.OrderBy(p => p.Text).ToList();

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = $"[Not Linked To Company]", Selected = _operationalProvider.CompanyID == 0 });
            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = uC.CompanyID.ToString(), Text = $"{company.Name}", Selected = _operationalProvider.CompanyID == uC.CompanyID });
            }

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_ReportABug.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Z_Bugs/Z_Bugs_ReportABug")]
        public async Task<IActionResult> Z_Bugs_ReportABug(Z_Bugs_ReportABugModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_ReportABug, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_ReportABug}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.PriorityID = (from p in ((Data.ReportedBug.PriorityEnum[])Enum.GetValues(typeof(Data.ReportedBug.PriorityEnum)))
                                select new SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString(),
                                    Selected = ((int)p).ToString() == Request.Form["PriorityID"]
                                }).ToList();
            model.LinkedSecureAreaID = new List<SelectListItem>();
            model.IsSuccess = false;
            model.CompanyID = new List<SelectListItem>();

            model.LinkedSecureAreaID.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    if (_operationalProvider.UserSecureAreaActions.Where(p => p.SecureAreaID == (int)sa.Item1).Count() > 0)
                        model.LinkedSecureAreaID.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureAreaID"] == ((int)sa.Item1).ToString() });
                }
            }
            model.LinkedSecureAreaID = model.LinkedSecureAreaID.OrderBy(p => p.Text).ToList();

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = $"[Not Linked To Company]", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) });
            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = uC.CompanyID.ToString(), Text = $"{company.Name}", Selected = Request.Form["CompanyID"] == uC.CompanyID.ToString() });
            }

            if (ModelState.IsValid)
            {
                Data.ReportedBug reportedBug = new ReportedBug()
                {
                    CompanyID = null,
                    CustomerNo = "",
                    DateCreated = DateTime.Now,
                    LinkedSecureAreaID = null,
                    MeterSerial = "",
                    PriorityID = Convert.ToInt32(Request.Form["PriorityID"]),
                    PriorityReason = model.PriorityReason,
                    Screenshot = "",
                    StatusID = (int)Data.ReportedBug.StatusEnum.New,
                    UserDescription = model.UserDescription,
                    UserID = _userManager.GetUserId(User),
                };

                if (!string.IsNullOrEmpty(model.CustomerNo))
                    reportedBug.CustomerNo = model.CustomerNo;

                if (!string.IsNullOrEmpty(model.MeterSerial))
                    reportedBug.MeterSerial = model.MeterSerial;

                if (!string.IsNullOrEmpty(Request.Form["CompanyID"]))
                    reportedBug.CompanyID = Convert.ToInt32(Request.Form["CompanyID"]);

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]))
                    reportedBug.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureAreaID"]);

                db.Add(reportedBug);
                db.SaveChanges();

                if (model.Screenshot != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "z-bugs-attachments";
                    string dirName = $"{reportedBug.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Screenshot.FileName);

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
                    model.Screenshot.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);


                    reportedBug.Screenshot = fileName;
                    db.Update(reportedBug);
                    db.SaveChanges();
                }

                Data.ReportedBugs_Log reportedBugs_Log = new ReportedBugs_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = "Bug Created",
                    BugID = reportedBug.ID,
                    UserDescription = "Bug Created",
                    UserID = _userManager.GetUserId(User),
                };

                db.Add(reportedBugs_Log);
                db.SaveChanges();


                model.IsSuccess = true;

            }

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_ReportABug.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_MyBugs")]
        public async Task<IActionResult> Z_Bugs_MyBugs()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_MyBugs, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_MyBugs}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            Z_Bugs_MyBugsModel model = new Z_Bugs_MyBugsModel()
            {
                Z_Bugs_MyBugsItems = new List<Z_Bugs_MyBugsModel.Z_Bugs_MyBugsItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var bugs = db.ReportedBugs.Where(p => p.UserID == _userManager.GetUserId(User)).ToList();
            if (_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_MyBugs, SecureAreaActionEnum.ManagementApproval))
                bugs = db.ReportedBugs.ToList();

            foreach (var b in bugs)
            {
                string userName = _userManager.FindByIdAsync(b.UserID).Result.UserName;
                var userProf = opProfs.Where(p => p.UserID == b.UserID).SingleOrDefault();
                if (userProf != null && !string.IsNullOrEmpty(userProf.FirstName))
                    userName = $"{userProf.FirstName} {userProf.LastName}";

                Z_Bugs_MyBugsModel.Z_Bugs_MyBugsItem item = new Z_Bugs_MyBugsModel.Z_Bugs_MyBugsItem()
                {
                    CompanyID = b.CompanyID,
                    CustomerNo = b.CustomerNo,
                    DateCreated = b.DateCreated,
                    ID = b.ID,
                    LinkedSecureAreaID = b.LinkedSecureAreaID,
                    PriorityID = b.PriorityID,
                    PriorityReason = b.PriorityReason,
                    Screenshot = b.Screenshot,
                    StatusID = b.StatusID,
                    MeterSerial = b.MeterSerial,
                    UserDescription = b.UserDescription,
                    UserID = b.UserID,
                    CompanyName = b.CompanyID.HasValue ? _operationalProvider.Companies.Where(p => p.CompanyID == b.CompanyID.Value).SingleOrDefault().Name : "[Not Linked]",
                    Username = userName,
                };

                model.Z_Bugs_MyBugsItems.Add(item);
            }

            model.Z_Bugs_MyBugsItems = model.Z_Bugs_MyBugsItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_MyBugs.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_BugDetail")]
        public async Task<IActionResult> Z_Bugs_BugDetail()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_BugDetail, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_BugDetail}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var reportedBug = db.ReportedBugs.Where(p => p.ID == _operationalProvider.SelectedBugID).SingleOrDefault();

            if (_operationalProvider.SelectedBugID == 0 || reportedBug == null)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            string userName = _userManager.FindByIdAsync(reportedBug.UserID).Result.UserName;
            var userProf = opProfs.Where(p => p.UserID == reportedBug.UserID).SingleOrDefault();
            if (userProf != null && !string.IsNullOrEmpty(userProf.FirstName))
                userName = $"{userProf.FirstName} {userProf.LastName}";

            Z_Bugs_BugDetailModel model = new Z_Bugs_BugDetailModel()
            {
                PriorityID = (from p in ((Data.ReportedBug.PriorityEnum[])Enum.GetValues(typeof(Data.ReportedBug.PriorityEnum)))
                              select new SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = ((int)p).ToString(),
                                  Selected = (int)p == reportedBug.PriorityID,
                              }).ToList(),
                LinkedSecureAreaID = new List<SelectListItem>(),
                IsSuccess = false,
                CompanyID = new List<SelectListItem>(),
                ReportedBug = new Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem()
                {
                    CompanyID = reportedBug.CompanyID,
                    CustomerNo = reportedBug.CustomerNo,
                    DateCreated = reportedBug.DateCreated,
                    ID = reportedBug.ID,
                    LinkedSecureAreaID = reportedBug.LinkedSecureAreaID,
                    MeterSerial = reportedBug.MeterSerial,
                    PriorityID = reportedBug.PriorityID,
                    PriorityReason = reportedBug.PriorityReason,
                    Screenshot = reportedBug.Screenshot,
                    StatusID = reportedBug.StatusID,
                    UserDescription = reportedBug.UserDescription,
                    UserID = reportedBug.UserID,
                    Username = userName,
                    LogItems = new List<Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem>(),
                },
                CustomerNo = reportedBug.CustomerNo,
                MeterSerial = reportedBug.MeterSerial,
                PriorityReason = reportedBug.PriorityReason,
                UserDescription = reportedBug.UserDescription,
            };

            var logs = db.ReportedBugs_Logs.Where(p => p.BugID == reportedBug.ID).OrderByDescending(p => p.DateCreated).ToList();
            var comments = db.ReportedBugs_Comments.Where(p => p.BugID == reportedBug.ID).OrderByDescending(p => p.DateCreated).ToList();
            foreach (var log in logs)
            {
                string loguserName = _userManager.FindByIdAsync(log.UserID).Result.UserName;
                var loguserProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                if (loguserProf != null && !string.IsNullOrEmpty(loguserProf.FirstName))
                    loguserName = $"{loguserProf.FirstName} {loguserProf.LastName}";

                Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem item = new Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem()
                {
                    BugID = log.BugID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserDescription = log.UserDescription,
                    UserID = log.UserID,
                    Username = loguserName,
                };

                if (log.UserDescription.StartsWith("COMMENT"))
                {
                    var comment = comments.Where(p => p.ID == Convert.ToInt32(log.UserDescription.Split(',')[1])).SingleOrDefault();
                    if (!string.IsNullOrEmpty(comment.Screenshot))
                        item.UserDescription = $"<a href=\"/operational/Z_Bugs/Z_Bugs_BugComment_Screenshot/{comment.ID}\" class=\"btn btn-sm btn-outline-primary-sm\" target=\"_blank\"><i class=\"fas fa-download\"></i>&nbsp;Download Attached Screenshot</a>";
                    else
                        item.UserDescription = comment.UserDescription;
                }

                model.ReportedBug.LogItems.Add(item);
            }

            model.LinkedSecureAreaID.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = !reportedBug.LinkedSecureAreaID.HasValue });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    if (_operationalProvider.UserSecureAreaActions.Where(p => p.SecureAreaID == (int)sa.Item1).Count() > 0)
                        model.LinkedSecureAreaID.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = reportedBug.LinkedSecureAreaID.HasValue && reportedBug.LinkedSecureAreaID.Value == (int)sa.Item1 });
                }
            }
            model.LinkedSecureAreaID = model.LinkedSecureAreaID.OrderBy(p => p.Text).ToList();

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = $"[Not Linked To Company]", Selected = !reportedBug.CompanyID.HasValue });
            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = uC.CompanyID.ToString(), Text = $"{company.Name}", Selected = reportedBug.CompanyID.HasValue && reportedBug.CompanyID.Value == uC.CompanyID });
            }

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_BugDetail.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Z_Bugs/Z_Bugs_BugDetail")]
        public async Task<IActionResult> Z_Bugs_BugDetail(Z_Bugs_BugDetailModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_BugDetail, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_BugDetail}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var reportedBug = db.ReportedBugs.Where(p => p.ID == _operationalProvider.SelectedBugID).SingleOrDefault();

            if (_operationalProvider.SelectedBugID == 0 || reportedBug == null)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            string userName = _userManager.FindByIdAsync(reportedBug.UserID).Result.UserName;
            var userProf = opProfs.Where(p => p.UserID == reportedBug.UserID).SingleOrDefault();
            if (userProf != null && !string.IsNullOrEmpty(userProf.FirstName))
                userName = $"{userProf.FirstName} {userProf.LastName}";

            model.ReportedBug = new Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem()
            {
                CompanyID = reportedBug.CompanyID,
                CustomerNo = reportedBug.CustomerNo,
                DateCreated = reportedBug.DateCreated,
                ID = reportedBug.ID,
                LinkedSecureAreaID = reportedBug.LinkedSecureAreaID,
                MeterSerial = reportedBug.MeterSerial,
                PriorityID = reportedBug.PriorityID,
                PriorityReason = reportedBug.PriorityReason,
                Screenshot = reportedBug.Screenshot,
                StatusID = reportedBug.StatusID,
                UserDescription = reportedBug.UserDescription,
                UserID = reportedBug.UserID,
                Username = userName,
                LogItems = new List<Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem>(),
            };

            var logs = db.ReportedBugs_Logs.Where(p => p.BugID == reportedBug.ID).OrderByDescending(p => p.DateCreated).ToList();
            foreach (var log in logs)
            {
                string loguserName = _userManager.FindByIdAsync(log.UserID).Result.UserName;
                var loguserProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                if (loguserProf != null && !string.IsNullOrEmpty(loguserProf.FirstName))
                    loguserName = $"{loguserProf.FirstName} {loguserProf.LastName}";

                Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem item = new Z_Bugs_BugDetailModel.Z_Bugs_BugDetailItem.Z_Bugs_BugDetail_LogItem()
                {
                    BugID = log.BugID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserDescription = log.UserDescription,
                    UserID = log.UserID,
                    Username = loguserName,
                };

                model.ReportedBug.LogItems.Add(item);
            }

            model.PriorityID = (from p in ((Data.ReportedBug.PriorityEnum[])Enum.GetValues(typeof(Data.ReportedBug.PriorityEnum)))
                                select new SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString(),
                                    Selected = ((int)p).ToString() == Request.Form["PriorityID"]
                                }).ToList();
            model.LinkedSecureAreaID = new List<SelectListItem>();
            model.IsSuccess = false;
            model.CompanyID = new List<SelectListItem>();

            model.LinkedSecureAreaID.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    if (_operationalProvider.UserSecureAreaActions.Where(p => p.SecureAreaID == (int)sa.Item1).Count() > 0)
                        model.LinkedSecureAreaID.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureAreaID"] == ((int)sa.Item1).ToString() });
                }
            }
            model.LinkedSecureAreaID = model.LinkedSecureAreaID.OrderBy(p => p.Text).ToList();

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = $"[Not Linked To Company]", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) });
            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = uC.CompanyID.ToString(), Text = $"{company.Name}", Selected = Request.Form["CompanyID"] == uC.CompanyID.ToString() });
            }

            if (ModelState.IsValid)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) && Request.Form["LinkedSecureAreaID"] != reportedBug.LinkedSecureAreaID.ToString())
                {
                    sbSysLog.AppendLine($"LinkedSecureAreaID from '{reportedBug.LinkedSecureAreaID}' to '{Request.Form["LinkedSecureAreaID"]}'.<br />");
                    reportedBug.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureAreaID"]);
                }
                else if (string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) && reportedBug.LinkedSecureAreaID.HasValue)
                {
                    sbSysLog.AppendLine($"LinkedSecureAreaID '{reportedBug.LinkedSecureAreaID}' removed.<br />");
                    reportedBug.LinkedSecureAreaID = null;
                }

                if (!string.IsNullOrEmpty(model.UserDescription) && model.UserDescription != reportedBug.UserDescription)
                {
                    sbSysLog.AppendLine($"UserDescription from '{reportedBug.UserDescription}' to '{model.UserDescription}'.<br />");
                    reportedBug.UserDescription = model.UserDescription;
                }
                else if (string.IsNullOrEmpty(model.UserDescription) && !string.IsNullOrEmpty(reportedBug.UserDescription))
                {
                    sbSysLog.AppendLine($"UserDescription '{reportedBug.UserDescription}' removed.<br />");
                    reportedBug.UserDescription = "";
                }

                if (!string.IsNullOrEmpty(Request.Form["CompanyID"]) && Request.Form["CompanyID"] != reportedBug.CompanyID.ToString())
                {
                    sbSysLog.AppendLine($"CompanyID from '{reportedBug.CompanyID}' to '{Request.Form["CompanyID"]}'.<br />");
                    reportedBug.CompanyID = Convert.ToInt32(Request.Form["CompanyID"]);
                }
                else if (string.IsNullOrEmpty(Request.Form["CompanyID"]) && reportedBug.CompanyID.HasValue)
                {
                    sbSysLog.AppendLine($"CompanyID '{reportedBug.CompanyID}' removed.<br />");
                    reportedBug.CompanyID = null;
                }

                if (!string.IsNullOrEmpty(model.CustomerNo) && model.CustomerNo != reportedBug.CustomerNo)
                {
                    sbSysLog.AppendLine($"CustomerNo from '{reportedBug.CustomerNo}' to '{model.CustomerNo}'.<br />");
                    reportedBug.CustomerNo = model.CustomerNo;
                }
                else if (string.IsNullOrEmpty(model.CustomerNo) && !string.IsNullOrEmpty(reportedBug.CustomerNo))
                {
                    sbSysLog.AppendLine($"CustomerNo '{reportedBug.CustomerNo}' removed.<br />");
                    reportedBug.CustomerNo = "";
                }

                if (!string.IsNullOrEmpty(model.MeterSerial) && model.MeterSerial != reportedBug.MeterSerial)
                {
                    sbSysLog.AppendLine($"MeterSerial from '{reportedBug.MeterSerial}' to '{model.MeterSerial}'.<br />");
                    reportedBug.MeterSerial = model.MeterSerial;
                }
                else if (string.IsNullOrEmpty(model.MeterSerial) && !string.IsNullOrEmpty(reportedBug.MeterSerial))
                {
                    sbSysLog.AppendLine($"MeterSerial '{reportedBug.MeterSerial}' removed.<br />");
                    reportedBug.MeterSerial = "";
                }

                if (Convert.ToInt32(Request.Form["PriorityID"]) != reportedBug.PriorityID)
                {
                    sbSysLog.AppendLine($"PriorityID from '{reportedBug.Priority.GetDescription()}' to '{((Data.ReportedBug.PriorityEnum)Convert.ToInt32(Request.Form["PriorityID"])).GetDescription()}'.<br />");
                    reportedBug.PriorityID = Convert.ToInt32(Request.Form["PriorityID"]);
                }

                if (!string.IsNullOrEmpty(model.PriorityReason) && model.PriorityReason != reportedBug.PriorityReason)
                {
                    sbSysLog.AppendLine($"PriorityReason from '{reportedBug.PriorityReason}' to '{model.PriorityReason}'.<br />");
                    reportedBug.PriorityReason = model.PriorityReason;
                }
                else if (string.IsNullOrEmpty(model.PriorityReason) && !string.IsNullOrEmpty(reportedBug.PriorityReason))
                {
                    sbSysLog.AppendLine($"PriorityReason '{reportedBug.PriorityReason}' removed.<br />");
                    reportedBug.PriorityReason = "";
                }

                if (model.Screenshot != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "z-bugs-attachments";
                    string dirName = $"{reportedBug.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Screenshot.FileName);

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
                    model.Screenshot.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);


                    sbSysLog.AppendLine($"Screenshot changed.<br />");
                    reportedBug.Screenshot = fileName;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(reportedBug);
                    db.SaveChanges();

                    Data.ReportedBugs_Log reportedBugs_Log = new ReportedBugs_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        BugID = reportedBug.ID,
                        UserDescription = "Bug Update",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(reportedBugs_Log);
                    db.SaveChanges();
                }

                model.IsSuccess = true;

            }

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_BugDetail.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_BugDetail_Screenshot/{ID}")]
        public async Task<IActionResult> Z_Bugs_BugDetail_Screenshot(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var bug = db.ReportedBugs.Where(p => p.ID == ID).SingleOrDefault();

            if (bug == null)
                return Content("Not Found");


            string shareName = "z-bugs-attachments";
            string dirName = $"{bug.ID}";
            string fileName = bug.Screenshot;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(bug.Screenshot));


            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_BugComment_Screenshot/{ID}")]
        public async Task<IActionResult> Z_Bugs_BugComment_Screenshot(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A08_Task_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A08_Task_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var bug = db.ReportedBugs_Comments.Where(p => p.ID == ID).SingleOrDefault();

            if (bug == null)
                return Content("Not Found");


            string shareName = "z-bugs-comments-attachments";
            string dirName = $"{bug.ID}";
            string fileName = bug.Screenshot;

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
                return File(uploadFile, contentType, System.IO.Path.GetFileName(bug.Screenshot));


            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/Z_Bugs/Z_Bugs_BugAdmin")]
        public async Task<IActionResult> Z_Bugs_BugAdmin()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_BugAdmin, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_BugAdmin}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (!_operationalProvider.IsDeveloper)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var reportedBug = db.ReportedBugs.Where(p => p.ID == _operationalProvider.SelectedBugID).SingleOrDefault();

            if (_operationalProvider.SelectedBugID == 0 || reportedBug == null)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            string userName = _userManager.FindByIdAsync(reportedBug.UserID).Result.UserName;
            var userProf = opProfs.Where(p => p.UserID == reportedBug.UserID).SingleOrDefault();
            if (userProf != null && !string.IsNullOrEmpty(userProf.FirstName))
                userName = $"{userProf.FirstName} {userProf.LastName}";

            Z_Bugs_BugAdminModel model = new Z_Bugs_BugAdminModel()
            {
                StatusID = (from p in ((Data.ReportedBug.StatusEnum[])Enum.GetValues(typeof(Data.ReportedBug.StatusEnum)))
                            select new SelectListItem()
                            {
                                Text = p.GetDescription(),
                                Value = ((int)p).ToString(),
                                Selected = (int)p == reportedBug.StatusID,
                            }).ToList(),
                IsSuccess = false,
                ReportedBug = new Z_Bugs_BugAdminModel.Z_Bugs_BugAdminItem()
                {
                    CompanyID = reportedBug.CompanyID,
                    CustomerNo = reportedBug.CustomerNo,
                    DateCreated = reportedBug.DateCreated,
                    ID = reportedBug.ID,
                    LinkedSecureAreaID = reportedBug.LinkedSecureAreaID,
                    MeterSerial = reportedBug.MeterSerial,
                    PriorityID = reportedBug.PriorityID,
                    PriorityReason = reportedBug.PriorityReason,
                    Screenshot = reportedBug.Screenshot,
                    StatusID = reportedBug.StatusID,
                    UserDescription = reportedBug.UserDescription,
                    UserID = reportedBug.UserID,
                    Username = userName,
                },
            };

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_BugAdmin.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Z_Bugs/Z_Bugs_BugAdmin")]
        public async Task<IActionResult> Z_Bugs_BugAdmin(Z_Bugs_BugAdminModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_Bugs_BugAdmin, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_Bugs_BugAdmin}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (!_operationalProvider.IsDeveloper)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            var reportedBug = db.ReportedBugs.Where(p => p.ID == _operationalProvider.SelectedBugID).SingleOrDefault();

            if (_operationalProvider.SelectedBugID == 0 || reportedBug == null)
                return Redirect("/operational/Z_Bugs/Z_Bugs_MyBugs");

            string userName = _userManager.FindByIdAsync(reportedBug.UserID).Result.UserName;
            var userProf = opProfs.Where(p => p.UserID == reportedBug.UserID).SingleOrDefault();
            if (userProf != null && !string.IsNullOrEmpty(userProf.FirstName))
                userName = $"{userProf.FirstName} {userProf.LastName}";

            model.StatusID = (from p in ((Data.ReportedBug.StatusEnum[])Enum.GetValues(typeof(Data.ReportedBug.StatusEnum)))
                              select new SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = ((int)p).ToString(),
                                  Selected = (int)p == reportedBug.StatusID,
                              }).ToList();
            model.IsSuccess = false;
            model.ReportedBug = new Z_Bugs_BugAdminModel.Z_Bugs_BugAdminItem()
            {
                CompanyID = reportedBug.CompanyID,
                CustomerNo = reportedBug.CustomerNo,
                DateCreated = reportedBug.DateCreated,
                ID = reportedBug.ID,
                LinkedSecureAreaID = reportedBug.LinkedSecureAreaID,
                MeterSerial = reportedBug.MeterSerial,
                PriorityID = reportedBug.PriorityID,
                PriorityReason = reportedBug.PriorityReason,
                Screenshot = reportedBug.Screenshot,
                StatusID = reportedBug.StatusID,
                UserDescription = reportedBug.UserDescription,
                UserID = reportedBug.UserID,
                Username = userName,
            };

            if (ModelState.IsValid)
            {
                Data.ReportedBugs_Comment reportedBugs_Comment = new ReportedBugs_Comment()
                {
                    DateCreated = DateTime.Now,
                    Screenshot = "",
                    UserDescription = model.UserDescription,
                    UserID = _userManager.GetUserId(User),
                    BugID = reportedBug.ID,
                };

                db.Add(reportedBugs_Comment);
                db.SaveChanges();

                StringBuilder sbSysLog = new StringBuilder();

                sbSysLog.AppendLine($"Developer Comment Added: '{model.UserDescription}'<br />");

                if (Convert.ToInt32(Request.Form["StatusID"]) != reportedBug.StatusID)
                {
                    sbSysLog.AppendLine($"StatusID from '{reportedBug.Status.GetDescription()}' to '{((Data.ReportedBug.StatusEnum)Convert.ToInt32(Request.Form["StatusID"])).GetDescription()}'.<br />");
                    reportedBug.StatusID = Convert.ToInt32(Request.Form["StatusID"]);
                    db.Update(reportedBug);
                    db.SaveChanges();
                }


                if (model.Screenshot != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "z-bugs-comments-attachments";
                    string dirName = $"{reportedBugs_Comment.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Screenshot.FileName);

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
                    model.Screenshot.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);


                    reportedBugs_Comment.Screenshot = fileName;
                    db.Update(reportedBugs_Comment);
                    db.SaveChanges();
                }

                Data.ReportedBugs_Log reportedBugs_Log = new ReportedBugs_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = sbSysLog.ToString(),
                    BugID = reportedBug.ID,
                    UserDescription = $"COMMENT,{reportedBugs_Comment.ID}",
                    UserID = _userManager.GetUserId(User),
                };

                db.Add(reportedBugs_Log);
                db.SaveChanges();


                model.IsSuccess = true;
            }

            return View("~/Views/Operational/Z_Bugs/Z_Bugs_BugAdmin.cshtml", model);
        }


    }
}
