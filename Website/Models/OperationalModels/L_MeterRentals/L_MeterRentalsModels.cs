using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.L_MeterRentals.L_MeterRentalsModels
{
    public class L_MeterRentals_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_SummaryItem> L_MeterRentals_SummaryItems { get; set; }

        public class L_MeterRentals_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<DateTime, decimal> MonthlyRentalFigures { get; set; }

        }
    }
    public class L_MeterRentals_ResultsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_ResultsItem> L_MeterRentals_ResultsItems { get; set; }

        public class L_MeterRentals_ResultsItem
        {
            public Data.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Dictionary<DateTime, decimal> MonthlyRentalFigures { get; set; }
            public string PhotoURL { get; set; }
        }

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
    }
    public class L_MeterRentals_Cost_SummaryModel
    {
        public List<L_MeterRentals_Cost_SummaryItem> L_MeterRentals_Cost_SummaryItems { get; set; }

        public class L_MeterRentals_Cost_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public decimal GatewayCost { get; set; }
            public decimal GatewayLabourAndConsumablesCost { get; set; }
            public decimal GatewayPreparationCost { get; set; }
            public decimal GatewayAntennaCost { get; set; }
            public decimal GatewaySundyCost { get; set; }
            public decimal DeviceCost { get; set; }
            public decimal DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal DevicePreparationCost { get; set; }
            public decimal DeviceAntennaCost { get; set; }
            public decimal DeviceCTsCost { get; set; }
            public decimal RTUCost { get; set; }
            public decimal RTUProbeCost { get; set; }
            public decimal RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal RTUPreparationCost { get; set; }
            public decimal RTUAntennaCost { get; set; }
            public decimal ControlUnitCost { get; set; }
            public decimal ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal ControlUnitPreparationCost { get; set; }
            public decimal AntennaCost { get; set; }
            public decimal SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return GatewayCost
+ GatewayLabourAndConsumablesCost
+ GatewayPreparationCost
+ GatewayAntennaCost
+ GatewaySundyCost
+ DeviceCost
+ DeviceInstallationCostLabourAndConsumables
+ DevicePreparationCost
+ DeviceAntennaCost
+ DeviceCTsCost
+ RTUCost
+ RTUProbeCost
+ RTUInstallationCostLabourAndConsumables
+ RTUPreparationCost
+ RTUAntennaCost
+ ControlUnitCost
+ ControlUnitInstallationCostLabourAndConsumables
+ ControlUnitPreparationCost
+ AntennaCost
+ SundyCost;

                }
            }
        }
    }
    public class L_MeterRentals_Cost_DetailsModel
    {
        public List<L_MeterRentals_Cost_DetailsItem> L_MeterRentals_Cost_DetailsItems { get; set; }

        public class L_MeterRentals_Cost_DetailsItem
        {
            public Data.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public string PhotoURL { get; set; }

            public decimal? GatewayCost { get; set; }
            public decimal? GatewayLabourAndConsumablesCost { get; set; }
            public decimal? GatewayPreparationCost { get; set; }
            public decimal? GatewayAntennaCost { get; set; }
            public decimal? GatewaySundyCost { get; set; }
            public decimal? DeviceCost { get; set; }
            public decimal? DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal? DevicePreparationCost { get; set; }
            public decimal? DeviceAntennaCost { get; set; }
            public decimal? DeviceCTsCost { get; set; }
            public decimal? RTUCost { get; set; }
            public decimal? RTUProbeCost { get; set; }
            public decimal? RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal? RTUPreparationCost { get; set; }
            public decimal? RTUAntennaCost { get; set; }
            public decimal? ControlUnitCost { get; set; }
            public decimal? ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal? ControlUnitPreparationCost { get; set; }
            public decimal? AntennaCost { get; set; }
            public decimal? SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return
                        (GatewayCost.HasValue ? GatewayCost.Value : 0)
                        + (GatewayLabourAndConsumablesCost.HasValue ? GatewayLabourAndConsumablesCost.Value : 0)
                        + (GatewayPreparationCost.HasValue ? GatewayPreparationCost.Value : 0)
                        + (GatewayAntennaCost.HasValue ? GatewayAntennaCost.Value : 0)
                        + (GatewaySundyCost.HasValue ? GatewaySundyCost.Value : 0)
                        + (DeviceCost.HasValue ? DeviceCost.Value : 0)
                        + (DeviceInstallationCostLabourAndConsumables.HasValue ? DeviceInstallationCostLabourAndConsumables.Value : 0)
                        + (DevicePreparationCost.HasValue ? DevicePreparationCost.Value : 0)
                        + (DeviceAntennaCost.HasValue ? DeviceAntennaCost.Value : 0)
                        + (DeviceCTsCost.HasValue ? DeviceCTsCost.Value : 0)
                        + (RTUCost.HasValue ? RTUCost.Value : 0)
                        + (RTUProbeCost.HasValue ? RTUProbeCost.Value : 0)
                        + (RTUInstallationCostLabourAndConsumables.HasValue ? RTUInstallationCostLabourAndConsumables.Value : 0)
                        + (RTUPreparationCost.HasValue ? RTUPreparationCost.Value : 0)
                        + (RTUAntennaCost.HasValue ? RTUAntennaCost.Value : 0)
                        + (ControlUnitCost.HasValue ? ControlUnitCost.Value : 0)
                        + (ControlUnitInstallationCostLabourAndConsumables.HasValue ? ControlUnitInstallationCostLabourAndConsumables.Value : 0)
                        + (ControlUnitPreparationCost.HasValue ? ControlUnitPreparationCost.Value : 0)
                        + (AntennaCost.HasValue ? AntennaCost.Value : 0)
                        + (SundyCost.HasValue ? SundyCost.Value : 0)
                        ;

                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false)
        {
            if (isBold)
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
                    else
                        return " class=\"text-nowrap font-weight-bold text-right\"";
                }
                else
                    return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
            }
            else
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap text-right table-danger\"";
                    else
                        return " class=\"text-nowrap text-right\"";
                }
                else
                    return " class=\"text-nowrap text-right table-danger\"";
            }
        }
    }
    public class L_MeterRentals_Cost_Gateway_DetailsModel
    {
        public List<L_MeterRentals_Cost_Gateway_DetailsItem> L_MeterRentals_Cost_Gateway_DetailsItems { get; set; }

        public class L_MeterRentals_Cost_Gateway_DetailsItem
        {
            public Data.Gateway Gateway { get; set; }

            public decimal? GatewayCost { get; set; }
            public decimal? GatewayLabourAndConsumablesCost { get; set; }
            public decimal? GatewayPreparationCost { get; set; }
            public decimal? GatewayAntennaCost { get; set; }
            public decimal? GatewaySundyCost { get; set; }
            public decimal? DeviceCost { get; set; }
            public decimal? DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal? DevicePreparationCost { get; set; }
            public decimal? DeviceAntennaCost { get; set; }
            public decimal? DeviceCTsCost { get; set; }
            public decimal? RTUCost { get; set; }
            public decimal? RTUProbeCost { get; set; }
            public decimal? RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal? RTUPreparationCost { get; set; }
            public decimal? RTUAntennaCost { get; set; }
            public decimal? ControlUnitCost { get; set; }
            public decimal? ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal? ControlUnitPreparationCost { get; set; }
            public decimal? AntennaCost { get; set; }
            public decimal? SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return
                        (GatewayCost.HasValue ? GatewayCost.Value : 0)
                        + (GatewayLabourAndConsumablesCost.HasValue ? GatewayLabourAndConsumablesCost.Value : 0)
                        + (GatewayPreparationCost.HasValue ? GatewayPreparationCost.Value : 0)
                        + (GatewayAntennaCost.HasValue ? GatewayAntennaCost.Value : 0)
                        + (GatewaySundyCost.HasValue ? GatewaySundyCost.Value : 0)
                        + (DeviceCost.HasValue ? DeviceCost.Value : 0)
                        + (DeviceInstallationCostLabourAndConsumables.HasValue ? DeviceInstallationCostLabourAndConsumables.Value : 0)
                        + (DevicePreparationCost.HasValue ? DevicePreparationCost.Value : 0)
                        + (DeviceAntennaCost.HasValue ? DeviceAntennaCost.Value : 0)
                        + (DeviceCTsCost.HasValue ? DeviceCTsCost.Value : 0)
                        + (RTUCost.HasValue ? RTUCost.Value : 0)
                        + (RTUProbeCost.HasValue ? RTUProbeCost.Value : 0)
                        + (RTUInstallationCostLabourAndConsumables.HasValue ? RTUInstallationCostLabourAndConsumables.Value : 0)
                        + (RTUPreparationCost.HasValue ? RTUPreparationCost.Value : 0)
                        + (RTUAntennaCost.HasValue ? RTUAntennaCost.Value : 0)
                        + (ControlUnitCost.HasValue ? ControlUnitCost.Value : 0)
                        + (ControlUnitInstallationCostLabourAndConsumables.HasValue ? ControlUnitInstallationCostLabourAndConsumables.Value : 0)
                        + (ControlUnitPreparationCost.HasValue ? ControlUnitPreparationCost.Value : 0)
                        + (AntennaCost.HasValue ? AntennaCost.Value : 0)
                        + (SundyCost.HasValue ? SundyCost.Value : 0)
                        ;

                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false)
        {
            if (isBold)
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
                    else
                        return " class=\"text-nowrap font-weight-bold text-right\"";
                }
                else
                    return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
            }
            else
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap text-right table-danger\"";
                    else
                        return " class=\"text-nowrap text-right\"";
                }
                else
                    return " class=\"text-nowrap text-right table-danger\"";
            }
        }
    }
    public class L_MeterRentals_Accounting_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_Accounting_SummaryItem> L_MeterRentals_Accounting_SummaryItems { get; set; }

        public class L_MeterRentals_Accounting_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<DateTime, decimal> MonthlyRentalFigures { get; set; }

        }
    }
    public class L_MeterRentals_Accounting_ResultsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_Accounting_ResultsItem> L_MeterRentals_Accounting_ResultsItems { get; set; }

        public class L_MeterRentals_Accounting_ResultsItem
        {
            public string CompanyName { get; set; }
            public string MeterID { get; set; }
            public string SerialNo { get; set; }
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public string Owner { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyRentalFigures { get; set; }
        }

        public static string GetCellClass(decimal? value, bool isBold = false)
        {
            if (isBold)
            {
                if (!value.HasValue)
                    return " class=\"font-weight-bold text-right table-danger\"";
                //if (Convert.ToInt32(value) == 0)
                //    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (!value.HasValue)
                    return " class=\"text-right table-danger\"";
                //if (Convert.ToInt32(value) == 0)
                //    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }
    }
    public class L_MeterRentals_Accounting_Cost_SummaryModel
    {
        public List<L_MeterRentals_Accounting_Cost_SummaryItem> L_MeterRentals_Accounting_Cost_SummaryItems { get; set; }

        public class L_MeterRentals_Accounting_Cost_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public decimal GatewayCost { get; set; }
            public decimal GatewayLabourAndConsumablesCost { get; set; }
            public decimal GatewayPreparationCost { get; set; }
            public decimal GatewayAntennaCost { get; set; }
            public decimal GatewaySundyCost { get; set; }
            public decimal DeviceCost { get; set; }
            public decimal DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal DevicePreparationCost { get; set; }
            public decimal DeviceAntennaCost { get; set; }
            public decimal DeviceCTsCost { get; set; }
            public decimal RTUCost { get; set; }
            public decimal RTUProbeCost { get; set; }
            public decimal RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal RTUPreparationCost { get; set; }
            public decimal RTUAntennaCost { get; set; }
            public decimal ControlUnitCost { get; set; }
            public decimal ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal ControlUnitPreparationCost { get; set; }
            public decimal AntennaCost { get; set; }
            public decimal SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return GatewayCost
+ GatewayLabourAndConsumablesCost
+ GatewayPreparationCost
+ GatewayAntennaCost
+ GatewaySundyCost
+ DeviceCost
+ DeviceInstallationCostLabourAndConsumables
+ DevicePreparationCost
+ DeviceAntennaCost
+ DeviceCTsCost
+ RTUCost
+ RTUProbeCost
+ RTUInstallationCostLabourAndConsumables
+ RTUPreparationCost
+ RTUAntennaCost
+ ControlUnitCost
+ ControlUnitInstallationCostLabourAndConsumables
+ ControlUnitPreparationCost
+ AntennaCost
+ SundyCost;

                }
            }
        }
    }
    public class L_MeterRentals_Accounting_Cost_DetailsModel
    {
        [Display(Name = "Rental Month")]
        public List<SelectListItem> RentalMonth { get; set; }
        public List<L_MeterRentals_Accounting_Cost_DetailsItem> L_MeterRentals_Accounting_Cost_DetailsItems { get; set; }

        public class L_MeterRentals_Accounting_Cost_DetailsItem
        {
            public Data.RentalDataDump RentalDataDump { get; set; }

            public decimal? GatewayCost { get; set; }
            public decimal? GatewayLabourAndConsumablesCost { get; set; }
            public decimal? GatewayPreparationCost { get; set; }
            public decimal? GatewayAntennaCost { get; set; }
            public decimal? GatewaySundyCost { get; set; }
            public decimal? DeviceCost { get; set; }
            public decimal? DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal? DevicePreparationCost { get; set; }
            public decimal? DeviceAntennaCost { get; set; }
            public decimal? DeviceCTsCost { get; set; }
            public decimal? RTUCost { get; set; }
            public decimal? RTUProbeCost { get; set; }
            public decimal? RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal? RTUPreparationCost { get; set; }
            public decimal? RTUAntennaCost { get; set; }
            public decimal? ControlUnitCost { get; set; }
            public decimal? ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal? ControlUnitPreparationCost { get; set; }
            public decimal? AntennaCost { get; set; }
            public decimal? SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return
                        (GatewayCost.HasValue ? GatewayCost.Value : 0)
                        + (GatewayLabourAndConsumablesCost.HasValue ? GatewayLabourAndConsumablesCost.Value : 0)
                        + (GatewayPreparationCost.HasValue ? GatewayPreparationCost.Value : 0)
                        + (GatewayAntennaCost.HasValue ? GatewayAntennaCost.Value : 0)
                        + (GatewaySundyCost.HasValue ? GatewaySundyCost.Value : 0)
                        + (DeviceCost.HasValue ? DeviceCost.Value : 0)
                        + (DeviceInstallationCostLabourAndConsumables.HasValue ? DeviceInstallationCostLabourAndConsumables.Value : 0)
                        + (DevicePreparationCost.HasValue ? DevicePreparationCost.Value : 0)
                        + (DeviceAntennaCost.HasValue ? DeviceAntennaCost.Value : 0)
                        + (DeviceCTsCost.HasValue ? DeviceCTsCost.Value : 0)
                        + (RTUCost.HasValue ? RTUCost.Value : 0)
                        + (RTUProbeCost.HasValue ? RTUProbeCost.Value : 0)
                        + (RTUInstallationCostLabourAndConsumables.HasValue ? RTUInstallationCostLabourAndConsumables.Value : 0)
                        + (RTUPreparationCost.HasValue ? RTUPreparationCost.Value : 0)
                        + (RTUAntennaCost.HasValue ? RTUAntennaCost.Value : 0)
                        + (ControlUnitCost.HasValue ? ControlUnitCost.Value : 0)
                        + (ControlUnitInstallationCostLabourAndConsumables.HasValue ? ControlUnitInstallationCostLabourAndConsumables.Value : 0)
                        + (ControlUnitPreparationCost.HasValue ? ControlUnitPreparationCost.Value : 0)
                        + (AntennaCost.HasValue ? AntennaCost.Value : 0)
                        + (SundyCost.HasValue ? SundyCost.Value : 0)
                        ;

                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false)
        {
            if (isBold)
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
                    else
                        return " class=\"text-nowrap font-weight-bold text-right\"";
                }
                else
                    return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
            }
            else
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap text-right table-danger\"";
                    else
                        return " class=\"text-nowrap text-right\"";
                }
                else
                    return " class=\"text-nowrap text-right table-danger\"";
            }
        }
    }
    public class L_MeterRentals_Accounting_Cost_Gateway_DetailsModel
    {
        [Display(Name = "Rental Month")]
        public List<SelectListItem> RentalMonth { get; set; }

        public List<L_MeterRentals_Accounting_Cost_Gateway_DetailsItem> L_MeterRentals_Accounting_Cost_Gateway_DetailsItems { get; set; }

        public class L_MeterRentals_Accounting_Cost_Gateway_DetailsItem
        {
            public Data.RentalDataDump RentalDataDump { get; set; }

            public decimal? GatewayCost { get; set; }
            public decimal? GatewayLabourAndConsumablesCost { get; set; }
            public decimal? GatewayPreparationCost { get; set; }
            public decimal? GatewayAntennaCost { get; set; }
            public decimal? GatewaySundyCost { get; set; }
            public decimal? DeviceCost { get; set; }
            public decimal? DeviceInstallationCostLabourAndConsumables { get; set; }
            public decimal? DevicePreparationCost { get; set; }
            public decimal? DeviceAntennaCost { get; set; }
            public decimal? DeviceCTsCost { get; set; }
            public decimal? RTUCost { get; set; }
            public decimal? RTUProbeCost { get; set; }
            public decimal? RTUInstallationCostLabourAndConsumables { get; set; }
            public decimal? RTUPreparationCost { get; set; }
            public decimal? RTUAntennaCost { get; set; }
            public decimal? ControlUnitCost { get; set; }
            public decimal? ControlUnitInstallationCostLabourAndConsumables { get; set; }
            public decimal? ControlUnitPreparationCost { get; set; }
            public decimal? AntennaCost { get; set; }
            public decimal? SundyCost { get; set; }
            public decimal TotalCost
            {
                get
                {
                    return
                        (GatewayCost.HasValue ? GatewayCost.Value : 0)
                        + (GatewayLabourAndConsumablesCost.HasValue ? GatewayLabourAndConsumablesCost.Value : 0)
                        + (GatewayPreparationCost.HasValue ? GatewayPreparationCost.Value : 0)
                        + (GatewayAntennaCost.HasValue ? GatewayAntennaCost.Value : 0)
                        + (GatewaySundyCost.HasValue ? GatewaySundyCost.Value : 0)
                        + (DeviceCost.HasValue ? DeviceCost.Value : 0)
                        + (DeviceInstallationCostLabourAndConsumables.HasValue ? DeviceInstallationCostLabourAndConsumables.Value : 0)
                        + (DevicePreparationCost.HasValue ? DevicePreparationCost.Value : 0)
                        + (DeviceAntennaCost.HasValue ? DeviceAntennaCost.Value : 0)
                        + (DeviceCTsCost.HasValue ? DeviceCTsCost.Value : 0)
                        + (RTUCost.HasValue ? RTUCost.Value : 0)
                        + (RTUProbeCost.HasValue ? RTUProbeCost.Value : 0)
                        + (RTUInstallationCostLabourAndConsumables.HasValue ? RTUInstallationCostLabourAndConsumables.Value : 0)
                        + (RTUPreparationCost.HasValue ? RTUPreparationCost.Value : 0)
                        + (RTUAntennaCost.HasValue ? RTUAntennaCost.Value : 0)
                        + (ControlUnitCost.HasValue ? ControlUnitCost.Value : 0)
                        + (ControlUnitInstallationCostLabourAndConsumables.HasValue ? ControlUnitInstallationCostLabourAndConsumables.Value : 0)
                        + (ControlUnitPreparationCost.HasValue ? ControlUnitPreparationCost.Value : 0)
                        + (AntennaCost.HasValue ? AntennaCost.Value : 0)
                        + (SundyCost.HasValue ? SundyCost.Value : 0)
                        ;

                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false)
        {
            if (isBold)
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
                    else
                        return " class=\"text-nowrap font-weight-bold text-right\"";
                }
                else
                    return " class=\"text-nowrap font-weight-bold text-right table-danger\"";
            }
            else
            {
                if (value.HasValue)
                {
                    if (Convert.ToInt32(value) == 0)
                        return " class=\"text-nowrap text-right table-danger\"";
                    else
                        return " class=\"text-nowrap text-right\"";
                }
                else
                    return " class=\"text-nowrap text-right table-danger\"";
            }
        }
    }
    public class L_MeterRentals_Accounting_Cost_Device_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_Accounting_Cost_Device_SummaryItem> L_MeterRentals_Accounting_Cost_Device_SummaryItems { get; set; }

        public class L_MeterRentals_Accounting_Cost_Device_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<DateTime, decimal> MonthlyRentalFigures { get; set; }

        }
    }
    public class L_MeterRentals_Accounting_Cost_Gateway_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<L_MeterRentals_Accounting_Cost_Gateway_SummaryItem> L_MeterRentals_Accounting_Cost_Gateway_SummaryItems { get; set; }

        public class L_MeterRentals_Accounting_Cost_Gateway_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<DateTime, decimal> MonthlyRentalFigures { get; set; }

        }
    }
}
