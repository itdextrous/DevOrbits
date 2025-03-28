using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A08_Tasks.A08_TasksModels
{
    public class A08_Tasks_Company_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_Company_SummaryStatusItem> A08_Tasks_Company_SummaryStatusItems { get; set; }
        public class A08_Tasks_Company_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A08_Tasks_Company_SummaryItem> A08_Tasks_CompanySummaryItems { get; set; }
        public class A08_Tasks_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public List<A08_Tasks_Company_SummaryItemStatus> A08_Tasks_Company_SummaryItemStatuses { get; set; }
            public class A08_Tasks_Company_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
                public int StatusGroupID { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A08_Tasks_Company_SummaryItemStatuses != null && A08_Tasks_Company_SummaryItemStatuses.Count > 0)
                        return A08_Tasks_Company_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTaskCreateDate { get; set; }
            public int? OldestUnresolvedTaskID { get; set; }
            public int? OldestUnresolvedTaskTypeID { get; set; }

        }

    }

    public class A08_Tasks_Company_DetailsModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_Company_DetailsItem> A08_Tasks_CompanyDetailsItems { get; set; }

        public class A08_Tasks_Company_DetailsItem : Data.A08_Task
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LatestComment { get; set; }
            public A08_Task_Type A08_Tasks_TypeItem { get; set; }

            public A08_Tasks_Company_DetailsItemStatus Status { get; set; }
            public class A08_Tasks_Company_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<A08_Tasks_ResponsiblePersonItem> A08_Tasks_ResponsiblePeople { get; set; }
            //public class A08_Tasks_ResponsiblePersonItem : Data.A08_Tasks.A08_Tasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class A08_Tasks_User_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_User_SummaryStatusItem> A08_Tasks_User_SummaryStatusItems { get; set; }
        public class A08_Tasks_User_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A08_Tasks_User_SummaryItem> A08_Tasks_UserSummaryItems { get; set; }
        public class A08_Tasks_User_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }

            public List<A08_Tasks_User_SummaryItemStatus> A08_Tasks_User_SummaryItemStatuses { get; set; }
            public class A08_Tasks_User_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
                public int StatusGroupID { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A08_Tasks_User_SummaryItemStatuses != null && A08_Tasks_User_SummaryItemStatuses.Count > 0)
                        return A08_Tasks_User_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTaskCreateDate { get; set; }
            public int? OldestUnresolvedTaskID { get; set; }
            public int? OldestUnresolvedTaskTypeID { get; set; }

            //public Data.A08_Task.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.A08_Task.StatusEnum.Completed;

            //        return Data.A08_Task.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class A08_Tasks_User_DetailsModel
    {
        public string UserName { get; set; }
        public bool TaskResponsibleUser { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_User_DetailsItem> A08_Tasks_UserDetailsItems { get; set; }

        public class A08_Tasks_User_DetailsItem : Data.A08_Task
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LatestComment { get; set; }

            public A08_Tasks_User_DetailsItemStatus Status { get; set; }
            public class A08_Tasks_User_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public A08_Task_Type A08_Tasks_TypeItem { get; set; }

            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<A08_Tasks_ResponsiblePersonItem> A08_Tasks_ResponsiblePeople { get; set; }
            //public class A08_Tasks_ResponsiblePersonItem : Data.A08_Tasks.A08_Tasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class A08_Tasks_Type_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_Type_SummaryStatusItem> A08_Tasks_Type_SummaryStatusItems { get; set; }
        public class A08_Tasks_Type_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<A08_Tasks_Type_SummaryItem> A08_Tasks_TypeSummaryItems { get; set; }
        public class A08_Tasks_Type_SummaryItem
        {
            public int TypeID { get; set; }
            public string TypeName { get; set; }

            public List<A08_Tasks_Type_SummaryItemStatus> A08_Tasks_Type_SummaryItemStatuses { get; set; }
            public class A08_Tasks_Type_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int StatusGroupID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (A08_Tasks_Type_SummaryItemStatuses != null && A08_Tasks_Type_SummaryItemStatuses.Count > 0)
                        return A08_Tasks_Type_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTaskCreateDate { get; set; }
            public int? OldestUnresolvedTaskID { get; set; }
            public int? OldestUnresolvedTaskTypeID { get; set; }

            //public Data.A08_Task.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.A08_Task.StatusEnum.Completed;

            //        return Data.A08_Task.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class A08_Tasks_Type_DetailsModel
    {
        public string TypeName { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<A08_Tasks_Type_DetailsItem> A08_Tasks_TypeDetailsItems { get; set; }

        public class A08_Tasks_Type_DetailsItem : Data.A08_Task
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LatestComment { get; set; }

            public A08_Tasks_Type_DetailsItemStatus Status { get; set; }
            public class A08_Tasks_Type_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public A08_Task_Type A08_Tasks_TypeItem { get; set; }

            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<A08_Tasks_ResponsiblePersonItem> A08_Tasks_ResponsiblePeople { get; set; }
            //public class A08_Tasks_ResponsiblePersonItem : Data.A08_Tasks.A08_Tasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class A08_Task_ReviewModel
    {
        public class WorkflowGroupItem
        {
            public int ID { get; set; }
            public int BusinessDepartmentID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<WorkflowGroupItem> WorkflowGroupItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupID { get; set; }

        public class BusinessPillarItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<BusinessPillarItem> BusinessPillarItems { get; set; }
        [Display(Name = "Business Pillar")]
        public List<SelectListItem> BusinessPillar { get; set; }

        public class BusinessDepartmentItem
        {
            public int ID { get; set; }
            public int BusinessPillarID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<BusinessDepartmentItem> BusinessDepartmentItems { get; set; }
        [Display(Name = "Business Department")]
        public List<SelectListItem> BusinessDepartment { get; set; }

        [Display(Name = "WrikeID")]
        public string WrikeID { get; set; }

        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        //[Required]
        [Display(Name = "WrikeCustomStatus")]
        public string WrikeCustomStatus { get; set; }

        //[Required]
        [Display(Name = "Add Comment")]
        public string Comments { get; set; }

        //[Required]
        //[Display(Name = "Status")]
        //public List<SelectListItem> Status { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Required]
        [ValidateDateRange]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Display(Name = "Meter Serial Number")]
        public string MeterSerialNumber { get; set; }

        public List<SelectListItem> NotificationsActive { get; set; }
        [Required]
        [Display(Name = "Notifications Active")]
        public bool NotificationData { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        public A08_Task_ReviewItem A08_Task { get; set; }

        public class A08_Task_ReviewItem : Data.A08_Task
        {
            public string WorkflowGroupName { get; set; }
            public string BusinessDepartmentName { get; set; }
            public string BusinessPillarName { get; set; }

            public A08_TaskStatus A08_TaskStatusItem { get; set; }
            public class A08_TaskStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public A08_TaskMeetingAgenda A08_TaskMeetingAgendaItem { get; set; }
            public class A08_TaskMeetingAgenda : Data.SiteAdmin_MeetingAgenda
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public A08_Task_Type A08_Tasks_TypeItem { get; set; }
            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public A08_Task_TypeStatus A08_Task_TypeStatusItem { get; set; }
                public List<A08_Task_TypeStatus> A08_Task_TypeStatuses { get; set; }
                public class A08_Task_TypeStatus : Data.SiteAdmin_Status
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
                public A08_Task_TypeMeetingAgenda A08_Task_TypeMeetingAgendaItem { get; set; }
                public List<A08_Task_TypeMeetingAgenda> A08_Task_TypeMeetingAgendaes { get; set; }
                public class A08_Task_TypeMeetingAgenda : Data.SiteAdmin_MeetingAgenda
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
                public string ReportingToUserUsername { get; set; }
                public string ResponsibleUserUsername { get; set; }
                public string WorkflowGroupName { get; set; }
                public string BusinessDepartmentName { get; set; }
                public string BusinessPillarName { get; set; }
                public string StatusGroupName { get; set; }
                public string MeetingAgendaGroupName { get; set; }
                public int FrequenciesCount { get; set; }
            }
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public List<A08_Task_Review_ReassignLogItem> A08_Task_Review_ReassignLogItems { get; set; }
            public class A08_Task_Review_ReassignLogItem : Data.A08_Tasks_ReassignLog
            {
                public string Username { get; set; }
            }
            public List<A08_Task_Review_AttachmentItem> A08_Task_Review_AttachmentItems { get; set; }
            public class A08_Task_Review_AttachmentItem : Data.A08_Tasks_Attachment
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
                public TimeSpan TimeAllocated { get { return (EndTime.HasValue ? EndTime.Value : DateTime.Now) - (StartTime.HasValue ? StartTime.Value : DateTime.Now); } }
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
        }
    }

    public class ValidateDateRange : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // your validation logic
            if (Convert.ToDateTime(value).Date <= DateTime.Now.AddDays(8).Date)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Date may not be more than 8 days in the future.");
            }
        }
    }

    public class A08_Task_CreateModel
    {
        [Required]
        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Required]
        [Display(Name = "Task Type")]
        public List<SelectListItem> TaskType { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        //[ValidateDateRange]
        public DateTime DueDate { get; set; }

        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Display(Name = "Meter Serial Number")]
        public string MeterSerialNumber { get; set; }

        //[Display(Name = "Km Travel Required")]
        //public int? KmTravelRequired { get; set; }

        //[Display(Name = "Stock Used")]
        //public string StockUsed { get; set; }

        [Required]
        [Display(Name = "Notifications Active")]
        public List<SelectListItem> NotificationsActive { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int TaskTypeID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }
        public List<Data.A08_Task_Type> A08_Task_Types { get; set; }

        public class WorkflowGroupItem
        {
            public int ID { get; set; }
            public int BusinessDepartmentID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<WorkflowGroupItem> WorkflowGroupItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupID { get; set; }

        public class BusinessPillarItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<BusinessPillarItem> BusinessPillarItems { get; set; }
        [Display(Name = "Business Pillar")]
        public List<SelectListItem> BusinessPillar { get; set; }

        public class BusinessDepartmentItem
        {
            public int ID { get; set; }
            public int BusinessPillarID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<BusinessDepartmentItem> BusinessDepartmentItems { get; set; }
        [Display(Name = "Business Department")]
        public List<SelectListItem> BusinessDepartment { get; set; }
    }

    public class A08_Task_Review_AddAttachmentModel
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

    public class A08_Tasks_SearchModel
    {
        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Task ID")]
        public string TaskID { get; set; }

        [Display(Name = "Task Type")]
        public List<SelectListItem> TaskType { get; set; }

        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

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

        public List<A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem> A08_Tasks_TypeDetailsItems { get; set; }
        public int TotalItemCount { get; set; }
    }

    public class A08_Tasks_OverviewModel
    {
        public List<SelectListItem> DisplayType { get; set; }
        public string DisplayTypeFilter { get; set; }

        public List<A08_Tasks_OverviewItem> A08_Tasks_CompanyDetailsItems { get; set; }

        public class WorkflowGroupGrandParentItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<WorkflowGroupGrandParentItem> WorkflowGroupGrandParentItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupGrandParentID { get; set; }

        public class WorkflowGroupParentItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<WorkflowGroupParentItem> WorkflowGroupParentItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupParentID { get; set; }

        public class WorkflowGroupItem
        {
            public int ID { get; set; }
            public int BusinessDepartmentID { get; set; }
            public string DisplayName { get; set; }
            public int? WorkflowGroupParentID { get; set; }
        }
        public List<WorkflowGroupItem> WorkflowGroupItems { get; set; }
        [Required]
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> WorkflowGroupID { get; set; }

        public class BusinessPillarItem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<BusinessPillarItem> BusinessPillarItems { get; set; }
        [Display(Name = "Business Pillar")]
        public List<SelectListItem> BusinessPillar { get; set; }

        public class BusinessDepartmentItem
        {
            public int ID { get; set; }
            public int BusinessPillarID { get; set; }
            public string DisplayName { get; set; }
            public string HeaderClass
            {
                get
                {
                    switch (ID)
                    {
                        default:
                            return "background-color: #666666; color:#FFFFFF!important;border:1px solid #666666!important;";
                        //20	Sales - Onboarding
                        case 20:
                            return "background-color: #3DB4A0; color:#FFFFFF!important;border:1px solid #3DB4A0!important;";
                        //21	Technical - Onboarding
                        case 21:
                            return "background-color: #9EC83C; color:#FFFFFF!important;border:1px solid #9EC83C!important;";
                        //22	People - Onboarding
                        case 22:
                            return "background-color: #602A7A; color:#FFFFFF!important;border:1px solid #602A7A!important;";
                        //23	Operational Finance - Onboarding
                        case 23:
                            return "background-color: #EB8B2D; color:#FFFFFF!important;border:1px solid #EB8B2D!important;";
                        //24	Project Management - Onboarding
                        case 24:
                            return "background-color: #4A4C65; color:#FFFFFF!important;border:1px solid #4A4C65!important;";
                        //25	Administation
                        case 25:
                            return "background-color: #c6c639; color:#FFFFFF!important;border:1px solid #c6c639!important;";
                        //26	System
                        case 26:
                            return "background-color: #f20d46; color:#FFFFFF!important;border:1px solid #f20d46!important;";
                    }

                    return "";
                }
            }
            public string CellClass
            {
                get
                {
                    switch (ID)
                    {
                        default:
                            return "border:1px solid #666666!important;";
                        //20	Sales - Onboarding
                        case 20:
                            return "border:1px solid #3DB4A0!important;";
                        //21	Technical - Onboarding
                        case 21:
                            return "border:1px solid #9EC83C!important;";
                        //22	People - Onboarding
                        case 22:
                            return "border:1px solid #602A7A!important;";
                        //23	Operational Finance - Onboarding
                        case 23:
                            return "border:1px solid #EB8B2D!important;";
                        //24	Project Management - Onboarding
                        case 24:
                            return "border:1px solid #4A4C65!important;";
                        //25	Administation
                        case 25:
                            return "border:1px solid #c6c639!important;";
                        //26	System
                        case 26:
                            return "border:1px solid #f20d46!important;";
                    }

                    return "";
                }
            }
        }
        public List<BusinessDepartmentItem> BusinessDepartmentItems { get; set; }
        [Display(Name = "Business Department")]
        public List<SelectListItem> BusinessDepartment { get; set; }


        public class A08_Tasks_OverviewItem : Data.A08_Task
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public A08_Task_Type A08_Tasks_TypeItem { get; set; }

            public A08_Tasks_OverviewItemStatus Status { get; set; }
            public class A08_Tasks_OverviewItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public int BusinessDepartmentID { get; set; }
                public int? WorkflowGroupParentID { get; set; }
            }
            public string LatestComment { get; set; }

            //public List<A08_Tasks_ResponsiblePersonItem> A08_Tasks_ResponsiblePeople { get; set; }
            //public class A08_Tasks_ResponsiblePersonItem : Data.A08_Tasks.A08_Tasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class A08_Tasks_HeatmapModel
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

        public List<A08_Task_Type> A08_Task_Types { get; set; }
        public List<A08_Tasks_HeatmapItem> A08_Tasks_CompanyDetailsItems { get; set; }
        public class A08_Tasks_HeatmapItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public List<A08_Tasks_HeatmapSubItem> A08_Tasks_HeatmapSubItems { get; set; }
            public class A08_Tasks_HeatmapSubItem
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
