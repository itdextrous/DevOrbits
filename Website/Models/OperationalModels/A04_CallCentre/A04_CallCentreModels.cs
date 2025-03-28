using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Models.OperationalModels.A04_CallCentre
{
    public class A04_CallCentre_ContactsImportTo3CxModel
    {

    }
    public class A04_CallCentre_CallLogImportModel
    {
        [Required(ErrorMessage = "CSV File is required")]
        [Display(Name = "CSV Import File")]
        public IFormFile File { get; set; }

        public string CallID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool IsSuccessfull { get; set; }

        public List<A04_CallCentreLogItem> A04_CallCentreLogItems { get; set; }
        public class A04_CallCentreLogItem : A04_CallCentreLog
        {
            public string CompanyName { get; set; }
            public string CustomerName { get; set; }
            public string CallType { get; set; }
            public string FromUser { get; set; }
            public string ToUser { get; set; }
            public int? FlagID { get; set; }
            public List<int> A09_Flags_AttachmentsIDs { get; set; }
            public bool IsResolvedStatus { get { return CompanyID.HasValue && CustomerNo.HasValue && Zendesk_TicketField_OptionID.HasValue; } }
        }
        public F_SystemGeneratedReports_SQLJobs_CallLogSync_Request LatestRequest { get; set; }
        public class F_SystemGeneratedReports_SQLJobs_CallLogSync_Request : Data.F_SystemGeneratedReports_SQLJobs_CallLogSync_Request
        {
            public string CreatedByUsername { get; set; }
        }

        public List<SelectListItem> Zendesk_TicketField_Options { get; set; }
    }

    public class A04_CallCentre_CallLogSummaryPerPropertyModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }


        public List<A04_CallCentre_CallLogSummaryPerPropertyItem> CallsPerDay { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerPropertyItem> DistinctCallsPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerPropertyItem
        {
            public string CompanyName { get; set; }
            public int? CompanyID { get; set; }
            public Dictionary<DateTime, int?> Values { get; set; }
        }
        public List<A04_CallCentre_CallLogSummaryPerPropertyItemAVG> AVGCallsPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerPropertyItemAVG
        {
            public string CompanyName { get; set; }
            public int? CompanyID { get; set; }
            public Dictionary<DateTime, decimal?> Values { get; set; }
        }
    }

    public class A04_CallCentre_CallLogSummaryPerPropertyDurationModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }


        public List<A04_CallCentre_CallLogSummaryPerPropertyDurationItem> CallsDurationPerDay { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerPropertyDurationItem> CallsDurationPerAgent { get; set; }
        public class A04_CallCentre_CallLogSummaryPerPropertyDurationItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public Dictionary<DateTime, int?> Values { get; set; }
        }
        public List<A04_CallCentre_CallLogSummaryPerPropertyDurationItemAVG> CallsCostPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerPropertyDurationItemAVG
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public Dictionary<DateTime, decimal?> Values { get; set; }
        }
    }

    public class A04_CallCentre_CallLogSummaryPerQueryTypeCountModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<Data.Zendesk_TicketField_Option> Zendesk_TicketField_Options { get; set; }
        public List<SelectListItem> Companies { get; set; }
        public int? CompanyID { get; set; }
        public List<SelectListItem> Zendesk_TicketFields { get; set; }


        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItem> CallsPerDay { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItem> DistinctCallsPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerQueryTypeCountItem
        {
            public string CompanyName { get; set; }
            public int? CompanyID { get; set; }
            public long ZendeskTicketFieldID { get; set; }
            public string ZendeskTicketFieldName { get; set; }
            public Dictionary<DateTime, int?> Values { get; set; }
        }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG> AVGCallsPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG
        {
            public string CompanyName { get; set; }
            public int? CompanyID { get; set; }
            public long ZendeskTicketFieldID { get; set; }
            public string ZendeskTicketFieldName { get; set; }
            public Dictionary<DateTime, decimal?> Values { get; set; }
        }

        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItem> CallsPerDay_PerCompany { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItem> DistinctCallsPerDay_PerCompany { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG> AVGCallsPerDay_PerCompany { get; set; }
    }

    public class A04_CallCentre_CallLogSummaryPerQueryTypeModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<Data.Zendesk_TicketField_Option> Zendesk_TicketField_Options { get; set; }
        public List<SelectListItem> Companies { get; set; }
        public int? CompanyID { get; set; }
        public List<SelectListItem> Zendesk_TicketFields { get; set; }


        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItem> CallsDurationPerDay { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItem> CallsDurationPerAgent { get; set; }
        public class A04_CallCentre_CallLogSummaryPerQueryTypeItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public long ZendeskTicketFieldID { get; set; }
            public string ZendeskTicketFieldName { get; set; }
            public Dictionary<DateTime, int?> Values { get; set; }
        }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG> CallsCostPerDay { get; set; }
        public class A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public long ZendeskTicketFieldID { get; set; }
            public string ZendeskTicketFieldName { get; set; }
            public Dictionary<DateTime, decimal?> Values { get; set; }
        }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItem> CallsDurationPerDay_PerCompany { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItem> CallsDurationPerAgent_PerCompany { get; set; }
        public List<A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG> CallsCostPerDay_PerCompany { get; set; }
    }

    public class A04_CallCentre_SearchModel
    {
        [Display(Name = "Task ID")]
        public string TaskID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Display(Name = "From Operator")]
        public List<SelectListItem> FromOperator { get; set; }

        [Display(Name = "To Operator")]
        public List<SelectListItem> ToOperator { get; set; }

        [Display(Name = "Due Date From")]
        public DateTime? DueDateFrom { get; set; }

        [Display(Name = "Due Date To")]
        public DateTime? DueDateTo { get; set; }

        [Display(Name = "Resolved Status Type")]
        public List<SelectListItem> ResolvedStatusType { get; set; }

        public List<A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem> A04_CallCentreLogItems { get; set; }

        public List<SelectListItem> Zendesk_TicketField_Options { get; set; }
    }

    public class A04_CallCentre_RecordingsAllocationModel
    {
        [Display(Name = "Due Date From")]
        public DateTime? DueDateFrom { get; set; }

        [Display(Name = "Due Date To")]
        public DateTime? DueDateTo { get; set; }

        [Display(Name = "Due Date To")]
        public List<SelectListItem> Extensions { get; set; }

        public List<A04_CallCentre_RecordingsAllocationItem> A04_CallCentre_RecordingsAllocationItems { get; set; }

        public class A04_CallCentre_RecordingsAllocationItem : Data.A04_CallCentreLogs_Recording
        {
        }

        public List<A09_FlagsItem> A09_Flags { get; set; }
        public class A09_FlagsItem : SelectListItem
        {
            public DateTime TimeEnd { get; set; }
        }

    }
}
