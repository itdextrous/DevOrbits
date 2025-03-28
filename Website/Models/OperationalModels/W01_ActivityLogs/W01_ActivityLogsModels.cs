using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Models.OperationalModels.W01_ActivityLogsModels
{
    public class W01_ActivityLogs_TimePlanner_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        [Display(Name = "Active Status")]
        public List<SelectListItem> ShowOnlyOpen { get; set; }

        public List<W01_ActivityLogs_TimePlanner_SummaryItem> W01_ActivityLogs_TimePlanner_SummaryItems { get; set; }
        public class W01_ActivityLogs_TimePlanner_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_TimePlanner_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public int EntriesPerPage { get; set; }
        public int TotalEntries { get; set; }

        public PaginatedList<W01_ActivityLogs_TimePlanner_DetailsItem> W01_ActivityLogs_TimePlanner_DetailsItems { get; set; }
        public class W01_ActivityLogs_TimePlanner_DetailsItem : Data.Module_TimeOfWorkPlanned
        {
            public string ActivityURL { get; set; }
            public string LatestComment { get; set; }
            public string ActivityHeading { get; set; }
            public string UserName { get; set; }
            public string Company { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_TimeAllocated_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_TimeAllocated_SummaryItem> W01_ActivityLogs_TimeAllocated_SummaryItems { get; set; }
        public class W01_ActivityLogs_TimeAllocated_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_TimeAllocated_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_TimeAllocated_DetailsItem> W01_ActivityLogs_TimeAllocated_DetailsItems { get; set; }
        public class W01_ActivityLogs_TimeAllocated_DetailsItem : Data.Module_TimeOfWorkAllocated
        {
            public string LatestComment { get; set; }
            public string ActivityURL { get; set; }
            public string ActivityHeading { get; set; }
            public string UserName { get; set; }
            public string Company { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }


    public class W01_ActivityLogs_MissingTimeAllocated_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_MissingTimeAllocated_SummaryItem> W01_ActivityLogs_MissingTimeAllocated_SummaryItems { get; set; }
        public class W01_ActivityLogs_MissingTimeAllocated_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }
    public class W01_ActivityLogs_TravelAllocation_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_TravelAllocation_SummaryItem> W01_ActivityLogs_TravelAllocation_SummaryItems { get; set; }
        public class W01_ActivityLogs_TravelAllocation_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TravelSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_TravelAllocation_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_TravelAllocation_DetailsItem> W01_ActivityLogs_TravelAllocation_DetailsItems { get; set; }
        public class W01_ActivityLogs_TravelAllocation_DetailsItem : Data.Module_TravelAllocation
        {
            public string LatestComment { get; set; }
            public string ActivityURL { get; set; }
            public string ActivityHeading { get; set; }
            public string UserName { get; set; }
            public string Company { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_StockAllocation_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_StockAllocation_SummaryItem> W01_ActivityLogs_StockAllocation_SummaryItems { get; set; }
        public class W01_ActivityLogs_StockAllocation_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> StockSpent { get; set; }

        }
    }
    public class W01_ActivityLogs_StockAllocation_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_StockAllocation_DetailsItem> W01_ActivityLogs_StockAllocation_DetailsItems { get; set; }
        public class W01_ActivityLogs_StockAllocation_DetailsItem : Data.Module_StockAllocation
        {
            public string LatestComment { get; set; }
            public string ActivityURL { get; set; }
            public string ActivityHeading { get; set; }
            public string UserName { get; set; }
            public string Company { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }


    public class W01_ActivityLogs_InvoiceAllocation_SummaryModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_InvoiceAllocation_SummaryItem> W01_ActivityLogs_InvoiceAllocation_SummaryItems { get; set; }
        public class W01_ActivityLogs_InvoiceAllocation_SummaryItem
        {
            public string UserName { get; set; }
            public string UserID { get; set; }
            public string ActivityType { get; set; }
            public string ActivityTypeID { get; set; }
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public List<KeyValuePair<DateTime, decimal>> InvoiceSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_InvoiceAllocation_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Display as")]
        public List<SelectListItem> DisplayAs { get; set; }

        public List<W01_ActivityLogs_InvoiceAllocation_DetailsItem> W01_ActivityLogs_InvoiceAllocation_DetailsItems { get; set; }
        public class W01_ActivityLogs_InvoiceAllocation_DetailsItem : Data.Module_InvoiceAllocation
        {
            public string LatestComment { get; set; }
            public string ActivityURL { get; set; }
            public string ActivityHeading { get; set; }
            public string UserName { get; set; }
            public string Company { get; set; }
            public List<KeyValuePair<DateTime, decimal>> TimeSpent { get; set; }

        }
    }

    public class W01_ActivityLogs_UserActivity_DetailsModel
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Activity Type")]
        public List<SelectListItem> ActivityType { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        public List<W01_ActivityLogs_UserActivity_DetailsItem> W01_ActivityLogs_UserActivity_DetailsItems { get; set; }
        public class W01_ActivityLogs_UserActivity_DetailsItem : Data.ActivityLog
        {
            public string UserName { get; set; }
            public string Company { get; set; }
        }
    }

}
