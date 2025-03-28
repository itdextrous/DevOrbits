using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.MyVoltage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MyVoltage.Api.Interfaces
{
    public interface IDeviceApi
    {
        T Get<T>(string url, int deviceApiID, int callCount = 0);
        string GetString(string url, int deviceApiID, int callCount = 0);
        T Post<T, Y>(string url, int deviceApiID, Y obj);
        void Authenticate(int deviceApiID);
        Device GetDeviceByMeterNumber(string meterNumber,int deviceApiID = 2);
        Device GetDeviceByReference(string meterNumber,int deviceApiID = 2);
        Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> GetMeterUsage(int deviceId, DateTime fromDate, DateTime toDate, int interval, Boolean isBalance, bool isSolar, bool maxDemand = false,int deviceApiID = 2);
        //Register[] GetMeterUsage(string meterNumber, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers,int deviceApiID = 2);
        Register[] GetMeterUsage(int deviceIDLinked, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers,int deviceApiID = 2);
        List<Register> GetMeterRegistersFromCache(int deviceIDLinked, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers,int deviceApiID = 2);
        NewGatewayResult GetRegisters(string meterNumber,int deviceApiID = 2);
        GetDeviceByIDResult GetDeviceByID(int deviceIDLinked,int deviceApiID = 2);
        Tuple<Decimal[], Decimal[]> GetMeterUsage(string meterNumber, int interval,int deviceApiID = 2);
        GatewayDevice[] GetGateways(int deviceApiID = 2);
        GatewayDevice GetGateway(string gatewayId,int deviceApiID = 2);
        GatewayDevice[] GetGatewayDevices(string gatewayId,int deviceApiID = 2);
        Boolean isDemandMeter(int deviceId,int deviceApiID = 2);
        Device[] GetAllDevices(int deviceApiID = 2);
        string DeleteMeter(string gateway, string deviceId,int deviceApiID = 2);
        string DeleteMeter(string deviceId,int deviceApiID = 2);
        Boolean ConnectMeter(int action, string deviceId, int control, string source, decimal? balance, string contactorState, string meterStatus, DateTime? meterStatusTime, bool isRetry = false,int deviceApiID = 2);
        Boolean MeterSTS(String sts, string deviceId, string source, decimal? balance, string contactorState, string meterStatus, DateTime? meterStatusTime, string remainingCredit = "", bool isRetry = false,int deviceApiID = 2);
        DeviceGatewaysAndMapping GetDeviceGatewaysAndMapping(int deviceID,int deviceApiID = 2);
        bool IsDeviceContactorConnected(int deviceIDLinked,int deviceApiID = 2);
        CreateOrUpdateM2MDeviceResult CreateDevice(int gatewayID, CreateOrUpdateM2MDevice m2MDevice,int deviceApiID = 2);
        object CreateDeviceConfig(int deviceID, CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig,int deviceApiID = 2);
        Tuple<decimal?, string> GetDeviceReading(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType, DateTime? startTimeOverride = null, DateTime? endTimeOverride = null, int? registerOverride = null,int deviceApiID = 2);
        Tuple<DateTime?, string> GetDeviceLatestReading(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType,int deviceApiID = 2);
        decimal? GetDeviceLatestReadingOnly(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType, DateTime? startTimeOverride = null, DateTime? endTimeOverride = null,int deviceApiID = 2);
        string GetRemainingCredit(string serial,int deviceApiID = 2);
        string GetConfigValue(int id,int deviceApiID = 2);
        Api2RegistersReadings GetApi2RegistersReadings(int deviceID, DateTime startDate, DateTime endDate, int interval,int deviceApiID = 2);
        DataTable GetApi2RegistersReadingsDataTable(int deviceID, DateTime fromDate, DateTime toDate, int interval, bool isSolarFeedback = false);
        DevicesResultApi2.Device[] GetAllDevicesApi2(int deviceApiID = 2);
        string GetApi2RegistersReadingsCSV(string serial, DateTime fromDate, DateTime toDate, int interval, string selectedType = "diff");
    }
}
