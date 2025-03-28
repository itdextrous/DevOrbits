using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.V04_InternalMeetingsModels
{
    public class V04_InternalMeetings_ExcoMeetingsModel
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

        public List<V04_InternalMeetings_ExcoMeetingsItem> V04_InternalMeetings_ExcoMeetingsItems { get; set; }

        public class V04_InternalMeetings_ExcoMeetingsItem : Data.A08_Task
        {
            public string ReportingToUserUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LatestComment { get; set; }
            public A08_Task_Type A08_Tasks_TypeItem { get; set; }

            public ItemStatus Status { get; set; }
            public class ItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class A08_Task_Type : Data.A08_Task_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }

        }

        public List<A09_Flags_Company_DetailsItem> A09_Flags_CompanyDetailsItems { get; set; }

        public class A09_Flags_Company_DetailsItem : Data.A09_Flags.A09_Flag
        {
            public string AssignedToUsername { get; set; }
            public string ReportingToUserUsername { get; set; }
            public A09_Flags_Type A09_Flags_TypeItem { get; set; }
            public string LatestComment { get; set; }
            public string CompanyName { get; set; }

            public A09_Flags_Company_DetailsItemStatus Status { get; set; }
            public class A09_Flags_Company_DetailsItemStatus : Data.SiteAdmin_Status
            {
                public string GroupName { get; set; }
                public string ActionName { get; set; }
                public string ReportingName { get; set; }
            }

            public class A09_Flags_Type : Data.A09_Flags.A09_Flags_Type
            {
                public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }
            }
            public Data.SiteAdmin_Priority SiteAdmin_Priority { get; set; }

        }

    }
}
