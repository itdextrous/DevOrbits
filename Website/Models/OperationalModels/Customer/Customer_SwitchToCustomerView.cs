using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer
{
    public class Customer_SwitchToCustomerViewModel
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
}
