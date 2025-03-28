using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_VehiclesModels
{
    public class SiteAdmin_VehiclesModel
    {
        public List<SiteAdmin_VehiclesItem> SiteAdmin_VehiclesItems { get; set; }

        public class SiteAdmin_VehiclesItem : Data.Vehicle
        {
            public string RegularDriverUsername { get; set; }
            public string OwnerUsername { get; set; }
            public MyVoltage.Api.MyVoltage.GetDeviceByIDResult.Device M2MDevice { get; set; }
        }
    }
    public class SiteAdmin_VehiclesEditModel
    {
        public Data.Vehicle Vehicle { get; set; }
        public MyVoltage.Api.MyVoltage.GetDeviceByIDResult.Device M2MDevice { get; set; }

        [Required]
        [Display(Name = "Meter ID (You can use this box to search exact serial OR enter M2M Device ID)")]
        public int? DeviceIDLinked { get; set; }

        [Required]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; }

        [Required]
        [Display(Name = "Vehicle Type (2010 Nissan NP200)")]
        public string VehicleTypeName { get; set; }

        [Required]
        [Display(Name = "RatePerKM")]
        public decimal? RatePerKM { get; set; }

        [Required]
        [Display(Name = "Regular Driver")]
        public List<SelectListItem> RegularDriverID { get; set; }

        [Display(Name = "Owner")]
        public List<SelectListItem> OwnerID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_VehiclesAddModel
    {
        [Required]
        [Display(Name = "Meter ID (You can use this box to search exact serial OR enter M2M Device ID)")]
        public int? DeviceIDLinked { get; set; }

        [Required]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; }

        [Required]
        [Display(Name = "Vehicle Type (2010 Nissan NP200)")]
        public string VehicleTypeName { get; set; }

        [Required]
        [Display(Name = "RatePerKM")]
        public decimal? RatePerKM { get; set; }

        [Required]
        [Display(Name = "Regular Driver")]
        public List<SelectListItem> RegularDriverID { get; set; }

        [Display(Name = "Owner")]
        public List<SelectListItem> OwnerID { get; set; }

        public bool IsSuccess { get; set; }
    }

}
