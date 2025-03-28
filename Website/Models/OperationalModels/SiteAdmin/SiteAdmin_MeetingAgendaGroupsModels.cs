using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_MeetingAgendaGroupsModel
    {
        public List<SiteAdmin_MeetingAgendaGroupItem> SiteAdmin_MeetingAgendaGroupItems { get; set; }

        public class SiteAdmin_MeetingAgendaGroupItem : Data.SiteAdmin_MeetingAgendaGroup
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public List<SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItem> SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItems { get; set; }

            public class SiteAdmin_MeetingAgendaGroupItemMeetingAgendaItem : Data.SiteAdmin_MeetingAgenda
            {
                public Data.SiteAdmin_MeetingAgendaAction SiteAdmin_MeetingAgendaAction { get; set; }
                public Data.SiteAdmin_MeetingAgendaReporting SiteAdmin_MeetingAgendaReporting { get; set; }
            }
        }
    }

    public class SiteAdmin_MeetingAgendaGroups_AddModel
    {
        [Display(Name = "Meeting Agenda Group Name")]
        [Required]
        public string MeetingAgendaGroupName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_MeetingAgendaGroups_EditModel
    {
        [Display(Name = "Meeting Agenda Group Name")]
        [Required]
        public string MeetingAgendaGroupName { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }

        public int MeetingAgendaGroupID { get; set; }

        public List<SiteAdmin_MeetingAgendaGroups_EditItem> SiteAdmin_MeetingAgendaGroups_EditItems { get; set; }

        public class SiteAdmin_MeetingAgendaGroups_EditItem : Data.SiteAdmin_MeetingAgenda
        {
            public Data.SiteAdmin_MeetingAgendaAction SiteAdmin_MeetingAgendaAction { get; set; }
            public Data.SiteAdmin_MeetingAgendaReporting SiteAdmin_MeetingAgendaReporting { get; set; }
        }
    }
}
