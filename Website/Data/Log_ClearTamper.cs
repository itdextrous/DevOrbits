using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_ClearTamper
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public string Serial { get; set; }
        public string OTP { get; set; }
        public bool HasBeenClaimed { get; set; }
        public DateTime DateRequested { get; set; }
        public string Token { get; set; }
    }
}
