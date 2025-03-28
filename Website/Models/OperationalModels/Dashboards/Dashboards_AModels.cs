using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Dashboards.Dashboards_AModels
{
    public class A1_011_Gateway_SummaryModel
    {
        public List<A1_011_Gateway_SummaryItem> A1_011_Gateway_SummaryItems { get; set; }

        public class A1_011_Gateway_SummaryItem : Data.Company
        {
            public OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType Status { get; set; }
            public int DevicesImpacted { get; set; }
            public int Offline { get; set; }
        }
    }

    public class A1_021_Device_SummaryModel
    {
        public List<A1_021_Device_SummaryItem> A1_021_Device_SummaryItems { get; set; }

        public class A1_021_Device_SummaryItem : Data.Company
        {
            public OperationalModels.A01_GatewayAndDeviceMonitoring.A01_GatewayAndDeviceMonitoring_DeviceSummaryModel.StatusType Status { get; set; }
            public int LessThan7DaysCount { get; set; }
            public int MoreThan7DaysCount { get; set; }
            public int Offline { get; set; }
        }
    }

    public class A2_011_Meter_Calibration_SummaryModel
    {
        public List<A2_011_Meter_Calibration_SummaryItem> A2_011_Meter_Calibration_SummaryItems { get; set; }

        public class A2_011_Meter_Calibration_SummaryItem : Data.Company
        {
            public OperationalModels.A02_MirrorMeterAuditing.A02_MirrorMeterAuditing_MeterCalibrationSummaryItem.StatusType Status { get; set; }
            public int CustomersCount { get; set; }
            public int MetersCount { get; set; }
            public int MetersCalibratedCount { get; set; }
            public int MetersNotCalibratedCount { get; set; }
            public int MetersProblematicCount { get; set; }
            public int MetersThatCannotBeCalibratedCount { get; set; }
        }
    }

    public class A2_022_Mirror_Reading_SummaryModel
    {
        public List<A2_022_Mirror_Reading_SummaryItem> A2_022_Mirror_Reading_SummaryItems { get; set; }

        public class A2_022_Mirror_Reading_SummaryItem : Data.Company
        {
            public string Status { get; set; }
            public int CustomersCount { get; set; }
            public int CustomersCheckedCount { get; set; }
            public int CustomersNotCheckedCount { get; set; }
        }
    }

    public class A3_011_Network_Balancing_SummaryModel
    {
        public List<A3_011_Network_Balancing_SummaryItem> A3_011_Network_Balancing_SummaryItems { get; set; }

        public class A3_011_Network_Balancing_SummaryItem : Data.Company
        {
            public A03_NetworkBalancing.A03_NetworkBalancing_SummaryItemModel.StatusType Status { get; set; }
            public DateTime? LatestReportMonth { get; set; }
            public int ReportCount { get; set; }
            public int ProblematicReportCount { get; set; }
        }
    }

    public class A4_031_Zendesk_Ticket_Category_SummaryModel
    {
        public List<A4_031_Zendesk_Ticket_Category_SummaryItem> A4_031_Zendesk_Ticket_Category_SummaryItems { get; set; }

        public class A4_031_Zendesk_Ticket_Category_SummaryItem
        {
            public Data.Zendesk_TicketField_Option Zendesk_TicketField_Option { get; set; }
            public A04_Tickets.A04_TicketsModels.A04_Tickets_ZendeskTicketCategorySummaryModel.A04_Tickets_ZendeskTicketCategorySummaryItem.StatusEnum Status { get; set; }
            public int TotalTickets { get { return PendingCount + HoldCount + ClosedCount + OpenCount + SolvedCount; } }
            public int PendingCount { get; set; }
            public int HoldCount { get; set; }
            public int ClosedCount { get; set; }
            public int OpenCount { get; set; }
            public int SolvedCount { get; set; }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
        }
    }

    public class A5_011_Midnight_Sync_SummaryModel
    {
        public List<A5_011_Midnight_Sync_SummaryItem> A5_011_Midnight_Sync_SummaryItems { get; set; }

        public class A5_011_Midnight_Sync_SummaryItem : MyVoltageApi.Data.DeviceReadingsMidnightSync_Item
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
        }
    }

    public class A6_011_Occupancy_SummaryModel
    {
        public List<A6_011_Occupancy_SummaryItem> A6_011_Occupancy_SummaryItems { get; set; }

        public class A6_011_Occupancy_SummaryItem : Data.Company
        {
            public int CustomersCount { get; set; }
            public int CustomersCheckedCount { get; set; }
            public int CustomersNotCheckedCount { get { return CustomersCount - CustomersCheckedCount; } }
            public int CustomersCheckedTooLongAgoCount { get; set; }
            public DateTime? DateLastChecked { get; set; }
            public string Status { get; set; }
        }
    }

    public class A6_031_Not_Billed_SummaryModel
    {
        public List<A6_031_Not_Billed_SummaryItem> A6_031_Not_Billed_SummaryItems { get; set; }

        public class A6_031_Not_Billed_SummaryItem : Data.Company
        {
            public A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType Status { get; set; }
            public int CustomersCount { get { return CustomersPerAccountTypeCount.Select(p => p.Value).Sum(); } }
            public Dictionary<AccountTypeEnum, int> CustomersPerAccountTypeCount { get; set; }

            public int MetersCount { get { return MetersPerAccountTypeCount.Select(p => p.Value).Sum(); } }
            public Dictionary<AccountTypeEnum, int> MetersPerAccountTypeCount { get; set; }

            //public int MetersCheckedCount { get { return MetersCheckedPerAccountTypeCount.Select(p => p.Value).Sum(); } }
            //public Dictionary<AccountTypeEnum, int> MetersCheckedPerAccountTypeCount { get; set; }

            public int MetersNotBilledCount { get { return MetersNotBilledPerAccountTypeCount.Select(p => p.Value).Sum(); } }
            public Dictionary<AccountTypeEnum, int> MetersNotBilledPerAccountTypeCount { get; set; }
        }
    }

    public class A6_041_Billing_Blocked_SummaryModel
    {
        public List<A6_041_Billing_Blocked_SummaryItem> A6_041_Billing_Blocked_SummaryItems { get; set; }

        public class A6_041_Billing_Blocked_SummaryItem : Data.Company
        {
            public MyVoltage.Models.OperationalModels.A06_BillingControlReport.StatusType Status
            {
                get
                {
                    if (BlockedCount > 0
                        )
                        return MyVoltage.Models.OperationalModels.A06_BillingControlReport.StatusType.Problematic;

                    return MyVoltage.Models.OperationalModels.A06_BillingControlReport.StatusType.Ok;
                }
            }
            public int BlockedCount { get; set; }
        }
    }

    public class A7_021_Credit_Control_SummaryModel
    {
        public List<A7_021_Credit_Control_SummaryItem> A7_021_Credit_Control_SummaryItems { get; set; }

        public class A7_021_Credit_Control_SummaryItem : Data.Company
        {
            public A07_CreditControlAndNotifierProcess.StatusType Status
            {
                get
                {
                    if (A07_CreditControlAndNotifierProcess_CreditControlSummaryItems != null && A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Count > 0)
                    {
                        decimal totalBalanceAmount = A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Select(p => p.Amount).Sum();

                        if (totalBalanceAmount < 0)
                            return A07_CreditControlAndNotifierProcess.StatusType.Problematic;
                    }

                    return A07_CreditControlAndNotifierProcess.StatusType.Ok;
                }
            }
            public int CustomersCount { get; set; }
            public List<A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_CreditControlSummaryModel.A07_CreditControlAndNotifierProcess_CreditControlSummaryItem> A07_CreditControlAndNotifierProcess_CreditControlSummaryItems { get; set; }
        }
    }

    public class A7_031_Meter_Mode_SummaryModel
    {
        public List<A7_031_Meter_Mode_SummaryItem> A7_031_Meter_Mode_SummaryItems { get; set; }

        public class A7_031_Meter_Mode_SummaryItem : Data.Company
        {
            public MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType Status
            {
                get
                {
                    if (MeterInPostPaidModeCount > 0
                        || CreditOnWallerMeterCount > 0
                        || NoModeInSkybillCount > 0
                        )
                        return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Problematic;

                    return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok;
                }
            }
            public int CustomersCount { get; set; }
            public int MeterCount { get; set; }
            public int MeterInPostPaidModeCount { get; set; }
            public int CreditOnWallerMeterCount { get; set; }
            public int NoModeInSkybillCount { get; set; }
        }
    }

    public class A7_041_Meter_On_Manual_SummaryModel
    {
        public List<A7_041_Meter_On_Manual_SummaryItem> A7_041_Meter_On_Manual_SummaryItems { get; set; }

        public class A7_041_Meter_On_Manual_SummaryItem : Data.Company
        {
            public MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType Status
            {
                get
                {
                    if (ElecMetersOnManual > 0
                        )
                        return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Problematic;

                    return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok;
                }
            }
            public int CustomersCount { get; set; }
            public int MeterCount { get; set; }
            public int ElecMetersOnManual { get; set; }
        }
    }

    public class A7_043_Meter_On_Manual_Request_SummaryModel
    {
        public List<A7_043_Meter_On_Manual_Request_SummaryItem> A7_043_Meter_On_Manual_Request_SummaryItems { get; set; }

        public class A7_043_Meter_On_Manual_Request_SummaryItem : Data.Company
        {
            public MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType Status
            {
                get
                {
                    if (PendingCount > 0
                        )
                        return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Problematic;

                    return MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess.StatusType.Ok;
                }
            }
            public int PendingCount { get; set; }
            public int ResolvedCount { get; set; }
            public int TotalCount { get { return PendingCount + ResolvedCount; } }
        }
    }

    public class A7_051_Connector_SummaryModel
    {
        public List<A7_051_Connector_SummaryItem> A7_051_Connector_SummaryItems { get; set; }

        public class A7_051_Connector_SummaryItem : Data.Company
        {
            public A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem.StatusType Status
            {
                get
                {
                    if (LessThan7DaysCount > 0
                        || MoreThan7DaysCount > 0)
                        return A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem.StatusType.Problematic;
                    else if (LessThan3DaysCount > 0)
                        return A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem.StatusType.AttentionRequired;
                    else
                        return A07_CreditControlAndNotifierProcess.A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem.StatusType.Ok;
                }
            }
            public int OfflineCount { get; set; }
            public int OnlineCount { get; set; }
            public int TotalCount { get { return OfflineCount + OnlineCount; } }
            public int LessThan4HoursCount { get; set; }
            public int LessThan24HoursCount { get; set; }
            public int LessThan3DaysCount { get; set; }
            public int LessThan7DaysCount { get; set; }
            public int MoreThan7DaysCount { get; set; }
            public DateTime? LatestConnectionRun { get; set; }
        }
    }

    public class A10_021_VirtualMeters_SummaryModel
    {
        public List<A10_021_VirtualMeters_SummaryItem> A10_021_VirtualMeters_SummaryItems { get; set; }

        public class A10_021_VirtualMeters_SummaryItem : Data.Company
        {
            public int MasterMeters { get; set; }
            public int TOUMeters { get; set; }
            public int MetersWithDiff { get; set; }
        }
    }

    public class A8_011_Tasks_Company_SummaryModel
    {
        public List<A08_Tasks_Company_SummaryStatusItem> A08_Tasks_Company_SummaryStatusItems { get; set; }
        public class A08_Tasks_Company_SummaryStatusItem : Data.SiteAdmin_Status
        {
            public string GroupName { get; set; }
            public string ActionName { get; set; }
            public string ReportingName { get; set; }
        }
        public List<A8_011_Tasks_Company_SummaryItem> A8_011_Tasks_Company_SummaryItems { get; set; }
        public class A8_011_Tasks_Company_SummaryItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }

            public List<A08_Tasks_Company_SummaryItemStatus> A08_Tasks_Company_SummaryItemStatuses { get; set; }
            public class A08_Tasks_Company_SummaryItemStatus
            {
                public int StatusID { get; set; }
                public int Count { get; set; }  
            }

            public int Total
            {
                get
                {
                    if (A08_Tasks_Company_SummaryItemStatuses != null && A08_Tasks_Company_SummaryItemStatuses.Count > 0)
                        return A08_Tasks_Company_SummaryItemStatuses.Select(p => p.Count).Sum();

                    return 0;
                }
            }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTaskCreateDate { get; set; }
            public int? OldestUnresolvedTaskID { get; set; }
            public int? OldestUnresolvedTaskTypeID { get; set; }

        }
    }

}
