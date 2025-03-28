using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_UserActivity
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int SecureAreaID { get; set; }
        public int SecureAreaActionID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
    }
}
