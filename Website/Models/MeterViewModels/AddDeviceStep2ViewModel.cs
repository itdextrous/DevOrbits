using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class AddDeviceStep2ViewModel
    {
        [Display(Name = "Gateway ID")]
        public int GatewayID { get; set; }

        [Display(Name = "Gateway Name")]
        public string GatewayName { get; set; }

        [Display(Name = "Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        [Display(Name = "Meter type")]
        public List<SelectListItem> MeterTypes { get; set; }




        public string ErrorMessage{get;set; }
    }
}
