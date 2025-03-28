using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.D02_SaleModels
{
    public class D02_SaleTasks_Company_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_Company_SummaryStatusItem> D02_SaleTasks_Company_SummaryStatusItems { get; set; }
        public class D02_SaleTasks_Company_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<D02_SaleTasks_Company_SummaryItem> D02_SaleTasks_CompanySummaryItems { get; set; }
        public class D02_SaleTasks_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public List<D02_SaleTasks_Company_SummaryItemStatus> D02_SaleTasks_Company_SummaryItemStatuses { get; set; }
            public class D02_SaleTasks_Company_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (D02_SaleTasks_Company_SummaryItemStatuses != null && D02_SaleTasks_Company_SummaryItemStatuses.Count > 0)
                        return D02_SaleTasks_Company_SummaryItemStatuses.Select(p => p.Count).Sum();

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

    public class D02_SaleTasks_Company_DetailsModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_Company_DetailsItem> D02_SaleTasks_CompanyDetailsItems { get; set; }

        public class D02_SaleTasks_Company_DetailsItem : Data.D02_SaleTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public D02_SaleTask_Type D02_SaleTasks_TypeItem { get; set; }

            public D02_SaleTasks_Company_DetailsItemStatus Status { get; set; }
            public class D02_SaleTasks_Company_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class D02_SaleTask_Type : Data.D02_SaleTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<D02_SaleTasks_ResponsiblePersonItem> D02_SaleTasks_ResponsiblePeople { get; set; }
            //public class D02_SaleTasks_ResponsiblePersonItem : Data.D02_SaleTasks.D02_SaleTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class D02_SaleTasks_User_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_User_SummaryStatusItem> D02_SaleTasks_User_SummaryStatusItems { get; set; }
        public class D02_SaleTasks_User_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<D02_SaleTasks_User_SummaryItem> D02_SaleTasks_UserSummaryItems { get; set; }
        public class D02_SaleTasks_User_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }

            public List<D02_SaleTasks_User_SummaryItemStatus> D02_SaleTasks_User_SummaryItemStatuses { get; set; }
            public class D02_SaleTasks_User_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (D02_SaleTasks_User_SummaryItemStatuses != null && D02_SaleTasks_User_SummaryItemStatuses.Count > 0)
                        return D02_SaleTasks_User_SummaryItemStatuses.Select(p => p.Count).Sum();

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

            //public Data.D02_SaleTask.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.D02_SaleTask.StatusEnum.Completed;

            //        return Data.D02_SaleTask.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class D02_SaleTasks_User_DetailsModel
    {
        public string UserName { get; set; }
        public bool TaskResponsibleUser { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_User_DetailsItem> D02_SaleTasks_UserDetailsItems { get; set; }

        public class D02_SaleTasks_User_DetailsItem : Data.D02_SaleTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }

            public D02_SaleTasks_User_DetailsItemStatus Status { get; set; }
            public class D02_SaleTasks_User_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public D02_SaleTask_Type D02_SaleTasks_TypeItem { get; set; }

            public class D02_SaleTask_Type : Data.D02_SaleTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<D02_SaleTasks_ResponsiblePersonItem> D02_SaleTasks_ResponsiblePeople { get; set; }
            //public class D02_SaleTasks_ResponsiblePersonItem : Data.D02_SaleTasks.D02_SaleTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class D02_SaleTasks_Type_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_Type_SummaryStatusItem> D02_SaleTasks_Type_SummaryStatusItems { get; set; }
        public class D02_SaleTasks_Type_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }

        public List<D02_SaleTasks_Type_SummaryItem> D02_SaleTasks_TypeSummaryItems { get; set; }
        public class D02_SaleTasks_Type_SummaryItem
        {
            public int TypeID { get; set; }
            public string TypeName { get; set; }

            public List<D02_SaleTasks_Type_SummaryItemStatus> D02_SaleTasks_Type_SummaryItemStatuses { get; set; }
            public class D02_SaleTasks_Type_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }
            }

            public int Total
            {
                get
                {
                    if (D02_SaleTasks_Type_SummaryItemStatuses != null && D02_SaleTasks_Type_SummaryItemStatuses.Count > 0)
                        return D02_SaleTasks_Type_SummaryItemStatuses.Select(p => p.Count).Sum();

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

            //public Data.D02_SaleTask.StatusEnum Status
            //{
            //    get
            //    {
            //        if (CompletedCount == Total)
            //            return Data.D02_SaleTask.StatusEnum.Completed;

            //        return Data.D02_SaleTask.StatusEnum.InProgress;
            //    }
            //}
        }

    }

    public class D02_SaleTasks_Type_DetailsModel
    {
        public string TypeName { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<D02_SaleTasks_Type_DetailsItem> D02_SaleTasks_TypeDetailsItems { get; set; }

        public class D02_SaleTasks_Type_DetailsItem : Data.D02_SaleTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }

            public D02_SaleTasks_Type_DetailsItemStatus Status { get; set; }
            public class D02_SaleTasks_Type_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public D02_SaleTask_Type D02_SaleTasks_TypeItem { get; set; }

            public class D02_SaleTask_Type : Data.D02_SaleTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

            //public List<D02_SaleTasks_ResponsiblePersonItem> D02_SaleTasks_ResponsiblePeople { get; set; }
            //public class D02_SaleTasks_ResponsiblePersonItem : Data.D02_SaleTasks.D02_SaleTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

    public class D02_SaleTask_ReviewModel
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

        public D02_SaleTask_ReviewItem D02_SaleTask { get; set; }

        public class D02_SaleTask_ReviewItem : Data.D02_SaleTask
        {
            public D02_SaleTask_Type D02_SaleTasks_TypeItem { get; set; }
            public D02_SaleTaskStatus D02_SaleTaskStatusItem { get; set; }
            public class D02_SaleTaskStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public D02_SaleTaskMeetingAgenda D02_SaleTaskMeetingAgendaItem { get; set; }
            public class D02_SaleTaskMeetingAgenda : Data.SiteAdmin_MeetingAgenda
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class D02_SaleTask_Type : Data.D02_SaleTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public List<D02_SaleTask_TypeStatus> D02_SaleTask_TypeStatuses { get; set; }
                public class D02_SaleTask_TypeStatus : Data.SiteAdmin_Status
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
                public List<D02_SaleTask_TypeMeetingAgenda> D02_SaleTask_TypeMeetingAgendaes { get; set; }
                public class D02_SaleTask_TypeMeetingAgenda : Data.SiteAdmin_MeetingAgenda
                {
                    public string GroupName { get; set; }
                    public string ActionName { get; set; }
                    public string ReportingName { get; set; }
                }
            }
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public List<D02_SaleTask_Review_ReassignLogItem> D02_SaleTask_Review_ReassignLogItems { get; set; }
            public class D02_SaleTask_Review_ReassignLogItem : Data.D02_SaleTasks_ReassignLog
            {
                public string Username { get; set; }
            }
            public List<D02_SaleTask_Review_AttachmentItem> D02_SaleTask_Review_AttachmentItems { get; set; }
            public class D02_SaleTask_Review_AttachmentItem : Data.D02_SaleTasks_Attachment
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

    public class D02_SaleTask_CreateModel
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

    public class D02_SaleTask_Review_AddAttachmentModel
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

    //public class D02_Sale_AnswersModel
    //{
    //    public List<D02_Sale_AnswersItem> D02_Sale_AnswersItems { get; set; }

    //    public class D02_Sale_AnswersItem : Data.BuildingOnboardingQuestion
    //    {
    //        public string Username { get; set; }
    //        public Answer Answered { get; set; }
    //        public class Answer : Data.BuildingOnboardingAnswer
    //        {
    //            public string Username { get; set; }
    //        }
    //    }
    //}

    //public class D02_Sale_Answers_Update_TXTModel
    //{
    //    public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }

    //    [Required]
    //    [Display(Name = "Answer")]
    //    public string Answer { get; set; }

    //    public bool IsSuccess { get; set; }
    //}

    //public class D02_Sale_Answers_Update_FUModel
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

    //public class D02_Sale_Answers_Update_YNModel
    //{
    //    public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }

    //    [Required]
    //    [Display(Name = "Explain your answer")]
    //    public string Answer { get; set; }

    //    [Display(Name = "Answer")]
    //    public List<SelectListItem> QuestionTypeAnswer { get; set; }

    //    public bool IsSuccess { get; set; }
    //}

    //public class D02_Sale_LogsModel
    //{
    //    public List<D02_Sale_LogsItem> D02_Sale_LogsItems { get; set; }

    //    public class D02_Sale_LogsItem : Data.BuildingOnboardingLog
    //    {
    //        public string Username { get; set; }
    //        public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }
    //    }
    //}

    public class D02_SaleTasks_Company_OnboardingModel
    {
        public List<SelectListItem> DisplayType { get; set; }
        public string DisplayTypeFilter { get; set; }

        public List<D02_SaleTasks_Company_OnboardingItem> D02_SaleTasks_CompanyDetailsItems { get; set; }

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


        public class D02_SaleTasks_Company_OnboardingItem : Data.D02_SaleTask
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public D02_SaleTask_Type D02_SaleTasks_TypeItem { get; set; }

            public D02_SaleTasks_Company_OnboardingItemStatus Status { get; set; }
            public class D02_SaleTasks_Company_OnboardingItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class D02_SaleTask_Type : Data.D02_SaleTask_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
                public int BusinessDepartmentID { get; set; }
                public int? WorkflowGroupParentID { get; set; }
            }
            public string LatestComment { get; set; }

            //public List<D02_SaleTasks_ResponsiblePersonItem> D02_SaleTasks_ResponsiblePeople { get; set; }
            //public class D02_SaleTasks_ResponsiblePersonItem : Data.D02_SaleTasks.D02_SaleTasks_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
        }
    }

}
