using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltageBLL
{
    public class MyVoltageBLLConfig
    {
        public string MyVoltageConnectionString { get; set; }
        public string MyVoltageAPIConnectionString { get; set; }
        public string MyVoltageLogConnectionString { get; set; }
        public string MyVoltageBillingConnectionString { get; set; }
        public string MyVoltageSkybillSyncConnectionString { get; set; }
        public string M2MBaseURL { get; set; }
        public string M2MUsername { get; set; }
        public string M2MPassword { get; set; }
        public string SkybillBaseURL { get; set; }
        public string SkybillUsername { get; set; }
        public string SkybillPassword { get; set; }
        public string SkybillTenant { get; set; }
    }

    public class MyVoltageConnectorConfig
    {
        public List<string> ToSendTo { get; set; }
    }

    public class OfflineGatewaysConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class MyVoltageNotifierAndDisconnectorConfig
    {
        public List<string> ToSendTo { get; set; }
        public List<int> CompaniesToExclude { get; set; }
    }

    public class OverdueAccountsConfig
    {
        public List<string> ToSendTo { get; set; }
    }

    public class UnipinConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class DeviceReadingUpdaterConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class GatewaysAndDevicesSyncConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class GatewaysAndDevicesSyncImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
    }
    public class DeviceReadingMidnightSyncConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class SkybillBillingConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class CheckSkybillBillingConfig
    {
        public List<string> ToSendTo { get; set; }
        public int UnbilledClientCount { get; set; }
    }
    public class PredictiveAnalyticsConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class DevicesMasterReportConfig
    {
        public List<string> ToSendTo { get; set; }
        public int DaysBillingToGet { get; set; }
        public int MonthsBillingsToGet { get; set; }
    }
    public class RentalBookConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class RentalDataDumpConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class ImportBillingFromFTPSiteConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class ReceiptReportConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class StockReportConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class OfflineDevicesReportConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class MirrorAuditReportConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class MirrorAuditDifferenceReportConfig
    {
        public List<string> ToSendTo { get; set; }
    }
    public class MirrorAuditImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class MirrorAuditCalibrationVerificationImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class BulkMeterImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }

    public class DetectAndMoveConfig
    {
        public List<string> ToSendTo { get; set; }
        public List<int> GatewayIDs { get; set; }
    }
    public class SignalOptimizerConfig
    {
        public List<string> ToSendTo { get; set; }
        public List<int> GatewayIDs { get; set; }
        public int SleepDurationMin { get; set; }
        public int LoopCount { get; set; }
        public decimal DontMoveAboveThisStrength { get; set; }
    }
    public class RentalBookImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class RentalExpensesImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class BuildingsMasterImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class BuildingsMasterReportConfig
    {
        public List<string> ToSendTo { get; set; }        
    }
    public class BuildingCouncilInvoicesImportConfig
    {
        public List<string> ToSendTo { get; set; }
        public string RootFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
    public class BuildingCouncilReconReportConfigSetting
    {
        public int CompanyID { get; set; }
        public List<string> ToSendTo { get; set; }        
    }
    public class CreditControlReportConfig
    {
        public List<string> ToSendTo { get; set; }        
    }
    public class BillingControlReportConfig
    {
        public List<string> ToSendTo { get; set; }        
    }
    public class BillingAlertReportConfig
    {
        public List<string> ToSendTo { get; set; }
        public int DaysBillingToGet { get; set; }
        public int MonthsBillingsToGet { get; set; }
    }
    public class BalanceNotificationSMSConfig
    {
        public DayOfWeek DayForWeekly { get; set; }
        public decimal DailyFee { get; set; }
        public decimal WeeklyFee { get; set; }
    }
    public class LoggedInLogSenderConfig
    {
        public List<string> ToSendTo { get; set; }
    }
}
