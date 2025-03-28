using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.Customer_UsageCalculatorModels
{
    public class UsageCalcMeterItem
    {
        public string Serial { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string MeterColor { get; set; }
    }

    public class UsageCalcLinkedAssetItem : Data.UsageCalc_LinkedAsset
    {
        public Data.UsageCalc_Asset UsageCalc_Asset { get; set; }
        public decimal TotalConsumption { get { return Quantity * HoursRunningPerDay * UsageCalc_Asset.AverageKWH; } }
        public decimal FirstOfTheMonth { get { return UnitForFirstOfMonth * Quantity * HoursRunningPerDay * UsageCalc_Asset.AverageKWH; } }
        public decimal MiddleOfTheMonth { get { return UnitForMiddleOfMonth * Quantity * HoursRunningPerDay * UsageCalc_Asset.AverageKWH; } }
        public decimal LastOfTheMonth { get { return UnitForLastOfMonth * Quantity * HoursRunningPerDay * UsageCalc_Asset.AverageKWH; } }
    }

    public class UsageCalcViewModel
    {
        public List<UsageCalcMeterItem> Meters { get; set; }
        public List<Data.UsageCalc> UsageCalcs { get; set; }
    }
    public class UsageCalcCreateModel
    {
        public UsageCalcMeterItem Meter { get; set; }
        public List<Data.UsageCalc_Asset> UsageCalc_Assets { get; set; }
    }
    public class UsageCalcEditModel
    {
        public UsageCalcMeterItem Meter { get; set; }
        public Data.UsageCalc UsageCalc { get; set; }
        public DateTime MonthUsedForTariff { get; set; }
        public List<UsageCalcLinkedAssetItem> UsageCalc_LinkedAssets { get; set; }
        public decimal UnitForFirstOfTheMonth { get; set; }
        public decimal UnitForMiddleOfTheMonth { get; set; }
        public decimal UnitForLastOfTheMonth { get; set; }
        public decimal FirstOfTheMonth { get { return UsageCalc_LinkedAssets == null ? 0 : UsageCalc_LinkedAssets.Select(p => p.FirstOfTheMonth).Sum(); } }
        public decimal MiddleOfTheMonth { get { return UsageCalc_LinkedAssets == null ? 0 : UsageCalc_LinkedAssets.Select(p => p.MiddleOfTheMonth).Sum(); } }
        public decimal LastOfTheMonth { get { return UsageCalc_LinkedAssets == null ? 0 : UsageCalc_LinkedAssets.Select(p => p.LastOfTheMonth).Sum(); } }
        public decimal TotalConsumptionPerDay { get { return UsageCalc_LinkedAssets == null ? 0 : UsageCalc_LinkedAssets.Select(p => p.TotalConsumption).Sum(); } }
        public decimal TotalConsumptionPerMonth { get { return UsageCalc_LinkedAssets == null ? 0 : UsageCalc_LinkedAssets.Select(p => p.TotalConsumption).Sum() * DateTime.DaysInMonth(MonthUsedForTariff.Year, MonthUsedForTariff.Month); } }
        public decimal ActualTotalConsumptionPerMonth { get; set; }
        public decimal ActualTotalConsumptionPerDay { get { return ActualTotalConsumptionPerMonth / DateTime.DaysInMonth(MonthUsedForTariff.Year, MonthUsedForTariff.Month); } }
        public decimal ConsumptionDifferencePerDay { get { return ActualTotalConsumptionPerDay - TotalConsumptionPerDay; } }
        public decimal ConsumptionDifferencePerMonth { get { return ActualTotalConsumptionPerMonth - TotalConsumptionPerMonth; } }
    }
}
