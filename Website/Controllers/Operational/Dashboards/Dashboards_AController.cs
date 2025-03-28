using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Dashboards.Dashboards_AModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Dashboards
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Dashboards_AController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _accessor;

        public Dashboards_AController(
            IHttpContextAccessor accessor,
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
            _accessor = accessor;
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A")]
        public async Task<IActionResult> Dashboards_A()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Dashboards_A, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Dashboards_A}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/Dashboards/Dashboards_A.cshtml");
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A1_011_Gateway_Summary")]
        public async Task<IActionResult> A1_011_Gateway_Summary()
        {
            A1_011_Gateway_SummaryModel model = new A1_011_Gateway_SummaryModel()
            {
                A1_011_Gateway_SummaryItems = new List<A1_011_Gateway_SummaryModel.A1_011_Gateway_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A1_011_Gateway_SummaryModel.A1_011_Gateway_SummaryItem item = new A1_011_Gateway_SummaryModel.A1_011_Gateway_SummaryItem()
                {

                };

                string key = $"A1_011_Gateway_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    List<Data.Gateway> gateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();
                    int onlineCount = 0;
                    int offlineCount = 0;
                    int devicesImpactedCount = 0;
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

                                //TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);

                                //if (offlineDuration.TotalHours < 4)
                                //    lessThan4HoursCount++;
                                //else if (offlineDuration.TotalHours < 24)
                                //    lessThan24HoursCount++;
                                //else if (offlineDuration.TotalDays < 3)
                                //    lessThan3DaysCount++;
                                //else if (offlineDuration.TotalDays < 7)
                                //    lessThan7DaysCount++;
                                //else if (offlineDuration.TotalDays >= 7)
                                //    moreThan7DaysCount++;

                                var m2mGWDevices = dbCache.GetGatewayDevices(gw.GatewayID);

                                if (m2mGWDevices != null)
                                {
                                    devicesImpactedCount += m2mGWDevices.Count;
                                }
                            }

                        }

                    }

                    item = new A1_011_Gateway_SummaryModel.A1_011_Gateway_SummaryItem()
                    {
                        Status = Models.OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType.Ok,
                        Offline = offlineCount,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        DevicesImpacted = devicesImpactedCount,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Offline > 0 && company.IsFlagStatusActive)
                {
                    item.Status = Models.OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType.Problematic;
                    model.A1_011_Gateway_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A1_011_Gateway_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A1_021_Device_Summary")]
        public async Task<IActionResult> A1_011_Device_Summary()
        {
            A1_021_Device_SummaryModel model = new A1_021_Device_SummaryModel()
            {
                A1_021_Device_SummaryItems = new List<A1_021_Device_SummaryModel.A1_021_Device_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var m2mDevices = dbCache.M2MDevices;
            var db = new MyVoltageDbContext(_options);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A1_021_Device_SummaryModel.A1_021_Device_SummaryItem item = new A1_021_Device_SummaryModel.A1_021_Device_SummaryItem()
                {

                };

                string key = $"A1_021_Device_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    int onlineCount = 0;
                    int offlineCount = 0;
                    int lessThan7DaysCount = 0;
                    int moreThan7DaysCount = 0;
                    var devices = dbCache.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

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

                                                if (offlineDuration.TotalDays < 7)
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

                                                        if (offlineDuration.TotalDays < 7)
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

                                    if (offlineDuration.TotalDays < 7)
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

                                        if (offlineDuration.TotalDays < 7)
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

                                                if (offlineDuration.TotalDays < 7)
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

                    item = new A1_021_Device_SummaryModel.A1_021_Device_SummaryItem()
                    {
                        Status = Models.OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType.Ok,
                        Offline = offlineCount,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        LessThan7DaysCount = lessThan7DaysCount,
                        MoreThan7DaysCount = moreThan7DaysCount,
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Offline > 0 && company.IsFlagStatusActive)
                {
                    item.Status = Models.OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType.Problematic;
                    model.A1_021_Device_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A1_021_Device_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A2_011_Meter_Calibration_Summary")]
        public async Task<IActionResult> A2_011_Meter_Calibration_Summary()
        {
            A2_011_Meter_Calibration_SummaryModel model = new A2_011_Meter_Calibration_SummaryModel()
            {
                A2_011_Meter_Calibration_SummaryItems = new List<A2_011_Meter_Calibration_SummaryModel.A2_011_Meter_Calibration_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.ToList();
            var a10_VirtualMeters = db.A10_VirtualMeters.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                var skybillCustomerMeters = (from p in dbCache.SkybillCustomers
                                             where p.CompanyID == uC.CompanyID
                                             && !p.No.ToUpper().Contains("KVA")
                                             && !p.Serial_No.ToUpper().Contains("KVA")
                                             && !p.No.ToUpper().Contains("OFFPEAK")
                                             && !p.Serial_No.ToUpper().Contains("OFFPEAK")
                                             && !p.No.ToUpper().Contains("PEAK")
                                             && !p.Serial_No.ToUpper().Contains("PEAK")
                                             && !p.No.ToUpper().Contains("STANDARD")
                                             && !p.Serial_No.ToUpper().Contains("STANDARD")
                                             select p.Serial_No).Distinct().ToList();


                A2_011_Meter_Calibration_SummaryModel.A2_011_Meter_Calibration_SummaryItem item = new A2_011_Meter_Calibration_SummaryModel.A2_011_Meter_Calibration_SummaryItem()
                {

                };

                string key = $"A2_011_Meter_Calibration_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    int meterCount = 0;
                    int calibratedCount = 0;
                    int notCalibratedCount = 0;
                    int problematicCount = 0;
                    int metersThatCannotBeCalibratedCount = 0;

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


                    item = new A2_011_Meter_Calibration_SummaryModel.A2_011_Meter_Calibration_SummaryItem()
                    {
                        MetersProblematicCount = problematicCount,
                        MetersCount = meterCount,
                        MetersCalibratedCount = calibratedCount,
                        MetersNotCalibratedCount = notCalibratedCount,
                        MetersThatCannotBeCalibratedCount = metersThatCannotBeCalibratedCount,
                        Status = calibratedCount == meterCount ? Models.OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType.Reviewed : Models.OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType.Outstanding,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        CustomersCount = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == uC.CompanyID
                                          && !p.No.ToUpper().Contains("KVA")
                                          && !p.No.ToUpper().Contains("OFFPEAK")
                                          && !p.No.ToUpper().Contains("PEAK")
                                          && !p.No.ToUpper().Contains("STANDARD")
                                          select p.Customer_No).Distinct().Count(),
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType.Reviewed)
                {
                    model.A2_011_Meter_Calibration_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A2_011_Meter_Calibration_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A2_022_Mirror_Reading_Summary")]
        public async Task<IActionResult> A2_022_Mirror_Reading_Summary()
        {
            A2_022_Mirror_Reading_SummaryModel model = new A2_022_Mirror_Reading_SummaryModel()
            {
                A2_022_Mirror_Reading_SummaryItems = new List<A2_022_Mirror_Reading_SummaryModel.A2_022_Mirror_Reading_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var A02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                var skybillCustomerMeters = (from p in dbCache.SkybillCustomers
                                             where p.CompanyID == uC.CompanyID
                                             && !p.No.ToUpper().Contains("KVA")
                                             && !p.No.ToUpper().Contains("OFFPEAK")
                                             && !p.No.ToUpper().Contains("PEAK")
                                             && !p.No.ToUpper().Contains("STANDARD")
                                             select p.Serial_No).Distinct().ToList();


                A2_022_Mirror_Reading_SummaryModel.A2_022_Mirror_Reading_SummaryItem item = new A2_022_Mirror_Reading_SummaryModel.A2_022_Mirror_Reading_SummaryItem()
                {

                };

                string key = $"A2_022_Mirror_Reading_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {

                    int checkedCount = 0;
                    int unCheckedCount = 0;

                    foreach (var skybillCustomer in dbCache.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList())
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


                    item = new A2_022_Mirror_Reading_SummaryModel.A2_022_Mirror_Reading_SummaryItem()
                    {
                        CustomersCheckedCount = checkedCount,
                        CustomersNotCheckedCount = unCheckedCount,
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        Status = "",
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        CustomersCount = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == uC.CompanyID
                                          && !p.No.ToUpper().Contains("KVA")
                                          && !p.No.ToUpper().Contains("OFFPEAK")
                                          && !p.No.ToUpper().Contains("PEAK")
                                          && !p.No.ToUpper().Contains("STANDARD")
                                          select p.Customer_No).Distinct().Count(),
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };


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

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status == "Outstanding")
                {
                    model.A2_022_Mirror_Reading_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A2_022_Mirror_Reading_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A3_011_Network_Balancing_Summary")]
        public async Task<IActionResult> A3_011_Network_Balancing_Summary()
        {
            A3_011_Network_Balancing_SummaryModel model = new A3_011_Network_Balancing_SummaryModel()
            {
                A3_011_Network_Balancing_SummaryItems = new List<A3_011_Network_Balancing_SummaryModel.A3_011_Network_Balancing_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A3_011_Network_Balancing_SummaryModel.A3_011_Network_Balancing_SummaryItem item = new A3_011_Network_Balancing_SummaryModel.A3_011_Network_Balancing_SummaryItem()
                {

                };

                string key = $"A3_011_Network_Balancing_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A3_011_Network_Balancing_SummaryModel.A3_011_Network_Balancing_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        Status = Models.OperationalModels.A03_NetworkBalancing.A03_NetworkBalancing_SummaryItemModel.StatusType.Outstanding,
                    };

                    var thisCompanyReportCount = (from p in dbCache.A03_NetworkBalancing_Captures
                                                  where p.CompanyID == uC.CompanyID
                                                  select p).Count();

                    item.ReportCount = thisCompanyReportCount;

                    var latestReport = (from p in dbCache.A03_NetworkBalancing_Captures
                                        where p.CompanyID == uC.CompanyID
                                        orderby p.ReportMonth descending
                                        select p).FirstOrDefault();

                    if (latestReport != null)
                        item.LatestReportMonth = latestReport.ReportMonth;

                    if (thisCompanyReportCount > 0)
                    {
                        if (item.LatestReportMonth.HasValue)
                        {
                            // 45 days = red
                            // 30 days = yellow
                            // less than 30 = green
                            if (new DateTime(item.LatestReportMonth.Value.Year, item.LatestReportMonth.Value.Month, 1).Date >= new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1).Date)
                                item.Status = Models.OperationalModels.A03_NetworkBalancing.A03_NetworkBalancing_SummaryItemModel.StatusType.Reviewed;
                            else
                                item.Status = Models.OperationalModels.A03_NetworkBalancing.A03_NetworkBalancing_SummaryItemModel.StatusType.TooLongAgo;
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A03_NetworkBalancing.A03_NetworkBalancing_SummaryItemModel.StatusType.Reviewed)
                {
                    model.A3_011_Network_Balancing_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A3_011_Network_Balancing_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A4_031_Zendesk_Ticket_Category_Summary")]
        public async Task<IActionResult> A4_031_Zendesk_Ticket_Category_Summary()
        {
            A4_031_Zendesk_Ticket_Category_SummaryModel model = new A4_031_Zendesk_Ticket_Category_SummaryModel()
            {
                A4_031_Zendesk_Ticket_Category_SummaryItems = new List<A4_031_Zendesk_Ticket_Category_SummaryModel.A4_031_Zendesk_Ticket_Category_SummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var zendeskTickets = (from p in db.Zendesk_Tickets
                                  where p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID
                                  && (p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge"))
                                  select p).ToList();
            var ticketField = dbCache.Zendesk_TicketFields.Where(p => p.ID == 360007062437).SingleOrDefault();
            var ticketFieldOptions = dbCache.Zendesk_TicketFieldOptions.Where(p => p.TicketFieldID == ticketField.ID).ToList();

            foreach (var tO in ticketFieldOptions)
            {
                A4_031_Zendesk_Ticket_Category_SummaryModel.A4_031_Zendesk_Ticket_Category_SummaryItem item = new A4_031_Zendesk_Ticket_Category_SummaryModel.A4_031_Zendesk_Ticket_Category_SummaryItem()
                {
                    Zendesk_TicketField_Option = tO,
                    ClosedCount = 0,
                    HoldCount = 0,
                    OpenCount = 0,
                    PendingCount = 0,
                    SolvedCount = 0,
                };

                string key = $"A4_031_Zendesk_Ticket_Category_SummaryItem_{tO.ID}";
                _cache.Remove(key);

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A4_031_Zendesk_Ticket_Category_SummaryModel.A4_031_Zendesk_Ticket_Category_SummaryItem()
                    {
                        Zendesk_TicketField_Option = tO,
                        ClosedCount = 0,
                        HoldCount = 0,
                        OpenCount = 0,
                        PendingCount = 0,
                        SolvedCount = 0,
                    };

                    foreach (var t in zendeskTickets)
                    {
                        if (string.IsNullOrEmpty(t.CustomFields))
                            continue;

                        var ticketCustomFields = t.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                        if (ticketCustomFields != null && ticketCustomFields.Length > 0 && ticketCustomFields.Where(p => p.id == tO.TicketFieldID).Count() > 0)
                        {
                            var itemToCompareValue = ticketCustomFields.Where(p => p.id == tO.TicketFieldID).ToList()[0].value;
                            if (itemToCompareValue == tO.Value)
                            {
                                if (!t.CompanyID.HasValue)
                                    continue;

                                if (t.Status.ToUpper() == "closed".ToUpper() || t.Status.ToUpper() == "solved".ToUpper())
                                    continue;

                                switch (t.Status.ToUpper())
                                {
                                    case "PENDING":
                                        item.PendingCount++;
                                        break;
                                    case "HOLD":
                                        item.HoldCount++;
                                        break;
                                    case "CLOSED":
                                        item.ClosedCount++;
                                        break;
                                    case "OPEN":
                                    case "NEW":
                                        item.OpenCount++;
                                        break;
                                    case "SOLVED":
                                        item.SolvedCount++;
                                        break;
                                }

                                if (t.CompanyID.HasValue && _operationalProvider.CompanyID != 0 && t.CompanyID.Value != _operationalProvider.CompanyID)
                                    continue;

                                TimeSpan openDuration = DateTime.Now - t.CreatedAt;

                                if (t.CreatedAt.Date == DateTime.Now.Date)
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
                        }

                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A04_Tickets.A04_TicketsModels.A04_Tickets_ZendeskTicketCategorySummaryModel.A04_Tickets_ZendeskTicketCategorySummaryItem.StatusEnum.Good)
                {
                    if (item.OlderThan7DaysCount != 0
                        || item.OlderThan14DaysCount != 0
                        || item.OlderThan1MonthCount != 0)
                        model.A4_031_Zendesk_Ticket_Category_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A4_031_Zendesk_Ticket_Category_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A5_011_Midnight_Sync_Summary")]
        public async Task<IActionResult> A5_011_Midnight_Sync_Summary()
        {
            A5_011_Midnight_Sync_SummaryModel model = new A5_011_Midnight_Sync_SummaryModel()
            {
                A5_011_Midnight_Sync_SummaryItems = new List<A5_011_Midnight_Sync_SummaryModel.A5_011_Midnight_Sync_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var localDevices = dbCache.Devices.ToList();
            var skybillCustomers = dbCache.SkybillCustomers.ToList();

            var midnightSyncExceptions = (from p in dbCache.DeviceReadingsMidnightSyncs
                                          where p.WhereDidIFindThis == "OldReading"
                                          select p).ToList();
            foreach (var msItem in midnightSyncExceptions)
            {
                A5_011_Midnight_Sync_SummaryModel.A5_011_Midnight_Sync_SummaryItem item = new A5_011_Midnight_Sync_SummaryModel.A5_011_Midnight_Sync_SummaryItem()
                {

                };

                string key = $"A5_011_Midnight_Sync_SummaryItem_{msItem.ID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A5_011_Midnight_Sync_SummaryModel.A5_011_Midnight_Sync_SummaryItem()
                    {
                        Device = localDevices.Where(p => p.Serial == msItem.m2mSerial).FirstOrDefault(),
                        ID = msItem.ID,
                        LiveReading = msItem.LiveReading,
                        m2mDeviceID = msItem.m2mDeviceID,
                        m2mDeviceName = msItem.m2mDeviceName,
                        m2mSerial = msItem.m2mSerial,
                        mirrorDeviceID = msItem.mirrorDeviceID,
                        mirrorDeviceName = msItem.mirrorDeviceName,
                        mirrorSerial = msItem.mirrorSerial,
                        OriginalTime = msItem.OriginalTime,
                        Reading = msItem.Reading,
                        SkybillCustomer = skybillCustomers.Where(p => p.Serial_No == msItem.m2mSerial).FirstOrDefault(),
                        TimeLogged = msItem.TimeLogged,
                        WhereDidIFindThis = msItem.WhereDidIFindThis,
                        Ceiling = msItem.Ceiling,
                        CeilingCalculationFrom = msItem.CeilingCalculationFrom,
                        CeilingMin = msItem.CeilingMin,
                        FirstAveragePerDay = msItem.FirstAveragePerDay,
                        HighestAveragePerDay = msItem.HighestAveragePerDay,
                        Last7AveragePerDay = msItem.Last7AveragePerDay,
                        PreviousPlusCeiling = msItem.PreviousPlusCeiling,
                        ReasonForOldReading = msItem.ReasonForOldReading,
                        SecondAveragePerDay = msItem.SecondAveragePerDay,
                        ThirdAveragePerDay = msItem.ThirdAveragePerDay,
                        UseOldReading = msItem.UseOldReading,
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (_operationalProvider.CompanyID > 0)
                {
                    if (item.SkybillCustomer == null || item.SkybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;
                }

                model.A5_011_Midnight_Sync_SummaryItems.Add(item);
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A5_011_Midnight_Sync_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A6_011_Occupancy_Summary")]
        public async Task<IActionResult> A6_011_Occupancy_Summary()
        {
            A6_011_Occupancy_SummaryModel model = new A6_011_Occupancy_SummaryModel()
            {
                A6_011_Occupancy_SummaryItems = new List<A6_011_Occupancy_SummaryModel.A6_011_Occupancy_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;


                A6_011_Occupancy_SummaryModel.A6_011_Occupancy_SummaryItem item = new A6_011_Occupancy_SummaryModel.A6_011_Occupancy_SummaryItem()
                {

                };

                string key = $"A6_011_Occupancy_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var currentVacancy = dbCache.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == company.CompanyID).ToList();
                    var skybillCustomerNumbers = (from p in dbCache.SkybillCustomers
                                                  where p.CompanyID == company.CompanyID
                                                  select p.Customer_No).Distinct().ToList();

                    int customersChecked = 0;
                    int customersCheckedTooLongAgo = 0;
                    DateTime? dateLastChecked = null;
                    string status = "";

                    foreach (var customerNo in skybillCustomerNumbers)
                    {
                        var currentCustomerVacancyCheck = (from p in currentVacancy
                                                           where p.CustomerNo == customerNo
                                                           orderby p.CreateDate descending
                                                           select p).FirstOrDefault();

                        if (currentCustomerVacancyCheck != null)
                        {
                            if (dateLastChecked.HasValue)
                            {
                                if (dateLastChecked.Value <= currentCustomerVacancyCheck.CreateDate)
                                    dateLastChecked = currentCustomerVacancyCheck.CreateDate;
                            }
                            else
                                dateLastChecked = currentCustomerVacancyCheck.CreateDate;
                            customersChecked++;

                            if ((DateTime.Now - currentCustomerVacancyCheck.CreateDate).TotalDays > 60)
                            {
                                customersCheckedTooLongAgo++;
                            }
                        }
                    }


                    item = new A6_011_Occupancy_SummaryModel.A6_011_Occupancy_SummaryItem()
                    {
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        CustomersCount = skybillCustomerNumbers.Count,
                        CustomersCheckedCount = customersChecked,
                        CustomersCheckedTooLongAgoCount = customersCheckedTooLongAgo,
                        DateLastChecked = dateLastChecked,
                        Status = status
                    };


                    if (item.CustomersNotCheckedCount > 0)
                        item.Status = "Outstanding";
                    else if (item.CustomersCheckedTooLongAgoCount > 0)
                        item.Status = "Too long ago";
                    else if (item.CustomersNotCheckedCount == 0)
                        item.Status = "Reviewed";


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != "Reviewed")
                {
                    model.A6_011_Occupancy_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A6_011_Occupancy_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A6_031_Not_Billed_Summary")]
        public async Task<IActionResult> A6_031_Not_Billed_Summary()
        {
            A6_031_Not_Billed_SummaryModel model = new A6_031_Not_Billed_SummaryModel()
            {
                A6_031_Not_Billed_SummaryItems = new List<A6_031_Not_Billed_SummaryModel.A6_031_Not_Billed_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            var log_BillingControlReport_OccupancyVerifications = dbCache.Log_BillingControlReport_OccupancyVerifications;
            var log_BillingControlReport_NotBilledVerifications = dbCache.Log_BillingControlReport_NotBilledVerifications;


            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A6_031_Not_Billed_SummaryModel.A6_031_Not_Billed_SummaryItem item = new A6_031_Not_Billed_SummaryModel.A6_031_Not_Billed_SummaryItem()
                {

                };

                string key = $"A6_031_Not_Billed_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A6_031_Not_Billed_SummaryModel.A6_031_Not_Billed_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        CustomersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        //MetersCheckedPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        MetersNotBilledPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        MetersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Reviewed,
                    };

                    var uniqueSerials = (from p in dbCache.SkybillCustomers
                                         where p.CompanyID == company.CompanyID
                                         select p.Serial_No).Distinct().ToList();

                    var uniqueCustomerNumbers = (from p in dbCache.SkybillCustomers
                                                 where p.CompanyID == company.CompanyID
                                                 select p.Customer_No).Distinct().ToList();

                    foreach (var customerNo in uniqueCustomerNumbers)
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID && p.Customer_No == customerNo).FirstOrDefault();
                        if (sC != null)
                        {
                            if (item.CustomersPerAccountTypeCount.ContainsKey(sC.AccountType))
                                item.CustomersPerAccountTypeCount[sC.AccountType] = item.CustomersPerAccountTypeCount[sC.AccountType] + 1;
                            else
                                item.CustomersPerAccountTypeCount.Add(sC.AccountType, 1);
                        }
                    }

                    foreach (var serial in uniqueSerials)
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID && p.Serial_No == serial).FirstOrDefault();
                        if (sC != null)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == serial).FirstOrDefault();
                            // Device is Active
                            if (localDevice != null && localDevice.ActiveStatusID.HasValue && (ActiveStatus)localDevice.ActiveStatusID.Value == ActiveStatus.Active)
                            {
                                if (item.MetersPerAccountTypeCount.ContainsKey(sC.AccountType))
                                    item.MetersPerAccountTypeCount[sC.AccountType] = item.MetersPerAccountTypeCount[sC.AccountType] + 1;
                                else
                                    item.MetersPerAccountTypeCount.Add(sC.AccountType, 1);

                                //#region Has it been checked

                                //var log_BillingControlReport_NotBilledVerification = log_BillingControlReport_NotBilledVerifications.Where(p => p.CompanyID == companyID && p.SerialNo == serial && p.CreateDate.Date == DateTime.Now.Date).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                                //if (log_BillingControlReport_NotBilledVerification != null)
                                //{
                                //    if (model.MetersCheckedPerAccountTypeCount.ContainsKey(sC.AccountType))
                                //        model.MetersCheckedPerAccountTypeCount[sC.AccountType] = model.MetersCheckedPerAccountTypeCount[sC.AccountType] + 1;
                                //    else
                                //        model.MetersCheckedPerAccountTypeCount.Add(sC.AccountType, 1);
                                //}

                                //#endregion

                                #region Is not billed

                                if (sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.MyWallet)
                                {
                                    bool isOccupied = true;
                                    var occupancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                                    if (occupancy != null && occupancy.Occupancy != "Occupied")
                                        isOccupied = false;
                                    // Customer marked as occupied
                                    if (isOccupied)
                                    {
                                        // Last 7 days usits billed is 0
                                        var total = dbCache.GetDeviceBillingTotal(localDevice.Id, DateTime.Now.AddDays(-6).Date, DateTime.Now.Date);
                                        if (total == null || total.Units == 0)
                                        {
                                            bool isConnected = _client.IsDeviceContactorConnected(localDevice.DeviceIDLinked);

                                            if (isConnected)
                                            {
                                                if (item.MetersNotBilledPerAccountTypeCount.ContainsKey(sC.AccountType))
                                                    item.MetersNotBilledPerAccountTypeCount[sC.AccountType] = item.MetersNotBilledPerAccountTypeCount[sC.AccountType] + 1;
                                                else
                                                    item.MetersNotBilledPerAccountTypeCount.Add(sC.AccountType, 1);
                                            }
                                        }
                                    }
                                }

                                #endregion

                            }


                        }
                    }
                    if (item.MetersNotBilledCount > 0)
                        item.Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Outstanding;

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Reviewed)
                {
                    model.A6_031_Not_Billed_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A6_031_Not_Billed_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A6_041_Billing_Blocked_Summary")]
        public async Task<IActionResult> A6_041_Billing_Blocked_Summary()
        {
            A6_041_Billing_Blocked_SummaryModel model = new A6_041_Billing_Blocked_SummaryModel()
            {
                A6_041_Billing_Blocked_SummaryItems = new List<A6_041_Billing_Blocked_SummaryModel.A6_041_Billing_Blocked_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A6_041_Billing_Blocked_SummaryModel.A6_041_Billing_Blocked_SummaryItem item = new A6_041_Billing_Blocked_SummaryModel.A6_041_Billing_Blocked_SummaryItem()
                {

                };

                string key = $"A6_041_Billing_Blocked_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A6_041_Billing_Blocked_SummaryModel.A6_041_Billing_Blocked_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var localDevices = dbCache.Devices;
                    var skybillSerialNosBlocked = (from p in dbCache.SkybillCustomers
                                                   where p.CompanyID == uC.CompanyID
                                                   && p.Blocked != null
                                                   && p.Blocked.Trim() != ""
                                                   select p.Serial_No).Distinct().ToList();

                    var skybillCustomers = dbCache.SkybillCustomers;

                    foreach (var serial in skybillSerialNosBlocked)
                    {

                        var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                        if (localDev == null || !localDev.ActiveStatusID.HasValue || localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                            continue;

                        var sC = skybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                        if (sC.AccountType == AccountTypeEnum.PrepaidCredit)
                            continue;

                        if (sC.Customer_No.StartsWith("SUP"))
                            continue;

                        item.BlockedCount++;
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A06_BillingControlReport.StatusType.Ok)
                {
                    model.A6_041_Billing_Blocked_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A6_041_Billing_Blocked_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A7_021_Credit_Control_Summary")]
        public async Task<IActionResult> A7_021_Credit_Control_Summary()
        {
            A7_021_Credit_Control_SummaryModel model = new A7_021_Credit_Control_SummaryModel()
            {
                A7_021_Credit_Control_SummaryItems = new List<A7_021_Credit_Control_SummaryModel.A7_021_Credit_Control_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A7_021_Credit_Control_SummaryModel.A7_021_Credit_Control_SummaryItem item = new A7_021_Credit_Control_SummaryModel.A7_021_Credit_Control_SummaryItem()
                {

                };

                string key = $"A7_021_Credit_Control_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A7_021_Credit_Control_SummaryModel.A7_021_Credit_Control_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        A07_CreditControlAndNotifierProcess_CreditControlSummaryItems = new List<Models.OperationalModels.A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_CreditControlSummaryModel.A07_CreditControlAndNotifierProcess_CreditControlSummaryItem>()
                    };

                    var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                              where p.CompanyID == uC.CompanyID
                                              select p.Customer_No).Distinct().ToList();

                    item.CustomersCount = skybillCustomerNos.Count;

                    foreach (var customerNo in skybillCustomerNos)
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                        AccountTypeEnum accountType = sC.AccountType;
                        decimal balance = sC.RealBalance;

                        if (balance < 0)
                        {
                            var existing = (from p in item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems
                                            where p.AccountType == accountType
                                            select p).SingleOrDefault();

                            if (existing != null)
                            {
                                // Update
                                item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Count = item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Count + 1;
                                item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Amount = item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Amount + balance;
                            }
                            else
                            {
                                // New
                                item.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Add(new Models.OperationalModels.A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_CreditControlSummaryModel.A07_CreditControlAndNotifierProcess_CreditControlSummaryItem()
                                {
                                    AccountType = accountType,
                                    Amount = balance,
                                    Count = 1
                                });
                            }
                        }


                    }


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok)
                {
                    model.A7_021_Credit_Control_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A7_021_Credit_Control_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A7_031_Meter_Mode_Summary")]
        public async Task<IActionResult> A7_031_Meter_Mode_Summary()
        {
            A7_031_Meter_Mode_SummaryModel model = new A7_031_Meter_Mode_SummaryModel()
            {
                A7_031_Meter_Mode_SummaryItems = new List<A7_031_Meter_Mode_SummaryModel.A7_031_Meter_Mode_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A7_031_Meter_Mode_SummaryModel.A7_031_Meter_Mode_SummaryItem item = new A7_031_Meter_Mode_SummaryModel.A7_031_Meter_Mode_SummaryItem()
                {

                };

                string key = $"A7_031_Meter_Mode_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A7_031_Meter_Mode_SummaryModel.A7_031_Meter_Mode_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                              where p.CompanyID == uC.CompanyID
                                              select p.Customer_No).Distinct().ToList();

                    var skybillCustomers = (from p in dbCache.SkybillCustomers
                                            where p.CompanyID == uC.CompanyID
                                            select p).ToList();

                    MeterProvider meterProvider = new MeterProvider(DateTime.Now, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _accessor, _configuration, _options, _APIoptions);
                    List<string> metersChecked = new List<string>();

                    item.CustomersCount = skybillCustomerNos.Count;

                    foreach (var sC in skybillCustomers)
                    {
                        if (metersChecked.Contains(sC.Serial_No))
                            continue;
                        metersChecked.Add(sC.Serial_No);


                        var result = meterProvider.GetMeterMode(sC);

                        if (result != null)
                        {
                            item.MeterCount++;
                            if (result.NoModeInSkybill)
                                item.NoModeInSkybillCount++;
                            if (result.CreditOnWallet)
                                item.CreditOnWallerMeterCount++;
                            if (result.MeterInPostPaidMode)
                                item.MeterInPostPaidModeCount++;
                        }

                    }


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok)
                {
                    model.A7_031_Meter_Mode_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A7_031_Meter_Mode_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A7_041_Meter_On_Manual_Summary")]
        public async Task<IActionResult> A7_041_Meter_On_Manual_Summary()
        {
            A7_041_Meter_On_Manual_SummaryModel model = new A7_041_Meter_On_Manual_SummaryModel()
            {
                A7_041_Meter_On_Manual_SummaryItems = new List<A7_041_Meter_On_Manual_SummaryModel.A7_041_Meter_On_Manual_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A7_041_Meter_On_Manual_SummaryModel.A7_041_Meter_On_Manual_SummaryItem item = new A7_041_Meter_On_Manual_SummaryModel.A7_041_Meter_On_Manual_SummaryItem()
                {

                };

                string key = $"A7_041_Meter_On_Manual_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A7_041_Meter_On_Manual_SummaryModel.A7_041_Meter_On_Manual_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                              where p.CompanyID == uC.CompanyID
                                              select p.Customer_No).Distinct().ToList();

                    var skybillCustomers = (from p in dbCache.SkybillCustomers
                                            where p.CompanyID == uC.CompanyID
                                            select p).ToList();

                    var notificationSettings = dbCache.NotificationCustomerMeters;
                    var localdevices = dbCache.Devices;

                    List<string> metersChecked = new List<string>();

                    item.CustomersCount = skybillCustomerNos.Count;

                    foreach (var sC in skybillCustomers)
                    {
                        if (metersChecked.Contains(sC.Serial_No))
                            continue;
                        metersChecked.Add(sC.Serial_No);

                        var localDevice = localdevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                        if (localDevice == null || !localDevice.TypeID.HasValue)
                            continue;

                        var deviceType = (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value;

                        if (deviceType != DeviceType.DeviceTypeEnum.Electricity)
                            continue;

                        item.MeterCount++;

                        var autoDisconnectSettings = (from p in notificationSettings
                                                      where p.MeterSerial == sC.Serial_No
                                                      orderby p.LastUpdated descending
                                                      select p).FirstOrDefault();

                        if (autoDisconnectSettings == null || !autoDisconnectSettings.AutoDisconnect)
                            item.ElecMetersOnManual++;

                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok)
                {
                    model.A7_041_Meter_On_Manual_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A7_041_Meter_On_Manual_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A7_043_Meter_On_Manual_Request_Summary")]
        public async Task<IActionResult> A7_043_Meter_On_Manual_Request_Summary()
        {
            A7_043_Meter_On_Manual_Request_SummaryModel model = new A7_043_Meter_On_Manual_Request_SummaryModel()
            {
                A7_043_Meter_On_Manual_Request_SummaryItems = new List<A7_043_Meter_On_Manual_Request_SummaryModel.A7_043_Meter_On_Manual_Request_SummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var a07_CreditControlAndNotifierProcess_MeterOnManualRequests = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A7_043_Meter_On_Manual_Request_SummaryModel.A7_043_Meter_On_Manual_Request_SummaryItem item = new A7_043_Meter_On_Manual_Request_SummaryModel.A7_043_Meter_On_Manual_Request_SummaryItem()
                {

                };

                string key = $"A7_043_Meter_On_Manual_Request_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A7_043_Meter_On_Manual_Request_SummaryModel.A7_043_Meter_On_Manual_Request_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    item.PendingCount = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == uC.CompanyID && !p.ApprovedDate.HasValue).Count();
                    item.ResolvedCount = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == uC.CompanyID && p.ApprovedDate.HasValue).Count();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok)
                {
                    model.A7_043_Meter_On_Manual_Request_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A7_043_Meter_On_Manual_Request_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A7_051_Connector_Summary")]
        public async Task<IActionResult> A7_051_Connector_Summary()
        {
            A7_051_Connector_SummaryModel model = new A7_051_Connector_SummaryModel()
            {
                A7_051_Connector_SummaryItems = new List<A7_051_Connector_SummaryModel.A7_051_Connector_SummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var latestConnectionRunID = (from p in db.ConnectionRun_Customers
                                         orderby p.ActionDate descending
                                         select p.ConnectionRunID).FirstOrDefault();

            var latestConnectionRunCustomers = (from p in db.ConnectionRun_Customers
                                                where p.ConnectionRunID == latestConnectionRunID
                                                select p).ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A7_051_Connector_SummaryModel.A7_051_Connector_SummaryItem item = new A7_051_Connector_SummaryModel.A7_051_Connector_SummaryItem()
                {

                };

                string key = $"A7_051_Connector_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new A7_051_Connector_SummaryModel.A7_051_Connector_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                    };

                    var latestConnectionRunCompany = (from p in latestConnectionRunCustomers
                                                      where p.CompanyID == uC.CompanyID
                                                      orderby p.ActionDate descending
                                                      select p).FirstOrDefault();

                    if (latestConnectionRunCompany == null)
                        continue;

                    item.LatestConnectionRun = latestConnectionRunCompany.ActionDate;

                    int onlineCount = 0;
                    int offlineCount = 0;
                    int lessThan4HoursCount = 0;
                    int lessThan24HoursCount = 0;
                    int lessThan3DaysCount = 0;
                    int lessThan7DaysCount = 0;
                    int moreThan7DaysCount = 0;

                    foreach (var dev in latestConnectionRunCustomers.Where(c => c.CompanyID == uC.CompanyID).ToList())
                    {
                        var m2mDev = dbCache.GetDevice(dev.MeterNumber);

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

                    item.OnlineCount = onlineCount;
                    item.OfflineCount = offlineCount;
                    item.LessThan4HoursCount = lessThan4HoursCount;
                    item.LessThan24HoursCount = lessThan24HoursCount;
                    item.LessThan3DaysCount = lessThan3DaysCount;
                    item.LessThan7DaysCount = lessThan7DaysCount;
                    item.MoreThan7DaysCount = moreThan7DaysCount;


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.Status != Models.OperationalModels.A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem.StatusType.Ok)
                {
                    model.A7_051_Connector_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A7_051_Connector_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A8_011_Tasks_Company_Summary")]
        public async Task<IActionResult> A8_011_Tasks_Company_Summary()
        {
            A8_011_Tasks_Company_SummaryModel model = new A8_011_Tasks_Company_SummaryModel()
            {
                A8_011_Tasks_Company_SummaryItems = new List<A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem>(),
                A08_Tasks_Company_SummaryStatusItems = new List<A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem item = new A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem()
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
                model.A08_Tasks_Company_SummaryStatusItems.Add(item);
            }

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (company == null)
                    company = new Company()
                    {
                        CompanyID = 0,
                        Name = "None",
                        IsFlagStatusActive = true,
                    };

                if (!company.IsFlagStatusActive)
                    continue;

                List<Data.A08_Task> a09_Tasks = (from p in db.A08_Tasks
                                                 where p.DueDate.HasValue
                                                 //&& p.DueDate.Value.Date >= DateTime.Now.AddMonths(-1).Date
                                                 //&& p.DueDate.Value.Date <= DateTime.Now.Date
                                                 && p.Level.HasValue
                                                 && (p.Level.Value == 4 || p.Level.Value == 5)
                                                 select p).ToList();
                if (uC.CompanyID == 0)
                    a09_Tasks = a09_Tasks.Where(p => !p.CompanyID.HasValue).ToList();
                else
                    a09_Tasks = a09_Tasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

                if (a09_Tasks.Count > 0)
                {
                    A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem item = new A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        A08_Tasks_Company_SummaryItemStatuses = new List<A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus itemStatus = new A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus()
                        {
                            Count = a09_Tasks.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                        };

                        item.A08_Tasks_Company_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var task in a09_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                        if (task.DueDate.Value.Date == DateTime.Now.Date)
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


                    var oldestFlag = a09_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                    if (oldestFlag != null)
                    {
                        item.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedTaskID = oldestFlag.ID;
                        item.OldestUnresolvedTaskTypeID = oldestFlag.TaskTypeID;
                    }

                    if (item.TodayCount != 0
                        || item.OlderThan14DaysCount != 0
                        || item.OlderThan3DaysCount != 0
                        || item.OlderThan1DayCount != 0
                        || item.OlderThan1MonthCount != 0
                        || item.OlderThan7DaysCount != 0)
                    {
                        model.A8_011_Tasks_Company_SummaryItems.Add(item);
                    }
                }

            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A8_011_Tasks_Company_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A9_011_Flags_Company_Summary")]
        public async Task<IActionResult> A9_011_Flags_Company_Summary()
        {
            A8_011_Tasks_Company_SummaryModel model = new A8_011_Tasks_Company_SummaryModel()
            {
                A8_011_Tasks_Company_SummaryItems = new List<A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem>(),
                A08_Tasks_Company_SummaryStatusItems = new List<A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.Where(p => !p.IsDeleted/* && p.StatusGroupID == 3*/).ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();

            foreach (var status in siteAdmin_Statuses)
            {
                A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem item = new A8_011_Tasks_Company_SummaryModel.A08_Tasks_Company_SummaryStatusItem()
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
                model.A08_Tasks_Company_SummaryStatusItems.Add(item);
            }

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (company == null)
                    company = new Company()
                    {
                        CompanyID = 0,
                        Name = "None",
                        IsFlagStatusActive = true,
                    };

                if (!company.IsFlagStatusActive)
                    continue;

                var a09_Tasks = (from p in db.A09_Flags
                                 where p.DueDate.HasValue
                                 //&& p.DueDate.Value.Date >= DateTime.Now.AddMonths(-1).Date
                                 //&& p.DueDate.Value.Date <= DateTime.Now.Date
                                 && p.Level.HasValue
                                 && (p.Level.Value == 4 || p.Level.Value == 5)
                                 select p).ToList();
                if (uC.CompanyID == 0)
                    a09_Tasks = a09_Tasks.Where(p => !p.CompanyID.HasValue).ToList();
                else
                    a09_Tasks = a09_Tasks.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();

                if (a09_Tasks.Count > 0)
                {
                    A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem item = new A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        A08_Tasks_Company_SummaryItemStatuses = new List<A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus>(),
                    };

                    foreach (var status in siteAdmin_Statuses)
                    {
                        A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus itemStatus = new A8_011_Tasks_Company_SummaryModel.A8_011_Tasks_Company_SummaryItem.A08_Tasks_Company_SummaryItemStatus()
                        {
                            Count = a09_Tasks.Where(p => p.StatusID == status.ID).Count(),
                            StatusID = status.ID,
                        };

                        item.A08_Tasks_Company_SummaryItemStatuses.Add(itemStatus);
                    }

                    foreach (var task in a09_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
                    {
                        TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                        if (task.DueDate.Value.Date == DateTime.Now.Date)
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


                    var oldestFlag = a09_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
                    if (oldestFlag != null)
                    {
                        item.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                        item.OldestUnresolvedTaskID = oldestFlag.ID;
                        item.OldestUnresolvedTaskTypeID = oldestFlag.FlagTypeID;
                    }

                    if (item.TodayCount != 0
                        || item.OlderThan14DaysCount != 0
                        || item.OlderThan3DaysCount != 0
                        || item.OlderThan1DayCount != 0
                        || item.OlderThan1MonthCount != 0
                        || item.OlderThan7DaysCount != 0)
                    {
                        model.A8_011_Tasks_Company_SummaryItems.Add(item);
                    }
                }

            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A9_011_Flags_Company_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_A/A10_021_VirtualMeters_Summary")]
        public async Task<IActionResult> A10_021_VirtualMeters_Summary()
        {
            A10_021_VirtualMeters_SummaryModel model = new A10_021_VirtualMeters_SummaryModel()
            {
                A10_021_VirtualMeters_SummaryItems = new List<A10_021_VirtualMeters_SummaryModel.A10_021_VirtualMeters_SummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            MVCache dbCache = new MVCache(_configuration, _cache, db, apiDB, _options, _APIoptions);
            var latestConnectionRunID = (from p in db.ConnectionRun_Customers
                                         orderby p.ActionDate descending
                                         select p.ConnectionRunID).FirstOrDefault();

            var latestConnectionRunCustomers = (from p in db.ConnectionRun_Customers
                                                where p.ConnectionRunID == latestConnectionRunID
                                                select p).ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                A10_021_VirtualMeters_SummaryModel.A10_021_VirtualMeters_SummaryItem item = new A10_021_VirtualMeters_SummaryModel.A10_021_VirtualMeters_SummaryItem()
                {

                };

                string key = $"A10_021_VirtualMeters_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var localDevices = (from p in db.Devices
                                        where p.CompanyID.HasValue
                                        && p.CompanyID.Value == uC.CompanyID
                                        && (p.Serial.ToUpper().Contains("Peak".ToUpper())
                                        || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                        || p.Serial.ToUpper().Contains("Standard".ToUpper()))
                                        select p).ToList();

                    var masterMeters = (from p in localDevices
                                        select p.Serial.ToUpper().Replace("OffPeak".ToUpper(), string.Empty).Replace("Peak".ToUpper(), string.Empty).Replace("Standard".ToUpper(), string.Empty).Replace("-".ToUpper(), string.Empty)).Distinct().ToList();

                    item = new A10_021_VirtualMeters_SummaryModel.A10_021_VirtualMeters_SummaryItem()
                    {
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        IsFlagStatusActive = company.IsFlagStatusActive,
                        Name = company.Name,
                        PartnerID = company.PartnerID,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        MasterMeters = masterMeters.Count,
                        TOUMeters = localDevices.Count,
                    };


                    var sbCustomers = db.SkybillCustomers.ToList();

                    var tOU_Holidays = apiDB.TOU_Holidays.ToList();
                    var tOU_Hours = apiDB.TOU_Hours.ToList();
                    var tOU_DemandTypeMonths = apiDB.TOU_DemandTypeMonths.ToList();

                    foreach (var masterSerial in masterMeters)
                    {
                        var dev = (from p in localDevices
                                   where p.Serial.ToUpper().Contains(masterSerial.ToUpper())
                                   select p).FirstOrDefault();

                        var sC = sbCustomers.Where(p => p.Serial_No == dev.Serial).FirstOrDefault();

                        if (sC == null)
                            continue;

                        string originalDeviceSerial = masterSerial;
                        //var originalDevice = apiDB.Devices.Where(p => p.Serial == originalDeviceSerial).SingleOrDefault();

                        Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem detailsItem = new Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem()
                        {
                            LinkedSerials = new List<string>(),
                            Device = dev,
                            SkybillCustomer = sC,
                            Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault(),
                            MasterSerial = masterSerial,
                        };

                        //if (originalDevice != null)
                        //{

                        var linkedDevices = (from p in apiDB.Devices
                                             where p.Serial.Contains(originalDeviceSerial)
                                             &&
                                             (
                                             p.Serial.ToUpper().Contains("Peak".ToUpper())
                                             || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                             || p.Serial.ToUpper().Contains("Standard".ToUpper())
                                             )
                                             select p).ToList();

                        if (linkedDevices.Count > 0)
                        {
                            detailsItem.LinkedSerials = linkedDevices.Select(p => p.Serial).ToList();
                            DateTime openingReadingTimeLogged = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day);
                            DateTime closingReadingTimeLogged = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                            Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading openingReading_item = new Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading()
                            {
                                TimeLogged = openingReadingTimeLogged,
                                A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                            };

                            var tOULookupResultOpening = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(openingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                            if (tOULookupResultOpening != null)
                            {
                                openingReading_item.TOUDayType = tOULookupResultOpening.TOUDayType;
                                openingReading_item.TOUDemandType = tOULookupResultOpening.TOUDemandType;
                                openingReading_item.TOUPeakType = tOULookupResultOpening.TOUPeakType;
                                openingReading_item.TOU_Holiday = tOULookupResultOpening.TOU_Holiday;
                            }

                            foreach (var lDev in linkedDevices)
                            {
                                decimal? virtualOdo = null;
                                decimal? counter = null;
                                decimal? diff = null;
                                var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == openingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                                if (reading != null)
                                {
                                    virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                                    counter = (reading.PulseCounter / 1000.0m);
                                    diff = 0;//(reading.Difference / 1000.0m);
                                }

                                Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                                {
                                    VirtualOdoReading = virtualOdo,
                                    Serial = lDev.Serial,
                                    Counter = counter,
                                    Difference = diff,
                                };


                                var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, detailsItem.Company.Name, new DateTime(openingReadingTimeLogged.Year, openingReadingTimeLogged.Month, 1));
                                if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                                {
                                    if (
                                        (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDev.Serial.ToUpper().Contains("-OffPeak".ToUpper()))
                                        || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDev.Serial.ToUpper().Contains("-Standard".ToUpper()))
                                        || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDev.Serial.ToUpper().Contains("-Peak".ToUpper()))
                                        )
                                    {
                                        openingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDev.Serial.ToUpper()).FirstOrDefault();
                                        meterItem.Tariff = openingReading_item.TenantConsumptionStatementItem != null ? openingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                                    }
                                }

                                openingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                            }

                            detailsItem.OpeningReading = openingReading_item;

                            Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading closingReading_item = new Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading()
                            {
                                TimeLogged = closingReadingTimeLogged,
                                A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                            };

                            var tOULookupResultClosing = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(closingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                            if (tOULookupResultClosing != null)
                            {
                                closingReading_item.TOUDayType = tOULookupResultClosing.TOUDayType;
                                closingReading_item.TOUDemandType = tOULookupResultClosing.TOUDemandType;
                                closingReading_item.TOUPeakType = tOULookupResultClosing.TOUPeakType;
                                closingReading_item.TOU_Holiday = tOULookupResultClosing.TOU_Holiday;
                            }

                            foreach (var lDev in linkedDevices)
                            {
                                decimal? virtualOdo = null;
                                decimal? counter = null;
                                decimal? diff = null;
                                string lDevSerial = lDev.Serial;
                                var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == closingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                                if (reading != null)
                                {
                                    virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                                    counter = (reading.PulseCounter / 1000.0m);
                                    if (detailsItem.OpeningReading != null && detailsItem.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).Count() > 0)
                                    {
                                        diff = virtualOdo - detailsItem.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).SingleOrDefault().VirtualOdoReading;
                                    }
                                    else
                                    {
                                        diff = (reading.Difference / 1000.0m);
                                    }
                                }

                                Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new Models.OperationalModels.A10_VirtualMeters.A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                                {
                                    VirtualOdoReading = virtualOdo,
                                    Serial = lDevSerial,
                                    Counter = counter,
                                    Difference = diff,
                                };


                                var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, detailsItem.Company.Name, new DateTime(closingReadingTimeLogged.Year, closingReadingTimeLogged.Month, 1));
                                if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                                {
                                    //if (
                                    //    (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDevSerial.ToUpper().Contains("-OffPeak".ToUpper()))
                                    //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDevSerial.ToUpper().Contains("-Standard".ToUpper()))
                                    //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDevSerial.ToUpper().Contains("-Peak".ToUpper()))
                                    //    )
                                    //{
                                    closingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDevSerial.ToUpper()).FirstOrDefault();
                                    meterItem.Tariff = closingReading_item.TenantConsumptionStatementItem != null ? closingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                                    //}
                                }

                                closingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                            }

                            detailsItem.ClosingReading = closingReading_item;


                        }
                        //}
                        if (detailsItem.OpeningReading != null || detailsItem.ClosingReading != null)
                        {
                            if (detailsItem.ClosingReading.CounterDiff >= 1000
                                || detailsItem.ClosingReading.CounterDiff <= -1000)
                            {
                                item.MetersWithDiff++;
                            }
                        }
                    }


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item.MetersWithDiff > 0)
                {
                    model.A10_021_VirtualMeters_SummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_A_A10_021_VirtualMeters_Summary.cshtml", model);
        }

    }
}
