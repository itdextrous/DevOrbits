using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.B02_CouncilReadingsModels
{
    public class B02_CouncilReadingsModel_MirrorReadingUpdateModel
    {
        [Required(ErrorMessage = "Meter is required")]
        [Display(Name = "Meter")]
        public List<SelectListItem> BuildingCouncilMeter { get; set; }

        [Required(ErrorMessage = "Odo Reading is required")]
        [Display(Name = "Odo Reading")]
        [Range(0, double.MaxValue, ErrorMessage = "Odo Reading cannot be negative")]
        public decimal OdoReading { get; set; }

        [Required(ErrorMessage = "Date Logged Reading is required")]
        [Display(Name = "Date Logged")]
        public string DateLogged { get; set; }

        [Required(ErrorMessage = "Time Logged Reading is required")]
        [Display(Name = "Time Logged")]
        public string TimeLogged { get; set; }

        [Required(ErrorMessage = "Photo Reading is required")]
        [Display(Name = "Photo")]
        public IFormFile Photo { get; set; }

        public bool IsSuccessful { get; set; }

    }

    public class B02_CouncilReadings_CouncilReadingUpdateNoAccessModel
    {
        [Required(ErrorMessage = "Meter is required")]
        [Display(Name = "Meter")]
        public List<SelectListItem> BuildingCouncilMeter { get; set; }

        [Required(ErrorMessage = "Denied access photo required.")]
        [Display(Name = "Photo")]
        public IFormFile Photo { get; set; }

        public bool IsSuccessful { get; set; }

    }

    public class B02_CouncilReadings_CouncilReadingDetailsModel
    {
        public int CompletedToday
        {
            get
            {
                if (B02_CouncilReadings_CouncilReadingDetailsItems != null)
                {
                    return (from p in B02_CouncilReadings_CouncilReadingDetailsItems
                            where p.LatestAuditReadingTime.HasValue
                            && p.LatestAuditReadingTime.Value.Date == DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int LeftToday
        {
            get
            {
                if (B02_CouncilReadings_CouncilReadingDetailsItems != null)
                {
                    return (from p in B02_CouncilReadings_CouncilReadingDetailsItems
                            where p.LatestAuditReadingTime.HasValue
                            && p.LatestAuditReadingTime.Value.Date != DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int Total { get { return LeftToday + CompletedToday; } }

        public List<B02_CouncilReadings_CouncilReadingDetailsItem> B02_CouncilReadings_CouncilReadingDetailsItems { get; set; }

        public class B02_CouncilReadings_CouncilReadingDetailsItem
        {
            public string Serial { get; set; }
            public string DeviceName { get; set; }
            public decimal? LatestReading { get; set; }
            public DateTime? LatestReadingTime { get; set; }
            public decimal? LatestAuditReading { get; set; }
            public DateTime? LatestAuditReadingTime { get; set; }
            public DateTime? LatestAuditReadingCreated { get; set; }
            public string LatestAuditReadingName { get; set; }
            public string LatestAuditReadingUploadName { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes Status { get; set; }
            public Data.BuildingCouncilMeter BuildingCouncilMeter { get; set; }
        }

    }

    public class A02_MirrorMeterAuditing_MirrorReadingResultsModel
    {
        public int CompletedToday
        {
            get
            {
                if (A02_MirrorMeterAuditing_MirrorReadingResultsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorReadingResultsItems
                            where p.LatestAuditReadingTime.Date == DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int LeftToday
        {
            get
            {
                if (A02_MirrorMeterAuditing_MirrorReadingResultsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorReadingResultsItems
                            where p.LatestAuditReadingTime.Date != DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int Total { get { return LeftToday + CompletedToday; } }

        public List<A02_MirrorMeterAuditing_MirrorReadingResultsItem> A02_MirrorMeterAuditing_MirrorReadingResultsItems { get; set; }

        public class A02_MirrorMeterAuditing_MirrorReadingResultsItem
        {
            public string Serial { get; set; }
            public string DeviceName { get; set; }
            public decimal LatestReading { get; set; }
            public DateTime LatestReadingTime { get; set; }
            public decimal LatestAuditReading { get; set; }
            public DateTime LatestAuditReadingTime { get; set; }
            public DateTime LatestAuditReadingCreated { get; set; }
            public string LatestAuditReadingName { get; set; }
            public string LatestAuditReadingUploadName { get; set; }
            public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes Status { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
        }

    }


    public class B02_CouncilReadings_CouncilReadingSummaryModel
    {
        public List<B02_CouncilReadings_CouncilReadingSummaryItem> B02_CouncilReadings_CouncilReadingSummaryItems { get; set; }

        public class B02_CouncilReadings_CouncilReadingSummaryItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int CouncilMeterCount { get; set; }
            public int ReadingsUploadedCount { get; set; }
            public int ReadingsVerifiedCount { get; set; }
            public int ReadingsSubmittedCount { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes Status { get; set; }
            public string PaymentTypes { get; set; }
        }
    }

    public class B02_CouncilReadings_CouncilReadingVerificationModel
    {
        public int B02_CouncilReadings_CouncilReadingUpdateID { get; set; }
        public decimal LatestReading { get; set; }
        public DateTime LatestReadingDateTime { get; set; }
        public decimal LatestReadingOdo { get; set; }
        public DateTime LatestReadingDateTimeOdo { get; set; }
        public DateTime LatestReadingOdoCreatedDateTime { get; set; }
        public string LatestReadingOdoCreatedBy { get; set; }
        public string LatestReadingOdoPhotoURL { get; set; }
        public decimal VerificationReading { get; set; }
        public DateTime VerificationReadingDateTime { get; set; }
        public Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes Status { get; set; }
        public string ChangedByUserName { get; set; }
        public DateTime? ChangedDate { get; set; }

        public string SubmittedByUserName { get; set; }
        public DateTime? SubmittedDate { get; set; }

        public string DeviceName { get; set; }
        public string DeviceSerial { get; set; }
        public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }

        public decimal CalculatedDifferenceReading
        {
            get
            {
                return LatestReadingOdo - VerificationReading;
            }
        }


        public List<B02_CouncilReadings_CouncilReadingVerificationAction> B02_CouncilReadings_CouncilReadingVerificationActions
        {
            //1 = Sorted, I fixed the problem
            //2 = Help, there is no consumption recorded from the meter
            //3 = Help, the consumption recorded is not billed on Skybill
            //4 = Nope, the system made a calculation error, everything is working as it should
            get
            {
                List<B02_CouncilReadings_CouncilReadingVerificationAction> actions = new List<B02_CouncilReadings_CouncilReadingVerificationAction>();

                if (Status == Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified)
                    actions.Add(new B02_CouncilReadings_CouncilReadingVerificationAction() { ActionID = 1, ActionDisplayName = "Verify", ActionDescription = "Verify reading is correct and can be added.", ActionIcon = "check-circle", ActionColor = "" });

                if (Status != Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Rejected)
                    actions.Add(new B02_CouncilReadings_CouncilReadingVerificationAction() { ActionID = 2, ActionDisplayName = "Reject", ActionDescription = "Reject the reading", ActionIcon = "exclamation-circle", ActionColor = "text-red" });

                if (Status == Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified)
                    actions.Add(new B02_CouncilReadings_CouncilReadingVerificationAction() { ActionID = 3, ActionDisplayName = "Submitted To Council", ActionDescription = "Submitted To Council", ActionIcon = "check-circle", ActionColor = "text-green" });


                return actions;
            }
        }

        public class B02_CouncilReadings_CouncilReadingVerificationAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }


    public class B02_CouncilDevice_CouncilMetersSummaryModel
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int CustomersCount { get; set; }
        public int MetersCount { get; set; }
        public string TableRowID { get; set; }

        public StatusType Status
        {
            get
            {
                return StatusType.Completed;
            }
        }

        public enum StatusType
        {
            [Description("Completed")]
            Completed = 1,
            [Description("Outstanding")]
            Outstanding = 2,
        }


    }

    public class B02_CouncilDevice_CouncilMetersDetailsModel
    {
        public List<B02_CouncilDevice_CouncilMetersDetailsItem> B02_CouncilDevice_CouncilMetersDetailsItems { get; set; }

        public class B02_CouncilDevice_CouncilMetersDetailsItem
        {
            public Data.BuildingCouncilMeter BuildingCouncilMeter { get; set; }
            public MyVoltageApi.Data.Device MirrorDevice { get; set; }
            public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate A02_MirrorMeterAuditing_MirrorReadingUpdate { get; set; }
            public LatestReadingItem LatestReading { get; set; }
            public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
            public Data.Company Company { get; set; }

            public class LatestReadingItem
            {
                public DateTime? TimeLogged { get; set; }
                public decimal? VirtualOdoReading { get; set; }
            }
        }
    }


    public class B02_CouncilDevice_CouncilMetersReviewModel
    {
        public long MirrorDeviceID { get; set; }

        [Required]
        [Display(Name = "Device ID Linked (Metering DB)")]
        public int DeviceIDLinked { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Correcting Factor")]
        public decimal CorrectingFactor { get; set; }

        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Display(Name = "Selected Meter")]
        public List<SelectListItem> BuildingCouncilMeter { get; set; }

        public MyVoltageApi.Data.Device MirrorDevice { get; set; }
        public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate A02_MirrorMeterAuditing_MirrorReadingUpdate { get; set; }
        public LatestReadingItem LatestReading { get; set; }
        public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
        public Data.Company Company { get; set; }

        public class LatestReadingItem
        {
            public DateTime? TimeLogged { get; set; }
            public decimal? VirtualOdoReading { get; set; }
        }
    }

    public class B02_CouncilReadings_CouncilReadingPlannerModel
    {
        public List<SelectListItem> StatusFilter { get; set; }

        public List<B02_CouncilReadings_CouncilReadingPlannerItem> B02_CouncilReadings_CouncilReadingPlannerItems { get; set; }

        public class B02_CouncilReadings_CouncilReadingPlannerItem
        {
            public Data.Company Company { get; set; }
            public Data.BuildingCycle BuildingCycle { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate B02_CouncilReadings_CouncilReadingUpdate { get; set; }
            public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
            public Data.BuildingCouncilType BuildingCouncilType { get; set; }
            public Data.BuildingCouncilMeter BuildingCouncilMeter { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes Status { get; set; }
        }
    }

}
