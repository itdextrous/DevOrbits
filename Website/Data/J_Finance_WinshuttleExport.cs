using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class J_Finance_WinshuttleExport
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal Progress { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int PartnerID { get; set; }
        public int ExportTypeID { get; set; }
        public string Customer_Purchase_Order_Number_VBKD_BSTKD { get; set; }
        //public ExportTypeEnum ExportType { get { return (ExportTypeEnum)ExportTypeID; } }
        public int? CompanyID { get; set; }
        public enum ExportTypeEnum
        {
            [Description("Pre Billing")]
            PreBilling = 1,
            [Description("Winshuttle Export")]
            WinshuttleExport = 2,
        }
    }

    public class J_Finance_WinshuttleExportItem
    {
        [Key]
        public int ID { get; set; }
        public int ReportID { get; set; }
        public DateTime DateCreated { get; set; }
        public string CustomerNo { get; set; }
        public string MeterNo { get; set; }
        public string MeterSerial { get; set; }
        public string Description { get; set; }
        public decimal OpeningReading { get; set; }
        public decimal ClosingReading { get; set; }
        public decimal Consumption { get; set; }
        public decimal Tariff { get; set; }
        public decimal TotalExVAT { get; set; }
        public int ItemResourceType { get; set; }
        public string CenterName { get; set; }
        public string CustomerRegisteredName { get; set; }
        public string CustomerTradingName { get; set; }
        public DateTime DateRead { get; set; }
        public decimal ConvFact { get; set; }
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
        public string AfroxCustomerTradingName { get; set; }
        public string AfroxCustomerAccNo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
