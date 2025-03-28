using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.CompanyAdminViewModels;
using MyVoltage.Models.OperationalModels.H_Device_AdministratorModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.J_Finance
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class H_Device_AdministratorController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;
        private readonly LoggingProvider _loggingProvider;

        public H_Device_AdministratorController(
            LoggingProvider loggingProvider,
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
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _emailSender = emailSender;
            _loggingProvider = loggingProvider;
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview")]
        public async Task<IActionResult> H_Device_Administrator_DeviceOverview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_DeviceOverview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_DeviceOverview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            H_Device_Administrator_DeviceOverviewModel model = new H_Device_Administrator_DeviceOverviewModel()
            {
                H_Device_Administrator_DeviceOverviewItems = new List<H_Device_Administrator_DeviceOverviewModel.H_Device_Administrator_DeviceOverviewItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            List<Data.Gateway> gateways = new List<Gateway>();
            if (_operationalProvider.CompanyID == 0)
                gateways = db.Gateways.Where(p => !p.CompanyID.HasValue).ToList();
            else
                gateways = db.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            var api2GWs = _client.GetGateways(2);
            foreach (var gw in gateways)
            {
                if (gw.DeviceAPIIDValue == 1)
                {
                    var m2mGW = dbCache.M2MGateways.Where(p => p.id == gw.GatewayID).SingleOrDefault();

                    if (m2mGW != null)
                    {
                        H_Device_Administrator_DeviceOverviewModel.H_Device_Administrator_DeviceOverviewItem item = new H_Device_Administrator_DeviceOverviewModel.H_Device_Administrator_DeviceOverviewItem()
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
                            DeviceAPIID = gw.DeviceAPIID,
                        };

                        var m2mGWDevices = dbCache.GetGatewayDevices(gw.GatewayID);

                        if (m2mGWDevices != null)
                        {
                            item.DevicesLinked += m2mGWDevices.Count;
                        }

                        if (m2mGW.status.id != 1)
                        {
                            TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);

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
                        //else
                        //    continue;

                        model.H_Device_Administrator_DeviceOverviewItems.Add(item);

                    }
                }
                else
                {
                    var m2mGW = api2GWs.Where(p => p.id == gw.GatewayID).SingleOrDefault();

                    if (m2mGW != null)
                    {
                        H_Device_Administrator_DeviceOverviewModel.H_Device_Administrator_DeviceOverviewItem item = new H_Device_Administrator_DeviceOverviewModel.H_Device_Administrator_DeviceOverviewItem()
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
                            DeviceAPIID = gw.DeviceAPIID,
                        };

                        var m2mGWDevices = _client.GetGatewayDevices(gw.GatewayID.ToString(), 2).ToList();

                        if (m2mGWDevices != null)
                        {
                            item.DevicesLinked += m2mGWDevices.Count;
                        }

                        if (m2mGW.status.id != 1)
                        {
                            TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);

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
                        //else
                        //    continue;

                        model.H_Device_Administrator_DeviceOverviewItems.Add(item);

                    }
                }
            }




            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_Device/{gatewayID}")]
        public async Task<IActionResult> H_Device_Administrator_DeviceOverview_Device(int gatewayID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_DeviceOverview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_DeviceOverview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            H_Device_Administrator_DeviceOverview_DeviceModel model = new H_Device_Administrator_DeviceOverview_DeviceModel()
            {
                H_Device_Administrator_DeviceOverview_DeviceItems = new List<H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem>(),
                GatewayName = "None",
                GatewayID = gatewayID,
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            //List<Data.Device> devices = new List<Data.Device>();

            //devices = dbCache.Devices.Where(p => p.GatewayID.HasValue && p.GatewayID.Value == gatewayID).ToList();
            var localGW = (from p in dbCache.Gateways
                           where p.GatewayID == gatewayID
                           select p).FirstOrDefault();
            if (localGW != null)
            {
                if ((localGW.CompanyID.HasValue ? localGW.CompanyID.Value : 0) != _operationalProvider.CompanyID)
                    return Redirect($"/operational/changeActiveCompany/{localGW.CompanyID}?R={HttpUtility.UrlEncode($"/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_Device/{gatewayID}")}");

                var m2mGW = _client.GetGateway(gatewayID.ToString(), localGW.DeviceAPIIDValue);
                if (m2mGW != null)
                {
                    model.GatewayName = $"{gatewayID} - {m2mGW.name}";

                    if (localGW != null && localGW.CompanyID.HasValue)
                    {
                        var company = _operationalProvider.Companies.Where(p => p.CompanyID == localGW.CompanyID.Value).SingleOrDefault();
                        model.GatewayName = $"{gatewayID} - {m2mGW.name} ({company.Name})";
                    }
                }

                var m2mGWDevices = _client.GetGatewayDevices(gatewayID.ToString(), localGW.DeviceAPIIDValue);

                foreach (var dev in m2mGWDevices)
                {
                    H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem item = new H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem()
                    {
                        id = dev.id,
                        name = dev.name,
                        serial = dev.serial,
                    };

                    model.H_Device_Administrator_DeviceOverview_DeviceItems.Add(item);
                }
            }

            if (model.H_Device_Administrator_DeviceOverview_DeviceItems.Count > 0)
                model.H_Device_Administrator_DeviceOverview_DeviceItems = model.H_Device_Administrator_DeviceOverview_DeviceItems.OrderBy(p => p.name).ToList();

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_Device.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_DeviceItem/{gatewayID}/{serial}/{trid}")]
        public async Task<IActionResult> H_Device_Administrator_DeviceOverview_DeviceItem(int gatewayID, string serial, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_DeviceOverview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_DeviceOverview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem model = new H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem()
            {
                TableRowID = trid,
            };

            var localDev = (from p in localDevices
                            where p.Serial == serial
                            && p.GatewayID.HasValue && p.GatewayID.Value == gatewayID
                            select p).FirstOrDefault();

            if (localDev != null)
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial, localDev.DeviceAPIIDValue);

                if (m2mDev != null)
                {
                    var mapping = _client.GetDeviceGatewaysAndMapping(m2mDev.id, localDev.DeviceAPIIDValue);

                    model = new H_Device_Administrator_DeviceOverview_DeviceModel.H_Device_Administrator_DeviceOverview_DeviceItem()
                    {
                        TableRowID = trid,
                        account = m2mDev.account,
                        account_type = m2mDev.account_type,
                        autoDisconnect = m2mDev.autoDisconnect,
                        balance = m2mDev.balance,
                        customer_number = m2mDev.customer_number,
                        GatewayID = 0,
                        gps_coordinates = m2mDev.gps_coordinates,
                        id = m2mDev.id,
                        mapClickable = m2mDev.mapClickable,
                        name = m2mDev.name,
                        OfflineDuration = "",
                        partner_code = m2mDev.partner_code,
                        serial = m2mDev.serial,
                        status = m2mDev.status,
                        type = m2mDev.type,
                        ActiveEnergy = "",
                        RemainingCredit = "",
                        Temp = "",
                        ContactorState = "",
                        InternalBatteryV = "",
                        CTRatio = "",
                        ReactiveEnergy = "",
                        GasConsumption = "",
                        MaxDemand = "",
                        WaterConsumption = "",
                        IsContactorInstalled = localDev != null && localDev.IsContactorInstalled.HasValue ? localDev.IsContactorInstalled.Value : false,
                    };

                    if (mapping != null && mapping.device != null && mapping.device.gateways != null && mapping.device.gateways.Length > 0 && mapping.device.gateways[0].mapping != null)
                    {
                        model.mapping = new DeviceMapping()
                        {
                            index = mapping.device.gateways[0].mapping.index,
                            port = mapping.device.gateways[0].mapping.port,
                            process_interval = mapping.device.gateways[0].mapping.process_interval,
                            last_communicated = mapping.device.gateways[0].mapping.last_communicated.ToString(),
                            protocol = mapping.device.gateways[0].mapping.protocol,
                            remote_address = mapping.device.gateways[0].mapping.remote_address,
                            remote_index = mapping.device.gateways[0].mapping.remote_index,
                        };
                    }

                    if (m2mDev.deviceStatus == "offline")
                    {
                        TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                        if (offlineDuration.TotalHours < 4)
                            model.OfflineDuration = "< 4 H";
                        else if (offlineDuration.TotalHours < 24)
                            model.OfflineDuration = "< 24 H";
                        else if (offlineDuration.TotalDays < 3)
                            model.OfflineDuration = "< 3 D";
                        else if (offlineDuration.TotalDays < 7)
                            model.OfflineDuration = "< 7 D";
                        else if (offlineDuration.TotalDays >= 7)
                            model.OfflineDuration = "> 7 D";
                    }

                    if (localDev.DeviceAPIIDValue == 1)
                    {
                        Dictionary<int, string> registers = new Dictionary<int, string>();
                        registers.Add(29, "readings"); // Max Demand
                        registers.Add(1, "readings"); // Active Energy
                        registers.Add(2, "readings"); // Reactive Energy
                        registers.Add(70, "readings"); // CT Ratio
                        registers.Add(91, "readings"); // Contactor State
                        registers.Add(102, "readings"); // Temp
                        registers.Add(90, "readings"); // Remaining Credit
                        registers.Add(80, "readings"); // Water Consumption
                        registers.Add(140, "readings"); // Gas Consumption

                        registers.Add(100, "readings"); // Internal Battery V
                        registers.Add(101, "readings"); // Signal RSSI
                        registers.Add(106, "readings"); // SNR

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }
                        DateTime startTime = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0);
                        string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                        DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                        string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                        var url = $"devices/{m2mDev.id}/data?start={start}&end={end}&interval=3600{registerStr}";
                        var readingResult = _client.Get<MeterUsageResult>(url, localDev.DeviceAPIIDValue);

                        if (readingResult != null && readingResult.data != null && readingResult.data.registers != null)
                            foreach (var register in readingResult.data.registers)
                            {
                                if (register.readings.Where(p => p.HasValue).Count() == 0)
                                    continue;

                                if (register.name.ToUpper().Contains("Max Demand".ToUpper()))
                                {
                                    model.MaxDemand = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Active Energy".ToUpper()))
                                {
                                    model.ActiveEnergy = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("CT Ratio".ToUpper()))
                                {
                                    model.CTRatio = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Contactor State".ToUpper()))
                                {
                                    model.ContactorState = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                    model.ContactorState = model.ContactorState == "1" ? "Connected" : "Disconnected";
                                }
                                if (register.name.ToUpper().Contains("Temp".ToUpper()))
                                {
                                    model.Temp = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Remaining Credit".ToUpper()))
                                {
                                    model.RemainingCredit = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Water Consumption".ToUpper()))
                                {
                                    model.WaterConsumption = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Gas Consumption".ToUpper()))
                                {
                                    model.GasConsumption = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("Internal Battery V".ToUpper()))
                                {
                                    model.InternalBatteryV = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToMoney();
                                }
                                if (register.name.ToUpper().Contains("Signal RSSI".ToUpper()))
                                {
                                    model.SignalRSSI = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                                if (register.name.ToUpper().Contains("SNR".ToUpper()))
                                {
                                    model.SNR = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                                }
                            }
                    }
                    else
                    {
                        DateTime startTime = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0);
                        DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                        var result = _client.GetApi2RegistersReadings(m2mDev.id, startTime, endTime, 3600, localDev.DeviceAPIIDValue);
                        model.MaxDemand = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._29.ToReading() : "";
                        model.ActiveEnergy = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._1.ToReading() : "";
                        model.CTRatio = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._70.ToReading() : "";
                        model.ContactorState = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._91.ToReading() : "";
                        model.Temp = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._102.ToReading() : "";
                        model.RemainingCredit = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._90.ToReading() : "";
                        model.WaterConsumption = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._80.ToReading() : "";
                        model.GasConsumption = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._140.ToReading() : "";
                        model.InternalBatteryV = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._100.ToReading() : "";
                        model.SignalRSSI = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._101.ToReading() : "";
                        model.SNR = result.readings.FirstOrDefault() != null ? result.readings.FirstOrDefault()._106.ToReading() : "";
                    }
                }
            }
            return PartialView("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_DeviceItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_DeviceItem_DeleteDevice/{gatewayID}/{deviceID}")]
        public async Task<IActionResult> H_Device_Administrator_DeviceOverview_DeviceItem_DeleteDevice(int gatewayID, int deviceID)
        {
            int logID = _loggingProvider.CreateLog(SecureAreaEnum.H_Device_Administrator_DeviceOverview, SecureAreaActionEnum.Delete, $"Device Delete: {gatewayID} - {deviceID}", _userManager.GetUserId(User));
            string result = _client.DeleteMeter(gatewayID.ToString(), deviceID.ToString());
            _loggingProvider.FinishLog(logID, $"Completed - {result}");
            _cache.Remove($"{MVCache.KEY_M2MGatewayDevices}{gatewayID}");
            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceOverview_DeviceItem_DeleteDevicesInGateway/{gatewayID}")]
        public async Task<IActionResult> H_Device_Administrator_DeviceOverview_DeviceItem_DeleteDevicesInGateway(int gatewayID)
        {
            int logID = _loggingProvider.CreateLog(SecureAreaEnum.H_Device_Administrator_DeviceOverview, SecureAreaActionEnum.Delete, $"Device Delete: {gatewayID} - All Devices", _userManager.GetUserId(User));
            var gatewaydevices = _client.GetGatewayDevices(gatewayID.ToString());
            StringBuilder sbResult = new StringBuilder();
            while (gatewaydevices.Count() > 0)
            {
                foreach (var device in gatewaydevices)
                {
                    Console.WriteLine($"DELETING {device.id}");
                    sbResult.AppendLine(_client.DeleteMeter(gatewayID.ToString(), device.id.ToString()));
                }
                gatewaydevices = _client.GetGatewayDevices(gatewayID.ToString());
            }

            _loggingProvider.FinishLog(logID, $"Completed - {sbResult.ToString()}");
            _cache.Remove($"{MVCache.KEY_M2MGatewayDevices}{gatewayID}");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_AddDeviceToGateway")]
        public async Task<IActionResult> H_Device_Administrator_AddDeviceToGateway()
        {
            return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceNameBulkUpdate")]
        public IActionResult H_Device_Administrator_DeviceNameBulkUpdate()
        {
            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_DeviceNameBulkUpdate.cshtml", new H_Device_Administrator_DeviceNameBulkUpdateModel());
        }

        [HttpPost]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_DeviceNameBulkUpdate")]
        public IActionResult H_Device_Administrator_DeviceNameBulkUpdate(H_Device_Administrator_DeviceNameBulkUpdateModel model)
        {
            H_Device_Administrator_DeviceNameBulkUpdateModel H_Device_Administrator_DeviceNameBulkUpdateModel1 = model;


            Dictionary<string, string> results = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(model.DeviceIDs))
            {
                var deviceIDs = model.DeviceIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (deviceIDs.Length == 0)
                {
                    H_Device_Administrator_DeviceNameBulkUpdateModel1.ErrorMessage = "No Device IDs supplied";
                    return View(H_Device_Administrator_DeviceNameBulkUpdateModel1);
                }


                #region Get results from m2m
                /// Get the results from m2m
                foreach (var deviceID in deviceIDs)
                {
                    if (string.IsNullOrEmpty(deviceID))
                        continue;

                    MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltage.Api.MyVoltage.MyVoltageApiClient();

                    MyVoltage.Api.MyVoltage.DeviceUpdate deviceUpdate = new MyVoltage.Api.MyVoltage.DeviceUpdate()
                    {
                        name = model.NewDeviceName
                    };

                    string url = $"devices/{deviceID}";

                    var result = _client.PUT<MyVoltage.Api.MyVoltage.DeviceUpdateResult, object>(url, 1, deviceUpdate);

                    results.Add(deviceID, $"{result.result} - Device {result.device.id} has been updated to {result.device.name}");
                }

                #endregion

                // Rebuild the results into pivot tables
            }
            else
            {
                H_Device_Administrator_DeviceNameBulkUpdateModel1.ErrorMessage = "No Device IDs supplied";
            }

            foreach (var result in results)
            {
                H_Device_Administrator_DeviceNameBulkUpdateModel1.ErrorMessage = H_Device_Administrator_DeviceNameBulkUpdateModel1.ErrorMessage + Environment.NewLine + result.Value;
            }

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_DeviceNameBulkUpdate.cshtml", H_Device_Administrator_DeviceNameBulkUpdateModel1);
        }


        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_Bulk433TestingUpload")]
        public IActionResult H_Device_Administrator_Bulk433TestingUpload()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            H_Device_Administrator_Bulk433TestingUploadModel model = new H_Device_Administrator_Bulk433TestingUploadModel()
            {
                DoSecondInput = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "1 Pulse input" },
                    new SelectListItem() { Value = "true", Text = "2 Pulse inputs" },
                },
                GatewayID = (from p in dbCache.Gateways
                             select new SelectListItem()
                             {
                                 Text = $"{p.GatewayID} - {p.Name}",
                                 Value = p.GatewayID.ToString(),
                             }).ToList(),
            };

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_Bulk433TestingUpload.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_Bulk433TestingUpload")]
        public IActionResult H_Device_Administrator_Bulk433TestingUpload(H_Device_Administrator_Bulk433TestingUploadModel H_Device_Administrator_Bulk433TestingUploadModel)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            H_Device_Administrator_Bulk433TestingUploadModel.DoSecondInput = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "1 Pulse input" },
                    new SelectListItem() { Value = "true", Text = "2 Pulse inputs" },
                };

            H_Device_Administrator_Bulk433TestingUploadModel.GatewayID = (from p in dbCache.Gateways
                                                                          select new SelectListItem()
                                                                          {
                                                                              Text = $"{p.GatewayID} - {p.Name}",
                                                                              Value = p.GatewayID.ToString(),
                                                                              Selected = p.GatewayID.ToString() == Request.Form["GatewayID"]
                                                                          }).ToList();

            H_Device_Administrator_Bulk433TestingUploadModel H_Device_Administrator_Bulk433TestingUploadModel1 = H_Device_Administrator_Bulk433TestingUploadModel;


            H_Device_Administrator_Bulk433TestingUploadModel1.ErrorMessage = "Your meters has been submitted. Please do not resubmit. The meters will be uploaded and may take up to 30min to complete.";

            var serialNumbers = H_Device_Administrator_Bulk433TestingUploadModel.SerialNos.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            bool DoSecondInput = Convert.ToBoolean(Request.Form["DoSecondInput"]);
            int GatewayID = Convert.ToInt32(Request.Form["GatewayID"]);

            // Run entire thing in background proceess and return immedietly 
            System.Threading.Thread thread = new System.Threading.Thread(() => H_Device_Administrator_Bulk433TestingUploadThread(serialNumbers.ToList(), DoSecondInput, GatewayID, _userManager.GetUserId(User)));
            thread.Start();
            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_Bulk433TestingUpload.cshtml", H_Device_Administrator_Bulk433TestingUploadModel1);
        }

        private void H_Device_Administrator_Bulk433TestingUploadThread(List<string> serialNumbers, bool doSecondPulseInput, int gwID, string userID)
        {
            List<string> results = new List<string>();
            string gatewayID = gwID.ToString();

            if (serialNumbers.Count == 0)
            {
                Console.WriteLine("No serials");
                return;
            }

            string request = $"Add devices to gateway:{Environment.NewLine}Gateway: {gwID}{Environment.NewLine}Serials: {string.Join(Environment.NewLine, serialNumbers)}";

            int logID = _loggingProvider.CreateLog(SecureAreaEnum.H_Device_Administrator_Bulk433TestingUpload, SecureAreaActionEnum.Add, request, userID);
            StringBuilder sbResult = new StringBuilder();

            #region Add devices to m2m
            /// Get the results from m2m
            foreach (var serial in serialNumbers)
            {
                if (string.IsNullOrEmpty(serial))
                    continue;

                string serial1 = $"Stock-{serial}-1";
                string serial2 = $"Delete-{serial}-2";

                //MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                {
                    devices = new CreateOrUpdateM2MDevice.Device[]
                    {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial1,
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 1,
                                    process_interval = 900
                                }
                            }
                    }
                };

                string url = $"gateways/{gatewayID}/devices";

                var result = _client.Post<object, object>(url, 1, deviceUpdate);

                string strResult = $"Device {serial1} has been added to test gateway {gatewayID} - {(result != null ? result.ToString() : "")}";

                sbResult.AppendLine(strResult);

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDevice deviceUpdate2 = new CreateOrUpdateM2MDevice()
                    {
                        devices = new CreateOrUpdateM2MDevice.Device[]
                        {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial2,
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 2,
                                    process_interval = 900
                                }
                            }
                        }
                    };

                    url = $"gateways/{gatewayID}/devices";

                    result = _client.Post<object, object>(url, 1, deviceUpdate2);

                    strResult = $"Device {serial2} has been added to test gateway {gatewayID} - {(result != null ? result.ToString() : "")}";

                    sbResult.AppendLine(strResult);

                    results.Add(strResult);


                }

            }

            #endregion

            sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
            System.Threading.Thread.Sleep(30000);

            #region double check all devices has been added

            sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Double checking add");

            var gatewaydevices = _client.GetGatewayDevices(gatewayID);
            bool lookAgain = false;

            foreach (var serial in serialNumbers)
            {
                string serial1 = $"Stock-{serial}-1";
                string serial2 = $"Delete-{serial}-2";

                if (gatewaydevices.Where(p => p.serial == serial1).Count() == 0)
                {
                    // Add it again
                    lookAgain = true;
                    sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial1} Adding it again");
                    bool addAgain = true;

                    while (addAgain)
                    {
                        CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                        {
                            devices = new CreateOrUpdateM2MDevice.Device[]
                            {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial1,
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 1,
                                    process_interval = 900
                                }
                            }
                            }
                        };
                        string url = $"gateways/{gatewayID}/devices";

                        var result = _client.Post<object, object>(url, 1, deviceUpdate);

                        sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
                        System.Threading.Thread.Sleep(30000);

                        gatewaydevices = _client.GetGatewayDevices(gatewayID);
                        if (gatewaydevices.Where(p => p.serial == serial1).Count() > 0)
                        {
                            sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial1} Adding it again");
                            addAgain = false;
                        }
                    }


                }
                if (doSecondPulseInput)
                    if (gatewaydevices.Where(p => p.serial == serial2).Count() == 0)
                    {
                        // Add it again
                        lookAgain = true;
                        sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial2} Adding it again");
                        bool addAgain = true;

                        while (addAgain)
                        {
                            CreateOrUpdateM2MDevice deviceUpdate = new CreateOrUpdateM2MDevice()
                            {
                                devices = new CreateOrUpdateM2MDevice.Device[]
                                {
                            new CreateOrUpdateM2MDevice.Device()
                            {
                                id = 0,
                                serial = serial2,
                                type_id = 2,
                                mapping = new CreateOrUpdateM2MDevice.Mapping()
                                {
                                    port = 4,
                                    protocol_id = 16,
                                    remote_address = $"0x{serial}",
                                    remote_index = 2,
                                    process_interval = 900
                                }
                            }
                                }
                            };
                            string url = $"gateways/{gatewayID}/devices";

                            var result = _client.Post<object, object>(url, 1, deviceUpdate);

                            sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Sleeping 30sec");
                            System.Threading.Thread.Sleep(30000);

                            gatewaydevices = _client.GetGatewayDevices(gatewayID);
                            if (gatewaydevices.Where(p => p.serial == serial2).Count() > 0)
                            {
                                sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial2} Adding it again");
                                addAgain = false;
                            }
                        }


                    }
            }

            while (lookAgain)
            {
                gatewaydevices = _client.GetGatewayDevices(gatewayID);
                lookAgain = false;

                foreach (var serial in serialNumbers)
                {
                    string serial1 = $"Stock-{serial}-1";
                    string serial2 = $"Delete-{serial}-2";
                    if (gatewaydevices.Where(p => p.serial == serial1).Count() == 0)
                    {
                        lookAgain = true;
                        sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cant find {serial1}");
                    }
                }
            }

            #endregion

            #region Add devices configuration to m2m

            sbResult.AppendLine("Config starting");
            /// Get the results from m2m
            /// 
            #region -1

            foreach (var serial in serialNumbers)
            {
                string serial1 = $"Stock-{serial}-1";
                string serial2 = $"Delete-{serial}-2";

                var device = gatewaydevices.Where(p => p.serial == serial1).SingleOrDefault();

                if (device == null)
                {
                    sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cannot find {serial1} to config");
                    continue;
                }
                MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDeviceConfig H_Device_Administrator_Bulk433TestingUploadConfig = new CreateOrUpdateM2MDeviceConfig()
                {
                    action = new CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = "1"
                    }
                };

                string url = $"devices/{device.id}/configurations/6";

                var result = _client.Post<object, object>(url, 1, H_Device_Administrator_Bulk433TestingUploadConfig);

                string strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDeviceConfig H_Device_Administrator_Bulk433TestingUploadConfig2 = new CreateOrUpdateM2MDeviceConfig()
                    {
                        action = new CreateOrUpdateM2MDeviceConfig.Action()
                        {
                            id = 1,
                            value = "1"
                        }
                    };

                    url = $"devices/{device.id}/configurations/6";

                    result = _client.Post<object, object>(url, 1, H_Device_Administrator_Bulk433TestingUploadConfig2);

                    strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                    sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                    results.Add(strResult);

                }
            }

            #endregion

            #region -2

            foreach (var serial in serialNumbers)
            {
                string serial1 = $"Stock-{serial}-1";
                string serial2 = $"Delete-{serial}-2";

                var device = gatewaydevices.Where(p => p.serial == serial2).SingleOrDefault();

                if (device == null)
                {
                    sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - Cannot find {serial2} to config");
                    continue;
                }
                MyVoltage.Api.MyVoltage.MyVoltageApiClient _client = new MyVoltageApiClient();

                CreateOrUpdateM2MDeviceConfig H_Device_Administrator_Bulk433TestingUploadConfig = new CreateOrUpdateM2MDeviceConfig()
                {
                    action = new CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = "1"
                    }
                };

                string url = $"devices/{device.id}/configurations/6";

                var result = _client.Post<object, object>(url, 1, H_Device_Administrator_Bulk433TestingUploadConfig);

                string strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                results.Add(strResult);

                if (doSecondPulseInput)
                {
                    CreateOrUpdateM2MDeviceConfig H_Device_Administrator_Bulk433TestingUploadConfig2 = new CreateOrUpdateM2MDeviceConfig()
                    {
                        action = new CreateOrUpdateM2MDeviceConfig.Action()
                        {
                            id = 1,
                            value = "1"
                        }
                    };

                    url = $"devices/{device.id}/configurations/6";

                    result = _client.Post<object, object>(url, 1, H_Device_Administrator_Bulk433TestingUploadConfig2);

                    strResult = $"Device {device.serial} has been configured - {(result != null ? result.ToString() : "")}";

                    sbResult.AppendLine($"{DateTime.Now:HH:mm:ss} - {strResult}");

                    results.Add(strResult);

                }
            }

            #endregion

            #endregion

            // Rebuild the results into pivot tables

            _loggingProvider.FinishLog(logID, sbResult.ToString());
        }

        public class H_Device_Administrator_AutoDeviceDiscoveryConfig
        {
            public List<string> ToSendTo { get; set; }
            public List<int> GatewayIDs { get; set; }
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_AutoDeviceDiscovery")]
        public IActionResult H_Device_Administrator_AutoDeviceDiscovery()
        {
            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_AutoDeviceDiscovery.cshtml", new H_Device_Administrator_AutoDeviceDiscoveryModel());
        }

        [HttpPost]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_AutoDeviceDiscovery")]
        public IActionResult H_Device_Administrator_AutoDeviceDiscovery(H_Device_Administrator_AutoDeviceDiscoveryModel H_Device_Administrator_AutoDeviceDiscoveryModel)
        {
            H_Device_Administrator_AutoDeviceDiscoveryModel H_Device_Administrator_AutoDeviceDiscoveryModel1 = H_Device_Administrator_AutoDeviceDiscoveryModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            if (string.IsNullOrEmpty(H_Device_Administrator_AutoDeviceDiscoveryModel.GatewayIDs) || string.IsNullOrEmpty(H_Device_Administrator_AutoDeviceDiscoveryModel.ToSendTo))
            {
                H_Device_Administrator_AutoDeviceDiscoveryModel1.ErrorMessage = "Gateway IDs and Emails may not be empty";

                return View(H_Device_Administrator_AutoDeviceDiscoveryModel1);
            }

            var gatewayIDs = H_Device_Administrator_AutoDeviceDiscoveryModel.GatewayIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var toSendTo = H_Device_Administrator_AutoDeviceDiscoveryModel.ToSendTo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder result = new StringBuilder();

            List<int> gatewayIDints = new List<int>();

            foreach (var gw in gatewayIDs)
            {
                if (string.IsNullOrEmpty(gw))
                    continue;
                try { gatewayIDints.Add(Convert.ToInt32(gw)); }
                catch
                {
                    result.AppendLine($"Invalid gateway id: {gw}");
                }
            }

            if (!string.IsNullOrEmpty(result.ToString()))
            {
                H_Device_Administrator_AutoDeviceDiscoveryModel1.ErrorMessage = result.ToString();

                return View(H_Device_Administrator_AutoDeviceDiscoveryModel1);
            }

            if (gatewayIDints.Count < 2)
            {
                result.Append("Gateway IDs must be more than 1");
                H_Device_Administrator_AutoDeviceDiscoveryModel1.ErrorMessage = result.ToString();

                return View(H_Device_Administrator_AutoDeviceDiscoveryModel1);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                Data.DetectAndMove H_Device_Administrator_AutoDeviceDiscovery = new DetectAndMove()
                {
                    GatewayIDs = H_Device_Administrator_AutoDeviceDiscoveryModel.GatewayIDs,
                    ToSendTo = H_Device_Administrator_AutoDeviceDiscoveryModel.ToSendTo
                };
                db.DetectAndMoves.Add(H_Device_Administrator_AutoDeviceDiscovery);
                db.SaveChanges();
            }


            result.AppendLine($"Request received for:{H_Device_Administrator_AutoDeviceDiscoveryModel.GatewayIDs} - {H_Device_Administrator_AutoDeviceDiscoveryModel.ToSendTo}");


            H_Device_Administrator_AutoDeviceDiscoveryModel1.ErrorMessage = result.ToString();

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_AutoDeviceDiscovery.cshtml", H_Device_Administrator_AutoDeviceDiscoveryModel1);
        }

        public class H_Device_Administrator_WirelessSignalOptimizerConfig
        {
            public List<string> ToSendTo { get; set; }
            public List<int> GatewayIDs { get; set; }
            public int SleepDurationMin { get; set; }
            public int LoopCount { get; set; }
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_WirelessSignalOptimizer")]
        public IActionResult H_Device_Administrator_WirelessSignalOptimizer()
        {
            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_WirelessSignalOptimizer.cshtml", new H_Device_Administrator_WirelessSignalOptimizerModel());
        }

        [HttpPost]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_WirelessSignalOptimizer")]
        public IActionResult H_Device_Administrator_WirelessSignalOptimizer(H_Device_Administrator_WirelessSignalOptimizerModel H_Device_Administrator_WirelessSignalOptimizerModel)
        {
            H_Device_Administrator_WirelessSignalOptimizerModel H_Device_Administrator_WirelessSignalOptimizerModel1 = H_Device_Administrator_WirelessSignalOptimizerModel;
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy/MM/dd";

            if (string.IsNullOrEmpty(H_Device_Administrator_WirelessSignalOptimizerModel.GatewayIDs) || string.IsNullOrEmpty(H_Device_Administrator_WirelessSignalOptimizerModel.ToSendTo))
            {
                H_Device_Administrator_WirelessSignalOptimizerModel1.ErrorMessage = "Gateway IDs and Emails may not be empty";

                return View(H_Device_Administrator_WirelessSignalOptimizerModel1);
            }

            var gatewayIDs = H_Device_Administrator_WirelessSignalOptimizerModel.GatewayIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var toSendTo = H_Device_Administrator_WirelessSignalOptimizerModel.ToSendTo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder result = new StringBuilder();

            List<int> gatewayIDints = new List<int>();

            foreach (var gw in gatewayIDs)
            {
                if (string.IsNullOrEmpty(gw))
                    continue;
                try { gatewayIDints.Add(Convert.ToInt32(gw)); }
                catch
                {
                    result.AppendLine($"Invalid gateway id: {gw}");
                }
            }

            if (!string.IsNullOrEmpty(result.ToString()))
            {
                H_Device_Administrator_WirelessSignalOptimizerModel1.ErrorMessage = result.ToString();

                return View(H_Device_Administrator_WirelessSignalOptimizerModel1);
            }

            if (gatewayIDints.Count < 2)
            {
                result.Append("Gateway IDs must be more than 1");
                H_Device_Administrator_WirelessSignalOptimizerModel1.ErrorMessage = result.ToString();

                return View(H_Device_Administrator_WirelessSignalOptimizerModel1);
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                Data.SignalOptimizer H_Device_Administrator_WirelessSignalOptimizer = new SignalOptimizer()
                {
                    GatewayIDs = H_Device_Administrator_WirelessSignalOptimizerModel.GatewayIDs,
                    ToSendTo = H_Device_Administrator_WirelessSignalOptimizerModel.ToSendTo,
                    DateRequested = DateTime.Now,
                    LoopCount = H_Device_Administrator_WirelessSignalOptimizerModel.LoopCount,
                    SleepDurationMin = H_Device_Administrator_WirelessSignalOptimizerModel.SleepDurationMin,
                    DontMoveAboveThisStrength = H_Device_Administrator_WirelessSignalOptimizerModel.DontMoveAboveThisStrength
                };
                db.SignalOptimizers.Add(H_Device_Administrator_WirelessSignalOptimizer);
                db.SaveChanges();
            }


            result.AppendLine($"Request received for:{H_Device_Administrator_WirelessSignalOptimizerModel.GatewayIDs} - {H_Device_Administrator_WirelessSignalOptimizerModel.ToSendTo}");


            H_Device_Administrator_WirelessSignalOptimizerModel1.ErrorMessage = result.ToString();

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_WirelessSignalOptimizer.cshtml", H_Device_Administrator_WirelessSignalOptimizerModel1);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Summary")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel model = new H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel()
            {
                H_Device_Administrator_ActiveEnergyAnomalies_SummaryItems = new List<H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            };

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            var db = new MyVoltageDbContext(_options);
            var companies = db.Companies.ToList();

            var uniqueCompanyIDsWithEntries = (from p in db.MeterActiveEnergyAnomaliesRuns
                                               where p.FromDate.Date >= model.FromDate.Date
                                               && p.ToDate.Date <= model.ToDate.Date
                                               select p.CompanyID).Distinct().ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var company = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (company == null)
                    continue;

                if (!uniqueCompanyIDsWithEntries.Contains(uC.CompanyID))
                    continue;

                var meterActiveEnergyAnomalies = (from p in db.MeterActiveEnergyAnomalies
                                                  where p.CompanyID == uC.CompanyID
                                                  && p.TimeLogged.Date >= model.FromDate.Date
                                                  && p.TimeLogged.Date <= model.ToDate.Date
                                                  select p).ToList();

                var meterActiveEnergyAnomaliesRuns = (from p in db.MeterActiveEnergyAnomaliesRuns
                                                      where p.CompanyID == uC.CompanyID
                                                      select p).ToList();

                H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem item = new H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem()
                {
                    BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                    Name = company.Name,
                    BalanceMustBeAbove = company.BalanceMustBeAbove,
                    Batch = company.Batch,
                    CompanyID = company.CompanyID,
                    ConvFactor = company.ConvFactor,
                    Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG,
                    Division_VBAK_SPART = company.Division_VBAK_SPART,
                    ExistsInSkybill = company.ExistsInSkybill,
                    IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                    IsFlagStatusActive = company.IsFlagStatusActive,
                    ItemID = company.ItemID,
                    MasterServiceKey = company.MasterServiceKey,
                    NetcashBalance = company.NetcashBalance,
                    NetcashBalanceDate = company.NetcashBalanceDate,
                    NetcashBankAccountNo = company.NetcashBankAccountNo,
                    NetcashBankAccountType = company.NetcashBankAccountType,
                    NetcashBankBranchCode = company.NetcashBankBranchCode,
                    NetcashBankName = company.NetcashBankName,
                    PartnerID = company.PartnerID,
                    PlantNo = company.PlantNo,
                    Registrable = company.Registrable,
                    Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01,
                    Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART,
                    Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR,
                    Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG,
                    ServiceKey = company.ServiceKey,
                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                    StockRefNo = company.StockRefNo,
                    ActionID = company.ActionID,
                    H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItems = new List<H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem.H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem>(),
                    ResponsibleUserID = company.ResponsibleUserID,
                    ResponsibleUserTimestamp = company.ResponsibleUserTimestamp,
                    TargetDate = company.TargetDate,
                };

                DateTime current = model.FromDate;

                while (current <= model.ToDate)
                {
                    DateTime startOfMonth = current.Date;
                    DateTime endOfMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)).Date;

                    var errorCount = (from p in meterActiveEnergyAnomalies
                                      where p.TimeLogged.Date >= startOfMonth
                                      && p.TimeLogged.Date <= endOfMonth
                                      && p.IsTheProblemRow.HasValue && p.IsTheProblemRow.Value
                                      select p).Count();

                    var anomolyRun = (from p in meterActiveEnergyAnomaliesRuns
                                      where p.FromDate == startOfMonth
                                      && p.ToDate == endOfMonth
                                      orderby p.DateCreated descending
                                      select p).FirstOrDefault();

                    H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem.H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem = new H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem.H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem()
                    {
                        ErrorCount = errorCount,
                        Month = current,
                    };

                    if (anomolyRun != null)
                    {
                        if (anomolyRun.DateEnded.HasValue)
                        {
                            h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem.IsCompleted = true;
                            h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem.DateCompleted = anomolyRun.DateEnded;
                        }
                        else
                        {
                            h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem.IsCompleted = false;
                            h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem.DateCompleted = anomolyRun.DateStarted;
                        }
                    }

                    item.H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItems.Add(h_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem);

                    current = current.AddMonths(1);
                }

                model.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItems.Add(item);
            }
            model.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItems = model.H_Device_Administrator_ActiveEnergyAnomalies_SummaryItems.OrderBy(p => p.Name).ToList();

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Details/{year?}/{month?}")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Details(string year, string month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Summary");

            var db = new MyVoltageDbContext(_options);

            H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel model = new H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel()
            {
                FromDate = null,
                H_Device_Administrator_ActiveEnergyAnomalies_DetailsItems = new List<H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel.H_Device_Administrator_ActiveEnergyAnomalies_DetailsItem>(),
                ToDate = null,
                ShowOnlyErrors = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = "Yes (Show Only Error Rows)", Selected = string.IsNullOrEmpty(Request.Query["ShowOnlyErrors"]) || Convert.ToBoolean(Request.Query["ShowOnlyErrors"]) },
                    new SelectListItem() { Value = false.ToString(), Text = "No (Show All Rows)", Selected = !string.IsNullOrEmpty(Request.Query["ShowOnlyErrors"]) && !Convert.ToBoolean(Request.Query["ShowOnlyErrors"]) },
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            if (!string.IsNullOrEmpty(year) && !string.IsNullOrEmpty(month))
            {
                model.FromDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), 1);
                model.ToDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month))).AddDays(1);
            }


            List<Data.MeterActiveEnergyAnomaly> meterActiveEnergyAnomalies = new List<MeterActiveEnergyAnomaly>();

            if (model.FromDate.HasValue && model.ToDate.HasValue)
            {
                meterActiveEnergyAnomalies = (from p in db.MeterActiveEnergyAnomalies
                                              where p.CompanyID == _operationalProvider.CompanyID
                                              && p.TimeLogged >= model.FromDate.Value
                                              && p.TimeLogged <= model.ToDate.Value
                                              && p.IsTheProblemRow.HasValue && p.IsTheProblemRow.Value
                                              orderby p.Serial, p.TimeLogged descending
                                              select p).ToList();

                if (!string.IsNullOrEmpty(Request.Query["ShowOnlyErrors"]) && !Convert.ToBoolean(Request.Query["ShowOnlyErrors"]))
                    meterActiveEnergyAnomalies = (from p in db.MeterActiveEnergyAnomalies
                                                  where p.CompanyID == _operationalProvider.CompanyID
                                                  && p.TimeLogged >= model.FromDate.Value
                                                  && p.TimeLogged <= model.ToDate.Value
                                                  orderby p.Serial, p.TimeLogged descending
                                                  select p).ToList();

            }
            else
            {
                meterActiveEnergyAnomalies = (from p in db.MeterActiveEnergyAnomalies
                                              where p.CompanyID == _operationalProvider.CompanyID
                                              && p.IsTheProblemRow.HasValue && p.IsTheProblemRow.Value
                                              orderby p.Serial, p.TimeLogged descending
                                              select p).Take(20).ToList();

                if (!string.IsNullOrEmpty(Request.Query["ShowOnlyErrors"]) && !Convert.ToBoolean(Request.Query["ShowOnlyErrors"]))
                    meterActiveEnergyAnomalies = (from p in db.MeterActiveEnergyAnomalies
                                                  where p.CompanyID == _operationalProvider.CompanyID
                                                  orderby p.Serial, p.TimeLogged descending
                                                  select p).Take(20).ToList();
            }

            foreach (var m in meterActiveEnergyAnomalies)
            {
                H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel.H_Device_Administrator_ActiveEnergyAnomalies_DetailsItem item = new H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel.H_Device_Administrator_ActiveEnergyAnomalies_DetailsItem()
                {
                    ActiveEnergyReading = m.ActiveEnergyReading,
                    CompanyID = m.CompanyID,
                    Diff = m.Diff,
                    ErrorPerc = m.ErrorPerc,
                    ID = m.ID,
                    MeterID = m.MeterID,
                    Name = m.Name,
                    Serial = m.Serial,
                    TimeLogged = m.TimeLogged,
                    IsTheProblemRow = m.IsTheProblemRow,
                    ErrorID = m.ErrorID,
                };

                model.H_Device_Administrator_ActiveEnergyAnomalies_DetailsItems.Add(item);
            }

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request/{companyID}/{year}/{month}")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Request(int companyID, int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            DateTime fromDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), 1);
            DateTime toDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month)));

            var entriesToRemove = (from p in db.MeterActiveEnergyAnomalies
                                   where p.CompanyID == companyID
                                   && p.TimeLogged.Date >= fromDate
                                   && p.TimeLogged.Date <= toDate
                                   select p).ToList();

            if (entriesToRemove.Count > 0)
            {
                db.RemoveRange(entriesToRemove);
                db.SaveChanges();
            }

            Data.MeterActiveEnergyAnomaliesRun meterActiveEnergyAnomaliesRun = new MeterActiveEnergyAnomaliesRun()
            {
                CompanyID = companyID,
                CreatedBy = _userManager.GetUserId(User),
                DateCreated = DateTime.Now,
                DateEnded = null,
                DateStarted = null,
                FromDate = fromDate,
                Progress = 0,
                ToDate = toDate,
            };

            db.Add(meterActiveEnergyAnomaliesRun);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Summary");
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request_AllCompanies/{year}/{month}")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Request_AllCompanies(int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            DateTime fromDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), 1);
            DateTime toDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month)));

            var entriesToRemove = (from p in db.MeterActiveEnergyAnomalies
                                   where p.TimeLogged.Date >= fromDate
                                   && p.TimeLogged.Date <= toDate
                                   select p).ToList();

            if (entriesToRemove.Count > 0)
            {
                db.RemoveRange(entriesToRemove);
                db.SaveChanges();
            }

            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).Select(p => p.CompanyID).ToList();

            foreach (var companyID in companies)
            {
                Data.MeterActiveEnergyAnomaliesRun meterActiveEnergyAnomaliesRun = new MeterActiveEnergyAnomaliesRun()
                {
                    CompanyID = companyID,
                    CreatedBy = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = fromDate,
                    Progress = 0,
                    ToDate = toDate,
                };

                db.Add(meterActiveEnergyAnomaliesRun);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Summary");
        }

        [HttpGet]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Request()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request}/{(int)SecureAreaActionEnum.View}");

            #endregion

            H_Device_Administrator_ActiveEnergyAnomalies_RequestModel model = new H_Device_Administrator_ActiveEnergyAnomalies_RequestModel()
            {
                CompanyID = new List<SelectListItem>(),
                H_Device_Administrator_ActiveEnergyAnomalies_RequestItems = new List<H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem>(),
                FromDate = null,
                IsSuccessful = false,
            };

            var db = new MyVoltageDbContext(_options);
            var meterActiveEnergyAnomaliesRuns = db.MeterActiveEnergyAnomaliesRuns.OrderByDescending(p => p.DateCreated).Take(500).ToList();
            var companies = db.Companies.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = company.CompanyID.ToString(), Text = company.Name });
            }
            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();

            foreach (var meterActiveEnergyAnomaliesRun in meterActiveEnergyAnomaliesRuns)
            {
                H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem item = new H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem()
                {
                    DateCreated = meterActiveEnergyAnomaliesRun.DateCreated,
                    CompanyID = meterActiveEnergyAnomaliesRun.CompanyID,
                    CompanyName = companies.Where(p => p.CompanyID == meterActiveEnergyAnomaliesRun.CompanyID).SingleOrDefault().Name,
                    CreatedBy = meterActiveEnergyAnomaliesRun.CreatedBy,
                    CreatedByUsername = "",
                    DateEnded = meterActiveEnergyAnomaliesRun.DateEnded,
                    DateStarted = meterActiveEnergyAnomaliesRun.DateStarted,
                    FromDate = meterActiveEnergyAnomaliesRun.FromDate,
                    ID = meterActiveEnergyAnomaliesRun.ID,
                    Progress = meterActiveEnergyAnomaliesRun.Progress,
                    ToDate = meterActiveEnergyAnomaliesRun.ToDate,
                };

                var opProf = opProfs.Where(p => p.UserID == meterActiveEnergyAnomaliesRun.CreatedBy).SingleOrDefault();
                if (opProf != null)
                    item.CreatedByUsername = $"{opProf.FirstName} {opProf.LastName}";

                model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems.Add(item);
            }

            model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems = model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request")]
        public IActionResult H_Device_Administrator_ActiveEnergyAnomalies_Request(H_Device_Administrator_ActiveEnergyAnomalies_RequestModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request}/{(int)SecureAreaActionEnum.View}");

            #endregion

            model.CompanyID = new List<SelectListItem>();
            model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems = new List<H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem>();

            var db = new MyVoltageDbContext(_options);
            var meterActiveEnergyAnomaliesRuns = db.MeterActiveEnergyAnomaliesRuns.ToList();
            var companies = db.Companies.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                model.CompanyID.Add(new SelectListItem() { Value = company.CompanyID.ToString(), Text = company.Name, Selected = company.CompanyID.ToString() == Request.Form["CompanyID"] });
            }
            model.CompanyID = model.CompanyID.OrderBy(p => p.Text).ToList();

            foreach (var meterActiveEnergyAnomaliesRun in meterActiveEnergyAnomaliesRuns)
            {
                H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem item = new H_Device_Administrator_ActiveEnergyAnomalies_RequestModel.H_Device_Administrator_ActiveEnergyAnomalies_RequestItem()
                {
                    DateCreated = meterActiveEnergyAnomaliesRun.DateCreated,
                    CompanyID = meterActiveEnergyAnomaliesRun.CompanyID,
                    CompanyName = companies.Where(p => p.CompanyID == meterActiveEnergyAnomaliesRun.CompanyID).SingleOrDefault().Name,
                    CreatedBy = meterActiveEnergyAnomaliesRun.CreatedBy,
                    CreatedByUsername = "",
                    DateEnded = meterActiveEnergyAnomaliesRun.DateEnded,
                    DateStarted = meterActiveEnergyAnomaliesRun.DateStarted,
                    FromDate = meterActiveEnergyAnomaliesRun.FromDate,
                    ID = meterActiveEnergyAnomaliesRun.ID,
                    Progress = meterActiveEnergyAnomaliesRun.Progress,
                    ToDate = meterActiveEnergyAnomaliesRun.ToDate,
                };

                var opProf = opProfs.Where(p => p.UserID == meterActiveEnergyAnomaliesRun.CreatedBy).SingleOrDefault();
                if (opProf != null)
                    item.CreatedByUsername = $"{opProf.FirstName} {opProf.LastName}";

                model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems.Add(item);
            }

            model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems = model.H_Device_Administrator_ActiveEnergyAnomalies_RequestItems.OrderByDescending(p => p.DateCreated).ToList();

            if (ModelState.IsValid)
            {
                model.FromDate = new DateTime(model.FromDate.Value.Year, model.FromDate.Value.Month, 1);
                DateTime toDate = new DateTime(model.FromDate.Value.Year, model.FromDate.Value.Month, DateTime.DaysInMonth(model.FromDate.Value.Year, model.FromDate.Value.Month));

                var existingReport = (from p in db.MeterActiveEnergyAnomaliesRuns
                                      where p.CompanyID == Convert.ToInt32(Request.Form["CompanyID"])
                                      && p.FromDate == model.FromDate.Value
                                      && p.ToDate == toDate
                                      && !p.DateEnded.HasValue
                                      select p).FirstOrDefault();

                if (existingReport == null)
                {
                    var entriesToRemove = (from p in db.MeterActiveEnergyAnomalies
                                           where p.CompanyID == Convert.ToInt32(Request.Form["CompanyID"])
                                           && p.TimeLogged.Date >= model.FromDate.Value
                                           && p.TimeLogged.Date <= toDate
                                           select p).ToList();

                    if (entriesToRemove.Count > 0)
                    {
                        db.RemoveRange(entriesToRemove);
                        db.SaveChanges();
                    }

                    Data.MeterActiveEnergyAnomaliesRun meterActiveEnergyAnomaliesRun = new MeterActiveEnergyAnomaliesRun()
                    {
                        CompanyID = Convert.ToInt32(Request.Form["CompanyID"]),
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        DateEnded = null,
                        DateStarted = null,
                        FromDate = model.FromDate.Value,
                        Progress = 0,
                        ToDate = toDate,
                    };

                    db.Add(meterActiveEnergyAnomaliesRun);
                    db.SaveChanges();

                    model.IsSuccessful = true;
                }
                else
                    ModelState.AddModelError("CompanyID", "Duplicate report detected. Please wait for previous report to finish.");
            }

            return View("~/Views/Operational/H_Device_Administrator/H_Device_Administrator_ActiveEnergyAnomalies_Request.cshtml", model);
        }

    }
}
