using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class UsageCalc
    {
        [Key]
        public int UsageCalcID { get; set; }
        public string UserID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? HighUsageToolID { get; set; }
        public string MeterSerial{ get; set; }
        public int CompanyID{get;set; }
        public string UpdatedByID{ get; set; }
    }
    public class UsageCalc_LinkedAsset
    {
        [Key]
        public int UsageCalc_LinkedAssetID { get; set; }
        public int UsageCalcID { get; set; }
        public int UsageCalc_AssetID { get; set; }
        public int Quantity { get; set; }
        public int HoursRunningPerDay { get; set; }
        public decimal UnitForFirstOfMonth { get; set; }
        public decimal UnitForMiddleOfMonth { get; set; }
        public decimal UnitForLastOfMonth { get; set; }
    }

    public class UsageCalc_Asset
    {
        [Key]
        public int UsageCalc_AssetID { get; set; }
        public string AssetIcon { get; set; }
        public string AssetName { get; set; }
        public decimal AverageKWH { get; set; }
        public int AssetTypeID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }

}
