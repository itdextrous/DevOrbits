using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class ManagementAccountsDataDump
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string PropertyType { get; set; }
        public string Province { get; set; }
        public string LocalMunicipality { get; set; }
        public string LegalEntity { get; set; }
        public string Partner { get; set; }
        public int ReportingCategoryID { get; set; }
        public int ReportingDescriptionID { get; set; }
        public string AccountNo { get; set; }
        public string Reference { get; set; }
        public string Basis { get; set; }
        public string SourceName { get; set; }
        public DateTime Date { get; set; }
        public int ActualRegisteredUnits { get; set; }
        public int ActualMeteringPoints { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal? ActualAmountPerRegisteredUnit { get; set; }
        public decimal? ActualAmountPerMeteringPoint { get; set; }
        public int? Forecast1RegisteredUnits { get; set; }
        public int? Forecast1MeteringPoints { get; set; }
        public decimal Forecast1Amount { get; set; }
        public decimal? Forecast1AmountPerRegisteredUnit { get; set; }
        public decimal? Forecast1AmountPerMeteringPoint { get; set; }
        public int? Forecast2RegisteredUnits { get; set; }
        public int? Forecast2MeteringPoints { get; set; }
        public decimal Forecast2Amount { get; set; }
        public decimal? Forecast2AmountPerRegisteredUnit { get; set; }
        public decimal? Forecast2AmountPerMeteringPoint { get; set; }
        public int? Forecast3RegisteredUnits { get; set; }
        public int? Forecast3MeteringPoints { get; set; }
        public decimal Forecast3Amount { get; set; }
        public decimal? Forecast3AmountPerRegisteredUnit { get; set; }
        public decimal? Forecast3AmountPerMeteringPoint { get; set; }
        public int? Forecast4RegisteredUnits { get; set; }
        public int? Forecast4MeteringPoints { get; set; }
        public decimal? Forecast4Amount { get; set; }
        public decimal? Forecast4AmountPerRegisteredUnit { get; set; }
        public decimal? Forecast4AmountPerMeteringPoint { get; set; }
        public int? Forecast5RegisteredUnits { get; set; }
        public int? Forecast5MeteringPoints { get; set; }
        public decimal? Forecast5Amount { get; set; }
        public decimal? Forecast5AmountPerRegisteredUnit { get; set; }
        public decimal? Forecast5AmountPerMeteringPoint { get; set; }
        public DateTime? DateActualAmountsSynced { get; set; }
        public string ReviewedByActual { get; set; }
        public DateTime? ReviewedDateActual { get; set; }
        public string ApprovedByActual { get; set; }
        public DateTime? ApprovedDateActual { get; set; }
        public string AuditByActual { get; set; }
        public DateTime? AuditDateActual { get; set; }
        public string ReviewedByForecast1 { get; set; }
        public DateTime? ReviewedDateForecast1 { get; set; }
        public string ApprovedByForecast1 { get; set; }
        public DateTime? ApprovedDateForecast1 { get; set; }
        public string AuditByForecast1 { get; set; }
        public DateTime? AuditDateForecast1 { get; set; }
        public string ReviewedByForecast2 { get; set; }
        public DateTime? ReviewedDateForecast2 { get; set; }
        public string ApprovedByForecast2 { get; set; }
        public DateTime? ApprovedDateForecast2 { get; set; }
        public string AuditByForecast2 { get; set; }
        public DateTime? AuditDateForecast2 { get; set; }
        public string ReviewedByForecast3 { get; set; }
        public DateTime? ReviewedDateForecast3 { get; set; }
        public string ApprovedByForecast3 { get; set; }
        public DateTime? ApprovedDateForecast3 { get; set; }
        public string AuditByForecast3 { get; set; }
        public DateTime? AuditDateForecast3 { get; set; }
        public string ReviewedByForecast4 { get; set; }
        public DateTime? ReviewedDateForecast4 { get; set; }
        public string ApprovedByForecast4 { get; set; }
        public DateTime? ApprovedDateForecast4 { get; set; }
        public string AuditByForecast4 { get; set; }
        public DateTime? AuditDateForecast4 { get; set; }
        public string ReviewedByForecast5 { get; set; }
        public DateTime? ReviewedDateForecast5 { get; set; }
        public string ApprovedByForecast5 { get; set; }
        public DateTime? ApprovedDateForecast5 { get; set; }
        public string AuditByForecast5 { get; set; }
        public DateTime? AuditDateForecast5 { get; set; }
        public DateTime? DateForecast1AmountsSynced { get; set; }
        public DateTime? DateForecast2AmountsSynced { get; set; }
        public DateTime? DateForecast3AmountsSynced { get; set; }
        public DateTime? DateForecast4AmountsSynced { get; set; }
        public DateTime? DateForecast5AmountsSynced { get; set; }
        public decimal? ActualRatePerUnit { get; set; }
        public decimal? ActualUnits { get; set; }
        public decimal? Forecast1RatePerUnit { get; set; }
        public decimal? Forecast1Units { get; set; }
        public decimal? Forecast2RatePerUnit { get; set; }
        public decimal? Forecast2Units { get; set; }
        public decimal? Forecast3RatePerUnit { get; set; }
        public decimal? Forecast3Units { get; set; }
        public decimal? Forecast4RatePerUnit { get; set; }
        public decimal? Forecast4Units { get; set; }
        public decimal? Forecast5RatePerUnit { get; set; }
        public decimal? Forecast5Units { get; set; }
    }

    public class ManagementAccountsDataDumps_Forecast
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ReportingCategoryID { get; set; }
        public int ReportingDescriptionID { get; set; }
        public string AccountNo { get; set; }
        public string Reference { get; set; }
        public DateTime Date { get; set; }
        public decimal ActualAmount { get; set; }
    }

    public class ManagementAccounts_ReportingCategory
    {
        [Key]
        public int ID { get; set; }
        public string ReportingCategory { get; set; }
        public int? FinancialCategoryID { get; set; }
        public string ChartType { get; set; }
        public string ChartColor { get; set; }
        public int? SortOrder { get; set; }

        public ManagementAccounts_ReportingCategory_FinancialCategoryEnum FinancialCategory
        {
            get
            {
                if (FinancialCategoryID.HasValue)
                    return (ManagementAccounts_ReportingCategory_FinancialCategoryEnum)FinancialCategoryID.Value;

                return ManagementAccounts_ReportingCategory_FinancialCategoryEnum.None;
            }
        }
    }

    public enum ManagementAccounts_ReportingCategory_FinancialCategoryEnum
    {
        [Description("None")]
        None = 0,
        [Description("Asset")]
        Asset = 1,
        [Description("Liability")]
        Liability = 2,
        [Description("Income")]
        Income = 3,
        [Description("Expense")]
        Expense = 4,
    }

    public enum ManagementAccounts_AmountTypeEnum
    {
        [Description("Actual")]
        Actual = 0,
        [Description("Forecast1")]
        Forecast1 = 1,
        [Description("Forecast2")]
        Forecast2 = 2,
        [Description("Forecast3")]
        Forecast3 = 3,
        [Description("Forecast4")]
        Forecast4 = 4,
        [Description("Forecast5")]
        Forecast5 = 5,
    }

    public class ManagementAccounts_ReportingDescription
    {
        [Key]
        public int ID { get; set; }
        public string ReportingDescription { get; set; }
        public int? FinancialCategoryID { get; set; }
        public int? ParentReportingDescriptionID { get; set; }
        public string ChartType { get; set; }
        public string ChartColor { get; set; }

        public ManagementAccounts_ReportingCategory_FinancialCategoryEnum FinancialCategory
        {
            get
            {
                if (FinancialCategoryID.HasValue)
                    return (ManagementAccounts_ReportingCategory_FinancialCategoryEnum)FinancialCategoryID.Value;

                return ManagementAccounts_ReportingCategory_FinancialCategoryEnum.None;
            }
        }
    }

    public class ManagementAccounts_ReportingParentDescription
    {
        [Key]
        public int ID { get; set; }
        public string ReportingParentDescription { get; set; }
        public string ChartType { get; set; }
        public string ChartColor { get; set; }
    }
    public class F_SystemGeneratedReports_ManagementAccounts_Request
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? CompanyID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal? Progress { get; set; }
        public int? SystemReportID { get; set; }
    }

}
