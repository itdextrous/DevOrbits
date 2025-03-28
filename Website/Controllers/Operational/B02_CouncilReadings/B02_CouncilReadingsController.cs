using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
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
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.B02_CouncilReadingsModels;
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

namespace MyVoltage.Controllers.Operational.B02_CouncilReadings
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class B02_CouncilReadingsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public B02_CouncilReadingsController(
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
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilReadings_CouncilReadingSummaryModel model = new B02_CouncilReadings_CouncilReadingSummaryModel()
            {
                B02_CouncilReadings_CouncilReadingSummaryItems = new List<B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            //System.Data.DataTable sp_B02_CouncilReadings_CouncilReadingResults = dbCache.sp_B02_CouncilReadings_CouncilReadingResults();
            MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomers = dbCache.SkybillCustomers.ToList();
            var B02_CouncilReadings_CouncilReadingUpdates = dbCache.B02_CouncilReadings_CouncilReadingUpdates;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem item = new B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                    CouncilMeterCount = 0,
                };

                int verifiedCount = 0;
                int submittedCount = 0;
                int readingsCount = 0;

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    continue;
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var meters = (from p in db.BuildingCouncilMeters
                              where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                              select p).ToList();


                item.CouncilMeterCount = meters.Count;

                foreach (var meter in meters)
                {
                    var latestB02_CouncilReadings_CouncilReadingUpdate = B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                    if (latestB02_CouncilReadings_CouncilReadingUpdate != null)
                    {
                        switch (((Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)latestB02_CouncilReadings_CouncilReadingUpdate.StatusID))
                        {
                            case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted:
                                readingsCount++;
                                submittedCount++;
                                break;
                            case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified:
                                readingsCount++;
                                verifiedCount++;
                                break;
                            case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified:
                                readingsCount++;
                                break;
                        }
                    }
                }

                item.ReadingsSubmittedCount = submittedCount;
                item.ReadingsUploadedCount = readingsCount;
                item.ReadingsVerifiedCount = verifiedCount;

                if (readingsCount > 0)
                {
                    if (submittedCount == readingsCount)
                    {
                        item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted;
                    }
                    else if (verifiedCount > 0)
                    {
                        item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified;
                    }
                    else
                    {
                        item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified;
                    }
                }

                if ((from p in bCDs
                     where p.PaymentType.HasValue
                     select p).Count() > 0)
                {
                    item.PaymentTypes = string.Join(",",
                        (from p in bCDs
                         where p.PaymentType.HasValue
                         select p.PaymentType.Value.GetDescription()).Distinct().ToList()
                        );
                }


                model.B02_CouncilReadings_CouncilReadingSummaryItems.Add(item);
            }
            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilReadings_CouncilReadingDetailsModel model = new B02_CouncilReadings_CouncilReadingDetailsModel()
            {
                B02_CouncilReadings_CouncilReadingDetailsItems = new List<B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            System.Data.DataTable sp_B02_CouncilReadings_CouncilReadingDetails = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
            var mirrorDevices = dbCache.MirrorDevices;
            MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var meters = (from p in db.BuildingCouncilMeters
                              where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                              select p).ToList();

                foreach (var meter in meters)
                {
                    B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem item = new B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem()
                    {
                        Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                        BuildingCouncilMeter = meter,
                    };

                    var mirrorDevice = (from p in mirrorDevices
                                        where p.Serial.StartsWith(meter.CouncilSerial)
                                        select p).FirstOrDefault();

                    if (mirrorDevice != null)
                    {
                        var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1 && p.Serial == mirrorDevice.Serial).FirstOrDefault();
                        if (localDev != null)
                            item.DeviceName = localDev.Name;
                        item.Serial = mirrorDevice.Serial;
                        DataRow[] results = sp_B02_CouncilReadings_CouncilReadingDetails.Select($"Serial = '{mirrorDevice.Serial}'");
                        if (results.Length > 0)
                        {
                            DataRow dr = results[0];

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

                        }

                        var latestB02_CouncilReadings_CouncilReadingUpdate = db.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID && p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                        if (latestB02_CouncilReadings_CouncilReadingUpdate != null)
                        {
                            item.Status = ((Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)latestB02_CouncilReadings_CouncilReadingUpdate.StatusID);
                            if (item.Status == Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified
                                || item.Status == Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified)
                                model.B02_CouncilReadings_CouncilReadingDetailsItems.Add(item);
                        }

                    }


                }

            }

            model.B02_CouncilReadings_CouncilReadingDetailsItems = model.B02_CouncilReadings_CouncilReadingDetailsItems.OrderBy(p => p.Serial).ToList();

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingResults")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilReadings_CouncilReadingDetailsModel model = new B02_CouncilReadings_CouncilReadingDetailsModel()
            {
                B02_CouncilReadings_CouncilReadingDetailsItems = new List<B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            System.Data.DataTable sp_B02_CouncilReadings_CouncilReadingDetails = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
            var mirrorDevices = dbCache.MirrorDevices;
            MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var meters = (from p in db.BuildingCouncilMeters
                              where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                              select p).ToList();

                foreach (var meter in meters)
                {
                    B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem item = new B02_CouncilReadings_CouncilReadingDetailsModel.B02_CouncilReadings_CouncilReadingDetailsItem()
                    {
                        Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                        BuildingCouncilMeter = meter,
                    };

                    var mirrorDevice = (from p in mirrorDevices
                                        where p.Serial.StartsWith(meter.CouncilSerial)
                                        select p).FirstOrDefault();

                    if (mirrorDevice != null)
                    {
                        var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1 && p.Serial == mirrorDevice.Serial).FirstOrDefault();
                        if (localDev != null)
                            item.DeviceName = localDev.Name;

                        item.Serial = mirrorDevice.Serial;
                        DataRow[] results = sp_B02_CouncilReadings_CouncilReadingDetails.Select($"Serial = '{mirrorDevice.Serial}'");
                        if (results.Length > 0)
                        {
                            DataRow dr = results[0];

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
                        }

                        var latestB02_CouncilReadings_CouncilReadingUpdate = db.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID && p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                        if (latestB02_CouncilReadings_CouncilReadingUpdate != null)
                        {
                            item.Status = ((Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)latestB02_CouncilReadings_CouncilReadingUpdate.StatusID);
                            //if (item.Status == Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified)
                            //    model.B02_CouncilReadings_CouncilReadingDetailsItems.Add(item);
                        }
                        model.B02_CouncilReadings_CouncilReadingDetailsItems.Add(item);

                    }


                }

            }

            model.B02_CouncilReadings_CouncilReadingDetailsItems = model.B02_CouncilReadings_CouncilReadingDetailsItems.OrderBy(p => p.Serial).ToList();

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingUpdate()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            DateTime dt = DateTime.Now;

            DateTime rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            if (dt.Minute > 30) // or just check dt.Minute >= 30 for regular rounding
                rounded = rounded.AddHours(1);

            B02_CouncilReadingsModel_MirrorReadingUpdateModel model = new B02_CouncilReadingsModel_MirrorReadingUpdateModel()
            {
                DateLogged = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeLogged = rounded.ToString("HH:mm"),
                BuildingCouncilMeter = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var mirrorDevices = dbCache.MirrorDevices;
                var db = new MyVoltageDbContext(_options);

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var meters = (from p in db.BuildingCouncilMeters
                              where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                              select p).ToList();

                foreach (var meter in meters)
                {
                    var mirrorDevice = (from p in mirrorDevices
                                        where p.Serial.StartsWith(meter.CouncilSerial)
                                        select p).FirstOrDefault();

                    if (mirrorDevice != null)
                    {
                        model.BuildingCouncilMeter.Add(new SelectListItem()
                        {
                            Text = $"{meter.Name} - {meter.MyVoltageSerial} - {meter.CouncilSerial} (MD:{mirrorDevice.Id} - {mirrorDevice.Serial})",
                            Value = $"{meter.ID}",
                        });
                    }
                }

            }


            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingUpdate(B02_CouncilReadingsModel_MirrorReadingUpdateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var mirrorDevices = dbCache.MirrorDevices;
            var db = new MyVoltageDbContext(_options);

            var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
            if (bD == null)
            {
                return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
            }

            var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

            var meters = (from p in db.BuildingCouncilMeters
                          where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                          select p).ToList();

            foreach (var meter in meters)
            {
                var mD = (from p in mirrorDevices
                          where p.Serial.StartsWith(meter.CouncilSerial)
                          select p).FirstOrDefault();

                if (mD != null)
                {
                    model.BuildingCouncilMeter.Add(new SelectListItem()
                    {
                        Text = $"{meter.Name} - {meter.MyVoltageSerial} - {meter.CouncilSerial} (MD:{mD.Id} - {mD.Serial})",
                        Value = $"{meter.ID}",
                        Selected = Request.Form["BuildingCouncilMeter"] == meter.ID.ToString(),
                    });
                }
            }




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

            Data.BuildingCouncilMeter buildingCouncilMeter = null;

            if (!string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"]))
            {
                try
                {
                    buildingCouncilMeter = (from p in db.BuildingCouncilMeters
                                            where p.ID == Convert.ToInt32(Request.Form["BuildingCouncilMeter"])
                                            select p).SingleOrDefault();
                }
                catch { }
            }

            if (buildingCouncilMeter == null)
                ModelState.AddModelError("BuildingCouncilMeter", "Please select meter.");

            var mirrorDevice = (from p in dbCache.MirrorDevices
                                where p.Serial.StartsWith(buildingCouncilMeter.CouncilSerial)
                                select p).FirstOrDefault();


            if (mirrorDevice == null)
                ModelState.AddModelError("BuildingCouncilMeter", "Mirror Device Not Found.");

            if (model.Photo == null)
                ModelState.AddModelError("Photo", "Required.");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{mirrorDevice.Id}".ToLower();
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
            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(mirrorDevice.Id.ToString().ToLower());
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
            file.Upload(uploadFile);

            #endregion

            #region DB Entry

            Data.B02_CouncilReadings_CouncilReadingUpdate b02_CouncilReadings_CouncilReadingUpdate = new B02_CouncilReadings_CouncilReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = mirrorDevice.Serial,
                MirrorDeviceID = mirrorDevice.Id,
                OdoReading = model.OdoReading,
                PhotoURL = $"{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedTime.Hour, timeLoggedTime.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified,
                BuildingCouncilMeterID = buildingCouncilMeter.ID,
            };

            db.Add(b02_CouncilReadings_CouncilReadingUpdate);
            db.SaveChanges();


            #endregion

            _cache.Remove(MVCache.KEY_B02_CouncilReadings_CouncilReadingUpdates);


            model.IsSuccessful = true;

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdate_GetReadingAndDiff/{serial}")]
        public JsonResult B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFill(string serial)
        {
            string latestLiveReading = "Unknown";
            string latestLiveReadingTime = "Unknown";
            string calculatedDifference = "Unknown";
            string differenceMessage = "";


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            Data.BuildingCouncilMeter buildingCouncilMeter = null;

            if (!string.IsNullOrEmpty(serial))
            {
                try
                {
                    buildingCouncilMeter = (from p in db.BuildingCouncilMeters
                                            where p.ID == Convert.ToInt32(serial)
                                            select p).SingleOrDefault();
                }
                catch { }
            }

            if (buildingCouncilMeter != null)
            {
                var mirrorDevice = (from p in dbCache.MirrorDevices
                                    where p.Serial.StartsWith(buildingCouncilMeter.CouncilSerial)
                                    select p).FirstOrDefault();
                if (mirrorDevice != null)
                {
                    System.Data.DataRow[] latestReading = dbCache.sp_GetAllDevicesLatestReading.Select($"Serial = '{mirrorDevice.Serial}'");
                    if (latestReading != null && latestReading.Length > 0)
                    {
                        decimal latestLiveReadingValue = Convert.ToDecimal(latestReading[0]["VirtualOdometerReading"]);
                        latestLiveReading = latestLiveReadingValue.ToString("N0");
                        latestLiveReadingTime = Convert.ToDateTime(latestReading[0]["TimeLogged"]).ToDateAndTimeShort();

                        if (!string.IsNullOrEmpty(Request.Query["OdoReading"]))
                        {
                            decimal calculatedDifferenceValue = (Convert.ToDecimal(Request.Query["OdoReading"]) - latestLiveReadingValue);
                            calculatedDifference = calculatedDifferenceValue.ToString("N0");
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
                }
            }

            return Json(
                new
                {
                    latestLiveReading = latestLiveReading,
                    latestLiveReadingTime = latestLiveReadingTime,
                    calculatedDifference = calculatedDifference,
                    differenceMessage = differenceMessage,
                }
                );//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdateNoAccess/{buildingCouncilMeterID}")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingUpdateNoAccess(int buildingCouncilMeterID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilReadings_CouncilReadingUpdateNoAccessModel model = new B02_CouncilReadings_CouncilReadingUpdateNoAccessModel()
            {
                BuildingCouncilMeter = new List<SelectListItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var db = new MyVoltageDbContext(_options);

            var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
            if (bD == null)
            {
                return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
            }

            var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

            var meters = (from p in db.BuildingCouncilMeters
                          where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                          select p).ToList();

            foreach (var meter in meters)
            {
                var mD = (from p in mirrorDevices
                          where p.Serial.StartsWith(meter.CouncilSerial)
                          select p).FirstOrDefault();

                if (mD != null)
                {
                    model.BuildingCouncilMeter.Add(new SelectListItem()
                    {
                        Text = $"{meter.Name} - {meter.MyVoltageSerial} - {meter.CouncilSerial} (MD:{mD.Id} - {mD.Serial})",
                        Value = $"{meter.ID}",
                        Selected = buildingCouncilMeterID == meter.ID,
                    });
                }
            }

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdateNoAccess.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdateNoAccess/{buildingCouncilMeterID}")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingUpdateNoAccess(int buildingCouncilMeterID, B02_CouncilReadings_CouncilReadingUpdateNoAccessModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            model.BuildingCouncilMeter = new List<SelectListItem>();

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var db = new MyVoltageDbContext(_options);
            var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
            if (bD == null)
            {
                return Redirect("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingSummary");
            }

            var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

            var meters = (from p in db.BuildingCouncilMeters
                          where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                          select p).ToList();

            foreach (var meter in meters)
            {
                var mD = (from p in mirrorDevices
                          where p.Serial.StartsWith(meter.CouncilSerial)
                          select p).FirstOrDefault();

                if (mD != null)
                {
                    model.BuildingCouncilMeter.Add(new SelectListItem()
                    {
                        Text = $"{meter.Name} - {meter.MyVoltageSerial} - {meter.CouncilSerial} (MD:{mD.Id} - {mD.Serial})",
                        Value = $"{meter.ID}",
                        Selected = Request.Form["BuildingCouncilMeter"] == meter.ID.ToString(),
                    });
                }
            }

            Data.BuildingCouncilMeter buildingCouncilMeter = null;

            if (!string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"]))
            {
                try
                {
                    buildingCouncilMeter = (from p in db.BuildingCouncilMeters
                                            where p.ID == Convert.ToInt32(Request.Form["BuildingCouncilMeter"])
                                            select p).SingleOrDefault();
                }
                catch { }
            }

            if (buildingCouncilMeter == null)
                ModelState.AddModelError("BuildingCouncilMeter", "Please select meter.");

            var mirrorDevice = (from p in dbCache.MirrorDevices
                                where p.Serial.StartsWith(buildingCouncilMeter.CouncilSerial)
                                select p).FirstOrDefault();


            if (mirrorDevice == null)
                ModelState.AddModelError("BuildingCouncilMeter", "Mirror Device Not Found.");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdateNoAccess.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{mirrorDevice.Id}".ToLower();
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
            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(mirrorDevice.Id.ToString().ToLower());
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
            file.Upload(uploadFile);

            #endregion

            #region DB Entry

            DateTime timeLoggedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            Data.B02_CouncilReadings_CouncilReadingUpdate a02_MirrorMeterAuditing_CouncilReadingUpdate = new B02_CouncilReadings_CouncilReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = mirrorDevice.Serial,
                MirrorDeviceID = mirrorDevice.Id,
                OdoReading = 0,
                PhotoURL = $"{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedDate.Hour, timeLoggedDate.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.NoAccess,
                BuildingCouncilMeterID = Convert.ToInt32(Request.Form["BuildingCouncilMeter"]),
            };

            db.Add(a02_MirrorMeterAuditing_CouncilReadingUpdate);
            db.SaveChanges();


            #endregion

            #region SMS/Email Notification


            //var customer = (from p in db.Customers
            //                where p.CustomerNumber == _operationalProvider.CustomerNumber
            //                && !p.IsDeleted
            //                orderby p.CustomerID descending
            //                select p).FirstOrDefault();

            //if (customer != null)
            //{
            //    if (!string.IsNullOrEmpty(customer.NotificationPhoneNumber) && customer.NotificationPhoneNumber.Length == 10)
            //    {
            //        List<Log_Notification> log_Notifications = new List<Log_Notification>();
            //        StringBuilder sbSMS = new StringBuilder();
            //        sbSMS.AppendLine($"We are unable to gain access to meter {_operationalProvider.CustomerMeterSerial} at {_operationalProvider.CustomerNumber}.");
            //        sbSMS.AppendLine($"Please take a photo of the meter and email it with the time of the photo to info@myvoltage.co.za");

            //        log_Notifications.Add(new Log_Notification()
            //        {
            //            CompanyID = customer.CompanyID,
            //            CustomerID = customer.CustomerID,
            //            MessagePreview = sbSMS.ToString(),
            //            Recipients = $"27{customer.NotificationPhoneNumber.Remove(0, 1)}",
            //            TimeSent = DateTime.Now
            //        });

            //        SMS.SendBulkSMSSinglePerson(log_Notifications, _options, sbSMS.ToString());
            //    }

            //    if (!string.IsNullOrEmpty(customer.NotificationEmail))
            //    {
            //        string emailBody = System.IO.File.ReadAllText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "noaccess_notice.txt"));

            //        string url = string.Concat(
            //          Request.Scheme,
            //          "://",
            //          Request.Host.ToUriComponent(),
            //          Request.PathBase.ToUriComponent());

            //        emailBody = emailBody.Replace("{name}", customer.FullName);
            //        emailBody = emailBody.Replace("{serial}", _operationalProvider.CustomerMeterSerial);
            //        emailBody = emailBody.Replace("{customerno}", _operationalProvider.CustomerNumber);
            //        emailBody = emailBody.Replace("{url}", url);


            //        List<Log_Notification> log_Notifications = new List<Log_Notification>();
            //        log_Notifications.Add(new Log_Notification()
            //        {
            //            CompanyID = customer.CompanyID,
            //            CustomerID = customer.CustomerID,
            //            MessagePreview = HttpUtility.HtmlEncode(emailBody),
            //            Recipients = $"{customer.FullName} <{customer.NotificationEmail}>",
            //            TimeSent = DateTime.Now
            //        });

            //        Stream uploadFileEmail = new MemoryStream();
            //        model.Photo.CopyTo(uploadFileEmail);
            //        byte[] fileContentsEmail = new byte[uploadFileEmail.Length];
            //        uploadFileEmail.Position = 0;
            //        uploadFileEmail.Read(fileContentsEmail, 0, fileContentsEmail.Length);

            //        await _emailSender.SendBulkEmailAsync(log_Notifications, _options, "Meter Access Problem", emailBody, emailBody, fileContentsEmail, System.IO.Path.GetFileName(model.Photo.FileName), model.Photo.ContentType);

            //    }
            //}


            #endregion

            _cache.Remove(MVCache.KEY_B02_CouncilReadings_CouncilReadingUpdates);
            model.IsSuccessful = true;

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingUpdateNoAccess.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingVerification()
        {
            return Redirect("/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingResults");
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification/{buildingCouncilMeterID}")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingVerification(int buildingCouncilMeterID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingVerification, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingVerification}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilReadings_CouncilReadingVerificationModel model = new B02_CouncilReadings_CouncilReadingVerificationModel()
            {
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var bCM = dbCache.BuildingCouncilMeters.Where(p => p.ID == buildingCouncilMeterID).SingleOrDefault();

            if (bCM == null)
                return Redirect($"/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails");

            model.DeviceName = bCM.Name;

            var mD = (from p in dbCache.MirrorDevices
                      where p.Serial.StartsWith(bCM.CouncilSerial)
                      select p).FirstOrDefault();

            if (mD != null)
            {
                model.DeviceSerial = $"{bCM.MyVoltageSerial} - {bCM.CouncilSerial} (MD:{mD.Id} - {mD.Serial})";
                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                System.Data.DataRow[] latestReading = dbCache.sp_GetAllDevicesLatestReading.Select($"Serial = '{mD.Serial}'");
                if (latestReading != null && latestReading.Length > 0)
                {
                    model.LatestReading = Convert.ToDecimal(latestReading[0]["VirtualOdometerReading"]);
                    model.LatestReadingDateTime = Convert.ToDateTime(latestReading[0]["TimeLogged"]);
                }

                var latestB02_CouncilReadings_CouncilReadingUpdate = dbCache.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.MirrorDeviceID == mD.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (latestB02_CouncilReadings_CouncilReadingUpdate != null)
                {
                    model.LatestReadingOdo = latestB02_CouncilReadings_CouncilReadingUpdate.OdoReading;
                    model.LatestReadingOdoCreatedBy = _userManager.FindByIdAsync(latestB02_CouncilReadings_CouncilReadingUpdate.UserID).Result.Email;
                    model.LatestReadingDateTimeOdo = latestB02_CouncilReadings_CouncilReadingUpdate.TimeLogged;
                    model.LatestReadingOdoCreatedDateTime = latestB02_CouncilReadings_CouncilReadingUpdate.DateCreated;
                    model.LatestReadingOdoPhotoURL = $"/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification_Photo/{latestB02_CouncilReadings_CouncilReadingUpdate.ID}";
                    model.B02_CouncilReadings_CouncilReadingUpdateID = latestB02_CouncilReadings_CouncilReadingUpdate.ID;
                    model.Status = (MyVoltage.Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)latestB02_CouncilReadings_CouncilReadingUpdate.StatusID;
                    model.ChangedDate = latestB02_CouncilReadings_CouncilReadingUpdate.DateChanged;
                    model.SubmittedDate = latestB02_CouncilReadings_CouncilReadingUpdate.DateSubmitted;

                    switch (model.Status)
                    {
                        case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified:
                            var actionToRemove = model.B02_CouncilReadings_CouncilReadingVerificationActions.Where(p => p.ActionID == 1).SingleOrDefault();
                            model.B02_CouncilReadings_CouncilReadingVerificationActions.Remove(actionToRemove);
                            break;
                        case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted:
                            var actionToRemoveSubmitted = model.B02_CouncilReadings_CouncilReadingVerificationActions.Where(p => p.ActionID == 3).SingleOrDefault();
                            model.B02_CouncilReadings_CouncilReadingVerificationActions.Remove(actionToRemoveSubmitted);
                            var actionToRemoveReject = model.B02_CouncilReadings_CouncilReadingVerificationActions.Where(p => p.ActionID == 2).SingleOrDefault();
                            model.B02_CouncilReadings_CouncilReadingVerificationActions.Remove(actionToRemoveReject);
                            break;
                    }

                    if (!string.IsNullOrEmpty(latestB02_CouncilReadings_CouncilReadingUpdate.ChangedByID))
                    {
                        var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == latestB02_CouncilReadings_CouncilReadingUpdate.ChangedByID).SingleOrDefault();
                        if (cBy != null)
                            model.ChangedByUserName = $"{cBy.FirstName} {cBy.LastName}";
                    }

                    if (!string.IsNullOrEmpty(latestB02_CouncilReadings_CouncilReadingUpdate.SubmittedByID))
                    {
                        var sBy = dbCache.OperationalProfiles.Where(p => p.UserID == latestB02_CouncilReadings_CouncilReadingUpdate.SubmittedByID).SingleOrDefault();
                        if (sBy != null)
                            model.SubmittedByUserName = $"{sBy.FirstName} {sBy.LastName}";
                    }

                    var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == latestB02_CouncilReadings_CouncilReadingUpdate.MirrorDeviceID && p.TimeLogged == latestB02_CouncilReadings_CouncilReadingUpdate.TimeLogged.AddHours(-1)).SingleOrDefault();
                    if (verificationReading != null)
                    {
                        model.VerificationReading = verificationReading.VirtualOdometerReading;
                        model.VerificationReadingDateTime = verificationReading.TimeLogged;
                    }


                }
            }


            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification_Photo/{B02_CouncilReadings_CouncilReadingUpdateID}")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingVerification_Photo(int B02_CouncilReadings_CouncilReadingUpdateID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var item = dbCache.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.ID == B02_CouncilReadings_CouncilReadingUpdateID).SingleOrDefault();

            if (item != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string shareName = "a02-mirrorreadingupdates";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
                if (directoryTemp.Exists())
                {
                    ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(company.Name.ToLower());
                    if (directoryCompany.Exists())
                    {
                        ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(item.MirrorDeviceID.ToString().ToLower());
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
                    ShareDirectoryClient directory = directoryCompany2.GetSubdirectoryClient(item.MirrorDeviceID.ToString().ToLower());
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

            return Content("Not found");
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingVerification_Action/{actionID}/{B02_CouncilReadings_CouncilReadingUpdateID}")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingVerification_Action(int actionID, int B02_CouncilReadings_CouncilReadingUpdateID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.ID == B02_CouncilReadings_CouncilReadingUpdateID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails");

            var modelForActions = new B02_CouncilReadings_CouncilReadingVerificationModel()
            {
                Status = (Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)item.StatusID
            };

            var action = modelForActions.B02_CouncilReadings_CouncilReadingVerificationActions.Where(p => p.ActionID == actionID).SingleOrDefault();

            if (action == null)
                return Redirect($"/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails");

            if (item != null)
            {
                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                var userEditing = _userManager.FindByIdAsync(_userManager.GetUserId(User)).Result;
                var userWhoAdded = _userManager.FindByIdAsync(item.UserID).Result;

                switch (action.ActionID)
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

                        //string un = _configuration["AppSettings:FTP_PhotoUploader_UN"];
                        //string pwd = _configuration["AppSettings:FTP_PhotoUploader_Password"];

                        //var originalTempFile = FTPProvider.DownloadFile(item.PhotoURL, un, pwd);

                        //if (originalTempFile != null)
                        //{
                        //    #region Create company folder

                        //    string dirUrl = $"{_operationalProvider.CompanyName}";

                        //    dirUrl = dirUrl + $"/{item.MirrorDeviceID}";

                        //    #endregion

                        //    string fileName = item.MeterSerial + item.TimeLogged.ToString("_yyyy_MM_dd_HH_mm") + System.IO.Path.GetExtension(item.PhotoURL);
                        //    // Copy the contents of the file to the request stream.
                        //    Stream uploadFile = new MemoryStream();
                        //    originalTempFile.CopyTo(uploadFile);
                        //    byte[] fileContents = new byte[uploadFile.Length];
                        //    uploadFile.Position = 0;
                        //    uploadFile.Read(fileContents, 0, fileContents.Length);

                        //    FTPProvider.UploadFile(dirUrl, fileName, fileContents, un, pwd);
                        //}

                        //#endregion

                        item.StatusID = (int)MyVoltage.Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified;
                        item.ChangedByID = userEditing.Id;
                        item.DateChanged = DateTime.Now;
                        break;
                    // Reject
                    case 2:
                        item.StatusID = (int)MyVoltage.Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Rejected;
                        item.ChangedByID = userEditing.Id;
                        item.DateChanged = DateTime.Now;

                        break;
                    // Submit to Council
                    case 3:
                        item.StatusID = (int)MyVoltage.Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted;
                        item.SubmittedByID = userEditing.Id;
                        item.DateSubmitted = DateTime.Now;

                        break;
                }

                db.Update(item);
                db.SaveChanges();
            }


            _cache.Remove(MVCache.KEY_B02_CouncilReadings_CouncilReadingUpdates);
            return Redirect($"/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingDetails");
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingPlanner")]
        public async Task<IActionResult> B02_CouncilReadings_CouncilReadingPlanner()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilReadings_CouncilReadingPlanner, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilReadings_CouncilReadingPlanner}/{(int)SecureAreaActionEnum.View}");

            #endregion



            B02_CouncilReadings_CouncilReadingPlannerModel model = new B02_CouncilReadings_CouncilReadingPlannerModel()
            {
                B02_CouncilReadings_CouncilReadingPlannerItems = new List<B02_CouncilReadings_CouncilReadingPlannerModel.B02_CouncilReadings_CouncilReadingPlannerItem>(),
                StatusFilter = (from p in (Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes[])Enum.GetValues(typeof(Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes))
                                orderby p.GetDescription()
                                select new SelectListItem()
                                {
                                    Selected = ((int)p).ToString() == Request.Query["statusFilterID"] ? true : false,
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString()
                                }).ToList(),
            };

            var db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var companies = _operationalProvider.Companies.ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();

                if (bD == null)
                    continue;

                var bCDs = (from p in db.BuildingCouncilDetails
                            where p.BuildingID == bD.ID
                            select p).ToList();

                foreach (var bCD in bCDs)
                {
                    var bCMeters = (from p in db.BuildingCouncilMeters
                                    where p.BuildingCouncilID == bCD.ID
                                    select p).ToList();

                    foreach (var meter in bCMeters)
                    {
                        B02_CouncilReadings_CouncilReadingPlannerModel.B02_CouncilReadings_CouncilReadingPlannerItem item = new B02_CouncilReadings_CouncilReadingPlannerModel.B02_CouncilReadings_CouncilReadingPlannerItem()
                        {
                            Company = co,
                            B02_CouncilReadings_CouncilReadingUpdate = db.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID).OrderByDescending(p => p.DateCreated).FirstOrDefault(),
                            BuildingCouncilDetail = bCD,
                            BuildingCouncilMeter = meter,
                            Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                        };

                        if (item.B02_CouncilReadings_CouncilReadingUpdate != null)
                            item.Status = item.B02_CouncilReadings_CouncilReadingUpdate.Status;


                        item.BuildingCouncilDetail = bCD;

                        if (bCD.CouncilTypeID.HasValue && bCD.CouncilCycleID.HasValue)
                        {
                            var bCycle = db.BuildingCycles.Where(p => p.BuildingCouncilTypeID == bCD.CouncilTypeID.Value && p.ID == bCD.CouncilCycleID.Value).FirstOrDefault();
                            if (bCycle != null)
                            {
                                var latestCycle = db.BuildingCycles.Where(p => p.BuildingCycleMonth.Year == DateTime.Now.Year && p.BuildingCycleMonth.Month == DateTime.Now.Month && p.BuildingCycleCode == bCycle.BuildingCycleCode && p.BuildingCouncilTypeID == bCycle.BuildingCouncilTypeID).SingleOrDefault();
                                if (latestCycle != null)
                                    item.BuildingCycle = latestCycle;
                            }
                        }

                        if (bCD.CouncilTypeID.HasValue)
                        {
                            var bCType = db.BuildingCouncilTypes.Where(p => p.ID == bCD.CouncilTypeID.Value).SingleOrDefault();
                            item.BuildingCouncilType = bCType;
                        }

                        if (item.BuildingCycle != null)
                        {
                            if (DateTime.Now.Date >= item.BuildingCycle.BuildingCycleReadingStartDate.Date
                               && DateTime.Now.Date <= item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                            {
                                if (item.B02_CouncilReadings_CouncilReadingUpdate == null)
                                    item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.ReadingDue;
                            }
                            else if (DateTime.Now.Date < item.BuildingCycle.BuildingCycleReadingStartDate.Date
                               || DateTime.Now.Date > item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                            {
                                if (item.B02_CouncilReadings_CouncilReadingUpdate == null)
                                    item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Missed;
                            }

                            if (item.B02_CouncilReadings_CouncilReadingUpdate != null)
                            {
                                if (item.B02_CouncilReadings_CouncilReadingUpdate.DateCreated.Date >= item.BuildingCycle.BuildingCycleReadingStartDate.Date
                                     && item.B02_CouncilReadings_CouncilReadingUpdate.DateCreated.Date <= item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                                {
                                    item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.ReadingDue;
                                }
                                else if (item.B02_CouncilReadings_CouncilReadingUpdate.DateCreated.Date < item.BuildingCycle.BuildingCycleReadingStartDate.Date
                                     || item.B02_CouncilReadings_CouncilReadingUpdate.DateCreated.Date > item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                                {
                                    item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.NoAction;
                                }
                            }

                        }


                        if (!string.IsNullOrEmpty(Request.Query["statusFilterID"]) && Convert.ToInt32(Request.Query["statusFilterID"]) != (int)item.Status)
                            continue;

                        model.B02_CouncilReadings_CouncilReadingPlannerItems.Add(item);
                    }
                }

            }

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilReadings_CouncilReadingPlanner.cshtml", model);
        }


        #endregion


        #region Devices

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersSummary")]
        public async Task<IActionResult> B02_CouncilDevice_CouncilMetersSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> B02_CouncilDevice_CouncilMetersSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B02_CouncilDevice_CouncilMetersSummaryModel model = new B02_CouncilDevice_CouncilMetersSummaryModel()
            {
                CompanyID = companyID,
                CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault().Name,
                TableRowID = $"CalibrationSummaryItem_{companyID}",
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            if (companyID > 0)
            {
                var sbCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID).ToList();
                model.CustomersCount = (from p in sbCustomers
                                        select p.Customer_No).Distinct().ToList().Count;

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == companyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    var meters = (from p in db.BuildingCouncilMeters
                                  where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                                  select p).ToList();

                    foreach (var meter in meters)
                    {
                        var mirrorDevice = (from p in dbCache.MirrorDevices
                                            where p.Serial.StartsWith(meter.CouncilSerial)
                                            select p).FirstOrDefault();

                        if (mirrorDevice != null)
                        {
                            model.MetersCount++;
                        }
                    }


                }
            }

            return PartialView("~/Views/Operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersSummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersDetails")]
        public async Task<IActionResult> B02_CouncilDevice_CouncilMetersDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilDevice_CouncilMetersDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilDevice_CouncilMetersDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            B02_CouncilDevice_CouncilMetersDetailsModel model = new B02_CouncilDevice_CouncilMetersDetailsModel()
            {
                B02_CouncilDevice_CouncilMetersDetailsItems = new List<B02_CouncilDevice_CouncilMetersDetailsModel.B02_CouncilDevice_CouncilMetersDetailsItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var mirrorDevices = dbCache.MirrorDevices;
            var localDevices = dbCache.Devices;
            var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            var odos = dbCache.MirrorOdoReadings;

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    var meters = (from p in db.BuildingCouncilMeters
                                  where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                                  select p).ToList();

                    foreach (var meter in meters)
                    {
                        var mirrorDevice = (from p in dbCache.MirrorDevices
                                            where p.Serial.StartsWith(meter.CouncilSerial)
                                            select p).FirstOrDefault();

                        if (mirrorDevice != null)
                        {
                            B02_CouncilDevice_CouncilMetersDetailsModel.B02_CouncilDevice_CouncilMetersDetailsItem item = new B02_CouncilDevice_CouncilMetersDetailsModel.B02_CouncilDevice_CouncilMetersDetailsItem()
                            {
                                A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault(),
                                MirrorDevice = mirrorDevice,
                                OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault(),
                                LatestReading = new B02_CouncilDevice_CouncilMetersDetailsModel.B02_CouncilDevice_CouncilMetersDetailsItem.LatestReadingItem()
                                {
                                    TimeLogged = null,
                                    VirtualOdoReading = null,
                                },
                                Company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault(),
                                BuildingCouncilMeter = meter,
                            };


                            DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{mirrorDevice.Serial}'");
                            if (latestReadingResults.Length > 0)
                                item.LatestReading = new B02_CouncilDevice_CouncilMetersDetailsModel.B02_CouncilDevice_CouncilMetersDetailsItem.LatestReadingItem()
                                {
                                    TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                                    VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                                };


                            model.B02_CouncilDevice_CouncilMetersDetailsItems.Add(item);
                        }
                    }

                }

                model.B02_CouncilDevice_CouncilMetersDetailsItems = model.B02_CouncilDevice_CouncilMetersDetailsItems.OrderBy(p => p.MirrorDevice.Serial).ToList();
            }

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersReview/{mirrorDeviceID?}")]
        public async Task<IActionResult> B02_CouncilDevice_CouncilMetersReview(long? mirrorDeviceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B02_CouncilDevice_CouncilMetersReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B02_CouncilDevice_CouncilMetersReview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            // TODO: Only allow edit on correcting factor.
            B02_CouncilDevice_CouncilMetersReviewModel model = new B02_CouncilDevice_CouncilMetersReviewModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00),
                ToDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 00, 00, 00),
                MirrorDeviceID = 0,
                BuildingCouncilMeter = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    var meters = (from p in db.BuildingCouncilMeters
                                  where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                                  select p).ToList();


                    List<MyVoltageApi.Data.Device> mirrorDevicesFound = new List<MyVoltageApi.Data.Device>();

                    foreach (var meter in meters)
                    {
                        var mirrorDevice = (from p in dbCache.MirrorDevices
                                            where p.Serial.StartsWith(meter.CouncilSerial)
                                            select p).FirstOrDefault();

                        if (mirrorDevice != null)
                        {
                            model.BuildingCouncilMeter.Add(new SelectListItem()
                            {
                                Text = $"{meter.Name} - {meter.MyVoltageSerial} - {meter.CouncilSerial}",
                                Value = mirrorDevice.Id.ToString(),
                                Selected = mirrorDeviceID == mirrorDevice.Id ? true : false,
                            });

                            mirrorDevicesFound.Add(mirrorDevice);
                        }
                    }

                    if (mirrorDevicesFound.Count > 0)
                    {
                        var selectedMeter = mirrorDevicesFound[0];

                        if (mirrorDeviceID.HasValue && mirrorDevicesFound.Where(p => p.Id == mirrorDeviceID.Value).Count() > 0)
                            selectedMeter = mirrorDevicesFound.Where(p => p.Id == mirrorDeviceID.Value).FirstOrDefault();

                        var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Id == selectedMeter.Id).FirstOrDefault();
                        if (mirrorDevice == null)
                            return View("~/Views/Operational/A02_MirrorMeterAuditing/B02_CouncilDevice_CouncilMetersDetails.cshtml", model);

                        var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
                        var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
                        var odos = dbCache.MirrorOdoReadings;

                        model.MirrorDeviceID = mirrorDevice.Id;
                        model.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                        model.MirrorDevice = mirrorDevice;
                        model.OdoReading = odos.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                        model.LatestReading = new B02_CouncilDevice_CouncilMetersReviewModel.LatestReadingItem()
                        {
                            TimeLogged = null,
                            VirtualOdoReading = null
                        };
                        model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();

                        model.DeviceIDLinked = mirrorDevice.DeviceIDLinked;
                        model.SerialNumber = mirrorDevice.Serial;
                        model.Name = mirrorDevice.Name;
                        model.CorrectingFactor = mirrorDevice.CorrectingFactor;

                        DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{mirrorDevice.Serial}'");
                        if (latestReadingResults.Length > 0)
                            model.LatestReading = new B02_CouncilDevice_CouncilMetersReviewModel.LatestReadingItem()
                            {
                                TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                                VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                            };

                    }
                }


            }
            else
            {
                return Redirect("/operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersSummary");
            }

            return View("~/Views/Operational/B02_CouncilReadings/B02_CouncilDevice_CouncilMetersReview.cshtml", model);
        }





        #endregion
    }
}
