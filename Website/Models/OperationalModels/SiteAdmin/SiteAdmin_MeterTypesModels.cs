using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_MeterTypesViewModel
    {
        public List<Data.MeterType> SiteAdmin_MeterTypes { get; set; }
    }

    public class AddSiteAdmin_MeterTypeViewModel
    {
        [Display(Name = "Type Name")]
        [Required]
        public string TypeName { get; set; }

        [Display(Name = "Default Port")]
        public int? Port { get; set; }

        [Display(Name = "Default Protocol")]
        public int? Protocol { get; set; }

        [Display(Name = "Default Remote Address")]
        public string RemoteAddress { get; set; }

        [Display(Name = "Default Remote Index")]
        public int? RemoteIndex { get; set; }

        [Display(Name = "Default Process Interval")]
        public int? ProcessInterval { get; set; }

        [Display(Name = "Requires Odo")]
        public bool RequiresOdo { get; set; }

        [Display(Name = "Device Type")]
        public List<SelectListItem> DeviceTypes { get; set; }

        [Display(Name = "Create On Mirror")]
        public bool CreateOnMirror { get; set; }

        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Default Config Value")]
        public string Config { get; set; }

        [Display(Name = "Default Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class EditSiteAdmin_MeterTypeViewModel
    {
        [Display(Name = "Type Name")]
        [Required]
        public string TypeName { get; set; }

        [Display(Name = "Default Port")]
        public int? Port { get; set; }

        [Display(Name = "Default Protocol")]
        public int? Protocol { get; set; }

        [Display(Name = "Default Remote Address")]
        public string RemoteAddress { get; set; }

        [Display(Name = "Default Remote Index")]
        public int? RemoteIndex { get; set; }

        [Display(Name = "Default Process Interval")]
        public int? ProcessInterval { get; set; }

        [Display(Name = "Requires Odo")]
        public bool RequiresOdo { get; set; }

        [Display(Name = "Device Type")]
        public List<SelectListItem> DeviceTypes { get; set; }

        [Display(Name = "Create On Mirror")]
        public bool CreateOnMirror { get; set; }

        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Default Config Value")]
        public string Config { get; set; }

        [Display(Name = "Default Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }
    }
}
