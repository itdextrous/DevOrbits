using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.ModulesModels
{
    public class AddTimeOfWorkPlannedModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Date Of Work Planned")]
        public DateTime? DateOfWorkPlanned { get; set; }

        [Required]
        [Display(Name = "Minutes Planned For Work")]
        public int? MinOfWorkPlanned { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class EditTimeOfWorkPlannedModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Date Of Work Planned")]
        public DateTime? DateOfWorkPlanned { get; set; }

        [Required]
        [Display(Name = "Minutes Planned For Work")]
        public int? MinOfWorkPlanned { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AddTimeOfWorkAllocatedModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class EditTimeOfWorkAllocatedModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }

        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AddTravelAllocationModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string DescriptionOfTravelAllocation { get; set; }

        [Required]
        [Display(Name = "Date Of Travel Allocation")]
        public DateTime? DateOfTravelAllocation { get; set; }

        [Required]
        [Display(Name = "Vehicle Type")]
        public List<SelectListItem> VehicleTypeID { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public List<SelectListItem> VehicleID { get; set; }

        [Required]
        [Display(Name = "Vehicle Odo Start")]
        public decimal? VehicleOdoStart { get; set; }

        [Required]
        [Display(Name = "Vehicle Odo End")]
        public decimal? VehicleOdoEnd { get; set; }

        [Required]
        [Display(Name = "Distance Traveled in Km")]
        public decimal? VehicleDistanceKm { get; set; }

        [Display(Name = "Photo")]
        public IFormFile PhotoURL { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AddStockAllocationModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string DescriptionOfStockAllocation { get; set; }

        [Required]
        [Display(Name = "Date Of Stock Allocation")]
        public DateTime? DateOfStockAllocation { get; set; }

        public bool IsSuccess { get; set; }
    }
    public class AddInvoiceAllocationModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Required]
        [Display(Name = "Customer No")]
        public string CustomerNo { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string DescriptionOfInvoiceAllocation { get; set; }

        [Required]
        [Display(Name = "Date Of Invoice Allocation")]
        public DateTime? DateOfInvoiceAllocation { get; set; }

        [Required]
        [Display(Name = "Invoice Amount")]
        public decimal? InvoiceAmount { get; set; }

        [Required]
        [Display(Name = "Invoice No")]
        public string InvoiceNo { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class EditNonComplianceModel
    {
        public Data.ActivityTypeEnum ActivityType { get; set; }
        public int ActivityID { get; set; }

        [Display(Name = "Has Been Resolved")]
        public List<SelectListItem> HasBeenResolved { get; set; }

        [Display(Name = "User Comment")]
        public string UserComment { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Enforcer Comment")]
        public string EnforcerComment { get; set; }

        public bool IsSuccess { get; set; }
    }

}
