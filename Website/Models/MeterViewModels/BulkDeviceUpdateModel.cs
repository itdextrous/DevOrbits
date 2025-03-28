using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class BulkDeviceUpdateModel
    {
        public string DeviceIDs { get; set; }
        public string NewDeviceName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
