using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data.C05_MonthlyManualInvoicing;
using MyVoltage.Data.Tariffs;
using MyVoltage.Models;

namespace MyVoltage.Data
{
    public class MyVoltageDbContext : IdentityDbContext<ApplicationUser>
    {
        public MyVoltageDbContext(DbContextOptions<MyVoltageDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            builder.Entity<DeviceSteppedTarrif>().HasKey(c => new { c.DeviceIDLinked, c.SteppedTarrifID });
            foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                // EF Core 1 & 2
                //property.Relational().ColumnType = "decimal(18, 6)";

                // EF Core 3
                //property.SetColumnType("decimal(18, 6)");

                // EF Core 5
                property.SetPrecision(18);
                property.SetScale(6);
            }
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomersDetail> CustomersDetails { get; set; }
        public DbSet<CustomersDetails_Attachment> CustomersDetails_Attachments { get; set; }
        public DbSet<CustomersDetails_Attachments_Comment> CustomersDetails_Attachments_Comments { get; set; }
        public DbSet<CustomerMeterType> CustomerMeterTypes { get; set; }
        public DbSet<CustomerMeter> CustomerMeters { get; set; }
        public DbSet<Customer_AppNotification> Customer_AppNotifications { get; set; }
        public DbSet<MeterType> MeterTypes { get; set; }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Company_FinancialDetail> Company_FinancialDetails { get; set; }
        public DbSet<Company_TechnicalDetail> Company_TechnicalDetails { get; set; }
        public DbSet<Company_Log> Company_Logs { get; set; }
        public DbSet<Company_BlockedMeterExclusion> Company_BlockedMeterExclusions { get; set; }
        public DbSet<CompanyType> CompanyTypes { get; set; }
        public DbSet<Companies_OperationalBalance> Companies_OperationalBalances { get; set; }

        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<PaymentMethods_SkybillJournalNo> PaymentMethods_SkybillJournalNos { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentStatus> PaymentStatuses { get; set; }
        public DbSet<NetcashStatement> NetcashStatements { get; set; }
        public DbSet<NetcashManualPayment> NetcashManualPayments { get; set; }
        public DbSet<NetcashManualPaymentRule> NetcashManualPaymentRules { get; set; }
        public DbSet<CompanySkins> CompanySkins { get; set; }
        public DbSet<UniPin> UniPins { get; set; }
        public DbSet<PaymentRawData> PaymentRawData { get; set; }
        public DbSet<NotificationCustomerMeter> NotificationCustomerMeters { get; set; }

        public DbSet<SkybillCustomer> SkybillCustomers { get; set; }
        public DbSet<SkybillResourceList> SkybillResourceLists { get; set; }
        public DbSet<SkybillResourceLedgerEntry> SkybillResourceLedgerEntries { get; set; }
        public DbSet<SkybillCustomer_Temp> SkybillCustomers_Temp { get; set; }
        public DbSet<SkybillJournalLog> SkybillJournalLogs { get; set; }
        public DbSet<SkybillCustomersUtility> SkybillCustomersUtilities { get; set; }
        public DbSet<ChartOfAccountsSnapshot> ChartOfAccountsSnapshots { get; set; }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceRentalFee> DeviceRentalFees { get; set; }
        public DbSet<Gateway> Gateways { get; set; }
        public DbSet<GatewayRentalFee> GatewayRentalFees { get; set; }
        public DbSet<Site> Sites { get; set; }

        public DbSet<RentalDataDump> RentalDataDumps { get; set; }
        public DbSet<Rental_Expense> Rental_Expenses { get; set; }
        public DbSet<Rental_ExpenseName> Rental_ExpenseNames { get; set; }
        public DbSet<Rental_ExpenseType> Rental_ExpenseTypes { get; set; }

        public DbSet<DetectAndMove> DetectAndMoves { get; set; }
        public DbSet<SignalOptimizer> SignalOptimizers { get; set; }
        public DbSet<APIKEY> APIKEYS { get; set; }
        public DbSet<BuildingCouncilReconReportConfig> BuildingCouncilReconReportConfigs { get; set; }
        public DbSet<ExternalChargesSchedulingImport> ExternalChargesSchedulingImports { get; set; }
        public DbSet<Log_Notification> Log_Notifications { get; set; }
        public DbSet<MeterConnectionStatusChange> MeterConnectionStatusChanges { get; set; }
        public DbSet<Log_ProfileChange> Log_ProfileChanges { get; set; }
        public DbSet<Log_TokenGeneration> Log_TokenGenerations { get; set; }
        public DbSet<Log_Connection> Log_Connections { get; set; }
        public DbSet<BuildingDetail> BuildingDetails { get; set; }
        public DbSet<BuildingCouncilDetail> BuildingCouncilDetails { get; set; }
        public DbSet<BuildingCouncilMeter> BuildingCouncilMeters { get; set; }
        public DbSet<BuildingCaretaker> BuildingCaretakers { get; set; }
        public DbSet<BuildingCouncilType> BuildingCouncilTypes { get; set; }
        public DbSet<BuildingCouncilInvoice> BuildingCouncilInvoices { get; set; }
        public DbSet<BuildingCycle> BuildingCycles { get; set; }
        public DbSet<BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public DbSet<BuildingCouncilInvoiceChargeType> BuildingCouncilInvoiceChargeTypes { get; set; }
        public DbSet<BuildingCouncilInvoiceReadingType> BuildingCouncilInvoiceReadingTypes { get; set; }
        public DbSet<BuildingCouncilDetails_Invoice> BuildingCouncilDetails_Invoices { get; set; }
        public DbSet<BuildingCouncilDetails_InvoiceItem> BuildingCouncilDetails_InvoiceItems { get; set; }
        public DbSet<BuildingCouncilDetails_InvoiceItem_Month> BuildingCouncilDetails_InvoiceItem_Months { get; set; }
        public DbSet<B02_CouncilReadings_CouncilReadingUpdate> B02_CouncilReadings_CouncilReadingUpdates { get; set; }
        public DbSet<Log_GatewayReset> Log_GatewayResets { get; set; }
        public DbSet<Log_ClearTamper> Log_ClearTampers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Log_CreatedDevice> Log_CreatedDevices { get; set; }
        public DbSet<PQAllocation> PQAllocations { get; set; }
        public DbSet<PQAllocationCustomer> PQAllocationCustomers { get; set; }
        public DbSet<UsageCalc_Asset> UsageCalc_Assets { get; set; }
        public DbSet<UsageCalc> UsageCalcs { get; set; }
        public DbSet<UsageCalc_LinkedAsset> UsageCalc_LinkedAssets { get; set; }
        public DbSet<ParentSecureArea> ParentSecureAreas { get; set; }
        public DbSet<SecureArea> SecureAreas { get; set; }
        public DbSet<SecureAreaAction> SecureAreaActions { get; set; }
        public DbSet<UserSecureAreaAction> UserSecureAreaActions { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<UserMeterSerial> UserMeterSerials { get; set; }
        public DbSet<OperationalProfile> OperationalProfiles { get; set; }
        public DbSet<Log_BillingControlReport_OccupancyVerification> Log_BillingControlReport_OccupancyVerifications { get; set; }
        public DbSet<DeviceBillingDaily_Item> DeviceBillingDaily { get; set; }
        public DbSet<Log_BillingControlReport_NotBilledVerification> Log_BillingControlReport_NotBilledVerifications { get; set; }
        public DbSet<Log_DevicesSkybillBillingSync> Log_DevicesSkybillBillingSyncs { get; set; }
        public DbSet<Log_DevicesSkybillBillingSyncCompanyDevice> Log_DevicesSkybillBillingSyncCompanyDevices { get; set; }
        public DbSet<Log_DevicesSkybillBillingSyncCompany> Log_DevicesSkybillBillingSyncCompanies { get; set; }

        public DbSet<Tariffs.DeviceSteppedTarrif> DeviceSteppedTarrifs { get; set; }
        public DbSet<Tariffs.SteppedTarrif> SteppedTarrifs { get; set; }

        public DbSet<A02_MirrorMeterAuditing_MirrorReadingUpdate> A02_MirrorMeterAuditing_MirrorReadingUpdates { get; set; }
        public DbSet<A02_MirrorMeterAuditing_MeterCalibrationVerification> A02_MirrorMeterAuditing_MeterCalibrationVerifications { get; set; }
        public DbSet<A08_AccountPayments_Payment> A08_AccountPayments_Payments { get; set; }
        public DbSet<A03_NetworkBalancing_Capture> A03_NetworkBalancing_Captures { get; set; }
        public DbSet<A03_NetworkBalancing_Capture_ReportType> A03_NetworkBalancing_Capture_ReportTypes { get; set; }
        public DbSet<A07_CreditControlAndNotifierProcess_MeterOnManualRequest> A07_CreditControlAndNotifierProcess_MeterOnManualRequests { get; set; }
        public DbSet<C05_MonthlyManualInvoicing_BillingsToOwner_Capture> C05_MonthlyManualInvoicing_BillingsToOwner_Captures { get; set; }

        public DbSet<SystemGeneratedReport> SystemGeneratedReports { get; set; }
        public DbSet<F_SystemGeneratedReports_Detail> F_SystemGeneratedReports_Details { get; set; }
        public DbSet<SystemGeneratedReports_MeterActiveEnergyAnomalie> SystemGeneratedReports_MeterActiveEnergyAnomalies { get; set; }
        public DbSet<SystemGeneratedReports_Company> SystemGeneratedReports_Companies { get; set; }
        public DbSet<F_SystemGeneratedReports_AccountingChecklist_Request> F_SystemGeneratedReports_AccountingChecklist_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_NetcashServicesChargesRecon_Request> F_SystemGeneratedReports_NetcashServicesChargesRecon_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_GenLedgerSync_Request> F_SystemGeneratedReports_GenLedgerSync_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_GenLedgerSyncFULL_Request> F_SystemGeneratedReports_GenLedgerSyncFULL_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Request> F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Request> F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_DirectDepositsAllocation_Request> F_SystemGeneratedReports_DirectDepositsAllocation_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_SQLJobs_CallLogSync_Request> F_SystemGeneratedReports_SQLJobs_CallLogSync_Requests { get; set; }
        public DbSet<F_SystemGeneratedReports_SageAccounting_JournalRequest> F_SystemGeneratedReports_SageAccounting_JournalRequests { get; set; }
        public DbSet<F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequest> F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedRequests { get; set; }

        public DbSet<Zendesk_User> Zendesk_Users { get; set; }
        public DbSet<Zendesk_Ticket> Zendesk_Tickets { get; set; }
        public DbSet<Zendesk_UserField> Zendesk_UserFields { get; set; }
        public DbSet<Zendesk_OrganizationField> Zendesk_OrganizationFields { get; set; }
        public DbSet<Zendesk_TicketField> Zendesk_TicketFields { get; set; }
        public DbSet<Zendesk_TicketField_Option> Zendesk_TicketField_Options { get; set; }

        public DbSet<A04_CallCentreLog> A04_CallCentreLogs { get; set; }
        public DbSet<A04_CallCentreLogs_Recording> A04_CallCentreLogs_Recordings { get; set; }

        public DbSet<A09_Flags.A09_Flag> A09_Flags { get; set; }
        public DbSet<A09_Flags.A09_Flags_ResponsiblePerson> A09_Flags_ResponsiblePersons { get; set; }
        public DbSet<A09_Flags.A09_Flags_Type> A09_Flags_Types { get; set; }
        public DbSet<A09_Flags.A09_Flags_ReassignLog> A09_Flags_ReassignLogs { get; set; }
        public DbSet<A09_Flags.A09_Flags_Types_SerialsToExclude> A09_Flags_Types_SerialsToExcludes { get; set; }
        public DbSet<A09_Flags.A09_Flags_Attachment> A09_Flags_Attachments { get; set; }
        public DbSet<A09_Flags.A09_Flag_Type_Log> A09_Flag_Type_Logs { get; set; }

        public DbSet<Company_CostSetting> Company_CostSettings { get; set; }
        public DbSet<Company_CostSetting_Item> Company_CostSetting_Items { get; set; }
        public DbSet<Company_CostSetting_Monthly> Company_CostSetting_Monthlies { get; set; }
        public DbSet<Company_CostSettings_Template> Company_CostSettings_Templates { get; set; }
        public DbSet<C08_Forecasting_CostSettings_Template> C08_Forecasting_CostSettings_Templates { get; set; }
        public DbSet<C08_Forecasting_Baseline> C08_Forecasting_Baselines { get; set; }

        public DbSet<SiteAdmin_Imports.SiteAdmin_Imports_RentalDataDump> SiteAdmin_Imports_RentalDataDumps { get; set; }
        public DbSet<SiteAdmin_Imports.SiteAdmin_Imports_Suburb> SiteAdmin_Imports_Suburbs { get; set; }
        public DbSet<SiteAdmin_Imports.SiteAdmin_Imports_RentalExpense> SiteAdmin_Imports_RentalExpenses { get; set; }
        public DbSet<SiteAdmin_LoginMessage> SiteAdmin_LoginMessages { get; set; }
        public DbSet<SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public DbSet<SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public DbSet<SiteAdmin_Priority> SiteAdmin_Priorities { get; set; }
        public DbSet<SiteAdmin_DeviceAPI> SiteAdmin_DeviceAPIs { get; set; }
        public DbSet<SiteAdmin_DeviceAPIs_CustomURL> SiteAdmin_DeviceAPIs_CustomURLs { get; set; }
        public DbSet<SiteAdmin_Municipality> SiteAdmin_Municipalities { get; set; }
        public DbSet<SiteAdmin_Suburb> SiteAdmin_Suburbs { get; set; }
        public DbSet<SiteAdmin_Town> SiteAdmin_Towns { get; set; }
        public DbSet<SiteAdmin_LegalEntity> SiteAdmin_LegalEntities { get; set; }
        public DbSet<SiteAdmin_DeviceType> SiteAdmin_DeviceTypes { get; set; }

        public DbSet<SiteAdmin_Imports.SiteAdmin_Imports_ManagementAccountsDataDump> SiteAdmin_Imports_ManagementAccountsDataDumps { get; set; }
        public DbSet<ManagementAccountsDataDump> ManagementAccountsDataDumps { get; set; }
        public DbSet<ManagementAccountsDataDumps_Forecast> ManagementAccountsDataDumps_Forecasts { get; set; }
        public DbSet<ManagementAccounts_ReportingCategory> ManagementAccounts_ReportingCategories { get; set; }
        public DbSet<ManagementAccounts_ReportingDescription> ManagementAccounts_ReportingDescriptions { get; set; }
        public DbSet<ManagementAccounts_ReportingParentDescription> ManagementAccounts_ReportingParentDescriptions { get; set; }
        public DbSet<F_SystemGeneratedReports_ManagementAccounts_Request> F_SystemGeneratedReports_ManagementAccounts_Requests { get; set; }

        public DbSet<SiteAdmin_StatusGroup> SiteAdmin_StatusGroups { get; set; }
        public DbSet<SiteAdmin_StatusAction> SiteAdmin_StatusActions { get; set; }
        public DbSet<SiteAdmin_StatusReporting> SiteAdmin_StatusReportings { get; set; }
        public DbSet<SiteAdmin_Status> SiteAdmin_Statuses { get; set; }
        public DbSet<SiteAdmin_ContactorStateHack> SiteAdmin_ContactorStateHacks { get; set; }

        public DbSet<SiteAdmin_MeetingAgendaGroup> SiteAdmin_MeetingAgendaGroups { get; set; }
        public DbSet<SiteAdmin_MeetingAgendaAction> SiteAdmin_MeetingAgendaActions { get; set; }
        public DbSet<SiteAdmin_MeetingAgendaReporting> SiteAdmin_MeetingAgendaReportings { get; set; }
        public DbSet<SiteAdmin_MeetingAgenda> SiteAdmin_MeetingAgendas { get; set; }

        public DbSet<Report_ProductsResourceLedgerMonthly> Report_ProductsResourceLedgerMonthlies { get; set; }
        public DbSet<Report_SupplyCostMonthly> Report_SupplyCostMonthlies { get; set; }
        public DbSet<Report_GeneralLedgerMonthly> Report_GeneralLedgerMonthlies { get; set; }
        public DbSet<Report_ProductsResourceLedgerCustomerMonthly> Report_ProductsResourceLedgerCustomerMonthlies { get; set; }
        public DbSet<Report_SageLedgerMonthly> Report_SageLedgerMonthlies { get; set; }

        public DbSet<Log_UserActivity> Log_UserActivities { get; set; }

        public DbSet<ConnectionRun> ConnectionRuns { get; set; }
        public DbSet<ConnectionRun_Customer> ConnectionRun_Customers { get; set; }
        public DbSet<GeneralLedgerEntry> GeneralLedgerEntries { get; set; }

        public DbSet<A08_Task> A08_Tasks { get; set; }
        public DbSet<A08_Task_Type> A08_Task_Types { get; set; }
        public DbSet<A08_Task_Type_Frequency> A08_Task_Type_Frequencies { get; set; }
        public DbSet<A08_Task_Type_Company> A08_Task_Type_Companies { get; set; }
        public DbSet<A08_Tasks_ReassignLog> A08_Tasks_ReassignLogs { get; set; }
        public DbSet<A08_Tasks_Attachment> A08_Tasks_Attachments { get; set; }
        public DbSet<A08_Task_Type_Log> A08_Task_Type_Logs { get; set; }

        public DbSet<D01_Leads_Import> D01_Leads_Imports { get; set; }
        public DbSet<D01_Lead> D01_Leads { get; set; }
        public DbSet<D01_Leads_Log> D01_Leads_Logs { get; set; }
        public DbSet<D01_Lead_Status_Log> D01_Lead_Status_Logs { get; set; }
        public DbSet<D01_Leads_Attachment> D01_Leads_Attachments { get; set; }
        public DbSet<D01_Leads_Status> D01_Leads_Statuses { get; set; }
        public DbSet<D01_Leads_Property> D01_Leads_Properties { get; set; }
        public DbSet<D01_Leads_Contact> D01_Leads_Contacts { get; set; }
        public DbSet<D01_LeadGenerator> D01_LeadGenerators { get; set; }
        public DbSet<D01_LeadGeneratorUser> D01_LeadGeneratorUsers { get; set; }
        public DbSet<D01_Property> D01_Properties { get; set; }
        public DbSet<D01_Property_Log> D01_Property_Logs { get; set; }
        public DbSet<D01_Property_Status_Log> D01_Property_Status_Logs { get; set; }
        public DbSet<D01_Properties_Status> D01_Properties_Statuses { get; set; }
        public DbSet<D01_Contact> D01_Contacts { get; set; }
        public DbSet<D01_Contact_Log> D01_Contact_Logs { get; set; }
        public DbSet<D01_Contact_Status_Log> D01_Contact_Status_Logs { get; set; }
        public DbSet<D01_Contacts_Status> D01_Contacts_Statuses { get; set; }
        public DbSet<D01_Properties_Contact> D01_Properties_Contacts { get; set; }
        public DbSet<D01_Contacts_ManagingAgent> D01_Contacts_ManagingAgents { get; set; }
        public DbSet<D01_Properties_ManagingAgent> D01_Properties_ManagingAgents { get; set; }
        public DbSet<D01_Product> D01_Products { get; set; }
        public DbSet<D01_Service> D01_Services { get; set; }
        public DbSet<D01_ManagingAgent> D01_ManagingAgents { get; set; }
        public DbSet<D01_ManagingAgent_Log> D01_ManagingAgent_Logs { get; set; }
        public DbSet<D01_ManagingAgent_Status_Log> D01_ManagingAgent_Status_Logs { get; set; }
        public DbSet<D01_ManagingAgents_Status> D01_ManagingAgents_Statuses { get; set; }
        public DbSet<D01_Competitor> D01_Competitors { get; set; }
        public DbSet<D01_Competitor_Log> D01_Competitor_Logs { get; set; }
        public DbSet<D01_Competitor_Status_Log> D01_Competitor_Status_Logs { get; set; }
        public DbSet<D01_Competitors_Status> D01_Competitors_Statuses { get; set; }
        public DbSet<D01_Contacts_Competitor> D01_Contacts_Competitors { get; set; }
        public DbSet<D01_Properties_Competitor> D01_Properties_Competitors { get; set; }
        public DbSet<D01_ManagingAgents_Competitor> D01_ManagingAgents_Competitors { get; set; }
        public DbSet<D01_Competitors_Attachment> D01_Competitors_Attachments { get; set; }
        public DbSet<D01_Leads_Commission> D01_Leads_Commissions { get; set; }
        public DbSet<D01_Leads_Commissions_Payable> D01_Leads_Commissions_Payables { get; set; }
        public DbSet<D01_Leads_Commissions_Target> D01_Leads_Commissions_Targets { get; set; }
        public DbSet<D01_Snapshot> D01_Snapshots { get; set; }
        public DbSet<D01_Properties_Snapshot> D01_Properties_Snapshots { get; set; }
        public DbSet<D01_Contacts_Snapshot> D01_Contacts_Snapshots { get; set; }
        public DbSet<D01_ManagingAgents_Snapshot> D01_ManagingAgents_Snapshots { get; set; }

        public DbSet<V01_Policy> V01_Policies { get; set; }
        public DbSet<V01_Policies_ResponsibleUser> V01_Policies_ResponsibleUsers { get; set; }
        public DbSet<V01_Policies_Attachment> V01_Policies_Attachments { get; set; }
        public DbSet<V01_PoliciesLog> V01_PoliciesLogs { get; set; }

        public DbSet<BuildingOnboardingQuestion> BuildingOnboardingQuestions { get; set; }
        public DbSet<BuildingOnboardingAnswer> BuildingOnboardingAnswers { get; set; }
        public DbSet<BuildingOnboardingLog> BuildingOnboardingLogs { get; set; }
        public DbSet<BuildingOnboardingQuestions_Company> BuildingOnboardingQuestions_Companies { get; set; }

        public DbSet<ReportedBug> ReportedBugs { get; set; }
        public DbSet<ReportedBugs_Log> ReportedBugs_Logs { get; set; }
        public DbSet<ReportedBugs_Comment> ReportedBugs_Comments { get; set; }

        public DbSet<J_Finance_WinshuttleExport> J_Finance_WinshuttleExports { get; set; }
        public DbSet<J_Finance_WinshuttleExportItem> J_Finance_WinshuttleExportItems { get; set; }

        public DbSet<J_Finance_AllocationRerun_Log> J_Finance_AllocationRerun_Logs { get; set; }
        public DbSet<J_Finance_AllocationRerun_Log_Item> J_Finance_AllocationRerun_Log_Items { get; set; }
        public DbSet<J_Finance_AllocationRerun_Log_PaymentComplete> J_Finance_AllocationRerun_Log_PaymentCompletes { get; set; }

        public DbSet<SafetyFileQuestions_Company> SafetyFileQuestions_Companies { get; set; }
        public DbSet<SafetyFileQuestion> SafetyFileQuestions { get; set; }
        public DbSet<SafetyFileLog> SafetyFileLogs { get; set; }
        public DbSet<SafetyFileAnswer> SafetyFileAnswers { get; set; }

        public DbSet<MeterActiveEnergyAnomaly> MeterActiveEnergyAnomalies { get; set; }
        public DbSet<MeterActiveEnergyAnomaliesRun> MeterActiveEnergyAnomaliesRuns { get; set; }

        public DbSet<A10_VirtualMeter> A10_VirtualMeters { get; set; }
        public DbSet<A10_VirtualMeterCustomer> A10_VirtualMeterCustomers { get; set; }

        public DbSet<A10_MergedMeter> A10_MergedMeters { get; set; }
        public DbSet<A10_MergedMeterLinkedMeter> A10_MergedMeterLinkedMeters { get; set; }

        public DbSet<Module_TimeOfWorkPlanned> Module_TimeOfWorkPlanneds { get; set; }
        public DbSet<Module_TimeOfWorkAllocated> Module_TimeOfWorkAllocateds { get; set; }
        public DbSet<Module_TravelAllocation> Module_TravelAllocations { get; set; }
        public DbSet<Module_StockAllocation> Module_StockAllocations { get; set; }
        public DbSet<Module_InvoiceAllocation> Module_InvoiceAllocations { get; set; }
        public DbSet<Module_NonCompliance> Module_NonCompliances { get; set; }

        public DbSet<E01_BuildingOnboardingTask> E01_BuildingOnboardingTasks { get; set; }
        public DbSet<E01_BuildingOnboardingTask_Type> E01_BuildingOnboardingTask_Types { get; set; }
        public DbSet<E01_BuildingOnboardingTask_Type_Company> E01_BuildingOnboardingTask_Type_Companies { get; set; }
        public DbSet<E01_BuildingOnboardingTasks_ReassignLog> E01_BuildingOnboardingTasks_ReassignLogs { get; set; }
        public DbSet<E01_BuildingOnboardingTasks_Attachment> E01_BuildingOnboardingTasks_Attachments { get; set; }

        public DbSet<D02_SaleTask> D02_SaleTasks { get; set; }
        public DbSet<D02_SaleTask_Type> D02_SaleTask_Types { get; set; }
        public DbSet<D02_SaleTask_Type_Company> D02_SaleTask_Type_Companies { get; set; }
        public DbSet<D02_SaleTasks_ReassignLog> D02_SaleTasks_ReassignLogs { get; set; }
        public DbSet<D02_SaleTasks_Attachment> D02_SaleTasks_Attachments { get; set; }

        public DbSet<SOC_Snapshot> SOC_Snapshots { get; set; }
        public DbSet<SOC_SnapshotItem> SOC_SnapshotItems { get; set; }

        public DbSet<V03_PersonalDetail> V03_PersonalDetails { get; set; }
        public DbSet<V03_PersonalDetails_Log> V03_PersonalDetails_Logs { get; set; }
        public DbSet<V03_JobDescription> V03_JobDescriptions { get; set; }
        public DbSet<V03_JobDescriptions_Log> V03_JobDescriptions_Logs { get; set; }
        public DbSet<V03_Correspondence> V03_Correspondences { get; set; }

        public DbSet<WSCustomerLoginToken> WSCustomerLoginTokens { get; set; }

        public DbSet<AccountingChecklist> AccountingChecklists { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        public DbSet<WorkflowGroupGrandParent> WorkflowGroupGrandParents { get; set; }
        public DbSet<WorkflowGroupParent> WorkflowGroupParents { get; set; }
        public DbSet<WorkflowGroup> WorkflowGroups { get; set; }
        public DbSet<BusinessPillar> BusinessPillars { get; set; }
        public DbSet<BusinessDepartment> BusinessDepartments { get; set; }

        public DbSet<AF_EDI_CompanyDetail> AF_EDI_CompanyDetails { get; set; }
        public DbSet<AF_SnapshotEmail> AF_SnapshotEmails { get; set; }
        public DbSet<ACO_StatusHack> ACO_StatusHacks { get; set; }

        public DbSet<SageAccounting_Company> SageAccounting_Companies { get; set; }
        public DbSet<SageAccounting_AccountCategory> SageAccounting_AccountCategories { get; set; }
        public DbSet<SageAccounting_AccountTaxType> SageAccounting_AccountTaxTypes { get; set; }
        public DbSet<SageAccounting_Account> SageAccounting_Accounts { get; set; }
        public DbSet<SageAccounting_DetailedLedgerTransaction> SageAccounting_DetailedLedgerTransactions { get; set; }
        public DbSet<SageAccounting_JournalLog> SageAccounting_JournalLogs { get; set; }
        //public DbSet<SageAccounting_JournalRequest> SageAccounting_JournalRequests { get; set; }
        public DbSet<SageAccounting_JournalRequests_AccountingChecklistID> SageAccounting_JournalRequests_AccountingChecklistIDs { get; set; }
 
        public DbSet<SageManagementAccounts_ReportingParentDescription> SageManagementAccounts_ReportingParentDescriptions { get; set; }
   }
}
