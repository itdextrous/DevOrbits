using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class DeviceRentalFee
    {
        [Key]
        public int Id { get; set; }
        public int DeviceIDLinked { get; set; }
        public System.DateTime RentalMonth { get; set; }
        public decimal StandardFee { get; set; }
        public decimal AgreedFee { get; set; }
    }

    public class RentalDataDump
    {
        [Key]
        public int ID { get; set; }
        public string PropertyLinked { get; set; }
        public string GWIDLinked { get; set; }
        public string MeterID { get; set; }
        public string SerialNumber { get; set; }
        public string Name { get; set; }
        public decimal StandardMonthlyRentalExclVAT { get; set; }
        public decimal AgreedMonthlyRentalExclVAT { get; set; }
        public DateTime RentalMonth { get; set; }
        public string EquipmentType { get; set; }
        public string Manufacturer { get; set; }
        public string Owner { get; set; }
        public decimal? GatewayCostExclVAT { get; set; }
        public decimal? GatewayLabourandconsumablescostExclVAT { get; set; }
        public decimal? GatewayPreparationCostExclVAT { get; set; }
        public decimal? GatewayAntennacostExclVAT { get; set; }
        public decimal? DevicecostExclVAT { get; set; }
        public decimal? DeviceInstallationcostLabourandconsumablesExclVAT { get; set; }
        public decimal? DevicePreparationCostExclVAT { get; set; }
        public decimal? DeviceAntennacostExclVAT { get; set; }
        public decimal? DeviceCTscostExclVAT { get; set; }
        public decimal? RTUcostExclVAT { get; set; }
        public decimal? RTUProbeCostExclVAT { get; set; }
        public decimal? RTUInstallationcostLabourandconsumablesExclVAT { get; set; }
        public decimal? RTUPreparationCostExclVAT { get; set; }
        public decimal? RTUAntennacostExclVAT { get; set; }
        public decimal? ControlUnitcostExclVAT { get; set; }
        public decimal? ControlUnitInstallationcostLabourandconsumablesExclVAT { get; set; }
        public decimal? ControlUnitPreparationCostExclVAT { get; set; }
        public decimal? SundycostExclVAT { get; set; }
        public decimal? TotalcostExclVAT { get; set; }
        public decimal? ElectricityMeter { get; set; }
        public int? WaterMeter { get; set; }
        public int? Controller { get; set; }
        public int? GasMeter { get; set; }
        public int? Other { get; set; }
        public int? TotalCount { get; set; }
        public string SkybillCustomerNo { get; set; }
        public string GPS { get; set; }
        public int? GatewayCount { get; set; }
    }

    public class Rental_Expense
    {
        [Key]
        public int ID { get; set; }
        public int ExpenseNameID { get; set; }
        public int ExpenseTypeID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class Rental_ExpenseName
    {
        [Key]
        public int ID { get; set; }
        public string ExpenseName { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class Rental_ExpenseType
    {
        [Key]
        public int ID { get; set; }
        public string TypeName { get; set; }
        public DateTime DateCreated { get; set; }
    }

}
