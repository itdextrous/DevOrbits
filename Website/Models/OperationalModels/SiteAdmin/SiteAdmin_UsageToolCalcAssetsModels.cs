using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_UsageToolCalcAssetsModels
{
    public class SiteAdmin_UsageToolCalcAssetsModel
    {
        public List<Data.UsageCalc_Asset> UsageCalc_Assets { get; set; }
    }
    public class SiteAdmin_UsageToolCalcAssetsAddModel
    {
        [Display(Name = "Icon")]
        public string AssetIcon { get; set; }

        [Display(Name = "Name")]
        [Required]
        public string AssetName { get; set; }

        [Display(Name = "Average kWh")]
        [Required]
        public decimal AverageKWH { get; set; }

        [Display(Name = "Asset Type")]
        [Required]
        public List<SelectListItem> AssetType { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_UsageToolCalcAssetsEditModel
    {
        [Display(Name = "Icon")]
        public string AssetIcon { get; set; }

        [Display(Name = "Name")]
        [Required]
        public string AssetName { get; set; }

        [Display(Name = "Average kWh")]
        [Required]
        public decimal AverageKWH { get; set; }

        [Display(Name = "Asset Type")]
        [Required]
        public List<SelectListItem> AssetType { get; set; }

        public bool IsSuccess { get; set; }
    }


}
