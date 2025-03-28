using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.S_CombinedReports.S_CombinedReportsModels
{
    public class S_CombinedReports_BillingAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_BillingAnalysis_Amount_SummaryItem> S_CombinedReports_BillingAnalysis_Amount_SummaryItems { get; set; }
        public class S_CombinedReports_BillingAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_BillingAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_BillingAnalysis_Amount_MonthlyItem> S_CombinedReports_BillingAnalysis_Amount_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_BillingAnalysis_Amount_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Amount
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_BillingAnalysis_Consumption_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_BillingAnalysis_Consumption_SummaryItem> S_CombinedReports_BillingAnalysis_Consumption_SummaryItems { get; set; }
        public class S_CombinedReports_BillingAnalysis_Consumption_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem> S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Consumption
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_MeteredAnalysis_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_MeteredAnalysis_Units_SummaryItem> S_CombinedReports_MeteredAnalysis_Units_SummaryItems { get; set; }
        public class S_CombinedReports_MeteredAnalysis_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_MeteredAnalysis_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_MeteredAnalysis_Units_MonthlyItem> S_CombinedReports_MeteredAnalysis_Units_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_MeteredAnalysis_Units_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Consumption
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_UnbilledAnalysis_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_UnbilledAnalysis_Units_SummaryItem> S_CombinedReports_UnbilledAnalysis_Units_SummaryItems { get; set; }
        public class S_CombinedReports_UnbilledAnalysis_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecMeteredUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }
            public decimal ElecUnbilledUnits { get { return ElecMeteredUnits - ElecUnits; } }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterMeteredUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return WaterAmount / WaterUnits;
                    return 0;
                }
            }
            public decimal WaterUnbilledUnits { get { return WaterMeteredUnits - WaterUnits; } }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasMeteredUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return GasAmount / GasUnits;
                    return 0;
                }
            }
            public decimal GasUnbilledUnits { get { return GasMeteredUnits - GasUnits; } }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSMeteredUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return GPSAmount / GPSUnits;
                    return 0;
                }
            }
            public decimal GPSUnbilledUnits { get { return GPSMeteredUnits - GPSUnits; } }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveMeteredUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ValveAmount / ValveUnits;
                    return 0;
                }
            }
            public decimal ValveUnbilledUnits { get { return ValveMeteredUnits - ValveUnits; } }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherMeteredUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return OtherAmount / OtherUnits;
                    return 0;
                }
            }
            public decimal OtherUnbilledUnits { get { return OtherMeteredUnits - OtherUnits; } }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
            public decimal TotalMeteredUnits { get { return ElecMeteredUnits + WaterMeteredUnits + GasMeteredUnits + GPSMeteredUnits + ValveMeteredUnits + OtherMeteredUnits; } }
            public decimal TotalUnbilledUnits { get { return TotalMeteredUnits - TotalUnits; } }
        }

    }

    public class S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem> S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) > 1)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) > 1)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Consumption
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_CostAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_CostAnalysis_Amount_SummaryItem> S_CombinedReports_CostAnalysis_Amount_SummaryItems { get; set; }
        public class S_CombinedReports_CostAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }
            public decimal ElecCost { get; set; }
            public decimal ElecProfit { get { return ElecAmount - ElecCost; } }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return WaterAmount / WaterUnits;
                    return 0;
                }
            }
            public decimal WaterCost { get; set; }
            public decimal WaterProfit { get { return WaterAmount - WaterCost; } }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return GasAmount / GasUnits;
                    return 0;
                }
            }
            public decimal GasCost { get; set; }
            public decimal GasProfit { get { return GasAmount - GasCost; } }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return GPSAmount / GPSUnits;
                    return 0;
                }
            }
            public decimal GPSCost { get; set; }
            public decimal GPSProfit { get { return GPSAmount - GPSCost; } }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ValveAmount / ValveUnits;
                    return 0;
                }
            }
            public decimal ValveCost { get; set; }
            public decimal ValveProfit { get { return ValveAmount - ValveCost; } }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return OtherAmount / OtherUnits;
                    return 0;
                }
            }
            public decimal OtherCost { get; set; }
            public decimal OtherProfit { get { return OtherUnits - OtherCost; } }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
            public decimal TotalCost { get { return ElecCost + WaterCost + GasCost + GPSCost + ValveCost + OtherCost; } }
            public decimal TotalProfit { get { return ElecProfit + WaterProfit + GasProfit + GPSProfit + ValveProfit + OtherProfit; } }
        }

    }

    public class S_CombinedReports_CostAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_CostAnalysis_Amount_MonthlyItem> S_CombinedReports_CostAnalysis_Amount_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_CostAnalysis_Amount_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Amount
            public List<Tuple<DateTime, decimal, string>> MonthlyCostFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyCostFigures != null && MonthlyCostFigures.Count > 0)
                        return MonthlyCostFigures.Select(p => p.Item2).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_ProfitAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_ProfitAnalysis_Amount_SummaryItem> S_CombinedReports_ProfitAnalysis_Amount_SummaryItems { get; set; }
        public class S_CombinedReports_ProfitAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }
            public decimal ElecCost { get; set; }
            public decimal ElecProfit { get { return ElecAmount - ElecCost; } }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return WaterAmount / WaterUnits;
                    return 0;
                }
            }
            public decimal WaterCost { get; set; }
            public decimal WaterProfit { get { return WaterAmount - WaterCost; } }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return GasAmount / GasUnits;
                    return 0;
                }
            }
            public decimal GasCost { get; set; }
            public decimal GasProfit { get { return GasAmount - GasCost; } }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return GPSAmount / GPSUnits;
                    return 0;
                }
            }
            public decimal GPSCost { get; set; }
            public decimal GPSProfit { get { return GPSAmount - GPSCost; } }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ValveAmount / ValveUnits;
                    return 0;
                }
            }
            public decimal ValveCost { get; set; }
            public decimal ValveProfit { get { return ValveAmount - ValveCost; } }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return OtherAmount / OtherUnits;
                    return 0;
                }
            }
            public decimal OtherCost { get; set; }
            public decimal OtherProfit { get { return OtherUnits - OtherCost; } }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
            public decimal TotalCost { get { return ElecCost + WaterCost + GasCost + GPSCost + ValveCost + OtherCost; } }
            public decimal TotalProfit { get { return ElecProfit + WaterProfit + GasProfit + GPSProfit + ValveProfit + OtherProfit; } }
        }

    }

    public class S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem> S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) <= 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) <= 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

        public class S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Amount
            public List<Tuple<DateTime, decimal, string>> MonthlyProfitFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyProfitFigures != null && MonthlyProfitFigures.Count > 0)
                        return MonthlyProfitFigures.Select(p => p.Item2).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem> S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItems { get; set; }
        public class S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem> S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            string cellClass = "class=\"text-nowrap text-right";

            if (isBold)
                cellClass = cellClass + " font-weight-bold";

            if (Convert.ToInt32(value) >= 0
                && Convert.ToInt32(value) < 10)
                cellClass = cellClass + " table-default";
            else if (Convert.ToInt32(value) >= 10
                && Convert.ToInt32(value) < 15)
                cellClass = cellClass + " table-primary";
            else if (Convert.ToInt32(value) >= 15
                && Convert.ToInt32(value) < 20)
                cellClass = cellClass + " table-warning";
            else if (Convert.ToInt32(value) >= 20
                && Convert.ToInt32(value) < 30)
                cellClass = cellClass + " table-success";
            else if (Convert.ToInt32(value) >= 30
                && Convert.ToInt32(value) < 100)
                cellClass = cellClass + " table-danger";

            cellClass = cellClass + "\"";

            return cellClass;
        }

        public class S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, GrossProfitPerc
            public List<Tuple<DateTime, decimal, string>> MonthlyProfitFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyProfitFigures != null && MonthlyProfitFigures.Count > 0)
                        return MonthlyProfitFigures.Select(p => p.Item2).Sum();

                    return 0;
                }
            }
        }

    }

    public class S_CombinedReports_OverallAnalysis_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_OverallAnalysis_SummaryItem> S_CombinedReports_OverallAnalysis_SummaryItems { get; set; }
        public class S_CombinedReports_OverallAnalysis_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_OverallAnalysis_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_OverallAnalysis_MonthlyItem> S_CombinedReports_OverallAnalysis_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            string cellClass = "class=\"text-nowrap text-right";

            if (isBold)
                cellClass = cellClass + " font-weight-bold";

            if (Convert.ToInt32(value) >= 0
                && Convert.ToInt32(value) < 10)
                cellClass = cellClass + " table-default";
            else if (Convert.ToInt32(value) >= 10
                && Convert.ToInt32(value) < 15)
                cellClass = cellClass + " table-primary";
            else if (Convert.ToInt32(value) >= 15
                && Convert.ToInt32(value) < 20)
                cellClass = cellClass + " table-warning";
            else if (Convert.ToInt32(value) >= 20
                && Convert.ToInt32(value) < 30)
                cellClass = cellClass + " table-success";
            else if (Convert.ToInt32(value) >= 30
                && Convert.ToInt32(value) < 100)
                cellClass = cellClass + " table-danger";

            cellClass = cellClass + "\"";

            return cellClass;
        }

        public class S_CombinedReports_OverallAnalysis_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            public List<S_CombinedReports_OverallAnalysis_MonthlyItem_SubItem> S_CombinedReports_OverallAnalysis_MonthlyItem_SubItems { get; set; }

            public class S_CombinedReports_OverallAnalysis_MonthlyItem_SubItem
            {
                public DateTime BillingMonth { get; set; }
                public decimal FirstReading { get; set; }
                public decimal LastReading { get; set; }
                // Consumption - Metered
                public decimal MeteredUnits { get { return LastReading - FirstReading; } }

                // Readings - First
                public decimal BilledFirstReading { get; set; }
                // Readings - Last
                public decimal BilledLastReading { get; set; }
                public decimal BilledUnits { get { return BilledLastReading - BilledFirstReading; } }
                // Consumption - Billed
                public decimal BilledActualUnits { get; set; }
                // Charges - Billed
                public decimal BilledAmount { get; set; }
                // Consumption - Unbilled
                public decimal UnbilledUnits { get { return MeteredUnits - BilledUnits; } }

                public decimal CostPerUnit { get; set; }
                // Charges - Cost
                public decimal Cost { get { return BilledUnits * CostPerUnit; } }

                // Profitability - Profit
                public decimal Profit { get { return BilledAmount - Cost; } }
                // Profitability - Margin
                public decimal GrossProfitPerc
                {
                    get
                    {
                        if (BilledAmount > 0)
                            return (Profit / BilledAmount) * 100.0m;
                        return 0;
                    }
                }

                public decimal AgreedMonthlyRental { get; set; }

                public decimal NettProfit { get { return Profit - AgreedMonthlyRental; } }
            }

        }

    }

    public class S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem> S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItems { get; set; }
        public class S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem> S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems { get; set; }
        public List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem> S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems_SUP { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                //if (Convert.ToInt32(value) == 0)
                //    return "font-weight-bold text-right table-danger";
                //else
                return "font-weight-bold text-right";
            }
            else
            {
                //if (Convert.ToInt32(value) == 0)
                //    return "text-right table-danger";
                //else
                return "text-right";
            }
        }

        public class S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Consumption
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

}
