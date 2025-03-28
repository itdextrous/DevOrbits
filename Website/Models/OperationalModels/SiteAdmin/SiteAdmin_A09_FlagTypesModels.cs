using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_A09_FlagTypesModels
{
    public class SiteAdmin_A09_FlagTypesModel
    {
        public List<SiteAdmin_A09_FlagTypesItem> SiteAdmin_A09_FlagTypesItems { get; set; }
        public class SiteAdmin_A09_FlagTypesItem : Data.A09_Flags.A09_Flags_Type
        {
            public string DefaultUsername { get; set; }
            public string ReportingToUserUsername { get; set; }
            public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            //public List<A09_Flags_ResponsiblePersonItem> A09_Flags_ResponsiblePeople { get; set; }

            //public class A09_Flags_ResponsiblePersonItem : Data.A09_Flags.A09_Flags_ResponsiblePerson
            //{
            //    public string PersonUsername { get; set; }
            //}
            public Data.SiteAdmin_StatusGroup SiteAdmin_StatusGroup { get; set; }
            public string SiteAdmin_Status { get; set; }
            public string WorkflowGroupName { get; set; }
            public string BusinessPillarName { get; set; }
            public string BusinessDepartmentName { get; set; }
        }
    }

    public class SiteAdmin_A09_FlagTypesAddModel
    {
        [Required]
        [Display(Name = "Flag Type Name")]
        public string FlagTypeName { get; set; }

        //[Required]
        [Display(Name = "Policy / Procedure Document")]
        public IFormFile PolicyDocument { get; set; }

        public bool IsSuccess { get; set; }
        public string ResultFlagTypeID { get; set; }
        public string DefaultAssignedToID { get; set; }
        public string LinkedToSecureAreaID { get; set; }

        [Display(Name = "Default Assigned To User")]
        public List<SelectListItem> AvailableUsers { get; set; }
        [Display(Name = "Default Reporting To User")]
        public List<SelectListItem> ReportingToUsers { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

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
    }

    public class SiteAdmin_A09_FlagTypesEditModel
    {
        [Required]
        [Display(Name = "Flag Type Name")]
        public string FlagTypeName { get; set; }

        [Required]
        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        [Display(Name = "Policy / Procedure Document")]
        public IFormFile PolicyDocument { get; set; }

        public int FlagTypeID { get; set; }
        public int? DefaultStatusID { get; set; }
        public int? DefaultMeetingAgendaID { get; set; }
        public string PolicyDocumentURL { get; set; }
        public string DefaultAssignedToID { get; set; }
        public string LinkedToSecureAreaID { get; set; }

        public bool IsSuccess { get; set; }

        [Display(Name = "Default Assigned To User")]
        public List<SelectListItem> AvailableUsers { get; set; }
        [Display(Name = "Default Reporting To User")]
        public List<SelectListItem> ReportingToUsers { get; set; }

        [Required]
        [Display(Name = "Reporting To User Requires Completed State")]
        public List<SelectListItem> ReportingToUserRequiresCompletedState { get; set; }

        public List<Data.A09_Flags.A09_Flags_Types_SerialsToExclude> A09_Flags_Types_SerialsToExcludes { get; set; }
        public List<Data.SkybillCustomer> SkybillCustomers { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        [Required]
        [Display(Name = "Status Group")]
        public List<SelectListItem> StatusGroup { get; set; }

        [Required]
        [Display(Name = "Default Status")]
        public List<SelectListItem> DefaultStatus { get; set; }

        [Required]
        [Display(Name = "MeetingAgenda Group")]
        public List<SelectListItem> MeetingAgendaGroup { get; set; }

        [Required]
        [Display(Name = "Default MeetingAgenda")]
        public List<SelectListItem> DefaultMeetingAgenda { get; set; }

        [Display(Name = "Default Minutes Planned")]
        public int? DefautlMinPlanned { get; set; }

        [Required]
        [Display(Name = "Has Compliance Check")]
        public List<SelectListItem> HasComplianceCheck { get; set; }

        public class StatusItem
        {
            public int ID { get; set; }
            public int StatusGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<StatusItem> StatusItems { get; set; }

        public class MeetingAgendaItem
        {
            public int ID { get; set; }
            public int MeetingAgendaGroupID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<MeetingAgendaItem> MeetingAgendaItems { get; set; }
        //public List<A09_Flags_ResponsiblePersonItem> A09_Flags_ResponsiblePeople { get; set; }

        //public class A09_Flags_ResponsiblePersonItem : Data.A09_Flags.A09_Flags_ResponsiblePerson
        //{
        //    public string PersonUsername { get; set; }
        //}

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

        public List<A09_Flag_Type_LogItem> A09_Flag_Type_LogItems { get; set; }
        public class A09_Flag_Type_LogItem : Data.A09_Flags.A09_Flag_Type_Log
        {
            public string Username { get; set; }
        }
    }


}
