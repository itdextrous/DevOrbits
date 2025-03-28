using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Zendesk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Jobs
{
    public static class JobList
    {
        //private static bool _runNotifications = false;
        //public static IRecurringJobManager AddJobList(this IRecurringJobManager manager, IConfiguration config, bool isDevEnvironment)
        //{
        //    //////////////////////////////////////////////
        //    //////////// HANGFIRE USES GMT 0 /////////////
        //    //////////////////////////////////////////////

        //    manager.RemoveIfExists("NotificationsUpdater");
        //    manager.RemoveIfExists("UniPinMailerDaily");
        //    manager.RemoveIfExists("UniPinMailerWeekly");
        //    manager.RemoveIfExists("UniPinMailerMonthly");
        //    manager.RemoveIfExists("ZendeskJob_Sync");

        //    manager.AddOrUpdate("ZendeskJob_TicketFieldOptionsSync", Job.FromExpression<ZendeskJobs.ZendeskJob_TicketFieldOptionsSync>(x => x.Run()), Cron.Hourly(9)); // 06:03
        //    manager.AddOrUpdate("ZendeskJob_UsersSync", Job.FromExpression<ZendeskJobs.ZendeskJob_UsersSync>(x => x.Run()), Cron.Hourly(10)); // 06:00
        //    manager.AddOrUpdate("ZendeskJob_UserFieldsSync", Job.FromExpression<ZendeskJobs.ZendeskJob_UserFieldsSync>(x => x.Run()), Cron.Hourly(15)); // 06:01
        //    manager.AddOrUpdate("ZendeskJob_OrganizationFieldsSync", Job.FromExpression<ZendeskJobs.ZendeskJob_OrganizationFieldsSync>(x => x.Run()), Cron.Hourly(16)); // 06:02
        //    manager.AddOrUpdate("ZendeskJob_TicketFieldsSync", Job.FromExpression<ZendeskJobs.ZendeskJob_TicketFieldsSync>(x => x.Run()), Cron.Hourly(17)); // 06:03
        //    manager.AddOrUpdate("ZendeskJob_TicketsSync", Job.FromExpression<ZendeskJobs.ZendeskJob_TicketsSync>(x => x.Run()), Cron.Hourly(18)); // 06:10

        //    manager.AddOrUpdate("SkybillJob_CustomersSync", Job.FromExpression<SkybillJobs.SkybillJob_CustomersSync>(x => x.Run()), Cron.Hourly(30)); // Every hour

        //    manager.AddOrUpdate("SkybillJob_CustomersUtilitiesSync", Job.FromExpression<SkybillJobs.SkybillJob_CustomersUtilitiesSync>(x => x.Run()), Cron.Daily(4)); // Every hour

        //    manager.AddOrUpdate("M2MJob_MeterActiveEnergyAnomalies", Job.FromExpression<M2MJobs.M2MJob_MeterActiveEnergyAnomalies>(x => x.Run()), Cron.Monthly(31)); // 04:15

        //    manager.RemoveIfExists("SQLJobs_RentalBook");
        //    //manager.AddOrUpdate("SQLJobs_RentalBook", Job.FromExpression<SQLJobs.SQLJobs_RentalBook>(x => x.Run()), Cron.Daily(2, 15)); // 04:15

        //    manager.AddOrUpdate("SkybillJob_CustomersSync", Job.FromExpression<SkybillJobs.SkybillJob_CustomersSync>(x => x.Run()), Cron.HourInterval(4)); // Every 4 hours

        //    manager.AddOrUpdate("M2MJob_GatewaysAndDevicesSync", Job.FromExpression<M2MJobs.M2MJob_GatewaysAndDevicesSync>(x => x.Run()), Cron.Daily(0)); // 02:00

        //    manager.AddOrUpdate("ArchivingJob_TokenLog", Job.FromExpression<ArchivingJobs.ArchivingJob_TokenLog>(x => x.Run()), Cron.Daily(21)); // 23:00

        //    //manager.AddOrUpdate("A01_GatewayAndDeviceMonitoring_GatewaysOffline", Job.FromExpression<A09_FlagsJobs.A01_GatewayAndDeviceMonitoring_GatewaysOffline>(x => x.Run()), Cron.Daily(4)); // 06:00
        //    //manager.AddOrUpdate("A01_DeviceAndDeviceMonitoring_DevicesOffline", Job.FromExpression<A09_FlagsJobs.A01_DeviceAndDeviceMonitoring_DevicesOffline>(x => x.Run()), Cron.Daily(4)); // 06:00

        //    //manager.AddOrUpdate("F_SystemGeneratedReports_A09FlagsDataDumpJob", Job.FromExpression<A09_FlagsJobs.F_SystemGeneratedReports_A09FlagsDataDumpJob>(x => x.Run()), Cron.Daily(21)); // 23:00

        //    //manager.AddOrUpdate("A2_MirrorMeterAuditing_Calibration", Job.FromExpression<A09_FlagsJobs.A2_MirrorMeterAuditing_Calibration>(x => x.Run()), Cron.Daily(21, 5)); // 23:05
        //    //manager.AddOrUpdate("A2_MirrorMeterAuditing_Reading", Job.FromExpression<A09_FlagsJobs.A2_MirrorMeterAuditing_Reading>(x => x.Run()), Cron.Daily(21, 10)); // 23:10
        //    //manager.AddOrUpdate("A7_CreditControlAndNotifierProcess_WalletInArears", Job.FromExpression<A09_FlagsJobs.A7_CreditControlAndNotifierProcess_WalletInArears>(x => x.Run()), Cron.Daily(21, 15)); // 23:15
        //    //manager.AddOrUpdate("A6_BillingControlReport_Occupancy", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_Occupancy>(x => x.Run()), Cron.Daily(21, 20)); // 23:20
        //    //manager.AddOrUpdate("A6_BillingControlReport_FaultyorTamperedMeter", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_FaultyorTamperedMeter>(x => x.Run()), Cron.Daily(21, 25)); // 23:25
        //    //manager.AddOrUpdate("A6_BillingControlReport_LastBilledExceedsLiveReading", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_LastBilledExceedsLiveReading>(x => x.Run()), Cron.Daily(21, 30)); // 23:30
        //    //manager.AddOrUpdate("A6_BillingControlReport_OccupancyStatusWrong", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_OccupancyStatusWrong>(x => x.Run()), Cron.Daily(21, 35)); // 23:35
        //    //manager.AddOrUpdate("A6_BillingControlReport_MeterCardSetupWrong", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_MeterCardSetupWrong>(x => x.Run()), Cron.Daily(21, 40)); // 23:40
        //    //manager.AddOrUpdate("A6_BillingControlReport_MeterOfflineMoreThan7Days", Job.FromExpression<A09_FlagsJobs.A6_BillingControlReport_MeterOfflineMoreThan7Days>(x => x.Run()), Cron.Daily(21, 45)); // 23:45


        //    manager.AddOrUpdate("SQLJobs_BillingControlReport_OccupancyReset", Job.FromExpression<SQLJobs.SQLJobs_BillingControlReport_OccupancyReset>(x => x.Run()), Cron.Daily(23)); // 23:50
        //    manager.AddOrUpdate("SQLJobs_RentalDataDump", Job.FromExpression<SQLJobs.SQLJobs_RentalDataDump>(x => x.Run()), Cron.Daily(22, 10)); // 00:10

        //    //manager.AddOrUpdate("A7_CreditControlAndNotifierProcess_MeterMode", Job.FromExpression<A09_FlagsJobs.A7_CreditControlAndNotifierProcess_MeterMode>(x => x.Run()), Cron.Daily(21, 55)); // 23:55

        //    //manager.AddOrUpdate("A7_CreditControlAndNotifierProcess_MeterOnManual", Job.FromExpression<A09_FlagsJobs.A7_CreditControlAndNotifierProcess_MeterOnManual>(x => x.Run()), Cron.Daily(22, 00)); // 00:00

        //    manager.AddOrUpdate("SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset", Job.FromExpression<SQLJobs.SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset>(x => x.Run()), Cron.Daily(22, 05)); // 00:05

        //    manager.AddOrUpdate("SkybillJob_ResourceListsSync", Job.FromExpression<SkybillJobs.SkybillJob_ResourceListsSync>(x => x.Run()), Cron.HourInterval(4)); // 00:05

        //    manager.AddOrUpdate("SkybillJob_ResourceLedgerEntriesSync", Job.FromExpression<SkybillJobs.SkybillJob_ResourceLedgerEntriesSync>(x => x.Run()), Cron.DayInterval(1)); // 00:05
        //    manager.RemoveIfExists("SkybillJob_ResourceLedgerEntriesSync");

        //    manager.AddOrUpdate("SkybillJob_DailyBillingNotifier", Job.FromExpression<SkybillJobs.SkybillJob_DailyBillingNotifier>(x => x.Run()), Cron.Daily(6)); // 08:00

        //    //manager.AddOrUpdate("A08_TasksJobs", Job.FromExpression<A08_TasksJobs.A08_TasksJobs>(x => x.Run()), Cron.Daily(3)); // 01:00
        //    //manager.AddOrUpdate("A08_TasksJobs2", Job.FromExpression<A08_TasksJobs.A08_TasksJobs>(x => x.Run()), Cron.Daily(5)); // 03:00

        //    manager.AddOrUpdate("J_Finance_WinshuttleExportJob", Job.FromExpression<ReportingJobs.J_Finance_WinshuttleExportJob>(x => x.Run()), Cron.Minutely()); // 01:00

        //    //manager.AddOrUpdate("SkybillJob_PaymentAllocations", Job.FromExpression<SkybillJobs.SkybillJob_PaymentAllocations>(x => x.Run(new DateTime(2021, 04, 01), isDevEnvironment)), Cron.Minutely()); // 01:00
        //    manager.AddOrUpdate("SkybillJob_PaymentAllocations", Job.FromExpression<SkybillJobs.SkybillJob_PaymentAllocations>(x => x.Run(DateTime.Now.AddHours(-2), isDevEnvironment)), Cron.MinuteInterval(10)); // 01:00

        //    manager.AddOrUpdate("SkybillJob_J_Finance_AllocationRerun", Job.FromExpression<SkybillJobs.SkybillJob_J_Finance_AllocationRerun>(x => x.Run()), Cron.Never()); // 01:00

        //    manager.AddOrUpdate("F_SystemGeneratedReports_Unipin_Daily", Job.FromExpression<ReportingJobs.UnipinReport>(x => x.Run(Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Daily)), Cron.Daily(0)); // 02:00
        //    manager.AddOrUpdate("F_SystemGeneratedReports_Unipin_Weekly", Job.FromExpression<ReportingJobs.UnipinReport>(x => x.Run(Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Weekly)), Cron.Weekly(DayOfWeek.Monday, 0)); // 02:00
        //    manager.AddOrUpdate("F_SystemGeneratedReports_Unipin_Monthly", Job.FromExpression<ReportingJobs.UnipinReport>(x => x.Run(Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Monthly)), Cron.Monthly(1, 0)); // 1st 02:00

        //    manager.AddOrUpdate("CompanyBalanceUpdates", Job.FromExpression<NetcashJobs.CompanyBalanceUpdates>(x => x.Run()), Cron.Hourly(15)); // 01:00

        //    manager.AddOrUpdate("NetcashJobs.StatementsSync", Job.FromExpression<NetcashJobs.StatementsSync>(x => x.Run()), Cron.Daily()); // Never

        //    manager.AddOrUpdate("NetcashJobs.StatementsSkybillAndPaymentsSync", Job.FromExpression<NetcashJobs.StatementsSkybillAndPaymentsSync>(x => x.Run()), Cron.Daily(2));

        //    manager.AddOrUpdate("SiteAdmin_Imports_RentalDataDumpJob", Job.FromExpression<ReportingJobs.SiteAdmin_Imports_RentalDataDumpJob>(x => x.Run()), Cron.Minutely()); // 01:00

        //    manager.AddOrUpdate("NetcashJobs.StatementsManualPaymentsSync", Job.FromExpression<NetcashJobs.StatementsManualPaymentsSync>(x => x.Run()), Cron.Hourly(16));
        
        //    manager.AddOrUpdate("H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob", Job.FromExpression<M2MJobs.H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob>(x => x.Run()), Cron.Minutely());

        //    manager.AddOrUpdate("AF_AfroxAdministration_Metering_Summary_Snapshots", Job.FromExpression<ReportingJobs.AF_AfroxAdministration_Metering_Summary_Snapshots>(x => x.Run(18)), Cron.Daily(5));

        //    if (isDevEnvironment)
        //    {
        //        manager.RemoveIfExists("ZendeskJob_UsersSync");
        //        manager.RemoveIfExists("ZendeskJob_UserFieldsSync");
        //        manager.RemoveIfExists("ZendeskJob_OrganizationFieldsSync");
        //        manager.RemoveIfExists("ZendeskJob_TicketFieldsSync");
        //        manager.RemoveIfExists("ZendeskJob_TicketsSync");
        //        manager.RemoveIfExists("ZendeskJob_TicketFieldOptionsSync");

        //        manager.RemoveIfExists("SkybillJob_CustomersSync");
        //        manager.RemoveIfExists("SkybillJob_CustomersUtilitiesSync");

        //        manager.RemoveIfExists("M2MJob_GatewaysAndDevicesSync");

        //        manager.RemoveIfExists("ArchivingJob_TokenLog");

        //        manager.RemoveIfExists("A01_GatewayAndDeviceMonitoring_GatewaysOffline");
        //        manager.RemoveIfExists("A01_DeviceAndDeviceMonitoring_DevicesOffline");

        //        manager.RemoveIfExists("F_SystemGeneratedReports_A09FlagsDataDumpJob");

        //        manager.RemoveIfExists("A2_MirrorMeterAuditing_Calibration");
        //        manager.RemoveIfExists("A2_MirrorMeterAuditing_Reading");
        //        manager.RemoveIfExists("A7_CreditControlAndNotifierProcess_WalletInArears");
        //        manager.RemoveIfExists("A6_BillingControlReport_Occupancy");
        //        manager.RemoveIfExists("A6_BillingControlReport_FaultyorTamperedMeter");
        //        manager.RemoveIfExists("A6_BillingControlReport_LastBilledExceedsLiveReading");
        //        manager.RemoveIfExists("A6_BillingControlReport_OccupancyStatusWrong");
        //        manager.RemoveIfExists("A6_BillingControlReport_MeterCardSetupWrong");
        //        manager.RemoveIfExists("A6_BillingControlReport_MeterOfflineMoreThan7Days");

        //        manager.RemoveIfExists("SQLJobs_BillingControlReport_OccupancyReset");

        //        manager.RemoveIfExists("SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset");
        //        manager.RemoveIfExists("A7_CreditControlAndNotifierProcess_MeterMode");
        //        manager.RemoveIfExists("A7_CreditControlAndNotifierProcess_MeterOnManual");

        //        manager.RemoveIfExists("SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset");
        //        manager.RemoveIfExists("SQLJobs_RentalDataDump");
        //        manager.RemoveIfExists("M2MJob_MeterActiveEnergyAnomalies");

        //        manager.RemoveIfExists("SkybillJob_ResourceListsSync");
        //        manager.RemoveIfExists("SkybillJob_ResourceLedgerEntriesSync");

        //        manager.RemoveIfExists("SkybillJob_DailyBillingNotifier");

        //        manager.RemoveIfExists("A08_TasksJobs");
        //        manager.RemoveIfExists("A08_TasksJobs2");

        //        manager.RemoveIfExists("J_Finance_WinshuttleExportJob");
        //        manager.RemoveIfExists("SkybillJob_PaymentAllocations");
        //        manager.RemoveIfExists("SkybillJob_J_Finance_AllocationRerun");

        //        manager.RemoveIfExists("F_SystemGeneratedReports_Unipin_Daily");
        //        manager.RemoveIfExists("F_SystemGeneratedReports_Unipin_Weekly");
        //        manager.RemoveIfExists("F_SystemGeneratedReports_Unipin_Monthly");

        //        manager.RemoveIfExists("CompanyBalanceUpdates");

        //        //manager.RemoveIfExists("NetcashJobs.StatementsSync");
        //        manager.RemoveIfExists("NetcashJobs.StatementsSkybillAndPaymentsSync");

        //        manager.RemoveIfExists("SiteAdmin_Imports_RentalDataDumpJob");

        //        manager.RemoveIfExists("NetcashJobs.StatementsManualPaymentsSync");
        //        manager.RemoveIfExists("H_Device_Administrator_ActiveEnergyAnomalies_RequestsJob");
        //        manager.RemoveIfExists("AF_AfroxAdministration_Metering_Summary_Snapshots");
        //    }

        //    return manager;
        //}
    }
}
