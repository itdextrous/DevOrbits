using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models
{
    public class MeterType
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type
        {
            get
            {
                if (id == (Int32)DeviceTypeOptions.WirelessPulseInput || id == (Int32)DeviceTypeOptions.WaterMeter)
                {
                    return "water";
                }
                else if (id == (Int32)DeviceTypeOptions.ControlValve)
                {
                    return "control valve";
                }
                else if (id == (Int32)DeviceTypeOptions.GasMeter)
                {
                    return "gas";
                }
                else if (id == (Int32)DeviceTypeOptions.GasModem)
                {
                    return "gas";
                }

                return "elec";
            }
        }

        public string colorType
        {
            get
            {
                if (id == (Int32)DeviceTypeOptions.WirelessPulseInput || id == (Int32)DeviceTypeOptions.WaterMeter)
                {
                    return "blue";
                }
                else if (id == (Int32)DeviceTypeOptions.GasMeter)
                {
                    return "grey";
                }
                else if (id == (Int32)DeviceTypeOptions.GasModem)
                {
                    return "grey";
                }
                else if (id == (Int32)DeviceTypeOptions.ControlValve)
                {
                    return "";
                }

                return "orange";
            }
        }

        public string UnitType
        {
            get
            {
                if (id == (Int32)DeviceTypeOptions.WirelessPulseInput || id == (Int32)DeviceTypeOptions.WaterMeter)
                {
                    return "litres";
                }
                else if (id == (Int32)DeviceTypeOptions.GasMeter)
                {
                    return "m3";
                }
                else if (id == (Int32)DeviceTypeOptions.GasModem)
                {
                    return "m3";
                }
                else if (id == (Int32)DeviceTypeOptions.ControlValve)
                {
                    return "";
                }

                return "units";
            }
        }
    }

    public enum DeviceTypeOptions
    {
        ElectricityMeter = 1,
        WaterMeter = 2,
        GasMeter = 8,
        WirelessPulseInput = 31,
        Hexing = 16,
        ModbusRTU = 22,
        ControlValve = 6,
        GasModem = 20,
    }
}
