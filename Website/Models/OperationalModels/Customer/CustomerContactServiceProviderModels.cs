using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.CustomerContactServiceProviderModels
{
    public class Customer_ContactServiceProviderModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Message { get; set; }
        public IFormFile file { get; set; }
        public List<CustomerElectricityMeter> CustomerElectricityMeters { get; set; }
        public class CustomerElectricityMeter
        {
            public string Serial { get; set; }
            public bool ShowEmergencyConnectButton { get; set; }
            public bool ShowSwitchingButton { get; set; }
        }

        public bool isSent {get;set; }
    }
}
