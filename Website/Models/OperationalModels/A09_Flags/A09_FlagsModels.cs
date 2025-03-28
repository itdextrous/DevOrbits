using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Data.A09_Flags;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A09_Flags.A09_FlagsModels
{
    public class A09_Flags_Company_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A09_Flags_Company_SummaryStatusItem> A09_Flags_Company_SummaryStatusItems { get; set; }
        public class A09_Flags_Company_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }
        public List<A09_Flags_Company_SummaryItem> A09_Flags_CompanySummaryItems { get; set; }
        public class A09_Flags_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }


            public List<A09_Flags_Company_SummaryItemStatus> A09_Flags_Company_SummaryItemStatuses { get; set; }
            public class A09_Flags_Company_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
                public int StatusGroupID { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A09_Flags_Company_SummaryItemStatuses != null && A09_Flags_Company_SummaryItemStatuses.Count > 0)
                        return A09_Flags_Company_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedFlagCreateDate { get; set; }
            public int? OldestUnresolvedFlagID { get; set; }
            public int? OldestUnresolvedFlagTypeID { get; set; }
        }

    }

    public class A09_Flags_CompanyReviewModel
    {
        public A09_Flags_TypeItem FlagType { get; set; }
        public class A09_Flags_TypeItem : Data.A09_Flags.A09_Flags_Type
        {
            public string WorkflowGroupName { get; set; }
            public string BusinessDepartmentName { get; set; }
            public string BusinessPillarName { get; set; }
            public string AssignedToUsername { get; set; }
            public string ReportingToUserUsername { get; set; }
            public Data.SiteAdmin_Priority FlagPriority { get; set; }
            public A09_Flag_TypeStatus A09_Flag_TypeStatus { get; set; }
            public Data.SiteAdmin_StatusGroup StatusGroup { get; set; }
        }

        public Data.A09_Flags.A09_Flag Flag { get; set; }
        public Data.SiteAdmin_Priority FlagPriority { get; set; }

        public string AssignedToUsername { get; set; }
        public List<SelectListItem> AvailableUsers { get; set; }
        public string ReportingToUserUsername { get; set; }
        public List<SelectListItem> ReportingToUsers { get; set; }

        public List<A09_Flags_ReassignLogItem> A09_Flags_ReassignLogItems { get; set; }
        public A09_FlagStatus A09_FlagStatusItem { get; set; }
        public class A09_FlagStatus : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public A09_FlagMeetingAgenda A09_FlagMeetingAgendaItem { get; set; }
        public class A09_FlagMeetingAgenda : Data.SiteAdmin_MeetingAgenda
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A09_Flag_TypeStatus> A09_Flag_TypeStatuses { get; set; }
        public class A09_Flag_TypeStatus : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A09_Flag_TypeMeetingAgenda> A09_Flag_TypeMeetingAgendaes { get; set; }
        public class A09_Flag_TypeMeetingAgenda : Data.SiteAdmin_MeetingAgenda
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public class A09_Flags_ReassignLogItem : Data.A09_Flags.A09_Flags_ReassignLog
        {
            public string ReassignedByUsername { get; set; }
            public string ReassignedToUsername { get; set; }
            public A09_FlagStatus Status { get; set; }
        }
        public string CustomName { get; set; }

        public List<A09_Flag_Review_AttachmentItem> A09_Flag_Review_AttachmentItems { get; set; }
        public class A09_Flag_Review_AttachmentItem : Data.A09_Flags.A09_Flags_Attachment
        {
            public string Username { get; set; }
        }
        public List<Module_TimeOfWorkPlanned> Module_TimeOfWorkPlanneds { get; set; }
        public class Module_TimeOfWorkPlanned : Data.Module_TimeOfWorkPlanned
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public TimeSpan TimePlanned { get { return TimeSpan.FromMinutes(MinOfWorkPlanned); } }
        }
        public List<Module_TimeOfWorkAllocated> Module_TimeOfWorkAllocateds { get; set; }
        public class Module_TimeOfWorkAllocated : Data.Module_TimeOfWorkAllocated
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public TimeSpan TimeAllocated { get { return TimeSpan.FromMinutes(MinOfWorkAllocated); } }
        }
        public List<Module_TravelAllocation> Module_TravelAllocations { get; set; }
        public class Module_TravelAllocation : Data.Module_TravelAllocation
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public Data.Vehicle Vehicle { get; set; }
        }
        public List<Module_StockAllocation> Module_StockAllocations { get; set; }
        public class Module_StockAllocation : Data.Module_StockAllocation
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
        }
        public List<Module_InvoiceAllocation> Module_InvoiceAllocations { get; set; }
        public class Module_InvoiceAllocation : Data.Module_InvoiceAllocation
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
        }
        public List<Module_NonCompliance> Module_NonCompliances { get; set; }
        public class Module_NonCompliance : Data.Module_NonCompliance
        {
            public string UserCommentUsername { get; set; }
            public string EnforcerCommentUsername { get; set; }
            public string ResponsibleUsername { get; set; }
        }
        //public List<A09_Flags_ResponsiblePersonItem> A09_Flags_ResponsiblePeople { get; set; }
        //public class A09_Flags_ResponsiblePersonItem : Data.A09_Flags.A09_Flags_ResponsiblePerson
        //{
        //    public string PersonUsername { get; set; }
        //}
    }

    public class A09_Flags_User_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A09_Flags_User_SummaryStatusItem> A09_Flags_User_SummaryStatusItems { get; set; }
        public class A09_Flags_User_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A09_Flags_User_SummaryItem> A09_Flags_UserSummaryItems { get; set; }
        public class A09_Flags_User_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }

            public List<A09_Flags_User_SummaryItemStatus> A09_Flags_User_SummaryItemStatuses { get; set; }
            public class A09_Flags_User_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
                public int StatusGroupID { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A09_Flags_User_SummaryItemStatuses != null && A09_Flags_User_SummaryItemStatuses.Count > 0)
                        return A09_Flags_User_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedFlagCreateDate { get; set; }
            public int? OldestUnresolvedFlagID { get; set; }
            public int? OldestUnresolvedFlagTypeID { get; set; }

            public Data.A09_Flags.A09_Flag LastTouchedFlag { get; set; }
        }

    }

    public class A09_Flags_Type_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A09_Flags_Type_SummaryStatusItem> A09_Flags_Type_SummaryStatusItems { get; set; }
        public class A09_Flags_Type_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A09_Flags_Type_SummaryItem> A09_Flags_TypeSummaryItems { get; set; }
        public class A09_Flags_Type_SummaryItem
        {
            public string TypeName { get; set; }
            public int TypeID { get; set; }

            public List<A09_Flags_Type_SummaryItemStatus> A09_Flags_Type_SummaryItemStatuses { get; set; }
            public class A09_Flags_Type_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
                public int StatusGroupID { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A09_Flags_Type_SummaryItemStatuses != null && A09_Flags_Type_SummaryItemStatuses.Count > 0)
                        return A09_Flags_Type_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedFlagCreateDate { get; set; }
            public int? OldestUnresolvedFlagID { get; set; }
            public int? OldestUnresolvedFlagTypeID { get; set; }

            public Data.A09_Flags.A09_Flag LastTouchedFlag { get; set; }
        }

    }

    public class A09_Flags_CreateModel
    {
        public A09_FlagStatus A09_FlagStatusItem { get; set; }
        public class A09_FlagStatus : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        [Display(Name = "Flag Type")]
        public List<SelectListItem> FlagType { get; set; }

        [Display(Name = "Assign To")]
        public List<SelectListItem> AssignTo { get; set; }

        [Display(Name = "Gateway ID (Gateway related flags)")]
        public string GatewayID { get; set; }

        [Display(Name = "Device Serial (Device/Customer related flags)")]
        public string DeviceSerial { get; set; }

        [Display(Name = "Customer Number (Device/Customer related flags)")]
        public string CustomerNumber { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Reason for flag")]
        [Required]
        public string ReasonForFlag { get; set; }
    }

    public class A09_Flag_Review_AddAttachmentModel
    {
        [Required]
        [Display(Name = "Description of File")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Browse a file to attach to task")]
        public IFormFile Attachment { get; set; }

        [Required]
        [Display(Name = "AttachmentType")]
        public List<SelectListItem> AttachmentType { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class A09_Flags_SearchModel
    {
        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Flag ID")]
        public string FlagID { get; set; }

        [Display(Name = "Flag Type")]
        public List<SelectListItem> FlagType { get; set; }

        [Display(Name = "Flag Type Name")]
        public string Heading { get; set; }

        [Display(Name = "Reason For Flag")]
        public string Description { get; set; }

        [Display(Name = "Linked To (e.g. Zendesk Ticket Number, Gateway ID, Device Serial etc)")]
        public string LinkedTo { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Due Date From")]
        public DateTime? DueDateFrom { get; set; }

        [Display(Name = "Due Date To")]
        public DateTime? DueDateTo { get; set; }

        [Display(Name = "Status Group")]
        public List<SelectListItem> StatusGroup { get; set; }

        [Display(Name = "Default Status")]
        public List<SelectListItem> DefaultStatus { get; set; }

        public int? DefaultStatusID { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int StatusGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        [Display(Name = "ResolvedStatusType")]
        public List<SelectListItem> ResolvedStatusType { get; set; }

        public List<A09_Flags_Type_DetailsItem> A09_Flags_TypeDetailsItems { get; set; }
        public class A09_Flags_Type_DetailsItem : Data.A09_Flags.A09_Flag
        {
            public string AssignedToUsername { get; set; }
            public string LatestComment { get; set; }
            public string ReportingToUserUsername { get; set; }
            public string CompanyName { get; set; }

            public A09_Flags_Type_DetailsItemStatus Status { get; set; }
            public class A09_Flags_Type_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class A09_Flags_Type : Data.A09_Flags.A09_Flags_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }
            public A09_Flags_Type A09_Flags_TypeItem { get; set; }
            public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            public string LastViewedBy { get; set; }
            //public List<A09_Flags_ResponsiblePersonItem> A09_Flags_ResponsiblePeople { get; set; }

            //public class A09_Flags_ResponsiblePersonItem : Data.A09_Flags.A09_Flags_ResponsiblePerson
            //{
            //    public string PersonTypename { get; set; }
            //}
        }
    }

    public class A09_Flags_HeatmapModel
    {
        public class WorkflowGroupGrandParentItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<WorkflowGroupGrandParentItem> WorkflowGroupGrandParentItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupGrandParentID { get; set; }

        [Display(Name = "Due Date From")]
        public DateTime DueDateFrom { get; set; }

        [Display(Name = "Due Date To")]
        public DateTime DueDateTo { get; set; }

        public List<A09_Flags_Type> A09_Flags_Types { get; set; }
        public List<A09_Flags_HeatmapItem> A09_Flags_CompanyDetailsItems { get; set; }
        public class A09_Flags_HeatmapItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public List<A09_Flags_HeatmapSubItem> A09_Flags_HeatmapSubItems { get; set; }
            public class A09_Flags_HeatmapSubItem
            {
                public string TaskTypeShortName { get; set; }
                public int TaskTypeID { get; set; }
                public int UnresolvedCount { get; set; }
                public int ResolvedCount { get; set; }
                public int TotalCount { get { return UnresolvedCount + ResolvedCount; } }
            }
        }
    }
}
