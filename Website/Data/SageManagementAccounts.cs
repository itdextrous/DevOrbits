using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class SageManagementAccounts_ReportingParentDescription
    {
        [Key]
        public int ID { get; set; }
        public string ReportingParentDescription { get; set; }
        public string ChartType { get; set; }
        public string ChartColor { get; set; }
        public int ReportingCategoryID { get; set; }
    }
}
