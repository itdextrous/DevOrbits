using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A01_GatewayAndDeviceMonitoring
{
    public class A01_GatewayAndDeviceMonitoring_GatewaySummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (LessThan3DaysCount > 0
                    || LessThan7DaysCount > 0
                    || MoreThan7DaysCount > 0)
                    return StatusType.Problematic;
                else if (LessThan24HoursCount > 0)
                    return StatusType.AttentionRequired;
                else
                    return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int OfflineCount { get; set; }
        public int OnlineCount { get; set; }
        public int TotalCount { get { return OfflineCount + OnlineCount; } }
        public int DevicesImpactedCount { get; set; }
        public int LessThan4HoursCount { get; set; }
        public int LessThan24HoursCount { get; set; }
        public int LessThan3DaysCount { get; set; }
        public int LessThan7DaysCount { get; set; }
        public int MoreThan7DaysCount { get; set; }

        public enum StatusType
        {
            Ok = 1,
            Problematic = 2,
            AttentionRequired = 3,
        }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Problematic:
                    return "Problematic";
                case StatusType.Ok:
                    return "Ok";
                case StatusType.AttentionRequired:
                    return "Attention Required";
            }
            return "";
        }
    }

    public class A01_GatewayAndDeviceMonitoring_GatewayDetailsModel
    {
        public List<SelectListItem> OnlineType { get; set; }
        public List<A01_GatewayAndDeviceMonitoring_GatewayDetailsItem> A01_GatewayAndDeviceMonitoring_GatewayDetailsItems { get; set; }

        public F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest Latest_Request { get; set; }
        public class F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest : Data.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest
        {
            public string CreatedByUsername { get; set; }
        }

        public class A01_GatewayAndDeviceMonitoring_GatewayDetailsItem : MyVoltage.Api.MyVoltage.GatewayDevice
        {
            public int DevicesLinked { get; set; }
            public string GISLocation { get; set; }
            public string OfflineDuration { get; set; }
            public TimeSpan OfflineDurationTS { get; set; }
            public string SimCardNumber { get; set; }
            public string SimCardNumberOverride { get; set; }
            public List<FlagItem> A09_Flags { get; set; }
            public class FlagItem
            {
                public int FlagID { get; set; }
                public int FlagTypeID { get; set; }
                public string FlagTypeName { get; set; }
            }
        }
    }

    public class A01_GatewayAndDeviceMonitoring_GatewayResultsModel
    {
        public List<A01_GatewayAndDeviceMonitoring_GatewayResultsItem> A01_GatewayAndDeviceMonitoring_GatewayResultsItems { get; set; }

        public class A01_GatewayAndDeviceMonitoring_GatewayResultsItem : MyVoltage.Api.MyVoltage.GatewayDevice
        {
            public int DevicesLinked { get; set; }
            public string GISLocation { get; set; }
            public string OfflineDuration { get; set; }
            public string SimCardNumber { get; set; }
        }
    }

    public class A01_GatewayAndDeviceMonitoring_DeviceSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (LessThan7DaysCount > 0
                    || MoreThan7DaysCount > 0)
                    return StatusType.Problematic;
                else if (LessThan3DaysCount > 0)
                    return StatusType.AttentionRequired;
                else
                    return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int OfflineCount { get; set; }
        public int OnlineCount { get; set; }
        public int TotalCount { get { return OfflineCount + OnlineCount; } }
        public int LessThan4HoursCount { get; set; }
        public int LessThan24HoursCount { get; set; }
        public int LessThan3DaysCount { get; set; }
        public int LessThan7DaysCount { get; set; }
        public int MoreThan7DaysCount { get; set; }

        public enum StatusType
        {
            [Description("Ok")]
            Ok = 1,
            [Description("Problematic")]
            Problematic = 2,
            [Description("Attention Required")]
            AttentionRequired = 3,
        }

        public static string GetStatusString(StatusType statusType)
        {
            return statusType.GetDescription();
        }
    }

    public class A01_GatewayAndDeviceMonitoring_DeviceDetailsModel
    {
        public List<SelectListItem> DeviceType { get; set; }
        public List<SelectListItem> OnlineType { get; set; }

        public F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest Latest_Request { get; set; }
        public class F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest : Data.F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest
        {
            public string CreatedByUsername { get; set; }
        }

        public int EntriesPerPage { get; set; }
        public int TotalEntries { get; set; }
        public PaginatedList<A01_GatewayAndDeviceMonitoring_DeviceDetailsItem> A01_GatewayAndDeviceMonitoring_DeviceDetailsItems { get; set; }
        public class A01_GatewayAndDeviceMonitoring_DeviceDetailsItem : MyVoltage.Api.MyVoltage.Device
        {
            public int? GatewayID { get; set; }
            public string GatewayStatus { get; set; }
            public string OfflineDuration { get; set; }
            public string Signal { get; set; }
            public string Battery { get; set; }
            public string SNR { get; set; }
            public TimeSpan OfflineDurationTS { get; set; }
            public List<FlagItem> A09_Flags { get; set; }
            public class FlagItem
            {
                public int FlagID { get; set; }
                public int FlagTypeID { get; set; }
                public string FlagTypeName { get; set; }
            }
            public Data.DeviceType.DeviceTypeEnum DeviceType
            {
                get
                {
                    if (type != null)
                        return (Data.DeviceType.DeviceTypeEnum)type.id;

                    return Data.DeviceType.DeviceTypeEnum.Unknown;
                }
            }
        }

        public List<SummaryBlock> SummaryBlocks
        {
            get
            {
                List<SummaryBlock> summaryBlocks = new List<SummaryBlock>();

                foreach (var type in A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Select(c => c.DeviceType).Distinct())
                {
                    var items = A01_GatewayAndDeviceMonitoring_DeviceDetailsItems.Where(p => p.DeviceType == type).ToList();

                    SummaryBlock summaryBlock = new SummaryBlock()
                    {
                        DeviceTypeID = (int)type,
                        OnlineCount = items.Where(c => c.status.id == 1).Count(),
                        LessThan4HoursCount = items.Where(c => c.status.id != 1 && c.OfflineDurationTS.TotalHours < 4).Count(),
                        LessThan24HoursMoreThan4HoursCount = items.Where(c => c.status.id != 1 && c.OfflineDurationTS.TotalHours < 24 && c.OfflineDurationTS.TotalHours >= 4).Count(),
                        LessThan3DaysMoreThan24HoursCount = items.Where(c => c.status.id != 1 && c.OfflineDurationTS.TotalDays < 3 && c.OfflineDurationTS.TotalHours >= 24).Count(),
                        LessThan7DaysMoreThan3DaysCount = items.Where(c => c.status.id != 1 && c.OfflineDurationTS.TotalDays < 7 && c.OfflineDurationTS.TotalDays >= 3).Count(),
                        MoreThan7Days = items.Where(c => c.status.id != 1 && c.OfflineDurationTS.TotalDays >= 7).Count(),
                    };

                    summaryBlocks.Add(summaryBlock);
                }

                return summaryBlocks;
            }
        }
        public class SummaryBlock
        {
            public int? DeviceTypeID { get; set; }
            public string DeviceTypeName
            {
                get
                {

                    if (DeviceTypeID.HasValue)
                    {
                        return ((Data.DeviceType.DeviceTypeEnum)DeviceTypeID.Value).GetDescription();
                    }
                    return Data.DeviceType.DeviceTypeEnum.Unknown.GetDescription();
                }
            }

            public decimal OnlineCount { get; set; }
            public decimal LessThan4HoursCount { get; set; }
            public decimal LessThan24HoursMoreThan4HoursCount { get; set; }
            public decimal LessThan3DaysMoreThan24HoursCount { get; set; }
            public decimal LessThan7DaysMoreThan3DaysCount { get; set; }
            public decimal MoreThan7Days { get; set; }
            public decimal Total { get { return OnlineCount + LessThan4HoursCount + LessThan24HoursMoreThan4HoursCount + LessThan3DaysMoreThan24HoursCount + LessThan7DaysMoreThan3DaysCount + MoreThan7Days; } }

            public class ChartDataObject
            {
                public List<string> labels { get; set; }
                public GraphData datasets { get; set; }
                public class GraphData
                {
                    public List<decimal> data { get; set; }
                    public List<string> backgroundColor { get; set; }
                }
            }

            public ChartDataObject ChartData
            {
                get
                {
                    var chartDO = new ChartDataObject()
                    {
                        datasets = new ChartDataObject.GraphData()
                        {
                            data = new List<decimal>(),
                            backgroundColor = new List<string>(),
                        },
                        labels = new List<string>(),
                    };


                    if (OnlineCount != 0)
                    {
                        chartDO.datasets.data.Add(OnlineCount);
                        chartDO.datasets.backgroundColor.Add("#2e7f18");
                        chartDO.labels.Add("Online");
                    }

                    if (LessThan4HoursCount != 0)
                    {
                        chartDO.datasets.data.Add(LessThan4HoursCount);
                        chartDO.datasets.backgroundColor.Add("#45731e");
                        chartDO.labels.Add("< 4 Hrs");
                    }

                    if (LessThan24HoursMoreThan4HoursCount != 0)
                    {
                        chartDO.datasets.data.Add(LessThan24HoursMoreThan4HoursCount);
                        chartDO.datasets.backgroundColor.Add("#675e24");
                        chartDO.labels.Add("< 24 Hrs > 4 Hrs");
                    }

                    if (LessThan3DaysMoreThan24HoursCount != 0)
                    {
                        chartDO.datasets.data.Add(LessThan3DaysMoreThan24HoursCount);
                        chartDO.datasets.backgroundColor.Add("#8d472b");
                        chartDO.labels.Add("< 3 D > 24 Hrs");
                    }

                    if (LessThan7DaysMoreThan3DaysCount != 0)
                    {
                        chartDO.datasets.data.Add(LessThan7DaysMoreThan3DaysCount);
                        chartDO.datasets.backgroundColor.Add("#b13433");
                        chartDO.labels.Add("< 7 D > 3 D");
                    }

                    if (MoreThan7Days != 0)
                    {
                        chartDO.datasets.data.Add(MoreThan7Days);
                        chartDO.datasets.backgroundColor.Add("#c82538");
                        chartDO.labels.Add("> 7 D");
                    }

                    return chartDO;
                }
            }

            public string BGColor
            {
                get
                {

                    if (DeviceTypeID.HasValue)
                    {
                        switch ((Data.DeviceType.DeviceTypeEnum)DeviceTypeID.Value)
                        {
                            case Data.DeviceType.DeviceTypeEnum.Electricity:
                                return "#A6CE39";
                                break;
                            case Data.DeviceType.DeviceTypeEnum.Water:
                                //if (ProductName.Trim().ToLower() == ClientzoneSD.Sanitation_Consumption)
                                //{
                                //    return "#0b1d5a";
                                //}
                                //if (ProductName.Trim().ToLower() == ClientzoneSD.Water_Consumption)
                                //{
                                return "#35bead";
                                //}
                                break;
                            case Data.DeviceType.DeviceTypeEnum.Gas:
                                return "#ED1A3A";
                                break;
                        }
                    }

                    //if (ProductName.Trim().ToLower() == ClientzoneSD.Sanitation_Consumption)
                    //{
                    //    return "#0b1d5a";
                    //}
                    //if (ProductName.Trim().ToLower() == ClientzoneSD.Water_Consumption)
                    //{
                    //    return "#35bead";
                    //}

                    return "#4E5375";
                }
            }
        }
    }

}
