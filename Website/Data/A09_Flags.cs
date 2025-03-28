using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data.A09_Flags
{
    public class A09_Flag
    {
        [Key]
        public int ID { get; set; }
        public int? CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public int FlagTypeID { get; set; }
        public string ReasonForFlag { get; set; }
        public string ReasonForTicket { get; set; }
        public DateTime Created { get; set; }
        public int StatusID { get; set; }
        public int PriorityID { get; set; }
        public string LinkedObjectDBTableName { get; set; }
        public string LinkedObjectUniqueID { get; set; }
        public string? AssignedToID { get; set; }
        public DateTime? ClosedDate { get; set; }
        public bool? OnceOffFlag { get; set; }
        public DateTime? DateLastViewed { get; set; }
        public string? DateLastViewdBy { get; set; }
        public int? MinRequiredToClear { get; set; }
        public int? TravelKmRequired { get; set; }
        public string StockUsed { get; set; }
        public decimal? AmountInvoiced { get; set; }
        public decimal? GPSLong { get; set; }
        public decimal? GPSLat { get; set; }
        public DateTime? DueDate { get; set; }
        public string? CreatedBy { get; set; }
        public int? Level { get; set; }
        public string? ReportingToUserID { get; set; }
        public int? WrikeID { get; set; }
        public string WrikeCustomStatus { get; set; }
        public DateTime? WrikeSyncDate { get; set; }
        public string WorkflowGroupName { get; set; }
        public string BusinessDepartmentName { get; set; }
        public string BusinessPillarName { get; set; }
        public int? MeetingAgendaID { get; set; }
        public int? SecureAreaGroupID { get; set; }
        public int? BusinessDepartmentID { get; set; }
        public string Identifier { get; set; }
    }

    //public enum A09_FlagsStatus
    //{
    //    [Description("Outstanding")]
    //    Outstanding = 1,

    //    [Description("Expired")]
    //    Expired = 2,

    //    [Description("Resolved")]
    //    Resolved = 3,

    //    [Description("In Progress")]
    //    InProgress = 4,

    //    [Description("Technician Dispatch Required")]
    //    TechnicianDispatchRequired = 5,

    //    [Description("Technician Dispatched")]
    //    TechnicianDispatched = 6,
    //}

    //public enum A09_FlagsPriority
    //{
    //    Problematic = 1,
    //    Attention = 2,
    //    Ok = 3,
    //}

    public class A09_Flags_Type
    {
        [Key]
        public int ID { get; set; }
        public string FlagTypeName { get; set; }
        public bool Active { get; set; }
        public string PolicyDocumentURL { get; set; }
        public string DefaultAssignedToID { get; set; }
        public int? SecureAreaID { get; set; }
        public int? PriorityID { get; set; }
        public int? StatusGroupID { get; set; }
        public int? DefaultStatusID { get; set; }
        public int? MeetingAgendaGroupID { get; set; }
        public int? DefaultMeetingAgendaID { get; set; }
        public string ReportingToUserID { get; set; }
        public bool? ReportingToUserRequiresCompletedState { get; set; }
        public int? DefautlMinPlanned { get; set; }
        public int? SecureAreaGroupID { get; set; }
        public bool? HasComplianceCheck { get; set; }
        public string Identifier { get; set; }
        public SecureAreaEnum? SecureArea
        {
            get
            {
                if (SecureAreaID.HasValue)
                    return (SecureAreaEnum)SecureAreaID.Value;

                return null;
            }

        }
    }

    public enum A09_Flags_TypeEnum
    {
        A1_GWandDeviceMonitoring_GatewayOffline = 1,
        A1_GWandDeviceMonitoring_DeviceOffline = 2,
        A2_MirrorMeterAuditing_Calibration = 3,
        A2_MirrorMeterAuditing_Reading = 4,
        A3_NetworkBalancing_Capture = 5,
        A6_BillingControlReport_Occupancy = 6,
        A7_CreditControlAndNotifierProcess_WalletInArears = 7,
        A6_BillingControlReport_FaultyorTamperedMeter = 8,
        A6_BillingControlReport_LastBilledExceedsLiveReading = 9,
        A6_BillingControlReport_OccupancyStatusWrong = 10,
        A6_BillingControlReport_MeterCardSetupWrong = 11,
        A6_BillingControlReport_MeterOfflineMoreThan7Days = 12,
        A7_CreditControlAndNotifierProcess_MeterMode = 13,
        A7_CreditControlAndNotifierProcess_MeterOnManual = 14,
        A10_VirtualMeters_TOUMetersNotBalancing = 15,
        A04_ZendeskTickets = 16,
        A1_GWandDeviceMonitoring_GatewaysAndDevicesNotLinked = 17,
        A1_GWandDeviceMonitoring_GatewaysAndDevicesBatteryLow = 18,
        Y0_SiteAdmin_CompaniesNotFilledIn = 19,
        SkybillResourceListsNoProduct = 20,
        A04_CallCentreLogs = 21,
    }

    public class A09_Flag_Type_Log
    {
        [Key]
        public int ID { get; set; }
        public int FlagTypeID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class A09_Flags_ResponsiblePerson
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int FlagTypeID { get; set; }
    }

    public class A09_Flags_ReassignLog
    {
        [Key]
        public int ID { get; set; }
        public int FlagID { get; set; }
        public string ReassignedByID { get; set; }
        public string ReassignedToID { get; set; }
        public string Comments { get; set; }
        public DateTime Created { get; set; }
        public int? ActionID { get; set; }
        public string ReportingToUserID { get; set; }
        public string SystemDescription { get; set; }
    }

    public class A09_Flags_Types_SerialsToExclude
    {
        [Key]
        public int ID { get; set; }
        public int FlagTypeID { get; set; }
        public string SerialToExclude { get; set; }
        public string CreatedByID { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class A09_Flags_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int FlagID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public int AttachmentTypeID { get; set; }
        public string Description { get; set; }
        public AttachmentTypeEnum AttachmentType { get { return (AttachmentTypeEnum)AttachmentTypeID; } }
        public bool IsDeleted { get; set; }
        public enum AttachmentTypeEnum
        {
            [Description("Other")]
            Other = 0,
            [Description("Email Communication (.msg)")]
            EmailCommunication = 1,
            [Description("Prelimiary Network Audit Result")]
            PrelimiaryNetworkAuditResult = 2,
            [Description("Profit Analysis Result")]
            ProfitAnalysisResult = 3,
        }
    }

}
