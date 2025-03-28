using System;
using System.Linq;
using System.Collections.Generic;
using MyVoltage.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Models.OperationalModels.C09_CashflowForecast
{

    public class C09_CashflowForecast_SummaryModel
    {
        public DateTime NetcashDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        public List<SelectListItem> ManagementAccounts_ReportingCategories { get; set; }
        public List<SelectListItem> ManagementAccounts_ReportingDescriptions { get; set; }
        public List<SelectListItem> Companies { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<CompanyItem> CompanyItems { get; set; }

        public List<C09_CashflowForecast_SummaryPivotItem> C09_CashflowForecast_SummaryPivotItemsSP { get; set; }
        public class C09_CashflowForecast_SummaryPivotItem
        {
            public string ReportingDescription { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
        }
        public List<C09_CashflowForecast_SummaryPivotItem> C09_CashflowForecast_SummaryPivotItemsBanksBalances { get; set; }
    }

    public class C09_CashflowForecast_UpdateBankBalancesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<SelectListItem> Companies { get; set; }

        public List<C09_CashflowForecast_UpdateBankBalances> C09_CashflowForecast_CostSettings_Templates { get; set; }
        public class C09_CashflowForecast_UpdateBankBalances : Data.Companies_OperationalBalance
        {
            public string CompanyName { get; set; }
        }
    }

    public class C09_CashflowForecast_DailyModel
    {
        public DateTime NetcashDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        [Display(Name = "Company")]
        public List<SelectListItem> CompanyID { get; set; }

        public List<SelectListItem> ManagementAccounts_ReportingCategories { get; set; }
        public List<SelectListItem> ManagementAccounts_ReportingDescriptions { get; set; }
        public List<SelectListItem> Companies { get; set; }

        public class CompanyItem
        {
            public int ID { get; set; }
            public int PartnerID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<CompanyItem> CompanyItems { get; set; }

        public List<C09_CashflowForecast_DailyPivotItem> C09_CashflowForecast_DailyPivotItemsSP { get; set; }
        public class C09_CashflowForecast_DailyPivotItem : Data.ManagementAccountsDataDumps_Forecast
        {
            public string Company { get; set; }
            public string ReportingDescription { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances
            {
                get
                {
                    return new List<KeyValuePair<DateTime, decimal>>()
                    {
                        new KeyValuePair<DateTime, decimal>(Date, ActualAmount),
                    };
                }
            }
        }
    }

    public class C09_CashflowForecast_ConsolidatedNetcashBalancesModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public bool IsMovementReport { get; set; }

        public List<C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItem> C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItemsSP { get; set; }
        public class C09_CashflowForecast_ConsolidatedNetcashBalancesPivotItem
        {
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public string AccountNo { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances { get; set; }

        }
    }

    public class C09_CashflowForecast_ConsolidatedBankBalancesModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public bool IsMovementReport { get; set; }

        public List<C09_CashflowForecast_ConsolidatedBankBalancesPivotItem> C09_CashflowForecast_ConsolidatedBankBalancesPivotItemsSP { get; set; }
        public class C09_CashflowForecast_ConsolidatedBankBalancesPivotItem
        {
            public string Company { get; set; }
            public string CompanyID { get; set; }
            public string OperationalBankName { get; set; }
            public string OperationalBankAccountType { get; set; }
            public string OperationalBankAccountNo { get; set; }
            public string OperationalBankBranchCode { get; set; }
            public List<KeyValuePair<DateTime, decimal>> ClosingBalances { get; set; }

        }
    }

}
