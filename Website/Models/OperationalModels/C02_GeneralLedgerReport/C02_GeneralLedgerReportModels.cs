using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C02_GeneralLedgerReportModels
{
    public class C02_GeneralLedgerReport_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_GeneralLedgerReport_SummaryItem> C02_GeneralLedgerReport_SummaryItems { get; set; }

        public class C02_GeneralLedgerReport_SummaryItem : Data.Company
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public List<SelectListItem> MovementReport { get; set; }
            public string CompanyName { get; set; }
            public string TableRowID { get; set; }

            public List<C02_GeneralLedgerReport_SummarySubItem> C02_GeneralLedgerReport_SummarySubItems { get; set; }
            public class C02_GeneralLedgerReport_SummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal BalanceSheet { get; set; }
                public int BalanceSheetCount { get; set; }
                public int BalanceSheetCompleted { get; set; }
                public DateTime? BalanceLatestCompleted { get; set; }
                public decimal IncomeStatement { get; set; }
                public int IncomeStatementCount { get; set; }
                public int IncomeStatementCompleted { get; set; }
                public DateTime? IncomeStatementLatestCompleted { get; set; }
                public decimal Total { get; set; }
            }

        }
    }

    public class C02_GeneralLedgerReport_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_GeneralLedgerReport_MonthlyItem> C02_GeneralLedgerReport_MonthlyItems_BalanceSheet { get; set; }
        public List<C02_GeneralLedgerReport_MonthlyItem> C02_GeneralLedgerReport_MonthlyItems_IncomeStatement { get; set; }

        public class C02_GeneralLedgerReport_MonthlyItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public List<C02_GeneralLedgerReport_MonthlySubItem> C02_GeneralLedgerReport_MonthlySubItems { get; set; }
            public class C02_GeneralLedgerReport_MonthlySubItem
            {
                public DateTime Month { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
                public string ToolTip { get; set; }
            }

        }
    }

    public class C02_GeneralLedgerReport_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_GeneralLedgerReport_DailyItem> C02_GeneralLedgerReport_DailyItems_BalanceSheet { get; set; }
        public List<C02_GeneralLedgerReport_DailyItem> C02_GeneralLedgerReport_DailyItems_IncomeStatement { get; set; }

        public class C02_GeneralLedgerReport_DailyItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public List<C02_GeneralLedgerReport_DailySubItem> C02_GeneralLedgerReport_DailySubItems { get; set; }
            public class C02_GeneralLedgerReport_DailySubItem
            {
                public DateTime Month { get; set; }
                public decimal? Amount { get; set; }
            }

        }
    }

    public class C02_GeneralLedgerReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string JournalNo { get; set; }
        public string JournalName { get; set; }
        public List<SelectListItem> TransactionType { get; set; }

        public List<C02_GeneralLedgerReport_DetailsItem> C02_GeneralLedgerReport_DetailsItems { get; set; }

        public class C02_GeneralLedgerReport_DetailsItem : MyVoltage.Data.GeneralLedgerEntry
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
        }
    }

    public class C02_GeneralLedgerReport_ConsolidatedTBModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_GeneralLedgerReport_ConsolidatedTBItem> C02_GeneralLedgerReport_ConsolidatedTBItems_BalanceSheet { get; set; }
        public List<C02_GeneralLedgerReport_ConsolidatedTBItem> C02_GeneralLedgerReport_ConsolidatedTBItems_IncomeStatement { get; set; }
        public Dictionary<int, string> Companies { get; set; }

        [Display(Name = "Partner")]
        [Required]
        public List<SelectListItem> PartnerID { get; set; }

        public class C02_GeneralLedgerReport_ConsolidatedTBItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public List<C02_GeneralLedgerReport_ConsolidatedTBSubItem> C02_GeneralLedgerReport_ConsolidatedTBSubItems { get; set; }
            public class C02_GeneralLedgerReport_ConsolidatedTBSubItem
            {
                public int CompanyID { get; set; }
                public string CompanyName { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
            }

        }
    }

    public class C02_GeneralLedgerReport_GLAuditViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> LedgerNo { get; set; }
        public List<SelectListItem> ApprovalRequired { get; set; }
        public List<SelectListItem> PendingIssue { get; set; }
        public List<SelectListItem> Classification { get; set; }
        public List<SelectListItem> AmountClassification { get; set; }
        public F_SystemGeneratedReports_AccountingChecklist_Request Latest_AccountingChecklist_Request { get; set; }
        public class F_SystemGeneratedReports_AccountingChecklist_Request : Data.F_SystemGeneratedReports_AccountingChecklist_Request
        {
            public string CreatedByUsername { get; set; }
        }
        public SageAccounting_JournalRequests_AccountingChecklistID_Request Latest_Journal_Request { get; set; }
        public class SageAccounting_JournalRequests_AccountingChecklistID_Request : Data.F_SystemGeneratedReports_SageAccounting_JournalRequest
        {
            public string CreatedByUsername { get; set; }
        }

        public List<C02_GeneralLedgerReport_GLAuditViewItem> C02_GeneralLedgerReport_GLAuditViewItems { get; set; }

        public class C02_GeneralLedgerReport_GLAuditViewItem : MyVoltage.Data.AccountingChecklist
        {
            public string CompanyName { get; set; }
            public string ReviewedByUsername { get; set; }
            public string ReviewedByUsernameBalance { get; set; }
            public string ApprovedByUsername { get; set; }
            public string ApprovedByUsernameBalance { get; set; }
            public string AuditByUsername { get; set; }
            public string AuditByUsernameBalance { get; set; }
            public string SageCompanyName { get; set; }
            public bool ShowJournalLink { get; set; }
            public decimal? SageAmount { get; set; }
        }
    }

    public class C02_GeneralLedgerReport_GLAuditUpdateModel
    {
        [Display(Name = "Confirmed Movement Amount")]
        public decimal? ConfirmedMovementAmount { get; set; }

        [Display(Name = "Confirmed Balance Amount")]
        public decimal? ConfirmedBalanceAmount { get; set; }

        [Display(Name = "Pending Issue")]
        public string PendingIssue { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "Audit Comments")]
        public string AuditComments { get; set; }

        [Display(Name = "Flag ID")]
        public int? FlagID { get; set; }

        [Display(Name = "Task ID")]
        public int? TaskID { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile Attachment { get; set; }

        public string JobTitle { get; set; }
        public C02_GeneralLedgerReport_GLAuditUpdate C02_GeneralLedgerReport_GLAuditUpdateItem { get; set; }

        public class C02_GeneralLedgerReport_GLAuditUpdate : MyVoltage.Data.AccountingChecklist
        {
            public string CompanyName { get; set; }
            public string ApprovedByUsername { get; set; }
            public string ReviewedByUsername { get; set; }
            public string ApprovedByUsernameBalance { get; set; }
            public string ReviewedByUsernameBalance { get; set; }
            public string AuditByUsername { get; set; }
            public string AuditByUsernameBalance { get; set; }
        }
    }

}
