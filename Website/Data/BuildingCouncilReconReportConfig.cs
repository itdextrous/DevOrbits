using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilReconReportConfig
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string ToSendTo { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime? DateStarted { get; set; }
        public string RequestedByID { get; set; }
        public string ReportURL { get; set; }
        public DateTime? DateCompleted { get; set; }
    }
}
