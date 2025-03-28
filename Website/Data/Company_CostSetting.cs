using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Company_CostSetting
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public decimal DefaultCostPerUnitElec { get; set; }
        public decimal DefaultCostPerUnitWater { get; set; }
        public decimal DefaultCostPerUnitGas { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class Company_CostSetting_Item
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string SerialNo { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal Units { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime BillingMonth { get; set; }
        public int? ProductID { get; set; }
    }

    public class Company_CostSetting_Monthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int DeviceTypeID { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal Units { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime BillingMonth { get; set; }
        public int? ProductID { get; set; }
        public int? SkybillJournalLogID { get; set; }
        public string SkybillDocumentNo { get; set; }
        public int? BuildingCouncilDetailID { get; set; }

        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                return ((DeviceType.DeviceTypeEnum)DeviceTypeID);
            }
        }
    }

    public class Company_CostSettings_Template
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int DeviceTypeID { get; set; }
        public int ProductID { get; set; }
        public string Tarrif_Resource_No { get; set; }
        public int CalculationID { get; set; }
        public DateTime Month { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string MeterSerial { get; set; }
        public decimal? Units { get; set; }
        public decimal? RatePerUnit { get; set; }
        public int? BuildingCouncilDetailID { get; set; }
        public int? SkybillJournalLogID { get; set; }
        public string SkybillDocumentNo { get; set; }
        public string LinkedTarrif_Resource_No { get; set; }

        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                return ((DeviceType.DeviceTypeEnum)DeviceTypeID);
            }
        }
        public CalculationTypeEnum CalculationType
        {
            get
            {
                return ((CalculationTypeEnum)CalculationID);
            }
        }

        public enum CalculationTypeEnum
        {
            [Description("Fixed - Single Charge")]
            Fixed_SingleUnitCharge = 1,
            [Description("Fixed - Per Unit Charge")]
            Fixed_PerUnitCharge = 3,
            [Description("Calculated")]
            Calculated = 2,
            [Description("Metered")]
            Metered = 4,
            [Description("Manual")]
            Manual = 5,
            [Description("Max Demand")]
            MaxDemand = 6,
            [Description("Meter Rental")]
            MeterRental = 7,
            [Description("Tariff Revenue")]
            TariffRevenue = 8,
            [Description("Linked Calculated")]
            LinkedCalculated = 9,
        }
    }

    public class C08_Forecasting_CostSettings_Template
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int DeviceTypeID { get; set; }
        public int ProductID { get; set; }
        public string Tarrif_Resource_No { get; set; }
        public int CalculationID { get; set; }
        public DateTime Month { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string MeterSerial { get; set; }
        public decimal? Units { get; set; }
        public decimal? RatePerUnit { get; set; }
        public int? BuildingCouncilDetailID { get; set; }
        public int? SkybillJournalLogID { get; set; }
        public string SkybillDocumentNo { get; set; }
        public int? ReportingCategoryID { get; set; }
        public string LinkedTarrif_Resource_No { get; set; }
        public int? ConsumptionTypeID { get; set; }

        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                return ((DeviceType.DeviceTypeEnum)DeviceTypeID);
            }
        }
        public CalculationTypeEnum CalculationType
        {
            get
            {
                return ((CalculationTypeEnum)CalculationID);
            }
        }

        public enum CalculationTypeEnum
        {
            [Description("Fixed - Single Charge")]
            Fixed_SingleUnitCharge = 1,
            [Description("Fixed - Per Unit Charge")]
            Fixed_PerUnitCharge = 3,
            [Description("Calculated")]
            Calculated = 2,
            [Description("Metered")]
            Metered = 4,
            [Description("Manual")]
            Manual = 5,
            [Description("Max Demand")]
            MaxDemand = 6,
            [Description("Meter Rental")]
            MeterRental = 7,
            [Description("Tariff Revenue")]
            TariffRevenue = 8,
            [Description("Linked Calculated")]
            LinkedCalculated = 9,
        }

        public enum ConsumptionTypeEnum
        {
            [Description("Fixed")]
            Fixed = 1,
            [Description("Consumption")]
            Consumption = 2,
        }
    }

    public class C08_Forecasting_Baseline
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int DeviceTypeID { get; set; }
        public DateTime Month { get; set; }
        public decimal? ActualUnits { get; set; }
        public DateTime? DateActualSynced { get; set; }
        public decimal ForecastPerc { get; set; }
        public decimal? ForecastUnits { get; set; }
        public decimal? ForecastBaseline { get; set; }
        public DateTime? DateForecastSynced { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string UpdatedBy { get; set; }

        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                return ((DeviceType.DeviceTypeEnum)DeviceTypeID);
            }
        }
    }
}
