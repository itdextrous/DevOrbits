using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C02_SageLedgerReportModels
{
    public class C02_SageLedgerReport_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_SageLedgerReport_SummaryItem> C02_SageLedgerReport_SummaryItems { get; set; }

        public List<Data.Company> Companies { get; set; }

        public class C02_SageLedgerReport_SummaryItem : Data.Company
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public List<SelectListItem> MovementReport { get; set; }
            public string CompanyName { get; set; }
            public string TableRowID { get; set; }

            public List<C02_SageLedgerReport_SummarySubItem> C02_SageLedgerReport_SummarySubItems { get; set; }
            public class C02_SageLedgerReport_SummarySubItem
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

    public class C02_SageLedgerReport_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<SelectListItem> ParentReportingCategory { get; set; }

        public List<C02_SageLedgerReport_MonthlyItem> C02_SageLedgerReport_MonthlyItems_BalanceSheet { get; set; }
        public List<C02_SageLedgerReport_MonthlyItem> C02_SageLedgerReport_MonthlyItems_IncomeStatement { get; set; }

        public class C02_SageLedgerReport_MonthlyItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string CompanyName { get; set; }
            public string CategoryName { get; set; }
            public string ReportingCategory { get; set; }
            public string ReportingDescription { get; set; }
            public string ParentReportingCategoryID { get; set; }

            public List<C02_SageLedgerReport_MonthlySubItem> C02_SageLedgerReport_MonthlySubItems { get; set; }
            public class C02_SageLedgerReport_MonthlySubItem
            {
                public DateTime Month { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
                public string ToolTip { get; set; }
            }

        }
    }

    public class C02_SageLedgerReport_MonthlySummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_SageLedgerReport_MonthlySummaryItem> C02_SageLedgerReport_MonthlySummaryItems_BalanceSheet { get; set; }
        public List<C02_SageLedgerReport_MonthlySummaryItem> C02_SageLedgerReport_MonthlySummaryItems_IncomeStatement { get; set; }

        public class C02_SageLedgerReport_MonthlySummaryItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string CategoryName { get; set; }
            public string CompanyName { get; set; }
            public string ReportingCategory { get; set; }
            public string ReportingDescription { get; set; }
            public string ParentReportingCategoryID { get; set; }

            public List<C02_SageLedgerReport_MonthlySummarySubItem> C02_SageLedgerReport_MonthlySummarySubItems { get; set; }
            public class C02_SageLedgerReport_MonthlySummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
                public string ToolTip { get; set; }
            }

        }
    }

    public class C02_SageLedgerReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string JournalNo { get; set; }
        public string JournalName { get; set; }
        public List<SelectListItem> TransactionType { get; set; }

        public List<C02_SageLedgerReport_DetailsItem> C02_SageLedgerReport_DetailsItems { get; set; }

        public class C02_SageLedgerReport_DetailsItem : MyVoltage.Data.SageAccounting_DetailedLedgerTransaction
        {
            public string CompanyName { get; set; }
        }
    }

    public class C02_SageLedgerReport_ConsolidatedTBModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<C02_SageLedgerReport_ConsolidatedTBItem> C02_SageLedgerReport_ConsolidatedTBItems_BalanceSheet { get; set; }
        public List<C02_SageLedgerReport_ConsolidatedTBItem> C02_SageLedgerReport_ConsolidatedTBItems_IncomeStatement { get; set; }
        public Dictionary<int, string> Companies { get; set; }

        [Display(Name = "Partner")]
        [Required]
        public List<SelectListItem> PartnerID { get; set; }

        public class C02_SageLedgerReport_ConsolidatedTBItem : MyVoltage.Data.GeneralJournal.GeneralJournalLedgerType
        {
            public List<C02_SageLedgerReport_ConsolidatedTBSubItem> C02_SageLedgerReport_ConsolidatedTBSubItems { get; set; }
            public class C02_SageLedgerReport_ConsolidatedTBSubItem
            {
                public int CompanyID { get; set; }
                public string CompanyName { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
            }

        }
    }

    public class C02_SageLedgerReport_GLAuditViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> LedgerNo { get; set; }
        public List<SelectListItem> ApprovalRequired { get; set; }
        public List<SelectListItem> PendingIssue { get; set; }
        public List<SelectListItem> Classification { get; set; }
        public F_SystemGeneratedReports_AccountingChecklist_Request LatestRequest { get; set; }

        public class F_SystemGeneratedReports_AccountingChecklist_Request : Data.F_SystemGeneratedReports_AccountingChecklist_Request
        {
            public string CreatedByUsername { get; set; }
        }

        public List<C02_SageLedgerReport_GLAuditViewItem> C02_SageLedgerReport_GLAuditViewItems { get; set; }

        public class C02_SageLedgerReport_GLAuditViewItem : MyVoltage.Data.AccountingChecklist
        {
            public string CompanyName { get; set; }
            public string ReviewedByUsername { get; set; }
            public string ReviewedByUsernameBalance { get; set; }
            public string ApprovedByUsername { get; set; }
            public string ApprovedByUsernameBalance { get; set; }
            public string AuditByUsername { get; set; }
            public string AuditByUsernameBalance { get; set; }
        }
    }

    public class C02_SageLedgerReport_GLAuditUpdateModel
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
        public C02_SageLedgerReport_GLAuditUpdate C02_SageLedgerReport_GLAuditUpdateItem { get; set; }

        public class C02_SageLedgerReport_GLAuditUpdate : MyVoltage.Data.AccountingChecklist
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
