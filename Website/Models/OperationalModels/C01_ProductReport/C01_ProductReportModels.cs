using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C01_ProductReportModels
{
    public class ChartDataset
    {
        public string label { get; set; }
        public List<decimal> data { get; set; }
        public string backgroundColor { get; set; }
        public string type { get; set; }
        public string borderColor { get { return backgroundColor; } }
        public bool fill
        {
            get
            {
                if (type == "bar")
                    return true;
                return false;
            }
        }
        public string stack { get; set; }
        public string yAxisID { get; set; }
    }

    public class C01_ProductReport_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<C01_ProductReport_SummaryItem> C01_ProductReport_SummaryItems { get; set; }

        public class C01_ProductReport_SummaryItem : Data.Company
        {
            public List<C01_ProductReport_SummarySubItem> C01_ProductReport_SummarySubItems { get; set; }
            public class C01_ProductReport_SummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal TotalSales { get; set; }
                public decimal TotalCOS { get; set; }
                public decimal GrossProfit { get { return TotalCOS + TotalSales; } }
                public decimal GrossPerc
                {
                    get
                    {
                        if (TotalSales > 0)
                            return (GrossProfit / TotalSales) * 100.0m;

                        return 0;
                    }
                }
            }

        }
    }

    public class C01_ProductReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }

        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }

        public List<C01_ProductReport_DetailsProductItem> C01_ProductReport_DetailsProductItems { get; set; }
        public class C01_ProductReport_DetailsProductItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C01_ProductReport_DetailsSupplyCostItem> C01_ProductReport_DetailsSupplyCostItems { get; set; }
        public class C01_ProductReport_DetailsSupplyCostItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C01_ProductReport_DetailsGrossAmountItem> C01_ProductReport_DetailsGrossAmountItems { get; set; }
        public class C01_ProductReport_DetailsGrossAmountItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C01_ProductReport_DetailsGrossPercItem> C01_ProductReport_DetailsGrossPercItems { get; set; }
        public class C01_ProductReport_DetailsGrossPercItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request LatestRequest { get; set; }
        public class F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request : Data.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request
        {
            public string CreatedByUsername { get; set; }
        }


    }

    public class C01_ProductReport_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }

        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }

        public List<C01_ProductReport_DailyProductItem> C01_ProductReport_DailyProductItems { get; set; }
        public class C01_ProductReport_DailyProductItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        //public List<C01_ProductReport_DailySupplyCostItem> C01_ProductReport_DailySupplyCostItems { get; set; }
        //public class C01_ProductReport_DailySupplyCostItem
        //{
        //    public int CompanyID { get; set; }
        //    public string CompanyName { get; set; }
        //    public int ProductID { get; set; }
        //    public string ProductName { get; set; }

        //    public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        //}

        //public List<C01_ProductReport_DailyGrossAmountItem> C01_ProductReport_DailyGrossAmountItems { get; set; }
        //public class C01_ProductReport_DailyGrossAmountItem
        //{
        //    public int CompanyID { get; set; }
        //    public string CompanyName { get; set; }
        //    public int ProductID { get; set; }
        //    public string ProductName { get; set; }

        //    public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        //}

        //public List<C01_ProductReport_DailyGrossPercItem> C01_ProductReport_DailyGrossPercItems { get; set; }
        //public class C01_ProductReport_DailyGrossPercItem
        //{
        //    public int CompanyID { get; set; }
        //    public string CompanyName { get; set; }
        //    public int ProductID { get; set; }
        //    public string ProductName { get; set; }

        //    public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        //}


    }

    public class C01_ProductReport_Details_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Products { get; set; }
        public List<SelectListItem> ServiceAddress { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> Tariffs { get; set; }

        public List<C01_ProductReport_Details_DetailsItem> C01_ProductReport_Details_DetailsItems { get; set; }

        public class C01_ProductReport_Details_DetailsItem
        {
            public string ServiceAddress { get; set; }

            public List<C01_ProductReport_Details_DetailsSubItem> C01_ProductReport_Details_DetailsSubItems { get; set; }

            public class C01_ProductReport_Details_DetailsSubItem : Data.SkybillCustomer
            {
                public string Occupancy { get; set; }
                public List<SkybillCustomersUtilityItem> SkybillCustomersUtilityItems { get; set; }
                public class SkybillCustomersUtilityItem : Data.SkybillCustomersUtility
                {
                    // Date, Amount
                    public List<KeyValuePair<DateTime, decimal?>> BillingFigures { get; set; }

                    public decimal Total
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum();

                            return 0;
                        }
                    }

                    public decimal TotalAVG
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() / BillingFigures.Where(p => p.Value.HasValue).Count();

                            return 0;
                        }
                    }

                    public Data.SiteAdmin_Product Product { get; set; }
                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false, string defaultClass = "")
        {
            if (isBold)
            {
                if (!value.HasValue || Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return string.IsNullOrEmpty(defaultClass) ? " class=\"font-weight-bold text-right\"" : $" class=\"font-weight-bold text-right {defaultClass}\"";
            }
            else
            {
                if (!value.HasValue || Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return string.IsNullOrEmpty(defaultClass) ? " class=\"text-right\"" : $" class=\"text-right {defaultClass}\"";
            }
        }

    }

    public class C01_ProductReport_Monthly_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Products { get; set; }
        public List<SelectListItem> ServiceAddress { get; set; }
        public bool HideNoData { get; set; }
        public List<SelectListItem> Tariffs { get; set; }

        public List<C01_ProductReport_Monthly_DetailsItem> C01_ProductReport_Monthly_DetailsItems { get; set; }

        public class C01_ProductReport_Monthly_DetailsItem
        {
            public string ServiceAddress { get; set; }

            public List<C01_ProductReport_Monthly_DetailsSubItem> C01_ProductReport_Monthly_DetailsSubItems { get; set; }

            public class C01_ProductReport_Monthly_DetailsSubItem : Data.SkybillCustomer
            {
                public string Occupancy { get; set; }
                public List<SkybillCustomersUtilityItem> SkybillCustomersUtilityItems { get; set; }
                public class SkybillCustomersUtilityItem : Data.SkybillCustomersUtility
                {
                    // Date, Amount
                    public List<KeyValuePair<DateTime, decimal?>> BillingFigures { get; set; }

                    public decimal Total
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum();

                            return 0;
                        }
                    }

                    public decimal TotalAVG
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() / BillingFigures.Where(p => p.Value.HasValue).Count();

                            return 0;
                        }
                    }

                    public Data.SiteAdmin_Product Product { get; set; }
                }
            }
        }

        public static string GetCellClass(decimal? value, bool isBold = false, string defaultClass = "")
        {
            if (isBold)
            {
                if (!value.HasValue || Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return string.IsNullOrEmpty(defaultClass) ? " class=\"font-weight-bold text-right\"" : $" class=\"font-weight-bold text-right {defaultClass}\"";
            }
            else
            {
                if (!value.HasValue || Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return string.IsNullOrEmpty(defaultClass) ? " class=\"text-right\"" : $" class=\"text-right {defaultClass}\"";
            }
        }

    }

    public class C01_ProductReport_AuditSyncReport_SummaryModel
    {
        public List<C01_ProductReport_AuditSyncReport_SummaryItem> C01_ProductReport_AuditSyncReport_SummaryItems { get; set; }
        public class C01_ProductReport_AuditSyncReport_SummaryItem : Data.Company
        {
            public string LatestReportRequestedBy { get; set; }
            public DateTime? LatestReportRequestedDate { get; set; }
            public DateTime? LatestReportStartedDate { get; set; }
            public DateTime? LatestReportCompletedDate { get; set; }
            public decimal? LatestReportProgress { get; set; }

            public string GenLatestReportRequestedBy { get; set; }
            public DateTime? GenLatestReportRequestedDate { get; set; }
            public DateTime? GenLatestReportStartedDate { get; set; }
            public DateTime? GenLatestReportCompletedDate { get; set; }
            public decimal? GenLatestReportProgress { get; set; }
        }
    }

    public class C01_ProductReport_AuditSyncReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ReportingDescriptions { get; set; }
        public bool HideNoData { get; set; }
        public List<ReportingDescriptionitem> AllReportingDescriptions { get; set; }
        public class ReportingDescriptionitem
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
        }
        public List<SelectListItem> ForecastType { get; set; }
        public int ForecastTypeID { get; set; }

        public List<C01_ProductReport_AuditSyncReport_DetailsItem> C01_ProductReport_AuditSyncReport_DetailsItems { get; set; }
        public class C01_ProductReport_AuditSyncReport_DetailsItem
        {
            public string ReportingDescription { get; set; }
            public string ReportingDescriptionCodeName { get; set; }

            public ReportingCategoryItem Sales_Actual { get; set; }
            public ReportingCategoryItem CostOfSales_Actual { get; set; }
            public ReportingCategoryItem GrossProfit_Actual { get; set; }
            public ReportingCategoryItem GrossProfit_Actual_Perc { get; set; }
            public ReportingCategoryItem Sales_Forecast1 { get; set; }
            public ReportingCategoryItem GrossProfit_Forecast1 { get; set; }
            public ReportingCategoryItem GrossProfit_Forecast1_Perc { get; set; }
            public ReportingCategoryItem GrossProfit_Difference { get; set; }
            public ReportingCategoryItem GrossProfit_Difference_Perc { get; set; }

            public class ReportingCategoryItem
            {
                public string ReportingCategory { get; set; }
                public Dictionary<DateTime, Tuple<decimal?, string>> MonthlyValues { get; set; }
            }

            public List<string> ChartLabels { get { return Sales_Actual.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
            public List<decimal> ChartSales_Actual { get { return Sales_Actual.MonthlyValues.Select(p => p.Value.Item1.HasValue ? p.Value.Item1.Value : 0).ToList(); } }
            public List<decimal> ChartCostOfSales_Actual { get { return CostOfSales_Actual.MonthlyValues.Select(p => p.Value.Item1.HasValue ? p.Value.Item1.Value * -1.0m : 0).ToList(); } }
            public List<decimal> ChartGrossProfit_Actual { get { return GrossProfit_Actual.MonthlyValues.Select(p => p.Value.Item1.HasValue ? p.Value.Item1.Value : 0).ToList(); } }
            public List<decimal> ChartSales_Forecast1 { get { return Sales_Forecast1.MonthlyValues.Select(p => p.Value.Item1.HasValue ? p.Value.Item1.Value : 0).ToList(); } }
            public List<decimal> ChartGrossProfit_Forecast1 { get { return GrossProfit_Forecast1.MonthlyValues.Select(p => p.Value.Item1.HasValue ? p.Value.Item1.Value : 0).ToList(); } }

            public List<ChartDataset> Chart_Datasets
            {
                get
                {
                    var chartDatasets = new List<ChartDataset>()
                    {
                        new ChartDataset()
                        {
                            label = "Sales - Actual",
                            data = ChartSales_Actual,
                            backgroundColor = "#39BEAE",
                            type = "bar",
                            stack = "stack 1",
                        },
                        new ChartDataset()
                        {
                            label = "Cost Of Sales - Actual",
                            data = ChartCostOfSales_Actual,
                            backgroundColor = "#ED1A3A",
                            type = "bar",
                            stack = "stack 2",
                        },
                        new ChartDataset()
                        {
                            label = "Gross Profit - Actual",
                            data = ChartGrossProfit_Actual,
                            backgroundColor = "#A6CE39",
                            type = "bar",
                            stack = "stack 3",
                        },
                        new ChartDataset()
                        {
                            label = "Sales - Forecast",
                            data = ChartSales_Forecast1,
                            type = "line",
                            backgroundColor = "#4E5375",
                            stack = "stack 4",
                        },
                        new ChartDataset()
                        {
                            label = "Gross Profit - Forecast",
                            data = ChartGrossProfit_Forecast1,
                            type = "line",
                            backgroundColor = "#6E328A",
                            stack = "stack 5",
                        }
                    };


                    return chartDatasets;
                }
            }
        }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_GrossProfit { get; set; }
        public ReportingCategoryItem Total_GrossProfit { get; set; }
        public List<ChartDataset> Total_GrossProfit_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_GrossProfit.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_GrossProfit[0].ReportingCategoryItems)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.ChartColor) ? p.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.ChartType) ? p.ChartType : "bar",
                            data = p.MonthlyValues.Values.ToList(),
                            label = p.ReportingDescription,
                        });
                    }
                }


                return chartDatasets;
            }
        }


        public class ReportingParentDescriptionItem : Data.ManagementAccounts_ReportingParentDescription
        {
            public List<ReportingCategoryItem> ReportingCategoryItems { get; set; }
            public ReportingCategoryItem Total { get; set; }
            public List<ChartDataset> Total_Datasets
            {
                get
                {
                    var chartDatasets = new List<ChartDataset>()
                    {

                    };
                    Random rnd = new Random();

                    if (ReportingCategoryItems.Count > 0)
                    {
                        foreach (var p in ReportingCategoryItems)
                        {
                            chartDatasets.Add(new ChartDataset()
                            {
                                backgroundColor = !string.IsNullOrEmpty(p.ChartColor) ? p.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                                type = !string.IsNullOrEmpty(p.ChartType) ? p.ChartType : "bar",
                                data = p.MonthlyValues.Values.ToList(),
                                label = p.ReportingDescription,
                            });
                        }
                    }


                    return chartDatasets;
                }
            }
        }
        public class ReportingCategoryItem
        {
            public int ReportingDescriptionID { get; set; }
            public string ReportingDescription { get; set; }
            public string ChartColor { get; set; }
            public string ChartType { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
            public List<decimal> ChartValues { get { return MonthlyValues.Values.ToList(); } }
            public bool ShowOnChart { get; set; }
            public bool IsPercentage { get; set; }
            public bool IsTotal { get; set; }
            public bool ShowLineBreakAfter { get; set; }
        }

    }


    public class C01_ProductReport_ProductAuditViewModel
    {
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Display(Name = "Reporting Category")]
        public List<SelectListItem> ReportingCategoryID { get; set; }

        [Display(Name = "Reporting Description")]
        public List<SelectListItem> ReportingDescriptionID { get; set; }

        [Display(Name = "Confirmed Movement Amount")]
        public List<SelectListItem> ApprovalRequired { get; set; }

        public F_SystemGeneratedReports_ManagementAccounts_Request LatestRequest { get; set; }

        public class F_SystemGeneratedReports_ManagementAccounts_Request : Data.F_SystemGeneratedReports_ManagementAccounts_Request
        {
            public string CreatedByUsername { get; set; }
        }

        public List<C01_ProductReport_ProductAuditViewItem> C01_ProductReport_ProductAuditViewItems { get; set; }

        public class C01_ProductReport_ProductAuditViewItem : MyVoltage.Data.ManagementAccountsDataDump
        {
            public string CompanyName { get; set; }
            public string ReportingCategory { get; set; }
            public string ReportingDescription { get; set; }

            public string ReviewedByUsernameActual { get; set; }
            public string ApprovedByUsernameActual { get; set; }
            public string AuditByUsernameActual { get; set; }

            public string ReviewedByUsernameForecast1 { get; set; }
            public string ApprovedByUsernameForecast1 { get; set; }
            public string AuditByUsernameForecast1 { get; set; }

            public string ReviewedByUsernameForecast2 { get; set; }
            public string ApprovedByUsernameForecast2 { get; set; }
            public string AuditByUsernameForecast2 { get; set; }

            public string ReviewedByUsernameForecast3 { get; set; }
            public string ApprovedByUsernameForecast3 { get; set; }
            public string AuditByUsernameForecast3 { get; set; }

            public string ReviewedByUsernameForecast4 { get; set; }
            public string ApprovedByUsernameForecast4 { get; set; }
            public string AuditByUsernameForecast4 { get; set; }

            public string ReviewedByUsernameForecast5 { get; set; }
            public string ApprovedByUsernameForecast5 { get; set; }
            public string AuditByUsernameForecast5 { get; set; }

        }
    }

}
