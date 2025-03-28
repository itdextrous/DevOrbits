using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
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
using MyVoltage.Models.OperationalModels.A01_GatewayAndDeviceMonitoring;
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
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.A01_GatewayAndDeviceMonitoring
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A01_GatewayAndDeviceMonitoringController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public A01_GatewayAndDeviceMonitoringController(
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
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewaySummary")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GatewaySummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var m2mGW = dbCache.M2MGateways.ToList();
            var gateways = dbCache.Gateways.ToList();
            return View("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewaySummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewaySummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GatewaySummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A01_GatewayAndDeviceMonitoring_GatewaySummaryModel model = new A01_GatewayAndDeviceMonitoring_GatewaySummaryModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID == 0 || uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                if (companyID == 0)
                {
                    company = new Company()
                    {
                        CompanyID = 0,
                        Name = "None",
                    };
                }

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                List<Data.Gateway> gateways = new List<Gateway>();
                if (companyID > 0)
                    gateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == companyID).ToList();
                else
                    gateways = dbCache.Gateways.Where(p => !p.CompanyID.HasValue).ToList();

                int onlineCount = 0;
                int offlineCount = 0;
                int devicesImpactedCount = 0;
                int lessThan4HoursCount = 0;
                int lessThan24HoursCount = 0;
                int lessThan3DaysCount = 0;
                int lessThan7DaysCount = 0;
                int moreThan7DaysCount = 0;

                foreach (var gw in gateways)
                {
                    var m2mGW = dbCache.M2MGateways.Where(p => p.id == gw.GatewayID).SingleOrDefault();

                    if (m2mGW != null)
                    {
                        if (m2mGW.status.id == 1)
                            onlineCount++;
                        else
                        {
                            offlineCount++;

                            TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);

                            if (offlineDuration.TotalHours < 4)
                                lessThan4HoursCount++;
                            else if (offlineDuration.TotalHours < 24)
                                lessThan24HoursCount++;
                            else if (offlineDuration.TotalDays < 3)
                                lessThan3DaysCount++;
                            else if (offlineDuration.TotalDays < 7)
                                lessThan7DaysCount++;
                            else if (offlineDuration.TotalDays >= 7)
                                moreThan7DaysCount++;

                            var m2mGWDevices = dbCache.GetGatewayDevices(gw.GatewayID);

                            if (m2mGWDevices != null)
                            {
                                devicesImpactedCount += m2mGWDevices.Count;
                            }
                        }

                    }
                }

                model.OnlineCount = onlineCount;
                model.OfflineCount = offlineCount;
                model.DevicesImpactedCount = devicesImpactedCount;
                model.LessThan4HoursCount = lessThan4HoursCount;
                model.LessThan24HoursCount = lessThan24HoursCount;
                model.LessThan3DaysCount = lessThan3DaysCount;
                model.LessThan7DaysCount = lessThan7DaysCount;
                model.MoreThan7DaysCount = moreThan7DaysCount;


            }


            return PartialView("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewaySummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayDetails")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GatewayDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A01_GatewayAndDeviceMonitoring_GatewayDetailsModel model = new A01_GatewayAndDeviceMonitoring_GatewayDetailsModel()
            {
                A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = new List<A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem>(),
                OnlineType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Online Types--]", Selected = string.IsNullOrEmpty(Request.Query["OnlineType"]) },
                    new SelectListItem() { Value = "1", Text = "Online", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "All Offline", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "Offline - > 4 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "3" },
                    new SelectListItem() { Value = "4", Text = "Offline - > 24 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "4" },
                    new SelectListItem() { Value = "5", Text = "Offline - > 3 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "5" },
                    new SelectListItem() { Value = "6", Text = "Offline - > 7 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "6" },
                }
            };


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            List<Data.Gateway> gateways = new List<Gateway>();
            if (_operationalProvider.CompanyID == 0)
                gateways = dbCache.Gateways.Where(p => !p.CompanyID.HasValue).ToList();
            else
                gateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            var db = new MyVoltageDbContext(_options);

            var opProfs = db.OperationalProfiles.ToList();
            var latest_Request = (from p in db.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequests
                                  orderby p.CreatedDate descending
                                  select p).FirstOrDefault();

            if (latest_Request != null)
            {
                model.Latest_Request = new A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest()
                {
                    CreatedByUsername = "",
                    CreatedBy = latest_Request.CreatedBy,
                    CompanyID = latest_Request.CompanyID,
                    CreatedDate = latest_Request.CreatedDate,
                    DateEnded = latest_Request.DateEnded,
                    DateStarted = latest_Request.DateStarted,
                    FromDate = latest_Request.FromDate,
                    ID = latest_Request.ID,
                    Progress = latest_Request.Progress,
                    SystemReportID = latest_Request.SystemReportID,
                    ToDate = latest_Request.ToDate,
                };

                if (!string.IsNullOrEmpty(latest_Request.CreatedBy))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == latest_Request.CreatedBy.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        model.Latest_Request.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                }

            }

            var unresolvedStatuses = db.SiteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).ToList();
            var flags = (from p in db.A09_Flags
                         where unresolvedStatuses.Select(c => c.ID).Contains(p.StatusID)
                         && p.LinkedObjectDBTableName == "Gateways"
                         select p).ToList();
            var a09_Flags_Types = db.A09_Flags_Types.ToList();

            foreach (var gw in gateways)
            {
                if (model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.gatewayID == gw.GatewayID).Count() != 0)
                    continue;

                var m2mGW = dbCache.M2MGateways.Where(p => p.id == gw.GatewayID).SingleOrDefault();

                if (m2mGW != null)
                {
                    A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem item = new A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem()
                    {
                        autoDisconnect = m2mGW.autoDisconnect,
                        devices = m2mGW.devices,
                        DevicesLinked = 0,
                        //deviceStatus = m2mGW.deviceStatus,
                        //deviceType = m2mGW.deviceType,
                        //deviceTypeName = m2mGW.deviceTypeName,
                        gatewayID = m2mGW.gatewayID,
                        id = m2mGW.id,
                        mapping = m2mGW.mapping,
                        name = m2mGW.name,
                        network = m2mGW.network,
                        OfflineDuration = "",
                        register = m2mGW.register,
                        serial = m2mGW.serial,
                        //since = m2mGW.since,
                        status = m2mGW.status,
                        type = m2mGW.type,
                        GISLocation = gw.GISLocation,
                        SimCardNumber = m2mGW.network != null && !string.IsNullOrEmpty(m2mGW.network.msisdn) ? m2mGW.network.msisdn : gw.SimCardNumber,
                        A09_Flags = new List<A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem.FlagItem>(),
                        SimCardNumberOverride = gw.SimCardNumberOverride,
                    };

                    //var m2mGWDevices = dbCache.GetGatewayDevices(gw.GatewayID);

                    //if (m2mGWDevices != null)
                    //{
                    //    item.DevicesLinked += m2mGWDevices.Count;
                    //}

                    TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);
                    if (m2mGW.status.id != 1)
                    {

                        if (offlineDuration.TotalHours < 4)
                            item.OfflineDuration = "< 4 H";
                        else if (offlineDuration.TotalHours < 24)
                            item.OfflineDuration = "< 24 H";
                        else if (offlineDuration.TotalDays < 3)
                            item.OfflineDuration = "< 3 D";
                        else if (offlineDuration.TotalDays < 7)
                            item.OfflineDuration = "< 7 D";
                        else if (offlineDuration.TotalDays >= 7)
                            item.OfflineDuration = "> 7 D";
                    }

                    item.OfflineDurationTS = offlineDuration;

                    var thisDeviceFlags = flags.Where(p => p.LinkedObjectUniqueID == m2mGW.id.ToString()).ToList();
                    foreach (var flag in thisDeviceFlags)
                    {
                        A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem.FlagItem flagItem = new A01_GatewayAndDeviceMonitoring_GatewayDetailsModel.A01_GatewayAndDeviceMonitoring_GatewayDetailsItem.FlagItem()
                        {
                            FlagID = flag.ID,
                            FlagTypeID = flag.FlagTypeID,
                            FlagTypeName = a09_Flags_Types.Where(c => c.ID == flag.FlagTypeID).SingleOrDefault().FlagTypeName,
                        };
                        item.A09_Flags.Add(flagItem);
                    }


                    model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Add(item);

                }
            }


            if (!string.IsNullOrEmpty(Request.Query["OnlineType"]))
                switch (Request.Query["OnlineType"].ToString())
                {
                    case "1":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "online").ToList();
                        break;
                    case "2":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "offline").ToList();
                        break;
                    case "3":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalHours > 4).ToList();
                        break;
                    case "4":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalHours > 24).ToList();
                        break;
                    case "5":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalDays > 3).ToList();
                        break;
                    case "6":
                        model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems = model.A01_GatewayAndDeviceMonitoring_GatewayDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalDays > 7).ToList();
                        break;
                }

            return View("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayDetails_GetDeviceCount/{gatewayID}")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GatewayDetails_GetDeviceCount(int gatewayID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var m2mGWDevices = dbCache.GetGatewayDevices(gatewayID);

            if (m2mGWDevices != null)
            {
                return Content(m2mGWDevices.Count.ToString());
            }

            return Content("");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayResults")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GatewayResults()
        {
            return Redirect("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayDetails");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/SendGatewayResetSMS/{gatewayID}/{cell}")]
        public async Task<IActionResult> SendGatewayResetSMS(int gatewayID, string cell)
        {
            Models.CompanyAdminViewModels.SendGatewayResetSMSResultViewModel model = new Models.CompanyAdminViewModels.SendGatewayResetSMSResultViewModel();

            try
            {
                var smsResponse = SMS.formatted_server_response(SMS.SendSms(cell, "reset"));

                Log_GatewayReset log_GatewayReset = new Log_GatewayReset()
                {
                    DateSent = DateTime.Now,
                    GatewayID = gatewayID,
                    SMSResponse = smsResponse,
                    MSISDN = cell
                };

                using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
                {
                    db.Log_GatewayResets.Add(log_GatewayReset);
                    db.SaveChanges();
                }
                return Content("true", "text/plain");
            }
            catch
            {
                return Content("false", "text/plain");
            }

            return Content("false", "text/plain");
        }

        [HttpPost]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/SimCardNumber_Update/{ID}")]
        public async Task<IActionResult> SimCardNumber_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.Gateways
                                     where p.GatewayID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    productToEdit.SimCardNumberOverride = Request.Form["simCardNumber"].ToString();
                    db.Update(productToEdit);
                    db.SaveChanges();
                }

                _cache.Remove(MVCache.KEY_Gateways);

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceCombinedRequestRerun")]
        public async Task<IActionResult> A01_GatewayAndDeviceCombinedRequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            Data.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest f_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest = new F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest()
            {
                CompanyID = _operationalProvider.CompanyID,
                CreatedBy = _userManager.GetUserId(User),
                CreatedDate = DateTime.Now,
                DateEnded = null,
                DateStarted = null,
                FromDate = new DateTime(2018, 01, 01),
                Progress = null,
                SystemReportID = null,
                ToDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month)),
            };

            db.Add(f_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GatewayDetails");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceSummary")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_DeviceSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion
            return View("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_DeviceSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A01_GatewayAndDeviceMonitoring_DeviceSummaryModel model = new A01_GatewayAndDeviceMonitoring_DeviceSummaryModel()
            {

            };

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

            if (companyID == 0)
                company = new Company()
                {
                    Name = "NONE",
                    CompanyID = 0,
                };

            model.CompanyID = companyID;
            model.CompanyName = company.Name;
            model.TableRowID = trid;

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var m2mDevices = dbCache.M2MDevices;
            var api2Devices = _client.GetAllDevices(2).ToList();

            var devices = dbCache.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == companyID).ToList();
            if (companyID == 0)
                devices = dbCache.Devices.Where(p => !p.CompanyID.HasValue).ToList();

            int onlineCount = 0;
            int offlineCount = 0;
            int lessThan4HoursCount = 0;
            int lessThan24HoursCount = 0;
            int lessThan3DaysCount = 0;
            int lessThan7DaysCount = 0;
            int moreThan7DaysCount = 0;

            foreach (var dev in devices)
            {
                if (dev.ActiveStatusID.HasValue && dev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                    continue;
                var m2mDev = m2mDevices.Where(p => p.serial == dev.Serial).FirstOrDefault();

                if (m2mDev != null)
                {
                    if (m2mDev.status.id == 1)
                        onlineCount++;
                    else
                    {
                        TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                        #region Check where device status should be retrieved

                        if (offlineDuration.TotalDays > 60)
                        {
                            if (dev.Serial.ToUpper().Contains("peak".ToUpper())
                                || dev.Serial.ToUpper().Contains("offpeak".ToUpper())
                                || dev.Serial.ToUpper().Contains("standard".ToUpper())
                                || dev.Serial.ToUpper().Contains("kva".ToUpper())
                                )
                            {
                                m2mDev = m2mDevices.Where(p => p.serial == dev.Serial.ToUpper().Replace("-Peak".ToUpper(), string.Empty).Replace("-Offpeak".ToUpper(), string.Empty).Replace("-Standard".ToUpper(), string.Empty)).FirstOrDefault();
                                if (m2mDev != null)
                                {
                                    if (m2mDev.status.id == 1)
                                        onlineCount++;
                                    else
                                    {
                                        offlineCount++;
                                        offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                                        if (offlineDuration.TotalHours < 4)
                                            lessThan4HoursCount++;
                                        else if (offlineDuration.TotalHours < 24)
                                            lessThan24HoursCount++;
                                        else if (offlineDuration.TotalDays < 3)
                                            lessThan3DaysCount++;
                                        else if (offlineDuration.TotalDays < 7)
                                            lessThan7DaysCount++;
                                        else if (offlineDuration.TotalDays >= 7)
                                            moreThan7DaysCount++;
                                    }
                                }
                            }
                            else
                            {
                                var a10_VirtualMeterCustomer = db.A10_VirtualMeterCustomers.Where(p => p.MirrorSerial == dev.Serial).FirstOrDefault();
                                if (a10_VirtualMeterCustomer != null)
                                {
                                    var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == a10_VirtualMeterCustomer.A10_VirtualMeterID).SingleOrDefault();
                                    if (a10_VirtualMeter != null)
                                    {
                                        m2mDev = m2mDevices.Where(p => p.serial == a10_VirtualMeter.SerialNumber).FirstOrDefault();
                                        if (m2mDev != null)
                                        {
                                            if (m2mDev.status.id == 1)
                                                onlineCount++;
                                            else
                                            {
                                                offlineCount++;
                                                offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                                                if (offlineDuration.TotalHours < 4)
                                                    lessThan4HoursCount++;
                                                else if (offlineDuration.TotalHours < 24)
                                                    lessThan24HoursCount++;
                                                else if (offlineDuration.TotalDays < 3)
                                                    lessThan3DaysCount++;
                                                else if (offlineDuration.TotalDays < 7)
                                                    lessThan7DaysCount++;
                                                else if (offlineDuration.TotalDays >= 7)
                                                    moreThan7DaysCount++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        else
                        {
                            offlineCount++;

                            if (offlineDuration.TotalHours < 4)
                                lessThan4HoursCount++;
                            else if (offlineDuration.TotalHours < 24)
                                lessThan24HoursCount++;
                            else if (offlineDuration.TotalDays < 3)
                                lessThan3DaysCount++;
                            else if (offlineDuration.TotalDays < 7)
                                lessThan7DaysCount++;
                            else if (offlineDuration.TotalDays >= 7)
                                moreThan7DaysCount++;
                        }
                    }

                }
                else
                {
                    #region Check where device status should be retrieved

                    if (dev.Serial.ToUpper().Contains("peak".ToUpper())
                        || dev.Serial.ToUpper().Contains("offpeak".ToUpper())
                        || dev.Serial.ToUpper().Contains("standard".ToUpper())
                        || dev.Serial.ToUpper().Contains("kva".ToUpper())
                        )
                    {
                        m2mDev = m2mDevices.Where(p => p.serial == dev.Serial.ToUpper().Replace("-Peak".ToUpper(), string.Empty).Replace("-Offpeak".ToUpper(), string.Empty).Replace("-Standard".ToUpper(), string.Empty)).FirstOrDefault();
                        if (m2mDev != null)
                        {
                            if (m2mDev.status.id == 1)
                                onlineCount++;
                            else
                            {
                                offlineCount++;
                                TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                                if (offlineDuration.TotalHours < 4)
                                    lessThan4HoursCount++;
                                else if (offlineDuration.TotalHours < 24)
                                    lessThan24HoursCount++;
                                else if (offlineDuration.TotalDays < 3)
                                    lessThan3DaysCount++;
                                else if (offlineDuration.TotalDays < 7)
                                    lessThan7DaysCount++;
                                else if (offlineDuration.TotalDays >= 7)
                                    moreThan7DaysCount++;
                            }
                        }
                    }
                    else
                    {
                        var a10_VirtualMeterCustomer = db.A10_VirtualMeterCustomers.Where(p => p.MirrorSerial == dev.Serial).FirstOrDefault();
                        if (a10_VirtualMeterCustomer != null)
                        {
                            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == a10_VirtualMeterCustomer.A10_VirtualMeterID).SingleOrDefault();
                            if (a10_VirtualMeter != null)
                            {
                                m2mDev = m2mDevices.Where(p => p.serial == a10_VirtualMeter.SerialNumber).FirstOrDefault();
                                if (m2mDev != null)
                                {
                                    if (m2mDev.status.id == 1)
                                        onlineCount++;
                                    else
                                    {
                                        offlineCount++;
                                        TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                                        if (offlineDuration.TotalHours < 4)
                                            lessThan4HoursCount++;
                                        else if (offlineDuration.TotalHours < 24)
                                            lessThan24HoursCount++;
                                        else if (offlineDuration.TotalDays < 3)
                                            lessThan3DaysCount++;
                                        else if (offlineDuration.TotalDays < 7)
                                            lessThan7DaysCount++;
                                        else if (offlineDuration.TotalDays >= 7)
                                            moreThan7DaysCount++;
                                    }
                                }
                            }
                        }
                    }

                    #endregion
                }
            }

            model.OnlineCount = onlineCount;
            model.OfflineCount = offlineCount;
            model.LessThan4HoursCount = lessThan4HoursCount;
            model.LessThan24HoursCount = lessThan24HoursCount;
            model.LessThan3DaysCount = lessThan3DaysCount;
            model.LessThan7DaysCount = lessThan7DaysCount;
            model.MoreThan7DaysCount = moreThan7DaysCount;




            return PartialView("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceSummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceDetails")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_DeviceDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A01_GatewayAndDeviceMonitoring_DeviceDetailsModel model = new A01_GatewayAndDeviceMonitoring_DeviceDetailsModel()
            {
                A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = new PaginatedList<A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem>(new List<A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem>(), 0, 1, 1),
                DeviceType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Device Types--]" },
                },
                EntriesPerPage = !string.IsNullOrEmpty(Request.Query["EntriesPerPage"]) ? Convert.ToInt32(Request.Query["EntriesPerPage"]) : 100,
                TotalEntries = 0,
                OnlineType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Online Types--]", Selected = string.IsNullOrEmpty(Request.Query["OnlineType"]) },
                    new SelectListItem() { Value = "1", Text = "Online", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "All Offline", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "Offline - > 4 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "3" },
                    new SelectListItem() { Value = "4", Text = "Offline - > 24 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "4" },
                    new SelectListItem() { Value = "5", Text = "Offline - > 3 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "5" },
                    new SelectListItem() { Value = "6", Text = "Offline - > 7 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "6" },
                }
            };

            model.DeviceType.AddRange(
                (from p in ((DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum)))
                 orderby p.GetDescription()
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = ((int)p).ToString(),
                     Text = p.GetDescription(),
                     Selected = !string.IsNullOrEmpty(Request.Query["DeviceType"]) && Request.Query["DeviceType"].ToString() == ((int)p).ToString(),
                 }
                 ).ToList()
                );

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var latest_Request = (from p in db.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequests
                                  orderby p.CreatedDate descending
                                  select p).FirstOrDefault();

            if (latest_Request != null)
            {
                model.Latest_Request = new A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest()
                {
                    CreatedByUsername = "",
                    CreatedBy = latest_Request.CreatedBy,
                    CompanyID = latest_Request.CompanyID,
                    CreatedDate = latest_Request.CreatedDate,
                    DateEnded = latest_Request.DateEnded,
                    DateStarted = latest_Request.DateStarted,
                    FromDate = latest_Request.FromDate,
                    ID = latest_Request.ID,
                    Progress = latest_Request.Progress,
                    SystemReportID = latest_Request.SystemReportID,
                    ToDate = latest_Request.ToDate,
                };

                if (!string.IsNullOrEmpty(latest_Request.CreatedBy))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == latest_Request.CreatedBy.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        model.Latest_Request.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                }

            }

            var A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = new List<A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem>();
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var m2mDevices = dbCache.M2MDevices;
            var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.ToList();
            var a10_VirtualMeters = db.A10_VirtualMeters.ToList();
            List<Data.Device> devices = new List<Data.Device>();
            var gateways = dbCache.M2MGateways.ToList();


            if (_operationalProvider.CompanyID == 0)
                devices = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && !p.CompanyID.HasValue).ToList();
            else
                devices = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            var unresolvedStatuses = db.SiteAdmin_Statuses.Where(p => !p.IsResolvedStatus.HasValue || !p.IsResolvedStatus.Value).ToList();

            var flags = (from p in db.A09_Flags
                         where unresolvedStatuses.Select(c => c.ID).Contains(p.StatusID)
                         && p.LinkedObjectDBTableName == "Devices"
                         select p).ToList();
            var a09_Flags_Types = db.A09_Flags_Types.ToList();

            foreach (var dev in devices)
            {
                if (dev.ActiveStatusID.HasValue && dev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                    continue;
                string offlineDurationString = "";

                if (!string.IsNullOrEmpty(Request.Query["DeviceType"]) && Request.Query["DeviceType"].ToString() != dev.TypeID.ToString())
                    continue;
                var m2mDev = m2mDevices.Where(p => p.serial == dev.Serial).FirstOrDefault();
                if (m2mDev != null)
                {
                    if (A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.id == m2mDev.id).Count() > 0)
                        continue;
                    A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem item = new A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem()
                    {
                        account = m2mDev.account,
                        account_type = m2mDev.account_type,
                        autoDisconnect = m2mDev.autoDisconnect,
                        balance = m2mDev.balance,
                        customer_number = m2mDev.customer_number,
                        GatewayID = dev.GatewayID,
                        gps_coordinates = m2mDev.gps_coordinates,
                        id = m2mDev.id,
                        mapClickable = m2mDev.mapClickable,
                        mapping = m2mDev.mapping,
                        name = m2mDev.name,
                        OfflineDuration = offlineDurationString,
                        partner_code = m2mDev.partner_code,
                        serial = m2mDev.serial,
                        status = m2mDev.status,
                        type = m2mDev.type,
                        A09_Flags = new List<A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem.FlagItem>(),
                        GatewayStatus = "",
                    };

                    if (dev.GatewayID.HasValue)
                    {
                        var gw = gateways.Where(p => p.id == dev.GatewayID.Value).FirstOrDefault();
                        if (gw != null && gw.status != null)
                            item.GatewayStatus = (gw.status.id == 1 ? "Online" : "Offline");
                    }

                    TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status != null ? m2mDev.status.time : DateTime.MinValue);
                    if (m2mDev.deviceStatus == "offline")
                    {

                        #region Check where device status should be retrieved

                        if (offlineDuration.TotalDays > 60)
                        {
                            if (dev.Serial.ToUpper().Contains("peak".ToUpper())
                                || dev.Serial.ToUpper().Contains("offpeak".ToUpper())
                                || dev.Serial.ToUpper().Contains("standard".ToUpper())
                                || dev.Serial.ToUpper().Contains("kva".ToUpper())
                                )
                            {
                                m2mDev = m2mDevices.Where(p => p.serial == dev.Serial.ToUpper().Replace("-Peak".ToUpper(), string.Empty).Replace("-Offpeak".ToUpper(), string.Empty).Replace("-Standard".ToUpper(), string.Empty)).FirstOrDefault();
                                if (m2mDev != null)
                                {
                                    if (m2mDev.status.id == 1)
                                        continue;
                                    else
                                    {
                                        if (offlineDuration.TotalHours < 4)
                                            offlineDurationString = "< 4 H";
                                        else if (offlineDuration.TotalHours < 24)
                                            offlineDurationString = "< 24 H";
                                        else if (offlineDuration.TotalDays < 3)
                                            offlineDurationString = "< 3 D";
                                        else if (offlineDuration.TotalDays < 7)
                                            offlineDurationString = "< 7 D";
                                        else if (offlineDuration.TotalDays >= 7)
                                            offlineDurationString = "> 7 D";
                                    }
                                    item.name += $" ({m2mDev.name})";
                                    item.serial += $" ({m2mDev.serial})";
                                }
                            }
                            else
                            {
                                var a10_VirtualMeterCustomer = a10_VirtualMeterCustomers.Where(p => p.MirrorSerial == dev.Serial).FirstOrDefault();
                                if (a10_VirtualMeterCustomer != null)
                                {
                                    var a10_VirtualMeter = a10_VirtualMeters.Where(p => p.ID == a10_VirtualMeterCustomer.A10_VirtualMeterID).SingleOrDefault();
                                    if (a10_VirtualMeter != null)
                                    {
                                        m2mDev = m2mDevices.Where(p => p.serial == a10_VirtualMeter.SerialNumber).FirstOrDefault();
                                        if (m2mDev != null)
                                        {
                                            if (m2mDev.status.id == 1)
                                                continue;
                                            else
                                            {
                                                if (offlineDuration.TotalHours < 4)
                                                    offlineDurationString = "< 4 H";
                                                else if (offlineDuration.TotalHours < 24)
                                                    offlineDurationString = "< 24 H";
                                                else if (offlineDuration.TotalDays < 3)
                                                    offlineDurationString = "< 3 D";
                                                else if (offlineDuration.TotalDays < 7)
                                                    offlineDurationString = "< 7 D";
                                                else if (offlineDuration.TotalDays >= 7)
                                                    offlineDurationString = "> 7 D";
                                            }
                                            item.name += $" ({m2mDev.name})";
                                            item.serial += $" ({m2mDev.serial})";
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        else
                        {
                            if (offlineDuration.TotalHours < 4)
                            {
                                offlineDurationString = "< 4 H";
                                m2mDev.status = new MyVoltage.Api.MyVoltage.Status()
                                {
                                    id = 1,
                                    time = m2mDev.status.time,
                                };
                            }
                            else if (offlineDuration.TotalHours < 24)
                            {
                                offlineDurationString = "< 24 H";
                                m2mDev.status = new MyVoltage.Api.MyVoltage.Status()
                                {
                                    id = 1,
                                    time = m2mDev.status.time,
                                };
                            }
                            else if (offlineDuration.TotalDays < 3)
                                offlineDurationString = "< 3 D";
                            else if (offlineDuration.TotalDays < 7)
                                offlineDurationString = "< 7 D";
                            else if (offlineDuration.TotalDays >= 7)
                                offlineDurationString = "> 7 D";
                        }
                    }
                    //else
                    //    continue;
                    item.OfflineDuration = offlineDurationString;
                    item.OfflineDurationTS = offlineDuration;
                    item.status = m2mDev.status;
                    item.type = m2mDev.type;

                    var thisDeviceFlags = flags.Where(p => p.LinkedObjectUniqueID == dev.Serial).ToList();
                    foreach (var flag in thisDeviceFlags)
                    {
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem.FlagItem flagItem = new A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem.FlagItem()
                        {
                            FlagID = flag.ID,
                            FlagTypeID = flag.FlagTypeID,
                            FlagTypeName = a09_Flags_Types.Where(c => c.ID == flag.FlagTypeID).SingleOrDefault().FlagTypeName,
                        };
                        item.A09_Flags.Add(flagItem);
                    }


                    A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Add(item);
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["OnlineType"]))
                switch (Request.Query["OnlineType"].ToString())
                {
                    case "1":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "online").ToList();
                        break;
                    case "2":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "offline").ToList();
                        break;
                    case "3":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalHours > 4).ToList();
                        break;
                    case "4":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalHours > 24).ToList();
                        break;
                    case "5":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalDays > 3).ToList();
                        break;
                    case "6":
                        A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.deviceStatus == "offline" && p.OfflineDurationTS.TotalDays > 7).ToList();
                        break;
                }

            if (A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Count > 0)
                A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.OrderBy(p => p.name).ToList();

            string page = Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = model.EntriesPerPage;
            model.TotalEntries = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Count;
            model.A01_GatewayAndDeviceMonitoring_DeviceDetailsItems = await PaginatedList<A01_GatewayAndDeviceMonitoring_DeviceDetailsModel.A01_GatewayAndDeviceMonitoring_DeviceDetailsItem>.CreateAsync(A01_GatewayAndDeviceMonitoring_DeviceDetailsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceResults")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_DeviceResults()
        {
            return Redirect("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_DeviceDetails");
        }

        [HttpPost]
        [Route("/operational/A01_GatewayAndDeviceMonitoring/A01_GatewayAndDeviceMonitoring_GetRegisters")]
        public async Task<IActionResult> A01_GatewayAndDeviceMonitoring_GetRegisters()
        {
            var A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
            {
                signal = "",
                snr = "",
                battery = "",
            };

            if (!string.IsNullOrEmpty(Request.Form["deviceid"]))
            {
                DateTime m2mStart = !string.IsNullOrEmpty(Request.Form["timelogged"]) ? Convert.ToDateTime(Request.Form["timelogged"]).AddHours(-2) : DateTime.Now.AddHours(-2);
                m2mStart = new DateTime(m2mStart.Year, m2mStart.Month, m2mStart.Day, m2mStart.Hour, 0, 0);
                DateTime m2mEnd = m2mStart.AddHours(2);
                m2mEnd = new DateTime(m2mEnd.Year, m2mEnd.Month, m2mEnd.Day, m2mEnd.Hour, 0, 0);

                string start = m2mStart.ToString("yyyy-MM-ddTHH:mm:ss");
                string end = m2mEnd.ToString("yyyy-MM-ddTHH:mm:ss");
                int interval = 3600;

                string url = $"devices/{Request.Form["deviceid"]}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[900]=readings&registers[901]=readings";

                var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 2);

                List<decimal?> battery = new List<decimal?>();
                List<decimal?> signal = new List<decimal?>();
                List<decimal?> snr = new List<decimal?>();

                if (result != null && result.data != null && result.data.registers != null)
                    foreach (MyVoltage.Api.MyVoltage.Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.id == 100)
                        {
                            battery = readingRegister.readings.ToList();
                        }
                        else if (readingRegister.id == 900)
                        {
                            signal = readingRegister.readings.ToList();
                        }
                        else if (readingRegister.id == 901)
                        {
                            snr = readingRegister.readings.ToList();
                        }
                    }


                #region Signal 

                string signalResult = "";
                if (signal.Count > 0)
                {
                    signalResult = "<i class=\"fas fa-exclamation-triangle text-orange\" title=\"Not Found\"></i>";
                    if (signal.Where(p => p.HasValue).Count() > 0)
                    {
                        signalResult = signal.Where(p => p.HasValue).LastOrDefault().Value.ToString("N");
                    }
                }

                #endregion

                #region Battery 

                string batteryResult = "";
                if (battery.Count > 0)
                {
                    batteryResult = "<i class=\"fas fa-exclamation-triangle text-orange\" title=\"Not Found\"></i>";
                    if (battery.Where(p => p.HasValue).Count() > 0)
                    {
                        batteryResult = battery.Where(p => p.HasValue).LastOrDefault().Value.ToString("N");
                    }
                }

                #endregion

                #region SNR 

                string snrResult = "";
                if (snr.Count > 0)
                {
                    snrResult = "<i class=\"fas fa-exclamation-triangle text-orange\" title=\"Not Found\"></i>";
                    if (snr.Where(p => p.HasValue).Count() > 0)
                    {
                        snrResult = snr.Where(p => p.HasValue).LastOrDefault().Value.ToString("N");
                    }
                }

                #endregion

                A01_GatewayAndDeviceMonitoring_GetRegistersResult = new
                {
                    signal = signalResult,
                    snr = snrResult,
                    battery = batteryResult,
                };

            }

            return Json(A01_GatewayAndDeviceMonitoring_GetRegistersResult);
        }


    }
}
