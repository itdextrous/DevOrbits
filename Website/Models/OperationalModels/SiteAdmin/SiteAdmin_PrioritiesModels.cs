using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_PrioritiesModel
    {
        public List<SiteAdmin_PriorityItem> SiteAdmin_PriorityItems { get; set; }

        public class SiteAdmin_PriorityItem : Data.SiteAdmin_Priority
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
        }
    }

    public class SiteAdmin_Priorities_AddModel
    {
        [Display(Name = "Priority Name")]
        [Required]
        public string PriorityName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_Priorities_EditModel
    {
        [Display(Name = "Priority Name")]
        [Required]
        public string PriorityName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }
}
