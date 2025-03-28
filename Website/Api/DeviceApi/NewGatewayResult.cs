using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class NewGatewayResult
    {
        public NewGatewayDevice device { get; set; }
        public NewGatewayDeviceMeta meta { get; set; }
    }

    public class NewGatewayDeviceMeta
    {
        public int count { get; set; }
        public int total { get; set; }
    }

    public class NewGatewayDevice
    {
        public int id { get; set; }
        public string name { get; set; }
        public string serial { get; set; }
        public NewGatewayDeviceType type { get; set; }
        public NewGatewayDeviceStatus status { get; set; }
        public NewGatewayDeviceRegister[] registers { get; set; }
        public string deviceType
        {
            get
            {
                if (type != null)
                {
                    if (type.id == (Int32)NewGatewayDeviceTypeOptions.WirelessPulseInput || type.id == (Int32)NewGatewayDeviceTypeOptions.WaterMeter)
                    {
                        return "water";
                    }
                    else if (type.id == (Int32)NewGatewayDeviceTypeOptions.ControlValve)
                    {
                        return "control valve";
                    }
                    else if (type.id == (Int32)NewGatewayDeviceTypeOptions.GasMeter)
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
                        s = status.time.Substring(0, 16).Replace("-", "/").Replace("T", " ");
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
    }

    public class NewGatewayDeviceRegister
    {
        public int id { get; set; }
        public string name { get; set; }
        public int number { get; set; }
        public string unit { get; set; }
        public GatewayDeviceRegisterReading reading { get; set; }
    }

    public class GatewayDeviceRegisterReading
    {
        public decimal? value { get; set; }
        public string time { get; set; }
    }

    public class NewGatewayDeviceType
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class NewGatewayDeviceStatus
    {
        public int id { get; set; }
        public string time { get; set; }
    }

    public enum NewGatewayDeviceTypeOptions
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
