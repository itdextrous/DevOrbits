using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class ConnectionRun
    {
        public int ID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
    }
    public class ConnectionRun_Customer
    {
        public int ID { get; set; }
        public int ConnectionRunID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerNumber { get; set; }
        public string CustomerName { get; set; }
        public string MeterNumber { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public decimal Balance { get; set; }
        public decimal? Low1 { get; set; }
        public decimal? Low2 { get; set; }
        public string Action { get; set; }
        public DateTime ActionDate { get; set; }
        public bool AutoDisconnect { get; set; }
        public bool? IsContactorConnected { get; set; }
        public DateTime? ContactorTimeLogged { get; set; }
    }
}
