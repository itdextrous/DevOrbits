using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.F_SystemGeneratedReports
{
    public class F_SystemGeneratedReports_AllReportsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SecureAreaCodeName { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SecureAreas { get; set; }

        public List<F_SystemGeneratedReports_AllReportsItem> F_SystemGeneratedReports_AllReportsItems { get; set; }
        public class F_SystemGeneratedReports_AllReportsItem
        {
            public string ReportName { get; set; }
            public int ReportsStarted { get; set; }
            public int ReportsCompleted { get; set; }
            public string SecureAreaName { get; set; }
            public DateTime? LatestStartDate { get; set; }
            public DateTime? LatestEndDate { get; set; }
            public string LatestReportURL { get; set; }
            public decimal? LatestReportProgress { get; set; }
            public TimeSpan? LatestReportDuration { get; set; }
            public bool LatestReportFailed { get; set; }
            public bool LatestReportInProgress { get; set; }
            public Data.SystemGeneratedReport.ReportLocationEnum ReportLocation { get; set; }
        }

    }

    public class F_SystemGeneratedReports_HistoricalSummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SecureAreaCodeName { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SecureAreas { get; set; }

        public List<F_SystemGeneratedReports_HistoricalSummaryItem> F_SystemGeneratedReports_HistoricalSummaryItems { get; set; }
        public class F_SystemGeneratedReports_HistoricalSummaryItem
        {
            public Data.SecureAreaEnum SecureArea { get; set; }
            public string ReportName { get; set; }
            public int ReportsStarted { get; set; }
            public int ReportsCompleted { get; set; }
            public string SecureAreaName { get; set; }
            public DateTime? LatestStartDate { get; set; }
            public DateTime? LatestEndDate { get; set; }
            public string LatestReportURL { get; set; }
            public decimal? LatestReportProgress { get; set; }
            public TimeSpan? LatestReportDuration { get; set; }
            public bool LatestReportFailed { get; set; }
            public bool LatestReportInProgress { get; set; }
            public Data.SystemGeneratedReport.ReportLocationEnum ReportLocation { get; set; }

            public F_SystemGeneratedReports_HistoricalSummarySubItem PastDay { get; set; }
            public F_SystemGeneratedReports_HistoricalSummarySubItem PastThreeDays { get; set; }
            public F_SystemGeneratedReports_HistoricalSummarySubItem PastWeek { get; set; }
            public F_SystemGeneratedReports_HistoricalSummarySubItem PastTwoWeeks { get; set; }
            public F_SystemGeneratedReports_HistoricalSummarySubItem PastMonth { get; set; }

            public class F_SystemGeneratedReports_HistoricalSummarySubItem
            {
                public int Started { get; set; }
                public int Completed { get; set; }
                public decimal SuccessRate
                {
                    get
                    {
                        if (Started != 0)
                            return (Convert.ToDecimal(Completed) / Convert.ToDecimal(Started)) * 100.0m;

                        return 0;
                    }
                }

            }
            public F_SystemGeneratedReports_Detail F_SystemGeneratedReports_DetailItem { get; set; }
            public class F_SystemGeneratedReports_Detail : Data.F_SystemGeneratedReports_Detail
            {
                public string UserName { get; set; }
            }
        }

    }

    public class F_SystemGeneratedReportsListModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SecureAreaCodeName { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SecureAreas { get; set; }

        public List<F_SystemGeneratedReportsListItem> F_SystemGeneratedReportsListItems { get; set; }
        public class F_SystemGeneratedReportsListItem : Data.SystemGeneratedReport
        {
            public string ReportParam { get; set; }
            public List<F_SystemGeneratedReportsListItemCompany> F_SystemGeneratedReportsListItemCompanies { get; set; }
            public class F_SystemGeneratedReportsListItemCompany : Data.SystemGeneratedReports_Company
            {
                public string CompanyName { get; set; }
            }
        }

    }
}
