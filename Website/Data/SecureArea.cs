using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SecureArea
    {
        [Key]
        public int SecureAreaID { get; set; }
        public SecureAreaEnum SecureAreaEnum { get { return (SecureAreaEnum)SecureAreaID; } }
        public int ParentSecureAreaID { get; set; }
        public ParentSecureAreaEnum ParentSecureAreaEnum { get { return (ParentSecureAreaEnum)ParentSecureAreaID; } }
        public string SecureAreaCodeName { get; set; }
        public string SecureAreaDisplayName { get; set; }
        public string SecureAreaDisplayIcon { get; set; }
        public int? GroupID { get; set; }
        public GroupEnum Group
        {
            get
            {
                if (GroupID.HasValue)
                    return (GroupEnum)GroupID.Value;

                return GroupEnum.NotLinked;
            }
        }
        public enum GroupEnum
        {
            [Description("[Not Linked]")]
            NotLinked = 0,
            [Description("A1.01 Gateway")]
            A1_01_Gateway = 1,
            [Description("A1.02 Device")]
            A1_02_Device = 2,
            [Description("A10.01 Virtual Meter")]
            A10_01_VirtualMeter = 3,
            [Description("A10.02 TOU Recon Meter")]
            A10_02_TOUReconMeter = 4,
            [Description("A2.01 Meter Calibration")]
            A2_01_MeterCalibration = 5,
            [Description("A2.02 Mirror Reading")]
            A2_02_MirrorReading = 6,
            [Description("A2.03 Mirror Checklist")]
            A2_03_MirrorChecklist = 7,
            [Description("A2.05 Mirror Device")]
            A2_05_MirrorDevice = 8,
            [Description("A2.06 Mirror Reading Delete")]
            A2_06_MirrorReadingDelete = 9,
            [Description("A3.01 Network balancing")]
            A3_01_NetworkBalancing = 10,
            [Description("A4.01 Zendesk Tickets")]
            A4_01_ZendeskTickets = 11,
            [Description("A4.02 Zendesk Agents")]
            A4_02_ZendeskAgents = 12,
            [Description("A4.03 Zendesk Ticket Category")]
            A4_03_ZendeskTicketCategory = 13,
            [Description("A5.01 Midnight Sync")]
            A5_01_MidnightSync = 14,
            [Description("A6.01 Occupancy")]
            A6_01_Occupancy = 15,
            [Description("A6.03 Not Billed")]
            A6_03_NotBilled = 16,
            [Description("A6.04 Billing Blocked")]
            A6_04_BillingBlocked = 17,
            [Description("A6.05 Daily Billing Overview")]
            A6_05_DailyBillingOverview = 18,
            [Description("A6.05 Prepaid Control")]
            A6_05_PrepaidControl = 19,
            [Description("A6.06 Tariff Review")]
            A6_06_TariffReview = 20,
            [Description("A7.02 - Credit Control")]
            A7_02_CreditControl = 21,
            [Description("A7.03 - Meter Mode")]
            A7_03_MeterMode = 22,
            [Description("A7.04 - Meter On Manual")]
            A7_04_MeterOnManual = 23,
            [Description("A7.05 Connector")]
            A7_05_Connector = 24,
            [Description("A7.06 Aging of customers")]
            A7_06_AgingOfCustomers = 25,
            [Description("A8.00 Tasks Search")]
            A8_00_TasksSearch = 26,
            [Description("A8.01 Tasks Company")]
            A8_01_TasksCompany = 27,
            [Description("A8.02 Tasks User")]
            A8_02_TasksUser = 28,
            [Description("A8.03 Tasks Type")]
            A8_03_TasksType = 29,
            [Description("A8.04 Task Create")]
            A8_04_TaskCreate = 30,
            [Description("A8.04 Tasks Details")]
            A8_04_TasksDetails = 31,
            [Description("A9.00 Flags Search")]
            A9_00_FlagsSearch = 32,
            [Description("A9.01 Flags Company")]
            A9_01_FlagsCompany = 33,
            [Description("A9.02 Flags User")]
            A9_02_FlagsUser = 34,
            [Description("A9.03 Flags Type")]
            A9_03_FlagsType = 35,
            [Description("A9.04 Flag Review")]
            A9_04_FlagReview = 36,
            [Description("A9.05 Flag Create")]
            A9_05_FlagCreate = 37,
            [Description("AF1.01 Metering Details")]
            AF1_01_MeteringDetails = 38,
            [Description("AF2.01 Meter Reading")]
            AF2_01_MeterReading = 39,
            [Description("AF5.01 Afrox Add Device")]
            AF5_01_AfroxAddDevice = 40,
            [Description("AF6.01 Gas Network Balancing")]
            AF6_01_GasNetworkBalancing = 41,
            [Description("AF7.01 Winshuttle")]
            AF7_01_Winshuttle = 42,
            [Description("B1.01 Account Details")]
            B1_01_AccountDetails = 43,
            [Description("B1.02 Supply Cost Settings")]
            B1_02_SupplyCostSettings = 44,
            [Description("B2.01 Council Reading")]
            B2_01_CouncilReading = 45,
            [Description("B2.02 Council Meter")]
            B2_02_CouncilMeter = 46,
            [Description("B2.03 Council Reading Planner")]
            B2_03_CouncilReadingPlanner = 47,
            [Description("B3.01 Council Statement")]
            B3_01_CouncilStatement = 48,
            [Description("B4.01 Council Check Recon")]
            B4_01_CouncilCheckRecon = 49,
            [Description("B5.01 Payment")]
            B5_01_Payment = 50,
            [Description("C1.01 Product Report")]
            C1_01_ProductReport = 51,
            [Description("C2.01 GL Report")]
            C2_01_GLReport = 52,
            [Description("C3.02 Average and per Unit Report")]
            C3_02_AverageAndPerUnitReport = 53,
            [Description("C4.011 Product Profit Report")]
            C4_011_ProductProfitReport = 54,
            [Description("C4.012 Property Profit Report")]
            C4_012_PropertyProfitReport = 55,
            [Description("C5.01 Billings to Owner")]
            C5_01_BillingsToOwner = 56,
            [Description("C6.01 TB and GL Recon Report")]
            C6_01_TBAndGLReconReport = 57,
            [Description("C6.02 Ledger Recon Report")]
            C6_02_LedgerReconReport = 58,
            [Description("D1.1 Sales Leads log")]
            D1_1_SalesLeadslog = 59,
            [Description("E1.01 Tasks Company")]
            E1_01_TasksCompany = 60,
            [Description("E1.02 Tasks User")]
            E1_02_TasksUser = 61,
            [Description("E1.03 Tasks Type")]
            E1_03_TasksType = 62,
            [Description("E1.04 Task Details")]
            E1_04_TaskDetails = 63,
            [Description("E2.01 Safety File Answers")]
            E2_01_SafetyFileAnswers = 64,
            [Description("FA1.01 Gateways and Devices Reports")]
            FA1_01_GatewaysAndDevicesReports = 65,
            [Description("FA2.011 Mirror Device Reports")]
            FA2_011_MirrorDeviceReports = 66,
            [Description("FA5.01 Device Reading Midnight Sync")]
            FA5_01_DeviceReadingMidnightSync = 67,
            [Description("FA6.01 Billing Control Reports")]
            FA6_01_BillingControlReports = 68,
            [Description("FA7.01 Credit Control Reports")]
            FA7_01_CreditControlReports = 69,
            [Description("FC1.01 General Ledger")]
            FC1_01_GeneralLedger = 70,
            [Description("FC2.01 Rental Book")]
            FC2_01_RentalBook = 71,
            [Description("FC3.01 Master Reports")]
            FC3_01_MasterReports = 72,
            [Description("G1.01 Bulk Communication")]
            G1_01_BulkCommunication = 73,
            [Description("G2.01 Notification Log")]
            G2_01_NotificationLog = 74,
            [Description("H1.01 Device Administrator")]
            H1_01_DeviceAdministrator = 75,
            [Description("H2.01 Active Energy Anomalies")]
            H2_01_ActiveEnergyAnomalies = 76,
            [Description("J0.00 Company Financial Details")]
            J0_00_CompanyFinancialDetails = 77,
            [Description("J1.01 Administration")]
            J1_01_Administration = 78,
            [Description("J2.01 Receipt Details")]
            J2_01_ReceiptDetails = 79,
            [Description("J3.01 External Charges")]
            J3_01_ExternalCharges = 80,
            [Description("J4.01 Meter Recon Report")]
            J4_01_MeterReconReport = 81,
            [Description("J5.01 Bulk Readings Export")]
            J5_01_BulkReadingsExport = 82,
            [Description("J6.01 Journal Payment Allocation")]
            J6_01_JournalPaymentAllocation = 83,
            [Description("J6.01 PQ Allocation")]
            J6_01_PQAllocation = 84,
            [Description("J6.02 Cigicell Recon")]
            J6_02_CigicellRecon = 85,
            [Description("J6.02 Netcash Recon")]
            J6_02_NetcashRecon = 86,
            [Description("J8.01 Netcash Releases")]
            J8_01_NetcashReleases = 87,
            [Description("J9.01 Netcash Report")]
            J9_01_NetcashReport = 88,
            [Description("L1.01 Meter Rentals")]
            L1_01_MeterRentals = 89,
            [Description("L1.02 Meter Cost")]
            L1_02_MeterCost = 90,
            [Description("L2.01 Meter Rentals - Accounting")]
            L2_01_MeterRentals_Accounting = 91,
            [Description("L2.02 Device Cost - Accounting")]
            L2_02_DeviceCost_Accounting = 92,
            [Description("M1.01 Vehicle Overview")]
            M1_01_VehicleOverview = 93,
            [Description("N1.01 Building Details")]
            N1_01_BuildingDetails = 94,
            [Description("N2.01 Installed devices")]
            N2_01_InstalledDevices = 95,
            [Description("N3.01 Add Devices")]
            N3_01_AddDevices = 96,
            [Description("N4.01 Tokens")]
            N4_01_Tokens = 97,
            [Description("S0.01 Overall Analysis")]
            S0_01_OverallAnalysis = 98,
            [Description("S1.01 Billing Analysis Amount")]
            S1_01_BillingAnalysis_Amount = 99,
            [Description("S1.02 Billing Analysis Units")]
            S1_02_BillingAnalysis_Units = 100,
            [Description("S1.03 Metered Analysis Units")]
            S1_03_MeteredAnalysis_Units = 101,
            [Description("S1.04 Unbilled Analysis Units")]
            S1_04_UnbilledAnalysis_Units = 102,
            [Description("S1.05 Cost Analysis Amount")]
            S1_05_CostAnalysis_Amount = 103,
            [Description("S1.06 Profit Analysis Amount")]
            S1_06_ProfitAnalysis_Amount = 104,
            [Description("S1.07 Profit Analysis Gross Profit %")]
            S1_07_ProfitAnalysis_GrossProfit = 105,
            [Description("S1.08 Network Balancing Analysis Units")]
            S1_08_NetworkBalancingAnalysis_Units = 106,
            [Description("Skybill Journal Logs")]
            Skybill_Journal_Logs = 107,
            [Description("SOC Summary")]
            SOC_Summary = 108,
            [Description("V1.01 Company Policies")]
            V1_01_CompanyPolicies = 109,
            [Description("V2.01 Task Allocations")]
            V2_01_TaskAllocations = 110,
            [Description("V3.01 All Human Resources")]
            V3_01_AllHumanResources = 111,
            [Description("Y00.C01 Product Report")]
            Y00_C01_ProductReport = 112,
            [Description("Z 01 System Bugs")]
            Z_01_SystemBugs = 113,
            [Description("Delete")]
            Delete = 114,
            [Description("Y00.A04 Tickets")]
            Y00_A04_Tickets = 115,
            [Description("Y00.A10 Virtual Meters")]
            Y00_A10_VirtualMeters = 116,
            [Description("Y00.B01 Supply Account Details")]
            Y00_B01_SupplyAccountDetails = 117,


            [Description("B6.01 - Supply Correspondence")]
            B6_01_SupplyCorrespondence = 118,
            [Description("C7.01 - External Reporting")]
            C7_01_ExternalReporting = 119,
            [Description("C8.01 - Exco Reporting")]
            C8_01_ExcoReporting = 120,
            [Description("C9.01 - Board Reporting")]
            C9_01_BoardReporting = 121,
            [Description("X0.01 - IT Systems")]
            X0_01_ITSystems = 122,
            [Description("X1.01 - Azure Servers")]
            X1_01_AzureServers = 123,
            [Description("X2.01 - Prism")]
            X2_01_Prism = 124,
            [Description("X3.01 - Cigicell Integration")]
            X3_01_CigicellIntegration = 125,
            [Description("X4.01 - M2M Servers")]
            X4_01_M2MServers = 126,
            [Description("X5.01 - Kronika Servers")]
            X5_01_KronikaServers = 127,
            [Description("X6.01 - Netcash Integration")]
            X6_01_NetcashIntegration = 128,
            [Description("X7.01 - Domains and hosting")]
            X7_01_DomainsAndHosting = 129,
            [Description("X8.01 - Skybill Integration")]
            X8_01_SkybillIntegration = 130,
            [Description("V4.01 - Exco Meeting")]
            V4_01_ExcoMeeting = 131,
            [Description("V4.02 - Manco Meeting")]
            V4_02_MancoMeeting = 132,
            [Description("V4.03 - Rocks Meeting")]
            V4_03_RocksMeeting = 133,
            [Description("V4.04 - Standup Meeting")]
            V4_04_StandupMeeting = 134,
            [Description("V4.05 - Cloud Cast Meeting")]
            V4_05_CloudCastMeeting = 135,
            [Description("V5.01 - Internal Admin")]
            V5_01_InternalAdmin = 136,
            [Description("W1.01 - Time Planner")]
            W1_01_TimePlanner = 137,
            [Description("W1.01 - Time Allocated")]
            W1_01_TimeAllocated = 138,
            [Description("W1.01 - Travel Allocated")]
            W1_01_TravelAllocated = 139,
            [Description("W1.01 - Stock Allocated")]
            W1_01_StockAllocated = 140,
            [Description("W1.01 - Invoice Allocation")]
            W1_01_InvoiceAllocation = 141,

        }
    }
}
