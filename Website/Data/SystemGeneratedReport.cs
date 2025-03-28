using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SystemGeneratedReport
    {
        [Key]
        public int ID { get; set; }
        public int SecureAreaID { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public string ReportURL { get; set; }
        public decimal? Progress { get; set; }
        public bool? NotificationSent { get; set; }
        public string NotificationSentTo { get; set; }
        public int? ReportLocationID { get; set; }
        public ReportLocationEnum ReportLocation
        {
            get
            {
                if (ReportLocationID.HasValue)
                    return (ReportLocationEnum)ReportLocationID.Value;

                return ReportLocationEnum.Unknown;
            }
        }

        public int? RetryCount { get; set; }

        public enum ReportLocationEnum
        {
            [Description("Unknown / Not Set")]
            Unknown = 0,
            [Description("My Voltage API - Hangfire")]
            MyVotageAAPIHangfire = 1,
            [Description("Virtual Machine")]
            NUC = 2,
            [Description("My Voltage Scheduler - Hangfire")]
            MyMeterSAHangfire = 3,
            [Description("VS Hosting VM")]
            VSHostingVM = 4,
        }
    }

    public class SystemGeneratedReports_MeterActiveEnergyAnomalie
    {
        [Key]
        public int ID { get; set; }
        public int SystemGeneratedReportID { get; set; }
        public string MeterID { get; set; }
        public string Serial { get; set; }
        public string Name { get; set; }
        public DateTime TimeLogged { get; set; }
        public decimal ActiveEnergyReading { get; set; }
        public decimal Diff { get; set; }
        public decimal ErrorPerc { get; set; }
        public string Report { get; set; }
    }

    public class SystemGeneratedReports_Company
    {
        [Key]
        public int ID { get; set; }
        public int SystemGeneratedReportID { get; set; }
        public int CompanyID { get; set; }
        public DateTime DateCompleted { get; set; }
    }

    public class F_SystemGeneratedReports_AccountingChecklist_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_NetcashServicesChargesRecon_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_GenLedgerSync_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_GenLedgerSyncFULL_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_DirectDepositsAllocation_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_SQLJobs_CallLogSync_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

    public class F_SystemGeneratedReports_Detail
    {
        [Key]
        public int ID { get; set; }
        public int SecureAreaID { get; set; }
        public int? PriorityID { get; set; }
        public string Frequency { get; set; }
        public string StartTimes { get; set; }
        public decimal? StandardDurationSec { get; set; }
        public DateTime? LastCodeUpdate { get; set; }
        public string Comments { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum F_SystemGeneratedReports_DetailPriority
    {
        [Description("Low")]
        Low = 1,
        [Description("Medium")]
        Medium = 2,
        [Description("High")]
        High = 3,
        [Description("Urgent")]
        Urgent = 4,
    }
    public class F_SystemGeneratedReports_SageAccounting_JournalRequest
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }
    public class F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }
}
