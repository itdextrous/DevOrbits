using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_DevicesSkybillBillingSyncCompanyDevice
    {
        public int ID { get; set; }
        public int Log_DevicesSkybillBillingSyncCompanyID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public string Result { get; set; }
        public long? LocalDeviceID { get; set; }
        public DateTime BillingDate { get; set; }
        public int? DeviceBillingDailyID { get; set; }
    }
}
