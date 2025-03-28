using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;

namespace MyVoltage.Models.WebServicesModels
{
    public class GenericResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class Customer_Register
    {
        public string MeterNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public DateTime? OccupancyDate { get; set; }
        public string CompanyName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
    }

    public class App_Version
    {
        public int Id { get; set; }
        public string OS { get; set; }
        public string LatestVersion { get; set; }
    }

    public class App_Details
    {
        public string AppName { get; set; }
        public string OSType { get; set; }
        public string CurrentVersion { get; set; }
    }

    public class Customer_RegisterResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string UserID { get; set; }
    }

    public class Customer_OTPConfirm
    {
        public string UserID { get; set; }
        public string OTP { get; set; }
    }

    public class Customer_OTPConfirmResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class Customer_OTPResend
    {
        public string UserID { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool? IsProfile { get; set; }
    }

    public class Customer_LoginResult
    {
        public bool IsSuccess { get; set; }
        public string Result { get; set; }
        public string Token { get; set; }
        public string EncodedToken { get; set; }
        public string CompanyDomain { get; set; }
    }

    public class Customer_Exists
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public bool? IsConfirmed { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
    }
    public class Customer_ProfileResult
    {
        public string ServiceProvider { get; set; }
        public string CustomerNumber { get; set; }
        public string LoginEmail { get; set; }
        public string NotificationEmail { get; set; }
        public string AccountType { get; set; }
        public DateTime? OccupancyDate { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }

        public List<ProfileCustomerMeterType> CustomerMeterTypes { get; set; }
        public class ProfileCustomerMeterType
        {
            public string MeterSerial { get; set; }
            public string UsageMeterSettingsDisplayType { get; set; }
        }
        public bool? ShowHourlyUsage { get; set; }
        public bool? ShowCostInclVAT { get; set; }
        public bool? ShowDetailedDailyBilling { get; set; }

        public string BalanceAndConsumptionSMS { get; set; }
        public decimal? LowBalanceNotification { get; set; }
        public bool? LowBalanceNotifications { get; set; }
        public bool? HighUsageNotifications { get; set; }
        public bool? LeakNotifications { get; set; }
        public bool? NewsLetters { get; set; }
        public bool? ActivateTaxInvoice { get; set; }
        public string RecipientName { get; set; }
        public string RecipientAddress { get; set; }
        public string RecipientVatNumber { get; set; }
        public string RecipientReferenceNumber { get; set; }
        public int? SystemNotificationType { get; set; }
        public int? InfoNotificationType { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
    }

    public class Customer_ProfileUsageDisplaySettingsResult
    {
        public bool? ShowHourlyUsage { get; set; }
        public bool? ShowCostInclVAT { get; set; }
        public bool? ShowDetailedDailyBilling { get; set; }
    }

    public class Customer_TaxInvoiceSettingsResult
    {
        public bool? ActivateTaxInvoice { get; set; }
        public string RecipientName { get; set; }
        public string RecipientAddress { get; set; }
        public string RecipientVatNumber { get; set; }
        public string RecipientReferenceNumber { get; set; }
    }

    public class Customer_PersonalDetailsResult
    {
        public string ServiceProvider { get; set; }
        public string CustomerNumber { get; set; }
        public string LoginEmail { get; set; }
        public string AccountType { get; set; }
        public DateTime? OccupancyDate { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class Customer_RechargeHistoryResult
    {
        public List<RechargeHistory> RechargeHistories { get; set; }

        public class RechargeHistory
        {
            public DateTime Date { get; set; }
            public string PaymentMethod { get; set; }
            public decimal Amount { get; set; }
        }
    }

    public class Customer_PersonalDetails_Update
    {
        public DateTime? OccupancyDate { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string NotificationEmail { get; set; }
    }

    public class ListItem
    {
        public int Value { get; set; }
        public string DisplayText { get; set; }
    }
    public class NotificationTypeResult
    {
        public List<ListItem> NotificationTypes
        {
            get
            {
                List<ListItem> keyValuePairs = new List<ListItem>();

                foreach (Data.NotificationTypeEnum notificationTypeEnum in (Data.NotificationTypeEnum[])Enum.GetValues(typeof(Data.NotificationTypeEnum)))
                {
                    keyValuePairs.Add(new ListItem() { Value = (int)notificationTypeEnum, DisplayText = notificationTypeEnum.GetDescription() });
                }

                return keyValuePairs;
            }
        }
    }

    public class BalanceNotificationSMSTypeResult
    {
        public List<ListItem> BalanceNotificationSMSTypes
        {
            get
            {
                List<ListItem> keyValuePairs = new List<ListItem>();

                foreach (Data.BalanceAndConsumptionSMSTypeEnum notificationTypeEnum in (Data.BalanceAndConsumptionSMSTypeEnum[])Enum.GetValues(typeof(Data.BalanceAndConsumptionSMSTypeEnum)))
                {
                    keyValuePairs.Add(new ListItem() { Value = (int)notificationTypeEnum, DisplayText = notificationTypeEnum.GetDescription() });
                }

                return keyValuePairs;
            }
        }
    }

    public class Customer_NotificationSettings_Update
    {
        public string NotificationEmail { get; set; }
        public int? BalanceAndConsumptionSMS { get; set; }
        public int? LowBalanceNotification { get; set; }
        public bool? LowBalanceNotifications { get; set; }
        public bool? HighUsageNotifications { get; set; }
        public bool? LeakNotifications { get; set; }
        public bool? NewsLetters { get; set; }
        public int? SystemNotificationType { get; set; }
        public int? InfoNotificationType { get; set; }
    }

    public class Customer_NotificationSettings_Update_New
    {
        public string Email { get; set; }
        public string NotificationEmail { get; set; }
        public int? BalanceAndConsumptionSMS { get; set; }
        public int? LowBalanceNotification { get; set; }
        public bool? LowBalanceNotifications { get; set; }
        public bool? HighUsageNotifications { get; set; }
        public bool? LeakNotifications { get; set; }
        public bool? NewsLetters { get; set; }
        public int? SystemNotificationType { get; set; }
        public int? InfoNotificationType { get; set; }
    }

    public class Customer_TaxInvoiceSettings_Update
    {
        public bool? ActivateTaxInvoice { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientAddress { get; set; }
        public string? RecipientVatNumber { get; set; }
        public string? RecipientReferenceNumber { get; set; }
        public string? IDNumberOrCompanyReg { get; set; }
    }

    public class Customer_UsageDisplaySettings_Update
    {
        public bool? ShowHourlyUsage { get; set; }
        public bool? ShowCostInclVAT { get; set; }
        public bool? ShowDetailedDailyBilling { get; set; }
    }

    public class Customer_UsageMeterSettings_CustomerResult
    {
        public List<UsageMeterSettings_CustomerCustomerMeterType> CustomerMeterTypes { get; set; }
        public class UsageMeterSettings_CustomerCustomerMeterType
        {
            public string MeterSerial { get; set; }
            public int UsageMeterSettingsDisplayTypeID { get; set; }
            public string UsageMeterSettingsDisplayTypeName { get; set; }
        }
    }

    public class MeterTypeResult
    {
        public List<ListItem> MeterTypeResults
        {
            get
            {
                List<ListItem> keyValuePairs = new List<ListItem>();

                foreach (MeterTypeEnum notificationTypeEnum in (MeterTypeEnum[])Enum.GetValues(typeof(MeterTypeEnum)))
                {
                    keyValuePairs.Add(new ListItem() { Value = (int)notificationTypeEnum, DisplayText = notificationTypeEnum.GetDescription() });
                }

                return keyValuePairs;
            }
        }
    }

    public class Customer_UsageMeterSettings_Update
    {
        public string MeterSerial { get; set; }
        public int? UsageMeterSettingsDisplayTypeID { get; set; }
    }

    public class Customer_ForgotPassword
    {
        public string Email { get; set; }
    }

    public class Customer_AccountDetailsResult
    {
        public decimal CustomerBalance { get; set; }
        public DateTime? CustomerLastPaymentDate { get; set; }
        public string CustomerLastPaymentMethod { get; set; }
        public decimal? CustomerLastPaymentAmount { get; set; }
    }

    public class Customer_ProductSummaryResult
    {
        public string CustomTitle { get; set; }
        public bool ShowIncVAT { get; set; }
        public DateTime BillingMonth { get; set; }
        public List<CustomerProductsResourceLedgersForMonthItem> CustomerProductsResourceLedgersForMonthItems { get; set; }

        public class CustomerProductsResourceLedgersForMonthItem : Data.SiteAdmin_Product
        {
            public bool ShowIncVAT { get; set; }
            public decimal Units { get; set; }
            public decimal PricePerUnit
            {
                get
                {
                    if (ShowIncVAT)
                    {
                        if (Units != 0)
                            return TotalInVat / Units;
                    }
                    else
                    {
                        if (Units != 0)
                            return Total / Units;
                    }
                    return 0;
                }
            }
            public decimal Total { get; set; }
            public decimal TotalInVat { get { return Total + (Total * MyVoltage.Services.Operational.OperationalBillingProvider.VAT); } }
        }
    }

    public class Customer_MeterListResult
    {
        public List<MeterItem> Devices { get; set; }
        public bool ShowVerticalLayout { get; set; }

        public class MeterItem
        {
            public DateTime LastComm { get; set; }
            public string ContactorState { get; set; }
            public string DisconnectionType { get; set; }
            public string Status { get; set; }
            public string MeterNumber { get; set; }
            public string Name { get; set; }
            public String MeterType { get; set; }
            public String MeterColor { get; set; }
            public String UnitType { get; set; }
            public DateTime ReadingDate { get; set; }
            public Decimal MonthlyTotal { get; set; }
        }
    }

    public class Customer_Statement
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class ProductsResult
    {
        public List<ListItem> Product { get; set; }
    }

    public class Customer_ConsumptionInsights
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ProductID { get; set; }
        public string IsDaily { get; set; }
    }

    public class Customer_ContactUs
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public string AttachmentFileName { get; set; }
        public string AttachmentFileBytes { get; set; }
    }

    public class CompanyNetcashDetailsResult
    {
        public string NetcashServiceKey { get; set; }
        public string NetcashBankName { get; set; }
        public string NetcashBankAccountType { get; set; }
        public string NetcashBankAccountNo { get; set; }
        public string NetcashBankBranchCode { get; set; }
    }

    public class PaymentMethodsResult
    {
        public List<ListItem> PaymentMethods
        {
            get
            {
                List<ListItem> keyValuePairs = new List<ListItem>();

                foreach (PaymentMethodEnum notificationTypeEnum in (PaymentMethodEnum[])Enum.GetValues(typeof(PaymentMethodEnum)))
                {
                    keyValuePairs.Add(new ListItem() { Value = (int)notificationTypeEnum, DisplayText = notificationTypeEnum.GetDescription() });
                }

                return keyValuePairs;
            }
        }
    }

    public class PaymentStatusesResult
    {
        public List<ListItem> PaymentStatuses
        {
            get
            {
                List<ListItem> keyValuePairs = new List<ListItem>();

                foreach (PaymentStatusEnum notificationTypeEnum in (PaymentStatusEnum[])Enum.GetValues(typeof(PaymentStatusEnum)))
                {
                    keyValuePairs.Add(new ListItem() { Value = (int)notificationTypeEnum, DisplayText = notificationTypeEnum.GetDescription() });
                }

                return keyValuePairs;
            }
        }
    }

    public class Customer_Payment
    {
        public decimal Amount { get; set; }
    }

    public class Customer_PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Reference { get; set; }
    }

    public class Customer_PaymentNotify
    {
        public string Reference { get; set; }
        public int PaymentMethodID { get; set; }
        public string CardHolderIpAddr { get; set; }
        public string RequestTrace { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public string Reason { get; set; }
    }

    public class Customer_MeterStatus
    {
        public string MeterSerial { get; set; }
    }

    public class Customer_MeterStatusResult
    {
        public DateTime StatusTime { get; set; }
        public bool IsOnline { get; set; }
        public int ContactorStatusID { get; set; }
        public string ContactorStatusDescription { get; set; }
        public string Service { get; set; }
        public string Reserve { get; set; }
        public string ChangeOverStatus { get; set; }
    }

    public class Customer_MeterStatusACO
    {
        public string MeterSerial { get; set; }
    }

    public class Customer_MeterStatusACOResult
    {
        public string ServiceStatus { get; set; }

        public string ReserveStatus { get; set; }

        public DateTime ChangeOverStatusTime { get; set; }
        public bool ChangeOverIsOnline { get; set; }
        public string ChangeOverStatus { get; set; }

    }

    public class Customer_Notifications
    {
        public bool ShowOnlyUnread { get; set; }
    }

    public class Customer_NotificationsResult
    {
        public List<Customer_NotificationsResultItem> Customer_NotificationsResultItems { get; set; }

        public class Customer_NotificationsResultItem
        {
            public int ID { get; set; }
            public string Message { get; set; }
            public DateTime DateCreated { get; set; }
            public DateTime? DateRead { get; set; }
            public DateTime ExpiryDate { get; set; }
        }
    }
    public class Customer_NotificationsMarkAsRead
    {
        public int ID { get; set; }
    }

    public class PostLead
    {
        public string APIKey { get; set; }
        public string ContactPersonEmail { get; set; }
        public string ContactPersonPhone { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPersonPosition { get; set; }
        public string ClientNeeds { get; set; }
        public string PropertyDescription { get; set; }
        public string PropertyAddress { get; set; }
        public string Municipality { get; set; }
        public int? NoOfUnits { get; set; }
        public bool? HasShownWebsiteAndVideo { get; set; }
    }

    public class CreateACODevice
    {
        public string UserID { get; set; }
        public int GatewayID { get; set; }
        public string SerialNumber { get; set; }
        public string RemoteAddress { get; set; }
        public string RemoteAddressChangeOver { get; set; }
    }

    public class ProfileContactVerification
    {
        public string PropType { get; set; }
        public string NotificationEmail { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ProfileContactConfirm: ProfileContactVerification
    {
        public string OTP { get; set; }
    }

    public class ProfileContactVerificationResult
    {
        public bool IsSuccess { get; set; }
        public bool? IsNumberChanged { get; set; }
        public bool? IsEmailChanged { get; set; }
        public string Message { get; set; }
    }
    public class RealTimeConsumption
    {
        public string meterNumber { get; set; }  
        public string meterType { get; set; }    
        public int year { get; set; }    
        public int month { get; set; }    
        public int day { get; set; }
        public bool showMirrorKGReading { get; set; }
        public int? accountTypeOverride { get;}
        public int? meterTypeOverride { get; set;}
    }


}
