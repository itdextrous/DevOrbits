using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Customer
    {
        public int CustomerID { get; set; }

        [Column(TypeName = "VARCHAR(500)")]
        public string UserID { get; set; }

        public string CustomerNumber { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string UnitNumber { get; set; }
        public string ComplexName { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string Province { get; set; }
        public int? PostalCode { get; set; }
        public int CompanyID { get; set; }
        public int AccountTypeID { get; set; }
        public string OTPCode { get; set; }
        public string EmailCode { get; set; }
        public string MeterNumber { get; set; }
        public DateTime OccupancyDate { get; set; }

        public string NotificationPhoneNumber { get; set; }
        public string NotificationEmail { get; set; }
        public string DisconnectionNotices { get; set; }
        public int? DisconnectionLowBalanceNotification1 { get; set; }
        public int? DisconnectionLowBalanceNotification2 { get; set; }
        public Boolean HighUsageNotifications { get; set; } = false;
        public Boolean LeakNotifications { get; set; } = false;
        public Boolean NewsLetters { get; set; } = false;
        public bool IsDeleted { get; set; }
        public bool? ShowDailyUsage { get; set; }
        public bool? ShowCostInclVAT { get; set; }
        public bool? AutoConvertBalanceToUnits { get; set; }
        public string BalanceNotificationSMSType { get; set; }

        public bool? ActivateTaxInvoice { get; set; }
        public string RecipientName { get; set; }
        public string RecipientAddress { get; set; }
        public string RecipientVATNumber { get; set; }
        public string RecipientReferenceNumber { get; set; }
        public int? SystemNotificationTypeID { get; set; }
        public int? InfoNotificationTypeID { get; set; }
        public bool? ShowCustomBankingDetails { get; set; }
        public bool? ShowDetailedDailyBilling { get; set; } = false;
    }

    public enum NotificationTypeEnum
    {
        [Description("Notifications via SMS")]
        SMS = 1,
        [Description("Notifications via Email")]
        Email = 2,
        [Description("SMS & Email")]
        SMSEmail = 3,
        [Description("None")]
        None = 4,
    }

    public enum BalanceAndConsumptionSMSTypeEnum
    {
        [Description("None (Free)")]
        None = 1,
        [Description("Daily (R2.00 per sms)")]
        Daily = 2,
        [Description("Weekly (Free)")]
        Weekly = 3,
    }

    public class CustomersDetail
    {
        [Key]
        public int ID { get; set; }
        public string SerialNo { get; set; }
        public string CustomerNo { get; set; }
        public string CustomerTradingName { get; set; }
        public string CustomerAccNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SystemCheckUserID { get; set; }
        public DateTime? SystemCheckDate { get; set; }
        public string CustomerCheckUserID { get; set; }
        public DateTime? CustomerCheckDate { get; set; }
        public string VerificationURL { get; set; }
        public string VerificationUserID { get; set; }
        public DateTime? VerificationDate { get; set; }
        public bool? CustomerSignOffDeleted { get; set; }
        public string MeterSerialNo { get; set; }
        public string MeterSerialNoCheckUserID { get; set; }
        public DateTime? MeterSerialNoCheckDate { get; set; }
        public bool? SystemSignOffDeleted { get; set; }
        public bool? IncludeInExport { get; set; }
        public decimal? MeteringLat { get; set; }
        public decimal? MeteringLong { get; set; }
        public string MeteringLatLongUserID { get; set; }
        public DateTime? MeteringLatLongDate { get; set; }
    }

    public class Customer_AppNotification
    {
        [Key]
        public int ID { get; set; }
        public string Message { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public int CompanyID { get; set; }
        public int CustomerID { get; set; }
        public DateTime? DateRead { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class CustomersDetails_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int CustomersDetailsID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public bool IsDeleted { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }
    }

    public class CustomersDetails_Attachments_Comment
    {
        [Key]
        public int ID { get; set; }
        public int CustomersDetails_AttachmentID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Comment { get; set; }
        public bool IsDeleted { get; set; }
    }
}
