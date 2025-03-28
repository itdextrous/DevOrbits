using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_BillingControlReport_OccupancyVerification
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public DateTime CreateDate { get; set; }
        public string Occupancy{ get; set; }
    }
}
