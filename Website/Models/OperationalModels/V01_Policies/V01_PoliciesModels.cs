using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.V01_Policies.V01_PoliciesModels
{
    public class V01_Policies_AllModel
    {
        public List<V01_Policies_AllItem> V01_Policies_AllItems { get; set; }
        public class V01_Policies_AllItem : Data.V01_Policy
        {
            public string Username { get; set; }
            public string SecureAreaName { get; set; }
            public List<V01_Policies_ResponsibleUserItem> V01_Policies_ResponsibleUserItems { get; set; }
            public class V01_Policies_ResponsibleUserItem : Data.V01_Policies_ResponsibleUser
            {
                public string Username { get; set; }
            }
        }
    }

    public class V01_Policies_EditModel
    {
        public V01_PoliciesItem V01_Policy { get; set; }
        public class V01_PoliciesItem : Data.V01_Policy
        {
            public string Username { get; set; }
            public string SecureAreaName { get; set; }

            public List<V01_Policies_AttachmentItem> V01_Policies_AttachmentItems { get; set; }
            public class V01_Policies_AttachmentItem : Data.V01_Policies_Attachment
            {
                public string Username { get; set; }
            }

            public List<V01_Policies_ResponsibleUserItem> V01_Policies_ResponsibleUserItems { get; set; }
            public class V01_Policies_ResponsibleUserItem : Data.V01_Policies_ResponsibleUser
            {
                public string Username { get; set; }
            }

            public List<V01_PoliciesLogItem> V01_PoliciesLogItems { get; set; }
            public class V01_PoliciesLogItem : Data.V01_PoliciesLog
            {
                public string Username { get; set; }
            }
        }

        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureAreaID { get; set; }

        [Required]
        [Display(Name = "Policy Type")]
        public List<SelectListItem> PolicyTypeID { get; set; }

        [Display(Name = "Active From Date")]
        public DateTime? ActiveFromDate { get; set; }

        [Display(Name = "Active To Date")]
        public DateTime? ActiveToDate { get; set; }

        public int ResultPolicyID { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class V01_Policies_CreateNewModel
    {
        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureAreaID { get; set; }

        [Required]
        [Display(Name = "Policy Type")]
        public List<SelectListItem> PolicyTypeID { get; set; }

        [Display(Name = "Active From Date")]
        public DateTime? ActiveFromDate { get; set; }

        [Display(Name = "Active To Date")]
        public DateTime? ActiveToDate { get; set; }

        public int ResultPolicyID { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class V01_Policies_AddAttachmentModel
    {
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
    public class V01_Policies_AddResponsibleUserModel
    {
        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class V01_Policies_MyPoliciesModel
    {
        public List<V01_Policies_MyPoliciesItem> V01_Policies_MyPoliciesItems { get; set; }
        public class V01_Policies_MyPoliciesItem : Data.V01_Policy
        {
            public string Username { get; set; }
            public string SecureAreaName { get; set; }
            public List<V01_Policies_ResponsibleUserItem> V01_Policies_ResponsibleUserItems { get; set; }
            public class V01_Policies_ResponsibleUserItem : Data.V01_Policies_ResponsibleUser
            {
                public string Username { get; set; }
            }
        }
    }

    public class V01_Policies_ViewModel
    {
        public V01_PoliciesItem V01_Policy { get; set; }
        public class V01_PoliciesItem : Data.V01_Policy
        {
            public string Username { get; set; }
            public string SecureAreaName { get; set; }
            public List<V01_Policies_AttachmentItem> V01_Policies_AttachmentItems { get; set; }
            public class V01_Policies_AttachmentItem : Data.V01_Policies_Attachment
            {
                public string Username { get; set; }
            }
            public List<V01_Policies_ResponsibleUserItem> V01_Policies_ResponsibleUserItems { get; set; }
            public class V01_Policies_ResponsibleUserItem : Data.V01_Policies_ResponsibleUser
            {
                public string Username { get; set; }
            }
        }

    }


}
