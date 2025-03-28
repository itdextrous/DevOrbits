using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class MeterConnectionStatusChange
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public string MeterSerial { get; set; }
        public string RequestedAction { get; set; }
        public DateTime DateRequested { get; set; }
        public bool? Approved { get; set; }
        public DateTime? DateApproved { get; set; }
    }
}
