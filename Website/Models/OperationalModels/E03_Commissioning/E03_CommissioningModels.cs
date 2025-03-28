using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.E03_CommissioningModels
{
    public class E03_CommissioningTasks_Company_SummaryModel
    {
        public List<E03_CommissioningTasks_Company_SummaryItem> E03_CommissioningTasks_CompanySummaryItems { get; set; }
        public class E03_CommissioningTasks_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int MeterCount { get; set; }
            public int CalibratedCount { get; set; }
            public int UnCalibratedCount { get; set; }
            public int OnlineCount { get; set; }
            public int OfflineCount { get; set; }
        }

    }

    public class E03_CommissioningTasks_Company_DetailsModel
    {
        public bool CustomerRegistered { get; set; }
        public bool CustomerNotRegistered { get; set; }
        public bool CustomerEmail { get; set; }
        public bool CustomerNoEmail { get; set; }
        public bool CustomerPhone { get; set; }
        public bool CustomerNoPhone { get; set; }

        public bool OccupancyOccupied { get; set; }
        public bool OccupancyOther { get; set; }
        public bool OccupancyUserApproved { get; set; }
        public bool OccupancySystemApproved { get; set; }
        public bool OccupancyValid { get; set; }
        public bool OccupancyInValid { get; set; }

        //public bool MeterIsOnline { get; set; }
        //public bool MeterIsOffline { get; set; }
        public bool MeterIsCalibrated { get; set; }
        public bool MeterIsNotCalibrated { get; set; }
        public List<SelectListItem> OnlineType { get; set; }
        public List<SelectListItem> DisconnectType { get; set; }

        public class E03_CommissioningTasks_Company_DetailsSubItem
        {
            public string M2MDeviceID { get; set; }
            public string Serial { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public bool? IsOnline { get; set; }
            public Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes CalibrationStatus { get; set; }
            public bool IsBlocked { get; set; }
            public string OfflineDuration { get; set; }
            public TimeSpan OfflineDurationTS { get; set; }
            public bool? IsContactorConnected { get; set; }
            public DateTime? LatestOdoTimeLogged { get; set; }
            public bool IsOnAuto { get; set; }
        }

        public List<E03_CommissioningTasks_Company_DetailsItem> E03_CommissioningTasks_CompanyDetailsItems { get; set; }

        public class E03_CommissioningTasks_Company_DetailsItem
        {
            public string ServiceAddress { get; set; }
            public string CustomerNo { get; set; }
            public GetAllCustomersRootObject.Value SkybillCustomer { get; set; }
            public Data.Customer Customer { get; set; }
            public Log_BillingControlReport_OccupancyVerification Log_BillingControlReport_OccupancyVerification { get; set; }
            public List<E03_CommissioningTasks_Company_DetailsSubItem> E03_CommissioningTasks_Company_DetailsSubItems { get; set; }
            public Dictionary<DateTime, decimal> LatestBillings { get; set; }
        }
    }
}
