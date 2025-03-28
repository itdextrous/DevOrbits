using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.V03_HumanResources
{
    public class V03_HumanResources_AllHumanResourcesModel
    {
        public List<V03_HumanResources_AllHumanResourcesItem> V03_HumanResources_AllHumanResourcesItems { get; set; }
        public class V03_HumanResources_AllHumanResourcesItem : Data.V03_PersonalDetail
        {
            public string OperationalUser { get; set; }
            public string ReportingToUser { get; set; }
            public Data.OperationalProfile OperationalProfile { get; set; }
            public string CompanyName { get; set; }
            public Data.V03_JobDescription V03_JobDescription { get; set; }
            public int V03_CorrespondencesCount { get; set; }
        }
    }

    public class V03_HumanResources_PersonalDetailsModel
    {
        public bool IsSuccess { get; set; }
        public int ID { get; set; }

        public List<V03_PersonalDetails_LogItem> V03_PersonalDetails_LogItems { get; set; }
        public class V03_PersonalDetails_LogItem : Data.V03_PersonalDetails_Log
        {
            public string Username { get; set; }
        }
        public string OperationalUser { get; set; }
        public string ReportingToUser { get; set; }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }


        [Required]
        [Display(Name = "Employee Email")]
        public string EmployeeEmail { get; set; }

        [Required]
        [Display(Name = "Employee No")]
        public string EmployeeNo { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "ID Number")]
        public string IDNumber { get; set; }

        [Required]
        [Display(Name = "Residential Address")]
        public string ResidentialAddress { get; set; }

        [Required]
        [Display(Name = "Contact No")]
        public string ContactNo { get; set; }

        [Required]
        [Display(Name = "Personal Email")]
        public string PersonalEmail { get; set; }

        [Required]
        [Display(Name = "Income Tax No")]
        public string IncomeTaxNo { get; set; }

        [Required]
        [Display(Name = "Date Of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Maritial Status")]
        public string MaritialStatus { get; set; }

        [Required]
        [Display(Name = "Spouse Name")]
        public string SpouseName { get; set; }

        [Required]
        [Display(Name = "Spouse Employer")]
        public string SpouseEmployer { get; set; }

        [Required]
        [Display(Name = "Spouse Contact No")]
        public string SpouseContactNo { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUserID { get; set; }

        [Required]
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        [Required]
        [Display(Name = "Bank Account Name")]
        public string BankAccountName { get; set; }

        [Required]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [Required]
        [Display(Name = "Bank Branch Name")]
        public string BankBranchName { get; set; }

        [Required]
        [Display(Name = "Bank Account No")]
        public string BankAccountNo { get; set; }

        [Required]
        [Display(Name = "Emergency Full Name")]
        public string EmergencyFullName { get; set; }

        [Required]
        [Display(Name = "Emergency Residential Address")]
        public string EmergencyResidentialAddress { get; set; }

        [Required]
        [Display(Name = "Emergency Primary Contact No")]
        public string EmergencyPrimaryContactNo { get; set; }

        [Required]
        [Display(Name = "Emergency Relationship")]
        public string EmergencyRelationship { get; set; }

        [Required]
        [Display(Name = "Operational User")]
        public List<SelectListItem> OperationalUserID { get; set; }
    }

    public class V03_HumanResources_JobDescriptionsModel
    {
        public bool IsSuccess { get; set; }
        public V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem V03_HumanResources_AllHumanResourcesItem { get; set; }
        public List<V02_Workflow_Allocations.V02_Workflow_AllocationsModels.V02_Workflow_Allocations_AllTaskAllocationsModel.V02_Workflow_Allocations_AllTaskAllocationsItem> V02_Workflow_Allocations_AllTaskAllocationsItems { get; set; }

        public List<V03_JobDescriptions_LogItem> V03_JobDescriptions_LogItems { get; set; }
        public class V03_JobDescriptions_LogItem : Data.V03_JobDescriptions_Log
        {
            public string Username { get; set; }
        }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }


        [Required]
        [Display(Name = "Primary")]
        public string DutiesAndResponsibilities_Primary { get; set; }

        [Required]
        [Display(Name = "Secondary")]
        public string DutiesAndResponsibilities_Secondary { get; set; }

        [Required]
        [Display(Name = "Overall")]
        public string DutiesAndResponsibilities_Overall { get; set; }

    }

    public class V03_HumanResources_CorrespondenceListModel
    {
        public V03_HumanResources_AllHumanResourcesModel.V03_HumanResources_AllHumanResourcesItem V03_HumanResources_AllHumanResourcesItem { get; set; }
        public List<V03_CorrespondenceItem> V03_CorrespondenceItems { get; set; }
        public class V03_CorrespondenceItem : Data.V03_Correspondence
        {
            public string Username { get; set; }
        }

        [Required]
        [Display(Name = "Description of File")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Browse a file to attach to lead")]
        public IFormFile Attachment { get; set; }

        [Required]
        [Display(Name = "AttachmentType")]
        public List<SelectListItem> AttachmentType { get; set; }

        public bool IsSuccess { get; set; }
    }
}
