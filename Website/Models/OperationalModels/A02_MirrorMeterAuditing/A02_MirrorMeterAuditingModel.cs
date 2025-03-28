using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A02_MirrorMeterAuditing
{
    public class A02_MirrorMeterAuditingModel_MirrorReadingUpdateModel
    {
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

        public string RemoteAddress { get; set; }
    }

    public class A02_MirrorMeterAuditingModel_MirrorReadingUpdateNoAccessModel
    {
        [Required(ErrorMessage = "Denied access photo required.")]
        [Display(Name = "Photo")]
        public IFormFile Photo { get; set; }

        public bool IsSuccessful { get; set; }

        public string RemoteAddress { get; set; }
    }

    public class A02_MirrorMeterAuditing_MirrorReadingDetailsModel
    {
        public int CompletedToday
        {
            get
            {
                if (A02_MirrorMeterAuditing_MirrorReadingDetailsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorReadingDetailsItems
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
                if (A02_MirrorMeterAuditing_MirrorReadingDetailsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorReadingDetailsItems
                            where p.LatestAuditReadingTime.Date != DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int Total { get { return LeftToday + CompletedToday; } }

        public int TotalEntries { get; set; }
        public int EntriesPerPage { get; set; }
        public PaginatedList<A02_MirrorMeterAuditing_MirrorReadingDetailsItem> A02_MirrorMeterAuditing_MirrorReadingDetailsItems { get; set; }

        public class A02_MirrorMeterAuditing_MirrorReadingDetailsItem
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
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
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

        public int TotalEntries { get; set; }
        public int EntriesPerPage { get; set; }
        public PaginatedList<A02_MirrorMeterAuditing_MirrorReadingResultsItem> A02_MirrorMeterAuditing_MirrorReadingResultsItems { get; set; }

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
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
        }

    }

    public class A02_MirrorMeterAuditing_MirrorReadingSummaryItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int CustomersCount { get; set; }
        public int CustomersCheckedCount { get; set; }
        public int CustomersNotCheckedCount { get; set; }
        public string Status { get; set; }
    }
    public class A02_MirrorMeterAuditing_MirrorReadingSummaryModel
    {
        public List<A02_MirrorMeterAuditing_MirrorReadingSummaryItem> A02_MirrorMeterAuditing_MirrorReadingSummaryItems { get; set; }
    }

    public class A02_MirrorMeterAuditing_MirrorReadingVerificationModel
    {
        public int A02_MirrorMeterAuditing_MirrorReadingUpdateID { get; set; }
        public decimal LatestReading { get; set; }
        public DateTime LatestReadingDateTime { get; set; }
        public decimal LatestReadingOdo { get; set; }
        public DateTime LatestReadingDateTimeOdo { get; set; }
        public DateTime LatestReadingOdoCreatedDateTime { get; set; }
        public string LatestReadingOdoCreatedBy { get; set; }
        public string LatestReadingOdoPhotoURL { get; set; }
        public decimal VerificationReading { get; set; }
        public DateTime VerificationReadingDateTime { get; set; }
        public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes Status { get; set; }
        public string ChangedByUserName { get; set; }
        public DateTime? ChangedDate { get; set; }
        public decimal CalculatedDifferenceReading
        {
            get
            {
                return LatestReadingOdo - VerificationReading;
            }
        }

        public int? PreviousOdoID { get; set; }
        public bool? PreviousOdoRequiresRecalc { get; set; }
        public decimal? PreviousReadingOdo { get; set; }
        public DateTime? PreviousReadingDateTimeOdo { get; set; }
        public DateTime? PreviousReadingOdoCreatedDateTime { get; set; }
        public decimal? PreviousVerificationReading { get; set; }
        public DateTime? PreviousVerificationReadingDateTime { get; set; }
        public decimal? PreviousCalculatedDifferenceReading
        {
            get
            {
                return PreviousReadingOdo - PreviousVerificationReading;
            }
        }

        public List<A02_MirrorMeterAuditing_MirrorReadingVerificationAction> A02_MirrorMeterAuditing_MirrorReadingVerificationActions
        {
            //1 = Sorted, I fixed the problem
            //2 = Help, there is no consumption recorded from the meter
            //3 = Help, the consumption recorded is not billed on Skybill
            //4 = Nope, the system made a calculation error, everything is working as it should
            get
            {
                List<A02_MirrorMeterAuditing_MirrorReadingVerificationAction> actions = new List<A02_MirrorMeterAuditing_MirrorReadingVerificationAction>();

                if (Status == Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified)
                {
                    //if (PreviousCalculatedDifferenceReading.HasValue && PreviousCalculatedDifferenceReading.Value != 0)
                    //{
                    //    if (PreviousOdoRequiresRecalc.HasValue && !PreviousOdoRequiresRecalc.Value)
                    //        actions.Add(new A02_MirrorMeterAuditing_MirrorReadingVerificationAction() { ActionID = 3, ActionDisplayName = "Recalc", ActionDescription = "Recalc reading.", ActionIcon = "redo-alt", ActionColor = "" });
                    //}
                    //else
                    actions.Add(new A02_MirrorMeterAuditing_MirrorReadingVerificationAction() { ActionID = 1, ActionDisplayName = "Verify", ActionDescription = "Verify reading is correct and can be added.", ActionIcon = "check-circle", ActionColor = "btn-outline-success" });
                }

                if (Status != Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected)
                    actions.Add(new A02_MirrorMeterAuditing_MirrorReadingVerificationAction() { ActionID = 2, ActionDisplayName = "Reject", ActionDescription = "Reject the reading", ActionIcon = "exclamation-circle", ActionColor = "btn-outline-danger-sm" });


                return actions;
            }
        }

        public class A02_MirrorMeterAuditing_MirrorReadingVerificationAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A02_MirrorMeterAuditing_MeterCalibrationSummaryItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int CustomersCount { get; set; }
        public int MetersCount { get; set; }
        public int MetersCalibratedCount { get; set; }
        public int MetersNotCalibratedCount { get; set; }
        public int MetersProblematicCount { get; set; }
        public int MetersThatCannotBeCalibratedCount { get; set; }
        public string TableRowID { get; set; }
        public StatusType Status { get; set; }

        public enum StatusType
        {
            [Description("Completed")]
            Reviewed = 1,
            [Description("Not Billed")]
            NotBilled = 2,
            [Description("Outstanding")]
            Outstanding = 3,
        }

        public static string GetStatusString(StatusType statusType)
        {
            return statusType.GetDescription();
        }
    }
    public class A02_MirrorMeterAuditing_MeterCalibrationSummaryModel
    {
        public List<A02_MirrorMeterAuditing_MeterCalibrationSummaryItem> A02_MirrorMeterAuditing_MeterCalibrationSummaryItems { get; set; }
    }

    public class A02_MirrorMeterAuditing_MeterCalibrationDetailItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public List<A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer> A02_MirrorMeterAuditing_MeterCalibrationDetailItems { get; set; }

        public class A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer
        {
            public string CompanyName { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerMeterName { get; set; }
            public string CustomerMeterSerial { get; set; }
            public DateTime? CalibrationDate { get; set; }

            public long MirrorDeviceID { get; set; }
            public decimal LatestReadingOdo { get; set; }
            public decimal VerificationReading { get; set; }
            public decimal CalculatedDifferenceReading
            {
                get
                {
                    return LatestReadingOdo - VerificationReading;
                }
            }

            public DateTime? ExpiryDate { get; set; }


            public Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes Status { get; set; }
        }
    }
    public class A02_MirrorMeterAuditing_MeterCalibrationDetailModel
    {
        public int TotalEntries { get; set; }
        public int EntriesPerPage { get; set; }
        public PaginatedList<A02_MirrorMeterAuditing_MeterCalibrationDetailItem.A02_MirrorMeterAuditing_MeterCalibrationDetailItemCustomer> A02_MirrorMeterAuditing_MeterCalibrationDetailItems { get; set; }
    }

    public class A02_MirrorMeterAuditing_MeterCalibrationVerificationModel
    {
        public int A02_MirrorMeterAuditing_MeterCalibrationUpdateID { get; set; }
        public Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes Status { get; set; }

        public List<A02_MirrorMeterAuditing_MeterCalibrationVerificationAction> A02_MirrorMeterAuditing_MeterCalibrationVerificationActions
        {
            //1 = Sorted, I fixed the problem
            //2 = Help, there is no consumption recorded from the meter
            //3 = Help, the consumption recorded is not billed on Skybill
            //4 = Nope, the system made a calculation error, everything is working as it should
            get
            {
                List<A02_MirrorMeterAuditing_MeterCalibrationVerificationAction> actions = new List<A02_MirrorMeterAuditing_MeterCalibrationVerificationAction>();


                if (Status != Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated)
                    actions.Add(new A02_MirrorMeterAuditing_MeterCalibrationVerificationAction() { ActionID = 1, ActionDisplayName = "Confirmed Calibration", ActionDescription = "Take a snapshot of calibration.", ActionIcon = "check-circle", ActionColor = "text-green" });

                actions.Add(new A02_MirrorMeterAuditing_MeterCalibrationVerificationAction() { ActionID = 2, ActionDisplayName = "Problematic", ActionDescription = "Problematic", ActionIcon = "exclamation-circle", ActionColor = "text-red" });

                actions.Add(new A02_MirrorMeterAuditing_MeterCalibrationVerificationAction() { ActionID = 3, ActionDisplayName = "In Progress", ActionDescription = "In Progress", ActionIcon = "exclamation-circle", ActionColor = "" });


                return actions;
            }
        }

        public class A02_MirrorMeterAuditing_MeterCalibrationVerificationAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A02_MirrorMeterAuditing_MirrorChecklistSummaryItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int CustomersCount { get; set; }
        public int MetersCount { get; set; }
        public int MetersCapturedCount { get; set; }
        public int MetersLeft { get { return MetersCount - MetersCapturedCount; } }
        public string TableRowID { get; set; }
        public StatusType Status
        {
            get
            {
                if (MetersLeft == 0)
                    return StatusType.Completed;
                else
                    return StatusType.Outstanding;
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
    public class A02_MirrorMeterAuditing_MirrorChecklistSummaryModel
    {
        public List<A02_MirrorMeterAuditing_MirrorChecklistSummaryItem> A02_MirrorMeterAuditing_MirrorChecklistSummaryItems { get; set; }
    }

    public class A02_MirrorMeterAuditing_MirrorChecklistDetailsModel
    {
        public int CompletedToday
        {
            get
            {
                if (A02_MirrorMeterAuditing_MirrorChecklistDetailsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorChecklistDetailsItems
                            where p.Status == A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Completed
                            select p).Count();
                }

                return 0;
            }
        }
        public int LeftToday
        {
            get
            {
                if (A02_MirrorMeterAuditing_MirrorChecklistDetailsItems != null)
                {
                    return (from p in A02_MirrorMeterAuditing_MirrorChecklistDetailsItems
                            where p.Status == A02_MirrorMeterAuditing_MirrorChecklistDetailsItem.StatusType.Outstanding
                            select p).Count();
                }

                return 0;
            }
        }
        public int Total { get { return LeftToday + CompletedToday; } }

        public List<A02_MirrorMeterAuditing_MirrorChecklistDetailsItem> A02_MirrorMeterAuditing_MirrorChecklistDetailsItems { get; set; }

        public class A02_MirrorMeterAuditing_MirrorChecklistDetailsItem
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
            public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes LatestEntryStatus { get; set; }
            public StatusType Status { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public string DeviceStatus { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public enum StatusType
            {
                [Description("Outstanding")]
                Outstanding = 1,
                [Description("Completed")]
                Completed = 2,
            }
        }

    }

    public class A02_MirrorMeterAuditing_MirrorDeviceSearchModel
    {
        public string SearchTerm { get; set; }
        public List<A02_MirrorMeterAuditing_MirrorDeviceSearchItem> A02_MirrorMeterAuditing_MirrorDeviceSearchItems { get; set; }

        public class A02_MirrorMeterAuditing_MirrorDeviceSearchItem
        {
            public Data.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
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

    public class A02_MirrorMeterAuditing_MirrorDeviceSummaryModel
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
    public class A02_MirrorMeterAuditing_MirrorDeviceDetailsModel
    {
        public List<A02_MirrorMeterAuditing_MirrorDeviceDetailsItem> A02_MirrorMeterAuditing_MirrorDeviceDetailsItems { get; set; }

        public class A02_MirrorMeterAuditing_MirrorDeviceDetailsItem
        {
            public Data.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
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
    public class A02_MirrorMeterAuditing_MirrorDeviceReviewModel
    {
        public long MirrorDeviceID { get; set; }

        //[Required]
        [Display(Name = "Device ID Linked (Metering DB)")]
        public int DeviceIDLinked { get; set; }

        //[Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        //[Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        //[Required]
        [Display(Name = "Correcting Factor")]
        public decimal CorrectingFactor { get; set; }

        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Required]
        [Display(Name = "Conversion Factor")]
        public decimal? ConvFactor { get; set; }

        //[Required]
        [Display(Name = "Consumption Tariff")]
        public List<SelectListItem> ConsumptionTariffCode { get; set; }

        //[Required]
        [Display(Name = "Converted Consumption Tariff")]
        public List<SelectListItem> ConvertedConsumptionTariffCode { get; set; }


        public Data.Device Device { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
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

    public class A02_MirrorMeterAuditing_M2MMirrorReconSummaryModel
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
    public class A02_MirrorMeterAuditing_M2MMirrorReconDetailsModel
    {
        public List<A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem> A02_MirrorMeterAuditing_M2MMirrorReconDetailsItems { get; set; }

        public class A02_MirrorMeterAuditing_M2MMirrorReconDetailsItem
        {
            public Data.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
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
    public class A02_MirrorMeterAuditing_M2MMirrorReconReviewModel
    {
        public long MirrorDeviceID { get; set; }

        //[Required]
        [Display(Name = "Device ID Linked (Metering DB)")]
        public int DeviceIDLinked { get; set; }

        //[Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        //[Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        //[Required]
        [Display(Name = "Correcting Factor")]
        public decimal CorrectingFactor { get; set; }

        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Required]
        [Display(Name = "Conversion Factor")]
        public decimal? ConvFactor { get; set; }

        //[Required]
        [Display(Name = "Consumption Tariff")]
        public List<SelectListItem> ConsumptionTariffCode { get; set; }

        //[Required]
        [Display(Name = "Converted Consumption Tariff")]
        public List<SelectListItem> ConvertedConsumptionTariffCode { get; set; }


        public Data.Device Device { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
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


    public class A02_MirrorMeterAuditing_MirrorDeviceAddModel
    {
        public string SerialNumber { get; set; }
        public MyVoltage.Api.MyVoltage.Device m2mDevice { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
        public Data.Device Device { get; set; }
        public Data.Company Company { get; set; }
        public MyVoltageApi.Data.Device MirrorDevice { get; set; }
    }
    public class A02_MirrorMeterAuditing_MirrorReadingDeleteModel
    {
        [Required(ErrorMessage = "Date Logged Reading is required")]
        [Display(Name = "Date Logged")]
        public string DateLogged { get; set; }

        [Required(ErrorMessage = "Time Logged Reading is required")]
        [Display(Name = "Time Logged")]
        public string TimeLogged { get; set; }

        public bool IsSuccessful { get; set; }
        public int? RowsRemoved { get; set; }

        public string RemoteAddress { get; set; }
    }

    public class A02_MirrorMeterAuditing_OdoReadingExportModel
    {
        public int EntriesPerPage { get; set; }
        public int TotalEntries { get; set; }

        public PaginatedList<A02_MirrorMeterAuditing_OdoReadingExportItem> A02_MirrorMeterAuditing_OdoReadingExportItems { get; set; }

        public class A02_MirrorMeterAuditing_OdoReadingExportItem
        {
            public MyVoltageApi.Data.Device MirrorDevice { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public LatestOdoItem LatestOdo { get; set; }
            public string SkybillCustomerNo { get; set; }

            public class LatestOdoItem
            {
                public DateTime? TimeLogged { get; set; }
                public decimal? VirtualOdoReading { get; set; }
                public decimal? register_scaling { get; set; }
                public string unit { get; set; }
            }
        }
    }
}
