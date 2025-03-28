using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_StatusGroupsModel
    {
        public List<SiteAdmin_StatusGroupItem> SiteAdmin_StatusGroupItems { get; set; }

        public class SiteAdmin_StatusGroupItem : Data.SiteAdmin_StatusGroup
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public List<SiteAdmin_StatusGroupItemStatusItem> SiteAdmin_StatusGroupItemStatusItems { get; set; }

            public class SiteAdmin_StatusGroupItemStatusItem : Data.SiteAdmin_Status
            {
                public Data.SiteAdmin_StatusAction SiteAdmin_StatusAction { get; set; }
                public Data.SiteAdmin_StatusReporting SiteAdmin_StatusReporting { get; set; }
            }
        }
    }

    public class SiteAdmin_StatusGroups_AddModel
    {
        [Display(Name = "StatusGroup Name")]
        [Required]
        public string StatusGroupName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_StatusGroups_EditModel
    {
        [Display(Name = "Status Group Name")]
        [Required]
        public string StatusGroupName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }

        public int StatusGroupID { get; set; }

        public List<SiteAdmin_StatusGroups_EditItem> SiteAdmin_StatusGroups_EditItems { get; set; }

        public class SiteAdmin_StatusGroups_EditItem : Data.SiteAdmin_Status
        {
            public Data.SiteAdmin_StatusAction SiteAdmin_StatusAction { get; set; }
            public Data.SiteAdmin_StatusReporting SiteAdmin_StatusReporting { get; set; }
        }
    }
}
