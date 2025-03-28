using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.G_Communication.G_CommunicationModels
{
    public class G_Communication_BulkCommunicationModel
    {
        [DisplayName("Customer Number")]
        [Required]
        public List<SelectListItem> SkybillCustomerNos { get; set; }

        [DisplayName("E-Mail Subject")]
        public string EmailSubject { get; set; }

        [DisplayName("E-Mail Content(Body) - Leave blank for no E-Mail")]
        public string EmailBody { get; set; }

        [DisplayName("E-Mail Attachment")]
        public IFormFile file { get; set; }

        [DisplayName("SMS Content(Body) - Leave blank for no SMS")]
        public string SMSBody { get; set; }

        [DisplayName("Mobile App Notification - Leave blank for no SMS")]
        public string MobileAppNotification { get; set; }

        [DisplayName("Mobile App Notification Expiry Date - Leave blank for 24 hours")]
        public DateTime? MobileAppNotificationExpiry { get; set; }

        public string Result { get; set; }

        public bool IsSuccess { get; set; }

        //public class ValidateSMSLength : ValidationAttribute
        //{
        //    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        //    {
        //        // your validation logic
        //        if (value.ToString().Length <= 160)
        //        {
        //            return ValidationResult.Success;
        //        }
        //        else
        //        {
        //            return new ValidationResult($"SMS characters may not be more than 160 ({value.ToString().Length} currently)");
        //        }
        //    }
        //}

    }

    public class G_Communication_NotificationLogModel
    {
        public int TotalEntries { get; set; }
        public PaginatedList<NotificationLogItem> Log_Notifications { get; set; }

        public class NotificationLogItem : Log_Notification
        {
            public string CustomerNo { get; set; }
            public string CompanyName { get; set; }
        }
        [DisplayName("Customer Number")]
        public string CustomerNumber { get; set; }
    }

    public class G_Communication_AppNotificationLogModel
    {
        public int TotalEntries { get; set; }
        public PaginatedList<NotificationLogItem> Log_Notifications { get; set; }

        public class NotificationLogItem : Customer_AppNotification
        {
            public string CustomerNo { get; set; }
            public string CompanyName { get; set; }
        }
        [DisplayName("Customer Number")]
        public string CustomerNumber { get; set; }
    }
}
