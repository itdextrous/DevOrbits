using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.B03_SupplyCouncilStatementsModels
{
    public class B03_SupplyCouncilStatements_CouncilStatementSummaryModel
    {
        [DisplayName("Partner")]
        public List<SelectListItem> PartnerID { get; set; }

        [DisplayName("Status")]
        public List<SelectListItem> Status { get; set; }

        [DisplayName("Payment Type")]
        public List<SelectListItem> PaymentType { get; set; }

        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B03_SupplyCouncilStatements_CouncilStatementSummaryItem> B03_SupplyCouncilStatements_CouncilStatementSummaryItems { get; set; }

        public class B03_SupplyCouncilStatements_CouncilStatementSummaryItem
        {
            public string PropertyLinked { get; set; }
            public int CompanyID { get; set; }
            public int CouncilInvoicesLoaded { get; set; }
            public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
            public Data.BuildingCycle BuildingCycle { get; set; }
            public Data.BuildingCouncilType BuildingCouncilType { get; set; }
            public LatestInvoice Latest_Invoice { get; set; }
            public StatusType Status { get; set; }
            public DateTime? CurrentCycleBillingDate { get; set; }

            public class LatestInvoice : Data.BuildingCouncilDetails_Invoice
            {
                public List<Data.BuildingCouncilDetails_InvoiceItem> BuildingCouncilDetails_InvoiceItems { get; set; }

                public decimal OpeningBalance { get; set; }
                public decimal ClosingBalance { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }
                public decimal OpeningBalanceServiceProvider { get; set; }
                public decimal ClosingBalanceServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }
                public decimal OpeningBalanceClient { get; set; }
                public decimal ClosingBalanceClient { get; set; }

                public decimal Diff
                {
                    get
                    {
                        return ClosingBalance - ClosingBalanceServiceProvider - ClosingBalanceClient;
                    }
                }
            }

            public enum StatusType
            {
                [Description("Unknown")]
                Unknown = 0,
                [Description("Current")]
                Current = 1,
                [Description("Outdated")]
                Outdated = 2,
            }
        }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementDetailsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B03_SupplyCouncilStatements_CouncilStatementDetailsItem> B03_SupplyCouncilStatements_CouncilStatementDetailsItems { get; set; }

        public class B03_SupplyCouncilStatements_CouncilStatementDetailsItem
        {
            public int InvoiceID { get; set; }
            public string PropertyLinked { get; set; }
            public string AccountNo { get; set; }
            public string TAXInvoiceNo { get; set; }
            public DateTime? TAXInvoiceDate { get; set; }
            public DateTime FinalDateForPayment { get; set; }
            public Dictionary<string, decimal> Resourcees { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal TotalCharges
            {
                get
                {
                    if (Resourcees != null)
                        return (from p in Resourcees
                                where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                select p.Value).Sum();
                    else
                        return 0;
                }
            }
            public string ReferencedDocument { get; set; }

            public decimal PaidByServiceProvider { get; set; }
            public decimal PayableByServiceProvider { get; set; }
            public decimal OpeningBalanceServiceProvider { get; set; }
            public decimal ClosingBalanceServiceProvider { get; set; }

            public decimal PaidByClient { get; set; }
            public decimal PayableByClient { get; set; }
            public decimal OpeningBalanceClient { get; set; }
            public decimal ClosingBalanceClient { get; set; }

            public decimal Diff
            {
                get
                {
                    return ClosingBalance - ClosingBalanceServiceProvider - ClosingBalanceClient;
                }
            }

            public Data.BuildingCouncilDetails_Invoice.StatusEnum Status { get; set; }
        }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementReportModel
    {
        public List<SelectListItem> AccountNo { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<SelectListItem> Meter { get; set; }
        public List<SelectListItem> ChargeType { get; set; }
        public List<SelectListItem> ResourceType { get; set; }
        public List<SelectListItem> PayableBy { get; set; }
        public List<SelectListItem> Product { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<BuildingCouncilDetails_Invoice> B03_SupplyCouncilStatements_CouncilStatementReportItems { get; set; }

        public class BuildingCouncilDetails_Invoice : Data.BuildingCouncilDetails_Invoice
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public string ApprovedByUsername { get; set; }
            public List<BuildingCouncilDetails_InvoiceItem> BuildingCouncilDetails_InvoiceItems { get; set; }

            public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
            {
                public string MeterNo { get; set; }
                public string ChargeType { get; set; }
                public string ResourceType { get; set; }
                public string ReadingType { get; set; }
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
                public string ProductName { get; set; }
            }
        }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel
    {
        public List<SelectListItem> AccountNo { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<SelectListItem> Meter { get; set; }
        public List<SelectListItem> ChargeType { get; set; }
        public List<SelectListItem> ResourceType { get; set; }
        public List<SelectListItem> PayableBy { get; set; }
        public List<SelectListItem> Product { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<BuildingCouncilDetails_Invoice> B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems { get; set; }

        public class BuildingCouncilDetails_Invoice : Data.BuildingCouncilDetails_Invoice
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public string ApprovedByUsername { get; set; }
            public List<BuildingCouncilDetails_InvoiceItem> BuildingCouncilDetails_InvoiceItems { get; set; }

            public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
            {
                public string MeterNo { get; set; }
                public string ChargeType { get; set; }
                public string ResourceType { get; set; }
                public string ReadingType { get; set; }
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
                public string ProductName { get; set; }
                public List<BuildingCouncilDetails_InvoiceItem_Month> BuildingCouncilDetails_InvoiceItem_Months { get; set; }

                public class BuildingCouncilDetails_InvoiceItem_Month : Data.BuildingCouncilDetails_InvoiceItem_Month
                {
                    public string ChargeType { get; set; }
                    public string ProductName { get; set; }
                    public string ResourceType { get; set; }
                }
            }
        }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementCaptureModel
    {
        [DisplayName("Referenced Document Upload")]
        public IFormFile ReferencedDocument { get; set; }

        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }

        [DisplayName("Tax Invoice No")]
        public string TaxInvoiceNo { get; set; }

        [DisplayName("Tax Invoice Date")]
        public DateTime? TaxInvoiceDate { get; set; }

        [DisplayName("Current Reading Date")]
        public DateTime? CurrentReadingDate { get; set; }

        [DisplayName("Final Date For Payment")]
        public DateTime? FinalDateForPayment { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        public string CreatedByUsername { get; set; }
        public string UpdatedByUsername { get; set; }
        public string ApprovedByUsername { get; set; }

        public Data.BuildingCouncilDetails_Invoice BuildingCouncilDetails_Invoice { get; set; }
        public decimal OpeningBalance { get; set; }

        public List<B03_SupplyCouncilStatements_CouncilStatementCaptureItem> B03_SupplyCouncilStatements_CouncilStatementCaptureItems { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<Data.BuildingCouncilInvoiceChargeType> BuildingCouncilInvoiceChargeTypes { get; set; }
        public List<Data.BuildingCouncilInvoiceReadingType> BuildingCouncilInvoiceReadingTypes { get; set; }

        public class B03_SupplyCouncilStatements_CouncilStatementCaptureItem : Data.BuildingCouncilDetails_InvoiceItem
        {
            public string MeterNo { get; set; }
            public string ChargeType { get; set; }
            public string ResourceType { get; set; }
            public string ReadingType { get; set; }
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public string ProductName { get; set; }
        }

        public bool IsSuccessfull { get; set; }
        public bool InvalidBuildingCouncilDetails { get; set; }

        public decimal OpeningBalanceServiceProvider { get; set; }
        public decimal PaymentsServiceProvider { get; set; }
        public decimal TransactionsServiceProvider { get; set; }
        public decimal ClosingBalanceServiceProvider { get { return OpeningBalanceServiceProvider + TransactionsServiceProvider + PaymentsServiceProvider; } }
        public decimal OpeningBalanceClient { get; set; }
        public decimal PaymentsClient { get; set; }
        public decimal TransactionsClient { get; set; }
        public decimal ClosingBalanceClient { get { return OpeningBalanceClient + TransactionsClient + PaymentsClient; } }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel
    {
        public Data.BuildingCouncilDetails_Invoice BuildingCouncilDetails_Invoice { get; set; }
        public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
        public int? InvoiceItemID { get; set; }

        [DisplayName("Referenced Document Upload")]
        public IFormFile ReferencedDocument { get; set; }

        [DisplayName("Meter")]
        public List<SelectListItem> BuildingCouncilMeter { get; set; }

        [DisplayName("Charge Type")]
        public List<SelectListItem> ChargeType { get; set; }

        [DisplayName("Resource Type")]
        public List<SelectListItem> ResourceType { get; set; }

        [DisplayName("Action Date")]
        public DateTime ActionDate { get; set; }

        [DisplayName("Reading Type")]
        public List<SelectListItem> ReadingType { get; set; }

        //[DisplayName("Payment By")]
        //public List<SelectListItem> PaymentBy { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Current Date")]
        public DateTime? CurrentDate { get; set; }

        [DisplayName("Previous Date")]
        public DateTime? PreviousDate { get; set; }

        [DisplayName("No Of Days")]
        public int? NoOfDays { get; set; }

        [DisplayName("Opening For Meter")]
        public decimal? OpeningForMeter { get; set; }

        [DisplayName("Closing For Meter")]
        public decimal? ClosingForMeter { get; set; }

        [DisplayName("Consumption Units")]
        public decimal? ConsumptionUnits { get; set; }

        [DisplayName("Average Rate Per Unit")]
        [DisplayFormat(DataFormatString = "{0:0.0000}")]
        public decimal? AverageRatePerUnit { get; set; }

        [DisplayName("Amount Ex VAT")]
        public decimal AmountExcl { get; set; }

        [DisplayName("VAT (%)")]
        [Range(0, 100)]
        public decimal VAT { get; set; }

        [DisplayName("Payable By Service Provider (%)")]
        [Range(0, 100)]
        public decimal PayableByServiceProvider { get; set; }

        [DisplayName("Payable By Service Provider Excl. VAT")]
        public decimal? PayableByServiceProviderExclVAT { get; set; }

        [DisplayName("Payable By Service Provider VAT")]
        public decimal? PayableByServiceProviderVAT { get; set; }

        [Required]
        [DisplayName("Product")]
        public List<SelectListItem> ProductID { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedByUsername { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string UpdatedByUsername { get; set; }

        public bool IsSuccessfull { get; set; }
    }

    public class B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel
    {
        public BuildingCouncilDetails_InvoiceItem InvoiceItem { get; set; }

        public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
        {
            public string MeterNo { get; set; }
            public string ChargeType { get; set; }
            public string ResourceType { get; set; }
            public string ReadingType { get; set; }
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public string ProductName { get; set; }
            public Controllers.Operational.B03_SupplyCouncilStatements.B03_SupplyCouncilStatementsController.BuildingCouncilDetails_InvoiceItem_MonthResult Item_MonthResult { get; set; }

        }

    }

    public class B03_SupplyCouncilStatements_CouncilReconReportModel
    {
        public List<B03_SupplyCouncilStatements_CouncilReconReportItem> B03_SupplyCouncilStatements_CouncilReconReportItems { get; set; }

        public class B03_SupplyCouncilStatements_CouncilReconReportItem : Data.BuildingCouncilReconReportConfig
        {
            public string RequestedByUsername { get; set; }
            public string CompanyName { get; set; }
        }
    }


}
