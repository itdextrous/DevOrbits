using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A03_NetworkBalancing_Capture
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public DateTime ReportMonth { get; set; }
        public int DeviceTypeID { get; set; }
        public int ReportTypeID { get; set; }
        public string ReportURL { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class A03_NetworkBalancing_Capture_ReportType
    {
        [Key]
        public int ReportTypeID { get; set; }
        public string ReportTypeName { get; set; }
    }
}
