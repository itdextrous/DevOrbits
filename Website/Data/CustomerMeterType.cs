using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class CustomerMeterType
    {
        public int CustomerMeterTypeID { get; set; }
        public int CustomerMeterID { get; set; }
        public int MeterTypeID { get; set; }
        public int Selected { get; set; }
    }
}
