using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MyVoltage.Data
{
    public class Log_ProfileChange
    {
        [Key]
        public int ID { get; set; }

        public string ChangedByID { get; set; }

        public DateTime ChangedDate { get; set; }

        public string ObjectBefore { get; set; }

        public string ObjectAfter { get; set; }
    }

    public class Log_ProfileChangeObject
    {
        public Customer Customer { get; set; }
        public List<NotificationCustomerMeter> NotificationCustomerMeters { get; set; }
        public List<CustomerMeterType> CustomerMeterTypes { get; set; }
    }
}
