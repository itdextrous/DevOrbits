using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_Notification
    {
        [Key]
        public int ID { get; set; }

        public System.DateTime TimeSent { get; set; }

        public string Recipients { get; set; }

        public string MessagePreview { get; set; }
        public int CompanyID { get; set; }
        public int CustomerID { get; set; }
    }
}
