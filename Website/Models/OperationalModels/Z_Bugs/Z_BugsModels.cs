using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Z_BugsModels
{
    public class Z_Bugs_ReportABugModel
    {
        [Required]
        [Display(Name = "Bug Location")]
        public List<SelectListItem> LinkedSecureAreaID { get; set; }

        [Required]
        [Display(Name = "Bug Description")]
        public string UserDescription { get; set; }

        [Display(Name = "Screenshot")]
        public IFormFile Screenshot { get; set; }

        [Required]
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Display(Name = "Meter Serial")]
        public string MeterSerial { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> PriorityID { get; set; }

        [Required]
        [Display(Name = "Priority Reason")]
        public string PriorityReason { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Z_Bugs_MyBugsModel
    {
        public List<Z_Bugs_MyBugsItem> Z_Bugs_MyBugsItems { get; set; }
        public class Z_Bugs_MyBugsItem : Data.ReportedBug
        {
            public string Username { get; set; }
            public string CompanyName { get; set; }
        }
    }

    public class Z_Bugs_BugDetailModel
    {
        public Z_Bugs_BugDetailItem ReportedBug { get; set; }

        public class Z_Bugs_BugDetailItem : Data.ReportedBug
        {
            public string Username { get; set; }
            public List<Z_Bugs_BugDetail_LogItem> LogItems { get; set; }

            public class Z_Bugs_BugDetail_LogItem : Data.ReportedBugs_Log
            {
                public string Username { get; set; }
            }
        }


        [Required]
        [Display(Name = "Bug Location")]
        public List<SelectListItem> LinkedSecureAreaID { get; set; }

        [Required]
        [Display(Name = "Bug Description")]
        public string UserDescription { get; set; }

        [Display(Name = "Screenshot")]
        public IFormFile Screenshot { get; set; }

        [Required]
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Display(Name = "Meter Serial")]
        public string MeterSerial { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public List<SelectListItem> PriorityID { get; set; }

        [Required]
        [Display(Name = "Priority Reason")]
        public string PriorityReason { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Z_Bugs_BugAdminModel
    {
        public Z_Bugs_BugAdminItem ReportedBug { get; set; }

        public class Z_Bugs_BugAdminItem : Data.ReportedBug
        {
            public string Username { get; set; }
        }


        [Required]
        [Display(Name = "Status")]
        public List<SelectListItem> StatusID { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string UserDescription { get; set; }

        [Display(Name = "Screenshot")]
        public IFormFile Screenshot { get; set; }

        public bool IsSuccess { get; set; }
    }

}
