using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C06_LedgerReconReportModels
{
    public class C06_LedgerReconReport_SummaryModel
    {
        public List<C06_LedgerReconReport_SummaryItem> C06_LedgerReconReport_SummaryItems { get; set; }
        public class C06_LedgerReconReport_SummaryItem
        {
            public string TableRowID { get; set; }
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public decimal CoaTotal { get; set; }
            public decimal ProductsTotal { get; set; }
            public decimal LedgerTotal { get; set; }
            public decimal Diff { get { return ProductsTotal - LedgerTotal; } }
            public decimal CoaDiff { get { return CoaTotal - LedgerTotal; } }
            public DateTime? LatestSyncDate { get; set; }
            public DateTime? LatestSyncDate_Monthly { get; set; }
        }
    }

    public class C06_LedgerReconReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }

        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }

        public List<C06_LedgerReconReport_DetailsProductItem> C06_LedgerReconReport_DetailsProductItems { get; set; }
        public class C06_LedgerReconReport_DetailsProductItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DetailsSupplyCostItem> C06_LedgerReconReport_DetailsSupplyCostItems { get; set; }
        public class C06_LedgerReconReport_DetailsSupplyCostItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DetailsGrossAmountItem> C06_LedgerReconReport_DetailsGrossAmountItems { get; set; }
        public class C06_LedgerReconReport_DetailsGrossAmountItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DetailsGrossPercItem> C06_LedgerReconReport_DetailsGrossPercItems { get; set; }
        public class C06_LedgerReconReport_DetailsGrossPercItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request LatestRequest { get; set; }
        public class F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request : Data.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request
        {
            public string CreatedByUsername { get; set; }
        }


    }

    public class C06_LedgerReconReport_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }

        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }

        public List<C06_LedgerReconReport_DailyProductItem> C06_LedgerReconReport_DailyProductItems { get; set; }
        public class C06_LedgerReconReport_DailyProductItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DailySupplyCostItem> C06_LedgerReconReport_DailySupplyCostItems { get; set; }
        public class C06_LedgerReconReport_DailySupplyCostItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DailyGrossAmountItem> C06_LedgerReconReport_DailyGrossAmountItems { get; set; }
        public class C06_LedgerReconReport_DailyGrossAmountItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C06_LedgerReconReport_DailyGrossPercItem> C06_LedgerReconReport_DailyGrossPercItems { get; set; }
        public class C06_LedgerReconReport_DailyGrossPercItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }

    public class C06_TBGLReconReport_SummaryModel
    {
        public DateTime ReportingDate { get; set; }
        public List<C06_TBGLReconReport_SummaryItem> C06_TBGLReconReport_SummaryItems { get; set; }
        public class C06_TBGLReconReport_SummaryItem
        {
            public DateTime ReportingDate { get; set; }
            public string TableRowID { get; set; }
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public decimal ChartOfAccountsTotal { get; set; }
            public decimal LedgerTotal { get; set; }
            public decimal Diff { get; set; }
            public decimal DiffABS { get; set; }

            //public decimal Diff { get { return ChartOfAccountsTotal - LedgerTotal; } }
            //public decimal DiffABS
            //{
            //    get
            //    {
            //        decimal diffABS = Math.Abs(ChartOfAccountsTotal - LedgerTotal);
            //        if (diffABS < 0)
            //            diffABS = diffABS * -1.0m;

            //        return diffABS;
            //    }
            //}
            public DateTime? LatestSyncDate { get; set; }
        }
    }

    public class C06_TBGLReconReport_DetailsModel
    {
        public DateTime Date { get; set; }
        public List<C06_TBGLReconReport_DetailsItem> C06_TBGLReconReport_DetailsItems { get; set; }
        public class C06_TBGLReconReport_DetailsItem : Data.ChartOfAccountsSnapshot
        {
            public MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount ChartOfAccount { get; set; }
            public decimal LedgerTotal { get; set; }
            public decimal Diff
            {
                get
                {
                    decimal diff = Convert.ToDecimal(Amount) - LedgerTotal;

                    return diff;
                }
            }
            public decimal DiffABS
            {
                get
                {
                    decimal diff = Math.Abs(Convert.ToDecimal(Amount) - LedgerTotal);
                    if (diff < 0)
                        diff = diff * -1.0m;

                    return diff;
                }
            }
        }
        public F_SystemGeneratedReports_GenLedgerSync_Request LatestRequest { get; set; }
        public class F_SystemGeneratedReports_GenLedgerSync_Request : Data.F_SystemGeneratedReports_GenLedgerSync_Request
        {
            public string CreatedByUsername { get; set; }
        }
        public F_SystemGeneratedReports_GenLedgerSyncFULL_Request LatestRequestFULL { get; set; }
        public class F_SystemGeneratedReports_GenLedgerSyncFULL_Request : Data.F_SystemGeneratedReports_GenLedgerSyncFULL_Request
        {
            public string CreatedByUsername { get; set; }
        }
    }

}
