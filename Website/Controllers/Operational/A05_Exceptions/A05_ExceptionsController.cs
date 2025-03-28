using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A05_Exceptions.A05_ExceptionsModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.A05_Exceptions
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A05_ExceptionsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;

        public A05_ExceptionsController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            OperationalProvider operationalProvider
            )
        {
            _configuration = configuration;
            _userManager = userManager;
            _operationalProvider = operationalProvider;
            _options = options;
            _APIoptions = APIoptions;
            _cache = cache;
            _context = context;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
        }

        [Route("/operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Summary")]
        [HttpGet]
        public async Task<ActionResult> A05_Exceptions_MidnightSyncHistory_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A05_Exceptions_MidnightSyncHistory_SummaryModel model = new A05_Exceptions_MidnightSyncHistory_SummaryModel()
            {
                A05_Exceptions_MidnightSyncHistory_SummaryItems = new List<A05_Exceptions_MidnightSyncHistory_SummaryModel.A05_Exceptions_MidnightSyncHistory_SummaryItem>(),
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var localDevices = dbCache.Devices.ToList();
            var skybillCustomers = dbCache.SkybillCustomers.ToList();

            var midnightSyncExceptions = (from p in dbCache.DeviceReadingsMidnightSyncs
                                          where p.WhereDidIFindThis == "OldReading"
                                          select p).ToList();

            foreach (var msItem in midnightSyncExceptions)
            {
                A05_Exceptions_MidnightSyncHistory_SummaryModel.A05_Exceptions_MidnightSyncHistory_SummaryItem item = new A05_Exceptions_MidnightSyncHistory_SummaryModel.A05_Exceptions_MidnightSyncHistory_SummaryItem()
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
                    DeviceStatus = "Unknown",
                };

                if (_operationalProvider.CompanyID > 0)
                {
                    if (item.SkybillCustomer == null || item.SkybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;
                }

                var m2mDev = _client.GetDeviceByMeterNumber(msItem.m2mSerial);
                if (m2mDev != null)
                {
                    item.DeviceStatus = m2mDev.deviceStatus.ToUpper();
                    if (m2mDev.status != null)
                        item.DeviceLastCommunicated = m2mDev.status.time;
                }

                model.A05_Exceptions_MidnightSyncHistory_SummaryItems.Add(item);
            }

            return View("~/Views/Operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Summary.cshtml", model);
        }

        [Route("/operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Results")]
        [HttpGet]
        public async Task<ActionResult> A05_Exceptions_MidnightSyncHistory_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A05_Exceptions_MidnightSyncHistory_ResultsModel model = new A05_Exceptions_MidnightSyncHistory_ResultsModel()
            {
                A05_Exceptions_MidnightSyncHistory_ResultsItems = new List<A05_Exceptions_MidnightSyncHistory_ResultsModel.A05_Exceptions_MidnightSyncHistory_ResultsItem>(),
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices.ToList();
            var skybillCustomers = dbCache.SkybillCustomers.ToList();

            var midnightSyncExceptions = (from p in dbCache.DeviceReadingsMidnightSyncs
                                              //where p.WhereDidIFindThis == "OldReading"
                                          select p).ToList();

            foreach (var msItem in midnightSyncExceptions)
            {
                A05_Exceptions_MidnightSyncHistory_ResultsModel.A05_Exceptions_MidnightSyncHistory_ResultsItem item = new A05_Exceptions_MidnightSyncHistory_ResultsModel.A05_Exceptions_MidnightSyncHistory_ResultsItem()
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

                if (_operationalProvider.CompanyID > 0)
                {
                    if (item.SkybillCustomer == null || item.SkybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;
                }

                model.A05_Exceptions_MidnightSyncHistory_ResultsItems.Add(item);
            }

            return View("~/Views/Operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Results.cshtml", model);
        }

        [Route("/operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Details")]
        [HttpGet]
        public async Task<ActionResult> A05_Exceptions_MidnightSyncHistory_Details(int? year, int? month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A05_Exceptions_MidnightSyncHistory_DetailsModel model = new A05_Exceptions_MidnightSyncHistory_DetailsModel()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                model = new A05_Exceptions_MidnightSyncHistory_DetailsModel()
                {
                    Device = dbCache.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault(),
                    DeviceReadingsMidnightSync = dbCache.DeviceReadingsMidnightSyncs.Where(p => p.m2mSerial == _operationalProvider.CustomerMeterSerial).FirstOrDefault(),
                    SkybillCustomer = dbCache.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault(),
                };
            }

            return View("~/Views/Operational/A05_Exceptions/A05_Exceptions_MidnightSyncHistory_Details.cshtml", model);
        }

    }
}
