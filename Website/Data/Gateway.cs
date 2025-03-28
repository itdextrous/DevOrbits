using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Gateway
    {
        [Key]
        public int ID { get; set; }
        public int GatewayID { get; set; }
        public DateTime DateFirstSynced { get; set; }
        public DateTime DateLastSynced { get; set; }
        public int ActiveStatusID { get; set; }
        public bool IsOnline { get; set; }
        public string Name { get; set; }
        public string HardwareType { get; set; }
        public int? HardwareID { get; set; }
        public DateTime? Since { get; set; }
        public string SimCardNumber { get; set; }
        public int? FirmwareVersion { get; set; }
        public string Network { get; set; }
        public string GSMSerial { get; set; }
        public string GISLocation { get; set; }
        public int? Signal { get; set; }
        public int? OnlineMeters { get; set; }
        public int? OfflineMeters { get; set; }
        public int? CompanyID { get; set; }
        public string InstalledInMeterSerial { get; set; }
        public decimal? HardwareCostEx { get; set; }
        public decimal? LabourAndConsumablesCostEx { get; set; }
        public decimal? AntennaCostEx { get; set; }
        public decimal? SundyCostEx { get; set; }
        public string CostReferenceNo { get; set; }
        public decimal? MonthlyRentalFeeEx { get; set; }
        public string Notes1 { get; set; }
        public string Notes2 { get; set; }
        public string Notes3 { get; set; }
        public decimal? PreparationCost { get; set; }
        public decimal? StandardMonthlyRentalFeeEx { get; set; }
        public decimal? AgreedMonthlyRentalFeeEx { get; set; }
        public string ContractReferenceNo { get; set; }
        public string Manufacturer { get; set; }
        public DateTime? Installation_Date { get; set; }
        public string Owner { get; set; }
        public bool? IsContactorInstalled { get; set; }
        public int? DeviceAPIID { get; set; }
        public int DeviceAPIIDValue { get { return DeviceAPIID.HasValue ? DeviceAPIID.Value : 1; } }
        public int? SiteID { get; set; }
        public string SimCardNumberOverride { get; set; }
    }
}
