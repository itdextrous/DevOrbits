using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SOC_Snapshot
    {
        [Key]
        public int ID { get; set; }
        public DateTime DateUpdated { get; set; }
        public DateTime SnapshotDate { get; set; }
        public int CompanyID { get; set; }
        public int CustomerCount { get; set; }
        public int GatewaysCount { get; set; }
        public int DevicesCount { get; set; }
        public decimal AvgTurnover { get; set; }
        public decimal AvgGrossProfit { get; set; }
    }

    public class SOC_SnapshotItem
    {
        [Key]
        public int ID { get; set; }
        public int SnapshotID { get; set; }
        public DateTime DateUpdated { get; set; }
        public string ItemCode { get; set; }
        public bool DidPass { get; set; }
        public int SecureAreaID { get; set; }
        public string ProblemChild { get; set; }
    }
}
