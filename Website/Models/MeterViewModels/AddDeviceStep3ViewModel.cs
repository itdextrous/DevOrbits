using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class AddDeviceStep3ViewModel
    {
        [Display(Name = "Gateway ID")]
        public int GatewayID { get; set; }

        [Display(Name = "Gateway Name")]
        public string GatewayName { get; set; }

        [Display(Name = "Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        [Display(Name = "Meter type")]
        public string MeterType { get; set; }

        [Display(Name = "Device type")]
        public string DeviceType { get; set; }


        [Display(Name = "Serial Number")]
        [Required]
        public string SerialNumber { get; set; }

        [Display(Name = "Name")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Port")]
        public string Port { get; set; }
        public bool ShowPort { get; set; }


        [Display(Name = "Protocol")]
        public string Protocol { get; set; }
        public bool ShowProtocol { get; set; }


        [Display(Name = "RemoteAddress")]
        public string RemoteAddress { get; set; }
        public bool ShowRemoteAddress { get; set; }


        [Display(Name = "RemoteIndex")]
        public string RemoteIndex { get; set; }
        public bool ShowRemoteIndex { get; set; }


        [Display(Name = "ProcessInterval")]
        public string ProcessInterval { get; set; }
        public bool ShowProcessInterval { get; set; }


        [Display(Name = "Odo Reading")]
        public string Odo { get; set; }
        [Display(Name = "Odo Reading Time")]
        public string OdoReadingTime { get; set; }
        public bool ShowOdo { get; set; }


        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Picture")]
        public IFormFile file { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }
}
