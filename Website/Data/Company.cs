using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Company
    {
        public int CompanyID { get; set; }
        public string Name { get; set; }
        public string ServiceKey { get; set; }
        public bool Registrable { get; set; }
        public decimal? BalanceMustBeAbove { get; set; }
        public string BalanceCheckSkybillCustomerNo { get; set; }
        public bool? ExistsInSkybill { get; set; }
        public int? PartnerID { get; set; }
        public bool IsFlagStatusActive { get; set; }
        public bool IsDailyBillingStatusActive { get; set; }
        public decimal? ConvFactor { get; set; }
        public string Batch { get; set; }
        public string PlantNo { get; set; }
        public string StockRefNo { get; set; }

        public string Sales_Document_Type_VBAK_AUART { get; set; }
        public string Sales_Organization_VBAK_VKORG { get; set; }
        public string Distribution_Channel_VBAK_VTWEG { get; set; }
        public string Division_VBAK_SPART { get; set; }
        public string Sales_Office_VBAK_VKBUR { get; set; }
        public string ItemID { get; set; }
        public string Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 { get; set; }
        public string Route_VBAP_ROUTE_01 { get; set; }

        public string MasterServiceKey { get; set; }
        public decimal? NetcashBalance { get; set; }
        public DateTime? NetcashBalanceDate { get; set; }

        public string NetcashBankName { get; set; }
        public string NetcashBankAccountType { get; set; }
        public string NetcashBankAccountNo { get; set; }
        public string NetcashBankBranchCode { get; set; }

        public int? ActionID { get; set; }
        public DateTime? TargetDate { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }

        public bool? IsCeilingActiveOnMidnightSync { get; set; }
        public int? NoOfRegisteredUnits { get; set; }

        public string SupplierName { get; set; }
        public string SupplierAddress { get; set; }
        public string SupplierVATNumber { get; set; }
        public string SupplierPostal { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierURL { get; set; }

        public int? NoOfMeteringPoints { get; set; }

        public decimal? SupplyPerCycle { get; set; }

        public int? CalibrationValidDays { get; set; }
        public int? CompanyTypeID { get; set; }

        public string Province { get; set; }
        public string LocalMunicipality { get; set; }
        public string LegalEntity { get; set; }

        public bool? SyncManagementAccounts { get; set; }

        public string CustomBankName { get; set; }
        public string CustomBankAccountType { get; set; }
        public string CustomBankAccountNo { get; set; }
        public string CustomBankBranchCode { get; set; }

        public bool? Active { get; set; }

        public decimal? ManifoldSupply_Left_Units { get; set; }
        public decimal? ManifoldSupply_Left_KG { get; set; }
        public decimal? ManifoldSupply_Right_Units { get; set; }
        public decimal? ManifoldSupply_Right_KG { get; set; }

        public string ACORegulatorSerialNo { get; set; }

        public int? DeviceAPIID { get; set; }

        public int? YearOfDevelopment { get; set; }
        public decimal? AverageValuation { get; set; }
        public string AverageLSM { get; set; }
        public int? MunicipalityID { get; set; }
        public int? SuburbID { get; set; }
        public string? StreetAddress { get; set; }
        public string Website { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }

        public int? LegalEntityID { get; set; }

        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string ContractAttachment { get; set; }

        public string OperationalBankName { get; set; }
        public string OperationalBankAccountType { get; set; }
        public string OperationalBankAccountNo { get; set; }
        public string OperationalBankBranchCode { get; set; }

        public decimal? ConvenienceFeePerc { get; set; }

        public int? Priority { get; set; }
        public int? SageAccountingCompanyID { get; set; }
        public int? SageAccountingLegalEntityCompanyID { get; set; }

        public enum ActionEnum
        {
            [Description("Outstanding Info")]
            OutstandingInfo = 1,
            [Description("System setup incomplete")]
            SystemSetupIncomplete = 2,
            [Description("Meter replacements required")]
            MeterReplacementsRequired = 3,
            [Description("Communication equipment installation")]
            CommunicationEquipmentInstallation = 4,
            [Description("Installed, awaiting final sign off")]
            InstalledAwaitingFinalSignOff = 5,
            [Description("Completed")]
            Completed = 6,
        }
    }

    public class Company_FinancialDetail
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerDetails { get; set; }
        public int BillingTypeID { get; set; }

        public string Sales_Electricity_Cons_EndUser_InvoiceTo { get; set; }
        public string Sales_Electricity_Cons_EndUser_Tariff { get; set; }
        public string Sales_Electricity_Cons_EndUser_BillingType { get; set; }

        public string Sales_Electricity_Cons_CommonArea_InvoiceTo { get; set; }
        public string Sales_Electricity_Cons_CommonArea_Tariff { get; set; }
        public string Sales_Electricity_Cons_CommonArea_BillingType { get; set; }

        public string Sales_Electricity_Fixed_InvoiceTo { get; set; }
        public string Sales_Electricity_Fixed_Tariff { get; set; }
        public string Sales_Electricity_Fixed_BillingType { get; set; }

        public string Sales_Water_Cons_EndUsers_InvoiceTo { get; set; }
        public string Sales_Water_Cons_EndUsers_Tariff { get; set; }
        public string Sales_Water_Cons_EndUsers_BillingType { get; set; }

        public string Sales_Water_Cons_CommonArea_InvoiceTo { get; set; }
        public string Sales_Water_Cons_CommonArea_Tariff { get; set; }
        public string Sales_Water_Cons_CommonArea_BillingType { get; set; }

        public string Sales_Water_Fixed_InvoiceTo { get; set; }
        public string Sales_Water_Fixed_Tariff { get; set; }
        public string Sales_Water_Fixed_BillingType { get; set; }

        public string Sales_Sanitation_Cons_EndUsers_InvoiceTo { get; set; }
        public string Sales_Sanitation_Cons_EndUsers_Tariff { get; set; }
        public string Sales_Sanitation_Cons_EndUsers_BillingType { get; set; }

        public string Sales_Sanitation_Cons_CommonArea_InvoiceTo { get; set; }
        public string Sales_Sanitation_Cons_CommonArea_Tariff { get; set; }
        public string Sales_Sanitation_Cons_CommonArea_BillingType { get; set; }

        public string Sales_Sanitation_Fixed_InvoiceTo { get; set; }
        public string Sales_Sanitation_Fixed_Tariff { get; set; }
        public string Sales_Sanitation_Fixed_BillingType { get; set; }

        public string Sales_Gas_Cons_EndUsers_InvoiceTo { get; set; }
        public string Sales_Gas_Cons_EndUsers_Tariff { get; set; }
        public string Sales_Gas_Cons_EndUsers_BillingType { get; set; }

        public string Sales_Gas_Cons_CommonArea_InvoiceTo { get; set; }
        public string Sales_Gas_Cons_CommonArea_Tariff { get; set; }
        public string Sales_Gas_Cons_CommonArea_BillingType { get; set; }

        public string Sales_Gas_Fixed_InvoiceTo { get; set; }
        public string Sales_Gas_Fixed_Tariff { get; set; }
        public string Sales_Gas_Fixed_BillingType { get; set; }

        public string Sales_MeteringFees_Cons_EndUsers_Electricity { get; set; }
        public string Sales_MeteringFees_Cons_EndUsers_Water { get; set; }
        public string Sales_MeteringFees_Cons_EndUsers_Gas { get; set; }
        public string Sales_MeteringFees_Cons_EndUsers_Other { get; set; }

        public string Sales_MeteringFees_Cons_CommonArea_Electricity { get; set; }
        public string Sales_MeteringFees_Cons_CommonArea_Water { get; set; }
        public string Sales_MeteringFees_Cons_CommonArea_Gas { get; set; }
        public string Sales_MeteringFees_Cons_CommonArea_Other { get; set; }

        public string Sales_MeteringFees_Fixed_InvoiceTo { get; set; }
        public string Sales_MeteringFees_Fixed_Tariff { get; set; }
        public string Sales_MeteringFees_Fixed_BillingType { get; set; }

        public string Sales_OtherFees_Cons_EndUsers_Electricity { get; set; }
        public string Sales_OtherFees_Cons_EndUsers_Water { get; set; }
        public string Sales_OtherFees_Cons_EndUsers_Gas { get; set; }
        public string Sales_OtherFees_Cons_EndUsers_Other { get; set; }

        public string Sales_OtherFees_Cons_CommonArea_Electricity { get; set; }
        public string Sales_OtherFees_Cons_CommonArea_Water { get; set; }
        public string Sales_OtherFees_Cons_CommonArea_Gas { get; set; }
        public string Sales_OtherFees_Cons_CommonArea_Other { get; set; }

        public string Sales_OtherFees_Cons_Fixed_InvoiceTo { get; set; }
        public string Sales_OtherFees_Cons_Fixed_Tariff { get; set; }
        public string Sales_OtherFees_Cons_Fixed_BillingType { get; set; }

        public string Supply_Electricity_Cons_InvoiceFrom { get; set; }
        public string Supply_Electricity_Cons_Tariff { get; set; }
        public string Supply_Electricity_Cons_BillingType { get; set; }

        public string Supply_Electricity_Fixed_InvoiceFrom { get; set; }
        public string Supply_Electricity_Fixed_Tariff { get; set; }
        public string Supply_Electricity_Fixed_BillingType { get; set; }

        public string Supply_Water_Cons_InvoiceFrom { get; set; }
        public string Supply_Water_Cons_Tariff { get; set; }
        public string Supply_Water_Cons_BillingType { get; set; }

        public string Supply_Water_Fixed_InvoiceFrom { get; set; }
        public string Supply_Water_Fixed_Tariff { get; set; }
        public string Supply_Water_Fixed_BillingType { get; set; }

        public string Supply_Sanitation_Cons_InvoiceFrom { get; set; }
        public string Supply_Sanitation_Cons_Tariff { get; set; }
        public string Supply_Sanitation_Cons_BillingType { get; set; }

        public string Supply_Fixed_InvoiceFrom { get; set; }
        public string Supply_Fixed_Tariff { get; set; }
        public string Supply_Fixed_BillingType { get; set; }

        public string Supply_Gas_Cons_InvoiceFrom { get; set; }
        public string Supply_Gas_Cons_Tariff { get; set; }
        public string Supply_Gas_Cons_BillingType { get; set; }

        public string Supply_Gas_Fixed_InvoiceFrom { get; set; }
        public string Supply_Gas_Fixed_Tariff { get; set; }
        public string Supply_Gas_Fixed_BillingType { get; set; }

        public string Supply_Metering_Electricity { get; set; }
        public string Supply_Metering_Water { get; set; }
        public string Supply_Metering_Gas { get; set; }
        public string Supply_Metering_Other { get; set; }

        public string Supply_Other_Electricity { get; set; }
        public string Supply_Other_Water { get; set; }
        public string Supply_Other_Gas { get; set; }
        public string Supply_Other_Other { get; set; }
    }

    public class Company_TechnicalDetail
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }

        public string Sales_Electricity_EndUsers { get; set; }
        public string Sales_Electricity_CommonArea { get; set; }
        public string Sales_Electricity_Other { get; set; }

        public string Sales_Water_EndUsers { get; set; }
        public string Sales_Water_CommonArea { get; set; }
        public string Sales_Water_Other { get; set; }

        public string Sales_Gas_EndUsers { get; set; }
        public string Sales_Gas_CommonArea { get; set; }
        public string Sales_Gas_Other { get; set; }

        public string Sales_Other_EndUsers { get; set; }
        public string Sales_Other_CommonArea { get; set; }
        public string Sales_Other_Other { get; set; }

        public string Supply_Electricity { get; set; }
        public string Supply_Water { get; set; }
        public string Supply_Gas { get; set; }
        public string Supply_Other { get; set; }
    }

    public class Company_Log
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class Company_BlockedMeterExclusion
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string MeterSerial { get; set; }
    }

    public class CompanyType
    {
        [Key]
        public int ID { get; set; }
        public string CompanyTypeName { get; set; }
    }

    public class Companies_OperationalBalance
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
