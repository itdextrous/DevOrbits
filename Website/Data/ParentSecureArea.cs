using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class ParentSecureArea
    {
        [Key]
        public int ParentSecureAreaID { get; set; }
        public string ParentSecureAreaCodeName { get; set; }
        public string ParentSecureAreaDisplayName { get; set; }
        public string ParentSecureAreaDisplayIcon { get; set; }
    }

    public enum ParentSecureAreaEnum : int
    {
        [Description("Y00 - Site Admin")]
        SiteAdmin = 1,
        [Description("001 - Customer")]
        Customer = 2,
        [Description("A06 - Billing Control Report")]
        A06_BillingControlReport = 3,
        [Description("A02 - Mirror Meter Auditing")]
        A02_MirrorMeterAuditing = 4,
        [Description("")]
        A08_AccountPayments = 5,
        [Description("A03 - Network Balancing")]
        A03_NetworkBalancing = 6,
        [Description("A01 - GW & Device Monitoring")]
        A01_GatewayAndDeviceMonitoring = 7,
        [Description("A07 - Credit Control & Notifier Process")]
        A07_CreditControlAndNotifierProcess = 8,
        [Description("F00 - System Generated Reports")]
        F_SystemGeneratedReports = 9,
        [Description("A04.0 - Zendesk")]
        A04_Tickets = 10,
        [Description("A09 - Flags")]
        A09_Flags = 11,
        [Description("S00 - Combined Reports")]
        S_CombinedReports = 12,
        [Description("L00 - Meter Rentals")]
        L_MeterRentals = 13,
        [Description("C05 - Monthly Manual Invoicing")]
        C05_MonthlyManualInvoicing = 14,
        [Description("M00 - Technician Dispatch")]
        M_TechnicianDispatch = 15,
        [Description("N00 - Technician Toolkit")]
        N_TechnicianToolkit = 16,
        [Description("G00 - Communication")]
        G_Communication = 17,
        [Description("J00 - Finance")]
        J_Finance = 18,
        [Description("B05 - Supply Payments")]
        B05_SupplyPayments = 19,
        [Description("B01 - Supply Account Details")]
        B01_SupplyAccountPayments = 20,
        [Description("B03 - Supply Statements")]
        B03_SupplyCouncilStatements = 21,
        [Description("B02 - Council Readings")]
        B02_CouncilReadings = 22,
        [Description("B05 - Council Account Payments")]
        A08_CouncilAccountPayments = 23,
        [Description("B04 - Supply Reconciliation")]
        B04_SupplyReconciliation = 24,
        [Description("C01 - Product Report")]
        C01_ProductReport = 25,
        [Description("C06 - Ledger Recon Report")]
        C06_LedgerReconReport = 26,
        [Description("C04 - Operational Profit Report")]
        C04_OperationalProfitReport = 27,
        [Description("H00 - Device Administrator")]
        H_Device_Administrator = 28,
        [Description("002 - Dashboards")]
        Dashboards = 29,
        [Description("A05 - Exceptions")]
        A05_Exceptions = 30,
        [Description("Z - System Logs")]
        Z_SystemLogs = 31,
        [Description("C02.0 - General Ledger Report")]
        C02_GeneralLedgerReport = 32,
        [Description("A08 - Tasks")]
        A08_Tasks = 33,
        [Description("D01 - Leads")]
        D01_Leads = 34,
        [Description("V01 - Policies")]
        V01_Policies = 35,
        [Description("E01 - Building Onboarding")]
        E01_BuildingOnboarding = 36,
        [Description("Z - Bugs")]
        Z_Bugs = 37,
        [Description("Afrox Administration")]
        AF_AfroxAdministration = 38,
        [Description("A10 - Virtual Meters")]
        A10_VirtualMeters = 39,
        [Description("000 - SOC")]
        SOC = 40,
        [Description("C03 - Management Reporting")]
        C03_Report = 41,
        [Description("V02 - Workflow Allocations")]
        V02_Workflow_Allocations = 42,
        [Description("V03 -  Human Resources")]
        V03_HumanResources = 43,
        [Description("V04 -  Internal Meetings")]
        V04_InternalMeetings = 44,
        [Description("W01 -  Activity Logs")]
        W01_ActivityLogs = 45,
        [Description("C07 - Management Accounts")]
        C07_ManagementAccounts = 46,
        [Description("S02 - Product Combined Reports")]
        S02_ProductCombinedReports = 47,
        [Description("X - MeterTeam Admin")]
        X_MeterTeamAdmin = 48,
        [Description("C08 - Forecasting")]
        C08_Forecasting = 49,
        [Description("C09 - Cashflow Forecast")]
        C09_CashflowForecast = 50,
        [Description("D02 - Sales")]
        D02_Sale = 51,
        [Description("E03 - Commissioning")]
        E03_Commissioning = 52,
        [Description("A04.1 - Call Centre")]
        A04_CallCentre = 53,
        [Description("Y01 - User Administration")]
        Y01_UserAdmin = 54,
        [Description("C02.1 - Sage Ledger Report")]
        C02_SageLedgerReport = 56,
    }

    public enum SecureAreaEnum : int
    {
        [Description("User Admin")]
        UserAdmin = 1,
        [Description("Dashboard")]
        Customer_Dashboard = 2,
        [Description("Map")]
        Customer_Map = 3,
        [Description("Billing")]
        Customer_Billing = 4,

        [Description("A6.011 Occupancy Summary")]
        A06_BillingControlReport_OccupancySummary = 5,
        [Description("A6.012 Occupancy Details")]
        A06_BillingControlReport_OccupancyDetails = 6,
        [Description("A6.013 Occupancy Verification")]
        A06_BillingControlReport_OccupancyVerification = 7,
        [Description("A6.014 Occupancy Results")]
        A06_BillingControlReport_OccupancyResults = 8,
        [Description("A6.031 Not Billed Summary")]
        A06_BillingControlReport_NotBilledSummary = 9,
        [Description("A6.032 Not Billed Details")]
        A06_BillingControlReport_NotBilledDetails = 10,
        [Description("A6.033 Not Billed Verification")]
        A06_BillingControlReport_NotBilledVerification = 11,
        [Description("A6.034 Not Billed Results")]
        A06_BillingControlReport_NotBilledResults = 12,

        [Description("Usage")]
        Customer_Usage = 13,
        [Description("Report")]
        Customer_Statement = 14,

        [Description("A2.021 Mirror Reading Update")]
        A02_MirrorMeterAuditing_MirrorReadingUpdate = 15,
        [Description("A2.025 Mirror Reading Results")]
        A02_MirrorMeterAuditing_MirrorReadingResults = 16,
        [Description("A2.022 Mirror Reading Summary")]
        A02_MirrorMeterAuditing_MirrorReadingSummary = 17,
        [Description("A2.023 Mirror Reading Details")]
        A02_MirrorMeterAuditing_MirrorReadingDetails = 18,
        [Description("A2.024 Mirror Reading Verification")]
        A02_MirrorMeterAuditing_MirrorReadingVerification = 19,
        [Description("A2.014 Meter Calibration Results")]
        A02_MirrorMeterAuditing_MeterCalibrationResults = 20,
        [Description("A2.011 Meter Calibration Summary")]
        A02_MirrorMeterAuditing_MeterCalibrationSummary = 21,
        [Description("A2.012 Meter Calibration Details")]
        A02_MirrorMeterAuditing_MeterCalibrationDetails = 22,
        [Description("A2.013 Meter Calibration Verification")]
        A02_MirrorMeterAuditing_MeterCalibrationVerification = 23,

        [Description("B1.012 Account Details")]
        B01_AccountPayments_AccountPaymentDetails = 24,
        [Description("B1.013 Account Capture")]
        B01_AccountPayments_AccountPaymentCapture = 28,
        [Description("B3.011 Council Statement Summary")]
        B03_SupplyCouncilStatements_CouncilStatementSummary = 25,
        [Description("B3.012 Council Statement Details")]
        B03_SupplyCouncilStatements_CouncilStatementDetails = 26,
        [Description("B3.014 Council Statement Capture")]
        B03_SupplyCouncilStatements_CouncilStatementCapture = 27,

        [Description("B5.011 Payment Summary")]
        B05_AccountPayments_PaymentSummary = 29,
        [Description("B5.012 Payment Details")]
        B05_AccountPayments_PaymentDetails = 30,
        [Description("B5.013 Payment Capture")]
        B05_AccountPayments_PaymentCapture = 31,

        [Description("A3.011 Network balancing Summary")]
        A03_NetworkBalancing_Summary = 32,
        [Description("A3.012 Network balancing Details")]
        A03_NetworkBalancing_Details = 33,
        [Description("A3.013 Network balancing Capture")]
        A03_NetworkBalancing_Capture = 34,

        [Description("A1.011 Gateway Summary")]
        A01_GatewayAndDeviceMonitoring_GatewaySummary = 35,
        [Description("A1.012 Gateway Details")]
        A01_GatewayAndDeviceMonitoring_GatewayDetails = 36,
        [Description("")]
        A01_GatewayAndDeviceMonitoring_GatewayVerification = 37,
        [Description("A1.014 Gateway All linked")]
        A01_GatewayAndDeviceMonitoring_GatewayResults = 38,
        [Description("A1.021 Device Summary")]
        A01_GatewayAndDeviceMonitoring_DeviceSummary = 39,
        [Description("A1.022 Device Details")]
        A01_GatewayAndDeviceMonitoring_DeviceDetails = 40,
        [Description("")]
        A01_GatewayAndDeviceMonitoring_DeviceVerification = 41,
        [Description("A1.024 Device All linked")]
        A01_GatewayAndDeviceMonitoring_DeviceResults = 42,

        [Description("A7.021 - Credit Control Summary")]
        A07_CreditControlAndNotifierProcess_CreditControlSummary = 43,
        [Description("A7.022 - Credit Control Details")]
        A07_CreditControlAndNotifierProcess_CreditControlDetails = 44,
        [Description("A7.023 - Credit Control Review")]
        A07_CreditControlAndNotifierProcess_CreditControlReview = 45,
        [Description("A7.024 - Credit Control Results")]
        A07_CreditControlAndNotifierProcess_CreditControlResults = 46,

        [Description("FA5.011 Device Reading Midnight Sync")]
        F_SystemGeneratedReports_MidnightSync = 47,
        [Description("FA5.019 Device Reading Midnight Sync NEXT DAY")]
        F_SystemGeneratedReports_MidnightSyncNextDay = 48,
        [Description("FA5.012 Device Reading Midnight Sync Technical Loss")]
        F_SystemGeneratedReports_MidnightSyncTechLoss = 49,
        [Description("FA5.013 Device Reading Midnight Sync PQ Allocation")]
        F_SystemGeneratedReports_MidnightSyncPQAllocation = 50,
        [Description("FA1.011 Offline Gateways and Devices Report")]
        F_SystemGeneratedReports_OfflineGatewaysAndDevices = 51,
        [Description("FA1.012 Gateways and Devices Sync Completed")]
        F_SystemGeneratedReports_GatewaysAndDevicesSync = 52,
        [Description("FA2.011 Mirror Audit Report")]
        F_SystemGeneratedReports_MirrorAudit = 53,
        [Description("FA2.012 Device Reading Updater")]
        F_SystemGeneratedReports_DeviceReadingUpdater = 54,
        [Description("FA6.011 Billing Control Report")]
        F_SystemGeneratedReports_BillingControl = 55,
        [Description("FA6.012 Billing Control Detail Report")]
        F_SystemGeneratedReports_BillingControlDetail = 56,
        [Description("FA7.011 Credit Control Report")]
        F_SystemGeneratedReports_CreditControl = 57,
        [Description("FA7.012 Notifier And Disconnector")]
        F_SystemGeneratedReports_NotifierAndDisconnector = 58,
        [Description("FA7.013 Connector")]
        F_SystemGeneratedReports_Connector = 59,
        [Description("FA7.019 Receipt Report")]
        F_SystemGeneratedReports_ReceiptReport = 60,
        [Description("FB2.011 Stock Report")]
        F_SystemGeneratedReports_StockReport = 61,
        [Description("FC1.011 General Ledger Sync")]
        F_SystemGeneratedReports_GenLedgerSync = 62,
        [Description("FC2.011 Rental Book")]
        F_SystemGeneratedReports_RentalBook = 63,
        [Description("FC2.012 Rental Data Dump")]
        F_SystemGeneratedReports_RentalDataDump = 64,
        [Description("FC3.011 Devices Master Report")]
        F_SystemGeneratedReports_DevicesMaster = 65,
        [Description("FC3.012 Building Master Report")]
        F_SystemGeneratedReports_BuildingsMaster = 66,
        [Description("FA0 - Summary")]
        F_SystemGeneratedReports_AllReports = 67,

        [Description("A4.011 Zendesk Tickets Summary")]
        A04_Tickets_ZendeskTicketsSummary = 68,
        [Description("A4.012 Zendesk Tickets Details")]
        A04_Tickets_ZendeskTicketsDetails = 69,
        [Description("A4.013 Zendesk Tickets Review")]
        A04_Tickets_ZendeskTicketsReview = 70,
        [Description("A4.014 Zendesk Tickets Results")]
        A04_Tickets_ZendeskTicketsResults = 71,
        [Description("A4.015 Zendesk Tickets Incomplete - Active")]
        A04_Tickets_ZendeskTicketsIncomplete = 72,
        [Description("A4.015 Zendesk Tickets Incomplete - Closed")]
        A04_Tickets_ZendeskTicketsIncompleteClosed = 73,
        [Description("A4.021 Zendesk Agents Summary")]
        A04_Tickets_ZendeskAgentsSummary = 74,
        [Description("A4.022 Zendesk Agents Details")]
        A04_Tickets_ZendeskAgentsDetails = 75,
        [Description("A4.023 Zendesk Agents Results")]
        A04_Tickets_ZendeskAgentsResults = 76,

        [Description("FC3.013 Skybill Customers Sync")]
        F_SystemGeneratedReports_SkybillCustomersSync = 77,

        [Description("Recharge")]
        Customer_Recharge = 78,

        [Description("A9.011 Flags Company Summary")]
        A09_Flags_CompanySummary = 79,
        [Description("A9.012 Flags Company Details")]
        A09_Flags_CompanyDetails = 80,
        [Description("A9.040 Flag Review")]
        A09_Flags_CompanyReview = 81,
        [Description("A9.014 Flags Company Results")]
        A09_Flags_CompanyResults = 82,

        [Description("A9.021 Flags User Summary")]
        A09_Flags_UserSummary = 83,
        [Description("A9.022 Flags User Details")]
        A09_Flags_UserDetails = 84,
        [Description("")]
        A09_Flags_UserReview = 85,
        [Description("A9.024 Flags User Results")]
        A09_Flags_UserResults = 86,

        [Description("A09 Flag Types")]
        SiteAdmin_A09_FlagTypes = 87,
        [Description("FC3.015 Archiving Token Log")]
        F_SystemGeneratedReports_ArchivingJob_TokenLog = 88,
        [Description("FA9.011 Flag Log")]
        F_SystemGeneratedReports_A09FlagsDataDump = 89,

        [Description("A9.041 Flag Create")]
        A09_Flags_Create = 90,

        [Description("A9.031 Flags Type Summary")]
        A09_Flags_TypeSummary = 91,
        [Description("A9.032 Flags Type Details")]
        A09_Flags_TypeDetails = 92,
        [Description("A9.034 Flags Type Results")]
        A09_Flags_TypeResults = 93,

        [Description("A7.031 - Meter Mode Summary")]
        A07_CreditControlAndNotifierProcess_MeterModeSummary = 94,
        [Description("A7.032 - Meter Mode Details")]
        A07_CreditControlAndNotifierProcess_MeterModeDetails = 95,
        [Description("A7.033 - Meter Mode Review")]
        A07_CreditControlAndNotifierProcess_MeterModeReview = 96,
        [Description("")]
        A07_CreditControlAndNotifierProcess_MeterModeResults = 97,

        [Description("A7.041 - Meter On Manual Summary")]
        A07_CreditControlAndNotifierProcess_MeterOnManualSummary = 98,
        [Description("A7.042 - Meter On Manual Details")]
        A07_CreditControlAndNotifierProcess_MeterOnManualDetails = 99,
        [Description("")]
        A07_CreditControlAndNotifierProcess_MeterOnManualReview = 100,
        [Description("")]
        A07_CreditControlAndNotifierProcess_MeterOnManualResults = 101,

        [Description("A7.043 - Meter On Manual Request Summary")]
        A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary = 102,
        [Description("A7.044 - Meter On Manual Request Details")]
        A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails = 103,
        [Description("A7.045 - Meter On Manual Request Create")]
        A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview = 104,
        [Description("A7.046 - Meter On Manual Request Results")]
        A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults = 105,

        [Description("A6.041 Billing Blocked Summary")]
        A06_BillingControlReport_BillingBlockedSummary = 106,
        [Description("A6.042 Billing Blocked Details")]
        A06_BillingControlReport_BillingBlockedDetails = 107,

        [Description("A4.031 Zendesk Ticket Category Summary")]
        A04_Tickets_ZendeskTicketCategorySummary = 108,
        [Description("A4.032 Zendesk Ticket Category Details")]
        A04_Tickets_ZendeskTicketCategoryDetails = 109,
        [Description("A4.033 Zendesk Ticket Category Results")]
        A04_Tickets_ZendeskTicketCategoryResults = 110,

        [Description("S1.011 Billing Analysis Amount Summary")]
        S_CombinedReports_BillingAnalysis_Amount_Summary = 111, // Elec billing ZAR Values
        [Description("S1.012 Billing Analysis Amount Monthly")]
        S_CombinedReports_BillingAnalysis_Amount_Monthly = 112, // Elec billing ZAR Values
        [Description("S1.013 Billing Analysis Amount Daily")]
        S_CombinedReports_BillingAnalysis_Amount_Daily = 113, // Elec billing ZAR Values

        [Description("S1.021 Billing Analysis Units Summary")]
        S_CombinedReports_BillingAnalysis_Consumption_Summary = 114, // Elec billing kWh Values
        [Description("S1.022 Billing Analysis Units Monthly")]
        S_CombinedReports_BillingAnalysis_Consumption_Monthly = 115, // Elec billing kWh Values
        [Description("S1.023 Billing Analysis Units Daily")]
        S_CombinedReports_BillingAnalysis_Consumption_Daily = 116, // Elec billing kWh Values

        [Description("S1.031 Metered Analysis Units Summary")]
        S_CombinedReports_MeteredAnalysis_Units_Summary = 117, // Electricity - Metered
        [Description("S1.032 Metered Analysis Units Monthly")]
        S_CombinedReports_MeteredAnalysis_Units_Monthly = 118, // Electricity - Metered
        [Description("S1.033 Metered Analysis Units Daily")]
        S_CombinedReports_MeteredAnalysis_Units_Daily = 119, // Electricity - Metered

        [Description("S1.041 Unbilled Analysis Units Summary")]
        S_CombinedReports_UnbilledAnalysis_Units_Summary = 120, // m2m and billing sync diff Units
        [Description("S1.042 Unbilled Analysis Units Monthly")]
        S_CombinedReports_UnbilledAnalysis_Units_Monthly = 121, // m2m and billing sync diff Units
        [Description("S1.043 Unbilled Analysis Units Daily")]
        S_CombinedReports_UnbilledAnalysis_Units_Daily = 122, // m2m and billing sync diff Units

        [Description("")]
        SiteAdmin_Company_CostSettings = 123, // Cost price capturing

        [Description("S1.051 Cost Analysis Amount Summary")]
        S_CombinedReports_CostAnalysis_Amount_Summary = 124, // SiteAdmin_Company_CostSettings billing ZAR Values
        [Description("S1.052 Cost Analysis Amount Monthly")]
        S_CombinedReports_CostAnalysis_Amount_Monthly = 125, // SiteAdmin_Company_CostSettings billing ZAR Values
        [Description("S1.053 Cost Analysis Amount Daily")]
        S_CombinedReports_CostAnalysis_Amount_Daily = 126, // SiteAdmin_Company_CostSettings billing ZAR Values

        [Description("S1.061 Profit Analysis Amount Summary")]
        S_CombinedReports_ProfitAnalysis_Amount_Summary = 127, // Diff - S_CombinedReports_CostAnalysis_Amount - S_CombinedReports_BillingAnalysis_Amount
        [Description("S1.062 Profit Analysis Amount Monthly")]
        S_CombinedReports_ProfitAnalysis_Amount_Monthly = 128, // Diff - S_CombinedReports_CostAnalysis_Amount - S_CombinedReports_BillingAnalysis_Amount
        [Description("S1.063 Profit Analysis Amount Daily")]
        S_CombinedReports_ProfitAnalysis_Amount_Daily = 129, // Diff - S_CombinedReports_CostAnalysis_Amount - S_CombinedReports_BillingAnalysis_Amount

        [Description("S1.071 Profit Analysis Gross Profit % Summary")]
        S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary = 130, // S_CombinedReports_ProfitAnalysis_Amount / S_CombinedReports_BillingAnalysis_Amount
        [Description("S1.072 Profit Analysis Gross Profit % Monthly")]
        S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly = 131, // S_CombinedReports_ProfitAnalysis_Amount / S_CombinedReports_BillingAnalysis_Amount
        [Description("S1.073 Profit Analysis Gross Profit % Daily")]
        S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily = 132, // S_CombinedReports_ProfitAnalysis_Amount / S_CombinedReports_BillingAnalysis_Amount

        [Description("S0.011 Overall Analysis Summary")]
        S_CombinedReports_OverallAnalysis_Summary = 133,
        [Description("S0.012 Overall Analysis Monthly")]
        S_CombinedReports_OverallAnalysis_Monthly = 134,
        [Description("")]
        S_CombinedReports_OverallAnalysis_Daily = 135,

        [Description("S1.081 Network Balancing Analysis Units Summary")]
        S_CombinedReports_NetworkBalancingAnalysis_Units_Summary = 136,
        [Description("S1.082 Network Balancing Analysis Units Monthly")]
        S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly = 137,
        [Description("S1.083 Network Balancing Analysis Units Daily")]
        S_CombinedReports_NetworkBalancingAnalysis_Units_Daily = 138,

        [Description("L1.011 Meter Rentals Summary")]
        L_MeterRentals_Summary = 139,
        [Description("L1.012 Meter Rentals Details")]
        L_MeterRentals_Details = 142,
        [Description("")]
        L_MeterRentals_Review = 141,
        [Description("L1.013 Meter Rentals Results")]
        L_MeterRentals_Results = 140,

        [Description("L1.021 Meter Cost Summary")]
        L_MeterRentals_Cost_Summary = 143,
        [Description("L1.022 Device Cost Details")]
        L_MeterRentals_Cost_Details = 144,
        [Description("")]
        L_MeterRentals_Cost_Review = 145,
        [Description("L1.023 Device Cost Results")]
        L_MeterRentals_Cost_Results = 146,

        [Description("L1.024 Gateway Cost Details")]
        L_MeterRentals_Cost_Gateway_Details = 147,
        [Description("")]
        L_MeterRentals_Cost_Gateway_Review = 148,
        [Description("L1.025 Gateway Cost Results")]
        L_MeterRentals_Cost_Gateway_Results = 149,

        [Description("C5.011 Billings to Owner Summary")]
        C05_MonthlyManualInvoicing_BillingsToOwner_Summary = 150,
        [Description("C5.012 Billings to Owner Details")]
        C05_MonthlyManualInvoicing_BillingsToOwner_Details = 151,
        [Description("C5.013 Billings to Owner Capture")]
        C05_MonthlyManualInvoicing_BillingsToOwner_Capture = 152,
        [Description("")]
        C05_MonthlyManualInvoicing_BillingsToOwner_Results = 153,

        [Description("Imports - Rental Data Dump")]
        SiteAdmin_Imports_RentalDataDump = 154,

        [Description("L2.011 Meter Rentals Summary - Accounting")]
        L_MeterRentals_Accounting_Summary = 155,
        [Description("L2.012 Meter Rentals Details - Accounting")]
        L_MeterRentals_Accounting_Details = 156,
        [Description("")]
        L_MeterRentals_Accounting_Review = 157,
        [Description("L2.013 Meter Rentals Results - Accounting")]
        L_MeterRentals_Accounting_Results = 158,

        [Description("")]
        L_MeterRentals_Accounting_Cost_Summary = 159,
        [Description("L2.022 Device Cost Details - Accounting")]
        L_MeterRentals_Accounting_Cost_Details = 160,
        [Description("")]
        L_MeterRentals_Accounting_Cost_Review = 161,
        [Description("L2.023 Device Cost Results - Accounting")]
        L_MeterRentals_Accounting_Cost_Results = 162,

        [Description("L2.032 Gateway Cost Details - Accounting")]
        L_MeterRentals_Accounting_Cost_Gateway_Details = 163,
        [Description("")]
        L_MeterRentals_Accounting_Cost_Gateway_Review = 164,
        [Description("L2.033 Gateway Cost Results - Accounting")]
        L_MeterRentals_Accounting_Cost_Gateway_Results = 165,

        [Description("L2.021 Device Cost Summary - Accounting")]
        L_MeterRentals_Accounting_Cost_Device_Summary = 166,
        [Description("L2.031 Gateway Cost Summary - Accounting")]
        L_MeterRentals_Accounting_Cost_Gateway_Summary = 167,

        [Description("FD1.011 Meter Active Energy Anomalies")]
        F_SystemGeneratedReports_MeterActiveEnergyAnomalies = 168,

        [Description("TOU")]
        SiteAdmin_TOU = 169,

        [Description("M1.011 Vehicle Overview")]
        M_TechnicianDispatch_VehicleOverview = 170,
        [Description("")]
        M_TechnicianDispatch_TechnicanDispatchSummary = 171,
        [Description("")]
        M_TechnicianDispatch_TechnicanDispatchRequired = 172,
        [Description("")]
        M_TechnicianDispatch_TechnicanDispatched = 173,
        [Description("")]
        M_TechnicianDispatch_MyDispatches = 174,

        [Description("N1.011 Building Details")]
        N_TechnicianToolkit_BuildingDetails = 175,
        [Description("N2.011 Communication Gateways")]
        N_TechnicianToolkit_CommunicationGateways = 176,
        [Description("N2.012 Offline Devices")]
        N_TechnicianToolkit_OfflineDevices = 177,
        [Description("N2.013 All Devices")]
        N_TechnicianToolkit_AllDevices = 178,
        [Description("N3.011 Add Device To Gateway")]
        N_TechnicianToolkit_AddDeviceToGateway = 179,
        [Description("N3.012 Newly Added Devices")]
        N_TechnicianToolkit_NewlyAddedDevices = 180,
        [Description("N4.011 Token Sender")]
        N_TechnicianToolkit_TokenSender = 181,
        [Description("N4.012 Token Log")]
        N_TechnicianToolkit_TokenLog = 182,

        [Description("G1.011 Bulk Communication")]
        G_Communication_BulkCommunication = 183,
        [Description("G2.011 Notification Log")]
        G_Communication_NotificationLog = 184,

        [Description("J1.010 Administration")]
        J_Finance_Administration = 185,
        [Description("J2.010 Receipt Log")]
        J_Finance_ReceiptLog = 186,
        [Description("J3.010 External Charges")]
        J_Finance_ExternalCharges = 187,
        [Description("J4.010 Meter Recon Report")]
        J_Finance_MeterReconReport = 188,

        [Description("Contact Service Provider")]
        Customer_ContactServiceProvider = 189,
        [Description("Historical Data Summary")]
        Customer_HistoricalDataSummary = 190,
        [Description("Profile")]
        Customer_Profile = 191,
        [Description("Protest Tool")]
        Customer_ProtestTool = 192,
        [Description("Usage Calculator")]
        Customer_UsageCalculator = 193,

        [Description("FE1.011 Pulse Counter Corrector")]
        F_SystemGeneratedReports_PulseCounterCorrector = 194,

        [Description("A2.030 Mirror Checklist Summary")]
        A02_MirrorMeterAuditing_MirrorChecklistSummary = 195,
        [Description("A2.031 Mirror Checklist Details")]
        A02_MirrorMeterAuditing_MirrorChecklistDetails = 196,
        [Description("A2.032 Mirror Checklist Results")]
        A02_MirrorMeterAuditing_MirrorChecklistResults = 197,


        [Description("Login Messages")]
        SiteAdmin_LoginMessages = 198,


        [Description("A2.051 Mirror Device Search")]
        A02_MirrorMeterAuditing_MirrorDeviceSearch = 199,
        [Description("A2.052 Mirror Device Summary")]
        A02_MirrorMeterAuditing_MirrorDeviceSummary = 200,
        [Description("A2.053 Mirror Device Details")]
        A02_MirrorMeterAuditing_MirrorDeviceDetails = 201,
        [Description("A2.054 Mirror Device Review")]
        A02_MirrorMeterAuditing_MirrorDeviceReview = 202,

        /// <summary>
        ///  Get all info from m2m according to serial. Do not allow save if stuff doesnt match up.
        /// </summary>
        [Description("A2.055 Mirror Device Add")]
        A02_MirrorMeterAuditing_MirrorDeviceAdd = 203,

        [Description("B1.011 Account Details Summary")]
        B01_AccountPayments_AccountPaymentSummary = 204,

        [Description("B3.013 Council Statement Report")]
        B03_SupplyCouncilStatements_CouncilStatementReport = 205,


        [Description("B2.011 Council Reading Update")]
        B02_CouncilReadings_CouncilReadingUpdate = 209,

        [Description("B2.012 Council Reading Summary")]
        B02_CouncilReadings_CouncilReadingSummary = 206,

        [Description("B2.013 Council Reading Details")]
        B02_CouncilReadings_CouncilReadingDetails = 207,

        [Description("B2.014 Council Reading Verification And Submission")]
        B02_CouncilReadings_CouncilReadingVerification = 210,

        [Description("B2.015 Council Reading Results")]
        B02_CouncilReadings_CouncilReadingResults = 208,

        [Description("B2.021 Council Meter Summary")]
        B02_CouncilDevice_CouncilMetersSummary = 211,

        [Description("B2.022 Council Meter Details")]
        B02_CouncilDevice_CouncilMetersDetails = 212,

        [Description("B2.023 Council Meter Review")]
        B02_CouncilDevice_CouncilMetersReview = 213,


        [Description("B3.021 Council Recon Report")]
        B03_SupplyCouncilStatements_CouncilReconReport = 214,

        [Description("B2.031 Council Reading Planner")]
        B02_CouncilReadings_CouncilReadingPlanner = 215,

        [Description("B5.021 Payment Forecast")]
        B05_SupplyPayments_PaymentForecast = 216,

        [Description("B1.022 Supply Cost Settings Details")]
        B01_AccountPayments_SupplyCostSettingsDetails = 217,

        [Description("B1.021 Supply Cost Settings Summary")]
        B01_AccountPayments_SupplyCostSettingsSummary = 218,

        [Description("Building Cycles")]
        SiteAdmin_BuildingCycles = 219,

        [Description("Building Council Types")]
        SiteAdmin_BuildingCouncilTypes = 220,

        [Description("Building Details")]
        SiteAdmin_BuildingDetails = 221,

        [Description("B4.011 Council Check Recon Summary")]
        B04_SupplyReconciliation_CouncilCheckRecon = 222,

        [Description("B4.012 Council Check Recon Details")]
        B04_SupplyReconciliation_CouncilCheckReconDetails = 223,


        [Description("Products")]
        SiteAdmin_Products = 224,
        [Description("Products - Skybill Resources")]
        SiteAdmin_ProductsSkybillResources = 225,

        [Description("C1.011 Product Report Summary")]
        C01_ProductReport_Summary = 226,
        [Description("C1.012 Product Report Monthly")]
        C01_ProductReport_Details = 227,

        [Description("FC3.016 Skybill Resource Ledger Sync")]
        F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync = 228,

        [Description("FC3.017 Report Products Monthlies Sync")]
        F_SystemGeneratedReports_Report_ProductsResourceLedgerMonthliesSync = 229,
        [Description("FC3.018 Report Supply Cost Monthlies Sync")]
        F_SystemGeneratedReports_Report_SupplyCostMonthliesSync = 230,
        [Description("FC3.019 Report General Ledger Monthlies Sync")]
        F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync = 231,

        [Description("C6.021 Product Ledger Recon Report Summary")]
        C06_LedgerReconReport_Summary = 232,
        [Description("C6.022 Product Ledger Recon Report Details")]
        C06_LedgerReconReport_Details = 233,

        [Description("C6.011 TB and GL Recon Report Summary")]
        C06_TBGLReconReport_Summary = 234,
        [Description("C6.012 TB and GL Recon Report Details")]
        C06_TBGLReconReport_Details = 235,

        [Description("Partners")]
        SiteAdmin_Partners = 236,
        [Description("Companies")]
        SiteAdmin_Companies = 237,

        [Description("C4.011 Operational Profit Report Summary")]
        C04_OperationalProfitReport_Summary = 238,
        [Description("C4.011 Operational Product Profit Report")]
        C04_OperationalProfitReport_Details = 239,
        [Description("C4.012 Operational Property Profit Report")]
        C04_OperationalPropertyProfitReport_Details = 240,

        [Description("S1.091 Billed Meter Readings Summary")]
        S_CombinedReports_BilledMeterReadings_Summary = 241,
        [Description("S1.092 Billed Meter Readings Monthly")]
        S_CombinedReports_BilledMeterReadings_Monthly = 242,
        [Description("S1.093 Billed Meter Readings Daily")]
        S_CombinedReports_BilledMeterReadings_Daily = 243,

        [Description("A7.051 Connector Summary")]
        A07_CreditControlAndNotifierProcess_Connector_Summary = 244,
        [Description("A7.052 Connector Details")]
        A07_CreditControlAndNotifierProcess_Connector_Details = 245,
        [Description("A7.053 Connector Results")]
        A07_CreditControlAndNotifierProcess_Connector_Results = 246,

        [Description("J5.010 Bulk Readings Export")]
        J_Finance_BulkReadingsExport = 247,

        [Description("H1.011 Device Overview")]
        H_Device_Administrator_DeviceOverview = 248,
        [Description("H1.012 Add Device To Gateway")]
        H_Device_Administrator_AddDeviceToGateway = 249,
        [Description("H1.013 Device Name Bulk Update")]
        H_Device_Administrator_DeviceNameBulkUpdate = 250,
        [Description("H1.014 Bulk 433 Test Upload")]
        H_Device_Administrator_Bulk433TestingUpload = 251,
        [Description("H1.015 Auto Device Discovery")]
        H_Device_Administrator_AutoDeviceDiscovery = 252,
        [Description("H1.016 Wireless Signal Optimizer")]
        H_Device_Administrator_WirelessSignalOptimizer = 253,

        [Description("Meter Types")]
        SiteAdmin_MeterTypes = 254,
        [Description("Balance Redeem")]
        Customer_BalanceRedeem = 255,

        [Description("FC3.021 Report Products Resource Ledger Customer Monthlies Sync")]
        F_SystemGeneratedReports_Report_ProductsResourceLedgerCustomerMonthliesSync = 256,

        [Description("C1.013 Product Report Daily")]
        C01_ProductReport_Daily = 257,

        [Description("A Dashboard")]
        Dashboards_A = 258,

        [Description("A6.051 Daily Billing Overview - Summary")]
        A06_BillingControlReport_DailyBillingOverview_Summary = 259,

        [Description("A6.061 Tariff Review")]
        A06_BillingControlReport_TarrifReview = 260,

        [Description("Tariff")]
        Customer_Tariff = 261,

        [Description("A5.011 Midnight Sync Summary")]
        A05_Exceptions_MidnightSyncHistory_Summary = 262,
        [Description("A5.012 Midnight Sync Details")]
        A05_Exceptions_MidnightSyncHistory_Details = 263,
        [Description("A5.013 Midnight Sync Results")]
        A05_Exceptions_MidnightSyncHistory_Results = 264,

        [Description("A6.051 Prepaid Control Summary")]
        A06_BillingControlReport_PrepaidControl_Summary = 265,
        [Description("A6.052 Prepaid Control Details")]
        A06_BillingControlReport_PrepaidControl_Details = 266,
        [Description("A6.053 Prepaid Control Results")]
        A06_BillingControlReport_PrepaidControl_Results = 267,

        [Description("A10.023 TOU Recon Meter Daily")]
        A10_VirtualMeters_TOUReconReport_Details = 268,
        [Description("A10.024 TOU Recon Meter Hourly")]
        A10_VirtualMeters_TOUReconReport_Review = 269,

        [Description("Skybill Journal Logs")]
        Z_SystemLogs_SkybillJournalLogs = 270,

        [Description("J6.011 Journal Payment Allocation")]
        J_Finance_JournalPaymentAllocation = 271,

        [Description("J8.011 Journal Sagepay Release Summary")]
        J_Finance_JournalSagepayRelease_Summary = 272,
        [Description("J8.012 Journal Sagepay Release Details")]
        J_Finance_JournalSagepayRelease_Details = 273,

        [Description("J6.011 Cigicell Report Summary")]
        J_Finance_JournalCigicellRecon_Summary = 274,

        [Description("FC3.014 Skybill Resource Lists Sync")]
        F_SystemGeneratedReports_SkybillResourceListsSync = 275,

        [Description("Skybill Daily Billing Notification")]
        F_SystemGeneratedReports_Skybill_DailyBilling = 276,

        [Description("C2.011 GL Report Summary")]
        C02_GeneralLedgerReport_Summary = 277,

        [Description("C2.012 GL Report Monthly")]
        C02_GeneralLedgerReport_Monthly = 278,

        [Description("C2.013 GL Report Daily")]
        C02_GeneralLedgerReport_Daily = 279,

        [Description("C2.014 GL Report Details")]
        C02_GeneralLedgerReport_Details = 280,

        [Description("Payment Methods Skybill Journal Nos")]
        SiteAdmin_PaymentMethodsSkybillJournalNos = 281,

        [Description("J8.021 Journal Sagepay Recon Summary")]
        J_Finance_JournalSagepayRecon_Summary = 282,

        [Description("A08 Task Types")]
        SiteAdmin_A08TaskTypes = 283,

        [Description("Task Jobs")]
        F_SystemGeneratedReports_A08_TasksJobs = 284,

        [Description("A8.011 Tasks Company Summary")]
        A08_Tasks_Company_Summary = 285,

        [Description("A8.012 Tasks Company Details")]
        A08_Tasks_Company_Details = 286,

        [Description("A8.013 Tasks Company Results")]
        A08_Tasks_Company_Results = 287,

        [Description("A8.021 Tasks User Summary")]
        A08_Tasks_User_Summary = 288,

        [Description("A8.022 Tasks User Details")]
        A08_Tasks_User_Details = 289,

        [Description("A8.023 Tasks User Results")]
        A08_Tasks_User_Results = 290,

        [Description("A8.031 Tasks Type Summary")]
        A08_Tasks_Type_Summary = 291,

        [Description("A8.032 Tasks Type Details")]
        A08_Tasks_Type_Details = 292,

        [Description("A8.033 Tasks Type Results")]
        A08_Tasks_Type_Results = 293,

        [Description("A8.040 Task Review")]
        A08_Task_Review = 294,

        [Description("D2.11 Log a Lead")]
        D01_Leads_LogLead = 295,

        [Description("D2.12 My Leads")]
        D01_Leads_MyLeads = 296,

        [Description("D2.13 View Lead")]
        D01_Leads_ViewLead = 297,

        [Description("V1.011 All Policies")]
        V01_Policies_All = 298,

        [Description("V1.012 My Policies")]
        V01_Policies_MyPolicies = 299,

        [Description("V1.013 View Policy")]
        V01_Policies_View = 300,

        [Description("Vehicles")]
        SiteAdmin_Vehicles = 301,

        [Description("E1 Building Onboarding Questions")]
        SiteAdmin_BuildingOnboardingQuestions = 302,

        //[Description("E1.011 Building Onboarding Answers")]
        //E01_BuildingOnboarding_Answers = 303,

        [Description("J0.000 Company Financial Details")]
        J_Finance_CompanyFinancialDetails = 304,

        [Description("N1.012 Technical Details")]
        N_TechnicianToolkit_TechnicalDetails = 305,

        [Description("Z 011 Report a Bug")]
        Z_Bugs_ReportABug = 306,

        [Description("Z 012 My Bugs")]
        Z_Bugs_MyBugs = 307,

        [Description("Z 013 Bug Detail")]
        Z_Bugs_BugDetail = 308,

        [Description("Z 014 Bug Admin")]
        Z_Bugs_BugAdmin = 309,

        [Description("B Dashboard")]
        Dashboards_B = 310,

        [Description("J9.011 Winshuttle Export")]
        J_Finance_WinshuttleExport = 311,

        [Description("J9.012 Winshuttle Export Requests")]
        J_Finance_WinshuttleExportRequests = 312,

        [Description("FJ1.11 Skybill Payment Allocations")]
        F_SystemGeneratedReports_SkybillPaymentAllocations = 313,

        [Description("J2.011 Sagepay Allocation Exceptions")]
        J_Finance_SagepayAllocationExceptions = 314,

        [Description("J2.012 Unipin Allocation Exceptions")]
        J_Finance_UnipinAllocationExceptions = 315,

        [Description("J2.013 Allocation Rerun")]
        J_Finance_AllocationRerun = 316,

        [Description("E2 Safety File Questions")]
        SiteAdmin_SafetyFileQuestions = 317,

        [Description("E2.011 Safety File Answers")]
        E02_SafetyFile_Answers = 318,

        [Description("C6.023 Product Ledger Recon Report Daily")]
        C06_LedgerReconReport_Daily = 319,

        [Description("Unipin Daily")]
        F_SystemGeneratedReports_Unipin_Daily = 320,

        [Description("Unipin Weekly")]
        F_SystemGeneratedReports_Unipin_Weekly = 321,

        [Description("Unipin Monthly")]
        F_SystemGeneratedReports_Unipin_Monthly = 322,

        [Description("Usage Tool Calc Assets")]
        SiteAdmin_UsageToolCalcAssets = 323,

        [Description("J6.010 PQ Allocation")]
        J_Finance_PQAllocation = 324,

        [Description("AF1.011 Metering Summary")]
        AF_AfroxAdministration_Metering_Summary = 325,

        [Description("AF1.012 Metering Details")]
        AF_AfroxAdministration_Metering_Details = 326,

        [Description("AF7.011 Meter Reading Export")]
        AF_AfroxAdministration_WinshuttleExport = 327,

        [Description("AF7.012 Meter Reading Export Requests")]
        AF_AfroxAdministration_WinshuttleExportRequests = 328,

        [Description("J9.015 Netcash Technical Review")]
        J_Finance_NetcashMissingSkybillJournals = 329,

        [Description("J2.012 Direct Deposits Allocation Exceptions")]
        J_Finance_NetcashManualPayments = 330,

        [Description("H2.011 Active Energy Anomalies Summary")]
        H_Device_Administrator_ActiveEnergyAnomalies_Summary = 331,

        [Description("H2.012 Active Energy Anomalies Details")]
        H_Device_Administrator_ActiveEnergyAnomalies_Details = 332,

        [Description("H2.013 Active Energy Anomalies Request")]
        H_Device_Administrator_ActiveEnergyAnomalies_Request = 333,

        [Description("A10.015 Virtual Meter Create")]
        A10_VirtualMeters_Create = 334,

        [Description("A10.012 Virtual Meter Details")]
        A10_VirtualMeters_Details = 335,

        [Description("A10.014 Virtual Meter Review")]
        A10_VirtualMeters_Review = 336,

        [Description("J2.011 Receipt Exceptions")]
        J_Finance_ReceiptLogExceptions = 337,

        [Description("J9.011 Netcash Report Summary")]
        J_Finance_NetcashReport_Summary = 338,

        [Description("J9.012 Netcash Report Monthly")]
        J_Finance_NetcashReport_Monthly = 339,

        [Description("J9.013 Netcash Report Daily")]
        J_Finance_NetcashReport_Daily = 340,

        [Description("J9.014 Netcash Report Details")]
        J_Finance_NetcashReport_Details = 341,

        [Description("C1.014 Product Report Details - Billings")]
        C01_ProductReport_Details_Details = 342,

        [Description("AF5.011 Afrox Add Device")]
        AF_AfroxAdministration_AddDevice = 343,

        [Description("AF5.012 Afrox Newly Added Devices")]
        AF_AfroxAdministration_NewlyAddedDevices = 344,

        [Description("AF5.013 Afrox Device Review")]
        AF_AfroxAdministration_DeviceReview = 345,

        [Description("FC3.022 Chart Of Accounts Snapshots")]
        F_SystemGeneratedReports_SkybillJob_ChartOfAccountsSnapshot = 346,

        [Description("Priorities")]
        SiteAdmin_Priorities = 347,

        [Description("Status Groups")]
        SiteAdmin_StatusGroups = 348,

        [Description("A8.041 Task Create")]
        A08_Task_Create = 349,

        [Description("FXX.XXX Levels Checker")]
        F_SystemGeneratedReports_LevelsChecker = 350,

        [Description("FP1.011 - Supplementary info dump")]
        F_SystemGeneratedReports_SupplementaryInfoDump = 351,

        [Description("E1.011 Tasks Company Summary")]
        E01_BuildingOnboardingTasks_Company_Summary = 352,

        [Description("E1.012 Tasks Company Details")]
        E01_BuildingOnboardingTasks_Company_Details = 353,

        [Description("E1.013 Tasks Company Results")]
        E01_BuildingOnboardingTasks_Company_Results = 354,

        [Description("E1.021 Tasks User Summary")]
        E01_BuildingOnboardingTasks_User_Summary = 355,

        [Description("E1.022 Tasks User Details")]
        E01_BuildingOnboardingTasks_User_Details = 356,

        [Description("E1.023 Tasks User Results")]
        E01_BuildingOnboardingTasks_User_Results = 357,

        [Description("E1.031 Tasks Type Summary")]
        E01_BuildingOnboardingTasks_Type_Summary = 358,

        [Description("E1.032 Tasks Type Details")]
        E01_BuildingOnboardingTasks_Type_Details = 359,

        [Description("E1.033 Tasks Type Results")]
        E01_BuildingOnboardingTasks_Type_Results = 360,

        [Description("E1.040 Task Review")]
        E01_BuildingOnboardingTask_Review = 361,

        [Description("E01 Building Onboarding Task Types")]
        SiteAdmin_E01_BuildingOnboardingTaskTypes = 362,

        [Description("FXX.XXX - Meter Serial Number Confirmation")]
        F_SystemGeneratedReports_MeterSerialNumberConfirmation = 363,

        [Description("AF1.010 Overall Stats")]
        AF_AfroxAdministration_OverallStats = 364,

        [Description("FXX.XXX - SOC Snapshot")]
        F_SystemGeneratedReports_SOCSnapshot = 365,

        [Description("SOC Summary")]
        SOC_Summary = 366,

        [Description("C1.015 Product Report Details - Units")]
        C01_ProductReport_Details_Details_Units = 367,

        [Description("C1.016 Product Report Details - Average")]
        C01_ProductReport_Details_Details_AVG = 368,

        [Description("AF2.011 Meter Reading Summary")]
        AF_AfroxAdministration_MeterReadingSummary = 369,

        [Description("AF2.012 Meter Reading Details")]
        AF_AfroxAdministration_MeterReadingDetails = 370,

        [Description("AF2.013 Meter Reading Verification")]
        AF_AfroxAdministration_MeterReadingVerification = 371,

        [Description("AF2.014 Meter Reading Update")]
        AF_AfroxAdministration_MeterReadingUpdate = 372,

        [Description("C1.017 Product Report Monthly - Billings")]
        C01_ProductReport_Monthly_Details = 373,

        [Description("C1.018 Product Report Monthly - Units")]
        C01_ProductReport_Monthly_Details_Units = 374,

        [Description("C1.019 Product Report Monthly - Average")]
        C01_ProductReport_Monthly_Details_AVG = 375,

        [Description("B1.024 Supply Cost Settings Templates")]
        B01_AccountPayments_SupplyCostSettingsTemplates = 376,

        [Description("B1.025 Supply Cost Settings Calculation Details")]
        B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails = 377,

        [Description("B1.023 Supply Cost Settings Templates Summary")]
        B01_AccountPayments_SupplyCostSettingsTemplatesSummary = 378,

        [Description("FXX.XXX - Cost Settings Templates Copier")]
        F_SystemGeneratedReports_CostSettingsTemplatesCopier = 379,

        [Description("A10.021 TOU Recon Meter Summary")]
        A10_VirtualMeters_TOUReconReport_Summary = 380,

        [Description("A10.022 TOU Recon Meter Monthly")]
        A10_VirtualMeters_TOUReconReport_Monthly = 381,

        [Description("Statement and Invoices")]
        Customer_AccountStatement = 382,

        [Description("C3.022 Average and per Unit Report - Summary")]
        C03_AverageAndPerUnitReport_Summary = 383,

        [Description("A Switch To Customer View")]
        Customer_SwitchToCustomerView = 384,

        [Description("AF6.012 Gas Network Balancing - Daily")]
        AF_AfroxAdministration_GasNetworkBalancing_Daily = 385,

        [Description("Insights")]
        Customer_Insights = 386,

        [Description("A7.063 Aging of customers Results")]
        A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results = 387,

        [Description("A2.061 Mirror Reading Delete")]
        A02_MirrorMeterAuditing_MirrorReadingDelete = 388,

        [Description("V2.011 All Task Allocations")]
        V02_Workflow_Allocations_AllTaskAllocations = 389,

        [Description("V2.012 My Task Allocations")]
        V02_Workflow_Allocations_MyTaskAllocations = 390,

        [Description("V2.013 View Task Allocations")]
        V02_Workflow_Allocations_ViewTaskAllocations = 391,

        [Description("A8.000 Tasks Search")]
        A08_Tasks_Search = 392,

        [Description("A9.000 Flags Search")]
        A09_Flags_Search = 393,

        [Description("V3.011 All Human Resources")]
        V03_HumanResources_AllHumanResources = 394,
        [Description("V3.012 Personal details")]
        V03_HumanResources_PersonalDetails = 395,
        [Description("V3.013 Job Descriptions")]
        V03_HumanResources_JobDescriptions = 396,
        [Description("V3.014 Correspondence list")]
        V03_HumanResources_CorrespondenceList = 397,

        [Description("Workflow Groups")]
        SiteAdmin_WorkflowGroups = 398,


        [Description("V04.010 Exco Meetings")]
        V04_InternalMeetings_ExcoMeetings = 399,
        [Description("V04.020 Manco Meetings")]
        V04_InternalMeetings_MancoMeetings = 400,
        [Description("V04.030 Rocks Meetings")]
        V04_InternalMeetings_RocksMeetings = 401,
        [Description("V04.040 Standup Meetings")]
        V04_InternalMeetings_StandupMeetings = 402,
        [Description("V04.050 Cloud Cast Meetings")]
        V04_InternalMeetings_CloudCastMeetings = 403,
        [Description("V04.060 Customer Care Meeting")]
        V04_InternalMeetings_CustomerCareMeeting = 404,
        [Description("V04.070 Technician Dispatch")]
        V04_InternalMeetings_TechnicianDispatch = 405,
        [Description("V04.080 IT System Meeting")]
        V04_InternalMeetings_ITSystemMeeting = 406,

        [Description("W1.011 Time Planner Summary")]
        W01_ActivityLogs_TimePlanner_Summary = 407,
        [Description("W1.012 Time Planner Details")]
        W01_ActivityLogs_TimePlanner_Details = 408,
        [Description("W1.013 Time Planner Results")]
        W01_ActivityLogs_TimePlanner_Results = 409,

        [Description("W1.021 Time Allocated Summary")]
        W01_ActivityLogs_TimeAllocated_Summary = 410,
        [Description("W1.022 Time Allocated Details")]
        W01_ActivityLogs_TimeAllocated_Details = 411,
        [Description("W1.023 Time Allocated Results")]
        W01_ActivityLogs_TimeAllocated_Results = 412,

        [Description("W1.031 Travel Allocation Summary")]
        W01_ActivityLogs_TravelAllocation_Summary = 413,
        [Description("W1.032 Travel Allocation Details")]
        W01_ActivityLogs_TravelAllocation_Details = 414,
        [Description("W1.033 Travel Allocation Results")]
        W01_ActivityLogs_TravelAllocation_Results = 415,

        [Description("W1.041 Stock Allocation Summary")]
        W01_ActivityLogs_StockAllocation_Summary = 416,
        [Description("W1.042 Stock Allocation Details")]
        W01_ActivityLogs_StockAllocation_Details = 417,
        [Description("W1.043 Stock Allocation Results")]
        W01_ActivityLogs_StockAllocation_Results = 418,

        [Description("W1.051 Invoice Allocation Summary")]
        W01_ActivityLogs_InvoiceAllocation_Summary = 419,
        [Description("W1.052 Invoice Allocation Details")]
        W01_ActivityLogs_InvoiceAllocation_Details = 420,
        [Description("W1.053 Invoice Allocation Results")]
        W01_ActivityLogs_InvoiceAllocation_Results = 421,

        [Description("FXX.XXX - TOU Reading Fixer")]
        F_SystemGeneratedReports_TOUReadingFixer = 422,

        [Description("C2.010 Consolidated TB")]
        C02_GeneralLedgerReport_ConsolidatedTB = 423,

        [Description("FXX.XXX - Create BTR Journals")]
        F_SystemGeneratedReports_CreateBTRJournals = 424,

        [Description("J9.016 Netcash Services Charges Recon")]
        J_Finance_NetcashServicesChargesRecon = 425,

        [Description("J9.017 Netcash Transactions Recon - Monthly")]
        J_Finance_NetcashTransactionsRecon = 426,

        [Description("FXX.XXX - Netcash SB & Payments Sync")]
        F_SystemGeneratedReports_StatementsSkybillAndPaymentsSync = 427,

        [Description("J9.018 Netcash Transactions Recon - Daily")]
        J_Finance_NetcashTransactionsRecon_Daily = 428,

        [Description("B5.022 Cashflow Forecast")]
        B05_SupplyPayments_CashflowForecast = 429,

        [Description("B5.023 Payment Matching Schedule")]
        B05_SupplyPayments_PaymentMatchingSchedule = 430,

        [Description("C9.022 Consolidated Netcash Balances")]
        C09_CashflowForecast_ConsolidatedNetcashBalances = 431,

        [Description("J6.012 Cigicell Report Daily")]
        J_Finance_JournalCigicellRecon_Daily = 432,

        [Description("J6.021 Cigicell Weekly Summary")]
        J_Finance_CigicellWeeklySummary = 433,

        [Description("J6.022 Cigicell Weekly Details")]
        J_Finance_CigicellWeeklyDetails = 434,

        [Description("J6.023 Cigicell Transaction Details")]
        J_Finance_CigicellTransactionDetails = 435,

        [Description("FXX.XXX - Accounting Checklists")]
        F_SystemGeneratedReports_AccountingChecklists = 436,

        [Description("C2.015 GL Audit View")]
        C02_GeneralLedgerReport_GLAuditView = 437,

        [Description("C2.016 GL Audit Update")]
        C02_GeneralLedgerReport_GLAuditUpdate = 438,

        [Description("B3.015 Council Statement Accounting Details")]
        B03_SupplyCouncilStatements_CouncilStatementAccountingDetails = 439,

        [Description("Skybill Customers Utilities")]
        SiteAdmin_SkybillCustomersUtilities = 440,

        [Description("Imports - Rental Expenses")]
        SiteAdmin_Imports_RentalExpenses = 441,

        [Description("Imports - Management Accounts")]
        SiteAdmin_Imports_ManagementAccountsDataDump = 442,

        [Description("FXX.XXX - Management Accounts Data Dump")]
        F_SystemGeneratedReports_ManagementAccountsDataDump = 443,

        [Description("C7.010 - Management Accounts Summary")]
        C07_ManagementAccounts_Summary = 444,

        [Description("C7.012 - Management Accounts Income")]
        C07_ManagementAccounts_Details = 445,

        [Description("Management Accounts")]
        SiteAdmin_ManagementAccounts_ReportingCategories = 446,

        [Description("C7.013 - Management Accounts Operating Expenses")]
        C07_ManagementAccounts_OperatingExpenses = 447,

        [Description("C7.014 - Management Accounts Other Expenses")]
        C07_ManagementAccounts_OtherExpenses = 448,

        [Description("C7.011 - Management Accounts Grand Finale Monthly")]
        C07_ManagementAccounts_GrandFinale_Monthly = 449,

        [Description("C7.011 - Management Accounts Grand Finale Yearly")]
        C07_ManagementAccounts_GrandFinale_Yearly = 450,

        [Description("Company Types")]
        SiteAdmin_CompanyTypes = 451,

        [Description("A10.035 Merged Meter Create")]
        A10_MergedMeters_Create = 452,

        [Description("A10.032 Merged Meter Details")]
        A10_MergedMeters_Details = 453,

        [Description("A10.034 Merged Meter Review")]
        A10_MergedMeters_Review = 454,

        [Description("W0.000 User Activity Details")]
        W01_ActivityLogs_UserActivity_Details = 455,

        [Description("FXX.XXX - Management Accounts")]
        F_SystemGeneratedReports_ManagementAccounts = 456,

        [Description("B4.022 Council Calendar Month Recon - Details")]
        B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details = 457,

        [Description("FXX.XXX - Building Council Details Invoice Item Months Sync")]
        F_SystemGeneratedReports_BuildingCouncilDetails_InvoiceItem_MonthsSync = 458,

        [Description("B4.032 Council To GL Recon - Resource Details")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details = 459,

        [Description("C05.012 - Monthly Management Fees Details")]
        C05_MonthlyManualInvoicing_MonthlyManagementFees_Details = 460,

        [Description("FXX.XXX - Netcash Services Charges Recon")]
        F_SystemGeneratedReports_NetcashServicesChargesRecon = 461,

        [Description("B4.031 Council To GL Recon - Resource Summary")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary = 462,

        [Description("B4.033 Council Transactions Details")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths = 463,

        [Description("B4.041 Council To GL Recon - Product Summary")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary = 464,

        [Description("B4.042 Council To GL Recon - Product Details")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details = 465,

        [Description("B4.043 Council Product Details")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths = 466,

        [Description("FXX.XXX - B01 Account Payments Supply Cost Settings Templates Products Sync")]
        F_SystemGeneratedReports_B01_AccountPayments_SupplyCostSettingsTemplates_ProductsSync = 467,

        [Description("FXX.XXX - Netcash Automation Payments To Council CRP")]
        F_SystemGeneratedReports_NetcashAutomation_PaymentsToCouncil_CRP = 468,

        [Description("FXX.XXX - Netcash Automation Payments for Meter Rental IAT")]
        F_SystemGeneratedReports_NetcashAutomation_PaymentsforMeterRental_IAT = 469,

        [Description("FXX.XXX - B05 Supply Payments Payment Details Document No Allocation")]
        F_SystemGeneratedReports_B05SupplyPayments_PaymentDetailsDocumentNoAllocation = 470,

        [Description("B4.051 Council To GL Adjustments")]
        B04_SupplyPayments_CouncilToGLAdjustments = 471,

        [Description("Device APIs")]
        SiteAdmin_DeviceAPIs = 472,

        [Description("B4.023 Council Calendar Month Recon - Exceptions")]
        B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions = 473,

        [Description("G2.012 App Notification Log")]
        G_Communication_AppNotificationLog = 474,

        [Description("Workflows | Pillars | Deparments")]
        SiteAdmin_Workflows = 475,

        [Description("B4.052 Council To GL Adjustments Summary")]
        B04_SupplyPayments_CouncilToGLAdjustments_Summary = 476,

        [Description("B4.021 Council Calendar Month Recon - Summary")]
        B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary = 477,

        [Description("B4.040 Council To GL Recon - Product All")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All = 478,

        [Description("B4.030 Council To GL Recon - Resource All")]
        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All = 479,


        [Description("S2.011 Billing Analysis Amount Summary")]
        S02_ProductCombinedReports_BillingAnalysis_Amount_Summary = 480, // Elec billing ZAR Values
        [Description("S2.012 Billing Analysis Amount Monthly")]
        S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly = 481, // Elec billing ZAR Values
        [Description("S2.013 Billing Analysis Amount Daily")]
        S02_ProductCombinedReports_BillingAnalysis_Amount_Daily = 482, // Elec billing ZAR Values

        [Description("S2.021 Billing Analysis Units Summary")]
        S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary = 483, // Elec billing kWh Values
        [Description("S2.022 Billing Analysis Units Monthly")]
        S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly = 484, // Elec billing kWh Values
        [Description("S2.023 Billing Analysis Units Daily")]
        S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily = 485, // Elec billing kWh Values

        [Description("S2.031 Metered Analysis Units Summary")]
        S02_ProductCombinedReports_MeteredAnalysis_Units_Summary = 486, // Electricity - Metered
        [Description("S2.032 Metered Analysis Units Monthly")]
        S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly = 487, // Electricity - Metered
        [Description("S2.033 Metered Analysis Units Daily")]
        S02_ProductCombinedReports_MeteredAnalysis_Units_Daily = 488, // Electricity - Metered

        [Description("S2.041 Unbilled Analysis Units Summary")]
        S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary = 489, // m2m and billing sync diff Units
        [Description("S2.042 Unbilled Analysis Units Monthly")]
        S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly = 490, // m2m and billing sync diff Units
        [Description("S2.043 Unbilled Analysis Units Daily")]
        S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily = 491, // m2m and billing sync diff Units

        [Description("S2.051 Cost Analysis Amount Summary")]
        S02_ProductCombinedReports_CostAnalysis_Amount_Summary = 492, // SiteAdmin_Company_CostSettings billing ZAR Values
        [Description("S2.052 Cost Analysis Amount Monthly")]
        S02_ProductCombinedReports_CostAnalysis_Amount_Monthly = 493, // SiteAdmin_Company_CostSettings billing ZAR Values
        [Description("S2.053 Cost Analysis Amount Daily")]
        S02_ProductCombinedReports_CostAnalysis_Amount_Daily = 494, // SiteAdmin_Company_CostSettings billing ZAR Values

        [Description("S2.061 Profit Analysis Amount Summary")]
        S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary = 495, // Diff - S02_ProductCombinedReports_CostAnalysis_Amount - S02_ProductCombinedReports_BillingAnalysis_Amount
        [Description("S2.062 Profit Analysis Amount Monthly")]
        S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly = 496, // Diff - S02_ProductCombinedReports_CostAnalysis_Amount - S02_ProductCombinedReports_BillingAnalysis_Amount
        [Description("S2.063 Profit Analysis Amount Daily")]
        S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily = 497, // Diff - S02_ProductCombinedReports_CostAnalysis_Amount - S02_ProductCombinedReports_BillingAnalysis_Amount

        [Description("S2.071 Profit Analysis Gross Profit % Summary")]
        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary = 498, // S02_ProductCombinedReports_ProfitAnalysis_Amount / S02_ProductCombinedReports_BillingAnalysis_Amount
        [Description("S2.072 Profit Analysis Gross Profit % Monthly")]
        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly = 499, // S02_ProductCombinedReports_ProfitAnalysis_Amount / S02_ProductCombinedReports_BillingAnalysis_Amount
        [Description("S2.073 Profit Analysis Gross Profit % Daily")]
        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily = 500, // S02_ProductCombinedReports_ProfitAnalysis_Amount / S02_ProductCombinedReports_BillingAnalysis_Amount

        [Description("S1.011 Overall Analysis Summary")]
        S02_ProductCombinedReports_OverallAnalysis_Summary = 501,
        [Description("S1.012 Overall Analysis Monthly")]
        S02_ProductCombinedReports_OverallAnalysis_Monthly = 502,
        [Description("")]
        S02_ProductCombinedReports_OverallAnalysis_Daily = 503,

        [Description("A3.021 Network Balancing Analysis Units Summary")]
        A03_NetworkBalancing_Units_Summary = 504,
        [Description("A3.022 Network Balancing Analysis Units Monthly")]
        A03_NetworkBalancing_Units_Monthly = 505,
        [Description("A3.023 Network Balancing Analysis Units Daily")]
        A03_NetworkBalancing_Units_Daily = 506,

        [Description("D0.001 Lead Generators")]
        D01_Leads_LeadGenerators = 507,

        [Description("AF8.011 Add ACO Device")]
        AF_AfroxAdministration_AddDeviceACO = 508,

        [Description("A7.071 Meter Contactor State")]
        A07_CreditControlAndNotifierProcess_MeterContactorState = 509,

        [Description("B4.053 Council ADJ to Income Statement")]
        B04_SupplyPayments_CouncilADJtoIncomeStatement = 510,

        [Description("X1.011 Customers")]
        X_MeterTeamAdmin_Customers = 511,

        [Description("X1.010 Add Customer")]
        X_MeterTeamAdmin_Customers_Add = 512,

        [Description("X1.021 Resource Lists")]
        X_MeterTeamAdmin_RecourceLists = 513,

        [Description("X1.020 Add Resource List")]
        X_MeterTeamAdmin_RecourceLists_Add = 514,

        [Description("D0.002 Products & Services")]
        D01_Leads_Products = 515,

        [Description("D1.011 Properties")]
        D01_Leads_Properties = 516,

        [Description("D1.012 Contacts")]
        D01_Leads_Contacts = 517,

        [Description("D2.000 Leads Import")]
        D01_Leads_Import = 518,

        [Description("AF9.001 Supply Details")]
        AF_AfroxAdministration_Supply_Details = 519,

        [Description("AF9.002 ACO Statuses")]
        AF_AfroxAdministration_ACOStatuses = 520,

        [Description("AF9.003 ACO Transaction")]
        AF_AfroxAdministration_SupplyTransaction = 521,

        [Description("AF1.000 Snapshot Emails")]
        AF_AfroxAdministration_SnapshotEmails = 522,

        [Description("AFZ.000 ACO Status Hack")]
        AF_AfroxAdministration_ACO_StatusHack = 523,

        [Description("AF1.013 Metering Review")]
        AF_AfroxAdministration_Metering_Review = 524,

        [Description("Contactor State Hacks")]
        SiteAdmin_ContactorStateHacks = 525,

        [Description("Municipalities")]
        SiteAdmin_Municipalities = 526,

        [Description("D1.013 Managing Agents")]
        D01_Leads_ManagingAgents = 527,

        [Description("D1.014 Competitors")]
        D01_Leads_Competitors = 528,

        [Description("D20.010 Lead Commission Setup")]
        D01_Leads_Commissions_Setup = 529,

        [Description("D20.012 Lead Commission Details")]
        D01_Leads_Commissions_Details = 530,

        [Description("D20.011 Lead Commission Summary")]
        D01_Leads_Commissions_Summary = 531,

        [Description("Suburbs")]
        SiteAdmin_Suburbs = 532,

        [Description("FXX.XXX - D01 Snapshot")]
        F_SystemGeneratedReports_D01_Snapshot = 533,

        [Description("D2.001 Leads Snapshot")]
        D01_Leads_Snapshots = 534,

        [Description("J3.011 - External Charged Summary")]
        J_Finance_ExternalChargedSummary = 535,

        [Description("C1.210 - Product Audit Sync Report Summary")]
        C01_ProductReport_AuditSyncReport_Summary = 536,

        [Description("C1.211 - Product Audit Sync Report Details")]
        C01_ProductReport_AuditSyncReport_Details = 537,

        [Description("C1.213 - Product Audit View")]
        C01_ProductReport_ProductAuditView = 538,

        [Description("C8.011 Forecast Summary")]
        C08_Forecasting_Summary = 539,

        [Description("C8.012 Forecast Details")]
        C08_Forecasting_Details = 540,

        //[Description("C8.013 Forecast Calculation")]
        //C08_Forecasting_DetailsCalculationDetails = 541,

        [Description("C8.013 Forecast Consumption Units")]
        C08_Forecasting_Baselines = 542,

        [Description("C3.011 User and Meter Summary")]
        C03_Report_UserAndMeter_Summary = 543,

        [Description("C3.031 Receipt per property Summary")]
        C03_Report_ReceiptPerProperty_Summary = 544,

        [Description("C3.035 Receipt per property Volumes")]
        C03_Report_ReceiptPerPropertyVolumes_Summary = 545,

        [Description("C3.032 Receipt per property Details")]
        C03_Report_ReceiptPerProperty_Details = 546,

        [Description("C3.036 Receipt per property Volumes")]
        C03_Report_ReceiptPerPropertyVolumes_Details = 547,

        [Description("C9.011 Cashflow Forecast Summary")]
        C09_CashflowForecast_Summary = 548,

        [Description("C9.024 Update Bank Balances")]
        C09_CashflowForecast_UpdateBankBalances = 549,

        [Description("C9.012 Cashflow Forecast Daily")]
        C09_CashflowForecast_Daily = 550,

        [Description("C9.023 Consolidated Bank Balances")]
        C09_CashflowForecast_ConsolidatedBankBalances = 551,

        [Description("E1.014 Company Onboarding")]
        E01_BuildingOnboardingTasks_Company_Onboarding = 552,

        [Description("Meeting Agenda Groups")]
        SiteAdmin_MeetingAgendaGroups = 553,

        [Description("V04.090 Sales Meeting")]
        V04_InternalMeetings_SalesMeeting = 554,

        [Description("V04.100 Onboarding Meeting")]
        V04_InternalMeetings_OnboardingMeeting = 555,

        [Description("A8.001 Tasks Overview")]
        A08_Tasks_Overview = 556,

        [Description("D2.011 Tasks Company Summary")]
        D02_SaleTasks_Company_Summary = 557,

        [Description("D2.012 Tasks Company Details")]
        D02_SaleTasks_Company_Details = 558,

        [Description("D2.013 Tasks Company Results")]
        D02_SaleTasks_Company_Results = 559,

        [Description("D2.021 Tasks User Summary")]
        D02_SaleTasks_User_Summary = 560,

        [Description("D2.022 Tasks User Details")]
        D02_SaleTasks_User_Details = 561,

        [Description("D2.023 Tasks User Results")]
        D02_SaleTasks_User_Results = 562,

        [Description("D2.031 Tasks Type Summary")]
        D02_SaleTasks_Type_Summary = 563,

        [Description("D2.032 Tasks Type Details")]
        D02_SaleTasks_Type_Details = 564,

        [Description("D2.033 Tasks Type Results")]
        D02_SaleTasks_Type_Results = 565,

        [Description("D2.040 Task Review")]
        D02_SaleTask_Review = 566,

        [Description("D2.014 Company Onboarding")]
        D02_SaleTasks_Company_Onboarding = 567,

        [Description("D02 Sale Task Types")]
        SiteAdmin_D02_SaleTaskTypes = 568,

        [Description("FXX.XXX - A09 Flags - A01_GatewayAndDeviceMonitoring_GatewaysOffline")]
        F_SystemGeneratedReports_Flags_A01_GatewayAndDeviceMonitoring_GatewaysOffline = 569,

        [Description("FXX.XXX - A09 Flags - A01_DeviceAndDeviceMonitoring_DevicesOffline")]
        F_SystemGeneratedReports_Flags_A01_DeviceAndDeviceMonitoring_DevicesOffline = 570,

        [Description("FXX.XXX - A09 Flags - A01_GWandDeviceMonitoring_GatewaysAndDevicesNotLinked")]
        F_SystemGeneratedReports_Flags_A01_GWandDeviceMonitoring_GatewaysAndDevicesNotLinked = 571,

        [Description("FXX.XXX - A09 Flags - A01_GWandDeviceMonitoring_GatewaysAndDevicesBatteryLow")]
        F_SystemGeneratedReports_Flags_A01_GWandDeviceMonitoring_GatewaysAndDevicesBatteryLow = 572,

        [Description("FXX.XXX - A09 Flags - A2_MirrorMeterAuditing_Calibration")]
        F_SystemGeneratedReports_Flags_A2_MirrorMeterAuditing_Calibration = 573,

        [Description("FXX.XXX - A09 Flags - A2_MirrorMeterAuditing_Reading")]
        F_SystemGeneratedReports_Flags_A2_MirrorMeterAuditing_Reading = 574,

        [Description("FXX.XXX - A09 Flags - A04_ZendeskTickets")]
        F_SystemGeneratedReports_Flags_A04_ZendeskTickets = 575,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_Occupancy")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_Occupancy = 576,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_FaultyorTamperedMeter")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_FaultyorTamperedMeter = 577,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_LastBilledExceedsLiveReading")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_LastBilledExceedsLiveReading = 578,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_OccupancyStatusWrong")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_OccupancyStatusWrong = 579,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_MeterCardSetupWrong")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_MeterCardSetupWrong = 580,

        [Description("FXX.XXX - A09 Flags - A6_BillingControlReport_MeterOfflineMoreThan7Days")]
        F_SystemGeneratedReports_Flags_A6_BillingControlReport_MeterOfflineMoreThan7Days = 581,

        [Description("FXX.XXX - A09 Flags - A7_CreditControlAndNotifierProcess_WalletInArears")]
        F_SystemGeneratedReports_Flags_A7_CreditControlAndNotifierProcess_WalletInArears = 582,

        [Description("FXX.XXX - A09 Flags - A7_CreditControlAndNotifierProcess_MeterMode")]
        F_SystemGeneratedReports_Flags_A7_CreditControlAndNotifierProcess_MeterMode = 583,

        [Description("FXX.XXX - A09 Flags - A7_CreditControlAndNotifierProcess_MeterOnManual")]
        F_SystemGeneratedReports_Flags_A7_CreditControlAndNotifierProcess_MeterOnManual = 584,

        [Description("FXX.XXX - A09 Flags - A10_VirtualMeters_TOUMetersNotBalancing")]
        F_SystemGeneratedReports_Flags_A10_VirtualMeters_TOUMetersNotBalancing = 585,

        [Description("FXX.XXX - A09 Flags - Y0_SiteAdmin_CompaniesNotFilledIn")]
        F_SystemGeneratedReports_Flags_Y0_SiteAdmin_CompaniesNotFilledIn = 586,

        [Description("A2.071 M2M Mirror Recon Summary")]
        A02_MirrorMeterAuditing_M2MMirrorReconSummary = 587,
        [Description("A2.072 M2M Mirror Recon Details")]
        A02_MirrorMeterAuditing_M2MMirrorReconDetails = 588,
        [Description("A2.073 M2M Mirror Recon Review")]
        A02_MirrorMeterAuditing_M2MMirrorReconReview = 589,

        [Description("Device Types")]
        SiteAdmin_DeviceTypes = 590,

        [Description("A2.081 Odo Reading Export")]
        A02_MirrorMeterAuditing_OdoReadingExport = 591,

        [Description("FXX.XXX - Direct Deposits Allocation")]
        F_SystemGeneratedReports_DirectDepositsAllocation = 592,

        [Description("FXX.XXX - Meter Calibration Verification Check")]
        F_SystemGeneratedReports_A02_MirrorMeterAuditing_MeterCalibrationVerificationCheck = 594,

        [Description("E3.011 Commissioning Company Summary")]
        E03_CommissioningTasks_Company_Summary = 595,

        [Description("E3.012 Commissioning Company Details")]
        E03_CommissioningTasks_Company_Details = 596,

        [Description("E3.013 Commissioning Company Results")]
        E03_CommissioningTasks_Company_Results = 597,

        [Description("FXX.XXX - Skybill Customers Utilities Sync")]
        F_SystemGeneratedReports_SkybillJob_CustomersUtilitiesSync = 598,

        [Description("W1.023 Missing Time Allocated Summary")]
        W01_ActivityLogs_MissingTimeAllocated_Summary = 599,

        [Description("Cost To Serve Summary")]
        SOC_CostToServe_Summary = 600,

        [Description("A04.101 Contacts import to 3Cx")]
        A04_CallCentre_ContactsImportTo3Cx = 601,

        [Description("A04.102 Call Log Import")]
        A04_CallCentre_CallLogImport = 602,

        [Description("FXX.XXX - Call Log Sync")]
        F_SystemGeneratedReports_SQLJobs_CallLogSync = 603,

        [Description("A04.103 Call Log Summary Per Property Count")]
        A04_CallCentre_CallLogSummaryPerProperty = 604,

        [Description("A04.104 Call Log Summary Per Property Duration")]
        A04_CallCentre_CallLogSummaryPerPropertyDuration = 605,

        [Description("FXX.XXX - M2MJobs.H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob")]
        F_SystemGeneratedReports_H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob = 606,

        [Description("FXX.XXX - NetcashJobs.CompanyBalanceUpdates")]
        F_SystemGeneratedReports_CompanyBalanceUpdates = 607,

        [Description("FXX.XXX - NetcashJobs.StatementsSync")]
        F_SystemGeneratedReports_StatementsSync = 608,

        [Description("FXX.XXX - ReportingJobs.J_Finance_WinshuttleExportJob")]
        F_SystemGeneratedReports_J_Finance_WinshuttleExportJob = 609,

        [Description("FXX.XXX - ReportingJobs.AF_AfroxAdministration_Metering_Summary_Snapshots")]
        F_SystemGeneratedReports_AF_AfroxAdministration_Metering_Summary_Snapshots = 610,

        [Description("FXX.XXX - SQLJobs.BillingControlReport_OccupancyReset")]
        F_SystemGeneratedReports_SQLJobs_BillingControlReport_OccupancyReset = 611,

        [Description("FXX.XXX - SQLJobs.CreditControlAndNotifierProcess_MeterOnManualReset")]
        F_SystemGeneratedReports_SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset = 612,

        [Description("FXX.XXX - ZendeskJob.UsersSync")]
        F_SystemGeneratedReports_ZendeskJob_UsersSync = 613,

        [Description("FXX.XXX - ZendeskJob.TicketsSync")]
        F_SystemGeneratedReports_ZendeskJob_TicketsSync = 614,

        [Description("FXX.XXX - ZendeskJob.UserFieldsSync")]
        F_SystemGeneratedReports_ZendeskJob_UserFieldsSync = 615,

        [Description("FXX.XXX - ZendeskJob.OrganizationFieldsSync")]
        F_SystemGeneratedReports_ZendeskJob_OrganizationFieldsSync = 616,

        [Description("FXX.XXX - ZendeskJob.TicketFieldsSync")]
        F_SystemGeneratedReports_ZendeskJob_TicketFieldsSync = 617,

        [Description("FXX.XXX - ZendeskJob.TicketFieldOptionsSync")]
        F_SystemGeneratedReports_ZendeskJob_TicketFieldOptionsSync = 618,

        [Description("FXX.XXX - External Charges Scheduling Import")]
        F_SystemGeneratedReports_ExternalChargesSchedulingImport = 619,

        [Description("FXX.XXX - Devices Skybill Billing Sync")]
        F_SystemGeneratedReports_DevicesSkybillBillingSync = 620,

        [Description("FXX.XXX - Netcash Manual Payments Sync")]
        F_SystemGeneratedReports_NetcashManualPaymentsSync = 621,

        [Description("FA0 - Historical Summary")]
        F_SystemGeneratedReports_HistoricalSummary = 622,

        [Description("FXX.XXX - SQLJob.NonComplianceCheck")]
        F_SystemGeneratedReports_SQLJobs_NonComplianceCheck = 623,

        [Description("FXX.XXX - A09 Flags - SkybillResourceListsNoProduct")]
        F_SystemGeneratedReports_Flags_SkybillResourceListsNoProduct = 624,

        [Description("Y01.010 - User Search")]
        Y01_UserAdmin_UserSearch = 625,

        [Description("Y01.011 - User Edit")]
        Y01_UserAdmin_UserEdit = 626,

        [Description("Y01.013 - User Add")]
        Y01_UserAdmin_UserAdd = 627,

        [Description("Y01.012 - Switch To User View")]
        Y01_UserAdmin_UserSwitch = 628,

        [Description("A8.002 Tasks Heatmap")]
        A08_Tasks_Heatmap = 629,

        [Description("A9.002 Flags Heatmap")]
        A09_Flags_Heatmap = 630,

        [Description("FXX.XXX - Sites Sync")]
        F_SystemGeneratedReports_SitesSync = 631,

        [Description("FXX.XXX - A09 Flags  - A04_CallCentreLogs")]
        F_SystemGeneratedReports_Flags_A04_CallCentreLogs = 632,

        [Description("A04.000 Call Log Search")]
        A04_CallCentre_Search = 633,

        [Description("FXX.XXX - Sage Accouting Job - Companies")]
        F_SystemGeneratedReports_SageAccoutingJob_CompaniesSync = 634,

        [Description("FXX.XXX - Sage Accouting Job - Account Categories")]
        F_SystemGeneratedReports_SageAccoutingJob_AccountCategoriesSync = 635,

        [Description("FXX.XXX - Sage Accouting Job - Account Tax Types")]
        F_SystemGeneratedReports_SageAccoutingJob_AccountTaxTypesSync = 636,

        [Description("FXX.XXX - Sage Accouting Job - Accounts")]
        F_SystemGeneratedReports_SageAccoutingJob_AccountsSync = 637,

        [Description("FXX.XXX - Sage Accouting Job - Detailed Ledger Transactions")]
        F_SystemGeneratedReports_SageAccoutingJob_DetailedLedgerTransactionsSync = 638,

        [Description("FXX.XXX - Sage Accouting Job - Ledger Monthlies")]
        F_SystemGeneratedReports_Report_SageLedgerMonthliesSync = 639,

        [Description("C2.111 Sage Report Summary")]
        C02_SageLedgerReport_Summary = 640,

        [Description("C2.112 Sage Report Monthly")]
        C02_SageLedgerReport_Monthly = 641,

        [Description("C2.113 Sage Report Daily")]
        C02_SageLedgerReport_Daily = 642,

        [Description("C2.113 Sage Report Details")]
        C02_SageLedgerReport_Details = 643,

        [Description("C2.010 Consolidated TB")]
        C02_SageLedgerReport_ConsolidatedTB = 644,

        [Description("C2.015 GL Audit View")]
        C02_SageLedgerReport_GLAuditView = 645,

        [Description("C2.016 GL Audit Update")]
        C02_SageLedgerReport_GLAuditUpdate = 646,

        [Description("Sage Management Accounts")]
        SiteAdmin_SageManagementAccounts_ReportingCategories = 647,

        [Description("C2.111 Sage Report Monthly Summary")]
        C02_SageLedgerReport_MonthlySummary = 648,

        [Description("FXX.XXX - Sage Accouting Job - Journal Request")]
        F_SystemGeneratedReports_SageAccoutingJob_SageAccounting_JournalRequestJob = 649,

        [Description("FXX.XXX - A09 Flags - A01_GatewayAndDeviceCombined")]
        F_SystemGeneratedReports_Flags_A01_GatewayAndDeviceCombined = 650,
 
        [Description("FXX.XXX - Call Recordings Sync")]
        F_SystemGeneratedReports_SQLJobs_CallLogSync_Recordings = 651,

        [Description("A04.107 Call Centre Recordings Allocation")]
        A04_CallCentre_RecordingsAllocation = 652,
 
        [Description("FXX.XXX - Zendesk Tickets Data Dump")]
        F_SystemGeneratedReports_ZendeskTicketsDataDump = 653,

        [Description("A04.106 Call Log Summary Per Query Type Duration")]
        A04_CallCentre_CallLogSummaryPerQueryType = 654,

        [Description("A04.105 Call Log Summary Per Query Type Count")]
        A04_CallCentre_CallLogSummaryPerQueryTypeCount = 655,
 
        [Description("FXX.XXX - A01 Gateway And Device Combined Requests")]
        F_SystemGeneratedReports_A01_GatewayAndDeviceCombinedJob = 656,
    }

    public enum SecureAreaActionEnum : int
    {
        [Description("View")]
        View = 1,
        [Description("Add")]
        Add = 2,
        [Description("Edit")]
        Edit = 3,
        [Description("Delete")]
        Delete = 4,
        [Description("Approval")]
        Approval = 5,
        [Description("Management Approval")]
        ManagementApproval = 6,
    }

    public class ParentSecureAreaDefaults
    {
        public static Dictionary<Tuple<ParentSecureAreaEnum, string, string>, List<Tuple<SecureAreaEnum, string, string>>> DefaultSecureAreas
        {
            get
            {
                Dictionary<Tuple<ParentSecureAreaEnum, string, string>, List<Tuple<SecureAreaEnum, string, string>>> defaultSecureAreas = new Dictionary<Tuple<ParentSecureAreaEnum, string, string>, List<Tuple<SecureAreaEnum, string, string>>>();

                #region Site Admin

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.SiteAdmin, "", ParentSecureAreaEnum.SiteAdmin.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.UserAdmin, "", SecureAreaEnum.UserAdmin.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_A09_FlagTypes, "", SecureAreaEnum.SiteAdmin_A09_FlagTypes.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Company_CostSettings, "", "Company Cost Settings"),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Imports_RentalDataDump, "", SecureAreaEnum.SiteAdmin_Imports_RentalDataDump.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_TOU, "", SecureAreaEnum.SiteAdmin_TOU.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_LoginMessages, "", SecureAreaEnum.SiteAdmin_LoginMessages.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_BuildingCycles, "", SecureAreaEnum.SiteAdmin_BuildingCycles.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_BuildingCouncilTypes, "", SecureAreaEnum.SiteAdmin_BuildingCouncilTypes.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_BuildingDetails, "", SecureAreaEnum.SiteAdmin_BuildingDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Products, "", SecureAreaEnum.SiteAdmin_Products.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_ProductsSkybillResources, "", SecureAreaEnum.SiteAdmin_ProductsSkybillResources.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Partners, "", SecureAreaEnum.SiteAdmin_Partners.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Companies, "", SecureAreaEnum.SiteAdmin_Companies.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_MeterTypes, "", SecureAreaEnum.SiteAdmin_MeterTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_PaymentMethodsSkybillJournalNos, "", SecureAreaEnum.SiteAdmin_PaymentMethodsSkybillJournalNos.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_A08TaskTypes, "", SecureAreaEnum.SiteAdmin_A08TaskTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Vehicles, "", SecureAreaEnum.SiteAdmin_Vehicles.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, "", SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_SafetyFileQuestions, "", SecureAreaEnum.SiteAdmin_SafetyFileQuestions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_UsageToolCalcAssets, "", SecureAreaEnum.SiteAdmin_UsageToolCalcAssets.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Priorities, "", SecureAreaEnum.SiteAdmin_Priorities.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_StatusGroups, "", SecureAreaEnum.SiteAdmin_StatusGroups.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_E01_BuildingOnboardingTaskTypes, "", SecureAreaEnum.SiteAdmin_E01_BuildingOnboardingTaskTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_WorkflowGroups, "", SecureAreaEnum.SiteAdmin_WorkflowGroups.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_SkybillCustomersUtilities, "", SecureAreaEnum.SiteAdmin_SkybillCustomersUtilities.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Imports_RentalExpenses, "", SecureAreaEnum.SiteAdmin_Imports_RentalExpenses.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump, "", SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_ManagementAccounts_ReportingCategories, "", SecureAreaEnum.SiteAdmin_ManagementAccounts_ReportingCategories.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_CompanyTypes, "", SecureAreaEnum.SiteAdmin_CompanyTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_DeviceAPIs, "", SecureAreaEnum.SiteAdmin_DeviceAPIs.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Workflows, "", SecureAreaEnum.SiteAdmin_Workflows.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_ContactorStateHacks, "", SecureAreaEnum.SiteAdmin_ContactorStateHacks.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Municipalities, "", SecureAreaEnum.SiteAdmin_Municipalities.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_Suburbs, "", SecureAreaEnum.SiteAdmin_Suburbs.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_MeetingAgendaGroups, "", SecureAreaEnum.SiteAdmin_MeetingAgendaGroups.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes, "", SecureAreaEnum.SiteAdmin_D02_SaleTaskTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_DeviceTypes, "", SecureAreaEnum.SiteAdmin_DeviceTypes.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SiteAdmin_SageManagementAccounts_ReportingCategories, "", SecureAreaEnum.SiteAdmin_SageManagementAccounts_ReportingCategories.GetDescription()),
                    });

                #endregion

                #region Y01_UserAdmin

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.Y01_UserAdmin, "", ParentSecureAreaEnum.Y01_UserAdmin.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Y01_UserAdmin_UserSearch, "", SecureAreaEnum.Y01_UserAdmin_UserSearch.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Y01_UserAdmin_UserEdit, "", SecureAreaEnum.Y01_UserAdmin_UserEdit.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Y01_UserAdmin_UserSwitch, "", SecureAreaEnum.Y01_UserAdmin_UserSwitch.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Y01_UserAdmin_UserAdd, "", SecureAreaEnum.Y01_UserAdmin_UserAdd.GetDescription()),
                    });

                #endregion

                #region 00 SOC 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.SOC, "", ParentSecureAreaEnum.SOC.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SOC_Summary, "", SecureAreaEnum.SOC_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.SOC_CostToServe_Summary, "", SecureAreaEnum.SOC_CostToServe_Summary.GetDescription()),
                    });


                #endregion

                #region Customer

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.Customer, "user-circle", ParentSecureAreaEnum.Customer.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_SwitchToCustomerView, "sign-in-alt", SecureAreaEnum.Customer_SwitchToCustomerView.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Dashboard, "desktop", SecureAreaEnum.Customer_Dashboard.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Map, "globe-africa", SecureAreaEnum.Customer_Map.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Billing, "file-alt", SecureAreaEnum.Customer_Billing.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Usage, "tachometer-alt", SecureAreaEnum.Customer_Usage.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Statement, "file-excel", SecureAreaEnum.Customer_Statement.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Recharge, "dollar-sign", SecureAreaEnum.Customer_Recharge.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_ContactServiceProvider, "phone", SecureAreaEnum.Customer_ContactServiceProvider.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_HistoricalDataSummary, "chart-bar", SecureAreaEnum.Customer_HistoricalDataSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Profile, "user-circle", SecureAreaEnum.Customer_Profile.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_ProtestTool, "angry", SecureAreaEnum.Customer_ProtestTool.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_UsageCalculator, "calculator", SecureAreaEnum.Customer_UsageCalculator.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_BalanceRedeem, "dollar-sign", SecureAreaEnum.Customer_BalanceRedeem.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Tariff, "dollar-sign", SecureAreaEnum.Customer_Tariff.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_AccountStatement, "file-invoice", SecureAreaEnum.Customer_AccountStatement.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Customer_Insights, "search-plus", SecureAreaEnum.Customer_Insights.GetDescription()),
                    });

                #endregion

                #region Dashboards

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.Dashboards, "", ParentSecureAreaEnum.Dashboards.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Dashboards_A, "", SecureAreaEnum.Dashboards_A.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Dashboards_B, "", SecureAreaEnum.Dashboards_B.GetDescription()),
                    });

                #endregion

                #region A - Workflows

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A01_GatewayAndDeviceMonitoring, "", ParentSecureAreaEnum.A01_GatewayAndDeviceMonitoring.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayDetails, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayVerification, "", "A1.013 Gateway Verification"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayResults, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayResults.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceDetails, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceVerification, "", "A1.023 Device Verification"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceResults, "", SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceResults.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A02_MirrorMeterAuditing, "", ParentSecureAreaEnum.A02_MirrorMeterAuditing.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary, "", SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails, "", SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationVerification, "", SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationResults, "", SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationResults.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDelete.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingSummary, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDetails, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingVerification, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingResults, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistSummary, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistDetails, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistResults, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorChecklistResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSearch, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSearch.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceDetails, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd, "", SecureAreaEnum.A02_MirrorMeterAuditing_MirrorDeviceAdd.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary, "", SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconDetails, "", SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview, "", SecureAreaEnum.A02_MirrorMeterAuditing_M2MMirrorReconReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A02_MirrorMeterAuditing_OdoReadingExport, "", SecureAreaEnum.A02_MirrorMeterAuditing_OdoReadingExport.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A03_NetworkBalancing, "", ParentSecureAreaEnum.A03_NetworkBalancing.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Summary, "", SecureAreaEnum.A03_NetworkBalancing_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Details, "", SecureAreaEnum.A03_NetworkBalancing_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Capture, "", SecureAreaEnum.A03_NetworkBalancing_Capture.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Units_Summary, "", SecureAreaEnum.A03_NetworkBalancing_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Units_Monthly, "", SecureAreaEnum.A03_NetworkBalancing_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A03_NetworkBalancing_Units_Daily, "", SecureAreaEnum.A03_NetworkBalancing_Units_Daily.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A04_Tickets, "", ParentSecureAreaEnum.A04_Tickets.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsSummary, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsDetails, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsReview, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsResults, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsIncomplete, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsIncomplete.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketsIncompleteClosed, "", SecureAreaEnum.A04_Tickets_ZendeskTicketsIncompleteClosed.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskAgentsSummary, "", SecureAreaEnum.A04_Tickets_ZendeskAgentsSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskAgentsDetails, "", SecureAreaEnum.A04_Tickets_ZendeskAgentsDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskAgentsResults, "", SecureAreaEnum.A04_Tickets_ZendeskAgentsResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketCategorySummary, "", SecureAreaEnum.A04_Tickets_ZendeskTicketCategorySummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryDetails, "", SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryResults, "", SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryResults.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A04_CallCentre, "", ParentSecureAreaEnum.A04_CallCentre.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_ContactsImportTo3Cx, "", SecureAreaEnum.A04_CallCentre_ContactsImportTo3Cx.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_CallLogImport, "", SecureAreaEnum.A04_CallCentre_CallLogImport.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_CallLogSummaryPerProperty, "", SecureAreaEnum.A04_CallCentre_CallLogSummaryPerProperty.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_CallLogSummaryPerPropertyDuration, "", SecureAreaEnum.A04_CallCentre_CallLogSummaryPerPropertyDuration.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_Search, "", SecureAreaEnum.A04_CallCentre_Search.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryTypeCount, "", SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryTypeCount.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryType, "", SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryType.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A04_CallCentre_RecordingsAllocation, "", SecureAreaEnum.A04_CallCentre_RecordingsAllocation.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A05_Exceptions, "", ParentSecureAreaEnum.A05_Exceptions.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Summary, "", SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Details, "", SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Results, "", SecureAreaEnum.A05_Exceptions_MidnightSyncHistory_Results.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A06_BillingControlReport, "", ParentSecureAreaEnum.A06_BillingControlReport.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_OccupancySummary, "", SecureAreaEnum.A06_BillingControlReport_OccupancySummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, "", SecureAreaEnum.A06_BillingControlReport_OccupancyDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_OccupancyVerification, "", SecureAreaEnum.A06_BillingControlReport_OccupancyVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_OccupancyResults, "", SecureAreaEnum.A06_BillingControlReport_OccupancyResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_NotBilledSummary, "", SecureAreaEnum.A06_BillingControlReport_NotBilledSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, "", SecureAreaEnum.A06_BillingControlReport_NotBilledDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_NotBilledVerification, "", SecureAreaEnum.A06_BillingControlReport_NotBilledVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_NotBilledResults, "", SecureAreaEnum.A06_BillingControlReport_NotBilledResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary, "", SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_BillingBlockedDetails, "", SecureAreaEnum.A06_BillingControlReport_BillingBlockedDetails.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_DailyBillingOverview_Summary, "", SecureAreaEnum.A06_BillingControlReport_DailyBillingOverview_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_TarrifReview, "", SecureAreaEnum.A06_BillingControlReport_TarrifReview.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary, "", SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Details, "", SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Results, "", SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Results.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A07_CreditControlAndNotifierProcess, "", ParentSecureAreaEnum.A07_CreditControlAndNotifierProcess.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlDetails, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlReview, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlResults, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeDetails, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeResults, "", "A7.034 - Meter Mode Results"),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualDetails, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualReview, "", "A7.033 - Meter On Manual Review"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualResults, "", "A7.034 - Meter OnManual Results"),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults, "", SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Summary, "",SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Details, "",SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Results, "",SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results, "",SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterContactorState, "",SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterContactorState.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A08_Tasks, "", ParentSecureAreaEnum.A08_Tasks.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Search, "", SecureAreaEnum.A08_Tasks_Search.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Company_Summary, "", SecureAreaEnum.A08_Tasks_Company_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Company_Details, "", SecureAreaEnum.A08_Tasks_Company_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Company_Results, "", SecureAreaEnum.A08_Tasks_Company_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_User_Summary, "", SecureAreaEnum.A08_Tasks_User_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_User_Details, "", SecureAreaEnum.A08_Tasks_User_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_User_Results, "", SecureAreaEnum.A08_Tasks_User_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Type_Summary, "", SecureAreaEnum.A08_Tasks_Type_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Type_Details, "", SecureAreaEnum.A08_Tasks_Type_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Type_Results, "", SecureAreaEnum.A08_Tasks_Type_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Task_Review, "", SecureAreaEnum.A08_Task_Review.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Task_Create, "", SecureAreaEnum.A08_Task_Create.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Overview, "", SecureAreaEnum.A08_Tasks_Overview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A08_Tasks_Heatmap, "", SecureAreaEnum.A08_Tasks_Heatmap.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A09_Flags, "", ParentSecureAreaEnum.A09_Flags.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_Search, "", SecureAreaEnum.A09_Flags_Search.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_CompanySummary, "", SecureAreaEnum.A09_Flags_CompanySummary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_CompanyDetails, "", SecureAreaEnum.A09_Flags_CompanyDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_CompanyResults, "", SecureAreaEnum.A09_Flags_CompanyResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_UserSummary, "", SecureAreaEnum.A09_Flags_UserSummary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_UserDetails, "", SecureAreaEnum.A09_Flags_UserDetails.GetDescription()),
                        ////new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_UserReview, "", "A9.023 User Review"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_UserResults, "", SecureAreaEnum.A09_Flags_UserResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_TypeSummary, "", SecureAreaEnum.A09_Flags_TypeSummary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_TypeDetails, "", SecureAreaEnum.A09_Flags_TypeDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_TypeResults, "", SecureAreaEnum.A09_Flags_TypeResults.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_Create, "", SecureAreaEnum.A09_Flags_Create.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_CompanyReview, "", SecureAreaEnum.A09_Flags_CompanyReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A09_Flags_Heatmap, "", SecureAreaEnum.A09_Flags_Heatmap.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.A10_VirtualMeters, "", ParentSecureAreaEnum.A10_VirtualMeters.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_Details, "", SecureAreaEnum.A10_VirtualMeters_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_Create, "", SecureAreaEnum.A10_VirtualMeters_Create.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_Review, "", SecureAreaEnum.A10_VirtualMeters_Review.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Summary, "", SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Details, "", SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Review, "", SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Review.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Monthly, "", SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Monthly.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_MergedMeters_Details, "", SecureAreaEnum.A10_MergedMeters_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_MergedMeters_Create, "", SecureAreaEnum.A10_MergedMeters_Create.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.A10_MergedMeters_Review, "", SecureAreaEnum.A10_MergedMeters_Review.GetDescription()),
                    });

                #endregion

                #region AF - AfroxAdministration 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.AF_AfroxAdministration, "", ParentSecureAreaEnum.AF_AfroxAdministration.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, "", SecureAreaEnum.AF_AfroxAdministration_Metering_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_Metering_Details, "", SecureAreaEnum.AF_AfroxAdministration_Metering_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, "", SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_WinshuttleExportRequests, "", SecureAreaEnum.AF_AfroxAdministration_WinshuttleExportRequests.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_AddDevice, "", SecureAreaEnum.AF_AfroxAdministration_AddDevice.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_NewlyAddedDevices, "", SecureAreaEnum.AF_AfroxAdministration_NewlyAddedDevices.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_DeviceReview, "", SecureAreaEnum.AF_AfroxAdministration_DeviceReview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_OverallStats, "", SecureAreaEnum.AF_AfroxAdministration_OverallStats.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_MeterReadingSummary, "", SecureAreaEnum.AF_AfroxAdministration_MeterReadingSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_MeterReadingDetails, "", SecureAreaEnum.AF_AfroxAdministration_MeterReadingDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_MeterReadingVerification, "", SecureAreaEnum.AF_AfroxAdministration_MeterReadingVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate, "", SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_GasNetworkBalancing_Daily, "", SecureAreaEnum.AF_AfroxAdministration_GasNetworkBalancing_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO, "", SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_Supply_Details, "", SecureAreaEnum.AF_AfroxAdministration_Supply_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_ACOStatuses, "", SecureAreaEnum.AF_AfroxAdministration_ACOStatuses.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction, "", SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_SnapshotEmails, "", SecureAreaEnum.AF_AfroxAdministration_SnapshotEmails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_ACO_StatusHack, "", SecureAreaEnum.AF_AfroxAdministration_ACO_StatusHack.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.AF_AfroxAdministration_Metering_Review, "", SecureAreaEnum.AF_AfroxAdministration_Metering_Review.GetDescription()),
                    });

                #endregion

                #region B - Supply

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.B01_SupplyAccountPayments, "", ParentSecureAreaEnum.B01_SupplyAccountPayments.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_AccountPaymentSummary, "", SecureAreaEnum.B01_AccountPayments_AccountPaymentSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, "", SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture, "", SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsSummary, "", SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails, "", SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplates, "", SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplates.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails, "", SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary, "", SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.B02_CouncilReadings, "", ParentSecureAreaEnum.B02_CouncilReadings.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingSummary, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingUpdate.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingVerification, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingVerification.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingResults, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingResults.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary, "", SecureAreaEnum.B02_CouncilDevice_CouncilMetersSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilDevice_CouncilMetersDetails, "", SecureAreaEnum.B02_CouncilDevice_CouncilMetersDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilDevice_CouncilMetersReview, "", SecureAreaEnum.B02_CouncilDevice_CouncilMetersReview.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B02_CouncilReadings_CouncilReadingPlanner, "", SecureAreaEnum.B02_CouncilReadings_CouncilReadingPlanner.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.B03_SupplyCouncilStatements, "", ParentSecureAreaEnum.B03_SupplyCouncilStatements.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementSummary, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementDetails, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementReport, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementReport.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilReconReport, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilReconReport.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementAccountingDetails, "", SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementAccountingDetails.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.B04_SupplyReconciliation, "", ParentSecureAreaEnum.B04_SupplyReconciliation.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckRecon, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckRecon.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckReconDetails, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckReconDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments, "", SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary, "", SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All, "", SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement, "", SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.B05_SupplyPayments, "", ParentSecureAreaEnum.B05_SupplyPayments.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_AccountPayments_PaymentSummary, "", SecureAreaEnum.B05_AccountPayments_PaymentSummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_AccountPayments_PaymentDetails, "", SecureAreaEnum.B05_AccountPayments_PaymentDetails.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_AccountPayments_PaymentCapture, "", SecureAreaEnum.B05_AccountPayments_PaymentCapture.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_SupplyPayments_PaymentForecast, "", SecureAreaEnum.B05_SupplyPayments_PaymentForecast.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_SupplyPayments_CashflowForecast, "", SecureAreaEnum.B05_SupplyPayments_CashflowForecast.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.B05_SupplyPayments_PaymentMatchingSchedule, "", SecureAreaEnum.B05_SupplyPayments_PaymentMatchingSchedule.GetDescription()),
                    });


                #endregion

                #region C - Reports

                #region C01_ProductReport

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C01_ProductReport, "", ParentSecureAreaEnum.C01_ProductReport.GetDescription()),
                           new List<Tuple<SecureAreaEnum, string, string>>()
                           {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Summary, "", SecureAreaEnum.C01_ProductReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Details, "", SecureAreaEnum.C01_ProductReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Daily, "", SecureAreaEnum.C01_ProductReport_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Details_Details, "", SecureAreaEnum.C01_ProductReport_Details_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Details_Details_Units, "", SecureAreaEnum.C01_ProductReport_Details_Details_Units.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Details_Details_AVG, "", SecureAreaEnum.C01_ProductReport_Details_Details_AVG.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Monthly_Details, "", SecureAreaEnum.C01_ProductReport_Monthly_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Monthly_Details_Units, "", SecureAreaEnum.C01_ProductReport_Monthly_Details_Units.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_Monthly_Details_AVG, "", SecureAreaEnum.C01_ProductReport_Monthly_Details_AVG.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_AuditSyncReport_Summary, "", SecureAreaEnum.C01_ProductReport_AuditSyncReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_AuditSyncReport_Details, "", SecureAreaEnum.C01_ProductReport_AuditSyncReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C01_ProductReport_ProductAuditView, "", SecureAreaEnum.C01_ProductReport_ProductAuditView.GetDescription()),
                           });

                #endregion

                #region C02_GeneralLedgerReport

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C02_GeneralLedgerReport, "", ParentSecureAreaEnum.C02_GeneralLedgerReport.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_Summary, "", SecureAreaEnum.C02_GeneralLedgerReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_Monthly, "", SecureAreaEnum.C02_GeneralLedgerReport_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_Daily, "", SecureAreaEnum.C02_GeneralLedgerReport_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_Details, "", SecureAreaEnum.C02_GeneralLedgerReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_ConsolidatedTB, "", SecureAreaEnum.C02_GeneralLedgerReport_ConsolidatedTB.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_GLAuditView, "", SecureAreaEnum.C02_GeneralLedgerReport_GLAuditView.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate, "", SecureAreaEnum.C02_GeneralLedgerReport_GLAuditUpdate.GetDescription()),
                    });

                #endregion

                #region C02_SageLedgerReport

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C02_SageLedgerReport, "", ParentSecureAreaEnum.C02_SageLedgerReport.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_Summary, "", SecureAreaEnum.C02_SageLedgerReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_Monthly, "", SecureAreaEnum.C02_SageLedgerReport_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_MonthlySummary, "", SecureAreaEnum.C02_SageLedgerReport_MonthlySummary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_Daily, "", SecureAreaEnum.C02_SageLedgerReport_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_Details, "", SecureAreaEnum.C02_SageLedgerReport_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_ConsolidatedTB, "", SecureAreaEnum.C02_SageLedgerReport_ConsolidatedTB.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_GLAuditView, "", SecureAreaEnum.C02_SageLedgerReport_GLAuditView.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C02_SageLedgerReport_GLAuditUpdate, "", SecureAreaEnum.C02_SageLedgerReport_GLAuditUpdate.GetDescription()),
                    });

                #endregion

                #region C03_Report

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C03_Report, "", ParentSecureAreaEnum.C03_Report.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_AverageAndPerUnitReport_Summary, "", SecureAreaEnum.C03_AverageAndPerUnitReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_Report_UserAndMeter_Summary, "", SecureAreaEnum.C03_Report_UserAndMeter_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_Report_ReceiptPerProperty_Summary, "", SecureAreaEnum.C03_Report_ReceiptPerProperty_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_Report_ReceiptPerProperty_Details, "", SecureAreaEnum.C03_Report_ReceiptPerProperty_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Summary, "", SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Details, "", SecureAreaEnum.C03_Report_ReceiptPerPropertyVolumes_Details.GetDescription()),
                    });

                #endregion

                #region C04_OperationalProfitReport

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C04_OperationalProfitReport, "", ParentSecureAreaEnum.C04_OperationalProfitReport.GetDescription()),
                     new List<Tuple<SecureAreaEnum, string, string>>()
                     {
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C04_OperationalProfitReport_Summary, "", SecureAreaEnum.C04_OperationalProfitReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C04_OperationalProfitReport_Details, "", SecureAreaEnum.C04_OperationalProfitReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C04_OperationalPropertyProfitReport_Details, "", SecureAreaEnum.C04_OperationalPropertyProfitReport_Details.GetDescription()),
                     });

                #endregion

                #region C05_MonthlyManualInvoicing 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C05_MonthlyManualInvoicing, "", ParentSecureAreaEnum.C05_MonthlyManualInvoicing.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary, "", SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Details, "", SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture, "", SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C_MonthlyManualInvoicing_BillingsToOwner_Results, "", "C5.014 Billings to Owner Results"),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C05_MonthlyManualInvoicing_MonthlyManagementFees_Details, "", SecureAreaEnum.C05_MonthlyManualInvoicing_MonthlyManagementFees_Details.GetDescription()),

                    }); ;


                #endregion

                #region C06_LedgerReconReport

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C06_LedgerReconReport, "", ParentSecureAreaEnum.C06_LedgerReconReport.GetDescription()),
                             new List<Tuple<SecureAreaEnum, string, string>>()
                             {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C06_LedgerReconReport_Summary, "", SecureAreaEnum.C06_LedgerReconReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C06_LedgerReconReport_Details, "", SecureAreaEnum.C06_LedgerReconReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C06_LedgerReconReport_Daily, "", SecureAreaEnum.C06_LedgerReconReport_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C06_TBGLReconReport_Summary, "", SecureAreaEnum.C06_TBGLReconReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C06_TBGLReconReport_Details, "", SecureAreaEnum.C06_TBGLReconReport_Details.GetDescription()),
                             });

                #endregion

                #region C07_ManagementAccounts

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C07_ManagementAccounts, "", ParentSecureAreaEnum.C07_ManagementAccounts.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_Summary, "", SecureAreaEnum.C07_ManagementAccounts_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Monthly, "", SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Yearly, "", SecureAreaEnum.C07_ManagementAccounts_GrandFinale_Yearly.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_Details, "", SecureAreaEnum.C07_ManagementAccounts_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_OperatingExpenses, "", SecureAreaEnum.C07_ManagementAccounts_OperatingExpenses.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C07_ManagementAccounts_OtherExpenses, "", SecureAreaEnum.C07_ManagementAccounts_OtherExpenses.GetDescription()),
                    });

                #endregion

                #region C08_Forecasting

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C08_Forecasting, "", ParentSecureAreaEnum.C08_Forecasting.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C08_Forecasting_Summary, "", SecureAreaEnum.C08_Forecasting_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C08_Forecasting_Details, "", SecureAreaEnum.C08_Forecasting_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C08_Forecasting_DetailsCalculationDetails, "", SecureAreaEnum.C08_Forecasting_DetailsCalculationDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C08_Forecasting_Baselines, "", SecureAreaEnum.C08_Forecasting_Baselines.GetDescription()),
                    });

                #endregion

                #region C09_CashflowForecast

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.C09_CashflowForecast, "", ParentSecureAreaEnum.C09_CashflowForecast.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C09_CashflowForecast_Summary, "", SecureAreaEnum.C09_CashflowForecast_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C09_CashflowForecast_UpdateBankBalances, "", SecureAreaEnum.C09_CashflowForecast_UpdateBankBalances.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C09_CashflowForecast_Daily, "", SecureAreaEnum.C09_CashflowForecast_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C09_CashflowForecast_ConsolidatedNetcashBalances, "", SecureAreaEnum.C09_CashflowForecast_ConsolidatedNetcashBalances.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.C09_CashflowForecast_ConsolidatedBankBalances, "", SecureAreaEnum.C09_CashflowForecast_ConsolidatedBankBalances.GetDescription()),
                    });

                #endregion

                #endregion

                #region D - Leads

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.D01_Leads, "", ParentSecureAreaEnum.D01_Leads.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_LogLead, "", SecureAreaEnum.D01_Leads_LogLead.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_MyLeads, "", SecureAreaEnum.D01_Leads_MyLeads.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_ViewLead, "", SecureAreaEnum.D01_Leads_ViewLead.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_LeadGenerators, "", SecureAreaEnum.D01_Leads_LeadGenerators.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Products, "", SecureAreaEnum.D01_Leads_Products.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Properties, "", SecureAreaEnum.D01_Leads_Properties.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Contacts, "", SecureAreaEnum.D01_Leads_Contacts.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Import, "", SecureAreaEnum.D01_Leads_Import.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_ManagingAgents, "", SecureAreaEnum.D01_Leads_ManagingAgents.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Competitors, "", SecureAreaEnum.D01_Leads_Competitors.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Commissions_Setup, "", SecureAreaEnum.D01_Leads_Commissions_Setup.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Commissions_Details, "", SecureAreaEnum.D01_Leads_Commissions_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Commissions_Summary, "", SecureAreaEnum.D01_Leads_Commissions_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D01_Leads_Snapshots, "", SecureAreaEnum.D01_Leads_Snapshots.GetDescription()),
                    });

                #endregion

                #region D02_Sale

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.D02_Sale, "", ParentSecureAreaEnum.D02_Sale.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Company_Summary, "", SecureAreaEnum.D02_SaleTasks_Company_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Company_Details, "", SecureAreaEnum.D02_SaleTasks_Company_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Company_Results, "", SecureAreaEnum.D02_SaleTasks_Company_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_User_Summary, "", SecureAreaEnum.D02_SaleTasks_User_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_User_Details, "", SecureAreaEnum.D02_SaleTasks_User_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_User_Results, "", SecureAreaEnum.D02_SaleTasks_User_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Type_Summary, "", SecureAreaEnum.D02_SaleTasks_Type_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Type_Details, "", SecureAreaEnum.D02_SaleTasks_Type_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Type_Results, "", SecureAreaEnum.D02_SaleTasks_Type_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTask_Review, "", SecureAreaEnum.D02_SaleTask_Review.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_Sale_Answers, "", SecureAreaEnum.D02_Sale_Answers.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E02_SafetyFile_Answers, "", SecureAreaEnum.E02_SafetyFile_Answers.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.D02_SaleTasks_Company_Onboarding, "", SecureAreaEnum.D02_SaleTasks_Company_Onboarding.GetDescription()),
                    });

                #endregion

                #region E - Building Onboarding / Commissioning

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.E01_BuildingOnboarding, "", ParentSecureAreaEnum.E01_BuildingOnboarding.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Summary, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Details, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Results, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_User_Summary, "", SecureAreaEnum.E01_BuildingOnboardingTasks_User_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_User_Details, "", SecureAreaEnum.E01_BuildingOnboardingTasks_User_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_User_Results, "", SecureAreaEnum.E01_BuildingOnboardingTasks_User_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Summary, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Details, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Results, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Type_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTask_Review, "", SecureAreaEnum.E01_BuildingOnboardingTask_Review.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboarding_Answers, "", SecureAreaEnum.E01_BuildingOnboarding_Answers.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E02_SafetyFile_Answers, "", SecureAreaEnum.E02_SafetyFile_Answers.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Onboarding, "", SecureAreaEnum.E01_BuildingOnboardingTasks_Company_Onboarding.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.E03_Commissioning, "", ParentSecureAreaEnum.E03_Commissioning.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E03_CommissioningTasks_Company_Summary, "", SecureAreaEnum.E03_CommissioningTasks_Company_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E03_CommissioningTasks_Company_Details, "", SecureAreaEnum.E03_CommissioningTasks_Company_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.E03_CommissioningTasks_Company_Results, "", SecureAreaEnum.E03_CommissioningTasks_Company_Results.GetDescription()),
                    });

                #endregion

                #region F - System 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.F_SystemGeneratedReports, "", ParentSecureAreaEnum.F_SystemGeneratedReports.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_AllReports, "", SecureAreaEnum.F_SystemGeneratedReports_AllReports.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary, "", SecureAreaEnum.F_SystemGeneratedReports_HistoricalSummary.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_OfflineGatewaysAndDevices, "", SecureAreaEnum.F_SystemGeneratedReports_OfflineGatewaysAndDevices.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_GatewaysAndDevicesSync, "", SecureAreaEnum.F_SystemGeneratedReports_GatewaysAndDevicesSync.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MirrorAudit, "", SecureAreaEnum.F_SystemGeneratedReports_MirrorAudit.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_DeviceReadingUpdater, "", SecureAreaEnum.F_SystemGeneratedReports_DeviceReadingUpdater.GetDescription()),


                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MidnightSync, "", SecureAreaEnum.F_SystemGeneratedReports_MidnightSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncTechLoss, "", SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncTechLoss.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncPQAllocation, "", SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncPQAllocation.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncNextDay, "", SecureAreaEnum.F_SystemGeneratedReports_MidnightSyncNextDay.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_BillingControl, "", SecureAreaEnum.F_SystemGeneratedReports_BillingControl.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_BillingControlDetail, "", SecureAreaEnum.F_SystemGeneratedReports_BillingControlDetail.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_CreditControl, "", SecureAreaEnum.F_SystemGeneratedReports_CreditControl.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_NotifierAndDisconnector, "", SecureAreaEnum.F_SystemGeneratedReports_NotifierAndDisconnector.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Connector, "", SecureAreaEnum.F_SystemGeneratedReports_Connector.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_ReceiptReport, "", SecureAreaEnum.F_SystemGeneratedReports_ReceiptReport.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_StockReport, "", SecureAreaEnum.F_SystemGeneratedReports_StockReport.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_GenLedgerSync, "", SecureAreaEnum.F_SystemGeneratedReports_GenLedgerSync.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_RentalBook, "", SecureAreaEnum.F_SystemGeneratedReports_RentalBook.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_RentalDataDump, "", SecureAreaEnum.F_SystemGeneratedReports_RentalDataDump.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_DevicesMaster, "", SecureAreaEnum.F_SystemGeneratedReports_DevicesMaster.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_BuildingsMaster, "", SecureAreaEnum.F_SystemGeneratedReports_BuildingsMaster.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SkybillCustomersSync, "", SecureAreaEnum.F_SystemGeneratedReports_SkybillCustomersSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceListsSync, "", SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceListsSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_ArchivingJob_TokenLog, "", SecureAreaEnum.F_SystemGeneratedReports_ArchivingJob_TokenLog.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync, "", SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerMonthliesSync, "", SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerMonthliesSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerCustomerMonthliesSync, "", SecureAreaEnum.F_SystemGeneratedReports_Report_ProductsResourceLedgerCustomerMonthliesSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Report_SupplyCostMonthliesSync, "", SecureAreaEnum.F_SystemGeneratedReports_Report_SupplyCostMonthliesSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync, "", SecureAreaEnum.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SkybillJob_ChartOfAccountsSnapshot, "", SecureAreaEnum.F_SystemGeneratedReports_SkybillJob_ChartOfAccountsSnapshot.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_A09FlagsDataDump, "", SecureAreaEnum.F_SystemGeneratedReports_A09FlagsDataDump.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MeterActiveEnergyAnomalies, "", SecureAreaEnum.F_SystemGeneratedReports_MeterActiveEnergyAnomalies.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_PulseCounterCorrector, "", SecureAreaEnum.F_SystemGeneratedReports_PulseCounterCorrector.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SkybillPaymentAllocations, "", SecureAreaEnum.F_SystemGeneratedReports_SkybillPaymentAllocations.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Unipin_Daily, "", SecureAreaEnum.F_SystemGeneratedReports_Unipin_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Unipin_Weekly, "", SecureAreaEnum.F_SystemGeneratedReports_Unipin_Weekly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Unipin_Monthly, "", SecureAreaEnum.F_SystemGeneratedReports_Unipin_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_Skybill_DailyBilling, "", SecureAreaEnum.F_SystemGeneratedReports_Skybill_DailyBilling.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_LevelsChecker, "", SecureAreaEnum.F_SystemGeneratedReports_LevelsChecker.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SupplementaryInfoDump, "", SecureAreaEnum.F_SystemGeneratedReports_SupplementaryInfoDump.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_MeterSerialNumberConfirmation, "", SecureAreaEnum.F_SystemGeneratedReports_MeterSerialNumberConfirmation.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_SOCSnapshot, "", SecureAreaEnum.F_SystemGeneratedReports_SOCSnapshot.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_CostSettingsTemplatesCopier, "", SecureAreaEnum.F_SystemGeneratedReports_CostSettingsTemplatesCopier.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_TOUReadingFixer, "", SecureAreaEnum.F_SystemGeneratedReports_TOUReadingFixer.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_CreateBTRJournals, "", SecureAreaEnum.F_SystemGeneratedReports_CreateBTRJournals.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_StatementsSkybillAndPaymentsSync, "", SecureAreaEnum.F_SystemGeneratedReports_StatementsSkybillAndPaymentsSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_AccountingChecklists, "", SecureAreaEnum.F_SystemGeneratedReports_AccountingChecklists.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_ManagementAccountsDataDump, "", SecureAreaEnum.F_SystemGeneratedReports_ManagementAccountsDataDump.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_ManagementAccounts, "", SecureAreaEnum.F_SystemGeneratedReports_ManagementAccounts.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_BuildingCouncilDetails_InvoiceItem_MonthsSync, "", SecureAreaEnum.F_SystemGeneratedReports_BuildingCouncilDetails_InvoiceItem_MonthsSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_NetcashServicesChargesRecon, "", SecureAreaEnum.F_SystemGeneratedReports_NetcashServicesChargesRecon.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_B01_AccountPayments_SupplyCostSettingsTemplates_ProductsSync, "", SecureAreaEnum.F_SystemGeneratedReports_B01_AccountPayments_SupplyCostSettingsTemplates_ProductsSync.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_NetcashAutomation_PaymentsToCouncil_CRP, "", SecureAreaEnum.F_SystemGeneratedReports_NetcashAutomation_PaymentsToCouncil_CRP.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_NetcashAutomation_PaymentsforMeterRental_IAT, "", SecureAreaEnum.F_SystemGeneratedReports_NetcashAutomation_PaymentsforMeterRental_IAT.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_B05SupplyPayments_PaymentDetailsDocumentNoAllocation, "", SecureAreaEnum.F_SystemGeneratedReports_B05SupplyPayments_PaymentDetailsDocumentNoAllocation.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.F_SystemGeneratedReports_D01_Snapshot, "", SecureAreaEnum.F_SystemGeneratedReports_D01_Snapshot.GetDescription()),
                   });


                #endregion

                #region G - Communication 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.G_Communication, "", ParentSecureAreaEnum.G_Communication.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.G_Communication_BulkCommunication, "", SecureAreaEnum.G_Communication_BulkCommunication.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.G_Communication_NotificationLog, "", SecureAreaEnum.G_Communication_NotificationLog.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.G_Communication_AppNotificationLog, "", SecureAreaEnum.G_Communication_AppNotificationLog.GetDescription()),
                    }); ;

                #endregion

                #region H - Device Administrator 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.H_Device_Administrator, "", ParentSecureAreaEnum.H_Device_Administrator.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_DeviceOverview, "", SecureAreaEnum.H_Device_Administrator_DeviceOverview.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway, "", SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_DeviceNameBulkUpdate, "", SecureAreaEnum.H_Device_Administrator_DeviceNameBulkUpdate.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_Bulk433TestingUpload, "", SecureAreaEnum.H_Device_Administrator_Bulk433TestingUpload.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_AutoDeviceDiscovery, "", SecureAreaEnum.H_Device_Administrator_AutoDeviceDiscovery.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_WirelessSignalOptimizer, "", SecureAreaEnum.H_Device_Administrator_WirelessSignalOptimizer.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Summary, "", SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Details, "", SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request, "", SecureAreaEnum.H_Device_Administrator_ActiveEnergyAnomalies_Request.GetDescription()),
                        }); ;


                #endregion

                #region J - Finance 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.J_Finance, "", ParentSecureAreaEnum.J_Finance.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_CompanyFinancialDetails, "", SecureAreaEnum.J_Finance_CompanyFinancialDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_Administration, "", SecureAreaEnum.J_Finance_Administration.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_ReceiptLog, "", SecureAreaEnum.J_Finance_ReceiptLog.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_ReceiptLogExceptions, "", SecureAreaEnum.J_Finance_ReceiptLogExceptions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_ExternalCharges, "", SecureAreaEnum.J_Finance_ExternalCharges.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_MeterReconReport, "", SecureAreaEnum.J_Finance_MeterReconReport.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_BulkReadingsExport, "", SecureAreaEnum.J_Finance_BulkReadingsExport.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalPaymentAllocation, "", SecureAreaEnum.J_Finance_JournalPaymentAllocation.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary, "", SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalSagepayRelease_Details, "", SecureAreaEnum.J_Finance_JournalSagepayRelease_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary, "", SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalCigicellRecon_Daily, "", SecureAreaEnum.J_Finance_JournalCigicellRecon_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary, "", SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_WinshuttleExport, "", SecureAreaEnum.J_Finance_WinshuttleExport.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_WinshuttleExportRequests, "", SecureAreaEnum.J_Finance_WinshuttleExportRequests.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_SagepayAllocationExceptions, "", SecureAreaEnum.J_Finance_SagepayAllocationExceptions.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_UnipinAllocationExceptions, "", SecureAreaEnum.J_Finance_UnipinAllocationExceptions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_AllocationRerun, "", SecureAreaEnum.J_Finance_AllocationRerun.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_PQAllocation, "", SecureAreaEnum.J_Finance_PQAllocation.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashMissingSkybillJournals, "", SecureAreaEnum.J_Finance_NetcashMissingSkybillJournals.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashManualPayments, "", SecureAreaEnum.J_Finance_NetcashManualPayments.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashReport_Summary, "", SecureAreaEnum.J_Finance_NetcashReport_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashReport_Monthly, "", SecureAreaEnum.J_Finance_NetcashReport_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashReport_Daily, "", SecureAreaEnum.J_Finance_NetcashReport_Daily.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashReport_Details, "", SecureAreaEnum.J_Finance_NetcashReport_Details.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashServicesChargesRecon, "", SecureAreaEnum.J_Finance_NetcashServicesChargesRecon.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashTransactionsRecon, "", SecureAreaEnum.J_Finance_NetcashTransactionsRecon.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_NetcashTransactionsRecon_Daily, "", SecureAreaEnum.J_Finance_NetcashTransactionsRecon_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_CigicellWeeklySummary, "", SecureAreaEnum.J_Finance_CigicellWeeklySummary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_CigicellWeeklyDetails, "", SecureAreaEnum.J_Finance_CigicellWeeklyDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_CigicellTransactionDetails, "", SecureAreaEnum.J_Finance_CigicellTransactionDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.J_Finance_ExternalChargedSummary, "", SecureAreaEnum.J_Finance_ExternalChargedSummary.GetDescription()),
                    });


                #endregion

                #region L - Meter Rentals 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.L_MeterRentals, "", ParentSecureAreaEnum.L_MeterRentals.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Summary, "", SecureAreaEnum.L_MeterRentals_Summary.GetDescription()),// global
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Details, "", SecureAreaEnum.L_MeterRentals_Details.GetDescription()),// per company errors
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Review, "", "L1.013 Meter Rentals Review"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Results, "", SecureAreaEnum.L_MeterRentals_Results.GetDescription()),// per company all results

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Summary, "", SecureAreaEnum.L_MeterRentals_Cost_Summary.GetDescription()),// global
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Details, "", SecureAreaEnum.L_MeterRentals_Cost_Details.GetDescription()),// per company errors
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Review, "", "L1.023 Meter Cost Review"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Results, "", SecureAreaEnum.L_MeterRentals_Cost_Results.GetDescription()),// per company all results

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Gateway_Details, "", SecureAreaEnum.L_MeterRentals_Cost_Gateway_Details.GetDescription()),// per company errors
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Gateway_Review, "", "L1.023 Gateway Cost Review"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Cost_Gateway_Results, "", SecureAreaEnum.L_MeterRentals_Cost_Gateway_Results.GetDescription()),// per company all results

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Summary, "", SecureAreaEnum.L_MeterRentals_Accounting_Summary.GetDescription()),// global
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Details, "", SecureAreaEnum.L_MeterRentals_Accounting_Details.GetDescription()),// per company errors
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Review, "", "L2.013 Meter Rentals Review - Accounting"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Results, "", SecureAreaEnum.L_MeterRentals_Accounting_Results.GetDescription()),// per company all results

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Device_Summary, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Device_Summary.GetDescription()),// global
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Summary, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Summary.GetDescription()),// global

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Summary, "", "L2.021 Meter Cost Summary - Accounting"),// global
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Details, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Details.GetDescription()),// per company errors
                        ////new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Review, "", "L2.023 Meter Cost Review - Accounting"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Results, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Results.GetDescription()),// per company all results

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Details, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Details.GetDescription()),// per company errors
                        //////new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Review, "", "L2.023 Gateway Cost Review - Accounting"),// meter based
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Results, "", SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Results.GetDescription()),// per company all results

                    }); ;


                #endregion

                #region M - Technician Dispatch 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.M_TechnicianDispatch, "", ParentSecureAreaEnum.M_TechnicianDispatch.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.M_TechnicianDispatch_VehicleOverview, "", SecureAreaEnum.M_TechnicianDispatch_VehicleOverview.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.M_TechnicianDispatch_TechnicanDispatchSummary, "", "M2.011 Technican Dispatch Summary"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.M_TechnicianDispatch_TechnicanDispatchRequired, "", "M2.012 Technican Dispatch Required"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.M_TechnicianDispatch_TechnicanDispatched, "", "M2.013 Technican Dispatched"),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.M_TechnicianDispatch_MyDispatches, "", "M2.014 My Dispatches"),
                    }); ;


                #endregion

                #region N - Technician Toolkit 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.N_TechnicianToolkit, "", ParentSecureAreaEnum.N_TechnicianToolkit.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_BuildingDetails, "", SecureAreaEnum.N_TechnicianToolkit_BuildingDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails, "", SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_CommunicationGateways, "", SecureAreaEnum.N_TechnicianToolkit_CommunicationGateways.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_OfflineDevices, "", SecureAreaEnum.N_TechnicianToolkit_OfflineDevices.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_AllDevices, "", SecureAreaEnum.N_TechnicianToolkit_AllDevices.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, "", SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_NewlyAddedDevices, "", SecureAreaEnum.N_TechnicianToolkit_NewlyAddedDevices.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_TokenSender, "", SecureAreaEnum.N_TechnicianToolkit_TokenSender.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.N_TechnicianToolkit_TokenLog, "", SecureAreaEnum.N_TechnicianToolkit_TokenLog.GetDescription()),
                    }); ;


                #endregion

                #region S - CombinedReports 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.S_CombinedReports, "", ParentSecureAreaEnum.S_CombinedReports.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Monthly, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Daily, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Monthly, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Daily, "", SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary, "", SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Monthly, "", SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Daily, "", SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary, "", SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Monthly, "", SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Daily, "", SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary, "", SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Monthly, "", SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Daily, "", SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Monthly, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Daily, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily, "", SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_OverallAnalysis_Summary, "", SecureAreaEnum.S_CombinedReports_OverallAnalysis_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_OverallAnalysis_Monthly, "", SecureAreaEnum.S_CombinedReports_OverallAnalysis_Monthly.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_OverallAnalysis_Daily, "", "S0.013 Overall Analysis Daily"),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary, "", SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly, "", SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Daily, "", SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Daily.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Summary, "", SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Monthly, "", SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Monthly.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Daily, "", SecureAreaEnum.S_CombinedReports_BilledMeterReadings_Daily.GetDescription()),

                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.S02_ProductCombinedReports, "", ParentSecureAreaEnum.S02_ProductCombinedReports.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily, "", SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily.GetDescription()),

                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Summary, "", SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Summary.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Monthly, "", SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Monthly.GetDescription()),


                    });


                #endregion

                #region V - Policies & Workflow Allocations

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.V01_Policies, "", ParentSecureAreaEnum.V01_Policies.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V01_Policies_All, "", SecureAreaEnum.V01_Policies_All.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V01_Policies_MyPolicies, "", SecureAreaEnum.V01_Policies_MyPolicies.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V01_Policies_View, "", SecureAreaEnum.V01_Policies_View.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.V02_Workflow_Allocations, "", ParentSecureAreaEnum.V02_Workflow_Allocations.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V02_Workflow_Allocations_AllTaskAllocations, "", SecureAreaEnum.V02_Workflow_Allocations_AllTaskAllocations.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V02_Workflow_Allocations_MyTaskAllocations, "", SecureAreaEnum.V02_Workflow_Allocations_MyTaskAllocations.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations, "", SecureAreaEnum.V02_Workflow_Allocations_ViewTaskAllocations.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.V03_HumanResources, "", ParentSecureAreaEnum.V03_HumanResources.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V03_HumanResources_AllHumanResources, "", SecureAreaEnum.V03_HumanResources_AllHumanResources.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V03_HumanResources_PersonalDetails, "", SecureAreaEnum.V03_HumanResources_PersonalDetails.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V03_HumanResources_JobDescriptions, "", SecureAreaEnum.V03_HumanResources_JobDescriptions.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V03_HumanResources_CorrespondenceList, "", SecureAreaEnum.V03_HumanResources_CorrespondenceList.GetDescription()),
                    });

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.V04_InternalMeetings, "", ParentSecureAreaEnum.V04_InternalMeetings.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_ExcoMeetings, "", SecureAreaEnum.V04_InternalMeetings_ExcoMeetings.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_MancoMeetings, "", SecureAreaEnum.V04_InternalMeetings_MancoMeetings.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_RocksMeetings, "", SecureAreaEnum.V04_InternalMeetings_RocksMeetings.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_StandupMeetings, "", SecureAreaEnum.V04_InternalMeetings_StandupMeetings.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_CustomerCareMeeting, "", SecureAreaEnum.V04_InternalMeetings_CustomerCareMeeting.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_TechnicianDispatch, "", SecureAreaEnum.V04_InternalMeetings_TechnicianDispatch.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_ITSystemMeeting, "", SecureAreaEnum.V04_InternalMeetings_ITSystemMeeting.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_SalesMeeting, "", SecureAreaEnum.V04_InternalMeetings_SalesMeeting.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_OnboardingMeeting, "", SecureAreaEnum.V04_InternalMeetings_OnboardingMeeting.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.V04_InternalMeetings_CloudCastMeetings, "", SecureAreaEnum.V04_InternalMeetings_CloudCastMeetings.GetDescription()),
                    });

                #endregion

                #region W - Activity Logs 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.W01_ActivityLogs, "", ParentSecureAreaEnum.W01_ActivityLogs.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimePlanner_Summary, "", SecureAreaEnum.W01_ActivityLogs_TimePlanner_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimePlanner_Details, "", SecureAreaEnum.W01_ActivityLogs_TimePlanner_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimePlanner_Results, "", SecureAreaEnum.W01_ActivityLogs_TimePlanner_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Summary, "", SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Details, "", SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Results, "", SecureAreaEnum.W01_ActivityLogs_TimeAllocated_Results.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_MissingTimeAllocated_Summary, "", SecureAreaEnum.W01_ActivityLogs_MissingTimeAllocated_Summary.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Summary, "", SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Details, "", SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Results, "", SecureAreaEnum.W01_ActivityLogs_TravelAllocation_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_StockAllocation_Summary, "", SecureAreaEnum.W01_ActivityLogs_StockAllocation_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_StockAllocation_Details, "", SecureAreaEnum.W01_ActivityLogs_StockAllocation_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_StockAllocation_Results, "", SecureAreaEnum.W01_ActivityLogs_StockAllocation_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Summary, "", SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Summary.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Details, "", SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Details.GetDescription()),
                        //new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Results, "", SecureAreaEnum.W01_ActivityLogs_InvoiceAllocation_Results.GetDescription()),

                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.W01_ActivityLogs_UserActivity_Details, "", SecureAreaEnum.W01_ActivityLogs_UserActivity_Details.GetDescription()),
                    });

                #endregion

                #region X - MeterTeam Admin 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.X_MeterTeamAdmin, "", ParentSecureAreaEnum.X_MeterTeamAdmin.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.X_MeterTeamAdmin_Customers, "", SecureAreaEnum.X_MeterTeamAdmin_Customers.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.X_MeterTeamAdmin_Customers_Add, "", SecureAreaEnum.X_MeterTeamAdmin_Customers_Add.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, "", SecureAreaEnum.X_MeterTeamAdmin_RecourceLists.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.X_MeterTeamAdmin_RecourceLists_Add, "", SecureAreaEnum.X_MeterTeamAdmin_RecourceLists_Add.GetDescription()),
                    });


                #endregion

                #region Z - Bugs 

                defaultSecureAreas.Add(new Tuple<ParentSecureAreaEnum, string, string>(ParentSecureAreaEnum.Z_Bugs, "bug", ParentSecureAreaEnum.Z_Bugs.GetDescription()),
                    new List<Tuple<SecureAreaEnum, string, string>>()
                    {
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Z_Bugs_ReportABug, "", SecureAreaEnum.Z_Bugs_ReportABug.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Z_Bugs_MyBugs, "", SecureAreaEnum.Z_Bugs_MyBugs.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Z_Bugs_BugDetail, "", SecureAreaEnum.Z_Bugs_BugDetail.GetDescription()),
                        new Tuple<SecureAreaEnum, string, string>( SecureAreaEnum.Z_Bugs_BugAdmin, "", SecureAreaEnum.Z_Bugs_BugAdmin.GetDescription()),
                    });


                #endregion

                return defaultSecureAreas;
            }
        }

        public static List<SecureAreaEnum> SecureAreasProgrammedForFlags
        {
            get
            {
                return new List<SecureAreaEnum>()
                {
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayResults,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayDetails,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewaySummary,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_GatewayVerification,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceResults,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceDetails,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceSummary,
                    SecureAreaEnum.A01_GatewayAndDeviceMonitoring_DeviceVerification,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationDetails,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationResults,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationSummary,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MeterCalibrationVerification,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingDetails,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingResults,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingSummary,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingUpdate,
                    SecureAreaEnum.A02_MirrorMeterAuditing_MirrorReadingVerification,
                    SecureAreaEnum.A03_NetworkBalancing_Capture,
                    SecureAreaEnum.A03_NetworkBalancing_Details,
                    SecureAreaEnum.A03_NetworkBalancing_Summary,
                    SecureAreaEnum.A06_BillingControlReport_OccupancyDetails,
                    SecureAreaEnum.A06_BillingControlReport_OccupancyResults,
                    SecureAreaEnum.A06_BillingControlReport_OccupancySummary,
                    SecureAreaEnum.A06_BillingControlReport_OccupancyVerification,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlReview,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlDetails,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlResults,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary,
                    SecureAreaEnum.A06_BillingControlReport_NotBilledDetails,
                    SecureAreaEnum.A06_BillingControlReport_NotBilledResults,
                    SecureAreaEnum.A06_BillingControlReport_NotBilledSummary,
                    SecureAreaEnum.A06_BillingControlReport_NotBilledVerification,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeDetails,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeResults,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview,
                    SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary,
                };
            }
        }
    }
}
