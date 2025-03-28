using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class GatewayResult
    {
        public GatewayDevice[] gateways { get; set; }
        public GatewayDevice gateway { get; set; }
        public GatewayDeviceMeta meta { get; set; }
    }

    public class GatewayDevicesResult
    {
        public GatewayDevice[] devices { get; set; }
    }

    public class GatewayDeviceResult
    {
        public GatewayDevice gateway { get; set; }
    }


    public class GatewayDeviceMeta
    {
        public int count { get; set; }
        public int total { get; set; }
    }

    public class GatewayDevice
    {
        public int id { get; set; }
        public string name { get; set; }
        public string serial { get; set; }
        public GatewayDeviceType type { get; set; }
        public GatewayDeviceStatus status { get; set; }
        public GatewayDevice[] devices { get; set; }
        public GatewayDeviceMapping mapping { get; set; }
        public NewRegisterViewModel register { get; set; }
        public GatewayNetwork network { get; set; }
        public bool autoDisconnect { get; set; }
        public int gatewayID { get; set; }
        public string deviceType
        {
            get
            {
                if (type != null)
                {
                    if (type.id == (Int32)DeviceTypeOptions.WirelessPulseInput || type.id == (Int32)DeviceTypeOptions.WaterMeter)
                    {
                        return "water";
                    }
                    else if (type.id == (Int32)DeviceTypeOptions.ControlValve)
                    {
                        return "control valve";
                    }
                    else if (type.id == (Int32)DeviceTypeOptions.GasMeter)
                    {
                        return "gas";
                    }

                    return "elec";
                }

                return "";
            }
        }
        public string deviceStatus
        {
            get
            {
                if (status != null)
                {
                    if (status.id == 1)
                    {
                        return "online";
                    }
                    else if (status.id == 2)
                    {
                        return "offline";
                    }
                    return "";
                }
                return "";
            }
        }
        public string since
        {
            get
            {
                var s = "";
                if (status != null)
                {
                    if (status.time.Length >= 16)
                    {
                        string format = "yyyy-MM-dd HH:mm";
                        return DateTimeOffset.Parse(status.time, CultureInfo.InvariantCulture).AddHours(2).ToString(format);
                    }
                }
                return s;
            }
        }
        public string deviceTypeName
        {
            get
            {
                if (type != null)
                {
                    return type.name;
                }
                return "";                
            }
        }
        public int onlineDevices
        {
            get
            {
                int count = 0;
                if (devices != null)
                {
                    foreach (GatewayDevice d in devices)
                    {
                        if (d.deviceStatus == "online")
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }
        public int offlineDevices
        {
            get
            {
                int count = 0;
                if (devices != null)
                {
                    foreach (GatewayDevice d in devices)
                    {
                        if (d.deviceStatus == "offline")
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }
    }

    public class GatewayDeviceType
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class GatewayDeviceStatus
    {
        public int id { get; set; }
        public string time { get; set; }
    }

    public class GatewayDeviceMapping
    {
        public int index { get; set; }
        public int port { get; set; }
        public int protocol { get; set; }
        public int remote_address { get; set; }
        public int remote_index { get; set; }
        public int process_interval { get; set; }
        public int status { get; set; }
        public string last_communicated
        {
            get;set;
        }

        public string lastCommunicated
        {
            get
            {
                if (last_communicated != null && last_communicated != String.Empty)
                {
                    string format = "yyyy-MM-dd HH:mm";

                    DateTimeOffset offset;

                    if (DateTimeOffset.TryParse(last_communicated, out offset))
                    {
                        string result = DateTimeOffset.Parse(last_communicated, CultureInfo.InvariantCulture).AddHours(2).ToString(format);
                        return result;
                    }
                    else
                    {
                        return "2000-01-01 00:00";
                    }
                }

                return last_communicated;
            }
        }

    }

    public class GatewayNetwork
    {
        public string start { get; set; }
        public string end { get; set; }
        public string duration { get; set; }
        public string ccid { get; set; }
        public string network { get; set; }
        public string msisdn { get; set; }
        public string ip { get; set; }
        public string apn { get; set; }
        public string csq { get; set; }
        public string lac { get; set; }
        public string cid { get; set; }
        public string version { get; set; }
        public string version_date { get; set; }
    }

    public enum GatewayDeviceTypeOptions
    {
        ElectricityMeter = 1,
        WaterMeter = 2,
        GasMeter = 8,
        WirelessPulseInput = 31,
        Hexing = 16,
        ModbusRTU = 22,
        ControlValve = 6
    }

}
