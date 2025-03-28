using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models.OperationalModels.Customer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A06_BillingControlReport
{
    public enum StatusType
    {
        [Description("Ok")]
        Ok = 1,
        [Description("Problematic")]
        Problematic = 2,
        [Description("Attention Required")]
        AttentionRequired = 3,
    }

    public class A06_BillingControlReport_OccupancySummaryItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int CustomersCount { get; set; }
        public int CustomersCheckedCount { get; set; }
        public int CustomersNotCheckedCount { get { return CustomersCount - CustomersCheckedCount; } }
        public int CustomersCheckedTooLongAgoCount { get; set; }
        public DateTime? DateLastChecked { get; set; }
        public string Status { get; set; }
    }
    public class A06_BillingControlReport_OccupancySummaryModel
    {
        public List<A06_BillingControlReport_OccupancySummaryItem> A06_BillingControlReport_OccupancySummaryItems { get; set; }
    }
    public class A06_BillingControlReport_OccupancyDetailItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public List<A06_BillingControlReport_OccupancyDetailItemCustomer> A06_BillingControlReport_OccupancyDetailItems { get; set; }

        public class A06_BillingControlReport_OccupancyDetailItemCustomer
        {
            public string CustomerNo { get; set; }
            public string CustomerMeterSerial { get; set; }
            public string Occupancy { get; set; }
            public DateTime? DateLastChecked { get; set; }
            public StatusType Status { get; set; }

            public enum StatusType
            {
                Reviewed = 1,
                TooLongAgo = 2,
                Outstanding = 3,
            }

            public static string GetStatusString(StatusType statusType)
            {
                switch (statusType)
                {
                    case StatusType.Outstanding:
                        return "Outstanding";
                    case StatusType.Reviewed:
                        return "Completed";
                    case StatusType.TooLongAgo:
                        return "Too Long Ago";
                }
                return "";
            }
        }
    }
    public class A06_BillingControlReport_OccupancyDetailModel
    {
        public int TotalCount { get; set; }
        public List<A06_BillingControlReport_OccupancyDetailItem> A06_BillingControlReport_OccupancyDetailItems { get; set; }
    }
    public class A06_BillingControlReport_OccupancyVerificationModel
    {
        public int CompanyID { get; set; }
        public Log_BillingControlReport_OccupancyVerification Log_BillingControlReport_OccupancyVerification { get; set; }
    }



    public class A06_BillingControlReport_NotBilledSummaryItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }

        public int CustomersCount { get { return CustomersPerAccountTypeCount.Select(p => p.Value).Sum(); } }
        public Dictionary<AccountTypeEnum, int> CustomersPerAccountTypeCount { get; set; }

        public int MetersCount { get { return MetersPerAccountTypeCount.Select(p => p.Value).Sum(); } }
        public Dictionary<AccountTypeEnum, int> MetersPerAccountTypeCount { get; set; }

        //public int MetersCheckedCount { get { return MetersCheckedPerAccountTypeCount.Select(p => p.Value).Sum(); } }
        //public Dictionary<AccountTypeEnum, int> MetersCheckedPerAccountTypeCount { get; set; }

        public int MetersNotBilledCount { get { return MetersNotBilledPerAccountTypeCount.Select(p => p.Value).Sum(); } }
        public Dictionary<AccountTypeEnum, int> MetersNotBilledPerAccountTypeCount { get; set; }

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

            switch (statusType)
            {
                case StatusType.Outstanding:
                    return "Outstanding";
                case StatusType.Reviewed:
                    return "Completed";
            }
            return "";
        }
    }
    public class A06_BillingControlReport_NotBilledSummaryModel
    {
        public List<A06_BillingControlReport_NotBilledSummaryItem> A06_BillingControlReport_NotBilledSummaryItems { get; set; }
    }
    public class A06_BillingControlReport_NotBilledDetailItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public List<A06_BillingControlReport_NotBilledDetailItemCustomer> A06_BillingControlReport_NotBilledDetailItems { get; set; }

        public class A06_BillingControlReport_NotBilledDetailItemCustomer
        {
            public string CompanyName { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerMeterName { get; set; }
            public string CustomerMeterSerial { get; set; }
            public DateTime? BillingDate { get; set; }
            public decimal? Units { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Rate { get; set; }
            public decimal? Reading { get; set; }
            public string Occupancy { get; set; }
            public string TodayAction { get; set; }
            public decimal? UnitsPastWeek { get; set; }
            public decimal? AmountPastWeek { get; set; }
            public AccountTypeEnum AccountType { get; set; }
            public bool IsContactorConnected { get; set; }
        }
    }
    public class A06_BillingControlReport_NotBilledDetailModel
    {
        public int TotalCount { get; set; }
        public List<A06_BillingControlReport_NotBilledDetailItem> A06_BillingControlReport_NotBilledDetailItems { get; set; }
    }
    public class A06_BillingControlReport_NotBilledVerificationModel
    {
        public List<BillingControlReport_NotBilledVerificationAction> BillingControlReport_NotBilledVerificationActions
        {
            //1 = Sorted, I fixed the problem
            //2 = Help, there is no consumption recorded from the meter
            //3 = Help, the consumption recorded is not billed on Skybill
            //4 = Nope, the system made a calculation error, everything is working as it should
            get
            {
                return new List<BillingControlReport_NotBilledVerificationAction>()
                {
                    new BillingControlReport_NotBilledVerificationAction() { ActionID = 1, ActionDisplayName = "Fixed It", ActionDescription = "I fixed the problem", ActionIcon="check-circle", ActionColor="text-green" },
                    new BillingControlReport_NotBilledVerificationAction() { ActionID = 4, ActionDisplayName = "Not an Issue", ActionDescription = "The system made a calculation error, everything is working as it should", ActionIcon="check-circle", ActionColor="text-green" },
                    new BillingControlReport_NotBilledVerificationAction() { ActionID = 2, ActionDisplayName = "No Consumption", ActionDescription = "There is no consumption recorded from the meter", ActionIcon="exclamation-circle", ActionColor="text-red" },
                    new BillingControlReport_NotBilledVerificationAction() { ActionID = 3, ActionDisplayName = "No Billing", ActionDescription = "The consumption recorded is not billed on Skybill", ActionIcon="exclamation-circle", ActionColor="text-red" },
                };
            }
        }

        public class BillingControlReport_NotBilledVerificationAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A06_BillingControlReport_NotBilledResultsModel
    {
        public int TotalCount { get; set; }
        public List<A06_BillingControlReport_NotBilledResultsItem> A06_BillingControlReport_NotBilledResultsItems { get; set; }

        public class A06_BillingControlReport_NotBilledResultsItem
        {
            public string CompanyName { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerMeterName { get; set; }
            public string CustomerMeterSerial { get; set; }
            public DateTime? BillingDate { get; set; }
            public decimal? Units { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Rate { get; set; }
            public decimal? Reading { get; set; }
            public string Occupancy { get; set; }
            public string TodayAction { get; set; }
            public decimal? UnitsPastWeek { get; set; }
            public decimal? AmountPastWeek { get; set; }
            public AccountTypeEnum? AccountType { get; set; }
            public bool IsContactorConnected { get; set; }
            public string RowClass
            {
                get
                {
                    if ((Units.HasValue && Units.Value > 0) ||
                        (UnitsPastWeek.HasValue && UnitsPastWeek.Value > 0)
                        || !Occupancy.Contains("Occupied")
                        || TodayAction.Contains("Fixed")
                        || (AccountType.HasValue && (AccountType.Value == AccountTypeEnum.PrepaidCredit || AccountType.Value == AccountTypeEnum.Metering))
                        )
                    {
                        return " class=\"table-success\"";
                    }
                    else if (TodayAction != "None" && !TodayAction.Contains("Fixed"))
                    {
                        return " class=\"table-warning\"";
                    }
                    else
                    {
                        return " class=\"table-danger\"";
                    }

                    return "";
                }
            }

            public string CellClass
            {
                get
                {
                    if ((Units.HasValue && Units.Value > 0) ||
                        (UnitsPastWeek.HasValue && UnitsPastWeek.Value > 0)
                        || !Occupancy.Contains("Occupied")
                        || TodayAction.Contains("Fixed")
                        || (AccountType.HasValue && (AccountType.Value == AccountTypeEnum.PrepaidCredit || AccountType.Value == AccountTypeEnum.Metering))
                        )
                    {
                        return " class=\"text-green font-weight-bold\"";
                    }
                    else if (TodayAction != "None" && !TodayAction.Contains("Fixed"))
                    {
                        return " class=\"text-orange font-weight-bold\"";
                    }
                    else
                    {
                        return " class=\"text-red font-weight-bold\"";
                    }

                    return "";
                }
            }
        }
    }

    public class A06_BillingControlReport_BillingBlockedSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (BlockedCount > 0
                    )
                    return StatusType.Problematic;

                return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int BlockedCount { get; set; }

        public static string GetStatusString(StatusType statusType)
        {
            return statusType.GetDescription();
        }
    }

    public class A06_BillingControlReport_BillingBlockedDetailsModel
    {
        public List<A06_BillingControlReport_BillingBlockedDetailsItem> A06_BillingControlReport_BillingBlockedDetailsItems { get; set; }

        public class A06_BillingControlReport_BillingBlockedDetailsItem : Data.SkybillCustomer
        {
            public StatusType Status
            {
                get
                {
                    if (!string.IsNullOrEmpty(Blocked))
                        return StatusType.Problematic;

                    return StatusType.Ok;
                }
            }
        }
    }

    public class A06_BillingControlReport_DailyBillingOverview_SummaryModel
    {
        public List<A06_BillingControlReport_DailyBillingOverview_SummaryItem> A06_BillingControlReport_DailyBillingOverview_SummaryItems { get; set; }

        public class A06_BillingControlReport_DailyBillingOverview_SummaryItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
        public bool IsDailyBillingStatusActive { get; set; }
            public Dictionary<DateTime, decimal?> BilledAmounts { get; set; }
            public Dictionary<DateTime, decimal?> BilledUnits { get; set; }
            public int MeterCount { get; set; }
            public string Status { get; set; }
        }
    }

    public class A06_BillingControlReport_TarrifReviewModel
    {
        public List<A06_BillingControlReport_TarrifReviewItem> A06_BillingControlReport_TarrifReviewItems { get; set; }

        public class A06_BillingControlReport_TarrifReviewItem : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
        {
            public Data.SkybillResourceList SkybillResource { get; set; }
            public Data.SiteAdmin_Product Product { get; set; }
        }
    }
    public class A06_BillingControlReport_PrepaidControl_SummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public string TableRowID { get; set; }

        public int CustomerInPrepaidModeCount { get; set; }
        public int CustomersInWalletOrPostPaidModeCount { get; set; }
        public int CustomersWithZeroWalletBalanceCount { get; set; }
        public int CustomersWithWalletBalanceCount { get; set; }

        public StatusType Status
        {
            get
            {
                if (CustomersInWalletOrPostPaidModeCount > 0 && CustomersWithWalletBalanceCount > 0)
                    return StatusType.Problematic;

                return StatusType.Ok;
            }
        }
    }

    public class A06_BillingControlReport_PrepaidControl_DetailsModel
    {
        public List<A06_BillingControlReport_PrepaidControl_DetailsItem> A06_BillingControlReport_PrepaidControl_DetailsItems { get; set; }

        public class A06_BillingControlReport_PrepaidControl_DetailsItem
        {
            public string CustomerNo { get; set; }
            public decimal? Balance { get; set; }
            public AccountTypeEnum AccountType { get; set; }
            public string MeterDescription { get; set; }
            public string SerialNo { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string OnlineStatus { get; set; }
            public string DisconnectionType { get; set; }
            public string ContactorState { get; set; }
            public string RemainingCredit { get; set; }
        }
    }
}
