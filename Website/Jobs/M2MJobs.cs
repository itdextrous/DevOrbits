using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Data;
using MyVoltage.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.M2MJobs
{
    //public class M2MJob_GatewaysAndDevicesSync
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;

    //    public M2MJob_GatewaysAndDevicesSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //    }

    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
    //        {
    //            int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_GatewaysAndDevicesSync;
    //            DateTime startDate = DateTime.Now;

    //            Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
    //            {
    //                DateStarted = startDate,
    //                ReportURL = "",
    //                SecureAreaID = secureAreaID,
    //            };
    //            db.Add(systemGeneratedReport);
    //            db.SaveChanges();

    //            try
    //            {
    //                StringBuilder sbEmail = new StringBuilder();
    //                List<string> errors = new List<string>();
    //                int notificationMetersCreated = 0;

    //                var m2mClient = new Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //                var m2mGateways = m2mClient.GetGateways();
    //                Serilog.Log.Information($"m2mClient.GetGateways() - {m2mGateways.Length}");

    //                var localGateways = db.Gateways.ToList();
    //                Serilog.Log.Information($"db.Gateways.ToList() - {localGateways.Count}");

    //                var localDevices = db.Devices.ToList();
    //                Serilog.Log.Information($"db.Devices.ToList() - {localDevices.Count}");

    //                var skybillCustomers = db.SkybillCustomers.ToList();
    //                Serilog.Log.Information($"db.SkybillCustomers.ToList() - {skybillCustomers.Count}");

    //                var notificationCustomerMeters = db.NotificationCustomerMeters.ToList();
    //                Serilog.Log.Information($"db.NotificationCustomerMeters.ToList() - {notificationCustomerMeters.Count}");

    //                int nCount = 0;


    //                DateTime gateways_startTime = DateTime.Now;

    //                Serilog.Log.Information($"Update Gateways started");
    //                #region Update Gateways

    //                int gatewaysCount = m2mGateways.Length;
    //                int currentGWCount = 0;

    //                foreach (var m2mGW in m2mGateways)
    //                {
    //                    currentGWCount++;
    //                    Console.WriteLine($"GATEWAYS - [{currentGWCount}/{gatewaysCount}]");

    //                    if (m2mGW.network == null)
    //                        continue;

    //                    var localGW = localGateways.Where(p => p.GatewayID == m2mGW.id).FirstOrDefault();

    //                    if (localGW != null)
    //                    {
    //                        #region Update

    //                        localGW.DateLastSynced = DateTime.Now;
    //                        localGW.Name = m2mGW.name;
    //                        localGW.IsOnline = m2mGW.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true;
    //                        localGW.HardwareType = m2mGW.deviceTypeName;
    //                        localGW.Manufacturer = m2mGW.deviceTypeName;
    //                        localGW.HardwareID = m2mGW.type.id;
    //                        localGW.Since = Convert.ToDateTime(m2mGW.since);
    //                        localGW.SimCardNumber = m2mGW.network.msisdn;
    //                        localGW.FirmwareVersion = Convert.ToInt32(m2mGW.network.version);
    //                        localGW.Network = m2mGW.network.network;
    //                        localGW.GSMSerial = m2mGW.serial;
    //                        localGW.GISLocation = ""; /// TODO: ??? (Original website hardcoded to always return nothing)
    //                        localGW.Signal = Convert.ToInt32(m2mGW.network.csq);
    //                        localGW.OnlineMeters = m2mGW.onlineDevices;
    //                        localGW.OfflineMeters = m2mGW.offlineDevices;

    //                        if (localGW.ActiveStatusID == (int)Data.ActiveStatus.Deleted
    //                            || localGW.ActiveStatusID == (int)Data.ActiveStatus.Inventory)
    //                        {
    //                            // Reset status back to active, next check will put it back in Deleted/Inventory if needed
    //                            localGW.ActiveStatusID = (int)Data.ActiveStatus.Active;
    //                        }

    //                        if (m2mGW.name.ToUpper().Contains("DELETED"))
    //                        {
    //                            localGW.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                        }
    //                        else if (m2mGW.name.ToUpper().Contains("STOCK"))
    //                        {
    //                            localGW.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                        }
    //                        else if (m2mGW.name.ToUpper().Contains("FAULTY"))
    //                        {
    //                            localGW.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                        }

    //                        //db.Gateways.Update(localGW, true);

    //                        db.Update(localGW);

    //                        #endregion
    //                    }

    //                    else
    //                    {
    //                        #region New

    //                        int activeStatusID = 1;
    //                        if (m2mGW.name.ToUpper().Contains("DELETED"))
    //                        {
    //                            activeStatusID = (int)Data.ActiveStatus.Deleted;
    //                        }
    //                        else if (m2mGW.name.ToUpper().Contains("STOCK"))
    //                        {
    //                            activeStatusID = (int)Data.ActiveStatus.Inventory;
    //                        }
    //                        else if (m2mGW.name.ToUpper().Contains("FAULTY"))
    //                        {
    //                            activeStatusID = (int)Data.ActiveStatus.Faulty;
    //                        }

    //                        var deviceWithCompany = (from p in localDevices
    //                                                 where p.GatewayID == m2mGW.id
    //                                                 && p.ActiveStatusID.HasValue
    //                                                 && p.ActiveStatusID.Value == 1
    //                                                 && p.IsOnline.HasValue
    //                                                 && p.IsOnline.Value
    //                                                 && p.CompanyID.HasValue
    //                                                 select p).FirstOrDefault();

    //                        Data.Gateway gateway = new Data.Gateway()
    //                        {
    //                            ActiveStatusID = activeStatusID,
    //                            DateFirstSynced = DateTime.Now,
    //                            DateLastSynced = DateTime.Now,
    //                            GatewayID = m2mGW.id,
    //                            IsOnline = m2mGW.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true,
    //                            Name = m2mGW.name,
    //                            HardwareType = m2mGW.deviceTypeName,
    //                            Manufacturer = m2mGW.deviceTypeName,
    //                            HardwareID = m2mGW.type.id,
    //                            Since = Convert.ToDateTime(m2mGW.since),
    //                            SimCardNumber = m2mGW.network.msisdn,
    //                            FirmwareVersion = Convert.ToInt32(m2mGW.network.version),
    //                            Network = m2mGW.network.network,
    //                            GSMSerial = m2mGW.serial,
    //                            GISLocation = "", /// TODO: ??? (Original website hardcoded to always return nothing)
    //                            Signal = Convert.ToInt32(m2mGW.network.csq),
    //                            OnlineMeters = m2mGW.onlineDevices,
    //                            OfflineMeters = m2mGW.offlineDevices,
    //                            CompanyID = deviceWithCompany != null ? deviceWithCompany.CompanyID : null
    //                        };

    //                        db.Add(gateway);

    //                        #endregion
    //                    }

    //                    nCount++;

    //                    if (nCount >= 1000)
    //                    {
    //                        nCount = 0;
    //                        db.SaveChanges();
    //                        Console.WriteLine($"DB Update");
    //                    }

    //                }

    //                #endregion

    //                db.SaveChanges();
    //                Console.WriteLine($"DB Update");

    //                Serilog.Log.Information($"Update Gateways ended");
    //                DateTime gateways_endTime = DateTime.Now;
    //                DateTime devices_startTime = DateTime.Now;

    //                #region Update Devices

    //                List<string> serialsWithGateways = new List<string>();

    //                Serilog.Log.Information($"Update Devices started");
    //                #region Devices With Gateways


    //                currentGWCount = 0;
    //                int currentDeviceCount = 0;

    //                foreach (var m2mGW in m2mGateways)
    //                {
    //                    currentGWCount++;
    //                    var m2mDevices = m2mClient.GetGatewayDevices(m2mGW.id.ToString());
    //                    currentDeviceCount = 0;

    //                    if (m2mDevices == null)
    //                    {
    //                        Serilog.Log.Error("m2mDevices == null");
    //                        continue;
    //                    }

    //                    int deviceCount = m2mDevices.Length;

    //                    foreach (var m2mDev in m2mDevices)
    //                    {
    //                        currentDeviceCount++;
    //                        Console.WriteLine($"DEVICES - [{currentGWCount}/{gatewaysCount}] - [{currentDeviceCount}/{deviceCount}]");

    //                        if (!serialsWithGateways.Contains(m2mDev.serial))
    //                            serialsWithGateways.Add(m2mDev.serial);

    //                        var localDevice = localDevices.Where(p => p.DeviceIDLinked == m2mDev.id || p.Serial == m2mDev.serial).FirstOrDefault();
    //                        var skybillMeter = skybillCustomers.Where(p => p.Serial_No == m2mDev.serial).FirstOrDefault();

    //                        if (skybillMeter != null)
    //                        {
    //                            if (localDevice != null && (!localDevice.CompanyID.HasValue || localDevice.CompanyID.Value != skybillMeter.CompanyID))
    //                            {
    //                                localDevice.CompanyID = skybillMeter.CompanyID;
    //                                db.SaveChanges();
    //                            }
    //                        }

    //                        #region Deleted, stok and faulty does not need to be resynced

    //                        bool skipDevice = false;

    //                        bool didCreateNewDevice = false;

    //                        if (localDevice != null)
    //                        {
    //                            if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                                skipDevice = true;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                                skipDevice = true;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("FAULT"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                                skipDevice = true;
    //                            }
    //                        }
    //                        else if (m2mDev.name.ToUpper().Contains("DELETED") || m2mDev.name.ToUpper().Contains("STOCK") || m2mDev.name.ToUpper().Contains("FAULT"))
    //                        {
    //                            skipDevice = true;
    //                        }


    //                        #endregion

    //                        if (!skipDevice)
    //                        {
    //                            #region Registers

    //                            Dictionary<int, string> registers = new Dictionary<int, string>();
    //                            registers.Add(1, "readings"); // Active Energy
    //                            registers.Add(2, "readings"); // Reactive Energy
    //                            registers.Add(29, "readings"); // Max Demand
    //                            registers.Add(70, "readings"); // CT Ratio
    //                            registers.Add(80, "readings"); // Water Consumption
    //                            registers.Add(140, "readings"); // Gas Consumption
    //                            registers.Add(91, "readings"); // Contactor State
    //                            registers.Add(100, "readings"); // Internal Battery V
    //                            registers.Add(101, "readings"); // Signal RSSI
    //                            registers.Add(106, "readings"); // SNR
    //                            registers.Add(102, "readings"); // Temp
    //                            registers.Add(90, "readings"); // Remaining Credit


    //                            DateTime twoHoursAgo = DateTime.Now.AddHours(-2);
    //                            DateTime registerStartDate = new DateTime(twoHoursAgo.Year, twoHoursAgo.Month, twoHoursAgo.Day, twoHoursAgo.Hour, 0, 0);
    //                            DateTime registerEndDate = startDate.AddHours(2);

    //                            var readingRegisters = m2mClient.GetMeterUsage(m2mDev.id.ToString(), registerStartDate, registerEndDate, 60 * 60, registers);

    //                            // Declare and set all the registers
    //                            var activeEnergy = readingRegisters != null && readingRegisters.Where(p => p.name == "Active Energy").Count() != 0 ? readingRegisters.Where(p => p.name == "Active Energy").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Active Energy").SingleOrDefault().readings.Count() - 1] : null;

    //                            var reactiveEnergy = readingRegisters != null && readingRegisters.Where(p => p.name == "Reactive Energy").Count() != 0 ? readingRegisters.Where(p => p.name == "Reactive Energy").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Reactive Energy").SingleOrDefault().readings.Count() - 1] : null;

    //                            var maxDemand = readingRegisters != null && readingRegisters.Where(p => p.name == "Max Demand").Count() != 0 ? readingRegisters.Where(p => p.name == "Max Demand").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Max Demand").SingleOrDefault().readings.Count() - 1] : null;

    //                            var ctRatio = readingRegisters != null && readingRegisters.Where(p => p.name == "CT Ratio").Count() != 0 ? readingRegisters.Where(p => p.name == "CT Ratio").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "CT Ratio").SingleOrDefault().readings.Count() - 1] : null;

    //                            var waterConsumption = readingRegisters != null && readingRegisters.Where(p => p.name == "Water Consumption").Count() != 0 ? readingRegisters.Where(p => p.name == "Water Consumption").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Water Consumption").SingleOrDefault().readings.Count() - 1] : null;

    //                            var gasConsumption = readingRegisters != null && readingRegisters.Where(p => p.name == "Gas Consumption").Count() != 0 ? readingRegisters.Where(p => p.name == "Gas Consumption").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Gas Consumption").SingleOrDefault().readings.Count() - 1] : null;

    //                            var contactorState = readingRegisters != null && readingRegisters.Where(p => p.name == "Contactor State").Count() != 0 ? readingRegisters.Where(p => p.name == "Contactor State").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Contactor State").SingleOrDefault().readings.Count() - 1] : null;

    //                            var internalBattery = readingRegisters != null && readingRegisters.Where(p => p.name.Contains("Internal Battery")).Count() != 0 ? readingRegisters.Where(p => p.name.Contains("Internal Battery")).SingleOrDefault().readings[readingRegisters.Where(p => p.name.Contains("Internal Battery")).SingleOrDefault().readings.Count() - 1] : null;

    //                            var signalRSSI = readingRegisters != null && readingRegisters.Where(p => p.name == "Signal RSSI").Count() != 0 ? readingRegisters.Where(p => p.name == "Signal RSSI").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Signal RSSI").SingleOrDefault().readings.Count() - 1] : null;

    //                            var snr = readingRegisters != null && readingRegisters.Where(p => p.name == "SNR").Count() != 0 ? readingRegisters.Where(p => p.name == "SNR").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "SNR").SingleOrDefault().readings.Count() - 1] : null;

    //                            var temp = readingRegisters != null && readingRegisters.Where(p => p.name.Contains("Temp")).Count() != 0 ? readingRegisters.Where(p => p.name.Contains("Temp")).SingleOrDefault().readings[readingRegisters.Where(p => p.name.Contains("Temp")).SingleOrDefault().readings.Count() - 1] : null;

    //                            var remainingCredit = readingRegisters != null && readingRegisters.Where(p => p.name == "Remaining Credit").Count() != 0 ? readingRegisters.Where(p => p.name == "Remaining Credit").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Remaining Credit").SingleOrDefault().readings.Count() - 1] : null;


    //                            var config6Value = m2mClient.GetConfigValue(m2mDev.id);

    //                            #endregion

    //                            bool isOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true;

    //                            if (localDevice != null)
    //                            {
    //                                localDevice.Name = m2mDev.name;
    //                                localDevice.GatewayID = m2mGW.id;
    //                                localDevice.DateFirstSynced = DateTime.Now;
    //                                localDevice.DateLastSynced = DateTime.Now;
    //                                localDevice.IsOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true;
    //                                localDevice.TypeID = m2mDev.type != null ? (int?)m2mDev.type.id : null;
    //                                localDevice.MappingIndex = m2mDev.mapping.index;
    //                                localDevice.Port = m2mDev.mapping.port;
    //                                localDevice.Protocol = m2mDev.mapping.protocol;
    //                                localDevice.RemoteAddress = m2mDev.mapping.remote_address.ToString();
    //                                localDevice.RemoteIndex = m2mDev.mapping.remote_index;
    //                                localDevice.ProcessInterval = m2mDev.mapping.process_interval;
    //                                localDevice.LastCommunicated = Convert.ToDateTime(m2mDev.mapping.last_communicated);
    //                                localDevice.ActiveEnergy = activeEnergy;
    //                                localDevice.ReactiveEnergy = reactiveEnergy;
    //                                localDevice.MaxDemand = maxDemand;
    //                                localDevice.CTRatio = ctRatio;
    //                                localDevice.WaterConsumption = waterConsumption;
    //                                localDevice.GasConsumption = gasConsumption;
    //                                localDevice.ContactorState = contactorState.ToString();
    //                                localDevice.InternalBattery = internalBattery;
    //                                localDevice.SignalRSSI = signalRSSI;
    //                                localDevice.SNR = snr;
    //                                localDevice.Temp = temp;
    //                                localDevice.RemainingCredit = remainingCredit;

    //                                if (m2mDev.type != null)
    //                                {
    //                                    if (m2mDev.type.name.ToUpper().Contains("WATER"))
    //                                    {
    //                                        localDevice.WaterMeter = 1;
    //                                        localDevice.GasMeter = 0;
    //                                        localDevice.ElectricityMeter = 0;
    //                                    }
    //                                    else if (m2mDev.type.name.ToUpper().Contains("GAS"))
    //                                    {
    //                                        localDevice.WaterMeter = 0;
    //                                        localDevice.GasMeter = 1;
    //                                        localDevice.ElectricityMeter = 0;
    //                                    }
    //                                    else if (m2mDev.type.name.ToUpper().Contains("ELEC"))
    //                                    {
    //                                        localDevice.GasMeter = 0;
    //                                        localDevice.WaterMeter = 0;
    //                                        localDevice.ElectricityMeter = 1;
    //                                    }
    //                                }

    //                                localDevice.Config6Value = config6Value;

    //                                if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                                }
    //                                else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                                }
    //                                else if (m2mDev.name.ToUpper().Contains("FAULT"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                                }
    //                                else
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Active;

    //                                db.Update(localDevice);
    //                                //db.Devices.Update(localDevice, true);
    //                            }
    //                            else
    //                            {
    //                                localDevice = new Data.Device()
    //                                {
    //                                    DeviceIDLinked = m2mDev.id,
    //                                    Name = m2mDev.name,
    //                                    Serial = m2mDev.serial,
    //                                    CorrectingFactor = 0, /// TODO: ???
    //                                    CreateDate = DateTime.Now,
    //                                    DeviceSerialLinked = m2mDev.serial,
    //                                    GatewayID = m2mGW.id,
    //                                    DateFirstSynced = DateTime.Now,
    //                                    DateLastSynced = DateTime.Now,
    //                                    IsOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true,
    //                                    TypeID = m2mDev.type != null ? (int?)m2mDev.type.id : null,
    //                                    MappingIndex = m2mDev.mapping.index,
    //                                    Port = m2mDev.mapping.port,
    //                                    Protocol = m2mDev.mapping.protocol,
    //                                    RemoteAddress = m2mDev.mapping.remote_address.ToString(),
    //                                    RemoteIndex = m2mDev.mapping.remote_index,
    //                                    ProcessInterval = m2mDev.mapping.process_interval,
    //                                    LastCommunicated = Convert.ToDateTime(m2mDev.mapping.last_communicated),
    //                                    ActiveEnergy = activeEnergy,
    //                                    ReactiveEnergy = reactiveEnergy,
    //                                    MaxDemand = maxDemand,
    //                                    CTRatio = ctRatio,
    //                                    WaterConsumption = waterConsumption,
    //                                    GasConsumption = gasConsumption,
    //                                    ContactorState = contactorState.ToString(),
    //                                    InternalBattery = internalBattery,
    //                                    SignalRSSI = signalRSSI,
    //                                    SNR = snr,
    //                                    Temp = temp,
    //                                    RemainingCredit = remainingCredit,
    //                                    DisconnectionTypeID = m2mDev.autoDisconnect ? 1 : 2,
    //                                    ActiveStatusID = (int)Data.ActiveStatus.Active,

    //                                    Config6Value = config6Value,
    //                                };

    //                                if (skybillMeter != null)
    //                                    localDevice.CompanyID = skybillMeter.CompanyID;

    //                                if (m2mDev.type != null)
    //                                {
    //                                    if (m2mDev.type.name.ToUpper().Contains("WATER"))
    //                                        localDevice.WaterMeter = 1;
    //                                    else if (m2mDev.type.name.ToUpper().Contains("GAS"))
    //                                        localDevice.GasMeter = 1;
    //                                    else if (m2mDev.type.name.ToUpper().Contains("ELEC"))
    //                                        localDevice.ElectricityMeter = 1;
    //                                }

    //                                if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                                }
    //                                else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                                }
    //                                else if (m2mDev.name.ToUpper().Contains("FAULTY"))
    //                                {
    //                                    localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                                }

    //                                db.Add(localDevice);
    //                                didCreateNewDevice = true;
    //                            }


    //                            #region Check if its a vehicle using number plate regex on serial

    //                            List<string> regularExp = new List<string>();

    //                            regularExp.Add("([a-zA-Z]{2}[0-9]{2}[a-zA-Z]{4})+");

    //                            bool isVehucle = false;

    //                            foreach (var regx in regularExp)
    //                                if (Regex.IsMatch(m2mDev.serial, regx))
    //                                    isVehucle = true;

    //                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == (int)Data.DeviceType.DeviceTypeEnum.GPS)
    //                                isVehucle = true;

    //                            if (localDevice.Protocol.HasValue && localDevice.Protocol.Value == 17)
    //                                isVehucle = true;

    //                            if (isVehucle)
    //                            {
    //                                var vehicle = db.Vehicles.Where(p => p.DeviceIDLinked == m2mDev.id).SingleOrDefault();

    //                                if (vehicle == null)
    //                                {
    //                                    Data.Vehicle newVehicle = new Data.Vehicle()
    //                                    {
    //                                        DeviceIDLinked = m2mDev.id,
    //                                        RegistrationNumber = m2mDev.serial,
    //                                        RatePerKM = null,
    //                                        RegularDriverID = "",
    //                                        VehicleTypeName = "",
    //                                    };
    //                                    db.Add(newVehicle);
    //                                }
    //                                else if (string.IsNullOrEmpty(vehicle.RegistrationNumber))
    //                                {
    //                                    vehicle.RegistrationNumber = m2mDev.serial;
    //                                    db.Update(vehicle);
    //                                }
    //                            }

    //                            #endregion

    //                            #region NotificationCustomerMeters

    //                            var existingNotificationCustomerMeters = (from p in notificationCustomerMeters
    //                                                                      where p.MeterSerial == m2mDev.serial
    //                                                                      select p).Count();

    //                            if (existingNotificationCustomerMeters == 0)
    //                            {
    //                                Data.NotificationCustomerMeter notificationCustomerMeter = new Data.NotificationCustomerMeter()
    //                                {
    //                                    MeterSerial = m2mDev.serial,
    //                                    AutoDisconnect = true,
    //                                    LastUpdated = DateTime.Now,
    //                                    CustomerID = 0,
    //                                    DisconnectNotification = false,
    //                                    LowBalanceNotification1 = false,
    //                                    LowBalanceNotification2 = false,
    //                                    AccountType = 1,
    //                                };

    //                                db.Add(notificationCustomerMeter);
    //                                notificationMetersCreated++;
    //                            }

    //                            #endregion
    //                        }

    //                        if (!didCreateNewDevice)
    //                            if (localDevice != null)
    //                            {
    //                                localDevice.DateLastSynced = DateTime.Now;
    //                                localDevice.Name = m2mDev.name;
    //                                db.Update(localDevice);
    //                            }

    //                        nCount++;

    //                        if (nCount >= 1000)
    //                        {
    //                            nCount = 0;
    //                            db.SaveChanges();
    //                            Console.WriteLine($"DB Update");
    //                        }

    //                    }

    //                }

    //                #endregion

    //                db.SaveChanges();
    //                Console.WriteLine($"DB Update");
    //                Serilog.Log.Information($"Update Devices ended");

    //                Serilog.Log.Information($"Update Devices Without Gateways started");
    //                #region Devices Without Gateways

    //                DateTime devicesWithoutGW_startTime = DateTime.Now;

    //                localDevices = db.Devices.ToList();
    //                var m2mDevicesToCheckNoGW = m2mClient.GetAllDevices();

    //                nCount = 0;
    //                currentDeviceCount = 0;
    //                foreach (var m2mDev in m2mDevicesToCheckNoGW)
    //                {
    //                    currentDeviceCount++;
    //                    Console.WriteLine($"DEVICES WITHOUT GATEWAY CHECK - [{currentDeviceCount}/{m2mDevicesToCheckNoGW.Length}]");

    //                    if (serialsWithGateways.Contains(m2mDev.serial))
    //                        continue;


    //                    var localDevice = localDevices.Where(p => p.DeviceIDLinked == m2mDev.id || p.Serial == m2mDev.serial).FirstOrDefault();
    //                    var skybillMeter = skybillCustomers.Where(p => p.Serial_No == m2mDev.serial).FirstOrDefault();

    //                    if (skybillMeter != null)
    //                    {
    //                        if (localDevice != null && (!localDevice.CompanyID.HasValue || localDevice.CompanyID.Value != skybillMeter.CompanyID))
    //                        {
    //                            localDevice.CompanyID = skybillMeter.CompanyID;
    //                            db.SaveChanges();
    //                        }
    //                    }

    //                    #region Deleted, stok and faulty does not need to be resynced

    //                    bool skipDevice = false;

    //                    bool didCreateNewDevice = false;

    //                    if (localDevice != null)
    //                    {
    //                        if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                        {
    //                            localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                            skipDevice = true;
    //                        }
    //                        else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                        {
    //                            localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                            skipDevice = true;
    //                        }
    //                        else if (m2mDev.name.ToUpper().Contains("FAULT"))
    //                        {
    //                            localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                            skipDevice = true;
    //                        }
    //                    }
    //                    else if (m2mDev.name.ToUpper().Contains("DELETED") || m2mDev.name.ToUpper().Contains("STOCK") || m2mDev.name.ToUpper().Contains("FAULT"))
    //                    {
    //                        skipDevice = true;
    //                    }


    //                    #endregion

    //                    if (!skipDevice)
    //                    {
    //                        #region Registers

    //                        Dictionary<int, string> registers = new Dictionary<int, string>();
    //                        registers.Add(1, "readings"); // Active Energy
    //                        registers.Add(2, "readings"); // Reactive Energy
    //                        registers.Add(29, "readings"); // Max Demand
    //                        registers.Add(70, "readings"); // CT Ratio
    //                        registers.Add(80, "readings"); // Water Consumption
    //                        registers.Add(140, "readings"); // Gas Consumption
    //                        registers.Add(91, "readings"); // Contactor State
    //                        registers.Add(100, "readings"); // Internal Battery V
    //                        registers.Add(101, "readings"); // Signal RSSI
    //                        registers.Add(106, "readings"); // SNR
    //                        registers.Add(102, "readings"); // Temp
    //                        registers.Add(90, "readings"); // Remaining Credit


    //                        DateTime twoHoursAgo = DateTime.Now.AddHours(-2);
    //                        DateTime registerStartDate = new DateTime(twoHoursAgo.Year, twoHoursAgo.Month, twoHoursAgo.Day, twoHoursAgo.Hour, 0, 0);
    //                        DateTime registerEndDate = startDate.AddHours(2);

    //                        var readingRegisters = m2mClient.GetMeterUsage(m2mDev.id.ToString(), registerStartDate, registerEndDate, 60 * 60, registers);

    //                        // Declare and set all the registers
    //                        var activeEnergy = readingRegisters != null && readingRegisters.Where(p => p.name == "Active Energy").Count() != 0 ? readingRegisters.Where(p => p.name == "Active Energy").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Active Energy").SingleOrDefault().readings.Count() - 1] : null;

    //                        var reactiveEnergy = readingRegisters != null && readingRegisters.Where(p => p.name == "Reactive Energy").Count() != 0 ? readingRegisters.Where(p => p.name == "Reactive Energy").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Reactive Energy").SingleOrDefault().readings.Count() - 1] : null;

    //                        var maxDemand = readingRegisters != null && readingRegisters.Where(p => p.name == "Max Demand").Count() != 0 ? readingRegisters.Where(p => p.name == "Max Demand").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Max Demand").SingleOrDefault().readings.Count() - 1] : null;

    //                        var ctRatio = readingRegisters != null && readingRegisters.Where(p => p.name == "CT Ratio").Count() != 0 ? readingRegisters.Where(p => p.name == "CT Ratio").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "CT Ratio").SingleOrDefault().readings.Count() - 1] : null;

    //                        var waterConsumption = readingRegisters != null && readingRegisters.Where(p => p.name == "Water Consumption").Count() != 0 ? readingRegisters.Where(p => p.name == "Water Consumption").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Water Consumption").SingleOrDefault().readings.Count() - 1] : null;

    //                        var gasConsumption = readingRegisters != null && readingRegisters.Where(p => p.name == "Gas Consumption").Count() != 0 ? readingRegisters.Where(p => p.name == "Gas Consumption").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Gas Consumption").SingleOrDefault().readings.Count() - 1] : null;

    //                        var contactorState = readingRegisters != null && readingRegisters.Where(p => p.name == "Contactor State").Count() != 0 ? readingRegisters.Where(p => p.name == "Contactor State").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Contactor State").SingleOrDefault().readings.Count() - 1] : null;

    //                        var internalBattery = readingRegisters != null && readingRegisters.Where(p => p.name.Contains("Internal Battery")).Count() != 0 ? readingRegisters.Where(p => p.name.Contains("Internal Battery")).SingleOrDefault().readings[readingRegisters.Where(p => p.name.Contains("Internal Battery")).SingleOrDefault().readings.Count() - 1] : null;

    //                        var signalRSSI = readingRegisters != null && readingRegisters.Where(p => p.name == "Signal RSSI").Count() != 0 ? readingRegisters.Where(p => p.name == "Signal RSSI").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Signal RSSI").SingleOrDefault().readings.Count() - 1] : null;

    //                        var snr = readingRegisters != null && readingRegisters.Where(p => p.name == "SNR").Count() != 0 ? readingRegisters.Where(p => p.name == "SNR").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "SNR").SingleOrDefault().readings.Count() - 1] : null;

    //                        var temp = readingRegisters != null && readingRegisters.Where(p => p.name.Contains("Temp")).Count() != 0 ? readingRegisters.Where(p => p.name.Contains("Temp")).SingleOrDefault().readings[readingRegisters.Where(p => p.name.Contains("Temp")).SingleOrDefault().readings.Count() - 1] : null;

    //                        var remainingCredit = readingRegisters != null && readingRegisters.Where(p => p.name == "Remaining Credit").Count() != 0 ? readingRegisters.Where(p => p.name == "Remaining Credit").SingleOrDefault().readings[readingRegisters.Where(p => p.name == "Remaining Credit").SingleOrDefault().readings.Count() - 1] : null;


    //                        var config6Value = m2mClient.GetConfigValue(m2mDev.id);

    //                        #endregion

    //                        bool isOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true;

    //                        if (localDevice != null)
    //                        {
    //                            localDevice.Name = m2mDev.name;
    //                            localDevice.DateFirstSynced = DateTime.Now;
    //                            localDevice.DateLastSynced = DateTime.Now;
    //                            localDevice.IsOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true;
    //                            localDevice.TypeID = m2mDev.type != null ? (int?)m2mDev.type.id : null;
    //                            if (m2mDev.mapping != null)
    //                            {
    //                                localDevice.MappingIndex = m2mDev.mapping.index;
    //                                localDevice.Port = m2mDev.mapping.port;
    //                                localDevice.Protocol = m2mDev.mapping.protocol;
    //                                localDevice.RemoteAddress = m2mDev.mapping.remote_address.ToString();
    //                                localDevice.RemoteIndex = m2mDev.mapping.remote_index;
    //                                localDevice.ProcessInterval = m2mDev.mapping.process_interval;
    //                                localDevice.LastCommunicated = Convert.ToDateTime(m2mDev.mapping.last_communicated);
    //                            }
    //                            localDevice.ActiveEnergy = activeEnergy;
    //                            localDevice.ReactiveEnergy = reactiveEnergy;
    //                            localDevice.MaxDemand = maxDemand;
    //                            localDevice.CTRatio = ctRatio;
    //                            localDevice.WaterConsumption = waterConsumption;
    //                            localDevice.GasConsumption = gasConsumption;
    //                            localDevice.ContactorState = contactorState.ToString();
    //                            localDevice.InternalBattery = internalBattery;
    //                            localDevice.SignalRSSI = signalRSSI;
    //                            localDevice.SNR = snr;
    //                            localDevice.Temp = temp;
    //                            localDevice.RemainingCredit = remainingCredit;

    //                            if (m2mDev.type != null)
    //                            {
    //                                if (m2mDev.type.name.ToUpper().Contains("WATER"))
    //                                {
    //                                    localDevice.WaterMeter = 1;
    //                                    localDevice.GasMeter = 0;
    //                                    localDevice.ElectricityMeter = 0;
    //                                }
    //                                else if (m2mDev.type.name.ToUpper().Contains("GAS"))
    //                                {
    //                                    localDevice.WaterMeter = 0;
    //                                    localDevice.GasMeter = 1;
    //                                    localDevice.ElectricityMeter = 0;
    //                                }
    //                                else if (m2mDev.type.name.ToUpper().Contains("ELEC"))
    //                                {
    //                                    localDevice.GasMeter = 0;
    //                                    localDevice.WaterMeter = 0;
    //                                    localDevice.ElectricityMeter = 1;
    //                                }
    //                            }

    //                            localDevice.Config6Value = config6Value;

    //                            if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("FAULT"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                            }
    //                            else
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Active;

    //                            db.Update(localDevice);
    //                            //db.Devices.Update(localDevice, true);
    //                        }
    //                        else
    //                        {
    //                            localDevice = new Data.Device()
    //                            {
    //                                DeviceIDLinked = m2mDev.id,
    //                                Name = m2mDev.name,
    //                                Serial = m2mDev.serial,
    //                                CorrectingFactor = 0, /// TODO: ???
    //                                CreateDate = DateTime.Now,
    //                                DeviceSerialLinked = m2mDev.serial,
    //                                DateFirstSynced = DateTime.Now,
    //                                DateLastSynced = DateTime.Now,
    //                                IsOnline = m2mDev.deviceStatus.ToLower().Contains("offline".ToLower()) ? false : true,
    //                                TypeID = m2mDev.type != null ? (int?)m2mDev.type.id : null,
    //                                ActiveEnergy = activeEnergy,
    //                                ReactiveEnergy = reactiveEnergy,
    //                                MaxDemand = maxDemand,
    //                                CTRatio = ctRatio,
    //                                WaterConsumption = waterConsumption,
    //                                GasConsumption = gasConsumption,
    //                                ContactorState = contactorState.ToString(),
    //                                InternalBattery = internalBattery,
    //                                SignalRSSI = signalRSSI,
    //                                SNR = snr,
    //                                Temp = temp,
    //                                RemainingCredit = remainingCredit,
    //                                DisconnectionTypeID = m2mDev.autoDisconnect ? 1 : 2,
    //                                ActiveStatusID = (int)Data.ActiveStatus.Active,

    //                                Config6Value = config6Value,
    //                            };

    //                            if (m2mDev.mapping != null)
    //                            {
    //                                localDevice.MappingIndex = m2mDev.mapping.index;
    //                                localDevice.Port = m2mDev.mapping.port;
    //                                localDevice.Protocol = m2mDev.mapping.protocol;
    //                                localDevice.RemoteAddress = m2mDev.mapping.remote_address.ToString();
    //                                localDevice.RemoteIndex = m2mDev.mapping.remote_index;
    //                                localDevice.ProcessInterval = m2mDev.mapping.process_interval;
    //                                localDevice.LastCommunicated = Convert.ToDateTime(m2mDev.mapping.last_communicated);
    //                            }

    //                            if (skybillMeter != null)
    //                                localDevice.CompanyID = skybillMeter.CompanyID;

    //                            if (m2mDev.type != null)
    //                            {
    //                                if (m2mDev.type.name.ToUpper().Contains("WATER"))
    //                                    localDevice.WaterMeter = 1;
    //                                else if (m2mDev.type.name.ToUpper().Contains("GAS"))
    //                                    localDevice.GasMeter = 1;
    //                                else if (m2mDev.type.name.ToUpper().Contains("ELEC"))
    //                                    localDevice.ElectricityMeter = 1;
    //                            }

    //                            if (m2mDev.name.ToUpper().Contains("DELETED"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Deleted;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("STOCK"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Inventory;
    //                            }
    //                            else if (m2mDev.name.ToUpper().Contains("FAULTY"))
    //                            {
    //                                localDevice.ActiveStatusID = (int)Data.ActiveStatus.Faulty;
    //                            }

    //                            db.Add(localDevice);
    //                            didCreateNewDevice = true;
    //                        }


    //                        #region Check if its a vehicle using number plate regex on serial

    //                        List<string> regularExp = new List<string>();

    //                        regularExp.Add("([a-zA-Z]{2}[0-9]{2}[a-zA-Z]{4})+");

    //                        bool isVehucle = false;

    //                        foreach (var regx in regularExp)
    //                            if (Regex.IsMatch(m2mDev.serial, regx))
    //                                isVehucle = true;

    //                        if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == (int)Data.DeviceType.DeviceTypeEnum.GPS)
    //                            isVehucle = true;

    //                        if (localDevice.Protocol.HasValue && localDevice.Protocol.Value == 17)
    //                            isVehucle = true;

    //                        if (isVehucle)
    //                        {
    //                            var vehicle = db.Vehicles.Where(p => p.DeviceIDLinked == m2mDev.id).SingleOrDefault();

    //                            if (vehicle == null)
    //                            {
    //                                Data.Vehicle newVehicle = new Data.Vehicle()
    //                                {
    //                                    DeviceIDLinked = m2mDev.id,
    //                                    RegistrationNumber = m2mDev.serial,
    //                                    RatePerKM = null,
    //                                    RegularDriverID = "",
    //                                    VehicleTypeName = "",
    //                                };
    //                                db.Add(newVehicle);
    //                            }
    //                            else if (string.IsNullOrEmpty(vehicle.RegistrationNumber))
    //                            {
    //                                vehicle.RegistrationNumber = m2mDev.serial;
    //                                db.Update(vehicle);
    //                            }
    //                        }

    //                        #endregion

    //                        #region NotificationCustomerMeters

    //                        var existingNotificationCustomerMeters = (from p in notificationCustomerMeters
    //                                                                  where p.MeterSerial == m2mDev.serial
    //                                                                  select p).Count();

    //                        if (existingNotificationCustomerMeters == 0)
    //                        {
    //                            Data.NotificationCustomerMeter notificationCustomerMeter = new Data.NotificationCustomerMeter()
    //                            {
    //                                MeterSerial = m2mDev.serial,
    //                                AutoDisconnect = true,
    //                                LastUpdated = DateTime.Now,
    //                                CustomerID = 0,
    //                                DisconnectNotification = false,
    //                                LowBalanceNotification1 = false,
    //                                LowBalanceNotification2 = false,
    //                                AccountType = 1,
    //                            };

    //                            db.Add(notificationCustomerMeter);
    //                            notificationMetersCreated++;
    //                        }

    //                        #endregion
    //                    }

    //                    if (!didCreateNewDevice)
    //                        if (localDevice != null)
    //                        {
    //                            localDevice.DateLastSynced = DateTime.Now;
    //                            localDevice.Name = m2mDev.name;
    //                            db.Update(localDevice);
    //                        }

    //                    nCount++;

    //                    if (nCount >= 1000)
    //                    {
    //                        nCount = 0;
    //                        db.SaveChanges();
    //                        Console.WriteLine($"DB Update");
    //                    }


    //                }

    //                db.SaveChanges();
    //                Console.WriteLine($"DB Update");

    //                DateTime devicesWithoutGW_endTime = DateTime.Now;


    //                #endregion
    //                db.SaveChanges();
    //                Console.WriteLine($"DB Update");
    //                Serilog.Log.Information($"Update Devices Without Gateways ended");

    //                #endregion
    //                DateTime devices_endTime = DateTime.Now;

    //                Serilog.Log.Information($"Recheck Db for gateways companyID started");
    //                DateTime reassignCompanyID_startTime = DateTime.Now;
    //                #region Recheck Db for gateways companyID (first in list of device where companyID.hasvalue)

    //                Console.WriteLine($"Recheck Db for gateways companyID");


    //                var gatewaysForRecheckingCompanyID = db.Gateways.ToList();

    //                foreach (var gatewayToCheck in gatewaysForRecheckingCompanyID)
    //                {
    //                    var deviceCompanyID = (from p in db.Devices
    //                                           where p.GatewayID.HasValue
    //                                           && p.GatewayID.Value == gatewayToCheck.GatewayID
    //                                           && p.IsOnline.HasValue
    //                                           && p.IsOnline.Value
    //                                           && p.CompanyID.HasValue
    //                                           select p).FirstOrDefault();

    //                    if (deviceCompanyID != null)
    //                    {
    //                        if (gatewayToCheck.CompanyID != deviceCompanyID.CompanyID)
    //                        {
    //                            var gwToUpdate = (from p in db.Gateways
    //                                              where p.ID == gatewayToCheck.ID
    //                                              select p).SingleOrDefault();

    //                            gwToUpdate.CompanyID = deviceCompanyID.CompanyID;
    //                            db.SaveChanges();
    //                        }
    //                    }
    //                    else if (!gatewayToCheck.CompanyID.HasValue)
    //                    {
    //                        // First 4 substring check
    //                        string startsWithGWName = gatewayToCheck.Name.ToUpper();
    //                        if (gatewayToCheck.Name.Length > 3)
    //                            startsWithGWName = gatewayToCheck.Name.ToUpper().Substring(0, 3);
    //                        startsWithGWName = startsWithGWName.Trim();

    //                        var company = (from p in db.Companies
    //                                       where p.Name.ToUpper().StartsWith(startsWithGWName)
    //                                       select p).FirstOrDefault();

    //                        if (company != null)
    //                        {
    //                            var gwToUpdate = (from p in db.Gateways
    //                                              where p.ID == gatewayToCheck.ID
    //                                              select p).SingleOrDefault();

    //                            gwToUpdate.CompanyID = company.CompanyID;
    //                            db.SaveChanges();
    //                        }
    //                    }

    //                }

    //                #endregion
    //                DateTime reassignCompanyID_endTime = DateTime.Now;
    //                Serilog.Log.Information($"Recheck Db for gateways companyID ended");


    //                sbEmail.AppendLine($"Gateways Start Time:{gateways_startTime}");
    //                sbEmail.AppendLine($"Gateways End Time:{gateways_endTime}");
    //                sbEmail.AppendLine($"Total:{(gateways_endTime - gateways_startTime).TotalMinutes:N} min");
    //                sbEmail.AppendLine("");

    //                sbEmail.AppendLine($"Devices Start Time:{devices_startTime}");
    //                sbEmail.AppendLine($"Devices End Time:{devices_endTime}");
    //                sbEmail.AppendLine($"Total:{(devices_endTime - devices_startTime).TotalMinutes:N} min");
    //                sbEmail.AppendLine("");

    //                sbEmail.AppendLine($"Devices Without GW Start Time:{devicesWithoutGW_startTime}");
    //                sbEmail.AppendLine($"Devices Without GW End Time:{devicesWithoutGW_endTime}");
    //                sbEmail.AppendLine($"Total:{(devicesWithoutGW_endTime - devicesWithoutGW_startTime).TotalMinutes:N} min");
    //                sbEmail.AppendLine("");

    //                sbEmail.AppendLine($"Reassign GW CompanyID Start Time:{reassignCompanyID_startTime}");
    //                sbEmail.AppendLine($"Reassign GW CompanyID End Time:{reassignCompanyID_endTime}");
    //                sbEmail.AppendLine($"Total:{(reassignCompanyID_endTime - reassignCompanyID_startTime).TotalMinutes:N} min");
    //                sbEmail.AppendLine("");

    //                sbEmail.AppendLine("<b>");
    //                sbEmail.AppendLine($"Start Time:{startDate}");
    //                sbEmail.AppendLine($"End Time:{DateTime.Now}");
    //                sbEmail.AppendLine($"Total:{(DateTime.Now - startDate).TotalMinutes:N} min");
    //                sbEmail.AppendLine($"New Disconnection Settings Created:{notificationMetersCreated}");
    //                sbEmail.AppendLine("</b>");
    //                sbEmail.AppendLine("");

    //                if (errors.Count > 0)
    //                {
    //                    sbEmail.AppendLine();
    //                    sbEmail.AppendLine("Errors:");
    //                    foreach (var err in errors)
    //                        sbEmail.AppendLine(err);
    //                }


    //                string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //                string ftpFileName = $"GatewaysAndDevicesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
    //                string username = $"systemgeneratedreports";
    //                string password = $"tGWd74yGHczN";

    //                Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

    //                systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //                systemGeneratedReport.DateEnded = DateTime.Now;
    //                db.Update(systemGeneratedReport);
    //                db.SaveChanges();

    //            }
    //            catch (Exception ex)
    //            {
    //                string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //                string ftpFileName = $"GatewaysAndDevicesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
    //                string username = $"systemgeneratedreports";
    //                string password = $"tGWd74yGHczN";

    //                Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

    //                systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //                systemGeneratedReport.DateEnded = DateTime.Now;
    //                db.Update(systemGeneratedReport);
    //                db.SaveChanges();

    //                EmailSender emailSender = new EmailSender();
    //                emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "GatewaysAndDevicesSync Error", ex.ToString(), ex.ToString(), from: "errors@mymetersa.co.za");
    //                //throw ex;
    //            }
    //        }
    //    }



    //}


    //public class M2MJob_MeterActiveEnergyAnomalies
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;

    //    public M2MJob_MeterActiveEnergyAnomalies(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //    }

    //    public async Task Run()
    //    {
    //        // execute 2021-05-31
    //        // start 2021-05-01
    //        // end date 2021-06-01
    //        DateTime startChecking = new DateTime(2021, 01, 01);
    //        DateTime endChecking = DateTime.Now;
    //        DateTime current = startChecking.Date;
    //        while (current <= endChecking)
    //        {
    //            RunCustomersSync(
    //               startChecking: new DateTime(current.Year, current.Month, 1).Date,
    //               endChecking: new DateTime(current.AddDays(1).Year, current.AddDays(1).Month, DateTime.DaysInMonth(current.AddDays(1).Year, current.AddDays(1).Month))
    //                );
    //            current = current.AddMonths(1);
    //        }
    //    }

    //    public async void RunCustomersSync(DateTime startChecking, DateTime endChecking)
    //    {
    //        return;
    //        using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
    //        {
    //            int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_MeterActiveEnergyAnomalies;
    //            DateTime startDate = DateTime.Now;

    //            Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
    //            {
    //                DateStarted = startDate,
    //                ReportURL = "",
    //                SecureAreaID = secureAreaID,
    //            };
    //            db.Add(systemGeneratedReport);
    //            db.SaveChanges();

    //            //try
    //            //{
    //            StringBuilder sbEmail = new StringBuilder();
    //            List<string> errors = new List<string>();
    //            int nCount = 0;
    //            int rowsAddedCount = 0;

    //            var m2mClient = new Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);

    //            var m2mGateways = m2mClient.GetGateways();
    //            Serilog.Log.Information($"m2mClient.GetGateways() - {m2mGateways.Length}");

    //            var m2mDevicesToCheckNoGW = m2mClient.GetAllDevices();
    //            Serilog.Log.Information($"m2mClient.GetAllDevices() - {m2mDevicesToCheckNoGW.Length}");

    //            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"MeterActiveEnergyAnomalies_{DateTime.Now:yyyy_MM_dd}");

    //            if (!System.IO.Directory.Exists(rootFolder))
    //                System.IO.Directory.CreateDirectory(rootFolder);

    //            string zipFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"MeterActiveEnergyAnomalies_{DateTime.Now:yyyy_MM_dd_HH_mm}.zip");
    //            string excelFileName = Path.Combine(rootFolder, $"MeterActiveEnergyAnomalies_{DateTime.Now:yyyy_MM_dd_HH_mm}.xlsx");

    //            // 077.SEASIDE (2020-02-01)
    //            var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == 77).ToList();
    //            m2mDevicesToCheckNoGW = m2mDevicesToCheckNoGW.Where(p => sbCustomers.Select(c => c.Serial_No).Contains(p.serial)).ToArray();
    //            //bool bBreak = false;
    //            foreach (var m2mDev in m2mDevicesToCheckNoGW)
    //            {
    //                //if (bBreak)
    //                //    break;
    //                nCount++;

    //                if (sbCustomers.Where(p => p.Serial_No == m2mDev.serial).Count() == 0)
    //                    continue;

    //                string start = startChecking.Date.ToString("yyyy-MM-ddTHH:mm:ss");
    //                string end = endChecking.Date.ToString("yyyy-MM-ddTHH:mm:ss");
    //                Dictionary<int, string> registers = new Dictionary<int, string>();
    //                registers.Add(1, "readings"); // Active Energy

    //                var registerStr = "";
    //                foreach (var register in registers)
    //                {
    //                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
    //                }

    //                string url = $"devices/{m2mDev.id}/data.csv?start={start}&end={end}&interval=900{registerStr}";
    //                var result = m2mClient.GetString(url);

    //                bool first = true;

    //                System.Data.DataTable dataTable = new System.Data.DataTable();

    //                foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
    //                {
    //                    if (first)
    //                    {
    //                        foreach (var lineVar in fileLine.Split(','))
    //                        {
    //                            string safeName = lineVar.Replace("\"", string.Empty);
    //                            Type colType = typeof(string);

    //                            if (safeName == "Time Logged")
    //                                colType = typeof(DateTime);

    //                            dataTable.Columns.Add(safeName, colType);
    //                        }
    //                        first = false;
    //                        continue;
    //                    }

    //                    DataRow row = dataTable.NewRow();
    //                    int colIndex = 0;
    //                    foreach (var lineVar in fileLine.Split(','))
    //                    {
    //                        string safeName = lineVar.Replace("\"", string.Empty);
    //                        if (colIndex == 1)
    //                            row[colIndex] = Convert.ToDateTime(safeName);
    //                        else
    //                            row[colIndex] = safeName;
    //                        colIndex++;
    //                    }

    //                    dataTable.Rows.Add(row);
    //                    dataTable.AcceptChanges();
    //                }

    //                if (dataTable.Rows.Count > 0)
    //                {
    //                    DataView dv = dataTable.DefaultView;
    //                    dv.Sort = "[Time Logged] asc";
    //                    System.Data.DataTable sortedDT = dv.ToTable();

    //                    decimal? currentReading = null;
    //                    decimal? previousReading = null;
    //                    int negativeDiffCount = 0;
    //                    int totalReadingCount = 0;
    //                    decimal totalReading = 0;
    //                    decimal negativeDiffReading = 0;
    //                    DateTime lastNegTimeLogged = new DateTime();
    //                    bool hasStartedGoingDown = false;
    //                    decimal negativeDiff = 0;

    //                    SystemGeneratedReports_MeterActiveEnergyAnomalie previousRow = new SystemGeneratedReports_MeterActiveEnergyAnomalie()
    //                    {
    //                        SystemGeneratedReportID = systemGeneratedReport.ID,
    //                    };

    //                    StringBuilder sbReport = new StringBuilder();

    //                    bool bFirst = true;
    //                    int rowIndex = 0;

    //                    foreach (DataRow dr in sortedDT.Rows)
    //                    {
    //                        if (dr.Table.Columns.Count > 2 && dr[2] != DBNull.Value && !string.IsNullOrEmpty(dr[2].ToString()))
    //                        {
    //                            if (bFirst)
    //                            {
    //                                sbReport.AppendLine($"{dr[1]} - {dr[2]} - First;");

    //                                bFirst = false;
    //                            }

    //                            totalReadingCount++;
    //                            currentReading = Convert.ToDecimal(dr[2]);
    //                            totalReading += currentReading.Value;
    //                            if (previousReading.HasValue)
    //                            {
    //                                // calculate diff
    //                                decimal diff = Convert.ToDecimal(dr[2]) - previousReading.Value;
    //                                sbReport.AppendLine($"{dr[1]} - {dr[2]} - {diff} - DIFF;");

    //                                if (diff < 0)
    //                                {
    //                                    negativeDiffCount++;
    //                                    negativeDiffReading += diff;
    //                                    lastNegTimeLogged = Convert.ToDateTime(dr[1]);
    //                                    hasStartedGoingDown = true;
    //                                    negativeDiff = diff;
    //                                    previousReading = currentReading;


    //                                }
    //                                else if (hasStartedGoingDown)
    //                                {
    //                                    // went back to positive after negative
    //                                    if (negativeDiffCount > 0)
    //                                    {
    //                                        #region Add Last 3 Rows

    //                                        decimal? last3PreviousReading = null;

    //                                        for (int i = 4; i >= 2; i--)
    //                                        {
    //                                            int ntwoRowsAgoIndex = sortedDT.Rows.IndexOf(dr) - i;
    //                                            if (ntwoRowsAgoIndex >= 0)
    //                                            {
    //                                                #region Temp object for previous line

    //                                                DataRow previousDR = sortedDT.Rows[ntwoRowsAgoIndex];

    //                                                previousRow.MeterID = m2mDev.id.ToString();
    //                                                previousRow.Serial = m2mDev.serial;
    //                                                previousRow.Name = m2mDev.name;
    //                                                previousRow.TimeLogged = Convert.ToDateTime(previousDR[1]);
    //                                                previousRow.ActiveEnergyReading = Convert.ToDecimal(previousDR[2]);
    //                                                previousRow.Diff = last3PreviousReading.HasValue ? (Convert.ToDecimal(previousDR[2]) - last3PreviousReading.Value) : 0;
    //                                                if (totalReadingCount > 0)
    //                                                    previousRow.ErrorPerc = (negativeDiffCount / totalReadingCount) * 100.0m;

    //                                                #endregion


    //                                                db.Add(previousRow);
    //                                                db.SaveChanges();
    //                                                //bBreak = true;
    //                                                //Console.Beep();
    //                                                previousRow = new SystemGeneratedReports_MeterActiveEnergyAnomalie()
    //                                                {
    //                                                    SystemGeneratedReportID = systemGeneratedReport.ID,
    //                                                };
    //                                                last3PreviousReading = Convert.ToDecimal(previousDR[2]);

    //                                                //Console.WriteLine(sbReport.ToString());
    //                                            }
    //                                        }

    //                                        #endregion

    //                                        SystemGeneratedReports_MeterActiveEnergyAnomalie dataRow = new SystemGeneratedReports_MeterActiveEnergyAnomalie()
    //                                        {
    //                                            SystemGeneratedReportID = systemGeneratedReport.ID,
    //                                            MeterID = m2mDev.id.ToString(),
    //                                            Serial = m2mDev.serial,
    //                                            Name = m2mDev.name,
    //                                            TimeLogged = lastNegTimeLogged,
    //                                            ActiveEnergyReading = currentReading.HasValue ? currentReading.Value : 0,
    //                                            Diff = negativeDiff,
    //                                            Report = sbReport.ToString(),
    //                                        };

    //                                        if (totalReadingCount > 0)
    //                                            dataRow.ErrorPerc = (negativeDiffCount / totalReadingCount) * 100.0m;

    //                                        db.Add(dataRow);
    //                                        db.SaveChanges();


    //                                        //if (bBreak)
    //                                        //    break;

    //                                        previousReading = currentReading;
    //                                        negativeDiffCount = 0;
    //                                        //totalReadingCount = 0;
    //                                        //totalReading = 0;
    //                                        negativeDiffReading = 0;
    //                                        lastNegTimeLogged = new DateTime();
    //                                        hasStartedGoingDown = false;
    //                                        negativeDiff = 0;
    //                                        rowsAddedCount++;

    //                                    }

    //                                }

    //                            }

    //                            previousReading = currentReading;


    //                        }
    //                        rowIndex++;
    //                    }

    //                    decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(m2mDevicesToCheckNoGW.Length)) * 100.0m;

    //                    systemGeneratedReport.Progress = progress;
    //                    db.Update(systemGeneratedReport);
    //                    db.SaveChanges();
    //                    Console.WriteLine($"{progress:N2} - [{nCount}/{m2mDevicesToCheckNoGW.Length}] - {rowsAddedCount}");

    //                }

    //            }

    //            var finalResult = (from p in db.SystemGeneratedReports_MeterActiveEnergyAnomalies
    //                               where p.SystemGeneratedReportID == systemGeneratedReport.ID
    //                               select new
    //                               {
    //                                   p.MeterID,
    //                                   p.Serial,
    //                                   p.Name,
    //                                   p.TimeLogged,
    //                                   p.ActiveEnergyReading,
    //                                   p.Diff,
    //                                   p.ErrorPerc,
    //                                   Problem = !string.IsNullOrEmpty(p.Report)
    //                               }
    //                               ).ToList();

    //            if (finalResult.Count > 0)
    //            {
    //                var workbook = new ClosedXML.Excel.XLWorkbook();

    //                var worksheet = workbook.Worksheets.Add("MeterActiveEnergyAnomalies");

    //                var table = worksheet.Cell(1, 1).InsertTable(finalResult, "MeterActiveEnergyAnomalies", true);

    //                worksheet.Columns("A", "ZZ").AdjustToContents();

    //                workbook.SaveAs(excelFileName);
    //            }


    //            sbEmail.AppendLine($"Start Time:{startDate}");
    //            sbEmail.AppendLine($"End Time:{DateTime.Now}");
    //            sbEmail.AppendLine($"Total:{(DateTime.Now - startDate).TotalMinutes:N} min");
    //            sbEmail.AppendLine("</b>");
    //            sbEmail.AppendLine("");

    //            if (errors.Count > 0)
    //            {
    //                sbEmail.AppendLine();
    //                sbEmail.AppendLine("Errors:");
    //                foreach (var err in errors)
    //                    sbEmail.AppendLine(err);
    //            }



    //            if (System.IO.File.Exists(zipFileName))
    //                System.IO.File.Delete(zipFileName);

    //            ZipFile.CreateFromDirectory(rootFolder, zipFileName);

    //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //            string ftpFileName = $"MeterActiveEnergyAnomalies_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.zip";
    //            string username = $"systemgeneratedreports";
    //            string password = $"tGWd74yGHczN";

    //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, System.IO.File.ReadAllBytes(zipFileName), username, password);

    //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //            systemGeneratedReport.DateEnded = DateTime.Now;
    //            db.Update(systemGeneratedReport);
    //            db.SaveChanges();

    //            System.IO.File.Delete(zipFileName);
    //            System.IO.Directory.Delete(rootFolder, true);
    //            Console.Beep();

    //            //}
    //            //catch (Exception ex)
    //            //{
    //            //    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //            //    string ftpFileName = $"MeterActiveEnergyAnomalies_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
    //            //    string username = $"systemgeneratedreports";
    //            //    string password = $"tGWd74yGHczN";

    //            //    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

    //            //    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //            //    systemGeneratedReport.DateEnded = DateTime.Now;
    //            //    db.Update(systemGeneratedReport);
    //            //    db.SaveChanges();

    //            //    EmailSender emailSender = new EmailSender();
    //            //    emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "MeterActiveEnergyAnomalies Error", ex.ToString(), ex.ToString(), from: "errors@mymetersa.co.za");
    //            //    throw ex;
    //            //}
    //        }
    //    }



    //}

    //public class H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;

    //    public H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //    }

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
    //        {
    //            var reportsDue = (from p in db.MeterActiveEnergyAnomaliesRuns
    //                              where !p.DateEnded.HasValue
    //                              select p).ToList();


    //            var m2mClient = new Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //            var allsbCustomers = db.SkybillCustomers.ToList();

    //            foreach (var rep in reportsDue)
    //            {
    //                var repToUpdate = db.MeterActiveEnergyAnomaliesRuns.Where(p => p.ID == rep.ID).SingleOrDefault();
    //                repToUpdate.DateStarted = DateTime.Now;
    //                db.Update(repToUpdate);
    //                db.SaveChanges();

    //                int nCount = 0;

    //                var sbCustomersSerials = allsbCustomers.Where(p => p.CompanyID == rep.CompanyID).Select(p => p.Serial_No).Distinct().ToList();
    //                //bool bBreak = false;
    //                foreach (var serial in sbCustomersSerials)
    //                {
    //                    //if (bBreak)
    //                    //    break;
    //                    nCount++;

    //                    var m2mDev = m2mClient.GetDeviceByMeterNumber(serial);
    //                    if (m2mDev != null)
    //                    {
    //                        var entriesToCheck = (from p in db.MeterActiveEnergyAnomalies
    //                                              where p.CompanyID == rep.CompanyID
    //                                              && p.TimeLogged.Date >= rep.FromDate
    //                                              && p.TimeLogged.Date <= rep.ToDate
    //                                              select p).ToList();

    //                        DateTime current = rep.FromDate;
    //                        while (current.Date <= rep.ToDate.Date)
    //                        {
    //                            Dictionary<int, string> registers = new Dictionary<int, string>();
    //                            registers.Add(1, "readings"); // Active Energy

    //                            var registerStr = "";
    //                            foreach (var register in registers)
    //                            {
    //                                registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
    //                            }

    //                            string start = current.Date.ToString("yyyy-MM-ddTHH:mm:ss");
    //                            string end = current.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

    //                            string url = $"devices/{m2mDev.id}/data.csv?start={start}&end={end}&interval=900{registerStr}";
    //                            var result = m2mClient.GetString(url);

    //                            bool first = true;

    //                            System.Data.DataTable dataTable = new System.Data.DataTable();

    //                            foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
    //                            {
    //                                if (first)
    //                                {
    //                                    foreach (var lineVar in fileLine.Split(','))
    //                                    {
    //                                        string safeName = lineVar.Replace("\"", string.Empty);
    //                                        Type colType = typeof(string);

    //                                        if (safeName == "Time Logged")
    //                                            colType = typeof(DateTime);

    //                                        dataTable.Columns.Add(safeName, colType);
    //                                    }
    //                                    first = false;
    //                                    continue;
    //                                }

    //                                DataRow row = dataTable.NewRow();
    //                                int colIndex = 0;
    //                                foreach (var lineVar in fileLine.Split(','))
    //                                {
    //                                    string safeName = lineVar.Replace("\"", string.Empty);
    //                                    if (colIndex == 1)
    //                                        row[colIndex] = Convert.ToDateTime(safeName);
    //                                    else
    //                                        row[colIndex] = safeName;
    //                                    colIndex++;
    //                                }

    //                                dataTable.Rows.Add(row);
    //                                dataTable.AcceptChanges();
    //                            }

    //                            if (dataTable.Rows.Count > 0)
    //                            {
    //                                DataView dv = dataTable.DefaultView;
    //                                dv.Sort = "[Time Logged] asc";
    //                                System.Data.DataTable sortedDT = dv.ToTable();

    //                                decimal? currentReading = null;
    //                                decimal? previousReading = null;
    //                                int negativeDiffCount = 0;
    //                                int totalReadingCount = 0;
    //                                decimal totalReading = 0;
    //                                decimal negativeDiffReading = 0;
    //                                //DateTime lastNegTimeLogged = new DateTime();
    //                                bool hasStartedGoingDown = false;
    //                                decimal negativeDiff = 0;

    //                                Data.MeterActiveEnergyAnomaly previousRow = new Data.MeterActiveEnergyAnomaly()
    //                                {
    //                                    CompanyID = rep.CompanyID,
    //                                };

    //                                bool bFirst = true;
    //                                int rowIndex = 0;

    //                                foreach (DataRow dr in sortedDT.Rows)
    //                                {
    //                                    if (dr.Table.Columns.Count > 2 && dr[2] != DBNull.Value && !string.IsNullOrEmpty(dr[2].ToString()))
    //                                    {
    //                                        if (bFirst)
    //                                        {
    //                                            bFirst = false;
    //                                        }

    //                                        totalReadingCount++;
    //                                        currentReading = Convert.ToDecimal(dr[2]);
    //                                        totalReading += currentReading.Value;
    //                                        if (previousReading.HasValue)
    //                                        {
    //                                            // calculate diff
    //                                            decimal diff = Convert.ToDecimal(dr[2]) - previousReading.Value;

    //                                            if (diff < 0)
    //                                            {
    //                                                negativeDiffCount++;
    //                                                negativeDiffReading += diff;
    //                                                //lastNegTimeLogged = Convert.ToDateTime(dr[1]);
    //                                                hasStartedGoingDown = true;
    //                                                negativeDiff = diff;
    //                                                previousReading = currentReading;

    //                                            }
    //                                            else if (hasStartedGoingDown)
    //                                            {
    //                                                // went back to positive after negative
    //                                                if (negativeDiffCount > 0)
    //                                                {
    //                                                    #region Add Last 3 Rows

    //                                                    List<Data.MeterActiveEnergyAnomaly> activeEnergyAnomaliesAdded = new List<MeterActiveEnergyAnomaly>();

    //                                                    bool hasFoundErrorRow = false;

    //                                                    decimal? last3PreviousReading = null;

    //                                                    for (int i = 4; i >= -1; i--)
    //                                                    {
    //                                                        int ntwoRowsAgoIndex = sortedDT.Rows.IndexOf(dr) - i;
    //                                                        if (ntwoRowsAgoIndex >= 0)
    //                                                        {
    //                                                            try
    //                                                            {
    //                                                                #region Temp object for previous line

    //                                                                DataRow previousDR = sortedDT.Rows[ntwoRowsAgoIndex];

    //                                                                previousRow.MeterID = m2mDev.id.ToString();
    //                                                                previousRow.Serial = m2mDev.serial;
    //                                                                previousRow.Name = m2mDev.name;
    //                                                                previousRow.TimeLogged = Convert.ToDateTime(previousDR[1]);
    //                                                                previousRow.ActiveEnergyReading = Convert.ToDecimal(previousDR[2]);
    //                                                                previousRow.Diff = last3PreviousReading.HasValue ? (Convert.ToDecimal(previousDR[2]) - last3PreviousReading.Value) : 0;

    //                                                                if (last3PreviousReading.HasValue && last3PreviousReading.Value != 0)
    //                                                                    previousRow.ErrorPerc = (((previousRow.ActiveEnergyReading / (last3PreviousReading.Value)) - 1.0m) + (previousRow.Diff / previousRow.ActiveEnergyReading)) * 100.0m;

    //                                                                if (!hasFoundErrorRow && (previousRow.ErrorPerc > 5.0m || previousRow.ErrorPerc < -5.0m))
    //                                                                {
    //                                                                    hasFoundErrorRow = true;
    //                                                                    previousRow.IsTheProblemRow = true;
    //                                                                }
    //                                                                else
    //                                                                    previousRow.IsTheProblemRow = false;

    //                                                                #endregion

    //                                                                var existingp = (from p in entriesToCheck
    //                                                                                 where p.MeterID == previousRow.MeterID
    //                                                                                 && p.Serial == previousRow.Serial
    //                                                                                 && p.Name == previousRow.Name
    //                                                                                 && p.TimeLogged == previousRow.TimeLogged
    //                                                                                 select p).SingleOrDefault();

    //                                                                if (existingp == null)
    //                                                                {
    //                                                                    db.Add(previousRow);
    //                                                                    db.SaveChanges();
    //                                                                    activeEnergyAnomaliesAdded.Add(previousRow);
    //                                                                }
    //                                                                //bBreak = true;
    //                                                                //Console.Beep();
    //                                                                previousRow = new MeterActiveEnergyAnomaly()
    //                                                                {
    //                                                                    CompanyID = rep.CompanyID,
    //                                                                };
    //                                                                last3PreviousReading = Convert.ToDecimal(previousDR[2]);
    //                                                            }
    //                                                            catch { }
    //                                                            //Console.WriteLine(sbReport.ToString());
    //                                                        }
    //                                                    }

    //                                                    #endregion

    //                                                    //Data.MeterActiveEnergyAnomaly dataRow = new Data.MeterActiveEnergyAnomaly()
    //                                                    //{
    //                                                    //    MeterID = m2mDev.id.ToString(),
    //                                                    //    Serial = m2mDev.serial,
    //                                                    //    Name = m2mDev.name,
    //                                                    //    TimeLogged = Convert.ToDateTime(dr[1]),
    //                                                    //    ActiveEnergyReading = Convert.ToDecimal(dr[2]),
    //                                                    //    Diff = negativeDiff,
    //                                                    //    CompanyID = rep.CompanyID,
    //                                                    //    ErrorPerc = 0,
    //                                                    //};

    //                                                    //if (totalReadingCount > 0)
    //                                                    //    dataRow.ErrorPerc = (negativeDiffCount / totalReadingCount) * 100.0m;

    //                                                    //var existing = (from p in entriesToCheck
    //                                                    //                where p.MeterID == dataRow.MeterID
    //                                                    //                && p.Serial == dataRow.Serial
    //                                                    //                && p.Name == dataRow.Name
    //                                                    //                && p.TimeLogged == dataRow.TimeLogged
    //                                                    //                select p).SingleOrDefault();

    //                                                    //if (existing == null)
    //                                                    //{
    //                                                    //    db.Add(dataRow);
    //                                                    //    db.SaveChanges();
    //                                                    //}

    //                                                    //if (bBreak)
    //                                                    //    break;

    //                                                    previousReading = currentReading;
    //                                                    negativeDiffCount = 0;
    //                                                    //totalReadingCount = 0;
    //                                                    //totalReading = 0;
    //                                                    negativeDiffReading = 0;
    //                                                    //lastNegTimeLogged = new DateTime();
    //                                                    hasStartedGoingDown = false;
    //                                                    negativeDiff = 0;

    //                                                }

    //                                            }

    //                                        }

    //                                        previousReading = currentReading;


    //                                    }
    //                                    rowIndex++;
    //                                }

    //                            }

    //                            current = current.AddDays(1);
    //                        }
    //                    }

    //                    repToUpdate = db.MeterActiveEnergyAnomaliesRuns.Where(p => p.ID == rep.ID).SingleOrDefault();
    //                    repToUpdate.Progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(sbCustomersSerials.Count)) * 100.0m;
    //                    db.Update(repToUpdate);
    //                    db.SaveChanges();

    //                }


    //                repToUpdate = db.MeterActiveEnergyAnomaliesRuns.Where(p => p.ID == rep.ID).SingleOrDefault();
    //                repToUpdate.DateEnded = DateTime.Now;
    //                repToUpdate.Progress = 100;
    //                db.Update(repToUpdate);
    //                db.SaveChanges();

    //                Console.Beep();
    //            }

    //        }
    //    }



    //}

}
