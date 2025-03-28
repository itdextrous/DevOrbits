using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_TokenGeneration
    {
        [Key]
        public int ID { get; set; }
        public string SerialNo { get; set; }
        public string Type { get; set; }
        public string SGC { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime DateCompleted { get; set; }
        public string URL { get; set; }
        public string Token { get; set; }
        public string Source { get; set; }
        public decimal? Balance { get; set; }
        public string ContactorState { get; set; }
        public string UserID { get; set; }
    }
}
