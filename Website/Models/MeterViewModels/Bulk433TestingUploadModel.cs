using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class Bulk433TestingUploadModel
    {
        public string SerialNos { get; set; }
        public string NewDeviceName { get; set; }
        public string ErrorMessage { get; set; }
        public bool DoSecondInput { get; set; }
    }
}
