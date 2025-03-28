using System;
using System.Linq;
using System.Collections.Generic;
using MyVoltage.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyVoltage.Models.OperationalModels.C07_ManagementAccounts
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

    public class C07_ManagementAccounts_SummaryModel
    {
        public List<C07_ManagementAccounts_SummaryItem> C07_ManagementAccounts_SummaryItems { get; set; }
        public class C07_ManagementAccounts_SummaryItem : Data.Company
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

    public class C07_ManagementAccounts_DetailsModel
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

        public List<C07_ManagementAccounts_DetailsItem> C07_ManagementAccounts_DetailsItems { get; set; }
        public class C07_ManagementAccounts_DetailsItem
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
                public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
            }

            public List<string> ChartLabels { get { return Sales_Actual.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
            public List<decimal> ChartSales_Actual { get { return Sales_Actual.MonthlyValues.Select(p => p.Value.HasValue ? p.Value.Value : 0).ToList(); } }
            public List<decimal> ChartCostOfSales_Actual { get { return CostOfSales_Actual.MonthlyValues.Select(p => p.Value.HasValue ? p.Value.Value * -1.0m : 0).ToList(); } }
            public List<decimal> ChartGrossProfit_Actual { get { return GrossProfit_Actual.MonthlyValues.Select(p => p.Value.HasValue ? p.Value.Value : 0).ToList(); } }
            public List<decimal> ChartSales_Forecast1 { get { return Sales_Forecast1.MonthlyValues.Select(p => p.Value.HasValue ? p.Value.Value : 0).ToList(); } }
            public List<decimal> ChartGrossProfit_Forecast1 { get { return GrossProfit_Forecast1.MonthlyValues.Select(p => p.Value.HasValue ? p.Value.Value : 0).ToList(); } }

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

    public class C07_ManagementAccounts_OperatingExpensesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems { get; set; }
        public ReportingCategoryItem Total { get; set; }
        public List<ChartDataset> Total_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
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
        }

    }

    public class C07_ManagementAccounts_OtherExpensesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems { get; set; }
        public ReportingCategoryItem Total { get; set; }
        public List<ChartDataset> Total_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
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
        }

    }

    public class C07_ManagementAccounts_GrandFinale_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_Sales.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Summary
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_Sales);
                reportingParentDescriptionItems.Add(Total_CostOfSales);
                reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Gross Profit %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, (Total_GrossProfit.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

                reportingParentDescriptionItems.Add(Total_OperatingExpenses);

                ReportingCategoryItem reportingCategoryItem_EBITDA = new ReportingCategoryItem()
                {
                    ReportingDescription = "EBITDA",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartType = "line",
                    ChartyAxisID = "y-axis-1",
                    ChartColor = "#632b85",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    reportingCategoryItem_EBITDA.MonthlyValues.Add(kvp.Key, (Total_GrossProfit.MonthlyValues[kvp.Key] - Total_OperatingExpenses.MonthlyValues[kvp.Key]));
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_EBITDA);

                ReportingCategoryItem reportingCategoryItem_EBITDAPerc = new ReportingCategoryItem()
                {
                    ReportingDescription = "EBITDA %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_EBITDAPerc.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_EBITDA.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_EBITDAPerc.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_EBITDAPerc);




                reportingParentDescriptionItems.Add(Total_OtherExpenses);

                ReportingCategoryItem reportingCategoryItem_Nett = new ReportingCategoryItem()
                {
                    ReportingDescription = "Net profit before taxes",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartType = "line",
                    ChartyAxisID = "y-axis-1",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    reportingCategoryItem_Nett.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_EBITDA.MonthlyValues[kvp.Key] - Total_OtherExpenses.MonthlyValues[kvp.Key]));
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_Nett);

                ReportingCategoryItem reportingCategoryItem_NettPerc = new ReportingCategoryItem()
                {
                    ReportingDescription = "Net profit before taxes %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_NettPerc.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_Nett.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_NettPerc.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_NettPerc);


                return reportingParentDescriptionItems;
            }
        }
        public List<ChartDataset> Total_Summary_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Summary.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Summary.Where(p => p.ShowOnChart).OrderByDescending(p => p.ChartType))
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.ChartColor) ? p.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.ChartType) ? p.ChartType : "bar",
                            data = p.MonthlyValues.Values.ToList(),
                            label = p.ReportingDescription,
                            stack = p.ChartStack,
                            yAxisID = p.ChartyAxisID,
                        });
                    }
                }


                return chartDatasets;
            }
        }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Sales { get; set; }
        public ReportingCategoryItem Total_Sales { get; set; }
        public List<ChartDataset> Total_Sales_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Sales.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Sales[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_CostOfSales { get; set; }
        public ReportingCategoryItem Total_CostOfSales { get; set; }
        public List<ChartDataset> Total_CostOfSales_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_CostOfSales.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_CostOfSales[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_OperatingExpenses { get; set; }
        public ReportingCategoryItem Total_OperatingExpenses { get; set; }
        public List<ChartDataset> Total_OperatingExpenses_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_OperatingExpenses.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_OperatingExpenses)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
                        });
                    }
                }


                return chartDatasets;
            }
        }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_OtherExpenses { get; set; }
        public ReportingCategoryItem Total_OtherExpenses { get; set; }
        public List<ChartDataset> Total_OtherExpenses_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_OtherExpenses.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_OtherExpenses)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
                        });
                    }
                }


                return chartDatasets;
            }
        }

        public List<ReportingCategoryItem> ReportingParentDescriptionItems_GrossProfit_Companies { get; set; }
        public ReportingCategoryItem Total_GrossProfit_Companies { get; set; }
        public List<ChartDataset> Total_GrossProfit_Companies_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {
                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_GrossProfit_Companies.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_GrossProfit_Companies)
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
            public string ChartStack { get; set; }
            public string ChartyAxisID { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
            public List<decimal> ChartValues { get { return MonthlyValues.Values.ToList(); } }
            public bool ShowOnChart { get; set; }
            public bool IsPercentage { get; set; }
            public bool IsTotal { get; set; }
            public bool ShowLineBreakAfter { get; set; }
        }

    }

    public class C07_ManagementAccounts_GrandFinale_YearlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_Sales.MonthlyValues.Select(p => $"{p.Key.Year}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Summary
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_Sales);
                reportingParentDescriptionItems.Add(Total_CostOfSales);
                reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Gross Profit %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, (Total_GrossProfit.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

                reportingParentDescriptionItems.Add(Total_OperatingExpenses);

                ReportingCategoryItem reportingCategoryItem_EBITDA = new ReportingCategoryItem()
                {
                    ReportingDescription = "EBITDA",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartType = "line",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    reportingCategoryItem_EBITDA.MonthlyValues.Add(kvp.Key, (Total_GrossProfit.MonthlyValues[kvp.Key] - Total_OperatingExpenses.MonthlyValues[kvp.Key]));
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_EBITDA);

                ReportingCategoryItem reportingCategoryItem_EBITDAPerc = new ReportingCategoryItem()
                {
                    ReportingDescription = "EBITDA %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_EBITDAPerc.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_EBITDA.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_EBITDAPerc.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_EBITDAPerc);




                reportingParentDescriptionItems.Add(Total_OtherExpenses);

                ReportingCategoryItem reportingCategoryItem_Nett = new ReportingCategoryItem()
                {
                    ReportingDescription = "Net profit before taxes",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = true,
                    ChartType = "line",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    reportingCategoryItem_Nett.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_EBITDA.MonthlyValues[kvp.Key] - Total_OtherExpenses.MonthlyValues[kvp.Key]));
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_Nett);

                ReportingCategoryItem reportingCategoryItem_NettPerc = new ReportingCategoryItem()
                {
                    ReportingDescription = "Net profit before taxes %",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = false,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                };
                foreach (var kvp in Total_Sales.MonthlyValues)
                {
                    if (Total_Sales.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_NettPerc.MonthlyValues.Add(kvp.Key, (reportingCategoryItem_Nett.MonthlyValues[kvp.Key] / Total_Sales.MonthlyValues[kvp.Key]) * 100.0m);
                    else
                        reportingCategoryItem_NettPerc.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_NettPerc);


                return reportingParentDescriptionItems;
            }
        }
        public List<ChartDataset> Total_Summary_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Summary.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Summary.Where(p => p.ShowOnChart))
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Sales { get; set; }
        public ReportingCategoryItem Total_Sales { get; set; }
        public List<ChartDataset> Total_Sales_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Sales.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Sales[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_CostOfSales { get; set; }
        public ReportingCategoryItem Total_CostOfSales { get; set; }
        public List<ChartDataset> Total_CostOfSales_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_CostOfSales.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_CostOfSales[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_OperatingExpenses { get; set; }
        public ReportingCategoryItem Total_OperatingExpenses { get; set; }
        public List<ChartDataset> Total_OperatingExpenses_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_OperatingExpenses.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_OperatingExpenses)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
                        });
                    }
                }


                return chartDatasets;
            }
        }

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_OtherExpenses { get; set; }
        public ReportingCategoryItem Total_OtherExpenses { get; set; }
        public List<ChartDataset> Total_OtherExpenses_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_OtherExpenses.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_OtherExpenses)
                    {
                        chartDatasets.Add(new ChartDataset()
                        {
                            backgroundColor = !string.IsNullOrEmpty(p.Total.ChartColor) ? p.Total.ChartColor : System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))),
                            type = !string.IsNullOrEmpty(p.Total.ChartType) ? p.Total.ChartType : "bar",
                            data = p.Total.MonthlyValues.Values.ToList(),
                            label = p.ReportingParentDescription,
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

}
