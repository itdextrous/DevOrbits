using System;
using System.Linq;
using System.Collections.Generic;
using MyVoltage.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyVoltage.Models.OperationalModels.C08_Forecasting
{
    public class C08_Forecasting_SummaryModel
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

        public List<C08_Forecasting_SummaryItem> C08_Forecasting_SummaryItems { get; set; }
        public class C08_Forecasting_SummaryItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public string LegalEntity { get; set; }
            public string Partner { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public int ReportingDescriptionID { get; set; }
            public string ReportingDescriptionName { get; set; }
            public DateTime Date { get; set; }

            public decimal Sales_Amount { get; set; }
            public decimal Sales_Units { get; set; }
            public decimal Sales_RatePerUnit { get; set; }

            public decimal CostOfSales_Amount { get; set; }
            public decimal CostOfSales_Units { get; set; }
            public decimal CostOfSales_RatePerUnit { get; set; }

            public decimal GrossAmount_Amount { get; set; }
            public decimal GrossAmount_Units { get; set; }
            public decimal GrossAmount_RatePerUnit { get; set; }

            public decimal GrossPerc_Amount { get; set; }
            public decimal GrossPerc_Units { get; set; }
            public decimal GrossPerc_RatePerUnit { get; set; }

        }
    }

    public class C08_Forecasting_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Products { get; set; }
        public List<SelectListItem> ReportingCategoryID { get; set; }

        public List<C08_Forecasting_Details> C08_Forecasting_CostSettings_Templates { get; set; }
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<MyVoltage.Api.SkyBill.Tarrifs.Tarrif> Tarrifs { get; set; }
        public List<Data.BuildingCouncilDetail> BuildingCouncilDetails { get; set; }
        public List<Data.ManagementAccounts_ReportingCategory> ManagementAccounts_ReportingCategories { get; set; }
        public List<SelectListItem> ConsumptionTypes { get; set; }
        public F_SystemGeneratedReports_ManagementAccounts_Request LatestRequest { get; set; }

        public class F_SystemGeneratedReports_ManagementAccounts_Request : Data.F_SystemGeneratedReports_ManagementAccounts_Request
        {
            public string CreatedByUsername { get; set; }
        }

        public class C08_Forecasting_Details : Data.C08_Forecasting_CostSettings_Template
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public Data.SiteAdmin_Product SiteAdmin_Product { get; set; }
            public Data.SkybillResourceList SkybillResourceList { get; set; }
            public TarrifItem Tarrif { get; set; }
            public class TarrifItem : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public DateTime? End_Date { get; set; }
            }
            public string ResourceType { get; set; }
            public Data.ManagementAccounts_ReportingCategory ManagementAccounts_ReportingCategory { get; set; }
            public bool IsForecast1PostedMatch { get; set; }
            public bool IsForecast2PostedMatch { get; set; }
            public bool IsForecast3PostedMatch { get; set; }
            public bool IsForecast4PostedMatch { get; set; }
            public bool IsForecast5PostedMatch { get; set; }
        }

        public List<C08_ProductReport_ProductAuditViewItem> C08_ProductReport_ProductAuditViewItems { get; set; }

        public class C08_ProductReport_ProductAuditViewItem : MyVoltage.Data.ManagementAccountsDataDump
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

    public class C08_Forecasting_DetailsCalculationDetailsModel
    {
        public string ReturnURL { get; set; }
        public C08_Forecasting_DetailsCalculationDetails C08_Forecasting_CostSettings_Template { get; set; }

        public class C08_Forecasting_DetailsCalculationDetails : C08_Forecasting_DetailsModel.C08_Forecasting_Details
        {
            public string CompanyName { get; set; }
            public Controllers.Operational.C08_Forecasting.C08_ForecastingController.C08_GetCalculation C08_GetCalculation { get; set; }
        }

    }

    public class C08_Forecasting_BaselinesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> DeviceType { get; set; }

        public List<C08_Forecasting_Baseline> C08_Forecasting_Baselines { get; set; }
        public class C08_Forecasting_Baseline : Data.C08_Forecasting_Baseline
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public decimal? ForecastUnits1YearAgo { get; set; }
            public decimal? ForecastUnits2YearAgo { get; set; }
            public decimal? ForecastUnits3YearAgo { get; set; }
            public decimal ForecastUnitsAverage
            {
                get
                {
                    decimal count = 0;
                    decimal total = 0;

                    if (ForecastUnits1YearAgo.HasValue && ForecastUnits1YearAgo.Value != 0)
                    {
                        count++;
                        total += ForecastUnits1YearAgo.Value;
                    }

                    if (ForecastUnits2YearAgo.HasValue && ForecastUnits2YearAgo.Value != 0)
                    {
                        count++;
                        total += ForecastUnits2YearAgo.Value;
                    }

                    if (ForecastUnits3YearAgo.HasValue && ForecastUnits3YearAgo.Value != 0)
                    {
                        count++;
                        total += ForecastUnits3YearAgo.Value;
                    }

                    if (count != 0)
                        return total / count;

                    return 0;
                }
            }
        }
    }
}
