using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models
{
    public class BuildingCouncilReconReportModel
    {
        public int CompanyID { get; set; }
        public string ToSendTo { get; set; }
        public string ErrorMessage { get; set; }
        public List<MyVoltage.Data.Company> Companies { get; set; }
    }
}
