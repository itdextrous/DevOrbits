using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class DeviceType
    {
        public enum DeviceTypeEnum : int
        {
            [Description("Unknown")]
            Unknown = 0,
            [Description("Electricity")]
            Electricity = 1,
            [Description("Water")]
            Water = 2,
            [Description("Valve")]
            Valve = 6,
            [Description("GPS")]
            GPS = 7,
            [Description("Gas")]
            Gas = 8,
            [Description("Gas Modem")]
            Gas_Modem = 20,
        }

        public string GetBGColor(DeviceTypeEnum deviceType)
        {
            switch (deviceType)
            {
                case Data.DeviceType.DeviceTypeEnum.Electricity:
                    return "#A6CE39";
                    break;
                case Data.DeviceType.DeviceTypeEnum.Water:
                    return "#39BEAE";
                    break;
                case Data.DeviceType.DeviceTypeEnum.Gas:
                    return "#ED1A3A";
                    break;
            }

            return "#4E5375";
        }

    }
}
