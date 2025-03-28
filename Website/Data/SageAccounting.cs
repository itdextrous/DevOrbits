using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class SageAccounting_Company
    {
        [Key]
        public int ID { get; set; }
        public int SageID { get; set; }
        public string Name { get; set; }
        public string? CurrencySymbol { get; set; }
        public int? CurrencyDecimalDigits { get; set; }
        public int? NumberDecimalDigits { get; set; }
        public string? DecimalSeparator { get; set; }
        public int? HoursDecimalDigits { get; set; }
        public int? ItemCostPriceDecimalDigits { get; set; }
        public int? ItemSellingPriceDecimalDigits { get; set; }
        public string? PostalAddress1 { get; set; }
        public string? PostalAddress2 { get; set; }
        public string? PostalAddress3 { get; set; }
        public string? PostalAddress4 { get; set; }
        public string? PostalAddress5 { get; set; }
        public string? GroupSeparator { get; set; }
        public int? RoundingValue { get; set; }
        public int? TaxSystem { get; set; }
        public int? RoundingType { get; set; }
        public bool? AgeMonthly { get; set; }
        public bool? DisplayInactiveItems { get; set; }
        public bool? WarnWhenItemCostIsZero { get; set; }
        public bool? DoNotAllowProcessingIntoNegativeQuantities { get; set; }
        public bool? WarnWhenItemQuantityIsZero { get; set; }
        public bool? WarnWhenItemSellingBelowCost { get; set; }
        public int? CountryId { get; set; }
        public bool? EnableCustomerZone { get; set; }
        public bool? EnableAutomaticBankFeedRefresh { get; set; }
        public string? ContactName { get; set; }
        public string? Telephone { get; set; }
        public string? Fax { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public bool? IsPrimarySendingEmail { get; set; }
        public string? PostalAddress01 { get; set; }
        public string? PostalAddress02 { get; set; }
        public string? PostalAddress03 { get; set; }
        public string? PostalAddress04 { get; set; }
        public string? PostalAddress05 { get; set; }
        public string? CompanyInfo01 { get; set; }
        public string? CompanyInfo02 { get; set; }
        public string? CompanyInfo03 { get; set; }
        public string? CompanyInfo04 { get; set; }
        public string? CompanyInfo05 { get; set; }
        public bool? IsOwner { get; set; }
        public bool? UseCCEmail { get; set; }
        public string? CCEmail { get; set; }
        public int? DateFormatId { get; set; }
        public bool? CheckForDuplicateCustomerReferences { get; set; }
        public bool? CheckForDuplicateSupplierReferences { get; set; }
        public string? DisplayName { get; set; }
        public bool? DisplayInactiveCustomers { get; set; }
        public bool? DisplayInactiveSuppliers { get; set; }
        public bool? DisplayInactiveTimeProjects { get; set; }
        public bool? UseInclusiveProcessingByDefault { get; set; }
        public bool? LockProcessing { get; set; }
        public bool? LockTimesheetProcessing { get; set; }
        public int? TaxPeriodFrequency { get; set; }
        public bool? UseNoreplyEmail { get; set; }
        public bool? AgeingBasedOnDueDate { get; set; }
        public bool? UseLogoOnEmails { get; set; }
        public bool? UseLogoOnCustomerZone { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
        public bool? Active { get; set; }
        public string? TaxNumber { get; set; }
        public string? RegisteredName { get; set; }
        public string? RegistrationNumber { get; set; }
        public bool? IsPracticeAccount { get; set; }
        public int? LogoPositionID { get; set; }
        public string? CompanyTaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? CustomerZoneGuid { get; set; }
        public int? ClientTypeId { get; set; }
        public int? DisplayTotalTypeId { get; set; }
        public bool? DisplayInCompanyConsole { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int? TaxReportingTypeId { get; set; }
        public bool? SalesOrdersReserveItemQuantities { get; set; }
        public bool? PrescribedGoodsTrader { get; set; }
        public bool? DisplayInactiveItemBundles { get; set; }
        public int? CompanyTransferStatus { get; set; }
        public int? InventoryTypeId { get; set; }
    }
    public class SageAccounting_AccountCategory
    {
        [Key]
        public int ID { get; set; }
        public int SageID { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public int? Order { get; set; }
        public bool? IsBalanceSheet { get; set; }
    }
    public class SageAccounting_AccountTaxType
    {
        [Key]
        public int ID { get; set; }
        public int? SageID { get; set; }
        public string? Name { get; set; }
        public decimal? Percentage { get; set; }
        public bool? IsDefault { get; set; }
        public bool? HasActivity { get; set; }
        public bool? IsManualTax { get; set; }
        public bool? Active { get; set; }
        public DateTime? Created { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? Modified { get; set; }
        public string? TaxTypeDefaultUID { get; set; }
    }
    public class SageAccounting_Account
    {
        [Key]
        public int ID { get; set; }
        public int? CompanyId { get; set; }
        public int? SageID { get; set; }
        public string? Name { get; set; }
        public int? Category { get; set; }
        public bool? Active { get; set; }
        public decimal? Balance { get; set; }
        public string? Description { get; set; }
        public bool? UnallocatedAccount { get; set; }
        public bool? IsTaxLocked { get; set; }
        public DateTime? Created { get; set; }
        public int? AccountType { get; set; }
        public bool? HasActivity { get; set; }
        public int? DefaultTaxTypeId { get; set; }
        public int? DefaultTaxType { get; set; }
        public int? ReportingParentDescriptionID { get; set; }
    }
    public class SageAccounting_DetailedLedgerTransaction
    {
        [Key]
        public int ID { get; set; }
        public long? SageID { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? Date { get; set; }
        public int? TransactionTypeId { get; set; }
        public string? Reference { get; set; }
        public string? Description { get; set; }
        public int? AccountId { get; set; }
        public string? AccountDescription { get; set; }
        public int? ContraAccountId { get; set; }
        public string? ContraAccountDescription { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public int? TaxTypeId { get; set; }
        public string? TaxTypeName { get; set; }
        public DateTime? Modified { get; set; }
        public int? AccountCategoryId { get; set; }
        public string? AccountCategoryDescription { get; set; }
        public string? TransactionTypeDescription { get; set; }
        public int? AnalysisCategoryId1 { get; set; }
        public int? AnalysisCategoryId2 { get; set; }
        public int? AnalysisCategoryId3 { get; set; }
    }
    public class SageAccounting_JournalLog
    {
        [Key]
        public int ID { get; set; }
        public long? SageID { get; set; }
        public decimal? Amount { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? AccountingChecklistID { get; set; }
        public int? ResponseCode { get; set; }
    }
    //public class SageAccounting_JournalRequest
    //{
    //    [Key]
    //    public int ID { get; set; }
    //    public DateTime DateCreated { get; set; }
    //    public DateTime? DateStarted { get; set; }
    //    public DateTime? DateEnded { get; set; }
    //}
    public class SageAccounting_JournalRequests_AccountingChecklistID
    {
        [Key]
        public int ID { get; set; }
        public int AccountChecklistID { get; set; }
        public int SageAccounting_JournalRequestsID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
    }
}
