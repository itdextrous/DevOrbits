using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.S02_ProductCombinedReports.S02_ProductCombinedReportsModels
{
    public class S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem> S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem> S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem> S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem> S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem> S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems { get; set; }
        public List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem> A03_NetworkBalancing_Units_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem> S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Math.Round(value, 2) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Math.Round(value, 2) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem> S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem> S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem> S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem> S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem> S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem> S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right\"";
                else
                    return " class=\"font-weight-bold text-right table-danger\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right\"";
                else
                    return " class=\"text-right table-danger\"";
            }
        }

    }

    public class S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem> S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem> S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem> S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem> S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem> S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem> S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems { get; set; }

        public class S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems { get; set; }

            public class S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem : Data.SkybillCustomer
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


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (Convert.ToInt32(value) == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }

    public class S02_ProductCombinedReports_OverallAnalysis_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<S02_ProductCombinedReports_OverallAnalysis_SummaryItem> S02_ProductCombinedReports_OverallAnalysis_SummaryItems { get; set; }
        public class S02_ProductCombinedReports_OverallAnalysis_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public int ElecCount { get; set; }
            public decimal ElecAmount { get; set; }
            public decimal ElecUnits { get; set; }
            public decimal ElecCostPerUnit
            {
                get
                {
                    if (ElecUnits > 0)
                        return ElecAmount / ElecUnits;
                    return 0;
                }
            }

            public int WaterCount { get; set; }
            public decimal WaterAmount { get; set; }
            public decimal WaterUnits { get; set; }
            public decimal WaterCostPerUnit
            {
                get
                {
                    if (WaterUnits > 0)
                        return ElecAmount / WaterUnits;
                    return 0;
                }
            }

            public int GasCount { get; set; }
            public decimal GasAmount { get; set; }
            public decimal GasUnits { get; set; }
            public decimal GasCostPerUnit
            {
                get
                {
                    if (GasUnits > 0)
                        return ElecAmount / GasUnits;
                    return 0;
                }
            }

            public int GPSCount { get; set; }
            public decimal GPSAmount { get; set; }
            public decimal GPSUnits { get; set; }
            public decimal GPSCostPerUnit
            {
                get
                {
                    if (GPSUnits > 0)
                        return ElecAmount / GPSUnits;
                    return 0;
                }
            }

            public int ValveCount { get; set; }
            public decimal ValveAmount { get; set; }
            public decimal ValveUnits { get; set; }
            public decimal ValveCostPerUnit
            {
                get
                {
                    if (ValveUnits > 0)
                        return ElecAmount / ValveUnits;
                    return 0;
                }
            }

            public int OtherCount { get; set; }
            public decimal OtherAmount { get; set; }
            public decimal OtherUnits { get; set; }
            public decimal OtherCostPerUnit
            {
                get
                {
                    if (OtherUnits > 0)
                        return ElecAmount / OtherUnits;
                    return 0;
                }
            }

            public int TotalCount { get { return ElecCount + WaterCount + GasCount + GPSCount + ValveCount + OtherCount; } }
            public decimal TotalAmount { get { return ElecAmount + WaterAmount + GasAmount + GPSAmount + ValveAmount + OtherAmount; } }
            public decimal TotalUnits { get { return ElecUnits + WaterUnits + GasUnits + GPSUnits + ValveUnits + OtherUnits; } }
        }

    }

    public class S02_ProductCombinedReports_OverallAnalysis_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Data.DeviceType.DeviceTypeEnum? DeviceType { get; set; }
        public List<S02_ProductCombinedReports_OverallAnalysis_MonthlyItem> S02_ProductCombinedReports_OverallAnalysis_MonthlyItems { get; set; }

        public static string GetCellClass(decimal value, bool isBold = false)
        {
            string cellClass = "class=\"text-nowrap text-right";

            if (isBold)
                cellClass = cellClass + " font-weight-bold";

            if (Convert.ToInt32(value) >= 0
                && Convert.ToInt32(value) < 10)
                cellClass = cellClass + " table-default";
            else if (Convert.ToInt32(value) >= 10
                && Convert.ToInt32(value) < 15)
                cellClass = cellClass + " table-primary";
            else if (Convert.ToInt32(value) >= 15
                && Convert.ToInt32(value) < 20)
                cellClass = cellClass + " table-warning";
            else if (Convert.ToInt32(value) >= 20
                && Convert.ToInt32(value) < 30)
                cellClass = cellClass + " table-success";
            else if (Convert.ToInt32(value) >= 30
                && Convert.ToInt32(value) < 100)
                cellClass = cellClass + " table-danger";

            cellClass = cellClass + "\"";

            return cellClass;
        }

        public class S02_ProductCombinedReports_OverallAnalysis_MonthlyItem
        {
            public string MeterSerial { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string Occupancy { get; set; }
            public List<S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItem> S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItems { get; set; }

            public class S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItem
            {
                public DateTime BillingMonth { get; set; }
                public decimal FirstReading { get; set; }
                public decimal LastReading { get; set; }
                // Consumption - Metered
                public decimal MeteredUnits { get { return LastReading - FirstReading; } }

                // Readings - First
                public decimal BilledFirstReading { get; set; }
                // Readings - Last
                public decimal BilledLastReading { get; set; }
                public decimal BilledUnits { get { return BilledLastReading - BilledFirstReading; } }
                // Consumption - Billed
                public decimal BilledActualUnits { get; set; }
                // Charges - Billed
                public decimal BilledAmount { get; set; }
                // Consumption - Unbilled
                public decimal UnbilledUnits { get { return MeteredUnits - BilledUnits; } }

                public decimal CostPerUnit { get; set; }
                // Charges - Cost
                public decimal Cost { get { return BilledUnits * CostPerUnit; } }

                // Profitability - Profit
                public decimal Profit { get { return BilledAmount - Cost; } }
                // Profitability - Margin
                public decimal GrossProfitPerc
                {
                    get
                    {
                        if (BilledAmount > 0)
                            return (Profit / BilledAmount) * 100.0m;
                        return 0;
                    }
                }

                public decimal AgreedMonthlyRental { get; set; }

                public decimal NettProfit { get { return Profit - AgreedMonthlyRental; } }
            }

        }

    }

}
