using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class AddDeviceViewModel
    {
        [Display(Name = "Gateway ID")]
        [Required]
        public int GatewayID { get; set; }


        public string ErrorMessage{get;set; }
    }
}
