using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels
{
    public class B05_AccountPayments_PaymentSummaryModel
    {
        public List<B05_AccountPayments_PaymentSummaryItem> B05_AccountPayments_PaymentSummaryItems { get; set; }

        public class B05_AccountPayments_PaymentSummaryItem
        {
            public string PropertyLinked { get; set; }
            public int CompanyID { get; set; }
            public int BuildingCouncilInvoiceCount { get; set; }
            public int BuildingCouncilDetails_InvoiceItemsCount { get; set; }
            public Data.BuildingCouncilDetails_InvoiceItem Latest_BuildingCouncilDetails_InvoiceItem { get; set; }
            public DateTime? LatestPaymentDate { get; set; }
        }
    }

    public class B05_AccountPayments_PaymentDetailsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }

        public List<B05_AccountPayments_PaymentDetailsItem> B05_AccountPayments_PaymentDetailsItems { get; set; }

        public class B05_AccountPayments_PaymentDetailsItem : Data.BuildingCouncilDetails_InvoiceItem
        {
            public Data.BuildingCouncilDetails_Invoice BuildingCouncilDetails_Invoice { get; set; }
            public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
            public string SkybillDescription { get; set; }
            public bool ReversalDetected { get; set; }
        }
    }


    public class B05_AccountPayments_PaymentCaptureModel
    {
        public bool InvalidBuildingCouncilDetails { get; set; }

        [DisplayName("Building No")]
        public string BuildingNo { get; set; }

        [DisplayName("Building Name")]
        public string BuildingName { get; set; }

        [DisplayName("Building Skybill Name")]
        public string BuildingSkybillName { get; set; }

        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }

        [DisplayName("TAX Invoice No")]
        public List<SelectListItem> TAXInvoiceNo { get; set; }

        [DisplayName("Payment Date")]
        [Required]
        public DateTime? PaymentDate { get; set; }

        [DisplayName("Payment Amount")]
        [Required]
        public decimal? PaymentAmount { get; set; }

        [DisplayName("Proof Of Payment")]
        [Required]
        public IFormFile POP { get; set; }

        public bool IsSuccessfull { get; set; }
    }

    public class B05_AccountPayments_UpdatePaymentIDModel
    {
        public Data.BuildingCouncilDetails_InvoiceItem BuildingCouncilDetails_InvoiceItem { get; set; }
        public Data.BuildingCouncilDetails_Invoice BuildingCouncilDetails_Invoice { get; set; }

        [Display(Name = "Internal DB ID")]
        public int? PaymentID { get; set; }

        [Display(Name = "Skybill Document No")]
        [Required]
        public string SkybillDocumentNo { get; set; }

        public bool IsSuccess { get; set; }
        public bool AllowEdit { get; set; }
    }

    public class B05_SupplyPayments_PaymentForecastModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<CompanyItem> CompanyItems { get; set; }

        public List<B05_SupplyPayments_PaymentForecastPivotItem> B05_SupplyPayments_PaymentForecastPivotItemsSP { get; set; }
        public List<B05_SupplyPayments_PaymentForecastPivotItem> B05_SupplyPayments_PaymentForecastPivotItemsC { get; set; }
        public class B05_SupplyPayments_PaymentForecastPivotItem
        {
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public string AccountNo { get; set; }
            public string TaxInvoiceNo { get; set; }
            public int TaxInvoiceID { get; set; }
            public DateTime? TaxInvoiceDate { get; set; }
            public DateTime? FinalDateForPayment { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances { get; set; }

        }
    }

    public class B05_SupplyPayments_CashflowForecastModel
    {
        public DateTime NetcashDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<CompanyItem> CompanyItems { get; set; }

        public List<B05_SupplyPayments_CashflowForecastPivotItem> B05_SupplyPayments_CashflowForecastPivotItemsSP { get; set; }
        public List<B05_SupplyPayments_CashflowForecastPivotItem> B05_SupplyPayments_CashflowForecastPivotItemsC { get; set; }
        public class B05_SupplyPayments_CashflowForecastPivotItem
        {
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public string AccountNo { get; set; }
            public decimal NetcashClosingBalance { get; set; }
            public string TaxInvoiceNo { get; set; }
            public int TaxInvoiceID { get; set; }
            public DateTime? TaxInvoiceDate { get; set; }
            public DateTime? FinalDateForPayment { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances { get; set; }
            public decimal Diff
            {
                get
                {
                    if (ClosingBalances.Count > 0)
                        return NetcashClosingBalance - ClosingBalances.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }
    }

    public class B05_SupplyPayments_PaymentMatchingScheduleModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<CompanyItem> CompanyItems { get; set; }

        public List<B05_SupplyPayments_PaymentMatchingSchedulePivotItem> B05_SupplyPayments_PaymentMatchingSchedulePivotItemsSP { get; set; }
        public List<B05_SupplyPayments_PaymentMatchingSchedulePivotItem> B05_SupplyPayments_PaymentMatchingSchedulePivotItemsC { get; set; }
        public class B05_SupplyPayments_PaymentMatchingSchedulePivotItem
        {
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public string AccountNo { get; set; }
            public string TaxInvoiceNo { get; set; }
            public int TaxInvoiceID { get; set; }
            public DateTime? TaxInvoiceDate { get; set; }
            public DateTime? FinalDateForPayment { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances { get; set; }
            public List<KeyValuePair<DateTime, decimal>> Payments { get; set; }
            public List<KeyValuePair<DateTime, decimal>> NetcashPayments { get; set; }

        }
    }

}
