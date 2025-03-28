using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels
{
    public class AF_AfroxAdministration_Metering_SummaryModel
    {
        public List<AF_AfroxAdministration_Metering_SummaryItem> AF_AfroxAdministration_Metering_SummaryItems { get; set; }

        public List<SelectListItem> Partners { get; set; }
        public List<SelectListItem> ResponsiblePerson { get; set; }
        //public List<SelectListItem> ActionRequired { get; set; }
        //public DateTime? FromDate { get; set; }
        //public DateTime? ToDate { get; set; }
        public List<SelectListItem> AvailableUsers { get; set; }
        public List<string> Manufacturers { get; set; }
        public class AF_AfroxAdministration_Metering_SummaryItem : Data.Company
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string PartnerName { get; set; }
            public string TableRowID { get; set; }
            public List<string> Manufacturers { get; set; }
            public int GatewaysOnline { get; set; }
            public int GatewaysOffline { get; set; }
            public int GatewaysUnknown { get; set; }
            public int MetersOnline { get; set; }
            public int MetersOffline { get; set; }
            public int MetersNotInstalled { get; set; }
            public int MetersUnknown { get; set; }
            public int TotalCount { get; set; }
            public bool InformationComplete { get { return TotalCount != 0 && TotalCount == InformationCompleteCount; } }
            public int InformationCompleteCount { get; set; }
            public bool SystemSetupSignOff { get { return TotalCount != 0 && TotalCount == SystemSetupSignOffCount; } }
            public int SystemSetupSignOffCount { get; set; }
            public bool CustomerSetupSignOff { get { return TotalCount != 0 && TotalCount == CustomerSetupSignOffCount; } }
            public int CustomerSetupSignOffCount { get; set; }
            public bool Verification { get { return TotalCount != 0 && TotalCount == VerificationCount; } }
            public int VerificationCount { get; set; }
            public bool VerificationUploadReview { get { return VerificationUploadReviewTotalCount != 0 && VerificationUploadReviewTotalCount == VerificationUploadReviewCount; } }
            public int VerificationUploadReviewCount { get; set; }
            public int VerificationUploadReviewTotalCount { get; set; }
            public int MetrixMeters { get; set; }
            public int ReplaceMeters { get; set; }
            public int OutstandingMeters { get; set; }
            public decimal MetrixMeterPerc
            {
                get
                {
                    if (MeterManufacturers != null && MeterManufacturers.Count != 0)
                    {
                        int metrix = 0;
                        int all = 0;
                        foreach (var kvp in MeterManufacturers)
                        {
                            if (kvp.Key.ToUpper() == "METRIX")
                                metrix += kvp.Value;
                            all += kvp.Value;
                        }

                        if (all != 0)
                            return (Convert.ToDecimal(metrix) / Convert.ToDecimal(all)) * 100.0m;
                    }

                    return 0;

                    if ((OutstandingMeters + ReplaceMeters + MetrixMeters) > 0)
                        return (Convert.ToDecimal(MetrixMeters) / Convert.ToDecimal(OutstandingMeters + ReplaceMeters + MetrixMeters)) * 100.0m;
                }
            }
            public string ResponsibleUsername { get; set; }
            public bool MeterSerialNoCheck { get { return TotalCount != 0 && TotalCount == MeterSerialNoCheckCount; } }
            public int MeterSerialNoCheckCount { get; set; }
            public E01_BuildingOnboardingTask Next_E01_BuildingOnboardingTask { get; set; }
            public class E01_BuildingOnboardingTask : Data.E01_BuildingOnboardingTask
            {
                public string ResponsibleUserUsername { get; set; }
                public Data.E01_BuildingOnboardingTask_Type E01_BuildingOnboardingTask_Type { get; set; }
            }
            public Dictionary<string, int> MeterManufacturers { get; set; }
        }
    }

    public class AF_AfroxAdministration_Metering_DetailsModel
    {
        public List<AF_AfroxAdministration_Metering_DetailsItem> AF_AfroxAdministration_Metering_DetailsItems { get; set; }

        public class AF_AfroxAdministration_Metering_DetailsItem
        {
            public string CustomerNo { get; set; }
            public string MeterDescription { get; set; }
            public string SerialNo { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string OnlineStatus { get; set; }
            public DateTime? LastCommunicated { get; set; }
            public List<AF_AfroxAdministration_Metering_Details_CustomerItem> AF_AfroxAdministration_Metering_Details_CustomerItems { get; set; }

            public class AF_AfroxAdministration_Metering_Details_CustomerItem : Data.CustomersDetail
            {
                public Log_BillingControlReport_OccupancyVerification Log_BillingControlReport_OccupancyVerification { get; set; }
                public decimal? ConvRate { get; set; }
                public string MeterMake { get; set; }
                public string SystemCheckUsername { get; set; }
                public string CustomerCheckUsername { get; set; }
                public string VerificationUsername { get; set; }
                public string MeterSerialNoCheckUsername { get; set; }

                public int TotalCount
                {
                    get
                    {
                        if (CustomersDetails_AttachmentItems != null)
                            return CustomersDetails_AttachmentItems.Count;

                        return 0;
                    }
                }

                public int ReviewedCount
                {
                    get
                    {
                        if (CustomersDetails_AttachmentItems != null)
                            return CustomersDetails_AttachmentItems.Where(p => p.ReviewedDate.HasValue).Count();

                        return 0;
                    }
                }

                public List<CustomersDetails_AttachmentItem> CustomersDetails_AttachmentItems { get; set; }

                public class CustomersDetails_AttachmentItem : Data.CustomersDetails_Attachment
                {
                    public string Username { get; set; }
                }
            }

            public int TotalCount
            {
                get
                {
                    if (AF_AfroxAdministration_Metering_Details_CustomerItems != null)
                        return AF_AfroxAdministration_Metering_Details_CustomerItems.Select(p => p.CustomersDetails_AttachmentItems.Count).Sum();

                    return 0;
                }
            }

            public int ReviewedCount
            {
                get
                {
                    if (AF_AfroxAdministration_Metering_Details_CustomerItems != null)
                        return AF_AfroxAdministration_Metering_Details_CustomerItems.Select(p => p.CustomersDetails_AttachmentItems.Where(p => p.ReviewedDate.HasValue).Count()).Sum();

                    return 0;
                }
            }
        }
    }
    public class AF_AfroxAdministration_Metering_Details_AddModel
    {
        [Required]
        [DisplayName("Customer Trading Name")]
        public string CustomerTradingName { get; set; }

        [Required]
        [DisplayName("Customer Acc No")]
        public string CustomerAccNo { get; set; }

        [Required]
        [DisplayName("Meter Serial No")]
        public string MeterSerialNo { get; set; }

        [Required]
        [DisplayName("From Date")]
        public DateTime? FromDate { get; set; }

        [DisplayName("To Date")]
        public DateTime? ToDate { get; set; }

        [DisplayName("Include In Export")]
        public bool IncludeInExport { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AF_AfroxAdministration_Metering_Details_EditModel
    {
        [Required]
        [DisplayName("Customer Trading Name")]
        public string CustomerTradingName { get; set; }

        [Required]
        [DisplayName("Customer Acc No")]
        public string CustomerAccNo { get; set; }

        [Required]
        [DisplayName("Meter Serial No")]
        public string MeterSerialNo { get; set; }

        [Required]
        [DisplayName("From Date")]
        public DateTime? FromDate { get; set; }

        [DisplayName("To Date")]
        public DateTime? ToDate { get; set; }

        [DisplayName("Include In Export")]
        public bool IncludeInExport { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AF_AfroxAdministration_Metering_Details_VerificationUploadModel
    {
        [DisplayName("Customer Trading Name")]
        public string CustomerTradingName { get; set; }

        [DisplayName("Customer Acc No")]
        public string CustomerAccNo { get; set; }

        [DisplayName("Meter Serial No")]
        public string MeterSerialNo { get; set; }

        [DisplayName("From Date")]
        public DateTime? FromDate { get; set; }

        [DisplayName("To Date")]
        public DateTime? ToDate { get; set; }

        [Required]
        [DisplayName("Verification Photo")]
        public IFormFile VerificationPhoto { get; set; }

        [DisplayName("Comment")]
        public string Comment { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class AF_AfroxAdministration_Metering_ReviewModel
    {
        public List<CustomersDetailItem> CustomersDetails { get; set; }
        public class CustomersDetailItem : CustomersDetail
        {
            public Log_BillingControlReport_OccupancyVerification Log_BillingControlReport_OccupancyVerification { get; set; }
            public decimal? ConvRate { get; set; }
            public string MeterMake { get; set; }
            public string SystemCheckUsername { get; set; }
            public string CustomerCheckUsername { get; set; }
            public string VerificationUsername { get; set; }
            public string MeterSerialNoCheckUsername { get; set; }

            public decimal? BuildingLat { get; set; }
            public decimal? BuildingLong { get; set; }
            public decimal? SystemLat { get; set; }
            public decimal? SystemLong { get; set; }
            public string MeteringLatLongUsername { get; set; }

            public List<CustomersDetails_AttachmentItem> CustomersDetails_Attachments { get; set; }
            public class CustomersDetails_AttachmentItem : CustomersDetails_Attachment
            {
                public string Username { get; set; }
                public string ReviewedByUsername { get; set; }
                public List<CustomersDetails_Attachments_CommentItem> CustomersDetails_Attachments_Comments { get; set; }
                public class CustomersDetails_Attachments_CommentItem : CustomersDetails_Attachments_Comment
                {
                    public string Username { get; set; }
                }
            }

            public int TotalCount
            {
                get
                {
                    if (CustomersDetails_Attachments != null)
                        return CustomersDetails_Attachments.Count;

                    return 0;
                }
            }

            public int ReviewedCount
            {
                get
                {
                    if (CustomersDetails_Attachments != null)
                        return CustomersDetails_Attachments.Where(p => p.ReviewedDate.HasValue).Count();

                    return 0;
                }
            }
        }
        //public int A02_MirrorMeterAuditing_MirrorReadingUpdateID { get; set; }
        //public decimal LatestReading { get; set; }
        //public DateTime LatestReadingDateTime { get; set; }
        //public decimal LatestReadingOdo { get; set; }
        //public DateTime LatestReadingDateTimeOdo { get; set; }
        //public DateTime LatestReadingOdoCreatedDateTime { get; set; }
        //public string LatestReadingOdoCreatedBy { get; set; }
        //public string LatestReadingOdoPhotoURL { get; set; }
        //public decimal VerificationReading { get; set; }
        //public DateTime VerificationReadingDateTime { get; set; }
        //public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes Status { get; set; }
        //public string ChangedByUserName { get; set; }
        //public DateTime? ChangedDate { get; set; }
        //public decimal CalculatedDifferenceReading { get { return LatestReadingOdo - VerificationReading; } }
        //public List<ReadingHistoryItem> ReadingHistoryItems { get; set; }
        //public class ReadingHistoryItem
        //{
        //    public string DisplayName { get; set; }
        //    public decimal OpeningReadingOriginal { get; set; }
        //    public decimal OpeningReading { get; set; }
        //    public decimal ClosingReadingOriginal { get; set; }
        //    public decimal ClosingReading { get; set; }
        //    public decimal UsageInM3 { get { return (ClosingReadingOriginal / 1000.0m) - (OpeningReadingOriginal / 1000.0m); } }
        //    public decimal ConvFactor { get; set; }
        //    public decimal KgInvoiced { get { return (ClosingReading / 1000.0m) - (OpeningReading / 1000.0m); } }
        //}
        //public string CustomerTradingName { get; set; }
        //public string SAPCustomerAccNo { get; set; }


        //public List<AF_AfroxAdministration_Metering_ReviewAction> AF_AfroxAdministration_Metering_ReviewActions
        //{
        //    //1 = Sorted, I fixed the problem
        //    //2 = Help, there is no consumption recorded from the meter
        //    //3 = Help, the consumption recorded is not billed on Skybill
        //    //4 = Nope, the system made a calculation error, everything is working as it should
        //    get
        //    {
        //        List<AF_AfroxAdministration_Metering_ReviewAction> actions = new List<AF_AfroxAdministration_Metering_ReviewAction>();

        //        if (Status == Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified)
        //            actions.Add(new AF_AfroxAdministration_Metering_ReviewAction() { ActionID = 1, ActionDisplayName = "Verify", ActionDescription = "Verify reading is correct and can be added.", ActionIcon = "check-circle", ActionColor = "text-green" });

        //        if (Status != Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected)
        //            actions.Add(new AF_AfroxAdministration_Metering_ReviewAction() { ActionID = 2, ActionDisplayName = "Reject", ActionDescription = "Reject the reading", ActionIcon = "exclamation-circle", ActionColor = "text-red" });


        //        return actions;
        //    }
        //}

        //public class AF_AfroxAdministration_Metering_ReviewAction
        //{
        //    public int ActionID { get; set; }
        //    public string ActionDisplayName { get; set; }
        //    public string ActionDescription { get; set; }
        //    public string ActionIcon { get; set; }
        //    public string ActionColor { get; set; }
        //}
    }

    public class AF_AfroxAdministration_WinshuttleExportModel
    {
        [Display(Name = "From Date (@ 00:00)")]
        [Required]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date (@ 23:59)")]
        [Required]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Partner")]
        [Required]
        public List<SelectListItem> PartnerID { get; set; }

        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        //[Display(Name = "Customer Purchase Order Number VBKD BSTKD")]
        //[Required]
        //public string Customer_Purchase_Order_Number_VBKD_BSTKD { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }

        public List<CompanyItem> CompanyItems { get; set; }

        public List<AF_AfroxAdministration_WinshuttleExportItem> AF_AfroxAdministration_WinshuttleExportItems { get; set; }

        public class AF_AfroxAdministration_WinshuttleExportItem
        {
            public string CentreName { get; set; }
            public string CustomerRegisteredName { get; set; }
            public string CustomerTradingName { get; set; }
            public string AccountNumber { get; set; }
            public DateTime DateRead { get; set; }
            public decimal OpeningReading { get; set; }
            public decimal ClosingReading { get; set; }
            public decimal DiffM3 { get { return ClosingReading - OpeningReading; } }
            public decimal ConvFact { get; set; }
            public decimal KgToInvoice { get { return DiffM3 * ConvFact; } }
            public string InvoiceNumber { get; set; }
            public string Batch { get; set; }
            public string PlantNo { get; set; }
            public string StockRefNo { get; set; }
            public string Notes { get; set; }
            public string UnbilledReason { get; set; }
        }
    }

    public class AF_AfroxAdministration_WinshuttleExportRequestResultModel
    {
        public bool AlreadyExist { get; set; }
        public Data.J_Finance_WinshuttleExport AF_AfroxAdministration_WinshuttleExport { get; set; }
    }

    public class AF_AfroxAdministration_WinshuttleExportRequestsModel
    {
        public List<AF_AfroxAdministration_WinshuttleExportRequestItem> AF_AfroxAdministration_WinshuttleExportRequestItems { get; set; }
        public class AF_AfroxAdministration_WinshuttleExportRequestItem : Data.J_Finance_WinshuttleExport
        {
            public string Username { get; set; }
            public string PartnerName { get; set; }
            public string CompanyName { get; set; }
            public int ItemCount { get; set; }
        }
    }

    public class AF_AfroxAdministration_AddDeviceModel
    {
        [Display(Name = "Gateway ID")]
        [Required]
        public List<SelectListItem> GatewayID { get; set; }


        public string ErrorMessage { get; set; }
    }

    public class AF_AfroxAdministration_AddDeviceModel_Step3Model
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

        [Required(ErrorMessage = "Date Logged Reading is required")]
        [Display(Name = "Date Logged")]
        public string DateLogged { get; set; }

        [Required(ErrorMessage = "Time Logged Reading is required")]
        [Display(Name = "Time Logged")]
        public string TimeLogged { get; set; }

        public bool ShowOdo { get; set; }


        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Picture")]
        public IFormFile file { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }


    public class AF_AfroxAdministration_AddDeviceACOModel
    {
        [Display(Name = "Gateway ID")]
        [Required]
        public List<SelectListItem> GatewayID { get; set; }


        public string ErrorMessage { get; set; }
    }

    public class AF_AfroxAdministration_AddDeviceACOModel_Step3Model
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

        //[Display(Name = "Name")]
        //[Required]
        //public string Name { get; set; }

        [Display(Name = "Port")]
        public string Port { get; set; }
        public bool ShowPort { get; set; }


        [Display(Name = "Protocol")]
        public string Protocol { get; set; }
        public bool ShowProtocol { get; set; }


        [Display(Name = "Remote Address (Left + Right)")]
        public string RemoteAddress { get; set; }
        public bool ShowRemoteAddress { get; set; }


        [Display(Name = "Remote Address (Change Over)")]
        public string RemoteAddressChangeOver { get; set; }
        public bool ShowRemoteAddressChangeOver { get; set; }


        [Display(Name = "Remote Index")]
        public string RemoteIndex { get; set; }
        public bool ShowRemoteIndex { get; set; }


        [Display(Name = "Process Interval")]
        public string ProcessInterval { get; set; }
        public bool ShowProcessInterval { get; set; }

        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class AF_AfroxAdministration_NewlyAddedDevicesModel
    {
        public bool ShowAll { get; set; }
        public List<DeviceItem> Devices { get; set; }

        public class DeviceItem
        {
            public int DeviceID { get; set; }
            public string SerialNumber { get; set; }
            public string MeterDescription { get; set; }
            public string MeterType { get; set; }
            public string Status { get; set; }
            public string LastCommunicated { get; set; }
            public string Signal { get; set; }
            public string Battery { get; set; }
            public int? GatewayID { get; set; }
            public string FormXML { get; set; }
            public bool ExistsOnM2M { get; set; }
        }
    }

    public class AF_AfroxAdministration_DeviceReviewModel
    {
        public List<AF_AfroxAdministration_DeviceReviewItem> AF_AfroxAdministration_DeviceReviewItems { get; set; }

        public class AF_AfroxAdministration_DeviceReviewItem : MyVoltage.Api.MyVoltage.GatewayDevice
        {
            public int DevicesLinked { get; set; }
            public string GISLocation { get; set; }
            public string OfflineDuration { get; set; }
        }
    }

    public class AF_AfroxAdministration_DeviceReview_DeviceModel
    {
        public string GatewayName { get; set; }
        public int GatewayID { get; set; }

        public List<AF_AfroxAdministration_DeviceReview_DeviceItem> AF_AfroxAdministration_DeviceReview_DeviceItems { get; set; }

        public class AF_AfroxAdministration_DeviceReview_DeviceItem : MyVoltage.Api.MyVoltage.Device
        {
            public string TableRowID { get; set; }
            public int? GatewayID { get; set; }
            public string OfflineDuration { get; set; }

            // Registers
            public string MaxDemand { get; set; }
            public string ActiveEnergy { get; set; }
            public string ReactiveEnergy { get; set; }
            public string CTRatio { get; set; }
            public string ContactorState { get; set; }
            public string Temp { get; set; }
            public string RemainingCredit { get; set; }
            public string WaterConsumption { get; set; }
            public string GasConsumption { get; set; }
            public string SNR { get; set; }
            public string SignalRSSI { get; set; }
            public string InternalBatteryV { get; set; }

            public bool IsContactorInstalled { get; set; }
        }
    }

    public class AF_AfroxAdministration_OverallStatsModel
    {
        public List<string> Manufacturers { get; set; }
        public List<AF_AfroxAdministration_OverallStatsItem> AF_AfroxAdministration_OverallStatsItems { get; set; }

        public class AF_AfroxAdministration_OverallStatsItem
        {
            public string PartnerName { get; set; }
            public int PartnerID { get; set; }
            public int CompanyCount { get; set; }
            public int CustomerCount { get; set; }

            public int InformationCompleteCompanyCount { get; set; }
            public int InformationCompleteCustomerCount { get; set; }

            public int SystemSetupSignOffCompanyCount { get; set; }
            public int SystemSetupSignOffCustomerCount { get; set; }

            public int CustomerSetupSignOffCompanyCount { get; set; }
            public int CustomerSetupSignOffCustomerCount { get; set; }

            public int VerificationCompanyCount { get; set; }
            public int VerificationCustomerCount { get; set; }

            public int MeterSerialNoCheckCompanyCount { get; set; }
            public int MeterSerialNoCheckCustomerCount { get; set; }

            public int OccupiedCompanyCount { get; set; }
            public int OccupiedCustomerCount { get; set; }

            public int VacantCompanyCount { get; set; }
            public int VacantCustomerCount { get; set; }

            public int ServiceMeterCompanyCount { get; set; }
            public int ServiceMeterCustomerCount { get; set; }

            public int LowUsageCompanyCount { get; set; }
            public int LowUsageCustomerCount { get; set; }

            public int ManualReadingsCompanyCount { get; set; }
            public int ManualReadingsCustomerCount { get; set; }

            public int ReplaceMetersCompanyCount { get; set; }
            public int ReplaceMetersCustomerCount { get; set; }

            public int NoInformationCompanyCount { get; set; }
            public int NoInformationCustomerCount { get; set; }

            public int MetrixMetersCompanyCount { get; set; }
            public int MetrixMetersCustomerCount { get; set; }

            public int ReplaceMetersCompanyCount2 { get; set; }
            public int ReplaceMetersCustomerCount2 { get; set; }

            public int OutstandingMetersCompanyCount { get; set; }
            public int OutstandingMetersCustomerCount { get; set; }
            public Dictionary<string, int> MeterManufacturers { get; set; }
        }

        public Dictionary<string, int> MeterManufacturers { get; set; }
        public int CompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.CompanyCount).Sum() : 0; } }
        public int CustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.CustomerCount).Sum() : 0; } }

        public int InformationCompleteCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.InformationCompleteCompanyCount).Sum() : 0; } }
        public int InformationCompleteCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.InformationCompleteCustomerCount).Sum() : 0; } }

        public int SystemSetupSignOffCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.SystemSetupSignOffCompanyCount).Sum() : 0; } }
        public int SystemSetupSignOffCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.SystemSetupSignOffCustomerCount).Sum() : 0; } }

        public int CustomerSetupSignOffCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.CustomerSetupSignOffCompanyCount).Sum() : 0; } }
        public int CustomerSetupSignOffCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.CustomerSetupSignOffCustomerCount).Sum() : 0; } }

        public int VerificationCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.VerificationCompanyCount).Sum() : 0; } }
        public int VerificationCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.VerificationCustomerCount).Sum() : 0; } }

        public int MeterSerialNoCheckCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.MeterSerialNoCheckCompanyCount).Sum() : 0; } }
        public int MeterSerialNoCheckCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.MeterSerialNoCheckCustomerCount).Sum() : 0; } }

        public int OccupiedCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.OccupiedCompanyCount).Sum() : 0; } }
        public int OccupiedCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.OccupiedCustomerCount).Sum() : 0; } }

        public int VacantCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.VacantCompanyCount).Sum() : 0; } }
        public int VacantCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.VacantCustomerCount).Sum() : 0; } }

        public int ServiceMeterCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ServiceMeterCompanyCount).Sum() : 0; } }
        public int ServiceMeterCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ServiceMeterCustomerCount).Sum() : 0; } }

        public int LowUsageCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.LowUsageCompanyCount).Sum() : 0; } }
        public int LowUsageCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.LowUsageCustomerCount).Sum() : 0; } }

        public int ManualReadingsCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ManualReadingsCompanyCount).Sum() : 0; } }
        public int ManualReadingsCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ManualReadingsCustomerCount).Sum() : 0; } }

        public int ReplaceMetersCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ReplaceMetersCompanyCount).Sum() : 0; } }
        public int ReplaceMetersCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ReplaceMetersCustomerCount).Sum() : 0; } }

        public int NoInformationCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.NoInformationCompanyCount).Sum() : 0; } }
        public int NoInformationCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.NoInformationCustomerCount).Sum() : 0; } }

        public int MetrixMetersCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.MetrixMetersCompanyCount).Sum() : 0; } }
        public int MetrixMetersCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.MetrixMetersCustomerCount).Sum() : 0; } }

        public int ReplaceMetersCompanyCount2 { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ReplaceMetersCompanyCount2).Sum() : 0; } }
        public int ReplaceMetersCustomerCount2 { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.ReplaceMetersCustomerCount2).Sum() : 0; } }

        public int OutstandingMetersCompanyCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.OutstandingMetersCompanyCount).Sum() : 0; } }
        public int OutstandingMetersCustomerCount { get { return AF_AfroxAdministration_OverallStatsItems != null && AF_AfroxAdministration_OverallStatsItems.Count > 0 ? AF_AfroxAdministration_OverallStatsItems.Select(p => p.OutstandingMetersCustomerCount).Sum() : 0; } }
    }

    public class AF_AfroxAdministration_MeterReadingSummaryModel
    {
        public int CompletedToday
        {
            get
            {
                if (AF_AfroxAdministration_MeterReadingSummaryItems != null)
                {
                    return (from p in AF_AfroxAdministration_MeterReadingSummaryItems
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
                if (AF_AfroxAdministration_MeterReadingSummaryItems != null)
                {
                    return (from p in AF_AfroxAdministration_MeterReadingSummaryItems
                            where p.LatestAuditReadingTime.Date != DateTime.Now.Date
                            select p).Count();
                }

                return 0;
            }
        }
        public int Total { get { return LeftToday + CompletedToday; } }

        public List<AF_AfroxAdministration_MeterReadingSummaryItem> AF_AfroxAdministration_MeterReadingSummaryItems { get; set; }

        public class AF_AfroxAdministration_MeterReadingSummaryItem
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
            public Data.CustomersDetail CustomersDetail { get; set; }
            public string OccupancyStatus { get; set; }
            public MyVoltageApi.Data.DeviceReading OneMonthAgo_DeviceReading { get; set; }
            public MyVoltageApi.Data.DeviceReading TwoMonthAgo_DeviceReading { get; set; }
        }

    }

    public class AF_AfroxAdministration_MeterReadingDetailsModel
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


        public Data.Device Device { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
        public MyVoltageApi.Data.Device MirrorDevice { get; set; }
        public Data.Company Company { get; set; }
        public AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem AF_AfroxAdministration_Metering_DetailsItem { get; set; }

    }

    public class AF_AfroxAdministration_MeterReadingVerificationModel
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
        public decimal CalculatedDifferenceReading { get { return LatestReadingOdo - VerificationReading; } }
        public List<ReadingHistoryItem> ReadingHistoryItems { get; set; }
        public class ReadingHistoryItem
        {
            public string DisplayName { get; set; }
            public decimal OpeningReadingOriginal { get; set; }
            public decimal OpeningReading { get; set; }
            public decimal ClosingReadingOriginal { get; set; }
            public decimal ClosingReading { get; set; }
            public decimal UsageInM3 { get { return (ClosingReadingOriginal / 1000.0m) - (OpeningReadingOriginal / 1000.0m); } }
            public decimal ConvFactor { get; set; }
            public decimal KgInvoiced { get { return (ClosingReading / 1000.0m) - (OpeningReading / 1000.0m); } }
        }
        public string CustomerTradingName { get; set; }
        public string SAPCustomerAccNo { get; set; }


        public List<AF_AfroxAdministration_MeterReadingVerificationAction> AF_AfroxAdministration_MeterReadingVerificationActions
        {
            //1 = Sorted, I fixed the problem
            //2 = Help, there is no consumption recorded from the meter
            //3 = Help, the consumption recorded is not billed on Skybill
            //4 = Nope, the system made a calculation error, everything is working as it should
            get
            {
                List<AF_AfroxAdministration_MeterReadingVerificationAction> actions = new List<AF_AfroxAdministration_MeterReadingVerificationAction>();

                if (Status == Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified)
                    actions.Add(new AF_AfroxAdministration_MeterReadingVerificationAction() { ActionID = 1, ActionDisplayName = "Verify", ActionDescription = "Verify reading is correct and can be added.", ActionIcon = "check-circle", ActionColor = "text-green" });

                if (Status != Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected)
                    actions.Add(new AF_AfroxAdministration_MeterReadingVerificationAction() { ActionID = 2, ActionDisplayName = "Reject", ActionDescription = "Reject the reading", ActionIcon = "exclamation-circle", ActionColor = "text-red" });


                return actions;
            }
        }

        public class AF_AfroxAdministration_MeterReadingVerificationAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class AF_AfroxAdministration_MeterReadingUpdateModel
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
        public string CustomerTradingName { get; set; }
        public string SAPCustomerAccNo { get; set; }
    }

    public class AF_AfroxAdministration_MeterReadingUpdateNoAccessModel
    {
        [Required(ErrorMessage = "Denied access photo required.")]
        [Display(Name = "Photo")]
        public IFormFile Photo { get; set; }

        public bool IsSuccessful { get; set; }

        public string RemoteAddress { get; set; }
    }

    public class AF_AfroxAdministration_GasNetworkBalancing_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<AF_AfroxAdministration_GasNetworkBalancing_DailyItem> AF_AfroxAdministration_GasNetworkBalancing_DailyItems { get; set; }
        public List<AF_AfroxAdministration_GasNetworkBalancing_DailyItem> AF_AfroxAdministration_GasNetworkBalancing_DailyItems_SUP { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal SupplyPerCycle { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                //if (Convert.ToInt32(value) == 0)
                //    return "font-weight-bold text-right table-danger";
                //else
                return "font-weight-bold text-right";
            }
            else
            {
                //if (Convert.ToInt32(value) == 0)
                //    return "text-right table-danger";
                //else
                return "text-right";
            }
        }

        public class AF_AfroxAdministration_GasNetworkBalancing_DailyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            // Month, Consumption
            public List<KeyValuePair<DateTime, decimal>> MonthlyBillingFigures { get; set; }

            public decimal Total
            {
                get
                {
                    if (MonthlyBillingFigures != null)
                        return MonthlyBillingFigures.Select(p => p.Value).Sum();

                    return 0;
                }
            }
        }

    }

    public class AF_AfroxAdministration_Supply_DetailsModel
    {
        public AF_EDI_CompanyDetail AF_EDI_CompanyDetail { get; set; }

        [Required(ErrorMessage = "Customer Name is required")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Address 1 is required")]
        [Display(Name = "Address 1")]
        public string Address1 { get; set; }

        [Required(ErrorMessage = "Address 2 is required")]
        [Display(Name = "Address 2")]
        public string Address2 { get; set; }

        [Required(ErrorMessage = "Address 3 is required")]
        [Display(Name = "Address 3")]
        public string Address3 { get; set; }

        [Required(ErrorMessage = "Postal Code is required")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Account Code is required")]
        [Display(Name = "Account Code")]
        public string AccountCode { get; set; }

        public bool IsSuccessful { get; set; }
    }

    public class AF_AfroxAdministration_ACOStatusesModel
    {
        public List<AF_AfroxAdministration_ACOStatusesItem> AF_AfroxAdministration_ACOStatusesItems { get; set; }
        public class AF_AfroxAdministration_ACOStatusesItem : MyGasManager.Data.ACO_Status
        {
        }
    }

    public class AF_AfroxAdministration_SupplyTransactionModel
    {
        [Display(Name = "ACO Transaction No")]
        [Required]
        public List<SelectListItem> ACOTransactionNo { get; set; }

        [Display(Name = "Status")]
        [Required]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Supply Status")]
        [Required]
        public List<SelectListItem> SupplyStatus { get; set; }

        [Display(Name = "Activation Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ActivationDate { get; set; }

        [Display(Name = "Depletion Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? DepletionDate { get; set; }

        public AF_AfroxAdministration_SupplyTransactionItem AF_AfroxAdministration_Item { get; set; }
        public class AF_AfroxAdministration_SupplyTransactionItem : MyGasManager.Data.AutoSupplyInsights
        {
            public List<AutoSupplyInsights_LogsItem> AutoSupplyInsights_LogsItems { get; set; }
            public class AutoSupplyInsights_LogsItem : MyGasManager.Data.AutoSupplyInsights_Log
            {
                public string Username { get; set; }
            }
        }
    }

    public class AF_AfroxAdministration_SnapshotEmailsModel
    {
        public List<AF_AfroxAdministration_SnapshotEmailsItem> AF_AfroxAdministration_SnapshotEmailsItems { get; set; }
        public class AF_AfroxAdministration_SnapshotEmailsItem : AF_SnapshotEmail
        {
        }
    }

    public class AF_AfroxAdministration_ACO_StatusHackModel
    {
        public List<AF_AfroxAdministration_ACO_StatusHackItem> AF_AfroxAdministration_ACO_StatusHackItems { get; set; }
        public class AF_AfroxAdministration_ACO_StatusHackItem : ACO_StatusHack
        {
        }
    }
}
