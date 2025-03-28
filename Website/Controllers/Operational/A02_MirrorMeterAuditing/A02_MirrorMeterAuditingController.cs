using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
using MyVoltage.Models.OperationalModels.A02_MirrorMeterAuditing;
using MyVoltage.Models.OperationalModels.A02_MirrorMeterAuditing;
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
using System.Web;

namespace MyVoltage.Controllers.Operational.A02_MirrorMeterAuditing
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A02_MirrorMeterAuditingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public A02_MirrorMeterAuditingController(
            IEmailSender emailSender,
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
            _emailSender = emailSender;
        }

        #region MirrorReading

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingUpdate()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            DateTime dt = DateTime.Now;

            DateTime rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            if (dt.Minute > 30) // or just check dt.Minute >= 30 for regular rounding
                rounded = rounded.AddHours(1);

            A02_MirrorMeterAuditingModel_MirrorReadingUpdateModel model = new A02_MirrorMeterAuditingModel_MirrorReadingUpdateModel()
            {
                DateLogged = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeLogged = rounded.ToString("HH:mm")
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active && p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
                if (localDev != null)
                    model.RemoteAddress = localDev.RemoteAddress;

            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingUpdate(A02_MirrorMeterAuditingModel_MirrorReadingUpdateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate");

            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy-MM-dd";
            dateTimeFormatInfo.ShortTimePattern = "HH:mm";

            DateTime timeLoggedDate = new DateTime();
            if (!DateTime.TryParse(model.DateLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedDate))
                ModelState.AddModelError("DateLogged", "Date incorrect. (yyyy-MM-dd)");
            DateTime timeLoggedTime = new DateTime();
            if (!DateTime.TryParse(model.TimeLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedTime))
                ModelState.AddModelError("TimeLogged", "Time incorrect. Can only fall on the hour. (HH:00)");

            if (timeLoggedTime.Minute != 0)
                ModelState.AddModelError("TimeLogged", "Date time incorrect. Can only fall on the hour. (yyyy-MM-dd HH:00)");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo.Replace("/", "-")}".ToLower();
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Photo.FileName);
            fileName = fileName.ToLower();

            // Get a reference to a share and then create it
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            share.CreateIfNotExists();

            // Get a reference to a directory and create it
            ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
            var directoryTempResult = directoryTemp.CreateIfNotExists();

            ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(_operationalProvider.CompanyName.ToLower());
            var directoryCompanyResult = directoryCompany.CreateIfNotExists();

            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.Replace("/", "-").ToLower());
            var directoryResult = directory.CreateIfNotExists();

            // Get a reference to a file and upload it
            ShareFileClient file = directory.GetFileClient(fileName);

            // Copy the contents of the file to the request stream.
            Stream uploadFile = new MemoryStream();
            model.Photo.CopyTo(uploadFile);
            //byte[] fileContents = new byte[uploadFile.Length];
            uploadFile.Position = 0;
            //uploadFile.Read(fileContents, 0, fileContents.Length);

            file.Create(uploadFile.Length);
            file.Upload(uploadFile);

            #endregion

            #region DB Entry

            Data.A02_MirrorMeterAuditing_MirrorReadingUpdate a02_MirrorMeterAuditing_MirrorReadingUpdate = new A02_MirrorMeterAuditing_MirrorReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = _operationalProvider.CustomerMeterSerial,
                MirrorDeviceID = _operationalProvider.MirrorDevices.Where(p => p.Value.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault().Key,
                OdoReading = model.OdoReading,
                PhotoURL = $"{dirName}/{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedTime.Hour, timeLoggedTime.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified
            };

            using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                db.Add(a02_MirrorMeterAuditing_MirrorReadingUpdate);
                db.SaveChanges();
            }


            #endregion



            model.IsSuccessful = true;

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate_GetReadingAndDiff")]
        public JsonResult B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFill()
        {
            string latestLiveReading = "Unknown";
            string latestLiveReadingTime = "Unknown";
            string calculatedDifference = "Unknown";
            string differenceMessage = "";
            string latestM2MReading = "Unknown";
            string latestM2MReadingTime = "Unknown";
            string calculatedDifferenceM2M = "Unknown";
            string differenceMessageM2M = "";

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                int decimalPlaces = 0;
                if (_operationalProvider.CustomerMeterDeviceType == DeviceType.DeviceTypeEnum.Gas)
                    decimalPlaces = 3;

                System.Data.DataRow[] latestReading = dbCache.sp_GetAllDevicesLatestReading.Select($"Serial = '{_operationalProvider.CustomerMeterSerial}'");
                if (latestReading != null && latestReading.Length > 0)
                {

                    decimal latestLiveReadingValue = Convert.ToDecimal(latestReading[0]["VirtualOdometerReading"]);
                    latestLiveReading = latestLiveReadingValue.ToDecimal(decimalPlaces);
                    latestLiveReadingTime = Convert.ToDateTime(latestReading[0]["TimeLogged"]).ToDateAndTimeShort();

                    if (!string.IsNullOrEmpty(Request.Query["OdoReading"]))
                    {
                        decimal calculatedDifferenceValue = (Convert.ToDecimal(Request.Query["OdoReading"]) - latestLiveReadingValue);
                        calculatedDifference = calculatedDifferenceValue.ToDecimal(decimalPlaces);
                        if (calculatedDifferenceValue < 0)
                            calculatedDifferenceValue = calculatedDifferenceValue * -1.0m;

                        if (calculatedDifferenceValue < 1000)
                            differenceMessage = "<span class=\"text-green\">Reading looks okay.</span>";
                        else if (calculatedDifferenceValue >= 1000 && calculatedDifferenceValue < 5000)
                            differenceMessage = "<span class=\"text-orange\">Meter calibration will not be successfull.</span>";
                        else// (calculatedDifferenceValue > 10000)
                            differenceMessage = "<span class=\"text-red\">Meter has a issue. Investigate what is wrong. Correct issue before leaving site.</span>";
                    }
                }

                var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);
                if (m2mDevice != null)
                {
                    System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                    string start = DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:00:00");
                    string end = DateTime.Now.ToString("yyyy-MM-ddTHH:00:00");
                    Dictionary<int, string> registers = new Dictionary<int, string>();

                    switch ((DeviceType.DeviceTypeEnum)m2mDevice.type.id)
                    {
                        default:
                        case DeviceType.DeviceTypeEnum.Electricity:
                            registers.Add(1, "readings"); // Active Energy
                            break;
                        case DeviceType.DeviceTypeEnum.Gas:
                            registers.Add(140, "readings"); // Gas Consumption
                            break;
                        case DeviceType.DeviceTypeEnum.Water:
                            registers.Add(80, "readings"); // Water Consumption
                            break;
                    }

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }
                    string urlReadings = $"devices/{m2mDevice.id}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                    var resultReadings = _client.GetString(urlReadings, 2);
                    bool first = true;

                    foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (first)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableReadings.Columns.Add(safeName, colType);
                            }
                            first = false;
                            continue;
                        }

                        DataRow row = dataTableReadings.NewRow();
                        int colIndex = 0;
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            if (colIndex == 1)
                                row[colIndex] = Convert.ToDateTime(safeName);
                            else
                                row[colIndex] = safeName;
                            colIndex++;
                        }

                        dataTableReadings.Rows.Add(row);
                        dataTableReadings.AcceptChanges();
                    }

                    if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                    {
                        decimal latestLiveReadingValue = Convert.ToDecimal(dataTableReadings.Rows[0][2]);
                        latestM2MReading = latestLiveReadingValue.ToDecimal(decimalPlaces);
                        latestM2MReadingTime = Convert.ToDateTime(dataTableReadings.Rows[0][1]).ToDateAndTimeShort();

                        if (!string.IsNullOrEmpty(Request.Query["OdoReading"]))
                        {
                            decimal calculatedDifferenceValue = (Convert.ToDecimal(Request.Query["OdoReading"]) - latestLiveReadingValue);
                            calculatedDifferenceM2M = calculatedDifferenceValue.ToDecimal(decimalPlaces);
                            if (calculatedDifferenceValue < 0)
                                calculatedDifferenceValue = calculatedDifferenceValue * -1.0m;

                            if (calculatedDifferenceValue < 1000)
                                differenceMessageM2M = "<span class=\"text-green\">Reading looks okay.</span>";
                            else if (calculatedDifferenceValue >= 1000 && calculatedDifferenceValue < 5000)
                                differenceMessageM2M = "<span class=\"text-orange\">Meter calibration will not be successfull.</span>";
                            else// (calculatedDifferenceValue > 10000)
                                differenceMessageM2M = "<span class=\"text-red\">Meter has a issue. Investigate what is wrong. Correct issue before leaving site.</span>";
                        }
                    }
                }
            }

            return Json(
                new
                {
                    latestLiveReading = latestLiveReading,
                    latestLiveReadingTime = latestLiveReadingTime,
                    calculatedDifference = calculatedDifference,
                    differenceMessage = differenceMessage,
                    latestM2MReading = latestM2MReading,
                    latestM2MReadingTime = latestM2MReadingTime,
                    calculatedDifferenceM2M = calculatedDifferenceM2M,
                    differenceMessageM2M = differenceMessageM2M,
                }
                );//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdate_Search")]
        public JsonResult A02_MirrorMeterAuditing_MirrorReadingUpdate_Search(string serialOrName)
        {
            MVCache mVCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            return Json(mVCache.A02_MirrorMeterAuditing_MirrorReadingUpdate_Search(serialOrName));//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditingModel_MirrorReadingUpdateNoAccessModel model = new A02_MirrorMeterAuditingModel_MirrorReadingUpdateNoAccessModel()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active && p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (localDev != null)
                    model.RemoteAddress = localDev.RemoteAddress;
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess(A02_MirrorMeterAuditingModel_MirrorReadingUpdateNoAccessModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo}".ToLower();
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Photo.FileName);
            fileName = fileName.ToLower();

            // Get a reference to a share and then create it
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            share.CreateIfNotExists();

            // Get a reference to a directory and create it
            ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
            directoryTemp.CreateIfNotExists();
            ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(_operationalProvider.CompanyName.ToLower());
            directoryCompany.CreateIfNotExists();
            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.ToLower());
            directory.CreateIfNotExists();

            // Get a reference to a file and upload it
            ShareFileClient file = directory.GetFileClient(fileName);

            // Copy the contents of the file to the request stream.
            Stream uploadFile = new MemoryStream();
            model.Photo.CopyTo(uploadFile);
            //byte[] fileContents = new byte[uploadFile.Length];
            uploadFile.Position = 0;
            //uploadFile.Read(fileContents, 0, fileContents.Length);

            file.Create(uploadFile.Length);
            file.UploadRange(
                new HttpRange(0, uploadFile.Length),
                uploadFile);

            #endregion

            #region DB Entry

            DateTime timeLoggedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            Data.A02_MirrorMeterAuditing_MirrorReadingUpdate a02_MirrorMeterAuditing_MirrorReadingUpdate = new A02_MirrorMeterAuditing_MirrorReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = _operationalProvider.CustomerMeterSerial,
                MirrorDeviceID = _operationalProvider.MirrorDevices.Where(p => p.Value.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault().Key,
                OdoReading = 0,
                PhotoURL = $"{dirName}/{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedDate.Hour, timeLoggedDate.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.NoAccess
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            db.Add(a02_MirrorMeterAuditing_MirrorReadingUpdate);
            db.SaveChanges();


            #endregion

            #region SMS/Email Notification


            var customer = (from p in db.Customers
                            where p.CustomerNumber == _operationalProvider.CustomerNumber
                            && !p.IsDeleted
                            orderby p.CustomerID descending
                            select p).FirstOrDefault();

            if (customer != null)
            {
                if (!string.IsNullOrEmpty(customer.NotificationPhoneNumber) && customer.NotificationPhoneNumber.Length == 10)
                {
                    List<Log_Notification> log_Notifications = new List<Log_Notification>();
                    StringBuilder sbSMS = new StringBuilder();
                    sbSMS.AppendLine($"We are unable to gain access to meter {_operationalProvider.CustomerMeterSerial} at {_operationalProvider.CustomerNumber}.");
                    sbSMS.AppendLine($"Please take a photo of the meter and email it with the time of the photo to info@myvoltage.co.za");

                    log_Notifications.Add(new Log_Notification()
                    {
                        CompanyID = customer.CompanyID,
                        CustomerID = customer.CustomerID,
                        MessagePreview = sbSMS.ToString(),
                        Recipients = $"27{customer.NotificationPhoneNumber.Remove(0, 1)}",
                        TimeSent = DateTime.Now
                    });

                    SMS.SendBulkSMSSinglePerson(log_Notifications, _options, sbSMS.ToString());
                }

                if (!string.IsNullOrEmpty(customer.NotificationEmail))
                {
                    string emailBody = System.IO.File.ReadAllText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "noaccess_notice.txt"));

                    string url = string.Concat(
                      Request.Scheme,
                      "://",
                      Request.Host.ToUriComponent(),
                      Request.PathBase.ToUriComponent());

                    emailBody = emailBody.Replace("{name}", customer.FullName);
                    emailBody = emailBody.Replace("{serial}", _operationalProvider.CustomerMeterSerial);
                    emailBody = emailBody.Replace("{customerno}", _operationalProvider.CustomerNumber);
                    emailBody = emailBody.Replace("{url}", url);


                    List<Log_Notification> log_Notifications = new List<Log_Notification>();
                    log_Notifications.Add(new Log_Notification()
                    {
                        CompanyID = customer.CompanyID,
                        CustomerID = customer.CustomerID,
                        MessagePreview = HttpUtility.HtmlEncode(emailBody),
                        Recipients = $"{customer.FullName} <{customer.NotificationEmail}>",
                        TimeSent = DateTime.Now
                    });

                    Stream uploadFileEmail = new MemoryStream();
                    model.Photo.CopyTo(uploadFileEmail);
                    byte[] fileContentsEmail = new byte[uploadFileEmail.Length];
                    uploadFileEmail.Position = 0;
                    uploadFileEmail.Read(fileContentsEmail, 0, fileContentsEmail.Length);

                    await _emailSender.SendBulkEmailAsync(log_Notifications, _options, "Meter Access Problem", emailBody, emailBody, fileContentsEmail, System.IO.Path.GetFileName(model.Photo.FileName), model.Photo.ContentType);

                }





            }


            #endregion

            model.IsSuccessful = true;

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingUpdateNoAccess.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingSummary")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorReadingSummaryModel model = new A02_MirrorMeterAuditing_MirrorReadingSummaryModel()
            {
                A02_MirrorMeterAuditing_MirrorReadingSummaryItems = new List<A02_MirrorMeterAuditing_MirrorReadingSummaryItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            //System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorReadingResults = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
            MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomers = dbCache.SkybillCustomers.ToList();
            var A02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                A02_MirrorMeterAuditing_MirrorReadingSummaryItem item = new A02_MirrorMeterAuditing_MirrorReadingSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    Status = "N/A",
                    CustomersCount = (from p in skybillCustomers where p.CompanyID == company.CompanyID select p.Customer_No).Distinct().Count()
                };

                int checkedCount = 0;
                int unCheckedCount = 0;

                foreach (var skybillCustomer in skybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList())
                {
                    var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MeterSerial == skybillCustomer.Serial_No).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                    if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate != null)
                        switch (((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID))
                        {
                            case Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified:
                                unCheckedCount++;
                                break;
                            case Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified:
                                checkedCount++;
                                break;
                        }

                }

                item.CustomersCheckedCount = checkedCount;
                item.CustomersNotCheckedCount = unCheckedCount;

                if (unCheckedCount > 0)
                {
                    item.Status = "Outstanding";
                }
                else if (checkedCount > 0)
                {
                    item.Status = "Verified";
                }
                else
                {
                    item.Status = "N/A";
                }

                //foreach (DataRow dr in sp_A02_MirrorMeterAuditing_MirrorReadingResults.Rows)
                //{
                //    var skybillCustomer = (from p in skybillCustomers
                //                           where p.Serial_No == dr["Serial"].ToString()
                //                           select p).FirstOrDefault();

                //    if (skybillCustomer == null || skybillCustomer.CompanyID != company.CompanyID)
                //        continue;

                //}


                model.A02_MirrorMeterAuditing_MirrorReadingSummaryItems.Add(item);
            }
            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDetails")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorReadingDetailsModel model = new A02_MirrorMeterAuditing_MirrorReadingDetailsModel()
            {
                A02_MirrorMeterAuditing_MirrorReadingDetailsItems = new PaginatedList<A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem>(new List<A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0
            };
            var A02_MirrorMeterAuditing_MirrorReadingDetailsItems = new List<A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem>();

            if (_operationalProvider.CompanyID != 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorReadingDetails = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
                MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        select p).ToList();
                var devices = (from p in db.Devices
                               where p.CompanyID.HasValue && p.CompanyID == _operationalProvider.CompanyID
                               && p.ActiveStatusID.HasValue && p.ActiveStatusID == 1
                               select p).ToList();

                SqlConnection connA02_MirrorMeterAuditing_MirrorReadingUpdates = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates = new SqlCommand("sp_A02_MirrorMeterAuditing_MirrorReadingUpdates", connA02_MirrorMeterAuditing_MirrorReadingUpdates);
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.CommandTimeout = 5000;
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.Parameters.AddWithValue("@CompanyID", _operationalProvider.CompanyID);

                var sp_A02_MirrorMeterAuditing_MirrorReadingUpdates = new System.Data.DataTable();

                connA02_MirrorMeterAuditing_MirrorReadingUpdates.Open();
                new SqlDataAdapter(sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates).Fill(sp_A02_MirrorMeterAuditing_MirrorReadingUpdates);
                connA02_MirrorMeterAuditing_MirrorReadingUpdates.Close();

                foreach (DataRow dr in sp_A02_MirrorMeterAuditing_MirrorReadingDetails.Rows)
                {
                    var skybillCustomer = (from p in skybillCustomers
                                           where p.Serial_No == dr["Serial"].ToString()
                                           select p).FirstOrDefault();

                    if (skybillCustomer == null)
                        continue;

                    var lDev = devices.Where(p => p.Serial == skybillCustomer.Serial_No).FirstOrDefault();

                    A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem item = new A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem()
                    {
                        Serial = dr["Serial"].ToString(),
                        Status = Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.None,
                        SkybillCustomer = skybillCustomer,
                        DeviceType = lDev != null ? lDev.MeterType : DeviceType.DeviceTypeEnum.Unknown,
                    };

                    if (dr["Name"] != DBNull.Value)
                        item.DeviceName = dr["Name"].ToString();

                    if (dr["VirtualOdometerReading"] != DBNull.Value)
                        item.LatestReading = Convert.ToDecimal(dr["VirtualOdometerReading"]);

                    if (dr["TimeLogged"] != DBNull.Value)
                        item.LatestReadingTime = Convert.ToDateTime(dr["TimeLogged"]);

                    if (dr["OdometerReading"] != DBNull.Value)
                        item.LatestAuditReading = Convert.ToDecimal(dr["OdometerReading"]);

                    if (dr["OdoTimeLogged"] != DBNull.Value)
                        item.LatestAuditReadingTime = Convert.ToDateTime(dr["OdoTimeLogged"]);

                    if (dr["CreateDate"] != DBNull.Value)
                        item.LatestAuditReadingCreated = Convert.ToDateTime(dr["CreateDate"]);

                    if (dr["AuditName"] != DBNull.Value)
                        item.LatestAuditReadingName = dr["AuditName"].ToString();

                    if (dr["AuditUploadName"] != DBNull.Value)
                        item.LatestAuditReadingUploadName = dr["AuditUploadName"].ToString();

                    var filteredRows = sp_A02_MirrorMeterAuditing_MirrorReadingUpdates.AsEnumerable()
                           .Where(row => row.Field<string>("MeterSerial") == dr["Serial"].ToString()
                                       && row.Field<Int64>("MirrorDeviceID") == Convert.ToInt64(dr["Id"]));

                    if (filteredRows.Any())
                    {
                        var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = filteredRows.OrderByDescending(row => row.Field<DateTime>("DateCreated")).First();

                        if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate != null)
                        {
                            item.Status = ((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)Convert.ToInt32(latestA02_MirrorMeterAuditing_MirrorReadingUpdate["StatusID"]));
                            if (item.Status == Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified)
                                A02_MirrorMeterAuditing_MirrorReadingDetailsItems.Add(item);
                        }
                    }


                }
                A02_MirrorMeterAuditing_MirrorReadingDetailsItems = A02_MirrorMeterAuditing_MirrorReadingDetailsItems.OrderBy(p => p.SkybillCustomer.No).ToList();
            }

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = A02_MirrorMeterAuditing_MirrorReadingDetailsItems.Count;
            model.A02_MirrorMeterAuditing_MirrorReadingDetailsItems = await PaginatedList<A02_MirrorMeterAuditing_MirrorReadingDetailsModel.A02_MirrorMeterAuditing_MirrorReadingDetailsItem>.CreateAsync(A02_MirrorMeterAuditing_MirrorReadingDetailsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDetails.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingResults")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorReadingResultsModel model = new A02_MirrorMeterAuditing_MirrorReadingResultsModel()
            {
                A02_MirrorMeterAuditing_MirrorReadingResultsItems = new PaginatedList<A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem>(new List<A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0
            };
            var A02_MirrorMeterAuditing_MirrorReadingResultsItems = new List<A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem>();

            if (_operationalProvider.CompanyID != 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorReadingResults = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
                MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        select p).ToList();

                var devices = (from p in db.Devices
                               where p.CompanyID.HasValue && p.CompanyID == _operationalProvider.CompanyID
                               && p.ActiveStatusID.HasValue && p.ActiveStatusID == 1
                               select p).ToList();

                SqlConnection connA02_MirrorMeterAuditing_MirrorReadingUpdates = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates = new SqlCommand("sp_A02_MirrorMeterAuditing_MirrorReadingUpdates", connA02_MirrorMeterAuditing_MirrorReadingUpdates);
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.CommandTimeout = 5000;
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates.Parameters.AddWithValue("@CompanyID", _operationalProvider.CompanyID);

                var sp_A02_MirrorMeterAuditing_MirrorReadingUpdates = new System.Data.DataTable();

                connA02_MirrorMeterAuditing_MirrorReadingUpdates.Open();
                new SqlDataAdapter(sqlCommandA02_MirrorMeterAuditing_MirrorReadingUpdates).Fill(sp_A02_MirrorMeterAuditing_MirrorReadingUpdates);
                connA02_MirrorMeterAuditing_MirrorReadingUpdates.Close();

                foreach (DataRow dr in sp_A02_MirrorMeterAuditing_MirrorReadingResults.Rows)
                {
                    var skybillCustomer = (from p in dbCache.SkybillCustomers
                                           where p.Serial_No == dr["Serial"].ToString()
                                           select p).FirstOrDefault();

                    if (skybillCustomer == null || skybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;

                    var lDev = devices.Where(p => p.Serial == skybillCustomer.Serial_No).FirstOrDefault();

                    A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem item = new A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem()
                    {
                        Serial = dr["Serial"].ToString(),
                        Status = Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.None,
                        SkybillCustomer = skybillCustomer,
                        DeviceType = lDev != null ? lDev.MeterType : DeviceType.DeviceTypeEnum.Unknown,
                    };

                    if (dr["Name"] != DBNull.Value)
                        item.DeviceName = dr["Name"].ToString();

                    if (dr["VirtualOdometerReading"] != DBNull.Value)
                        item.LatestReading = Convert.ToDecimal(dr["VirtualOdometerReading"]);

                    if (dr["TimeLogged"] != DBNull.Value)
                        item.LatestReadingTime = Convert.ToDateTime(dr["TimeLogged"]);

                    if (dr["OdometerReading"] != DBNull.Value)
                        item.LatestAuditReading = Convert.ToDecimal(dr["OdometerReading"]);

                    if (dr["OdoTimeLogged"] != DBNull.Value)
                        item.LatestAuditReadingTime = Convert.ToDateTime(dr["OdoTimeLogged"]);

                    if (dr["CreateDate"] != DBNull.Value)
                        item.LatestAuditReadingCreated = Convert.ToDateTime(dr["CreateDate"]);

                    if (dr["AuditName"] != DBNull.Value)
                        item.LatestAuditReadingName = dr["AuditName"].ToString();

                    if (dr["AuditUploadName"] != DBNull.Value)
                        item.LatestAuditReadingUploadName = dr["AuditUploadName"].ToString();

                    var filteredRows = sp_A02_MirrorMeterAuditing_MirrorReadingUpdates.AsEnumerable()
                           .Where(row => row.Field<string>("MeterSerial") == dr["Serial"].ToString()
                                       && row.Field<Int64>("MirrorDeviceID") == Convert.ToInt64(dr["Id"]));

                    if (filteredRows.Any())
                    {
                        var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = filteredRows.OrderByDescending(row => row.Field<DateTime>("DateCreated")).First();

                        if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate != null)
                        {
                            item.Status = ((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)Convert.ToInt32(latestA02_MirrorMeterAuditing_MirrorReadingUpdate["StatusID"]));
                        }
                    }

                    A02_MirrorMeterAuditing_MirrorReadingResultsItems.Add(item);
                }

                A02_MirrorMeterAuditing_MirrorReadingResultsItems = A02_MirrorMeterAuditing_MirrorReadingResultsItems.OrderBy(p => p.SkybillCustomer.No).ToList();
            }

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = A02_MirrorMeterAuditing_MirrorReadingResultsItems.Count;
            model.A02_MirrorMeterAuditing_MirrorReadingResultsItems = await PaginatedList<A02_MirrorMeterAuditing_MirrorReadingResultsModel.A02_MirrorMeterAuditing_MirrorReadingResultsItem>.CreateAsync(A02_MirrorMeterAuditing_MirrorReadingResultsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingVerification()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingVerification, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingVerification}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorReadingVerificationModel model = new A02_MirrorMeterAuditing_MirrorReadingVerificationModel()
            {
            };


            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                System.Data.DataRow[] latestReading = dbCache.sp_GetAllDevicesLatestReading.Select($"Serial = '{_operationalProvider.CustomerMeterSerial}'");
                if (latestReading != null && latestReading.Length > 0)
                {
                    model.LatestReading = Convert.ToDecimal(latestReading[0]["VirtualOdometerReading"]);
                    model.LatestReadingDateTime = Convert.ToDateTime(latestReading[0]["TimeLogged"]);
                }
                var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MeterSerial == _operationalProvider.CustomerMeterSerial).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate != null)
                {
                    model.LatestReadingOdo = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.OdoReading;
                    model.LatestReadingOdoCreatedBy = _userManager.FindByIdAsync(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.UserID).Result.Email;
                    model.LatestReadingDateTimeOdo = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged;
                    model.LatestReadingOdoCreatedDateTime = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.DateCreated;
                    model.LatestReadingOdoPhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ID}";
                    model.A02_MirrorMeterAuditing_MirrorReadingUpdateID = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ID;
                    model.Status = (MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID;
                    model.ChangedDate = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.DateChanged;

                    switch (model.Status)
                    {
                        case Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified:
                            var actionToRemove = model.A02_MirrorMeterAuditing_MirrorReadingVerificationActions.Where(p => p.ActionID == 1).SingleOrDefault();
                            model.A02_MirrorMeterAuditing_MirrorReadingVerificationActions.Remove(actionToRemove);
                            break;
                    }

                    if (!string.IsNullOrEmpty(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ChangedByID))
                    {
                        model.ChangedByUserName = _userManager.FindByIdAsync(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ChangedByID).Result.UserName;
                    }

                    var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);
                    if (m2mDevice != null)
                    {
                        System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                        string start = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged.AddHours(-1).ToString("yyyy-MM-ddTHH:00:00");
                        string end = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged.ToString("yyyy-MM-ddTHH:00:00");
                        Dictionary<int, string> registers = new Dictionary<int, string>();


                        switch ((DeviceType.DeviceTypeEnum)m2mDevice.type.id)
                        {
                            default:
                            case DeviceType.DeviceTypeEnum.Electricity:
                                registers.Add(1, "readings"); // Active Energy
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                registers.Add(140, "readings"); // Gas Consumption
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                registers.Add(80, "readings"); // Water Consumption
                                break;
                        }

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }
                        string urlReadings = $"devices/{m2mDevice.id}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                        var resultReadings = _client.GetString(urlReadings, 2);
                        bool first = true;

                        foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (first)
                            {
                                foreach (var lineVar in fileLine.Split(','))
                                {
                                    string safeName = lineVar.Replace("\"", string.Empty);
                                    Type colType = typeof(string);

                                    if (safeName == "Time Logged")
                                        colType = typeof(DateTime);

                                    dataTableReadings.Columns.Add(safeName, colType);
                                }
                                first = false;
                                continue;
                            }

                            DataRow row = dataTableReadings.NewRow();
                            int colIndex = 0;
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                if (colIndex == 1)
                                    row[colIndex] = Convert.ToDateTime(safeName);
                                else
                                    row[colIndex] = safeName;
                                colIndex++;
                            }

                            dataTableReadings.Rows.Add(row);
                            dataTableReadings.AcceptChanges();
                        }

                        if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                        {
                            if (dataTableReadings.Columns.Count > 2)
                                try { model.VerificationReading = Convert.ToDecimal(dataTableReadings.Rows[0][2]); } catch { }
                            if (dataTableReadings.Columns.Count > 1)
                                try { model.VerificationReadingDateTime = Convert.ToDateTime(dataTableReadings.Rows[0][1]); } catch { }

                        }
                    }

                    //var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.MirrorDeviceID && p.TimeLogged == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged.AddHours(-1)).SingleOrDefault();
                    //if (verificationReading != null)
                    //{
                    //    model.VerificationReading = verificationReading.VirtualOdometerReading;
                    //    model.VerificationReadingDateTime = verificationReading.TimeLogged;
                    //}


                    var previousOdo = (from p in apiDB.OdoReadings
                                       where p.DeviceId == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.MirrorDeviceID
                                       orderby p.TimeLogged descending
                                       select p).FirstOrDefault();

                    if (previousOdo != null)
                    {
                        var previousDeviceReading = (from p in apiDB.DeviceReadings
                                                     where p.DeviceId == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.MirrorDeviceID
                                                     && p.TimeLogged == previousOdo.TimeLogged
                                                     select p).FirstOrDefault();

                        model.PreviousOdoID = previousOdo.Id;
                        model.PreviousReadingDateTimeOdo = previousOdo.TimeLogged;
                        model.PreviousReadingOdo = previousOdo.OdometerReading;
                        model.PreviousReadingOdoCreatedDateTime = previousOdo.CreateDate;
                        model.PreviousOdoRequiresRecalc = previousOdo.RequiresRecalc;
                        model.PreviousVerificationReading = previousDeviceReading != null ? previousDeviceReading.VirtualOdometerReading : 0;
                        if (previousDeviceReading != null)
                            model.PreviousVerificationReadingDateTime = previousDeviceReading.TimeLogged;
                    }
                }

            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{A02_MirrorMeterAuditing_MirrorReadingUpdateID}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingVerification_Photo(int A02_MirrorMeterAuditing_MirrorReadingUpdateID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var item = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.ID == A02_MirrorMeterAuditing_MirrorReadingUpdateID).SingleOrDefault();

            if (item != null)
            {
                var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No == item.MeterSerial).FirstOrDefault();
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault();
                string shareName = "a02-mirrorreadingupdates";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
                if (directoryTemp.Exists())
                {
                    ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(company.Name.ToLower());
                    if (directoryCompany.Exists())
                    {
                        ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(sC.No.Replace("/", "-").ToLower());
                        if (directory.Exists())
                        {
                            ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.PhotoURL).ToLower());

                            if (file.Exists())
                            {
                                // Download the file
                                ShareFileDownloadInfo download = file.Download();
                                Stream uploadFile = new MemoryStream();
                                download.Content.CopyTo(uploadFile);
                                uploadFile.Position = 0;
                                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                                string contentType;
                                if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.PhotoURL), out contentType))
                                {
                                    contentType = "application/octet-stream";
                                }

                                if (uploadFile != null)
                                    return File(uploadFile, contentType, System.IO.Path.GetFileName(item.PhotoURL));
                            }
                        }
                    }
                }

                ShareDirectoryClient directoryCompany2 = share.GetDirectoryClient(company.Name.ToLower());
                if (directoryCompany2.Exists())
                {
                    ShareDirectoryClient directory = directoryCompany2.GetSubdirectoryClient(sC.No.Replace("/", "-").ToLower());
                    if (directory.Exists())
                    {
                        ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.PhotoURL).ToLower());

                        if (file.Exists())
                        {
                            // Download the file
                            ShareFileDownloadInfo download = file.Download();
                            Stream uploadFile = new MemoryStream();
                            download.Content.CopyTo(uploadFile);
                            uploadFile.Position = 0;
                            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                            string contentType;
                            if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.PhotoURL), out contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            if (uploadFile != null)
                                return File(uploadFile, contentType, System.IO.Path.GetFileName(item.PhotoURL));
                        }
                    }
                }
            }

            return Content("The file you are looking for cannot be found.");
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Action/{actionID}/{A02_MirrorMeterAuditing_MirrorReadingUpdateID}/{OdoID?}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingVerification_Action(int actionID, int A02_MirrorMeterAuditing_MirrorReadingUpdateID, int? OdoID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.ID == A02_MirrorMeterAuditing_MirrorReadingUpdateID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification");

            var modelForActions = new A02_MirrorMeterAuditing_MirrorReadingVerificationModel()
            {
                Status = (Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)item.StatusID
            };

            //var action = modelForActions.A02_MirrorMeterAuditing_MirrorReadingVerificationActions.Where(p => p.ActionID == actionID).SingleOrDefault();

            //if (action == null)
            //    return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDetails");

            if (item != null)
            {
                var sC = db.SkybillCustomers.Where(p => p.Serial_No == item.MeterSerial).FirstOrDefault();
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault();
                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                var userEditing = _userManager.FindByIdAsync(_userManager.GetUserId(User)).Result;
                var userWhoAdded = _userManager.FindByIdAsync(item.UserID).Result;

                switch (actionID)
                {
                    // Verify
                    case 1:
                        var existingOdo = apiDB.OdoReadings.Where(p => p.DeviceId == item.MirrorDeviceID && p.TimeLogged == item.TimeLogged).FirstOrDefault();

                        if (existingOdo != null)
                        {
                            // Update existing odo
                            existingOdo.OdometerReading = item.OdoReading;
                            existingOdo.RequiresRecalc = true;
                            apiDB.Update(existingOdo);
                        }
                        else
                        {
                            // Add New odo
                            MyVoltageApi.Data.OdoReading odo = new OdoReading()
                            {
                                AuditName = userWhoAdded.Email,
                                AuditUploadName = userEditing.Email,
                                CreateDate = DateTime.Now,
                                DeviceId = item.MirrorDeviceID,
                                OdometerReading = item.OdoReading,
                                ReasonForDiff = "",
                                RequiresRecalc = true,
                                TimeLogged = item.TimeLogged
                            };

                            apiDB.Add(odo);
                        }

                        apiDB.SaveChanges();

                        //#region FTP Upload

                        //string shareName = "a02-mirrorreadingupdates";
                        //string dirName = $"00Temp/{company.Name}/{sC.No}";

                        //// Get a reference to the file
                        //ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        //ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
                        //directoryTemp.CreateIfNotExists();
                        //ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(company.Name.ToLower());
                        //directoryCompany.CreateIfNotExists();
                        //ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(sC.No.ToLower());
                        //directory.CreateIfNotExists();
                        //ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.PhotoURL).ToLower());

                        //// Download the file
                        //ShareFileDownloadInfo download = file.Download();
                        //Stream uploadFile = new MemoryStream();
                        //download.Content.CopyTo(uploadFile);
                        //uploadFile.Position = 0;

                        //if (uploadFile != null)
                        //{
                        //    string dirUrl = $"{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo}";

                        //    // Get a reference to a directory and create it
                        //    ShareDirectoryClient directoryFinalCompany = share.GetDirectoryClient(_operationalProvider.CompanyName.ToLower());
                        //    directoryFinalCompany.CreateIfNotExists();
                        //    ShareDirectoryClient directoryFinal = directoryFinalCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.ToLower());
                        //    directoryFinal.CreateIfNotExists();

                        //    string fileName = item.MeterSerial + item.TimeLogged.ToString("_yyyy_MM_dd_HH_mm") + System.IO.Path.GetExtension(item.PhotoURL);
                        //    fileName = fileName.ToLower();

                        //    // Get a reference to a file and upload it
                        //    ShareFileClient fileFinal = directoryFinal.GetFileClient(fileName);

                        //    // Copy the contents of the file to the request stream.
                        //    Stream uploadFileFinal = new MemoryStream();
                        //    uploadFile.CopyTo(uploadFileFinal);
                        //    //byte[] fileContents = new byte[uploadFileFinal.Length];
                        //    uploadFileFinal.Position = 0;
                        //    //uploadFileFinal.Read(fileContents, 0, fileContents.Length);

                        //    fileFinal.Create(uploadFileFinal.Length);
                        //    fileFinal.Upload(uploadFileFinal);

                        //    item.PhotoURL = $"{dirUrl}/{fileName}";

                        //    //file.DeleteIfExists();
                        //}

                        //#endregion

                        item.StatusID = (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified;
                        break;
                    // Reject
                    case 2:
                        item.StatusID = (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected;

                        break;
                    // Recalc
                    case 3:
                        var odoToUpdate = apiDB.OdoReadings.Where(p => p.Id == OdoID.Value).SingleOrDefault();
                        if (odoToUpdate != null)
                        {
                            odoToUpdate.RequiresRecalc = true;
                            apiDB.Update(odoToUpdate);
                            apiDB.SaveChanges();
                        }

                        break;
                }

                item.ChangedByID = userEditing.Id;
                item.DateChanged = DateTime.Now;
                db.Update(item);
                db.SaveChanges();
            }


            return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification");
        }



        #endregion

        #region Meter Calibration

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationSummary")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationSummary.cshtml");


        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MeterCalibrationSummaryItem model = new A02_MirrorMeterAuditing_MeterCalibrationSummaryItem()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var skybillCustomerMeters = (from p in dbCache.SkybillCustomers
                                         where p.CompanyID == companyID
                                         && !p.No.ToUpper().Contains("KVA")
                                         && !p.Serial_No.ToUpper().Contains("KVA")
                                         && !p.No.ToUpper().Contains("OFFPEAK")
                                         && !p.Serial_No.ToUpper().Contains("OFFPEAK")
                                         && !p.No.ToUpper().Contains("PEAK")
                                         && !p.Serial_No.ToUpper().Contains("PEAK")
                                         && !p.No.ToUpper().Contains("STANDARD")
                                         && !p.Serial_No.ToUpper().Contains("STANDARD")
                                         select p.Serial_No).Distinct().ToList();

            model.CustomersCount = (from p in dbCache.SkybillCustomers
                                    where p.CompanyID == companyID
                                    && !p.No.ToUpper().Contains("KVA")
                                    && !p.Serial_No.ToUpper().Contains("KVA")
                                    && !p.No.ToUpper().Contains("OFFPEAK")
                                    && !p.Serial_No.ToUpper().Contains("OFFPEAK")
                                    && !p.No.ToUpper().Contains("PEAK")
                                    && !p.Serial_No.ToUpper().Contains("PEAK")
                                    && !p.No.ToUpper().Contains("STANDARD")
                                    && !p.Serial_No.ToUpper().Contains("STANDARD")
                                    select p.Customer_No).Distinct().Count();

            int meterCount = 0;
            int calibratedCount = 0;
            int notCalibratedCount = 0;
            int problematicCount = 0;
            int metersThatCannotBeCalibratedCount = 0;

            var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.ToList();
            var a10_VirtualMeters = db.A10_VirtualMeters.ToList();

            foreach (var skybillMeter in skybillCustomerMeters)
            {
                var vMeterCustomers = (from p in a10_VirtualMeterCustomers
                                       where p.MirrorSerial == skybillMeter
                                       select p).ToList();

                var vMeter = (from p in a10_VirtualMeters
                              where p.SerialNumber == skybillMeter
                              || vMeterCustomers.Select(c => c.A10_VirtualMeterID).Contains(p.ID)
                              select p).Count();

                if (vMeter > 0 || vMeterCustomers.Count > 0)
                    continue;

                var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == skybillMeter).FirstOrDefault();
                if (mirrorDevice != null)
                {
                    meterCount++;
                    var a02_MirrorMeterAuditing_MeterCalibrationVerification = dbCache.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mirrorDevice.Id).SingleOrDefault();
                    if (a02_MirrorMeterAuditing_MeterCalibrationVerification != null)
                    {
                        switch ((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)a02_MirrorMeterAuditing_MeterCalibrationVerification.StatusID)
                        {
                            case Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated:
                                calibratedCount++;
                                break;
                            case Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Problematic:
                                problematicCount++;
                                break;
                            case Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.UnCalibrated:
                                notCalibratedCount++;
                                break;
                        }
                    }
                    else
                        metersThatCannotBeCalibratedCount++;
                }
            }


            model.MetersProblematicCount = problematicCount;
            model.MetersCount = meterCount;
            model.MetersCalibratedCount = calibratedCount;
            model.MetersNotCalibratedCount = notCalibratedCount;
            model.MetersThatCannotBeCalibratedCount = metersThatCannotBeCalibratedCount;
            model.Status = calibratedCount == meterCount ? Models.OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType.Reviewed : Models.OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType.Outstanding;
            model.TableRowID = $"CalibrationSummaryItem_{companyID}";

            return PartialView("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationDetails")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A02_MirrorMeterAuditing_MeterCalibrationDetailModel model = new A02_MirrorMeterAuditing_MeterCalibrationDetailModel()
            {
                A02_MirrorMeterAuditing_MeterCalibrationDetailItems = new PaginatedList<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>(new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0,
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            int currentCOunt = 0;

            var a02_MirrorMeterAuditing_MeterCalibrationDetailItems = new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>();
            if (_operationalProvider.CompanyID > 0)
            {
                var skybillCustomers = (from p in dbCache.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        && !p.No.ToUpper().Contains("KVA")
                                        && !p.Serial_No.ToUpper().Contains("KVA")
                                        && !p.No.ToUpper().Contains("OFFPEAK")
                                        && !p.Serial_No.ToUpper().Contains("OFFPEAK")
                                        && !p.No.ToUpper().Contains("PEAK")
                                        && !p.Serial_No.ToUpper().Contains("PEAK")
                                        && !p.No.ToUpper().Contains("STANDARD")
                                        && !p.Serial_No.ToUpper().Contains("STANDARD")
                                        orderby p.Customer_No
                                        select p).ToList();

                var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.ToList();
                var a10_VirtualMeters = db.A10_VirtualMeters.ToList();

                List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer> A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers = new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>();


                foreach (var skybillMeter in skybillCustomers)
                {
                    var existing = (from p in A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers
                                    where p.CustomerMeterSerial == skybillMeter.Serial_No
                                    select p).SingleOrDefault();

                    if (existing != null)
                        continue;

                    var vMeterCustomers = (from p in a10_VirtualMeterCustomers
                                           where p.MirrorSerial == skybillMeter.Serial_No
                                           select p).ToList();

                    var vMeter = (from p in a10_VirtualMeters
                                  where p.SerialNumber == skybillMeter.Serial_No
                                  || vMeterCustomers.Select(c => c.A10_VirtualMeterID).Contains(p.ID)
                                  select p).Count();

                    if (vMeter > 0 || vMeterCustomers.Count > 0)
                        continue;

                    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == skybillMeter.Serial_No).FirstOrDefault();
                    if (mirrorDevice != null)
                    {
                        currentCOunt++;
                        A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer itemToAdd = new A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer()
                        {
                            CompanyName = _operationalProvider.CompanyName,
                            CustomerMeterName = skybillMeter.No,
                            CustomerNo = skybillMeter.Customer_No,
                            CustomerMeterSerial = skybillMeter.Serial_No,
                            Status = Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.None,
                            MirrorDeviceID = mirrorDevice.Id,
                        };

                        var a02_MirrorMeterAuditing_MeterCalibrationVerification = dbCache.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mirrorDevice.Id).SingleOrDefault();
                        if (a02_MirrorMeterAuditing_MeterCalibrationVerification != null)
                        {
                            itemToAdd.CalibrationDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.CalibrationVerificationDate;
                            itemToAdd.Status = ((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)a02_MirrorMeterAuditing_MeterCalibrationVerification.StatusID);
                            itemToAdd.ExpiryDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.ExpireDate;
                        }

                        //var latestOdo = dbCache.MirrorOdoReadings.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                        //if (latestOdo != null)
                        //{
                        //    itemToAdd.LatestReadingOdo = latestOdo.OdometerReading;
                        //    var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == mirrorDevice.Id && p.TimeLogged == latestOdo.TimeLogged.AddHours(-1)).FirstOrDefault();
                        //    if (verificationReading != null)
                        //        itemToAdd.VerificationReading = verificationReading.VirtualOdometerReading;
                        //}

                        if (itemToAdd.Status != Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated
                            && itemToAdd.Status != Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.InProgress)
                        {
                            A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.Add(itemToAdd);
                        }
                    }
                }


                A02_MirrorMeterAuditing_MeterCalibrationDetailItem item = new A02_MirrorMeterAuditing_MeterCalibrationDetailItem()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CompanyName = _operationalProvider.CompanyName,
                    A02_MirrorMeterAuditing_MeterCalibrationDetailItems = A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.OrderBy(p => p.CustomerNo).ToList(),
                };

                a02_MirrorMeterAuditing_MeterCalibrationDetailItems.AddRange(A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.OrderBy(p => p.CustomerNo).ToList());
            }

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = a02_MirrorMeterAuditing_MeterCalibrationDetailItems.Count;
            model.A02_MirrorMeterAuditing_MeterCalibrationDetailItems = await PaginatedList<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>.CreateAsync(a02_MirrorMeterAuditing_MeterCalibrationDetailItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationResults")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A02_MirrorMeterAuditing_MeterCalibrationDetailModel model = new A02_MirrorMeterAuditing_MeterCalibrationDetailModel()
            {
                A02_MirrorMeterAuditing_MeterCalibrationDetailItems = new PaginatedList<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>(new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0,
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            int currentCOunt = 0;
            var a02_MirrorMeterAuditing_MeterCalibrationDetailItems = new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>();

            if (_operationalProvider.CompanyID > 0)
            {
                var skybillCustomers = (from p in dbCache.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        && !p.No.ToUpper().Contains("KVA")
                                        && !p.Serial_No.ToUpper().Contains("KVA")
                                        && !p.No.ToUpper().Contains("OFFPEAK")
                                        && !p.Serial_No.ToUpper().Contains("OFFPEAK")
                                        && !p.No.ToUpper().Contains("PEAK")
                                        && !p.Serial_No.ToUpper().Contains("PEAK")
                                        && !p.No.ToUpper().Contains("STANDARD")
                                        && !p.Serial_No.ToUpper().Contains("STANDARD")
                                        orderby p.Customer_No
                                        select p).ToList();

                var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.ToList();
                var a10_VirtualMeters = db.A10_VirtualMeters.ToList();

                List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer> A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers = new List<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>();


                foreach (var skybillMeter in skybillCustomers)
                {
                    var existing = (from p in A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers
                                    where p.CustomerMeterSerial == skybillMeter.Serial_No
                                    select p).SingleOrDefault();

                    if (existing != null)
                        continue;

                    var vMeterCustomers = (from p in a10_VirtualMeterCustomers
                                           where p.MirrorSerial == skybillMeter.Serial_No
                                           select p).ToList();

                    var vMeter = (from p in a10_VirtualMeters
                                  where p.SerialNumber == skybillMeter.Serial_No
                                  || vMeterCustomers.Select(c => c.A10_VirtualMeterID).Contains(p.ID)
                                  select p).Count();

                    if (vMeter > 0 || vMeterCustomers.Count > 0)
                        continue;

                    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == skybillMeter.Serial_No).FirstOrDefault();
                    if (mirrorDevice != null)
                    {
                        currentCOunt++;
                        A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer itemToAdd = new A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer()
                        {
                            CompanyName = _operationalProvider.CompanyName,
                            CustomerMeterName = skybillMeter.No,
                            CustomerNo = skybillMeter.Customer_No,
                            CustomerMeterSerial = skybillMeter.Serial_No,
                            Status = Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.None,
                            MirrorDeviceID = mirrorDevice.Id,
                        };

                        var a02_MirrorMeterAuditing_MeterCalibrationVerification = dbCache.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mirrorDevice.Id).SingleOrDefault();
                        if (a02_MirrorMeterAuditing_MeterCalibrationVerification != null)
                        {
                            itemToAdd.CalibrationDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.CalibrationVerificationDate;
                            itemToAdd.Status = ((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)a02_MirrorMeterAuditing_MeterCalibrationVerification.StatusID);
                            itemToAdd.ExpiryDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.ExpireDate;
                        }

                        //var latestOdo = dbCache.MirrorOdoReadings.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                        //if (latestOdo != null)
                        //{
                        //    itemToAdd.LatestReadingOdo = latestOdo.OdometerReading;
                        //    var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == mirrorDevice.Id && p.TimeLogged == latestOdo.TimeLogged.AddHours(-1)).FirstOrDefault();
                        //    if (verificationReading != null)
                        //        itemToAdd.VerificationReading = verificationReading.VirtualOdometerReading;
                        //}

                        A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.Add(itemToAdd);
                    }
                }


                A02_MirrorMeterAuditing_MeterCalibrationDetailItem item = new A02_MirrorMeterAuditing_MeterCalibrationDetailItem()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CompanyName = _operationalProvider.CompanyName,
                    A02_MirrorMeterAuditing_MeterCalibrationDetailItems = A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.OrderBy(p => p.CustomerNo).ToList(),
                };

                a02_MirrorMeterAuditing_MeterCalibrationDetailItems.AddRange(A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomers.OrderBy(p => p.CustomerNo).ToList());
            }

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = a02_MirrorMeterAuditing_MeterCalibrationDetailItems.Count;
            model.A02_MirrorMeterAuditing_MeterCalibrationDetailItems = await PaginatedList<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer>.CreateAsync(a02_MirrorMeterAuditing_MeterCalibrationDetailItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationResults.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibration_GetMirrorReading")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GetRegisters()
        {
            var A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
            {
                LatestReadingOdo = "",
                LatestReadingOdoTime = "",
                VerificationReading = "",
                CalculatedDifferenceReading = "",
            };

            if (!string.IsNullOrEmpty(Request.Form["deviceid"]))
            {
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var latestReadings = dbCache.sp_GetAllDevicesLatestOdo;
                DataRow[] latestReadingResults = latestReadings.Select($"Id = '{Request.Form["deviceid"]}'");
                if (latestReadingResults.Length > 0)
                {
                    // M2M Reading
                    decimal? verificationReadingValue = 0;
                    var m2mDevice = _client.GetDeviceByMeterNumber(latestReadingResults[0]["Serial"].ToString());
                    if (m2mDevice != null)
                    {
                        System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                        string start = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).AddHours(-2).ToString("yyyy-MM-ddTHH:00:00");
                        string end = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).AddHours(-1).ToString("yyyy-MM-ddTHH:00:00");
                        Dictionary<int, string> registers = new Dictionary<int, string>();


                        switch ((DeviceType.DeviceTypeEnum)m2mDevice.type.id)
                        {
                            default:
                            case DeviceType.DeviceTypeEnum.Electricity:
                                registers.Add(1, "readings"); // Active Energy
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                registers.Add(140, "readings"); // Gas Consumption
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                registers.Add(80, "readings"); // Water Consumption
                                break;
                        }

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }
                        string urlReadings = $"devices/{m2mDevice.id}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                        var resultReadings = _client.GetString(urlReadings, 2);
                        bool first = true;

                        foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (first)
                            {
                                foreach (var lineVar in fileLine.Split(','))
                                {
                                    string safeName = lineVar.Replace("\"", string.Empty);
                                    Type colType = typeof(string);

                                    if (safeName == "Time Logged")
                                        colType = typeof(DateTime);

                                    dataTableReadings.Columns.Add(safeName, colType);
                                }
                                first = false;
                                continue;
                            }

                            DataRow row = dataTableReadings.NewRow();
                            int colIndex = 0;
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                if (colIndex == 1)
                                    row[colIndex] = Convert.ToDateTime(safeName);
                                else
                                    row[colIndex] = safeName;
                                colIndex++;
                            }

                            dataTableReadings.Rows.Add(row);
                            dataTableReadings.AcceptChanges();
                        }

                        if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                        {
                            if (dataTableReadings.Columns.Count > 2)
                                try { verificationReadingValue = Convert.ToDecimal(dataTableReadings.Rows[0][2]); } catch { }
                            //if (dataTableReadings.Columns.Count > 1)
                            //    try { model.VerificationReadingDateTime = Convert.ToDateTime(dataTableReadings.Rows[0][1]); } catch { }

                        }
                    }



                    //var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == Convert.ToInt32(Request.Form["deviceid"]) && p.TimeLogged == Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).AddHours(-1)).FirstOrDefault();
                    A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
                    {
                        LatestReadingOdo = Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]).ToReading(),
                        LatestReadingOdoTime = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).ToDateAndTimeShort(),
                        VerificationReading = verificationReadingValue != null ? verificationReadingValue.ToReading() : 0.0m.ToReading(),
                        CalculatedDifferenceReading = (Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]) - (verificationReadingValue != null ? verificationReadingValue : 0)).ToReading(),
                    };
                    MyVoltageDbContext db = new MyVoltageDbContext(_options);
                    var lDev = db.Devices.Where(p => p.Serial == latestReadingResults[0]["Serial"].ToString() && p.ActiveStatusID.HasValue && p.ActiveStatusID == 1).FirstOrDefault();
                    if (lDev != null && lDev.MeterType == DeviceType.DeviceTypeEnum.Gas)
                    {
                        A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
                        {
                            LatestReadingOdo = Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]).ToDecimal(3),
                            LatestReadingOdoTime = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).ToDateAndTimeShort(),
                            VerificationReading = verificationReadingValue != null ? verificationReadingValue.ToDecimal(3) : 0.0m.ToDecimal(3),
                            CalculatedDifferenceReading = (Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]) - (verificationReadingValue != null ? verificationReadingValue : 0)).ToDecimal(3),
                        };
                    }
                }
            }
            return Json(A01_GatewayAndDeviceMonitoring_GetRegistersResult);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationVerification")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationVerification()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationVerification, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationVerification}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MeterCalibrationVerificationModel model = new A02_MirrorMeterAuditing_MeterCalibrationVerificationModel()
            {
                Status = Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.UnCalibrated,
            };


            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var mDev = dbCache.MirrorDevices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (mDev != null)
                {
                    var latestA02_MirrorMeterAuditing_MeterCalibrationUpdate = dbCache.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mDev.Id).FirstOrDefault();
                    if (latestA02_MirrorMeterAuditing_MeterCalibrationUpdate != null)
                    {
                        model.A02_MirrorMeterAuditing_MeterCalibrationUpdateID = latestA02_MirrorMeterAuditing_MeterCalibrationUpdate.ID;
                        model.Status = (Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)latestA02_MirrorMeterAuditing_MeterCalibrationUpdate.StatusID;
                    }
                }
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationVerification.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationVerification_Action/{actionID}/{A02_MirrorMeterAuditing_MeterCalibrationUpdateID}/{*reasonForOverride}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MeterCalibrationVerification_Action(int actionID, int A02_MirrorMeterAuditing_MeterCalibrationUpdateID, string reasonForOverride)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var item = db.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.ID == A02_MirrorMeterAuditing_MeterCalibrationUpdateID).SingleOrDefault();
            string userID = _operationalProvider.OperationalProfile.UserID;
            string userName = !string.IsNullOrEmpty(_operationalProvider.OperationalProfile.FirstName) ? $"{_operationalProvider.OperationalProfile.FirstName} {_operationalProvider.OperationalProfile.LastName}" : _userManager.GetUserName(User);
            if (item == null)
                return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationDetails");

            var modelForActions = new A02_MirrorMeterAuditing_MeterCalibrationVerificationModel()
            {
                Status = (Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)item.StatusID,
            };

            var action = modelForActions.A02_MirrorMeterAuditing_MeterCalibrationVerificationActions.Where(p => p.ActionID == actionID).SingleOrDefault();

            if (action == null)
                return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationDetails");

            if (item != null)
            {
                switch (action.ActionID)
                {
                    #region Confirmed Calibration

                    case 1:
                        // Skybill Customer lookup for company
                        var skybillCustomer = (from p in dbCache.SkybillCustomers
                                               where p.Serial_No == _operationalProvider.CustomerMeterSerial
                                               select p).FirstOrDefault();
                        var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                        if (skybillCustomer != null)
                        {
                            item.CompanyID = skybillCustomer.CompanyID;
                            company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                        }
                        else
                            item.CompanyID = _operationalProvider.CompanyID;

                        var apiDevice = dbCache.MirrorDevices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                        if (apiDevice != null)
                        {
                            item.SerialNumber = apiDevice.Serial;
                            item.Name = apiDevice.Name;

                            var apiDB = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);
                            var latestOdo = apiDB.OdoReadings.Where(p => p.DeviceId == apiDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                            if (latestOdo != null)
                            {
                                item.LatestOdoReading = latestOdo.OdometerReading;
                                item.LatestOdoTimeLogged = latestOdo.TimeLogged;
                            }
                        }

                        var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);
                        if (m2mDevice != null)
                        {
                            item.DeviceSerialLinked = m2mDevice.serial;
                            item.DeviceIDLinked = m2mDevice.id;
                            item.DeviceNameLinked = m2mDevice.name;

                            var deviceGatewayAndConfigs = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

                            if (deviceGatewayAndConfigs.device != null && deviceGatewayAndConfigs.device.gateways != null && deviceGatewayAndConfigs.device.gateways.Length > 0)
                            {
                                item.TypeName = deviceGatewayAndConfigs.device.type.name;
                                item.GatewayID = deviceGatewayAndConfigs.device.gateways[0].id;

                                if (deviceGatewayAndConfigs.device.gateways[0].mapping != null)
                                {
                                    item.Port = deviceGatewayAndConfigs.device.gateways[0].mapping.port.ToString();
                                    item.RemoteIndex = deviceGatewayAndConfigs.device.gateways[0].mapping.remote_index.ToString();
                                    item.RemoteAddress = deviceGatewayAndConfigs.device.gateways[0].mapping.remote_address.ToString();

                                    if (deviceGatewayAndConfigs.device.configurations.Length > 0 && deviceGatewayAndConfigs.device.configurations[0].value != null)
                                    {
                                        item.Config6Value = deviceGatewayAndConfigs.device.configurations[0].value.value;
                                    }
                                }
                            }
                        }

                        item.CalibrationVerificationDate = DateTime.Now;
                        item.CalibrationVerificationName = userName;
                        item.StatusID = (int)Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated;

                        if (company.CalibrationValidDays.HasValue && company.CalibrationValidDays.Value != 0)
                        {
                            item.ExpireDate = item.LatestOdoTimeLogged.HasValue ? item.LatestOdoTimeLogged.Value.AddDays(company.CalibrationValidDays.Value) : DateTime.Now.AddDays(company.CalibrationValidDays.Value);
                        }

                        if (reasonForOverride != null && !string.IsNullOrEmpty(reasonForOverride))
                        {
                            item.ReasonForOverride = reasonForOverride;
                            item.OverridedByID = userID;
                        }

                        db.Update(item);
                        db.SaveChanges();

                        // Clear cache
                        _cache.Remove(MVCache.KEY_A02_MirrorMeterAuditing_MeterCalibrationVerifications);
                        break;

                    #endregion

                    case 2:
                        item.CalibrationVerificationDate = DateTime.Now;
                        item.CalibrationVerificationName = userName;
                        item.StatusID = (int)Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Problematic;

                        if (reasonForOverride != null && !string.IsNullOrEmpty(reasonForOverride))
                        {
                            item.ReasonForOverride = reasonForOverride;
                            item.OverridedByID = userID;
                        }

                        db.Update(item);
                        db.SaveChanges();
                        break;
                    case 3:
                        item.CalibrationVerificationDate = DateTime.Now;
                        item.CalibrationVerificationName = userName;
                        item.StatusID = (int)Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.InProgress;

                        if (reasonForOverride != null && !string.IsNullOrEmpty(reasonForOverride))
                        {
                            item.ReasonForOverride = reasonForOverride;
                            item.OverridedByID = userID;
                        }

                        db.Update(item);
                        db.SaveChanges();
                        break;
                }
            }

            return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MeterCalibrationDetails");
        }

        #endregion

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistSummary")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorChecklistSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistSummary, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistSummary}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            A02_MirrorMeterAuditing_MirrorChecklistSummaryModel model = new A02_MirrorMeterAuditing_MirrorChecklistSummaryModel()
            {
                A02_MirrorMeterAuditing_MirrorChecklistSummaryItems = new List<A02_MirrorMeterAuditing_MirrorChecklistSummaryItem>(),
            };

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var sbCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList();

                var mirrorDevices = (from p in dbCache.MirrorDevices
                                     where sbCustomers.Select(d => d.Serial_No).Contains(p.Serial)
                                     select p).ToList();

                var capturesToday = (from p in dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates
                                     where p.DateCreated.Date == DateTime.Now.Date
                                     && mirrorDevices.Select(d => d.Serial).Contains(p.MeterSerial)
                                     select p).ToList();

                A02_MirrorMeterAuditing_MirrorChecklistSummaryItem item = new A02_MirrorMeterAuditing_MirrorChecklistSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    CustomersCount = sbCustomers.Select(p => p.Customer_No).Distinct().Count(),
                    MetersCount = mirrorDevices.Select(p => p.Serial).Distinct().Count(),
                    MetersCapturedCount = capturesToday.Count,
                };

                if (item.MetersCount > 0)
                    model.A02_MirrorMeterAuditing_MirrorChecklistSummaryItems.Add(item);
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistDetails")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorChecklistDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorChecklistDetailsModel model = new A02_MirrorMeterAuditing_MirrorChecklistDetailsModel()
            {
                A02_MirrorMeterAuditing_MirrorChecklistDetailsItems = new List<A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var m2mDevices = dbCache.M2MDevices.ToList();
                System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorChecklistDetails = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
                MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var localDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID == _operationalProvider.CompanyID && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();

                foreach (DataRow dr in sp_A02_MirrorMeterAuditing_MirrorChecklistDetails.Rows)
                {
                    var skybillCustomer = (from p in dbCache.SkybillCustomers
                                           where p.Serial_No == dr["Serial"].ToString()
                                           && p.CompanyID != 120 // 000.SUPPLY
                                           select p).FirstOrDefault();

                    if (skybillCustomer == null || skybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;

                    A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem item = new A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem()
                    {
                        Serial = dr["Serial"].ToString(),
                        LatestEntryStatus = Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.None,
                        SkybillCustomer = skybillCustomer,
                        Status = A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Outstanding,
                        DeviceStatus = "Unknown",
                        DeviceType = DeviceType.DeviceTypeEnum.Unknown,
                    };

                    var latestUpdateToday = (from p in dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates
                                             where p.DateCreated.Date == DateTime.Now.Date
                                             && p.MeterSerial == dr["Serial"].ToString()
                                             orderby p.DateCreated descending
                                             select p).FirstOrDefault();

                    if (latestUpdateToday != null)
                    {
                        item.LatestEntryStatus = (Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestUpdateToday.StatusID;
                        if (latestUpdateToday.StatusID != (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected
                            && latestUpdateToday.StatusID != (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Deleted)
                        {
                            item.Status = A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Completed;
                        }

                    }

                    var localDev = localDevices.Where(p => p.Serial == skybillCustomer.Serial_No).FirstOrDefault();
                    if (localDev != null)
                    {
                        item.DeviceName = localDev.Name;
                        item.DeviceType = localDev.MeterType;
                    }

                    if (dr["VirtualOdometerReading"] != DBNull.Value)
                        item.LatestReading = Convert.ToDecimal(dr["VirtualOdometerReading"]);

                    if (dr["TimeLogged"] != DBNull.Value)
                        item.LatestReadingTime = Convert.ToDateTime(dr["TimeLogged"]);

                    if (dr["OdometerReading"] != DBNull.Value)
                        item.LatestAuditReading = Convert.ToDecimal(dr["OdometerReading"]);

                    if (dr["OdoTimeLogged"] != DBNull.Value)
                        item.LatestAuditReadingTime = Convert.ToDateTime(dr["OdoTimeLogged"]);

                    if (dr["CreateDate"] != DBNull.Value)
                        item.LatestAuditReadingCreated = Convert.ToDateTime(dr["CreateDate"]);

                    if (dr["AuditName"] != DBNull.Value)
                        item.LatestAuditReadingName = dr["AuditName"].ToString();

                    if (dr["AuditUploadName"] != DBNull.Value)
                        item.LatestAuditReadingUploadName = dr["AuditUploadName"].ToString();

                    if (item.Status == A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Outstanding)
                    {
                        var m2mDev = m2mDevices.Where(p => p.serial == dr["Serial"].ToString()).FirstOrDefault();
                        if (m2mDev != null)
                            item.DeviceStatus = m2mDev.deviceStatus;


                        model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems.Add(item);
                    }
                }

                model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems = model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems.OrderBy(p => p.SkybillCustomer.No).ToList();
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistResults")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorChecklistResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorChecklistDetailsModel model = new A02_MirrorMeterAuditing_MirrorChecklistDetailsModel()
            {
                A02_MirrorMeterAuditing_MirrorChecklistDetailsItems = new List<A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorChecklistDetails = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
                MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var localDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID == _operationalProvider.CompanyID && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();

                foreach (DataRow dr in sp_A02_MirrorMeterAuditing_MirrorChecklistDetails.Rows)
                {
                    var skybillCustomer = (from p in dbCache.SkybillCustomers
                                           where p.Serial_No == dr["Serial"].ToString()
                                           && p.CompanyID != 120 // 000.SUPPLY
                                           select p).FirstOrDefault();

                    if (skybillCustomer == null || skybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;

                    A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem item = new A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem()
                    {
                        Serial = dr["Serial"].ToString(),
                        LatestEntryStatus = Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.None,
                        SkybillCustomer = skybillCustomer,
                        Status = A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Outstanding,
                        DeviceType = DeviceType.DeviceTypeEnum.Unknown,
                    };

                    var latestUpdateToday = (from p in dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates
                                             where p.DateCreated.Date == DateTime.Now.Date
                                             && p.MeterSerial == dr["Serial"].ToString()
                                             orderby p.DateCreated descending
                                             select p).FirstOrDefault();

                    if (latestUpdateToday != null)
                    {
                        item.LatestEntryStatus = (Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestUpdateToday.StatusID;
                        if (latestUpdateToday.StatusID != (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected
                            && latestUpdateToday.StatusID != (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Deleted)
                        {
                            item.Status = A02_MirrorMeterAuditing_MirrorChecklistDetailsModel.A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Completed;
                        }

                    }

                    var localDev = localDevices.Where(p => p.Serial == skybillCustomer.Serial_No).FirstOrDefault();
                    if (localDev != null)
                    {
                        item.DeviceName = localDev.Name;
                        item.DeviceType = localDev.MeterType;
                    }

                    if (dr["VirtualOdometerReading"] != DBNull.Value)
                        item.LatestReading = Convert.ToDecimal(dr["VirtualOdometerReading"]);

                    if (dr["TimeLogged"] != DBNull.Value)
                        item.LatestReadingTime = Convert.ToDateTime(dr["TimeLogged"]);

                    if (dr["OdometerReading"] != DBNull.Value)
                        item.LatestAuditReading = Convert.ToDecimal(dr["OdometerReading"]);

                    if (dr["OdoTimeLogged"] != DBNull.Value)
                        item.LatestAuditReadingTime = Convert.ToDateTime(dr["OdoTimeLogged"]);

                    if (dr["CreateDate"] != DBNull.Value)
                        item.LatestAuditReadingCreated = Convert.ToDateTime(dr["CreateDate"]);

                    if (dr["AuditName"] != DBNull.Value)
                        item.LatestAuditReadingName = dr["AuditName"].ToString();

                    if (dr["AuditUploadName"] != DBNull.Value)
                        item.LatestAuditReadingUploadName = dr["AuditUploadName"].ToString();

                    model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems.Add(item);
                }

                model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems = model.A02_MirrorMeterAuditing_MirrorChecklistDetailsItems.OrderBy(p => p.SkybillCustomer.No).ToList();
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorChecklistResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSearch")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceSearch()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSearch, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSearch}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorDeviceSearchModel model = new A02_MirrorMeterAuditing_MirrorDeviceSearchModel()
            {
                A02_MirrorMeterAuditing_MirrorDeviceSearchItems = new List<A02_MirrorMeterAuditing_MirrorDeviceSearchModel.A02_MirrorMeterAuditing_MirrorDeviceSearchItem>(),
                SearchTerm = "",
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            if (!string.IsNullOrEmpty(Request.Query["txtSearch"]))
            {
                model.SearchTerm = Request.Query["txtSearch"].ToString().ToLower();

                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.Customer_No.ToLower().Contains(model.SearchTerm)
                                   || p.No.ToLower().Contains(model.SearchTerm)
                                   || p.Customer_Name.ToLower().Contains(model.SearchTerm)
                                   || p.Serial_No.ToLower().Contains(model.SearchTerm)
                                   select p).ToList();

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No))
                        continue;
                    serialsChecked.Add(sC.Serial_No);

                    var mirrorDevice = mirrorDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || (ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;



                    A02_MirrorMeterAuditing_MirrorDeviceSearchModel.A02_MirrorMeterAuditing_MirrorDeviceSearchItem item = new A02_MirrorMeterAuditing_MirrorDeviceSearchModel.A02_MirrorMeterAuditing_MirrorDeviceSearchItem()
                    {
                        A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault(),
                        Device = localDev,
                        MirrorDevice = mirrorDevice,
                        OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault(),
                        SkybillCustomer = sC,
                        LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceSearchModel.A02_MirrorMeterAuditing_MirrorDeviceSearchItem.LatestReadingItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null,
                        },
                        Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault(),
                    };


                    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
                    if (latestReadingResults.Length > 0)
                        item.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceSearchModel.A02_MirrorMeterAuditing_MirrorDeviceSearchItem.LatestReadingItem()
                        {
                            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                        };


                    model.A02_MirrorMeterAuditing_MirrorDeviceSearchItems.Add(item);

                    if (model.A02_MirrorMeterAuditing_MirrorDeviceSearchItems.Count == 20)
                        break;
                }


                model.A02_MirrorMeterAuditing_MirrorDeviceSearchItems = model.A02_MirrorMeterAuditing_MirrorDeviceSearchItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();

            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSearch.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSummary")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorDeviceSummaryModel model = new A02_MirrorMeterAuditing_MirrorDeviceSummaryModel()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name,
                TableRowID = $"CalibrationSummaryItem_{companyID}",
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            if (companyID > 0)
            {
                var sbCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID).ToList();
                model.CustomersCount = (from p in sbCustomers
                                        select p.Customer_No).Distinct().ToList().Count;

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No))
                        continue;
                    serialsChecked.Add(sC.Serial_No);

                    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    //var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                    //if (m2mDevice == null)
                    //    m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                    //string serialToUse = sC.Serial_No;

                    //if (m2mDevice != null)
                    //    serialToUse = m2mDevice.serial;

                    //var localDev = dbCache.Devices.Where(p => p.Serial == serialToUse).FirstOrDefault();

                    //if (localDev == null || !localDev.ActiveStatusID.HasValue || (ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                    //    continue;

                    model.MetersCount++;
                }


            }

            return PartialView("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceSummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A02_MirrorMeterAuditing_MirrorDeviceDetailsModel model = new A02_MirrorMeterAuditing_MirrorDeviceDetailsModel()
            {
                A02_MirrorMeterAuditing_MirrorDeviceDetailsItems = new List<A02_MirrorMeterAuditing_MirrorDeviceDetailsModel.A02_MirrorMeterAuditing_MirrorDeviceDetailsItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            if (_operationalProvider.CompanyID > 0)
            {
                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No.ToUpper()))
                        continue;
                    serialsChecked.Add(sC.Serial_No.ToUpper());

                    var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                    if (m2mDevice == null)
                        m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                    string serialToUse = sC.Serial_No;

                    if (m2mDevice != null)
                        serialToUse = m2mDevice.serial;

                    var mirrorDevice = mirrorDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    var localDev = localDevices.Where(p => p.Serial.ToUpper() == serialToUse.ToUpper()).FirstOrDefault();

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || (ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    A02_MirrorMeterAuditing_MirrorDeviceDetailsModel.A02_MirrorMeterAuditing_MirrorDeviceDetailsItem item = new A02_MirrorMeterAuditing_MirrorDeviceDetailsModel.A02_MirrorMeterAuditing_MirrorDeviceDetailsItem()
                    {
                        A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault(),
                        Device = localDev,
                        MirrorDevice = mirrorDevice,
                        OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault(),
                        SkybillCustomer = sC,
                        LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceDetailsModel.A02_MirrorMeterAuditing_MirrorDeviceDetailsItem.LatestReadingItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null,
                        },
                        Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault(),
                    };


                    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
                    if (latestReadingResults.Length > 0)
                        item.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceDetailsModel.A02_MirrorMeterAuditing_MirrorDeviceDetailsItem.LatestReadingItem()
                        {
                            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                        };


                    model.A02_MirrorMeterAuditing_MirrorDeviceDetailsItems.Add(item);
                }

                model.A02_MirrorMeterAuditing_MirrorDeviceDetailsItems = model.A02_MirrorMeterAuditing_MirrorDeviceDetailsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            // TODO: Only allow edit on correcting factor.
            A02_MirrorMeterAuditing_MirrorDeviceReviewModel model = new A02_MirrorMeterAuditing_MirrorDeviceReviewModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00),
                ToDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 00, 00, 00),
                MirrorDeviceID = 0,
                ConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]" },
                },
                ConvertedConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]" },
                },
            };

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial, 2);
            if (m2mDevice == null)
                m2mDevice = _client.GetDeviceByReference(_operationalProvider.CustomerMeterSerial, 2);

            string serialToUse = _operationalProvider.CustomerMeterSerial;

            if (m2mDevice != null)
                serialToUse = m2mDevice.serial;

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            if (mirrorDevice == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            var localDev = dbCache.Devices.Where(p => p.Serial.ToUpper() == serialToUse.ToUpper() && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
            if (localDev == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            if (sC == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            var companyTarrifs = skyBillApiClient.GetTarrifsForCompany();

            var distinctTarrifCodes = companyTarrifs.Select(p => p.Resource_No).Distinct();
            foreach (var code in distinctTarrifCodes)
            {
                var tarrif = companyTarrifs.Where(p => p.Resource_No == code).FirstOrDefault();
                model.ConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = !string.IsNullOrEmpty(mirrorDevice.ConsumptionTariffCode) && mirrorDevice.ConsumptionTariffCode == code,
                });
                model.ConvertedConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = !string.IsNullOrEmpty(mirrorDevice.ConvertedConsumptionTariffCode) && mirrorDevice.ConvertedConsumptionTariffCode == code,
                });
            }

            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            model.MirrorDeviceID = mirrorDevice.Id;
            model.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
            model.Device = localDev;
            model.MirrorDevice = mirrorDevice;
            model.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
            model.SkybillCustomer = sC;
            model.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceReviewModel.LatestReadingItem()
            {
                TimeLogged = null,
                VirtualOdoReading = null
            };
            model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault();

            model.DeviceIDLinked = mirrorDevice.DeviceIDLinked;
            model.SerialNumber = mirrorDevice.Serial;
            model.Name = !string.IsNullOrEmpty(mirrorDevice.Name) ? mirrorDevice.Name : localDev.Name;
            model.CorrectingFactor = mirrorDevice.CorrectingFactor;
            model.ConvFactor = mirrorDevice.ConvFactor;

            DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
            if (latestReadingResults.Length > 0)
                model.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceReviewModel.LatestReadingItem()
                {
                    TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                    VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                };



            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceReview(A02_MirrorMeterAuditing_MirrorDeviceReviewModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial, 2);
            if (m2mDevice == null)
                m2mDevice = _client.GetDeviceByReference(_operationalProvider.CustomerMeterSerial, 2);

            string serialToUse = _operationalProvider.CustomerMeterSerial;

            if (m2mDevice != null)
                serialToUse = m2mDevice.serial;

            var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            if (mirrorDevice == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            var localDev = dbCache.Devices.Where(p => p.Serial.ToUpper() == serialToUse.ToUpper() && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
            if (localDev == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            if (sC == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceDetails");

            model.ConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]", Selected = string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) },
            };
            model.ConvertedConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]", Selected = string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) },
            };
            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            var companyTarrifs = skyBillApiClient.GetTarrifsForCompany();

            var distinctTarrifCodes = companyTarrifs.Select(p => p.Resource_No).Distinct();
            foreach (var code in distinctTarrifCodes)
            {
                var tarrif = companyTarrifs.Where(p => p.Resource_No == code).FirstOrDefault();
                model.ConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = !string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) && Request.Form["ConsumptionTariffCode"].ToString() == code,
                });
                model.ConvertedConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = !string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) && Request.Form["ConvertedConsumptionTariffCode"].ToString() == code,
                });
            }

            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            model.MirrorDeviceID = mirrorDevice.Id;
            model.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
            model.Device = localDev;
            model.MirrorDevice = mirrorDevice;
            model.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
            model.SkybillCustomer = sC;
            model.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceReviewModel.LatestReadingItem()
            {
                TimeLogged = null,
                VirtualOdoReading = null
            };
            model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault();

            model.SerialNumber = mirrorDevice.Serial;
            model.Name = !string.IsNullOrEmpty(mirrorDevice.Name) ? mirrorDevice.Name : localDev.Name;
            model.CorrectingFactor = mirrorDevice.CorrectingFactor;

            DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
            if (latestReadingResults.Length > 0)
                model.LatestReading = new A02_MirrorMeterAuditing_MirrorDeviceReviewModel.LatestReadingItem()
                {
                    TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                    VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                };

            if (_client.GetDeviceByID(model.DeviceIDLinked, 2) == null)
            {
                ModelState.AddModelError("DeviceIDLinked", "Device ID Linked not found on Metering DB");
            }

            if (ModelState.IsValid)
            {
                var apidb = new MyVoltageApiDbContext(_APIoptions);
                var deviceToUpdate = apidb.Devices.Where(p => p.Id == mirrorDevice.Id).SingleOrDefault();

                if (mirrorDevice.ConvFactor != model.ConvFactor)
                    deviceToUpdate.ConvFactor = model.ConvFactor;
                if (mirrorDevice.DeviceIDLinked != model.DeviceIDLinked)
                    deviceToUpdate.DeviceIDLinked = model.DeviceIDLinked;

                if (!string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) && Request.Form["ConsumptionTariffCode"].ToString() != mirrorDevice.ConsumptionTariffCode)
                    deviceToUpdate.ConsumptionTariffCode = Request.Form["ConsumptionTariffCode"].ToString();

                if (!string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) && Request.Form["ConvertedConsumptionTariffCode"].ToString() != mirrorDevice.ConvertedConsumptionTariffCode)
                    deviceToUpdate.ConvertedConsumptionTariffCode = Request.Form["ConvertedConsumptionTariffCode"].ToString();

                apidb.Update(deviceToUpdate);
                apidb.SaveChanges();
                _cache.Remove(MVCache.KEY_MirrorDevices);

                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview");
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceAdd")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceAdd()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_MirrorDeviceAddModel model = new A02_MirrorMeterAuditing_MirrorDeviceAddModel()
            {
                SerialNumber = "",
            };


            if (!string.IsNullOrEmpty(Request.Query["txtSearch"]))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                model.SerialNumber = Request.Query["txtSearch"];
                model.m2mDevice = _client.GetDeviceByMeterNumber(model.SerialNumber, 2);
                if (model.m2mDevice == null)
                    model.m2mDevice = _client.GetDeviceByReference(model.SerialNumber, 2);

                if (model.m2mDevice != null)
                {
                    model.SkybillCustomer = dbCache.SkybillCustomers.Where(p => p.Serial_No == model.SerialNumber).FirstOrDefault();
                    if (model.SkybillCustomer != null)
                        model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == model.SkybillCustomer.CompanyID).FirstOrDefault();
                    model.Device = dbCache.Devices.Where(p => p.DeviceIDLinked == model.m2mDevice.id && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
                    model.MirrorDevice = dbCache.MirrorDevices.Where(p => p.DeviceIDLinked == model.m2mDevice.id).FirstOrDefault();
                }
            }


            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceAdd.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceAdd/{*m2mSerial}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorDeviceAdd(string m2mSerial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (m2mSerial != null && !string.IsNullOrEmpty(m2mSerial))
            {
                var m2mDevice = _client.GetDeviceByMeterNumber(m2mSerial, 2);
                if (m2mDevice == null)
                    m2mDevice = _client.GetDeviceByReference(m2mSerial, 2);

                if (m2mDevice != null)
                {
                    MyVoltageApi.Data.Device newDev = new MyVoltageApi.Data.Device()
                    {
                        CorrectingFactor = 1,
                        CreateDate = DateTime.Now,
                        DeviceIDLinked = m2mDevice.id,
                        DeviceSerialLinked = m2mDevice.serial,
                        Name = m2mDevice.name,
                        Serial = m2mSerial,
                    };

                    var db = new MyVoltageApiDbContext(_APIoptions);
                    db.Add(newDev);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_MirrorDevices);
                    _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES);
                    return Redirect($"/operational/ChangeActiveMeter/{m2mSerial}?R={HttpUtility.UrlEncode($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceReview")}");
                }
            }


            return Redirect($"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorDeviceAdd?txtSearch={m2mSerial}&Err=T");
        }


        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingDelete()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete}/{(int)SecureAreaActionEnum.View}");

            #endregion

            DateTime dt = DateTime.Now;

            DateTime rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            if (dt.Minute > 30) // or just check dt.Minute >= 30 for regular rounding
                rounded = rounded.AddHours(1);

            A02_MirrorMeterAuditing_MirrorReadingDeleteModel model = new A02_MirrorMeterAuditing_MirrorReadingDeleteModel()
            {
                DateLogged = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeLogged = rounded.ToString("HH:mm")
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active && p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (localDev != null)
                    model.RemoteAddress = localDev.RemoteAddress;

            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_MirrorReadingDelete(A02_MirrorMeterAuditing_MirrorReadingDeleteModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete");

            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy-MM-dd";
            dateTimeFormatInfo.ShortTimePattern = "HH:mm";

            DateTime timeLoggedDate = new DateTime();
            if (!DateTime.TryParse(model.DateLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedDate))
                ModelState.AddModelError("DateLogged", "Date incorrect. (yyyy-MM-dd)");
            DateTime timeLoggedTime = new DateTime();
            if (!DateTime.TryParse(model.TimeLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedTime))
                ModelState.AddModelError("TimeLogged", "Time incorrect. Can only fall on the hour. (HH:00)");

            if (timeLoggedTime.Minute != 0)
                ModelState.AddModelError("TimeLogged", "Date time incorrect. Can only fall on the hour. (yyyy-MM-dd HH:00)");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete.cshtml", model);


            #region DB Entry

            using (MyVoltageApiDbContext db = new MyVoltageApiDbContext(_APIoptions))
            {
                DateTime timeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedTime.Hour, 0, 0);
                var device = (from p in db.Devices
                              where p.Serial == _operationalProvider.CustomerMeterSerial
                              select p).FirstOrDefault();
                if (device != null)
                {
                    var readings = (from p in db.DeviceReadings
                                    where p.DeviceId == device.Id
                                    && p.TimeLogged >= timeLogged
                                    select p).ToList();

                    model.RowsRemoved = readings.Count;

                    db.RemoveRange(readings);
                    db.SaveChanges();
                }
            }


            #endregion

            model.IsSuccessful = true;

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingDelete.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconSummary")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_M2MMirrorReconSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_M2MMirrorReconSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A02_MirrorMeterAuditing_M2MMirrorReconSummaryModel model = new A02_MirrorMeterAuditing_M2MMirrorReconSummaryModel()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name,
                TableRowID = $"CalibrationSummaryItem_{companyID}",
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            if (companyID > 0)
            {
                var sbCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID).ToList();
                model.CustomersCount = (from p in sbCustomers
                                        select p.Customer_No).Distinct().ToList().Count;

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No))
                        continue;
                    serialsChecked.Add(sC.Serial_No);

                    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    //var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                    //if (m2mDevice == null)
                    //    m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                    //string serialToUse = sC.Serial_No;

                    //if (m2mDevice != null)
                    //    serialToUse = m2mDevice.serial;

                    //var localDev = dbCache.Devices.Where(p => p.Serial == serialToUse).FirstOrDefault();

                    //if (localDev == null || !localDev.ActiveStatusID.HasValue || (ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                    //    continue;

                    model.MetersCount++;
                }


            }

            return PartialView("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconSummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_M2MMirrorReconDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel model = new A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel()
            {
                A02_MirrorMeterAuditing_M2MMirrorReconDetailsItems = new List<A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            if (_operationalProvider.CompanyID > 0)
            {
                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No.ToUpper()))
                        continue;
                    serialsChecked.Add(sC.Serial_No.ToUpper());


                    var mirrorDevice = mirrorDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    //if (mirrorDevice == null)
                    //    continue;

                    var localDev = localDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    if (localDev == null)
                    {
                        //var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                        //if (m2mDevice == null)
                        //    m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                        //string serialToUse = sC.Serial_No;

                        //if (m2mDevice != null)
                        //    serialToUse = m2mDevice.serial;

                        localDev = localDevices.Where(p => p.Reference != null && p.Reference.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();
                    }

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || (ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem item = new A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem()
                    {
                        Device = localDev,
                        MirrorDevice = mirrorDevice,
                        SkybillCustomer = sC,
                        LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem.LatestReadingItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null,
                        },
                        Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault(),
                    };

                    if (mirrorDevice != null)
                    {
                        item.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                        item.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                    }


                    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
                    if (latestReadingResults.Length > 0)
                        item.LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem.LatestReadingItem()
                        {
                            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                        };


                    model.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItems.Add(item);
                }

                model.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItems = model.A02_MirrorMeterAuditing_M2MMirrorReconDetailsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();
            }

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconReview")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_M2MMirrorReconReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            // TODO: Only allow edit on correcting factor.
            A02_MirrorMeterAuditing_M2MMirrorReconReviewModel model = new A02_MirrorMeterAuditing_M2MMirrorReconReviewModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00),
                ToDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 00, 00, 00),
                MirrorDeviceID = 0,
                ConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]" },
                },
                ConvertedConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]" },
                },
            };

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

            var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial, 2);
            if (m2mDevice == null)
                m2mDevice = _client.GetDeviceByReference(_operationalProvider.CustomerMeterSerial, 2);

            string serialToUse = _operationalProvider.CustomerMeterSerial;

            if (m2mDevice != null)
                serialToUse = m2mDevice.serial;

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            //if (mirrorDevice == null)
            //    return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

            var localDev = dbCache.Devices.Where(p => p.Serial.ToUpper() == serialToUse.ToUpper() && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
            if (localDev == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

            var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
            if (sC == null)
                return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            var companyTarrifs = skyBillApiClient.GetTarrifsForCompany();

            var distinctTarrifCodes = companyTarrifs.Select(p => p.Resource_No).Distinct();
            foreach (var code in distinctTarrifCodes)
            {
                var tarrif = companyTarrifs.Where(p => p.Resource_No == code).FirstOrDefault();
                model.ConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = mirrorDevice != null && !string.IsNullOrEmpty(mirrorDevice.ConsumptionTariffCode) && mirrorDevice.ConsumptionTariffCode == code,
                });
                model.ConvertedConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Value = code,
                    Text = $"{tarrif.Resource_Name} ({code})",
                    Selected = mirrorDevice != null && !string.IsNullOrEmpty(mirrorDevice.ConvertedConsumptionTariffCode) && mirrorDevice.ConvertedConsumptionTariffCode == code,
                });
            }

            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            if (mirrorDevice != null)
            {
                model.MirrorDeviceID = mirrorDevice.Id;
                model.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                model.DeviceIDLinked = mirrorDevice.DeviceIDLinked;
                model.SerialNumber = mirrorDevice.Serial;
                model.Name = !string.IsNullOrEmpty(mirrorDevice.Name) ? mirrorDevice.Name : localDev.Name;
                model.CorrectingFactor = mirrorDevice.CorrectingFactor;
                model.ConvFactor = mirrorDevice.ConvFactor;
                model.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
            }

            model.Device = localDev;
            model.MirrorDevice = mirrorDevice;
            model.SkybillCustomer = sC;
            model.LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconReviewModel.LatestReadingItem()
            {
                TimeLogged = null,
                VirtualOdoReading = null
            };
            model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault();

            DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
            if (latestReadingResults.Length > 0)
                model.LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconReviewModel.LatestReadingItem()
                {
                    TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                    VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                };



            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconReview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_OdoReadingExport")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_OdoReadingExport()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_OdoReadingExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_OdoReadingExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A02_MirrorMeterAuditing_OdoReadingExportModel model = new A02_MirrorMeterAuditing_OdoReadingExportModel()
            {
                A02_MirrorMeterAuditing_OdoReadingExportItems = new PaginatedList<A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem>(new List<A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem>(), 0, 1, 1),
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0
            };
            var A02_MirrorMeterAuditing_OdoReadingExportItems = new List<A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem>();

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestOdo;
            var odos = dbCache.MirrorOdoReadings;

            if (_operationalProvider.CompanyID > 0)
            {
                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No.ToUpper()))
                        continue;
                    serialsChecked.Add(sC.Serial_No.ToUpper());

                    //var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                    //if (m2mDevice == null)
                    //    m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                    //string serialToUse = sC.Serial_No;

                    //if (m2mDevice != null)
                    //    serialToUse = m2mDevice.serial;

                    var mirrorDevice = mirrorDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    var localDev = localDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem item = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem()
                    {
                        MirrorDevice = mirrorDevice,
                        LatestOdo = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem.LatestOdoItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null,
                        },
                        DeviceType = DeviceType.DeviceTypeEnum.Unknown,
                        SkybillCustomerNo = sC.Customer_No,
                    };


                    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
                    if (latestReadingResults.Length > 0)
                        item.LatestOdo = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem.LatestOdoItem()
                        {
                            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]),
                        };

                    if (localDev != null)
                    {
                        switch (localDev.MeterType)
                        {
                            case DeviceType.DeviceTypeEnum.Water:
                                item.LatestOdo.register_scaling = 0.5m;
                                item.LatestOdo.unit = "l";
                                break;
                            case DeviceType.DeviceTypeEnum.Electricity:
                                item.LatestOdo.register_scaling = 1;
                                item.LatestOdo.unit = "w";
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                item.LatestOdo.register_scaling = 0.01m;
                                item.LatestOdo.unit = "m3";
                                break;
                        }
                        item.DeviceType = localDev.MeterType;
                    }

                    A02_MirrorMeterAuditing_OdoReadingExportItems.Add(item);
                }

                A02_MirrorMeterAuditing_OdoReadingExportItems = A02_MirrorMeterAuditing_OdoReadingExportItems.OrderBy(p => p.MirrorDevice.Serial).ToList();
            }

            string page = Request.Query["pageIndex"];
            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = A02_MirrorMeterAuditing_OdoReadingExportItems.Count;
            model.A02_MirrorMeterAuditing_OdoReadingExportItems = await PaginatedList<A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem>.CreateAsync(A02_MirrorMeterAuditing_OdoReadingExportItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_OdoReadingExport.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_OdoReadingExport_GetRegisters")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_OdoReadingExport_GetRegisters()
        {
            var A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
            {
                LatestReadingOdo = "",
                VerificationReading = "",
                CalculatedDifferenceReading = "",
            };

            if (!string.IsNullOrEmpty(Request.Form["deviceid"]))
            {
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var latestReadings = dbCache.sp_GetAllDevicesLatestOdo;
                DataRow[] latestReadingResults = latestReadings.Select($"Id = '{Request.Form["deviceid"]}'");
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                if (latestReadingResults.Length > 0)
                {
                    // M2M Reading
                    decimal? verificationReadingValue = 0;
                    DateTime? verificationReadingDate = null;
                    var lDev = db.Devices.Where(p => p.Serial == latestReadingResults[0]["Serial"].ToString() && p.ActiveStatusID.HasValue && p.ActiveStatusID == 1).FirstOrDefault();
                    var deviceTypeID = DeviceType.DeviceTypeEnum.Unknown;
                    int? m2mDeviceID = null;
                    if (lDev != null)
                    {
                        m2mDeviceID = lDev.DeviceIDLinked;
                        deviceTypeID = lDev.MeterType;
                    }
                    if (!m2mDeviceID.HasValue)
                    {
                        var m2mDevice = _client.GetDeviceByMeterNumber(latestReadingResults[0]["Serial"].ToString());
                        if (m2mDevice != null)
                            m2mDeviceID = m2mDevice.id;
                    }
                    if (m2mDeviceID != null)
                    {
                        System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                        string start = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).ToString("yyyy-MM-ddTHH:00:00");
                        string end = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).AddHours(1).ToString("yyyy-MM-ddTHH:00:00");
                        Dictionary<int, string> registers = new Dictionary<int, string>();


                        switch (deviceTypeID)
                        {
                            default:
                            case DeviceType.DeviceTypeEnum.Electricity:
                                registers.Add(1, "readings"); // Active Energy
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                registers.Add(140, "readings"); // Gas Consumption
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                registers.Add(80, "readings"); // Water Consumption
                                break;
                        }

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }
                        string urlReadings = $"devices/{m2mDeviceID}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                        var resultReadings = _client.GetString(urlReadings, 2);
                        bool first = true;

                        foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (first)
                            {
                                foreach (var lineVar in fileLine.Split(','))
                                {
                                    string safeName = lineVar.Replace("\"", string.Empty);
                                    Type colType = typeof(string);

                                    if (safeName == "Time Logged")
                                        colType = typeof(DateTime);

                                    dataTableReadings.Columns.Add(safeName, colType);
                                }
                                first = false;
                                continue;
                            }

                            DataRow row = dataTableReadings.NewRow();
                            int colIndex = 0;
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                if (colIndex == 1)
                                    row[colIndex] = Convert.ToDateTime(safeName);
                                else
                                    row[colIndex] = safeName;
                                colIndex++;
                            }

                            dataTableReadings.Rows.Add(row);
                            dataTableReadings.AcceptChanges();
                        }

                        if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                        {
                            if (dataTableReadings.Columns.Count > 2)
                                try { verificationReadingValue = Convert.ToDecimal(dataTableReadings.Rows[0][2]); } catch { }
                            if (dataTableReadings.Columns.Count > 1)
                                try { verificationReadingDate = Convert.ToDateTime(dataTableReadings.Rows[0][1]); } catch { }

                        }
                    }



                    //var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == Convert.ToInt32(Request.Form["deviceid"]) && p.TimeLogged == Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).AddHours(-1)).FirstOrDefault();
                    A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
                    {
                        LatestReadingOdo = $"{Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]).ToDecimal(0)} ({Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).ToDateAndTimeShort()})",
                        VerificationReading = verificationReadingValue != null ? $"{verificationReadingValue.ToDecimal(0)} ({verificationReadingDate.ToDateAndTimeShort(true)})" : 0.0m.ToDecimal(0),
                        CalculatedDifferenceReading = (Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]) - (verificationReadingValue != null ? verificationReadingValue : 0)).ToDecimal(0),
                    };
                    if (deviceTypeID == DeviceType.DeviceTypeEnum.Gas)
                    {
                        A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
                        {
                            LatestReadingOdo = $"{Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]).ToDecimal(3)} ({Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]).ToDateAndTimeShort()})",
                            VerificationReading = verificationReadingValue != null ? $"{verificationReadingValue.ToDecimal(3)} ({verificationReadingDate.ToDateAndTimeShort(true)})" : 0.0m.ToDecimal(3),
                            CalculatedDifferenceReading = (Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]) - (verificationReadingValue != null ? verificationReadingValue : 0)).ToDecimal(3),
                        };
                    }
                }
            }
            return Json(A01_GatewayAndDeviceMonitoring_GetRegistersResult);
        }

        [HttpGet]
        [Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_OdoReadingExport_Excel")]
        public async Task<IActionResult> A02_MirrorMeterAuditing_OdoReadingExport_Excel()
        {
                var A02_MirrorMeterAuditing_OdoReadingExportItems = new List<A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem>();

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestOdo;
            var odos = dbCache.MirrorOdoReadings;

            if (_operationalProvider.CompanyID > 0)
            {
                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                List<string> serialsChecked = new List<string>();

                foreach (var sC in sbCustomers)
                {
                    if (serialsChecked.Contains(sC.Serial_No.ToUpper()))
                        continue;
                    serialsChecked.Add(sC.Serial_No.ToUpper());

                    //var m2mDevice = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
                    //if (m2mDevice == null)
                    //    m2mDevice = _client.GetDeviceByReference(sC.Serial_No, 2);

                    //string serialToUse = sC.Serial_No;

                    //if (m2mDevice != null)
                    //    serialToUse = m2mDevice.serial;

                    var mirrorDevice = mirrorDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    if (mirrorDevice == null)
                        continue;

                    var localDev = localDevices.Where(p => p.Serial.ToUpper() == sC.Serial_No.ToUpper()).FirstOrDefault();

                    A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem item = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem()
                    {
                        MirrorDevice = mirrorDevice,
                        LatestOdo = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem.LatestOdoItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null,
                        },
                        SkybillCustomerNo = sC.Customer_No,
                    };


                    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
                    if (latestReadingResults.Length > 0)
                        item.LatestOdo = new A02_MirrorMeterAuditing_OdoReadingExportModel.A02_MirrorMeterAuditing_OdoReadingExportItem.LatestOdoItem()
                        {
                            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["OdometerReading"]),
                        };

                    if (localDev != null)
                    {
                        switch (localDev.MeterType)
                        {
                            case DeviceType.DeviceTypeEnum.Water:
                                item.LatestOdo.register_scaling = 0.5m;
                                break;
                            case DeviceType.DeviceTypeEnum.Electricity:
                                item.LatestOdo.register_scaling = 1;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                item.LatestOdo.register_scaling = 0.01m;
                                break;
                        }
                    }

                    A02_MirrorMeterAuditing_OdoReadingExportItems.Add(item);
                }

                A02_MirrorMeterAuditing_OdoReadingExportItems = A02_MirrorMeterAuditing_OdoReadingExportItems.OrderBy(p => p.MirrorDevice.Serial).ToList();
            }
            var result = (from p in A02_MirrorMeterAuditing_OdoReadingExportItems
                          select new
                          {
                              device_id = p.MirrorDevice.DeviceIDLinked,
                              register_source = 180,
                              register_scaling = p.LatestOdo.register_scaling,
                              register_value = p.LatestOdo.VirtualOdoReading,
                              register_time = p.LatestOdo.TimeLogged.HasValue ? p.LatestOdo.TimeLogged.Value.ToString("yyyy-MM-ddTHH:mm:ss") : "",
                          }).ToList();

            // Create a MemoryStream to hold the CSV data
            MemoryStream csvStream = new MemoryStream();

            // Create a StreamWriter to write to the MemoryStream
            StreamWriter sw = new StreamWriter(csvStream);

            // Retrieve the worksheet
            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("Export");

                // Insert the table data into the worksheet
                worksheet.Cell(1, 1).InsertTable(result);

                // Adjust column widths
                worksheet.Columns("A", "ZZ").AdjustToContents();

                // Iterate through the rows and columns to write to CSV
                foreach (var row in worksheet.RowsUsed())
                {
                    foreach (var cell in row.CellsUsed())
                    {
                        // Write cell value, add comma if not last column
                        sw.Write(cell.Value);
                        if (cell.Address.ColumnNumber < row.LastCellUsed().Address.ColumnNumber)
                        {
                            sw.Write(",");
                        }
                    }
                    // Move to the next line after each row
                    sw.WriteLine();
                }
            }

            // Flush the StreamWriter to ensure all data is written to the underlying MemoryStream
            sw.Flush();

            // Reset the position of the MemoryStream
            csvStream.Position = 0;

            // At this point, the CSV data is in the MemoryStream and can be used as needed

            if (csvStream != null && csvStream.Length > 0)
            {
                return File(csvStream, "text/csv", "OdoReadingExport_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv");
            }
            // Dispose of the StreamWriter and MemoryStream
            sw.Dispose();
            csvStream.Dispose();



            return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_OdoReadingExport_Excel");
        }

        //[HttpPost]
        //[Route("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconReview")]
        //public async Task<IActionResult> A02_MirrorMeterAuditing_M2MMirrorReconReview(A02_MirrorMeterAuditing_M2MMirrorReconReviewModel model)
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview, SecureAreaActionEnum.ManagementApproval))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview}/{(int)SecureAreaActionEnum.ManagementApproval}");

        //    #endregion

        //    if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
        //        return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");


        //    MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


        //    var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial, 2);
        //    if (m2mDevice == null)
        //        m2mDevice = _client.GetDeviceByReference(_operationalProvider.CustomerMeterSerial, 2);

        //    string serialToUse = _operationalProvider.CustomerMeterSerial;

        //    if (m2mDevice != null)
        //        serialToUse = m2mDevice.serial;

        //    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
        //    if (mirrorDevice == null)
        //        return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

        //    var localDev = dbCache.Devices.Where(p => p.Serial.ToUpper() == serialToUse.ToUpper() && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
        //    if (localDev == null)
        //        return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

        //    var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
        //    if (sC == null)
        //        return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconDetails");

        //    model.ConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
        //    {
        //        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]", Selected = string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) },
        //    };
        //    model.ConvertedConsumptionTariffCode = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
        //    {
        //        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[--NONE--]", Selected = string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) },
        //    };
        //    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

        //    var companyTarrifs = skyBillApiClient.GetTarrifsForCompany();

        //    var distinctTarrifCodes = companyTarrifs.Select(p => p.Resource_No).Distinct();
        //    foreach (var code in distinctTarrifCodes)
        //    {
        //        var tarrif = companyTarrifs.Where(p => p.Resource_No == code).FirstOrDefault();
        //        model.ConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
        //        {
        //            Value = code,
        //            Text = $"{tarrif.Resource_Name} ({code})",
        //            Selected = !string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) && Request.Form["ConsumptionTariffCode"].ToString() == code,
        //        });
        //        model.ConvertedConsumptionTariffCode.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
        //        {
        //            Value = code,
        //            Text = $"{tarrif.Resource_Name} ({code})",
        //            Selected = !string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) && Request.Form["ConvertedConsumptionTariffCode"].ToString() == code,
        //        });
        //    }

        //    var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
        //    var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
        //    var odos = dbCache.MirrorOdoReadings;

        //    model.MirrorDeviceID = mirrorDevice.Id;
        //    model.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
        //    model.Device = localDev;
        //    model.MirrorDevice = mirrorDevice;
        //    model.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
        //    model.SkybillCustomer = sC;
        //    model.LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconReviewModel.LatestReadingItem()
        //    {
        //        TimeLogged = null,
        //        VirtualOdoReading = null
        //    };
        //    model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault();

        //    model.SerialNumber = mirrorDevice.Serial;
        //    model.Name = !string.IsNullOrEmpty(mirrorDevice.Name) ? mirrorDevice.Name : localDev.Name;
        //    model.CorrectingFactor = mirrorDevice.CorrectingFactor;

        //    DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{sC.Serial_No}'");
        //    if (latestReadingResults.Length > 0)
        //        model.LatestReading = new A02_MirrorMeterAuditing_M2MMirrorReconReviewModel.LatestReadingItem()
        //        {
        //            TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
        //            VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
        //        };

        //    if (_client.GetDeviceByID(model.DeviceIDLinked, 2) == null)
        //    {
        //        ModelState.AddModelError("DeviceIDLinked", "Device ID Linked not found on Metering DB");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        var apidb = new MyVoltageApiDbContext(_APIoptions);
        //        var deviceToUpdate = apidb.Devices.Where(p => p.Id == mirrorDevice.Id).SingleOrDefault();

        //        if (mirrorDevice.ConvFactor != model.ConvFactor)
        //            deviceToUpdate.ConvFactor = model.ConvFactor;
        //        if (mirrorDevice.DeviceIDLinked != model.DeviceIDLinked)
        //            deviceToUpdate.DeviceIDLinked = model.DeviceIDLinked;

        //        if (!string.IsNullOrEmpty(Request.Form["ConsumptionTariffCode"]) && Request.Form["ConsumptionTariffCode"].ToString() != mirrorDevice.ConsumptionTariffCode)
        //            deviceToUpdate.ConsumptionTariffCode = Request.Form["ConsumptionTariffCode"].ToString();

        //        if (!string.IsNullOrEmpty(Request.Form["ConvertedConsumptionTariffCode"]) && Request.Form["ConvertedConsumptionTariffCode"].ToString() != mirrorDevice.ConvertedConsumptionTariffCode)
        //            deviceToUpdate.ConvertedConsumptionTariffCode = Request.Form["ConvertedConsumptionTariffCode"].ToString();

        //        apidb.Update(deviceToUpdate);
        //        apidb.SaveChanges();
        //        _cache.Remove(MVCache.KEY_MirrorDevices);

        //        return Redirect("/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconReview");
        //    }

        //    return View("~/Views/Operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_M2MMirrorReconReview.cshtml", model);
        //}

        //
    }
}
