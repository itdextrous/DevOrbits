using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels
{
    public class DeveloperPageModel
    {
        public int? ProductionCreditsLeft { get; set; }
        public int? BackupCreditsLeft { get; set; }
        public string Database { get; set; }
        public string ApiDatabase { get; set; }
        public string HangfireDatabase { get; set; }
        public string OrderPost { get; set; }
        public string OrderResponse { get; set; }
    }

    public class MapModel
    {
        public decimal OpenLat { get; set; }
        public decimal OpenLong { get; set; }

        public List<SelectListItem> Partners { get; set; }
        public List<SelectListItem> Companies { get; set; }

        public string PartnerName { get; set; }
        public List<MapItem> MapItems { get; set; }

        public class MapItem : Data.Company
        {
            public string PartnerName { get; set; }
            public Data.BuildingDetail BuildingDetail { get; set; }
        }
    }

    public class MapMarkersViewModel
    {
        public List<Marker> Markers { get; set; }

        public string MarkersXML { get { return Markers.ToXML<List<Marker>, List<Marker>>(); } }

        public class Marker
        {
            public string Name { get; set; }
            public decimal Long { get; set; }
            public decimal Lat { get; set; }
        }
    }

    public class GameplanModel
    {
        public bool ShowUsers { get; set; }
        public List<SelectListItem> Users { get; set; }

        public List<A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem> A08_TaskItems_Active { get; set; }
        public List<A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem> A09_FlagItem_Active { get; set; }

        public List<A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem> A08_TaskItems_Urgent { get; set; }
        public List<A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem> A09_FlagItem_Urgent { get; set; }

        public List<A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem> A08_TaskItems_Normal { get; set; }
        public List<A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem> A09_FlagItem_Normal { get; set; }

        public List<A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem> A08_TaskItems_LeftOver { get; set; }

        public List<A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem> A08_Tasks_User_SummaryItems { get; set; }
        public List<A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem> A08_Tasks_User_SummaryStatusItems { get; set; }
    }
    public class ActiveTimeLogModel
    {
        public bool ShowUsers { get; set; }
        public List<SelectListItem> Users { get; set; }

        public List<Module_TimeOfWorkAllocated> Module_TimeOfWorkAllocateds { get; set; }
        public class Module_TimeOfWorkAllocated : Data.Module_TimeOfWorkAllocated
        {
            public int ActivityID { get; set; }
            public int ActivityTypeID { get; set; }
            public int ActivityTypeTypeID { get; set; }
            public string CreatedByUsername { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public TimeSpan TimeAllocated { get { return TimeSpan.FromMinutes(MinOfWorkAllocated); } }
            public string ReasonForFlag { get; set; }
        }
    }
}
