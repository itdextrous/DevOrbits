using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.InsightsModels
{
    public class InsightsModel
    {
        public DateTime Month { get; set; }
    }

    public class AnnualCostTotalModel
    {
        public DateTime Month { get; set; }
        public List<ProductItem> ProductItems { get; set; }

        public class ProductItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }
    }

    public class ProductsDailyCostModel
    {
        public DateTime Month { get; set; }
        public List<ProductItem> ProductItems { get; set; }

        public class ProductItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public int DeviceTypeID { get; set; }
            public List<BillingItem> DailyBillingItems { get; set; }
            public BillingData DailyBillingData
            {
                get
                {
                    BillingData billingData = new BillingData()
                    {
                        Avgs = new List<decimal>(),
                        Labels = new List<string>(),
                        Data = new BillingData.GraphData()
                        {
                            Item1 = new List<decimal>(),
                            Item2 = new List<decimal>(),
                        },
                        Totals = new List<decimal>(),
                    };

                    if (DailyBillingItems != null)
                    {
                        billingData.Avgs = DailyBillingItems.OrderBy(p => p.Date).Select(p => Convert.ToDecimal(Math.Round(p.CostPerUnit, 2))).ToList();
                        billingData.Labels = DailyBillingItems.OrderBy(p => p.Date).Select(p => $"{p.Date.Day}").ToList();
                        billingData.Data.Item1 = DailyBillingItems.OrderBy(p => p.Date).Select(p => p.Units).ToList();
                        billingData.Data.Item2 = DailyBillingItems.OrderBy(p => p.Date).Select(p => Convert.ToDecimal(Math.Round(p.CostPerUnit, 2))).ToList();
                        billingData.Totals = DailyBillingItems.OrderBy(p => p.Date).Select(p => p.Amount).ToList();
                    }

                    return billingData;
                }
            }
            public List<BillingItem> MonthlyBillingItems { get; set; }
            public BillingData MonthlyBillingData
            {
                get
                {
                    BillingData billingData = new BillingData()
                    {
                        Avgs = new List<decimal>(),
                        Labels = new List<string>(),
                        Data = new BillingData.GraphData()
                        {
                            Item1 = new List<decimal>(),
                            Item2 = new List<decimal>(),
                        },
                        Totals = new List<decimal>(),
                    };

                    if (MonthlyBillingItems != null)
                    {
                        billingData.Avgs = MonthlyBillingItems.OrderBy(p => p.Date).Select(p => Convert.ToDecimal(Math.Round(p.CostPerUnit, 2))).ToList();
                        billingData.Labels = MonthlyBillingItems.OrderBy(p => p.Date).Select(p => $"{p.Date:MM yyyy}").ToList();
                        billingData.Data.Item1 = MonthlyBillingItems.OrderBy(p => p.Date).Select(p => p.Units).ToList();
                        billingData.Data.Item2 = MonthlyBillingItems.OrderBy(p => p.Date).Select(p => Convert.ToDecimal(Math.Round(p.CostPerUnit, 2))).ToList();
                        billingData.Totals = MonthlyBillingItems.OrderBy(p => p.Date).Select(p => p.Amount).ToList();
                    }

                    return billingData;
                }
            }
            public class BillingItem
            {
                public DateTime Date { get; set; }
                public decimal Units { get; set; }
                public decimal Amount { get; set; }
                public decimal CostPerUnit
                {
                    get
                    {
                        if (Units != 0)
                            return Amount / Units;
                        return 0;
                    }
                }
            }

            public class BillingData
            {
                public List<decimal> Avgs { get; set; }
                public GraphData Data { get; set; }
                public class GraphData
                {
                    public List<decimal> Item1 { get; set; }
                    public List<decimal> Item2 { get; set; }
                }
                public List<string> Labels { get; set; }
                public List<decimal> Totals { get; set; }
            }
        }
    }

    public class CustomerBillingModel
    {
        public PaginatedList<Ledger> AllEntries { get; set; }
        public List<ExternalChargesSchedulingImport> ExternalChargesSchedulingImports { get; set; }

    }
}
