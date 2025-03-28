using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.E01_BuildingOnboardingModels
{
    public class E01_BuildingOnboardingTasks_Company_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_Company_SummaryStatusItem> E01_BuildingOnboardingTasks_Company_SummaryStatusItems { get; set; }
        public class E01_BuildingOnboardingTasks_Company_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<E01_BuildingOnboardingTasks_Company_SummaryItem> E01_BuildingOnboardingTasks_CompanySummaryItems { get; set; }
        public class E01_BuildingOnboardingTasks_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public List<E01_BuildingOnboardingTasks_Company_SummaryItemStatus> E01_BuildingOnboardingTasks_Company_SummaryItemStatuses { get; set; }
            public class E01_BuildingOnboardingTasks_Company_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (E01_BuildingOnboardingTasks_Company_SummaryItemStatuses != null && E01_BuildingOnboardingTasks_Company_SummaryItemStatuses.Count > 0)
                        return E01_BuildingOnboardingTasks_Company_SummaryItemStatuses.Select(p => p.Count).Sum();

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

    public class E01_BuildingOnboardingTasks_Company_DetailsModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_Company_DetailsItem> E01_BuildingOnboardingTasks_CompanyDetailsItems { get; set; }

        public class E01_BuildingOnboardingTasks_Company_DetailsItem : Data.E01_BuildingOnboardingTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTasks_TypeItem { get; set; }

            public E01_BuildingOnboardingTasks_Company_DetailsItemStatus Status { get; set; }
            public class E01_BuildingOnboardingTasks_Company_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class E01_BuildingOnboardingTask_Type : Data.E01_BuildingOnboardingTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<E01_BuildingOnboardingTasks_ResponsiblePersonItem> E01_BuildingOnboardingTasks_ResponsiblePeople { get; set; }
            //public class E01_BuildingOnboardingTasks_ResponsiblePersonItem : Data.E01_BuildingOnboardingTasks.E01_BuildingOnboardingTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class E01_BuildingOnboardingTasks_User_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_User_SummaryStatusItem> E01_BuildingOnboardingTasks_User_SummaryStatusItems { get; set; }
        public class E01_BuildingOnboardingTasks_User_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<E01_BuildingOnboardingTasks_User_SummaryItem> E01_BuildingOnboardingTasks_UserSummaryItems { get; set; }
        public class E01_BuildingOnboardingTasks_User_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }

            public List<E01_BuildingOnboardingTasks_User_SummaryItemStatus> E01_BuildingOnboardingTasks_User_SummaryItemStatuses { get; set; }
            public class E01_BuildingOnboardingTasks_User_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (E01_BuildingOnboardingTasks_User_SummaryItemStatuses != null && E01_BuildingOnboardingTasks_User_SummaryItemStatuses.Count > 0)
                        return E01_BuildingOnboardingTasks_User_SummaryItemStatuses.Select(p => p.Count).Sum();

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

            //public Data.E01_BuildingOnboardingTask.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.E01_BuildingOnboardingTask.StatusEnum.Completed;

            //        return Data.E01_BuildingOnboardingTask.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class E01_BuildingOnboardingTasks_User_DetailsModel
    {
        public string UserName { get; set; }
        public bool TaskResponsibleUser { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_User_DetailsItem> E01_BuildingOnboardingTasks_UserDetailsItems { get; set; }

        public class E01_BuildingOnboardingTasks_User_DetailsItem : Data.E01_BuildingOnboardingTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }

            public E01_BuildingOnboardingTasks_User_DetailsItemStatus Status { get; set; }
            public class E01_BuildingOnboardingTasks_User_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTasks_TypeItem { get; set; }

            public class E01_BuildingOnboardingTask_Type : Data.E01_BuildingOnboardingTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<E01_BuildingOnboardingTasks_ResponsiblePersonItem> E01_BuildingOnboardingTasks_ResponsiblePeople { get; set; }
            //public class E01_BuildingOnboardingTasks_ResponsiblePersonItem : Data.E01_BuildingOnboardingTasks.E01_BuildingOnboardingTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class E01_BuildingOnboardingTasks_Type_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_Type_SummaryStatusItem> E01_BuildingOnboardingTasks_Type_SummaryStatusItems { get; set; }
        public class E01_BuildingOnboardingTasks_Type_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<E01_BuildingOnboardingTasks_Type_SummaryItem> E01_BuildingOnboardingTasks_TypeSummaryItems { get; set; }
        public class E01_BuildingOnboardingTasks_Type_SummaryItem
        {
            public int TypeID { get; set; }
            public string TypeName { get; set; }

            public List<E01_BuildingOnboardingTasks_Type_SummaryItemStatus> E01_BuildingOnboardingTasks_Type_SummaryItemStatuses { get; set; }
            public class E01_BuildingOnboardingTasks_Type_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (E01_BuildingOnboardingTasks_Type_SummaryItemStatuses != null && E01_BuildingOnboardingTasks_Type_SummaryItemStatuses.Count > 0)
                        return E01_BuildingOnboardingTasks_Type_SummaryItemStatuses.Select(p => p.Count).Sum();

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

            //public Data.E01_BuildingOnboardingTask.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.E01_BuildingOnboardingTask.StatusEnum.Completed;

            //        return Data.E01_BuildingOnboardingTask.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class E01_BuildingOnboardingTasks_Type_DetailsModel
    {
        public string TypeName { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<E01_BuildingOnboardingTasks_Type_DetailsItem> E01_BuildingOnboardingTasks_TypeDetailsItems { get; set; }

        public class E01_BuildingOnboardingTasks_Type_DetailsItem : Data.E01_BuildingOnboardingTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }

            public E01_BuildingOnboardingTasks_Type_DetailsItemStatus Status { get; set; }
            public class E01_BuildingOnboardingTasks_Type_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTasks_TypeItem { get; set; }

            public class E01_BuildingOnboardingTask_Type : Data.E01_BuildingOnboardingTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<E01_BuildingOnboardingTasks_ResponsiblePersonItem> E01_BuildingOnboardingTasks_ResponsiblePeople { get; set; }
            //public class E01_BuildingOnboardingTasks_ResponsiblePersonItem : Data.E01_BuildingOnboardingTasks.E01_BuildingOnboardingTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class E01_BuildingOnboardingTask_ReviewModel
    {
        [Display(Name = "Comments")]
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

        public E01_BuildingOnboardingTask_ReviewItem E01_BuildingOnboardingTask { get; set; }

        public class E01_BuildingOnboardingTask_ReviewItem : Data.E01_BuildingOnboardingTask
        {
            public E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTasks_TypeItem { get; set; }
            public E01_BuildingOnboardingTaskStatus E01_BuildingOnboardingTaskStatusItem { get; set; }
            public class E01_BuildingOnboardingTaskStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public E01_BuildingOnboardingTaskMeetingAgenda E01_BuildingOnboardingTaskMeetingAgendaItem { get; set; }
            public class E01_BuildingOnboardingTaskMeetingAgenda : Data.SiteAdmin_MeetingAgenda
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class E01_BuildingOnboardingTask_Type : Data.E01_BuildingOnboardingTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public List<E01_BuildingOnboardingTask_TypeStatus> E01_BuildingOnboardingTask_TypeStatuses { get; set; }
                public class E01_BuildingOnboardingTask_TypeStatus : Data.SiteAdmin_Status
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
                public List<E01_BuildingOnboardingTask_TypeMeetingAgenda> E01_BuildingOnboardingTask_TypeMeetingAgendaes { get; set; }
                public class E01_BuildingOnboardingTask_TypeMeetingAgenda : Data.SiteAdmin_MeetingAgenda
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
            }
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public List<E01_BuildingOnboardingTask_Review_ReassignLogItem> E01_BuildingOnboardingTask_Review_ReassignLogItems { get; set; }
            public class E01_BuildingOnboardingTask_Review_ReassignLogItem : Data.E01_BuildingOnboardingTasks_ReassignLog
            {
                public string Username { get; set; }
            }
            public List<E01_BuildingOnboardingTask_Review_AttachmentItem> E01_BuildingOnboardingTask_Review_AttachmentItems { get; set; }
            public class E01_BuildingOnboardingTask_Review_AttachmentItem : Data.E01_BuildingOnboardingTasks_Attachment
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

    public class E01_BuildingOnboardingTask_CreateModel
    {
        [Required]
        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Required]
        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Required]
        [Display(Name = "Task Type")]
        public List<SelectListItem> TaskType { get; set; }

        [Required]
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
        [ValidateDateRange]
        public DateTime DueDate { get; set; }

        //[Display(Name = "Km Travel Required")]
        //public int? KmTravelRequired { get; set; }

        //[Display(Name = "Stock Used")]
        //public string StockUsed { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int TaskTypeID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }
    }

    public class E01_BuildingOnboardingTask_Review_AddAttachmentModel
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

    //public class E01_BuildingOnboarding_AnswersModel
    //{
    //    public List<E01_BuildingOnboarding_AnswersItem> E01_BuildingOnboarding_AnswersItems { get; set; }

    //    public class E01_BuildingOnboarding_AnswersItem : Data.BuildingOnboardingQuestion
    //    {
    //        public string Username { get; set; }
    //        public Answer Answered { get; set; }
    //        public class Answer : Data.BuildingOnboardingAnswer
    //        {
    //            public string Username { get; set; }
    //        }
    //    }
    //}

    //public class E01_BuildingOnboarding_Answers_Update_TXTModel
    //{
    //    public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }

    //    [Required]
    //    [Display(Name = "Answer")]
    //    public string Answer { get; set; }

    //    public bool IsSuccess { get; set; }
    //}

    //public class E01_BuildingOnboarding_Answers_Update_FUModel
    //{
    //    public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }

    //    [Required]
    //    [Display(Name = "Describe the file")]
    //    public string Answer { get; set; }

    //    [Required]
    //    [Display(Name = "Browse a file to attach")]
    //    public IFormFile Attachment { get; set; }


    //    public bool IsSuccess { get; set; }
    //}

    //public class E01_BuildingOnboarding_Answers_Update_YNModel
    //{
    //    public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }

    //    [Required]
    //    [Display(Name = "Explain your answer")]
    //    public string Answer { get; set; }

    //    [Display(Name = "Answer")]
    //    public List<SelectListItem> QuestionTypeAnswer { get; set; }

    //    public bool IsSuccess { get; set; }
    //}

    //public class E01_BuildingOnboarding_LogsModel
    //{
    //    public List<E01_BuildingOnboarding_LogsItem> E01_BuildingOnboarding_LogsItems { get; set; }

    //    public class E01_BuildingOnboarding_LogsItem : Data.BuildingOnboardingLog
    //    {
    //        public string Username { get; set; }
    //        public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }
    //    }
    //}

    public class E01_BuildingOnboardingTasks_Company_OnboardingModel
    {
        public List<SelectListItem> DisplayType { get; set; }
        public string DisplayTypeFilter { get; set; }

        public List<E01_BuildingOnboardingTasks_Company_OnboardingItem> E01_BuildingOnboardingTasks_CompanyDetailsItems { get; set; }

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


        public class E01_BuildingOnboardingTasks_Company_OnboardingItem : Data.E01_BuildingOnboardingTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTasks_TypeItem { get; set; }

            public E01_BuildingOnboardingTasks_Company_OnboardingItemStatus Status { get; set; }
            public class E01_BuildingOnboardingTasks_Company_OnboardingItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class E01_BuildingOnboardingTask_Type : Data.E01_BuildingOnboardingTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public int BusinessDepartmentID { get; set; }
                public int? WorkflowGroupParentID { get; set; }
            }
            public string LatestComment { get; set; }

            //public List<E01_BuildingOnboardingTasks_ResponsiblePersonItem> E01_BuildingOnboardingTasks_ResponsiblePeople { get; set; }
            //public class E01_BuildingOnboardingTasks_ResponsiblePersonItem : Data.E01_BuildingOnboardingTasks.E01_BuildingOnboardingTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

}
