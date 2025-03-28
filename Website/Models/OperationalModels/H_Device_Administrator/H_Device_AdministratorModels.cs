using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.H_Device_AdministratorModels
{
    public class H_Device_Administrator_DeviceOverviewModel
    {
        public List<H_Device_Administrator_DeviceOverviewItem> H_Device_Administrator_DeviceOverviewItems { get; set; }

        public class H_Device_Administrator_DeviceOverviewItem : MyVoltage.Api.MyVoltage.GatewayDevice
        {
            public int DevicesLinked { get; set; }
            public string GISLocation { get; set; }
            public string OfflineDuration { get; set; }
        public int? DeviceAPIID { get; set; }
        }
    }

    public class H_Device_Administrator_DeviceOverview_DeviceModel
    {
        public string GatewayName { get; set; }
        public int GatewayID { get; set; }

        public List<H_Device_Administrator_DeviceOverview_DeviceItem> H_Device_Administrator_DeviceOverview_DeviceItems { get; set; }

        public class H_Device_Administrator_DeviceOverview_DeviceItem : MyVoltage.Api.MyVoltage.Device
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
    public class H_Device_Administrator_DeviceNameBulkUpdateModel
    {
        public string DeviceIDs { get; set; }
        public string NewDeviceName { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class H_Device_Administrator_Bulk433TestingUploadModel
    {
        public string SerialNos { get; set; }
        public List<SelectListItem> GatewayID { get; set; }
        public string ErrorMessage { get; set; }
        public List<SelectListItem> DoSecondInput { get; set; }
    }

    public class H_Device_Administrator_AutoDeviceDiscoveryModel
    {
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class H_Device_Administrator_WirelessSignalOptimizerModel
    {
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public string ErrorMessage { get; set; }
        public int LoopCount { get; set; }
        public int SleepDurationMin { get; set; }
        public decimal DontMoveAboveThisStrength { get; set; }
    }

    public class H_Device_Administrator_ActiveEnergyAnomalies_SummaryModel
    {
        [Display(Name = "From Date")]
        [Required]
        public DateTime FromDate { get; set; }

        [Display(Name = "To Date")]
        [Required]
        public DateTime ToDate { get; set; }

        public List<H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem> H_Device_Administrator_ActiveEnergyAnomalies_SummaryItems { get; set; }

        public class H_Device_Administrator_ActiveEnergyAnomalies_SummaryItem : Data.Company
        {
            public List<H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem> H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItems { get; set; }
            public class H_Device_Administrator_ActiveEnergyAnomalies_SummaryMonthlyItem
            {
                public DateTime Month { get; set; }
                public bool? IsCompleted { get; set; }
                public DateTime? DateCompleted { get; set; }
                public int ErrorCount { get; set; }
            }
        }
    }

    public class H_Device_Administrator_ActiveEnergyAnomalies_DetailsModel
    {
        [Display(Name = "From Date (yyyy/MM/dd 00:00:00)")]
        [Required]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date (yyyy/MM/dd 00:00:00)")]
        [Required]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Show Only Errors")]
        [Required]
        public List<SelectListItem> ShowOnlyErrors { get; set; }

        public List<H_Device_Administrator_ActiveEnergyAnomalies_DetailsItem> H_Device_Administrator_ActiveEnergyAnomalies_DetailsItems { get; set; }

        public class H_Device_Administrator_ActiveEnergyAnomalies_DetailsItem : Data.MeterActiveEnergyAnomaly
        {

        }
    }

    public class H_Device_Administrator_ActiveEnergyAnomalies_RequestModel
    {
        [Display(Name = "Month")]
        [Required]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Company")]
        [Required]
        public List<SelectListItem> CompanyID { get; set; }

        public bool IsSuccessful { get; set; }

        public List<H_Device_Administrator_ActiveEnergyAnomalies_RequestItem> H_Device_Administrator_ActiveEnergyAnomalies_RequestItems { get; set; }

        public class H_Device_Administrator_ActiveEnergyAnomalies_RequestItem : Data.MeterActiveEnergyAnomaliesRun
        {
            public string CreatedByUsername { get; set; }
            public string CompanyName { get; set; }
        }
    }
}
