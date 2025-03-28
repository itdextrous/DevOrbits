using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Notification
    {
        public int NotificationID { get; set; }
        public string DeviceID { get; set; }
        public int DeviceBalance { get; set; }
        public DateTime Date { get; set; }
        public Boolean Sent { get; set; }
    }
}
