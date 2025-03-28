using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data.SiteAdmin_Imports
{
    public class SiteAdmin_Imports_RentalDataDump
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateUploadStarted { get; set; }
        public DateTime? DateUploadEnded { get; set; }
        public DateTime? DateImportStarted { get; set; }
        public DateTime? DateImportEnded { get; set; }
        public string ResultMessage { get; set; }
        public string OriginalFileName { get; set; }
        public int? SourceItemCount { get; set; }
        public int? ItemsCompleted { get; set; }
        public int? ItemsSucceeded { get; set; }
        public int? ItemsFailed { get; set; }
        public string ResultFriendly { get; set; }
    }
    public class SiteAdmin_Imports_RentalExpense
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateUploadStarted { get; set; }
        public DateTime? DateUploadEnded { get; set; }
        public DateTime? DateImportStarted { get; set; }
        public DateTime? DateImportEnded { get; set; }
        public string ResultMessage { get; set; }
        public string OriginalFileName { get; set; }
        public int? SourceItemCount { get; set; }
        public int? ItemsCompleted { get; set; }
        public int? ItemsSucceeded { get; set; }
        public int? ItemsFailed { get; set; }
        public string ResultFriendly { get; set; }
    }
    public class SiteAdmin_Imports_ManagementAccountsDataDump
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateUploadStarted { get; set; }
        public DateTime? DateUploadEnded { get; set; }
        public DateTime? DateImportStarted { get; set; }
        public DateTime? DateImportEnded { get; set; }
        public string ResultMessage { get; set; }
        public string OriginalFileName { get; set; }
        public int? SourceItemCount { get; set; }
        public int? ItemsCompleted { get; set; }
        public int? ItemsSucceeded { get; set; }
        public int? ItemsFailed { get; set; }
        public string ResultFriendly { get; set; }
    }
    public class SiteAdmin_Imports_Suburb
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateUploadStarted { get; set; }
        public DateTime? DateUploadEnded { get; set; }
        public DateTime? DateImportStarted { get; set; }
        public DateTime? DateImportEnded { get; set; }
        public string ResultMessage { get; set; }
        public string OriginalFileName { get; set; }
        public int? SourceItemCount { get; set; }
        public int? ItemsCompleted { get; set; }
        public int? ItemsSucceeded { get; set; }
        public int? ItemsFailed { get; set; }
        public string ResultFriendly { get; set; }
    }
}

namespace MyVoltage.Data
{
    public class SiteAdmin_LoginMessage
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string LoginMessage1 { get; set; }
        public string LoginMessage2 { get; set; }
        public string LoginMessage3 { get; set; }
    }


    public class SiteAdmin_Product
    {
        [Key]
        public int ID { get; set; }
        public string ProductName { get; set; }
        public string CreatedByID { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int CostOfSalesLinkID { get; set; }
        public SiteAdmin_ProductLinkEnum CostOfSalesLink { get { return (SiteAdmin_ProductLinkEnum)CostOfSalesLinkID; } }
        public int SalesLinkID { get; set; }
        public SiteAdmin_ProductLinkEnum SalesLink { get { return (SiteAdmin_ProductLinkEnum)SalesLinkID; } }
        public bool IncludeInC602x { get; set; }
        public int? DeviceTypeID { get; set; }
        public int? ChargeTypeID { get; set; }
        public string ShortName { get; set; }
        public SiteAdmin_ProductChargeType ChargeType
        {
            get
            {
                if (ChargeTypeID.HasValue)
                    return (SiteAdmin_ProductChargeType)ChargeTypeID;

                return SiteAdmin_ProductChargeType.None;
            }
        }
        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                if (DeviceTypeID.HasValue)
                    return (DeviceType.DeviceTypeEnum)DeviceTypeID;

                return Data.DeviceType.DeviceTypeEnum.Unknown;
            }
        }
        public int? BuildingCouncilInvoiceResourceTypeID { get; set; }
    }

    public enum SiteAdmin_ProductLinkEnum : int
    {
        [Description("Skybill Resource Ledger Entries")]
        SkybillResourceLedgerEntries = 1,
        [Description("L2.011 Meter Rentals - Accounting")]
        L_MeterRentals_Accounting = 2,
        [Description("GL Account - 6810 Fees and Charges")]
        GL_Account_6810 = 3,
        [Description("GL Account - 7191 Direct Cost Applied, Retail")]
        GL_Account_7191 = 4,
        [Description("GL Account - 8640 Bank Charges")]
        GL_Account_8640 = 5,
        [Description("GL Account - 6610 Sales, Other Job Expenses")]
        GL_Account_6610 = 6,
        [Description("GL Account - 8620 Bad Debts")]
        GL_Account_8620 = 7,
        [Description("GL Account - 6811")]
        GL_Account_6811 = 8,
    }

    public enum SiteAdmin_ProductChargeType : int
    {
        [Description("None")]
        None = 0,
        [Description("Fixed")]
        Fixed = 1,
        [Description("Consumption")]
        Consumption = 2,
    }

    public class SiteAdmin_Partner
    {
        [Key]
        public int ID { get; set; }
        public string PartnerName { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_Priority
    {
        [Key]
        public int ID { get; set; }
        public string PriorityName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SiteAdmin_StatusGroup
    {
        [Key]
        public int ID { get; set; }
        public string StatusGroupName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_StatusAction
    {
        [Key]
        public int ID { get; set; }
        public string StatusActionName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_StatusReporting
    {
        [Key]
        public int ID { get; set; }
        public string StatusReportingName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_Status
    {
        [Key]
        public int ID { get; set; }
        public int StatusGroupID { get; set; }
        public int StatusActionID { get; set; }
        public int StatusReportingID { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsResolvedStatus { get; set; }
        public int? WIPType { get; set; }
        public SiteAdmin_Status_WIP_TypeEnum WIPTypeEnum
        {
            get
            {
                if (WIPType.HasValue)
                    return (SiteAdmin_Status_WIP_TypeEnum)WIPType.Value;

                return SiteAdmin_Status_WIP_TypeEnum.NotStarted;
            }
        }
        public string WIPTypeIcon
        {
            get
            {
                switch (WIPTypeEnum)
                {
                    default:
                        return "";
                        break;
                    case SiteAdmin_Status_WIP_TypeEnum.NotStarted:
                        return "<i class=\"fas fa-times-circle text-red\"></i>";
                        break;
                    case SiteAdmin_Status_WIP_TypeEnum.Completed:
                        return "<i class=\"fas fa-check-circle text-green\"></i>";
                        break;
                    case SiteAdmin_Status_WIP_TypeEnum.WIP:
                        return "<i class=\"fas fa-exclamation-triangle text-orange\"></i>";
                        break;
                }
            }
        }
    }

    public enum SiteAdmin_Status_WIP_TypeEnum
    {
        [Description("Not Started")]
        NotStarted = 1,
        [Description("Completed")]
        Completed = 2,
        [Description("Work In Progress")]
        WIP = 3,

    }

    public class SiteAdmin_MeetingAgendaGroup
    {
        [Key]
        public int ID { get; set; }
        public string MeetingAgendaGroupName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_MeetingAgendaAction
    {
        [Key]
        public int ID { get; set; }
        public string MeetingAgendaActionName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_MeetingAgendaReporting
    {
        [Key]
        public int ID { get; set; }
        public string MeetingAgendaReportingName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_MeetingAgenda
    {
        [Key]
        public int ID { get; set; }
        public int MeetingAgendaGroupID { get; set; }
        public int MeetingAgendaActionID { get; set; }
        public int MeetingAgendaReportingID { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsResolvedMeetingAgenda { get; set; }
        public int? WIPType { get; set; }
        public SiteAdmin_MeetingAgenda_WIP_TypeEnum WIPTypeEnum
        {
            get
            {
                if (WIPType.HasValue)
                    return (SiteAdmin_MeetingAgenda_WIP_TypeEnum)WIPType.Value;

                return SiteAdmin_MeetingAgenda_WIP_TypeEnum.NotStarted;
            }
        }
        public string WIPTypeIcon
        {
            get
            {
                switch (WIPTypeEnum)
                {
                    default:
                        return "";
                        break;
                    case SiteAdmin_MeetingAgenda_WIP_TypeEnum.NotStarted:
                        return "<i class=\"fas fa-times-circle text-red\"></i>";
                        break;
                    case SiteAdmin_MeetingAgenda_WIP_TypeEnum.Completed:
                        return "<i class=\"fas fa-check-circle text-green\"></i>";
                        break;
                    case SiteAdmin_MeetingAgenda_WIP_TypeEnum.WIP:
                        return "<i class=\"fas fa-exclamation-triangle text-orange\"></i>";
                        break;
                }
            }
        }
    }

    public enum SiteAdmin_MeetingAgenda_WIP_TypeEnum
    {
        [Description("Not Started")]
        NotStarted = 1,
        [Description("Completed")]
        Completed = 2,
        [Description("Work In Progress")]
        WIP = 3,

    }

    public class SiteAdmin_DeviceAPI
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime? LatestSyncDate { get; set; }
        public int? LatestSyncDeviceCount { get; set; }
        public int? LatestSyncGWCount { get; set; }
        public bool? UseSkybill { get; set; }
        public bool? IsSyncActive { get; set; }
        public string Bearer { get; set; }
    }

    public class SiteAdmin_DeviceAPIs_CustomURL
    {
        [Key]
        public int ID { get; set; }
        public int SiteAdmin_DeviceAPIID { get; set; }
        public int CustomURLID { get; set; }
        public DeviceAPIs_CustomURLs DeviceAPIs_CustomURL { get { return (DeviceAPIs_CustomURLs)CustomURLID; } }
        public string CustomURL { get; set; }
    }

    public enum DeviceAPIs_CustomURLs
    {
        [Description("data")]
        Data = 1,
    }
    public class SiteAdmin_ContactorStateHack
    {
        [Key]
        public int ID { get; set; }
        public string MeterSerial { get; set; }
        public bool ContactorIsOnline { get; set; }
    }
    public class SiteAdmin_Municipality
    {
        [Key]
        public int ID { get; set; }
        public string MunicipalityName { get; set; }
        public int MunicipalityTypeID { get; set; }
        public int ProvinceID { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public ProvinceEnum Province
        {
            get
            {
                return (ProvinceEnum)ProvinceID;
            }
        }

        public MunicipalityTypeEnum MunicipalityType
        {
            get
            {
                return (MunicipalityTypeEnum)MunicipalityTypeID;
            }
        }
    }

    public enum MunicipalityTypeEnum
    {
        [Description("Metropolitan")]
        Metropolitan = 1,
        [Description("District")]
        District = 2,
        [Description("Local")]
        Local = 3,
    }

    public enum ProvinceEnum
    {
        [Description("Eastern Cape")]
        EasternCape = 1,
        [Description("Free State")]
        FreeState = 2,
        [Description("Gauteng")]
        Gauteng = 3,
        [Description("KwaZulu-Natal")]
        KwaZuluNatal = 4,
        [Description("Limpopo")]
        Limpopo = 5,
        [Description("Mpumalanga")]
        Mpumalanga = 6,
        [Description("North West")]
        NorthWest = 7,
        [Description("Northern Cape")]
        NorthernCape = 8,
        [Description("Western Cape")]
        WesternCape = 9,
    }

    public class SiteAdmin_Suburb
    {
        [Key]
        public int ID { get; set; }
        public string SuburbName { get; set; }
        public int TownID { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_Town
    {
        [Key]
        public int ID { get; set; }
        public string TownName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int ProvinceID { get; set; }
        public ProvinceEnum Province
        {
            get
            {
                return (ProvinceEnum)ProvinceID;
            }
        }

    }

    public class SiteAdmin_LegalEntity
    {
        [Key]
        public int ID { get; set; }
        public string LegalEntityName { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class SiteAdmin_DeviceType
    {
        [Key]
        public int ID { get; set; }
        public int DeviceTypeID { get; set; }
        public decimal CalibrationDiff { get; set; }
        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                return (DeviceType.DeviceTypeEnum)DeviceTypeID;
            }
        }
    }
}
