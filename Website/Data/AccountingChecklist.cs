using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class AccountingChecklist
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public DateTime Date { get; set; }
        public int LedgerNo { get; set; }
        public string LedgerName { get; set; }
        public decimal MovementAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal? MovementAmountConfirmed { get; set; }
        public decimal? BalanceAmountConfirmed { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string PendingIssue { get; set; }
        public string Comments { get; set; }
        public int? FlagID { get; set; }
        public int? TaskID { get; set; }
        public DateTime MovementSyncDate { get; set; }
        public DateTime BalanceSyncDate { get; set; }
        public string ReviewedByBalance { get; set; }
        public DateTime? ReviewedDateBalance { get; set; }
        public string ApprovedByBalance { get; set; }
        public DateTime? ApprovedDateBalance { get; set; }
        public string AttachmentFileName { get; set; }
        public DateTime? LastCheckedDate { get; set; }
        public string AuditBy { get; set; }
        public DateTime? AuditDate { get; set; }
        public string AuditByBalance { get; set; }
        public DateTime? AuditDateBalance { get; set; }
        public string AuditComments { get; set; }
        public int? SageCompanyID { get; set; }
        public long? SageJournalID { get; set; }
    }
}
