using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class MeterActiveEnergyAnomaly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string MeterID { get; set; }
        public string Serial { get; set; }
        public string Name { get; set; }
        public DateTime TimeLogged { get; set; }
        public decimal ActiveEnergyReading { get; set; }
        public decimal Diff { get; set; }
        public decimal ErrorPerc { get; set; }
        public bool? IsTheProblemRow { get; set; }
        public string ErrorID { get; set; }
    }

    public class MeterActiveEnergyAnomaliesRun
    {
        [Key]
        public int ID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal Progress { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
