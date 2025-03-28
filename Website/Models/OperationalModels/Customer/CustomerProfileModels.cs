using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.CustomerProfileModels
{
    public class Customer_ProfileModel
    {
        public Data.OperationalProfile OperationalProfile { get; set; }
        public Data.Customer SelectedCustomer { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
        public bool IsCurrentUserCustomer { get; set; }
        public bool IsConfirmed { get; set; }
        public bool AllowCustomerEdit { get; set; }
        public string OldUserIDForMeter { get; set; }
        public string OldUserForMeter { get; set; }

        //[DisplayName("First Name")]
        //[Required]
        //public string OperationalFirstName { get; set; }

        //[DisplayName("Last Name (Surname)")]
        //[Required]
        //public string OperationalLastName { get; set; }

        //[DisplayName("Phone Number")]
        //[Required]
        //[MaxLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        //[MinLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        //public string OperationalPhoneNumber { get; set; }

        //[DisplayName("Login E-mail")]
        //[Required]
        //public string OperationalEmail { get; set; }

        //[DisplayName("Job Role")]
        //[Required]
        //public string OperationalJobRole { get; set; }

        [DisplayName("Login E-mail")]
        public string Email { get; set; }

        [DisplayName("Customer Number")]
        public string CustomerNumber { get; set; }

        [DisplayName("Account Type")]
        public List<SelectListItem> AccountType { get; set; }

        [DisplayName("Occupancy Date")]
        [Required]
        //[ValidateOccupancyDate]
        public DateTime OccupancyDate { get; set; }

        [DisplayName("Service Provider")]
        public string CompanyName { get; set; }

        public List<ProfileCustomerMeterType> CustomerMeterTypes { get; set; }
        public class ProfileCustomerMeterType
        {
            public string MeterSerial { get; set; }
            public List<SelectListItem> selectList { get; set; }
        }

        [DisplayName("Full Name")]
        [Required]
        public string FullName { get; set; }

        [DisplayName("Auto Convert Balance To Units")]
        public bool AutoConvertBalanceToUnits { get; set; }

        [DisplayName("Phone Number")]
        [Required]
        [MaxLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        [MinLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        public string PhoneNumber { get; set; }

        [DisplayName("Alt Phone Number")]
        [MaxLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        [MinLength(10, ErrorMessage = "Phone number must be 10 digits.")]
        public string AltPhoneNumber { get; set; }

        [DisplayName("Notification E-Mail")]
        [Required]
        public string NotificationEMail { get; set; }

        [DisplayName("System Notification Type")]
        public List<SelectListItem> SystemNotificationTypeID { get; set; }

        [DisplayName("Information Notification Type")]
        public List<SelectListItem> InfoNotificationTypeID { get; set; }

        [DisplayName("Low Balance Notification")]
        [Required]
        [Range(20, int.MaxValue)]
        public int LowBalanceNotification { get; set; }

        [DisplayName("Low Balance Notifications")]
        public List<SelectListItem> LowBalanceNotifications { get; set; }

        [DisplayName("High Usage Notifications")]
        public List<SelectListItem> HighUsageNotifications { get; set; }

        [DisplayName("Leak Notifications")]
        public List<SelectListItem> LeakNotifications { get; set; }

        [DisplayName("News Letters")]
        public List<SelectListItem> NewsLetters { get; set; }

        [DisplayName("Show Hourly Usage")]
        public List<SelectListItem> ShowHourlyUsage { get; set; }

        [DisplayName("Show Cost Incl. VAT")]
        public List<SelectListItem> ShowCostInclVAT { get; set; }

        [DisplayName("Balance And Consumption SMS")]
        public List<SelectListItem> BalanceNotificationSMSType { get; set; }

        [DisplayName("Activate Tax Invoice")]
        public List<SelectListItem> ActivateTaxInvoice { get; set; }

        [DisplayName("Show Custom Banking Details")]
        public List<SelectListItem> ShowCustomBankingDetails { get; set; }

        [DisplayName("Recipient Name")]
        public string RecipientName { get; set; }

        [DisplayName("Recipient Address")]
        public string RecipientAddress { get; set; }

        [DisplayName("Recipient VAT Number")]
        public string RecipientVATNumber { get; set; }

        [DisplayName("Recipient Reference Number")]
        public string RecipientReferenceNumber { get; set; }

        public bool IsSuccessfull { get; set; }

        public class ValidateOccupancyDate : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // your validation logic
                if (Convert.ToDateTime(value) <= DateTime.Now.AddYears(-2))
                {
                    return new ValidationResult("Invalid Occupancy Date.");
                }

                return ValidationResult.Success;
            }
        }

    }
}
