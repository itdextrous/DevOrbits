using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class DeviceBillingDaily_Item
    {
        [Key]
        public int ID { get; set; }
        public long DeviceID { get; set; }
        public DateTime Date { get; set; }
        public decimal Units { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public decimal? Reading { get; set; }
    }
}
