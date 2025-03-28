using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class NotificationCustomerMeter
    {
        public int NotificationCustomerMeterID { get; set; }
        public int CustomerID { get; set; }
        public string MeterSerial { get; set; }
        public Boolean LowBalanceNotification1 { get; set; }
        public Boolean LowBalanceNotification2 { get; set; }
        public Boolean DisconnectNotification { get; set; }
        public int AccountType { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Reading { get; set; }
        public bool AutoDisconnect { get; set; }
        public DateTime? SwitchBackToAutoDate { get; set; }
    }

    public enum NotificationCustomerMeterEnum : int
    {
        LowBalanceNotification1 = 1,
        LowBalanceNotification2 = 2,
        DisconnectNotification = 3
    }

}
