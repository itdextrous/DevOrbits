using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_DevicesSkybillBillingSync
    {
        [Key]
        public int ID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
    }
}
