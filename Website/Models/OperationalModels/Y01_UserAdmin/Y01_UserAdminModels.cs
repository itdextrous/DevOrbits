using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Y01_UserAdmin
{
    public class UserMeterItem : UserMeterSerial
    {
        public string DisplayName { get; set; }
    }

    public class Y01_UserAdmin_UserSearchModel
    {
        public System.Data.DataTable SearchResultsTable { get; set; }
    }

    public class Y01_UserAdminSearchResultsModel
    {
        [Required]
        public string SearchString { get; set; }
        public System.Data.DataTable SearchResultsTable { get; set; }
    }
    public class Y01_UserAdmin_UserEditModel
    {
        public string Email { get; set; }
        public string UserID { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Internal Charge Out Rate Per Hour")]
        public decimal InternalChargeOutRatePerHour { get; set; }

        [Display(Name = "External Charge Out Rate Per Hour")]
        public decimal ExternalChargeOutRatePerHour { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [CValidations.ValidatePhoneNumber]
        public string PhoneNumber { get; set; }

        [Display(Name = "Has access to all companies")]
        public bool HasAccessToAllCompanies { get; set; }

        [Display(Name = "Has access to all meters in linked companies")]
        public bool HasAccessToAllMetersInLinkedCompanies { get; set; }

        [Display(Name = "3Cx Rate Per Min")]
        public decimal? ThreeCxRatePerMin { get; set; }

        [Display(Name = "3Cx VOIP Ext No")]
        public int? ThreeCxVOIPExt { get; set; }

        [Display(Name = "Partner Group")]
        public List<SelectListItem> SiteAdmin_Partners { get; set; }

        [Display(Name = "Organizational Status")]
        public List<SelectListItem> OrganizationalStatusID { get; set; }

        public List<UserSecureAreaAction> UserSecureAreaActions { get; set; }
        public List<Company> Companies { get; set; }
        public List<UserCompany> UserCompanies { get; set; }

        public List<KeyValuePair<string, string>> MeterSerials { get; set; }
        public List<UserMeterItem> UserMeterSerials { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Y01_UserAdmin_UserSwitchModel
    {
        [Required]
        [Display(Name = "Company")]
        public List<SelectListItem> Company { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public List<SelectListItem> Customer { get; set; }

        public class CustomerItem
        {
            public string ID { get; set; }
            public int CompanyID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<CustomerItem> CustomerItems { get; set; }

    }

    public class Y01_UserAdmin_UserAddModel
    {
        [Required]
        [Display(Name = "Email Address")]
        [RegularExpression(@"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$)", ErrorMessage = "Email Address incorrect.")]
        public string Email { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Internal Charge Out Rate Per Hour")]
        public decimal InternalChargeOutRatePerHour { get; set; }

        [Display(Name = "External Charge Out Rate Per Hour")]
        public decimal ExternalChargeOutRatePerHour { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [CValidations.ValidatePhoneNumber]
        public string PhoneNumber { get; set; }

        [Display(Name = "Default Company")]
        [Required]
        public List<SelectListItem> DefaultCompany { get; set; }

        [Display(Name = "Has access to all companies")]
        public bool HasAccessToAllCompanies { get; set; }

        [Display(Name = "Has access to all meters in linked companies")]
        public bool HasAccessToAllMetersInLinkedCompanies { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ResultUserID { get; set; }
    }


}
