using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C03_ReportModels
{
    public class C03_AverageAndPerUnitReport_SummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<C03_AverageAndPerUnitReport_SummaryItem> C03_AverageAndPerUnitReport_SummaryItems { get; set; }

        public class C03_AverageAndPerUnitReport_SummaryItem : Data.Company
        {
            public decimal? AvgMeterPointPerUnit
            {
                get
                {
                    if (NoOfMeteringPoints.HasValue && NoOfRegisteredUnits.HasValue && NoOfRegisteredUnits.Value != 0)
                    {
                        return Convert.ToDecimal(NoOfMeteringPoints) / Convert.ToDecimal(NoOfRegisteredUnits);
                    }

                    return null;
                }
            }

            public C03_AverageAndPerUnitReport_SummaryItemSubItem AllProducts_Total { get; set; }
            public C03_AverageAndPerUnitReport_SummaryItemSubItem AllProducts_PerUnit { get; set; }

            public C03_AverageAndPerUnitReport_SummaryItemSubItem MeterCharges_Total { get; set; }
            public C03_AverageAndPerUnitReport_SummaryItemSubItem MeterCharges_PerUnit { get; set; }

            public C03_AverageAndPerUnitReport_SummaryItemSubItem CombinedTotal_Total { get; set; }
            public C03_AverageAndPerUnitReport_SummaryItemSubItem CombinedTotal_PerUnit { get; set; }

            public class C03_AverageAndPerUnitReport_SummaryItemSubItem
            {
                public decimal Sales { get; set; }
                public decimal CostOfSales { get; set; }
                public decimal GrossProfit { get { return Sales + CostOfSales; } }
                public decimal GrossProfitPerc
                {
                    get
                    {
                        if (Sales > 0)
                        {
                            return (GrossProfit / Sales) * 100.0m;
                        }

                        return 0;
                    }
                }
            }
        }
    }
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

    public class C03_Report_UserAndMeter_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_RegisteredUnits.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Summary
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_RegisteredUnits);
                reportingParentDescriptionItems.Add(Total_MeteringPoints);
                //reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Average number of Metering per unit",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = true,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_RegisteredUnits.MonthlyValues)
                {
                    if (Total_MeteringPoints.MonthlyValues.ContainsKey(kvp.Key) && Total_RegisteredUnits.MonthlyValues.ContainsKey(kvp.Key) && Total_RegisteredUnits.MonthlyValues[kvp.Key] != 0)
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, (Total_MeteringPoints.MonthlyValues[kvp.Key] / Total_RegisteredUnits.MonthlyValues[kvp.Key]));
                    else
                        reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, 0);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_RegisteredUnits { get; set; }
        public ReportingCategoryItem Total_RegisteredUnits { get; set; }
        public List<ChartDataset> Total_RegisteredUnits_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_RegisteredUnits.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_RegisteredUnits[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_MeteringPoints { get; set; }
        public ReportingCategoryItem Total_MeteringPoints { get; set; }
        public List<ChartDataset> Total_MeteringPoints_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_MeteringPoints.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_MeteringPoints[0].ReportingCategoryItems)
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

        public class ReportingParentDescriptionItem
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

    public class C03_Report_ReceiptPerProperty_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_UniPins.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Summary
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_UniPins);
                reportingParentDescriptionItems.Add(Total_Payments);
                reportingParentDescriptionItems.Add(Total_NetcashManualPayments);
                //reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Total",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_UniPins.MonthlyValues)
                {
                    decimal amount = 0;
                    if (Total_Payments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_Payments.MonthlyValues[kvp.Key];

                    if (Total_UniPins.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_UniPins.MonthlyValues[kvp.Key];

                    if (Total_NetcashManualPayments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_NetcashManualPayments.MonthlyValues[kvp.Key];

                    reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, amount);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_UniPins { get; set; }
        public ReportingCategoryItem Total_UniPins { get; set; }
        public List<ChartDataset> Total_UniPins_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_UniPins.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_UniPins[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Payments { get; set; }
        public ReportingCategoryItem Total_Payments { get; set; }
        public List<ChartDataset> Total_Payments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Payments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Payments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_NetcashManualPayments { get; set; }
        public ReportingCategoryItem Total_NetcashManualPayments { get; set; }
        public List<ChartDataset> Total_NetcashManualPayments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_NetcashManualPayments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_NetcashManualPayments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Totals { get; set; }
        public ReportingCategoryItem Total_Totals { get; set; }
        public List<ChartDataset> Total_Totals_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Totals.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Totals[0].ReportingCategoryItems)
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

        public class ReportingParentDescriptionItem
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

    public class C03_Report_ReceiptPerProperty_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_UniPins.MonthlyValues.Select(p => $"{p.Key.ToDateShort()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Details
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_UniPins);
                reportingParentDescriptionItems.Add(Total_Payments);
                reportingParentDescriptionItems.Add(Total_NetcashManualPayments);
                //reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Total",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_UniPins.MonthlyValues)
                {
                    decimal amount = 0;
                    if (Total_Payments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_Payments.MonthlyValues[kvp.Key];

                    if (Total_UniPins.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_UniPins.MonthlyValues[kvp.Key];

                    if (Total_NetcashManualPayments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_NetcashManualPayments.MonthlyValues[kvp.Key];

                    reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, amount);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

                return reportingParentDescriptionItems;
            }
        }
        public List<ChartDataset> Total_Details_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Details.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Details.Where(p => p.ShowOnChart).OrderByDescending(p => p.ChartType))
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_UniPins { get; set; }
        public ReportingCategoryItem Total_UniPins { get; set; }
        public List<ChartDataset> Total_UniPins_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_UniPins.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_UniPins[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Payments { get; set; }
        public ReportingCategoryItem Total_Payments { get; set; }
        public List<ChartDataset> Total_Payments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Payments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Payments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_NetcashManualPayments { get; set; }
        public ReportingCategoryItem Total_NetcashManualPayments { get; set; }
        public List<ChartDataset> Total_NetcashManualPayments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_NetcashManualPayments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_NetcashManualPayments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Totals { get; set; }
        public ReportingCategoryItem Total_Totals { get; set; }
        public List<ChartDataset> Total_Totals_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Totals.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Totals[0].ReportingCategoryItems)
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

        public class ReportingParentDescriptionItem
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

    public class C03_Report_ReceiptPerPropertyVolumes_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_UniPins.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Volumes
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_UniPins);
                reportingParentDescriptionItems.Add(Total_Payments);
                reportingParentDescriptionItems.Add(Total_NetcashManualPayments);
                //reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Total",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_UniPins.MonthlyValues)
                {
                    decimal amount = 0;
                    if (Total_Payments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_Payments.MonthlyValues[kvp.Key];

                    if (Total_UniPins.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_UniPins.MonthlyValues[kvp.Key];

                    if (Total_NetcashManualPayments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_NetcashManualPayments.MonthlyValues[kvp.Key];

                    reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, amount);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

                return reportingParentDescriptionItems;
            }
        }
        public List<ChartDataset> Total_Volumes_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Volumes.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Volumes.Where(p => p.ShowOnChart).OrderByDescending(p => p.ChartType))
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_UniPins { get; set; }
        public ReportingCategoryItem Total_UniPins { get; set; }
        public List<ChartDataset> Total_UniPins_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_UniPins.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_UniPins[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Payments { get; set; }
        public ReportingCategoryItem Total_Payments { get; set; }
        public List<ChartDataset> Total_Payments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Payments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Payments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_NetcashManualPayments { get; set; }
        public ReportingCategoryItem Total_NetcashManualPayments { get; set; }
        public List<ChartDataset> Total_NetcashManualPayments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_NetcashManualPayments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_NetcashManualPayments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Totals { get; set; }
        public ReportingCategoryItem Total_Totals { get; set; }
        public List<ChartDataset> Total_Totals_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Totals.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Totals[0].ReportingCategoryItems)
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

        public class ReportingParentDescriptionItem
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

    public class C03_Report_ReceiptPerPropertyVolumes_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> AmountType { get; set; }
        public List<SelectListItem> Partner { get; set; }
        public List<SelectListItem> Company { get; set; }
        public List<Data.Company> Companies { get; set; }

        public List<string> ChartLabels { get { return Total_UniPins.MonthlyValues.Select(p => $"{p.Key.ToMonth()}").ToList(); } }
        public List<ReportingCategoryItem> ReportingParentDescriptionItems_Volumes
        {
            get
            {
                List<ReportingCategoryItem> reportingParentDescriptionItems = new List<ReportingCategoryItem>()
                {

                };

                reportingParentDescriptionItems.Add(Total_UniPins);
                reportingParentDescriptionItems.Add(Total_Payments);
                reportingParentDescriptionItems.Add(Total_NetcashManualPayments);
                //reportingParentDescriptionItems.Add(Total_GrossProfit);

                ReportingCategoryItem reportingCategoryItem_GrossProfit = new ReportingCategoryItem()
                {
                    ReportingDescription = "Total",
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                    IsTotal = true,
                    IsPercentage = false,
                    ShowOnChart = false,
                    ChartType = "line",
                    ShowLineBreakAfter = true,
                };
                foreach (var kvp in Total_UniPins.MonthlyValues)
                {
                    decimal amount = 0;
                    if (Total_Payments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_Payments.MonthlyValues[kvp.Key];

                    if (Total_UniPins.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_UniPins.MonthlyValues[kvp.Key];

                    if (Total_NetcashManualPayments.MonthlyValues.ContainsKey(kvp.Key))
                        amount += Total_NetcashManualPayments.MonthlyValues[kvp.Key];

                    reportingCategoryItem_GrossProfit.MonthlyValues.Add(kvp.Key, amount);
                }
                reportingParentDescriptionItems.Add(reportingCategoryItem_GrossProfit);

                return reportingParentDescriptionItems;
            }
        }
        public List<ChartDataset> Total_Volumes_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Volumes.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Volumes.Where(p => p.ShowOnChart).OrderByDescending(p => p.ChartType))
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_UniPins { get; set; }
        public ReportingCategoryItem Total_UniPins { get; set; }
        public List<ChartDataset> Total_UniPins_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_UniPins.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_UniPins[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Payments { get; set; }
        public ReportingCategoryItem Total_Payments { get; set; }
        public List<ChartDataset> Total_Payments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Payments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Payments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_NetcashManualPayments { get; set; }
        public ReportingCategoryItem Total_NetcashManualPayments { get; set; }
        public List<ChartDataset> Total_NetcashManualPayments_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_NetcashManualPayments.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_NetcashManualPayments[0].ReportingCategoryItems)
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

        public List<ReportingParentDescriptionItem> ReportingParentDescriptionItems_Totals { get; set; }
        public ReportingCategoryItem Total_Totals { get; set; }
        public List<ChartDataset> Total_Totals_Datasets
        {
            get
            {
                var chartDatasets = new List<ChartDataset>()
                {

                };
                Random rnd = new Random();

                if (ReportingParentDescriptionItems_Totals.Count > 0)
                {
                    foreach (var p in ReportingParentDescriptionItems_Totals[0].ReportingCategoryItems)
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

        public class ReportingParentDescriptionItem
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
}
