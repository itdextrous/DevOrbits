using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_DevicesSkybillBillingSyncCompany
    {
        [Key]
        public int ID { get; set; }
        public int Log_DevicesSkybillBillingSyncID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public string CompanyName { get; set; }
    }
}
