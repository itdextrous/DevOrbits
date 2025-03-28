using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_GatewayReset
    {
        [Key]
        public int ID { get; set; }
        public int GatewayID { get; set; }
        public DateTime DateSent { get; set; }
        public string SMSResponse { get; set; }
        public string MSISDN{ get; set; }
    }
}
