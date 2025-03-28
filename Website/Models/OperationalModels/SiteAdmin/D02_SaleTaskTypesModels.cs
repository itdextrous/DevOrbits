using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_D02_SaleTaskTypesModels
{
    public class SiteAdmin_D02_SaleTask_TypesModel
    {
        public bool ShowDeleted { get; set; }
        public List<SiteAdmin_D02_SaleTaskTypesItem> SiteAdmin_D02_SaleTaskTypesItems { get; set; }

        public class SiteAdmin_D02_SaleTaskTypesItem : Data.D02_SaleTask_Type
        {
            public string ResponsibleUsername { get; set; }
            public string ReportingToUserUsername { get; set; }
            public DateTime? PreviousRunDate { get; set; }
            public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            public Data.SiteAdmin_StatusGroup SiteAdmin_StatusGroup { get; set; }
            public string SiteAdmin_Status { get; set; }
            public bool IsLinkedToSelectedCompany { get; set; }
            public string WorkflowGroupName { get; set; }
            public string BusinessPillarName { get; set; }
            public string BusinessDepartmentName { get; set; }
            public Data.SiteAdmin_MeetingAgendaGroup SiteAdmin_MeetingAgendaGroup { get; set; }
            public string SiteAdmin_MeetingAgenda { get; set; }
        }
    }

    public class SiteAdmin_D02_SaleTask_TypesAddModel
    {
        [Display(Name = "Template No")]
        public int TemplateNo { get; set; }

        [Required]
        [Display(Name = "Department")]
        public List<SelectListItem> Department { get; set; }

        [Required]
        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureArea { get; set; }

        [Display(Name = "How To Document")]
        public IFormFile HowToDocument { get; set; }

        [Required]
        [Display(Name = "Work Days Required To Clear (Mon-Fri)")]
        public int MinRequiredToClear { get; set; }

        [Required]
        [Display(Name = "Status Group")]
        public List<SelectListItem> StatusGroup { get; set; }

        [Required]
        [Display(Name = "Default Status")]
        public List<SelectListItem> DefaultStatus { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int StatusGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }

        [Required]
        [Display(Name = "Meeting Agenda Group")]
        public List<SelectListItem> MeetingAgendaGroup { get; set; }

        [Required]
        [Display(Name = "Default Meeting Agenda")]
        public List<SelectListItem> DefaultMeetingAgenda { get; set; }

        public class MeetingAgendaItem
        {
            public int ID { get; set; }
            public int MeetingAgendaGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<MeetingAgendaItem> MeetingAgendaItems { get; set; }

        [Required]
        [Display(Name = "TaskClassification")]
        public List<SelectListItem> TaskClassification { get; set; }

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
        public bool IsSuccess { get; set; }
        public string ResultFlagTypeID { get; set; }
    }

    public class SiteAdmin_D02_SaleTask_TypesEditModel
    {
        public Data.D02_SaleTask_Type D02_SaleTask_Type { get; set; }
        public List<Data.D02_SaleTask_Type_Company> D02_SaleTask_Type_Companies { get; set; }

        [Display(Name = "Template No")]
        public int TemplateNo { get; set; }

        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Required]
        [Display(Name = "Reporting To User Requires Completed State")]
        public List<SelectListItem> ReportingToUserRequiresCompletedState { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        [Display(Name = "How To Document")]
        public IFormFile HowToDocument { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureArea { get; set; }

        [Required]
        [Display(Name = "Work Days Required To Clear (Mon-Fri)")]
        public int MinRequiredToClear { get; set; }

        [Display(Name = "Default Minutes Planned")]
        public int? DefautlMinPlanned { get; set; }

        [Required]
        [Display(Name = "Status Group")]
        public List<SelectListItem> StatusGroup { get; set; }

        [Required]
        [Display(Name = "Default Status")]
        public List<SelectListItem> DefaultStatus { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int StatusGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }

        [Required]
        [Display(Name = "Meeting Agenda Group")]
        public List<SelectListItem> MeetingAgendaGroup { get; set; }

        [Required]
        [Display(Name = "Default Meeting Agenda")]
        public List<SelectListItem> DefaultMeetingAgenda { get; set; }

        public class MeetingAgendaItem
        {
            public int ID { get; set; }
            public int MeetingAgendaGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<MeetingAgendaItem> MeetingAgendaItems { get; set; }

        [Required]
        [Display(Name = "TaskClassification")]
        public List<SelectListItem> TaskClassification { get; set; }

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
        public bool IsSuccess { get; set; }
        public string ResultFlagTypeID { get; set; }
    }

}
