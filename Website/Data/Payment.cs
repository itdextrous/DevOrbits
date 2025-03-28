using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Payment
    {

        public int PaymentID { get; set; }

        [Column(TypeName = "VARCHAR(500)")]
        public string UserID { get; set; }

        public DateTime CreateDate { get; set; }

        //PayGate specific fields
        public string Reference { get; set; }

        public string CardHolderIpAddr { get; set; }

        public string RequestTrace { get; set; }

        public string Extra1 { get; set; }

        public string Extra2 { get; set; }

        public string Extra3 { get; set; }

        public decimal Amount { get; set; }

        public int? PaymentStatusID { get; set; }
        public virtual PaymentStatus PaymentStatus { get; set; }

        public int? PaymentMethodID { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }

        public string Reason { get; set; }
        public bool? IsCompanyAdminRecharge { get; set; }
        public string SkybillCompanyName { get; set; }
        public string SkybillCustomerNo { get; set; }
        public decimal? SkybillFeeAmount { get; set; }
        public bool? FeeRequired { get; set; }
        public bool? Vending1Required { get; set; }
        public int? Vending1LogID { get; set; }
        public bool? Vending2Required { get; set; }
        public int? Vending2LogID { get; set; }
        public bool? Vending3Required { get; set; }
        public int? Vending3LogID { get; set; }
        public bool? Vending4Required { get; set; }
        public int? Vending4LogID { get; set; }
        public decimal? SkybillFeeAmount6810 { get; set; }
        public decimal? SkybillFeeAmount5611 { get; set; }
        public decimal? Vending1Amount7191 { get; set; }
        public decimal? Vending1Amount8640 { get; set; }
        public decimal? Vending1Amount5621 { get; set; }
        public decimal? Vending2Amount7191 { get; set; }
        public decimal? Vending2Amount8640 { get; set; }
        public decimal? Vending2Amount5621 { get; set; }
        public decimal? Vending3Amount7191 { get; set; }
        public decimal? Vending3Amount8640 { get; set; }
        public decimal? Vending3Amount5621 { get; set; }
        public decimal? Vending4Amount7191 { get; set; }
        public decimal? Vending4Amount8640 { get; set; }
        public decimal? Vending4Amount5621 { get; set; }
        public bool? IsOperationalRecharge { get; set; }
        public string Vending1SkybillCompanyName { get; set; }
        public decimal? Vending1Amount6810 { get; set; }
        public decimal? Vending1Amount5611 { get; set; }
        public string Vending2SkybillCompanyName { get; set; }
        public decimal? Vending2Amount6810 { get; set; }
        public decimal? Vending2Amount5611 { get; set; }
        public string Vending3SkybillCompanyName { get; set; }
        public decimal? Vending3Amount6810 { get; set; }
        public decimal? Vending3Amount5611 { get; set; }
        public string Vending4SkybillCompanyName { get; set; }
        public decimal? Vending4Amount6810 { get; set; }
        public decimal? Vending4Amount5611 { get; set; }
        public DateTime? SkybillCheckupDate { get; set; }
        public bool? IsMobile { get; set; }
    }

    public class NetcashStatement
    {
        [Key]
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string TransactionCode { get; set; }
        public int InternalDBID { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string InternalIndicator { get; set; }
        public string LedgerAccountAffected { get; set; }
        public DateTime DateSynced { get; set; }
        public int CompanyID { get; set; }
        public string SkybillDocumentNo { get; set; }
        public int? SkybillGLNo { get; set; }
        public int? PaymentID { get; set; }
        public string StatementReference { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public decimal? Balance { get; set; }
        public decimal? RealAmount { get; set; }

        public string TransactionCodeDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(TransactionCode))
                {
                    switch (TransactionCode)
                    {
                        case "OBL": return "Opening balance";
                        case "CBL": return "Closing balance";
                        case "DTT": return "Deposit Received";
                        case "DTR": return "Deposit Return";
                        case "NSF": return "Service fee";
                        case "BTR": return "Bank transfer to client";
                        case "BTU": return "Bank transfer return";
                        case "INP": return "Interest paid to Netcash";
                        case "INR": return "Interest received by Merchant";
                        case "ABR": return "Account balance recovery";
                        case "ABU": return "Account balance recovery return	Business";
                        case "VAT": return "Value added tax";
                        case "IAT": return "Inter-account transfer";
                        case "IST": return "Inter-system transfer";
                        case "BDW": return "Bad Debt Write-Off";
                        case "IPR": return "Interest paid to Netcash reversal";
                        case "IRR": return "Interest paid to Merchants reversal";
                        case "REB": return "Rebate";
                        case "BAR": return "Bank Account Redirect";
                        case "ELM": return "Electronic Mandate";
                        case "USI": return "Unallocated Statement Transaction In";
                        case "USO": return "Unallocated Statement Transaction Out";
                        case "INS": return "Interest swept";

                        case "TDD": return "2 Day debit order";
                        case "TDC": return "2 Day credit card";
                        case "DRU": return "Debit unpaid";
                        case "SDD": return "Same day debit order";
                        case "SDC": return "Same day credit card";
                        case "DCU": return "Debit order credit card unpaid";

                        case "CRP": return "Same day creditor payment";
                        case "CRU": return "Creditor payment return";
                        case "DCP": return "Dated creditor payment";

                        case "PNP": return "Retail payment";
                        case "PNQ": return "Retail payment return";
                        case "PNM": return "Scan to Pay payment";
                        case "PNW": return "Scan to Pay declined";
                        case "PNE": return "EFT payment";
                        case "PNZ": return "EFT return";
                        case "PNA": return "Credit Card authorize";
                        case "PNC": return "Credit Card payment";
                        case "PNU": return "Credit Card declined";
                        case "PND": return "Credit Card dispute";
                        case "PNR": return "Credit Card refund";
                        case "PIA": return "Ozow Auth";
                        case "PIS": return "Ozow Success";
                        case "PIF": return "Ozow Failure";
                        case "PIR": return "Ozow Recall";
                        case "PVC": return "Visa CheckOut Payment";
                        case "PVU": return "Visa CheckOut Decline";
                        case "PVR": return "Visa CheckOut Refund";
                        case "PVD": return "Visa CheckOut Dispute";
                        case "PQR": return "Masterpass QR";
                        case "PCD": return "Client Deposit";

                        case "CDR": return "Risk Reports report";
                        case "AVS": return "Account Verification Single";
                        case "AVB": return "Account Verification Bulk";
                    }
                }
                return "";
            }
        }

        public bool IsSystemTrans
        {
            get
            {
                if (!string.IsNullOrEmpty(TransactionCode))
                {
                    switch (TransactionCode)
                    {
                        case "PNA":
                        case "PIA":
                        case "PIF":
                        case "PNU":
                        case "PVU":
                            return true;
                    }
                }
                return false;
            }
        }

        public DateTime ReportingDate
        {
            get
            {
                if (!string.IsNullOrEmpty(TransactionCode))
                {
                    switch (TransactionCode)
                    {
                        case "NSF":
                        case "VAT":
                            return new DateTime(Date.AddMonths(-1).Year, Date.AddMonths(-1).Month, DateTime.DaysInMonth(Date.AddMonths(-1).Year, Date.AddMonths(-1).Month));
                    }
                }
                return Date;
            }
        }

    }

    public class NetcashManualPayment
    {
        [Key]
        public int ID { get; set; }
        public int NetcashStatementID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public DateTime DateCreated { get; set; }
        public bool? Approved { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? SkybillJournalLogID { get; set; }
        public string SkybillCompanyName { get; set; }
        public string SkybillCustomerNo { get; set; }
        public bool? Vending1Required { get; set; }
        public int? Vending1LogID { get; set; }
        public decimal? Vending1Amount7191 { get; set; }
        public decimal? Vending1Amount8640 { get; set; }
        public decimal? Vending1Amount5621 { get; set; }
        public DateTime? SkybillCheckupDate { get; set; }
        public int? RuleID { get; set; }
    }

    public class NetcashManualPaymentRule
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public string NetcashDescription { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsDeleted { get; set; }
    }
}
